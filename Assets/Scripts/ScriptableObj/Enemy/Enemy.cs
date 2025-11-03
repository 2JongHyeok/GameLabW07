using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Data")] public EnemyBaseSO enemyData;

    [Header("References")] public Transform firePoint;

    [Header("Respawn Settings")] [SerializeField]
    private float maxDistanceFromTarget = 30f; // 타겟으로부터 최대 거리

    [SerializeField] private float respawnDistanceOffset = 2f;
    
    [HideInInspector] public GameObject activeSpecialEffect = null;

    // 런타임 상태 (외부에서 접근 필요)
    [HideInInspector] public Transform target;
    [HideInInspector] public IObjectPool<GameObject> myPool;
    [HideInInspector] public int enemyHP;
    [HideInInspector] public bool isDead = false;
    [HideInInspector] public bool isAttacking = false; // Planet1WaveManager에서 초기화
    [HideInInspector] public float attackTimer = 0f; // Planet1WaveManager에서 초기화
    public int enemyNum = 0;
    private bool hasLoggedFirstAttack = false; // 첫 공격 로그 여부
    private bool isBouncingBack = false; // Rammer가 후퇴 중인지 여부

    // 내부 상태
    private EnemyType enemyType;
    private float enemySpeed;
    public float attackCooldown;

    [HideInInspector] public int shieldHP = 0; // Commander가 부여하는 쉴드 HP

    // 피격 이펙트
    private HitFlashEffect hitFlashEffect;

    // 보스 처치 시 생성될 코어
    [SerializeField] private GameObject bossCorePrefab;
    public bool isHittedByObital = false;
    
    // --- [여기에 넉백 변수 추가] ---
    [Header("Knockback Settings")]
    [Tooltip("넉백 상태가 지속될 시간 (초)")]
    [SerializeField] private float knockbackDuration = 0.15f; 
    
    private bool isKnockedBack = false; // 넉백 상태인지 확인
    private float knockbackTimer = 0f;  // 넉백 남은 시간
    private Vector2 knockbackDirection; // 넉백 방향
    private float knockbackSpeed;       // 넉백 속도 (총알로부터 받음)
    // --- [넉백 변수 끝] ---




    private void Start()
    {
        // HitFlashEffect 컴포넌트 찾기
        hitFlashEffect = GetComponent<HitFlashEffect>();
        if (hitFlashEffect == null)
        {
            // 없으면 자동으로 추가
            hitFlashEffect = gameObject.AddComponent<HitFlashEffect>();
        }
    }

    public void SetTaget(Transform newTarget)
    {
        target = newTarget;
    }

    // Enemy.cs
    private void Update()
    {
        // [Rammer 로직] 후퇴 중일 땐 모든 이동/공격 로직 중지
        if (isBouncingBack)
        {
            return;
        }
        
        // --- [수정된 넉백 로직] ---
        // 1. 넉백/정지 상태인지 최우선으로 확인
        if (isKnockedBack)
        {
            // [수정]
            // 넉백 + 정지 시간(1.5f) 중, 타이머가 1.5f보다 클 때만 (즉, 넉백 이동 시간 동안만)
            // 뒤로 밀려나는 이동을 적용합니다.
            if (knockbackTimer > 0.5f)
            {
                transform.Translate(knockbackDirection * knockbackSpeed * Time.deltaTime, Space.World);
            }
            // (만약 knockbackTimer가 1.5f 이하 0f 초과가 되면, 
            //  이동(Translate)은 건너뛰고 타이머만 감소하므로 '정지' 상태가 됩니다.)

            // 넉백 타이머 감소
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0)
            {
                isKnockedBack = false; // 넉백 및 정지 상태 모두 종료
            }
            
            // 넉백 중에는 아래의 모든 일반 로직(이동, 공격, 회전)을 건너뜀
            return; 
        }
        // --- [넉백 로직 끝] ---
        
        // (타겟이 없으면 멈추는 로직 추가 권장)
        if (target == null) return;

        // 'isAttacking'이 true인 원거리/특수 유닛들의 공격 및 이동 로직
        if (isAttacking && (enemyData.enemyType == EnemyType.Ranger ||
                            enemyData.enemyType == EnemyType.RangerTank ||
                            enemyData.enemyType == EnemyType.Parasite ||
                            enemyData.enemyType == EnemyType.RailGun ||
                            enemyData.enemyType == EnemyType.Commander ||
                            enemyData.enemyType == EnemyType.Boss))
        {
            // 1. 공격에 필요한 사거리를 SO에서 가져옵니다.
            float currentAttackRange = 0f;
            switch (enemyData.enemyType)
            {
                case EnemyType.Ranger:
                    currentAttackRange = (enemyData as RangerEnemySO)?.attackRange ?? 0f;
                    break;
                case EnemyType.RangerTank:
                    currentAttackRange = (enemyData as RangerEnemyTankSO)?.attackRange ?? 0f;
                    break;
                case EnemyType.RailGun:
                    currentAttackRange = (enemyData as RailGunEnemySO)?.attackRange ?? 0f;
                    break;
                case EnemyType.Boss:
                    currentAttackRange = (enemyData as BossSO)?.attackRange ?? 0f;
                    break;

                case EnemyType.Commander:
                case EnemyType.Parasite:
                    currentAttackRange = Mathf.Infinity; 
                    break;
            }

            // 2. 실제 거리 계산
            float distanceToTarget = Vector2.Distance(transform.position, target.position);

            // 3. 사거리(currentAttackRange) 밖에 있다면: "이동"
            if (distanceToTarget > currentAttackRange)
            {
                attackTimer = 0f; 
                
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    target.position,
                    enemyData.enemySpeed * Time.deltaTime
                );
            }
            // 4. 사거리(currentAttackRange) 안에 있다면: "공격" (및 정지)
            else
            {
                if (attackTimer <= 0f)
                {
                    enemyData.PerformAttack(this); 
                    attackTimer = attackCooldown;

                    if (!hasLoggedFirstAttack)
                    {
                        hasLoggedFirstAttack = true;
                        GameAnalyticsLogger.instance.LogEnemyStartAttack(enemyNum);
                    }
                }
                else
                {
                    attackTimer -= Time.deltaTime;
                }
            }
        }
        // --- [수정 끝] ---
        else
        {   // isAttacking이 false이거나, 근접 공격 타입(Rammer, Kamikaze)의 이동 로직
            transform.position = Vector2.MoveTowards(
                transform.position,
                target.position,
                enemyData.enemySpeed * Time.deltaTime
            );
        }
        
        // 회전은 항상 마지막에
        transform.rotation = Quaternion.LookRotation(Vector3.forward, target.position - transform.position);
    }
    
    public void ApplyKnockback(Vector2 direction, float speed)
    {
        // 이미 죽었거나, Rammer가 후퇴 중이거나, 이미 넉백 중이면 중첩하지 않음
        if (isDead || isBouncingBack || isKnockedBack)
        {
            return;
        }

        // 넉백 상태로 전환
        isKnockedBack = true;
        
        // [수정] 넉백 시간 + 1.5초(정지 시간)를 더한 값으로 타이머 설정
        knockbackTimer = knockbackDuration + 1.5f; 
        
        knockbackDirection = direction.normalized;
        knockbackSpeed = speed; // 총알이 전달해준 속도
    }

    
    public void SetPool(IObjectPool<GameObject> pool)
    {
        myPool = pool;
    }
    
    public void ResetAttackCooldownFromSO()
    {
        if (enemyData != null)
        {
            if (enemyData.enemyType == EnemyType.Ranger)
            {
                var ranger = enemyData as RangerEnemySO;
                if (ranger != null)
                {
                    attackCooldown = ranger.attackCooldown;
                }
            }
            else if (enemyData.enemyType == EnemyType.RangerTank)
            {
                var rangerTank = enemyData as RangerEnemyTankSO;
                if (rangerTank != null)
                {
                    attackCooldown = rangerTank.attackCooldown;
                }
            }
            else if (enemyData.enemyType == EnemyType.Parasite)
            {
                var parasite = enemyData as ParasiteSO;
                if (parasite != null)
                {
                    attackCooldown = parasite.attackCooldown;
                }
            }
            else if (enemyData.enemyType == EnemyType.RailGun)
            {
                var railgun = enemyData as RailGunEnemySO;
                if (railgun != null)
                {
                    attackCooldown = railgun.attackCooldown;
                }
            }
            else if (enemyData.enemyType == EnemyType.Rammer)
            {
                var rammer = enemyData as RammerEnemySO;
                if (rammer != null)
                {
                    attackCooldown = rammer.attackCooldown;
                }
            }
            else if (enemyData.enemyType == EnemyType.Commander)
            {
                var commander = enemyData as CommanderSO;
                if (commander != null)
                {
                    attackCooldown = commander.attackCooldown;
                }
            }
            // 보스 쿨다운 로직 복원
            else if (enemyData.enemyType == EnemyType.Boss)
            {
                var boss = enemyData as BossSO;
                if (boss != null)
                {
                    attackCooldown = boss.attackCooldown;
                }
            }
        }
    }
    
    // 풀에서 재사용 시 상태 초기화
    public void ResetState()
    {
        enemyType = enemyData.enemyType;
        enemyHP = enemyData.enemyHP;
        enemySpeed = enemyData.enemySpeed;
        isAttacking = false;
        attackTimer = 0f;
        isDead = false;
        isBouncingBack = false;
        hasLoggedFirstAttack = false;
        
        
        // [추가] 넉백 상태도 초기화
        isKnockedBack = false;
        knockbackTimer = 0f;shieldHP = 0; // 쉴드 초기화

        
        shieldHP = 0; // 쉴드 초기화
        
        enemyNum = Planet1WaveManager.Instance.enemyNum; 
        
        if (hitFlashEffect != null)
        {
            hitFlashEffect.ResetColor();
        }
        
        ResetAttackCooldownFromSO();

        if (EnemyBuffManager.Instance != null)
        {
            EnemyBuffManager.Instance.RegisterEnemy(this);
        }
        else
        {
            Debug.LogError($"Enemy {enemyNum} ({enemyType}) 등록 실패! EnemyBuffManager.Instance가 NULL입니다!");
        }
    }

    /// <summary>
    /// 외부(EnemyBuffManager 등)에서 호출하여 적의 색상을 원래대로 되돌립니다.
    /// </summary>
    public void ResetSpriteColor()
    {
        if (hitFlashEffect != null)
            hitFlashEffect.ResetColor();
    }
    public void TakeDamage(int damage, string weaponType)
    {
        if (isDead) return; 
        
        if (shieldHP > 0)
        {
            shieldHP -= damage;
            Debug.Log($"적 {enemyNum} 쉴드 피격! 남은 쉴드: {shieldHP}");
            if (shieldHP <= 0)
            {
                Debug.Log($"적 {enemyNum} 쉴드 파괴!");
            }
            return; 
        }

        if (enemyData != null && enemyData.enemyType == EnemyType.Parasite)
        {
            return;
        }

        if (enemyData.enemyType == EnemyType.Boss)
        {
            Debug.Log("Boss took damage: " + damage);
            Planet1WaveManager.Instance.bossHpSlider.value  -= (float)damage / enemyData.enemyHP;
        }
        if (enemyData.enemyType == EnemyType.MainBoss)
        {
            Planet1WaveManager.Instance.mainBossHpSlider.value  -= (float)damage / enemyData.enemyHP;
        }

        enemyHP -= damage;
        
        if (hitFlashEffect != null)
        {
            hitFlashEffect.Flash();
        }
        
        if (enemyHP <= 0)
        {
            if (bossCorePrefab != null && enemyData.enemyType == EnemyType.Boss)
            {
                Instantiate(bossCorePrefab, gameObject.transform.position, Quaternion.identity);
            }
            
            GameAnalyticsLogger.instance.LogEnemyKilled(enemyType.ToString(), weaponType);
            
            if (EnemyBuffManager.Instance != null)
            {
                EnemyBuffManager.Instance.UnregisterEnemy(this);
            }

            if (enemyData.enemyType == EnemyType.Commander)
            {
                if (EnemyBuffManager.Instance != null)
                {
                    EnemyBuffManager.Instance.DeactivateCommanderBuffs();
                }
            }
            
            isDead = true;
            myPool.Release(gameObject);

            if (enemyData.enemyType == EnemyType.MainBoss)
            {
                // 게임 승리 처리
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (enemyData == null) return;
    
        switch (enemyData.enemyType)
        {
            case EnemyType.Parasite:
                if (collision.CompareTag("Respawn"))
                {
                    isAttacking = true;
                }
                else if (collision.CompareTag("Weapon"))
                {
                    isDead = true;
                    myPool.Release(gameObject); 
                }
                break;

            case EnemyType.Ranger:
                if (collision.CompareTag("AttackArea1"))
                {
                    isAttacking = true;
                }
                break;

            case EnemyType.RangerTank:
                if (collision.CompareTag("AttackArea2"))
                {
                    isAttacking = true;
                }
                break;
            
            case EnemyType.RailGun:
                if (collision.CompareTag("AttackArea3"))
                {
                    isAttacking = true;
                }
                break;
            
            case EnemyType.Boss: // 보스 공격 트리거 복원
                if (collision.CompareTag("AttackArea3"))
                {
                    isAttacking = true;
                }
                break;
            case EnemyType.Commander:
                if (collision.CompareTag("AttackArea1") || collision.CompareTag("AttackArea2") || collision.CompareTag("AttackArea3"))
                {
                    isAttacking = true; 

                    if (EnemyBuffManager.Instance != null)
                    {
                        EnemyBuffManager.Instance.ActivateCommanderBuffs();
                    }
                }
                break;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (enemyData == null) return;
    
        switch (enemyData.enemyType)
        {
            case EnemyType.Parasite when collision.CompareTag("Respawn"):
                isAttacking = false;
                break;
            case EnemyType.Ranger when collision.CompareTag("AttackArea1"):
                isAttacking = false;
                break;
            case EnemyType.RangerTank when collision.CompareTag("AttackArea2"):
                isAttacking = false;
                break;
            case EnemyType.RailGun when collision.CompareTag("AttackArea3"):
                isAttacking = false;
                break;
            case EnemyType.Boss when collision.CompareTag("AttackArea3"):
                isAttacking = false;
                break;
            case EnemyType.Commander when collision.CompareTag("AttackArea1") || collision.CompareTag("AttackArea2") || collision.CompareTag("AttackArea3"):
                isAttacking = false;
                break;
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleKamikazeCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleKamikazeCollision(collision);
    }

    private void HandleKamikazeCollision(Collision2D collision)
    {
        if (!gameObject.activeInHierarchy) return;
        
        // Rammer 타입 쉴드/코어 충돌 처리
        if (enemyData != null && enemyData.enemyType == EnemyType.Rammer && !isBouncingBack)
        {
            var rammerSO = enemyData as RammerEnemySO;
            if (rammerSO == null) return; 

            bool didHitTarget = false; 

            Tilemap tilemap = collision.collider.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                Planet manager = tilemap.GetComponent<Planet>();
                if (manager != null)
                {
                    Vector3 hitPoint = collision.GetContact(0).point;
                    Vector3 correctedHitPoint = hitPoint - ((Vector3)collision.GetContact(0).normal * 0.01f);
                    Vector3Int centerCellPos = tilemap.WorldToCell(correctedHitPoint);

                    for (int xOffset = -2; xOffset <= 2; xOffset++)
                    {
                        for (int yOffset = -2; yOffset <= 2; yOffset++)
                        {
                            Vector3Int neighborCellPos = new Vector3Int(
                                centerCellPos.x + xOffset,
                                centerCellPos.y + yOffset,
                                centerCellPos.z
                            );
                            manager.DamageTile(neighborCellPos, rammerSO.shieldDamage);
                        }
                    }
                    didHitTarget = true; 
                }
            }
            else if (collision.collider.CompareTag("Core"))
            {
                Core core = collision.collider.GetComponent<Core>();
                if (core != null)
                {
                    core.TakeDamage(rammerSO.shieldDamage); 
                    didHitTarget = true;
                }
            }

            if (didHitTarget)
            {
                StartCoroutine(BounceBackCoroutine(rammerSO));
            }
        }

        // Kamikaze 타입 폭발 처리
        if (enemyData != null && enemyData.enemyType == EnemyType.Kamikaze)
        {
            (enemyData as KamikazeSO).Explode(this, collision);
        }
        else if (enemyData != null && enemyData.enemyType == EnemyType.KamikazeTank)
        {
            (enemyData as KamikazeTankSO).Explode(this, collision);
        } 
        // [수정] MainBoss를 Kamikaze 그룹에 추가합니다.
        else if(enemyData != null && enemyData.enemyType == EnemyType.MainBoss)
        {
            // MainBossSO에 KamikazeSO와 동일한 Explode(Enemy, Collision2D) 메서드가 필요합니다.
            (enemyData as MainBossSO).Explode(this, collision);
        }
    }
    
    private IEnumerator BounceBackCoroutine(RammerEnemySO so)
    {
        isBouncingBack = true; 

        Vector2 knockbackDirection = (transform.position - target.position).normalized; 
        Vector3 startPos = transform.position;
        Vector3 endPos = transform.position + (Vector3)knockbackDirection * so.knockbackDistance;

        float timer = 0f;
        while (timer < so.knockbackDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / so.knockbackDuration);
            float easeT = Mathf.Sin(t * Mathf.PI * 0.5f); 
            
            transform.position = Vector3.Lerp(startPos, endPos, easeT);
            yield return null;
        }

        yield return new WaitForSeconds(this.attackCooldown);

        isBouncingBack = false; 
    }

    private void RespawnAtRandomPosition()
    {
        /* ... (리스폰 로직) ... */
    }
    private void OnEnable()
    {
        Core.OnCoreDied += HandleCoreDied;
    }

    private void OnDisable()
    {
        Core.OnCoreDied -= HandleCoreDied;
        
        if (activeSpecialEffect != null)
        {
            Destroy(activeSpecialEffect);
            activeSpecialEffect = null;
        }
        
        if (enemyData != null)
        {
            enemyData.OnEnemyDisabled(this);
        }
    }

    private void HandleCoreDied(int deadCoreNumber)
    {
        if (deadCoreNumber == 2)
        {
            TrySwitchTargetToCore1();
        }
    }

    private void TrySwitchTargetToCore1()
    {
        Debug.Log("코어깨져서 재이동");
        var cores = FindObjectsByType<Core>(FindObjectsSortMode.None);
        Core core1 = null;
        foreach (var c in cores)
        {
            if (c.coreNumber == 1 && !c.IsDead)
            {
                core1 = c;
                break;
            }
        }

        if (core1 != null)
        {
            // 공격 상태 완전 초기화
            isAttacking = false;
            attackTimer = 0f;
            StopAllCoroutines(); // 혹시 공격 코루틴 중일 수도 있음

            // 타겟 교체
            SetTaget(core1.transform);

            // 즉시 이동 로직 재개
            transform.position = Vector2.MoveTowards(
                transform.position,
                core1.transform.position,
                enemyData.enemySpeed * Time.deltaTime
            );
        }
    }
}