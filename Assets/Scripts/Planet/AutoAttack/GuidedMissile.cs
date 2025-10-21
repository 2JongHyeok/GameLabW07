using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount, Transform source = null);
}

public class GuidedMissile : Projectile
{
    [Header("이동/유도")]
    public float initialSpeed = 2f;
    public float maxSpeed = 15f;
    public float acceleration = 5f;
    [Tooltip("초당 회전 속도(도/초)")]
    public float turnRateDegPerSec = 360f;

    [Header("타겟 탐색")]
    [Tooltip("타겟 스캔 반경")]
    public float searchRadius = 30f;
    [Tooltip("타겟 재탐색 주기(초)")]
    public float retargetInterval = 0.25f;

    [Header("타겟 상실 시 처리")]
    public bool keepStraightWhenLost = true;
    public float selfDestructAfterLost = 1.5f;

    [Header("충돌/피해")]
    [Tooltip("레이어 필터(비워두면 전부 허용)")]
    public LayerMask damageLayers;
    [Tooltip("태그 필터(비워두면 무시)")]
    public string targetTag = "Enemy";
    [SerializeField] private ExplosionPulse2D explosionPrefab;
    private readonly List<Collider2D> scanResults = new List<Collider2D>(64);
    private ContactFilter2D contactFilter;
    private Transform target;
    private Rigidbody2D rb;
    private bool spawnExplosionOnDestroy;

    private float currentSpeed;
    private float lostTimer;
    private float retargetTimer;

    // --- 히트 처리용 ---
    private bool hasHit;
    private float cachedDamage;

    // 스캔 버퍼(GC 방지용). 동시 다발 사용에 충분한 크기로 조정.
    private static readonly Collider2D[] scanBuf = new Collider2D[64];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = initialSpeed;
        lostTimer = 0f;
        hasHit = false;
        spawnExplosionOnDestroy = true;
        retargetTimer = 0f;
        Vector3 currentPosition = transform.position;

        // 2. 현재 위치의 X와 Y 값은 그대로 두고, Z 값만 -3으로 변경합니다.
        currentPosition.z = -3f;

        // 3. 변경된 위치를 오브젝트에 다시 적용합니다.
        transform.position = currentPosition;
        // 수명 타이머: 10초 뒤 파괴 → OnDestroy에서 폭발 이펙트 재생
        Destroy(gameObject, 10f);
    }

    // 기존 API 유지: 외부에서 주는 타겟은 '초기 잠금' 정도로만 사용
    public void SetTarget(Transform newTarget, AutoTurret _)
    {
        target = newTarget;
        currentSpeed = initialSpeed;
        lostTimer = 0f;
    }

    // Projectile.Init(baseDamage, shooterTransform) 가정
    public new void Init(float baseDamage, Transform shooter)
    {
        base.Init(baseDamage, shooter);
        cachedDamage = baseDamage;
        this.shooter = shooter;

        // 스폰 순간에도 한 번 스캔해서 가까운 타겟을 바로 잡는다
        AcquireNearestTarget();
    }

    private void FixedUpdate()
    {
        if (hasHit) return;

        // 주기적 재탐색(타겟이 죽었거나 멀어졌을 때 갱신)
        retargetTimer -= Time.fixedDeltaTime;
        if (target == null || retargetTimer <= 0f)
        {
            // 현재 타겟이 너무 멀어졌거나 사라졌으면 재탐색
            AcquireNearestTarget();
            retargetTimer = retargetInterval;
        }

        if (target == null)
        {
            HandleNoTarget(Time.fixedDeltaTime);
            return;
        }

        // 가속
        currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.fixedDeltaTime, maxSpeed);

        // 회전(Z축, up 방향이 진행방향)
        Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float desiredZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float nextZ = Mathf.MoveTowardsAngle(rb.rotation, desiredZ, turnRateDegPerSec * Time.fixedDeltaTime);
        rb.MoveRotation(nextZ);

        // 전진
        rb.linearVelocity = (Vector2)transform.up * currentSpeed;
    }

    private void HandleNoTarget(float dt)
    {
        if (keepStraightWhenLost)
        {
            currentSpeed = Mathf.Min(currentSpeed + acceleration * dt, maxSpeed);
            rb.linearVelocity = (Vector2)transform.up * currentSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (selfDestructAfterLost > 0f)
        {
            lostTimer += dt;
            if (lostTimer >= selfDestructAfterLost)
                Destroy(gameObject);
        }
    }

    /// <summary>
    /// 미사일 ‘자신’ 기준으로 가장 가까운 적을 스캔하여 target 잠금.
    /// damageLayers/targetTag/아군 무시 필터 적용.
    /// </summary>
    private void AcquireNearestTarget()
    {
        // 1) 오버랩 결과 수집 (할당 없음: 재사용 List)
        scanResults.Clear();
        int count = Physics2D.OverlapCircle(
            (Vector2)transform.position,
            searchRadius,
            contactFilter,
            scanResults
        );

        // 2) 최단거리 타깃 선택
        Transform best = null;
        float bestSqr = float.PositiveInfinity;
        Vector2 selfPos = transform.position;

        for (int i = 0; i < count; i++)
        {
            var col = scanResults[i];
            if (!col) continue;

            // (1) 발사자/아군 무시
            if (shooter && col.transform.IsChildOf(shooter))
                continue;

            // (2) 태그 필터
            if (!string.IsNullOrEmpty(targetTag) && !col.CompareTag(targetTag))
                continue;

            // (3) Enemy 컴포넌트(자식 콜라이더 대비 부모까지)
            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null) continue;
            // 필요하면 enemy 생존 여부 체크
            // if (!enemy.IsAlive) continue;

            float sqr = ((Vector2)enemy.transform.position - selfPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = enemy.transform;
            }
        }

        target = best;
        if (target == null)
        {
            // 못 찾으면 로스트 로직 유지
            // (즉시 파괴하려면 여기서 Destroy(gameObject); 가능)
        }
        else
        {
            // 새 타깃 확보 시 로스트 초기화
            lostTimer = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // 1) 발사자/아군 무시
        if (shooter != null && other.transform.IsChildOf(shooter))
            return;

        // 2) 레이어 필터
        if (damageLayers.value != 0)
        {
            if ((damageLayers.value & (1 << other.gameObject.layer)) == 0)
                return;
        }

        // 3) 태그 필터
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag))
            return;

        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            int dmg = Mathf.RoundToInt(cachedDamage);
            enemy.TakeDamage(dmg);

            SpawnExplosion(transform.position);
            spawnExplosionOnDestroy = false;
            hasHit = true;
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (spawnExplosionOnDestroy)
            SpawnExplosion(transform.position);
    }

    private void SpawnExplosion(Vector3 pos)
    {
        if (explosionPrefab != null)
        {
            var fx = Instantiate(explosionPrefab, pos, Quaternion.identity);
            fx.Play(pos);
        }
    }

#if UNITY_EDITOR
    // 에디터에서 탐색 반경 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 1, 1, 0.25f);
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
#endif
}
