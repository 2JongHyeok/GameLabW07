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

        // --- [수정 시작] 'if' 문에 Boss와 MainBoss를 다시 추가합니다. ---
        if (isAttacking && (enemyData.enemyType == EnemyType.Ranger ||
                            enemyData.enemyType == EnemyType.RangerTank ||
                            enemyData.enemyType == EnemyType.Parasite ||
                            enemyData.enemyType == EnemyType.RailGun ||
                            enemyData.enemyType == EnemyType.Commander ||
                            enemyData.enemyType == EnemyType.Boss ||      // [복원]
                            enemyData.enemyType == EnemyType.MainBoss))   // [복원]
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
                
                // [복원] 보스 사거리 체크 로직
                case EnemyType.Boss:
                    currentAttackRange = (enemyData as BossSO)?.attackRange ?? 0f;
                    break;
                case EnemyType.MainBoss:
                    currentAttackRange = (enemyData as MainBossSO)?.attackRange ?? 0f;
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
        {
            // [수정]
            // 1. 'isAttacking=false'인 적 (Ranger 등)이 이 로직을 따릅니다.
            // 2. 'isAttacking'을 사용하지 않는 적 (Rammer, Kamikaze)이 이 로직을 따릅니다.
            // 3. (이제 Boss는 위 'if'문에 포함되므로 이 로직을 따르지 않습니다.)
            transform.position = Vector2.MoveTowards(
                transform.position,
                target.position,
                enemyData.enemySpeed * Time.deltaTime
            );
        }
        
        // 회전은 항상 마지막에
        transform.rotation = Quaternion.LookRotation(Vector3.forward, target.position - transform.position);
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
            
            // --- [수정 시작] 보스의 쿨다운 로직 (원거리 공격용)을 복원합니다. ---
            else if (enemyData.enemyType == EnemyType.Boss)
            {
                var boss = enemyData as BossSO;
                if (boss != null)
                {
                    attackCooldown = boss.attackCooldown;
                }
            }
            else if (enemyData.enemyType == EnemyType.MainBoss)
            {
                var mainBoss = enemyData as MainBossSO;
                if (mainBoss != null)
                {
                    attackCooldown = mainBoss.attackCooldown;
                }
            }
            // --- [수정 끝] ---
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
                if (collision.CompareTag("AttackArea2"))
                {
                    isAttacking = true;
                }
                break;
            
            // --- [수정 시작] 보스가 AttackArea2에서 멈추도록 로직을 복원합니다. ---
            case EnemyType.Boss:
                if (collision.CompareTag("AttackArea2"))
                {
                    isAttacking = true;
                }
                break;
            case EnemyType.MainBoss:
                if (collision.CompareTag("AttackArea2"))
                {
                    isAttacking = true;
                }
                break;
            // --- [수정 끝] ---

            case EnemyType.Commander:
                if (collision.CompareTag("AttackArea1") || collision.CompareTag("AttackArea2")) 
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
            case EnemyType.RailGun when collision.CompareTag("AttackArea2"):
                isAttacking = false;
                break;

            // --- [수정 시작] 보스가 AttackArea2를 나갈 때 다시 이동하도록 로직을 복원합니다. ---
            case EnemyType.Boss when collision.CompareTag("AttackArea2"):
                isAttacking = false;
                break;
            case EnemyType.MainBoss when collision.CompareTag("AttackArea2"):
                isAttacking = false;
                break;
            // --- [수정 끝] ---

            case EnemyType.Commander when collision.CompareTag("AttackArea1") || collision.CompareTag("AttackArea2"):
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
        
        // --- [수정 시작] Boss와 MainBoss가 충돌 시 Explode 하던 로직을 제거합니다. ---
        // (Ranger처럼 원거리 공격을 하므로, 충돌 시 폭발하면 안 됩니다)
        /*
        else if (enemyData != null && enemyData.enemyType == EnemyType.Boss)
        {
            (enemyData as BossSO).Explode(this, collision);
        } 
        else if(enemyData != null && enemyData.enemyType == EnemyType.MainBoss)
        {
            (enemyData as MainBossSO).Explode(this, collision);
        }
        */
        // --- [수정 끝] ---
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