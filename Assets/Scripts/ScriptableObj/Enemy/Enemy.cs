using UnityEngine;
using UnityEngine.Pool;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Data")]
    public EnemyBaseSO enemyData;
    
    [Header("References")]
    public Transform firePoint;
    
    [Header("Respawn Settings")]
    [SerializeField] private float maxDistanceFromTarget = 30f; // 타겟으로부터 최대 거리
    
    // 런타임 상태 (외부에서 접근 필요)
    [HideInInspector] public Transform target;
    [HideInInspector] public IObjectPool<GameObject> myPool;
    [HideInInspector] public int enemyHP;
    [HideInInspector] public bool isDead = false;
    [HideInInspector] public bool isAttacking = false; // WaveManager에서 초기화
    [HideInInspector] public float attackTimer = 0f; // WaveManager에서 초기화
    public int enemyNum = 0;
    private bool hasLoggedFirstAttack = false; // 첫 공격 로그 여부

    // 내부 상태
    private EnemyType enemyType;
    private float enemySpeed;
    private float attackCooldown;
    
    // 피격 이펙트
    private HitFlashEffect hitFlashEffect;
    
    // 보스 처치 시 생성될 코어
    [SerializeField] private GameObject bossCorePrefab;
    
    private void Start()
    {
        if (target != null)
        {
            target.position = Vector2.zero;
        }
        
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
        // 타겟과의 거리 체크 - 너무 멀어지면 리스폰
        if (target != null)
        {
            float distanceToTarget = Vector2.Distance(transform.position, target.position);
            if (distanceToTarget > maxDistanceFromTarget)
            {
                RespawnAtRandomPosition();
                return;
            }
        }
        
        // Ranger 또는 RangerTank 타입이고 공격 중일 때
        if (isAttacking && (enemyData.enemyType == EnemyType.Ranger ||
                            enemyData.enemyType == EnemyType.RangerTank ||
                            enemyData.enemyType == EnemyType.Parasite) ||
                            enemyData.enemyType == EnemyType.Boss)
        {
            if (attackTimer <= 0f)
            {
                enemyData.PerformAttack(this);
                attackTimer = attackCooldown;

                if (!hasLoggedFirstAttack)
                {
                    hasLoggedFirstAttack = true;

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
            // 단순 이동
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
    
    // 풀에서 재사용 시 상태 초기화
    public void ResetState()
    {
        enemyType = enemyData.enemyType;
        enemyHP = enemyData.enemyHP;
        enemySpeed = enemyData.enemySpeed;
        isAttacking = false;
        attackTimer = 0f;
        isDead = false;
        hasLoggedFirstAttack = false;
        enemyNum = WaveManager.Instance.enemyNum;
        // 피격 이펙트 초기화
        if (hitFlashEffect != null)
        {
            hitFlashEffect.ResetColor();
        }
        
        // Ranger 및 RangerTank 타입은 attackCooldown 설정
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
        }
    }
    public void TakeDamage(int damage, string weaponType)
    {
        if (isDead) return; // 이미 죽었으면 무시

        if (enemyData != null && enemyData.enemyType == EnemyType.Parasite)
        {
            return;
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
            GameAnalyticsLogger.instance.LogEnemyKilled(enemyType.ToString(), weaponType);
            isDead = true;
            myPool.Release(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (enemyData == null) return;

        // 패러사이트는 "Respawn" 태그에 반응
        if (enemyData.enemyType == EnemyType.Parasite)
        {
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
        }
        // 레인저/탱크는 기존대로 "Player" 태그에 반응
        else if (enemyData.enemyType == EnemyType.Ranger || enemyData.enemyType == EnemyType.RangerTank || enemyData.enemyType == EnemyType.Boss)
        {
            if (collision.CompareTag("Player"))
            {
                isAttacking = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (enemyData == null) return;

        // 패러사이트는 "Respawn" 태그에서 벗어났을 때
        if (enemyData.enemyType == EnemyType.Parasite)
        {
            if (collision.CompareTag("Respawn"))
            {
                isAttacking = false;
            }
        }
        // 레인저/탱크는 "Player" 태그에서 벗어났을 때
        else if (enemyData.enemyType == EnemyType.Ranger || enemyData.enemyType == EnemyType.RangerTank)
        {
            if (collision.CompareTag("Player"))
            {
                isAttacking = false;
            }
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
        // 이미 비활성화되었으면(풀로 반환되었으면) 무시
        if (!gameObject.activeInHierarchy) return;

        // Kamikaze 타입 폭발 처리 (enemyData로 직접 체크)
        if (enemyData != null && enemyData.enemyType == EnemyType.Kamikaze)
        {
            (enemyData as KamikazeSO).Explode(this, collision);
        }
        else if (enemyData != null && enemyData.enemyType == EnemyType.KamikazeTank)
        {
            (enemyData as KamikazeTankSO).Explode(this, collision);
        }
    }

    // 타겟으로부터 너무 멀어졌을 때 랜덤 스폰 포인트로 리스폰
    private void RespawnAtRandomPosition()
    {
        if (WaveManager.Instance == null) return;
        
        // WaveManager의 GetRandomSpawnPosition 메서드를 public으로 만들어야 함
        // 또는 여기서 직접 계산
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        Camera mainCamera = Camera.main;
        float aspect = mainCamera != null ? mainCamera.aspect : 16f / 9f;
        float maxCameraSize = WaveManager.Instance.maxCameraSize;
        float spawnDistanceOffset = WaveManager.Instance.spawnDistanceOffset;
        
        float horizontalSize = maxCameraSize * aspect;
        float spawnRadius = Mathf.Max(maxCameraSize, horizontalSize) + spawnDistanceOffset;
        
        float x = Mathf.Cos(randomAngle) * spawnRadius;
        float y = Mathf.Sin(randomAngle) * spawnRadius;
        
        transform.position = new Vector3(x, y, 0f);
    }
    
    // EnemyCount는 WaveManager의 풀 시스템에서 관리
    // void OnEnable()
    // {
    //     WaveManager.Instance.EnemyCount++;
    // }
    // void OnDestroy()
    // {
    //     WaveManager.Instance.EnemyCount--;
    // }
    // public void OnDrawGizmos()
    // {
    //     if (enemyType == EnemyType.Ranger)
    //     {
    //         Gizmos.color = Color.red;
    //         Gizmos.DrawWireSphere(transform.position, (enemyData as RangerEnemySO).attackRange);
    //     }
    // }
}
