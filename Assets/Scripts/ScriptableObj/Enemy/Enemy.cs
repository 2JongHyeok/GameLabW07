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

    private void Update()
    {
        // [Rammer 로직] 후퇴 중일 땐 모든 이동/공격 로직 중지
        if (isBouncingBack)
        {
            return;
        }

        // Ranger 또는 RangerTank 타입이고 공격 중일 때
        if (isAttacking && (enemyData.enemyType == EnemyType.Ranger ||
                            enemyData.enemyType == EnemyType.RangerTank ||
                            enemyData.enemyType == EnemyType.Parasite ||
                            enemyData.enemyType == EnemyType.RailGun ||
                            enemyData.enemyType == EnemyType.Commander))

{
            if (attackTimer <= 0f)
            {
                enemyData.PerformAttack(this);
                attackTimer = attackCooldown;

                if (!hasLoggedFirstAttack)
                {
                    hasLoggedFirstAttack = true;

                    // [복원] 기존 코드
                    GameAnalyticsLogger.instance.LogEnemyStartAttack(
                        enemyNum
                    );
                }
            }
            else
            {
                attackTimer -= Time.deltaTime;
            }
        }
        else
        {
            // 단순 이동 (Rammer는 항상 이 로직을 따름)
            transform.position = Vector2.MoveTowards(
                transform.position,
                target.position,
                enemyData.enemySpeed * Time.deltaTime
            );

        }
        transform.rotation = Quaternion.LookRotation(Vector3.forward, target.position - transform.position);

    }
    public void SetPool(IObjectPool<GameObject> pool)
    {
        myPool = pool;
    }
    
    /// <summary>
    /// ScriptableObject에서 원본 공격 쿨다운 값을 다시 읽어옵니다.
    /// (버프 해제 시 사용)
    /// </summary>
    public void ResetAttackCooldownFromSO()
    {
        // ResetState()에 있던 로직과 100% 동일합니다.
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
            // [추가] Commander도 쿨다운을 사용 (멈추기 위해)
            else if (enemyData.enemyType == EnemyType.Commander)
            {
                var commander = enemyData as CommanderSO;
                if (commander != null)
                {
                    attackCooldown = commander.attackCooldown;
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
        
        shieldHP = 0; // 쉴드 초기화
        
        // 기존 코드
        enemyNum = Planet1WaveManager.Instance.enemyNum; 
        
        // 피격 이펙트 초기화
        if (hitFlashEffect != null)
        {
            hitFlashEffect.ResetColor();
        }
        
        // --- [기존 쿨다운 로직을 아래 함수 호출로 변경] ---
        ResetAttackCooldownFromSO();

        // --- [이 코드를 추가하세요] ---
        // 버프 매니저에 자신을 등록합니다.
        if (EnemyBuffManager.Instance != null)
        {
            EnemyBuffManager.Instance.RegisterEnemy(this);
            // Debug.Log($"Enemy {enemyNum} ({enemyType}) 등록 성공. (Manager.Instance 유효)");
        }
        else
        {
            // "일부만" 활성화되는 경우, 콘솔에 이 에러가 반드시 찍힙니다.
            Debug.LogError($"Enemy {enemyNum} ({enemyType}) 등록 실패! EnemyBuffManager.Instance가 NULL입니다!");
        }
    }
    public void TakeDamage(int damage, string weaponType)
    {
        if (isDead) return; // 이미 죽었으면 무시
        
        // --- [쉴드 로직 추가] ---
        // 쉴드가 있다면 쉴드 HP를 먼저 깎고, 본체 데미지는 무시
        if (shieldHP > 0)
        {
            shieldHP -= damage;
            Debug.Log($"적 {enemyNum} 쉴드 피격! 남은 쉴드: {shieldHP}");
            if (shieldHP <= 0)
            {
                // TODO: 쉴드 파괴 이펙트
                Debug.Log($"적 {enemyNum} 쉴드 파괴!");
            }
            return; // 쉴드가 데미지를 흡수
        }

        if (enemyData != null && enemyData.enemyType == EnemyType.Parasite)
        {
            return;
        }

        // [복원] 기존 코드
        if (enemyData.enemyType == EnemyType.Boss)
        {
            Debug.Log("Boss took damage: " + damage);
            // 보스 체력 슬라이더 감소 - 보스 최대 체력 대비 비율로 감소
            Planet1WaveManager.Instance.bossHpSlider.value  -= (float)damage / enemyData.enemyHP;
        }
        // [복원] 기존 코드
        if (enemyData.enemyType == EnemyType.MainBoss)
        {
            // 보스 체력 슬라이더 감소 - 보스 최대 체력 대비 비율로 감소
            Planet1WaveManager.Instance.mainBossHpSlider.value  -= (float)damage / enemyData.enemyHP;
        }

        enemyHP -= damage;
        
        // 피격 이펙트 재생
        if (hitFlashEffect != null)
        {
            hitFlashEffect.Flash();
        }
        
        if (enemyHP <= 0)
        {
            // 보스 처치 시 코어 생성
            if (bossCorePrefab != null && enemyData.enemyType == EnemyType.Boss)
            {
                Instantiate(bossCorePrefab, gameObject.transform.position, Quaternion.identity);
            }
            
            // [복원] 기존 코드
            GameAnalyticsLogger.instance.LogEnemyKilled(enemyType.ToString(), weaponType);
            
            // --- [버프 매니저 로직 추가] ---
            // 1. 버프 매니저에서 이 적을 제거합니다.
            if (EnemyBuffManager.Instance != null)
            {
                EnemyBuffManager.Instance.UnregisterEnemy(this);
            }

            // 2. 만약 죽은 적이 Commander라면, 모든 버프를 끕니다.
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
            // 패러사이트는 "Respawn" 태그에 반응
            case EnemyType.Parasite:
                if (collision.CompareTag("Respawn"))
                {
                    isAttacking = true;
                }
                else if (collision.CompareTag("Weapon"))
                {
                    isDead = true;
                    // (여기에 밟혀 죽는 이펙트/사운드 추가하면 좋음)
                    myPool.Release(gameObject); // 풀로 반환 (죽음)
                }
                break;

            // 레인저/탱크는 "AttackArea1", "AttackArea2" 태그에 반응
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
            case EnemyType.Commander:
                if (collision.CompareTag("AttackArea1") || collision.CompareTag("AttackArea2")) 
                {
                    isAttacking = true; // 멈추기

                    // Commander가 멈추는 이 시점에 모든 적 버프 활성화
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

    // --- [이 함수가 수정되었습니다] ---
    private void HandleKamikazeCollision(Collision2D collision)
    {
        // 이미 비활성화되었으면(풀로 반환되었으면) 무시
        if (!gameObject.activeInHierarchy) return;
        
        // Rammer 타입 쉴드/코어 충돌 처리
        if (enemyData != null && enemyData.enemyType == EnemyType.Rammer && !isBouncingBack)
        {
            var rammerSO = enemyData as RammerEnemySO;
            if (rammerSO == null) return; // RammerSO가 없으면 중지

            bool didHitTarget = false; // 쉴드나 코어에 부딪혔는지 확인

            // 1. 쉴드(Tilemap)에 부딪혔는지 확인
            Tilemap tilemap = collision.collider.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                Planet manager = tilemap.GetComponent<Planet>();
                if (manager != null)
                {
                    // 3x3 광역 데미지 주기
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
                    didHitTarget = true; // 쉴드에 명중
                }
            }
            // 2. 코어(Core)에 부딪혔는지 확인 (쉴드가 아닐 경우)
            else if (collision.collider.CompareTag("Core"))
            {
                Core core = collision.collider.GetComponent<Core>();
                if (core != null)
                {
                    // 코어에는 3x3이 아닌 단일 데미지
                    core.TakeDamage(rammerSO.shieldDamage); 
                    didHitTarget = true; // 코어에 명중
                }
            }

            // 3. 쉴드 또는 코어에 부딪혔다면 후퇴 코루틴 시작
            if (didHitTarget)
            {
                StartCoroutine(BounceBackCoroutine(rammerSO));
            }
        }

        // Kamikaze 타입 폭발 처리 (enemyData로 직접 체크)
        // [복원] 기존 코드
        if (enemyData != null && enemyData.enemyType == EnemyType.Kamikaze)
        {
            (enemyData as KamikazeSO).Explode(this, collision);
        }
        else if (enemyData != null && enemyData.enemyType == EnemyType.KamikazeTank)
        {
            (enemyData as KamikazeTankSO).Explode(this, collision);
        }
        else if (enemyData != null && enemyData.enemyType == EnemyType.Boss)
        {
            (enemyData as BossSO).Explode(this, collision);
        } 
        else if(enemyData != null && enemyData.enemyType == EnemyType.MainBoss)
        {
            (enemyData as MainBossSO).Explode(this, collision);
        } 
    }
    
    // --- [이 함수가 수정되었습니다] ---
    // Rammer가 쉴드에 부딪혔을 때 후퇴하는 코루틴 (부드러운 감속 적용)
    private IEnumerator BounceBackCoroutine(RammerEnemySO so)
    {
        isBouncingBack = true; // 상태를 '후퇴 중'으로 변경

        // --- 1. 후퇴 (Knockback) ---
        Vector2 knockbackDirection = (transform.position - target.position).normalized; // 타겟 반대 방향
        Vector3 startPos = transform.position;
        Vector3 endPos = transform.position + (Vector3)knockbackDirection * so.knockbackDistance;

        float timer = 0f;
        while (timer < so.knockbackDuration)
        {
            timer += Time.deltaTime;
            // t 값을 0~1 사이로 제한
            float t = Mathf.Clamp01(timer / so.knockbackDuration);
            
            // [수정] Ease-Out 효과 적용 (부드럽게 감속)
            // t가 0->1로 갈 때, easeT도 0->1로 가지만 점점 느려짐 (Sin 곡선)
            float easeT = Mathf.Sin(t * Mathf.PI * 0.5f); 
            
            transform.position = Vector3.Lerp(startPos, endPos, easeT);
            yield return null;
        }

        // SO의 원본 값(so.attackCooldown) 대신,
        // 버프 매니저가 수정한 이 인스턴스의 값(this.attackCooldown)을 사용합니다.
        yield return new WaitForSeconds(this.attackCooldown);

        // --- 3. 상태 리셋 ---
        isBouncingBack = false; // 상태를 '돌진 가능'으로 복구
    }

    // 타겟으로부터 너무 멀어졌을 때 랜덤 스폰 포인트로 리스폰
    private void RespawnAtRandomPosition()
    {
        /*if (target == null) return;

        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        Camera mainCamera = Camera.main;
        float aspect = mainCamera ? mainCamera.aspect : 16f / 9f;
        float size = mainCamera ? mainCamera.orthographicSize : 20f;

        float horizontalSize = size * aspect;
        float spawnRadius = Mathf.Max(size, horizontalSize) + respawnDistanceOffset;

        float x = Mathf.Cos(randomAngle) * spawnRadius;
        float y = Mathf.Sin(randomAngle) * spawnRadius;

        transform.position = target.position + new Vector3(x, y, 0f);*/
    }
    private void OnEnable()
    {
        Core.OnCoreDied += HandleCoreDied;
    }

    private void OnDisable()
    {
        Core.OnCoreDied -= HandleCoreDied;
    }

    private void HandleCoreDied(int deadCoreNumber)
    {
        // 코어2가 죽었을 때만 코어1로 갈아탄다
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
            // 사격/고정 상태 해제 후 이동 상태로 전환
            isAttacking = false;   // AttackArea1 트리거에 다시 들어갈 때까지 이동하게 함
            attackTimer = 0f;

            // 네이밍 유지: SetTaget(철자 주의) 사용 가능, 또는 target 직접 대입
            SetTaget(core1.transform); // 혹은: target = core1.transform;
        }
        else
        {
            // 코어1도 없으면(이미 파괴 등) 아무것도 하지 않음. 필요시 디스폰/후퇴 로직 추가 가능
        }
    }
}