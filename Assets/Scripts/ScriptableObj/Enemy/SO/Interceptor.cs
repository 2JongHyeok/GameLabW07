using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 보스가 발사하는 인터셉터의 움직임과 충돌을 처리하는 스크립트입니다.
/// 1. 지정된 시간 동안 360도 무작위 방향으로 발사됩니다.
/// 2. 이후 목표물을 향해 방향을 틀어 유도 비행을 시작합니다.
/// </summary>
public class Interceptor : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 8f;
    [Tooltip("초기 무작위 방향으로 비행할 시간(초)")]
    public float initialLaunchDuration = 0.5f;
    [Tooltip("목표를 향해 방향을 전환하는 속도")]
    public float rotationSpeed = 5f;
    [Tooltip("인터셉터의 최대 생존 시간(초). 이 시간이 지나면 목표물과 상관없이 파괴됩니다.")]
    public float lifetime = 15f;

    [Header("Damage Settings")]
    public int damage = 2;
    [Tooltip("충돌 시 생성할 폭발 이펙트 프리팹")]
    public GameObject explosionEffectPrefab;
    [Tooltip("폭발의 크기")]
    public float effectExplosionRadius = 1.5f;
    [Tooltip("코어 주변에 생성될 중간 목표 지점의 반경")]
    public float waypointRadius = 5f;

    // --- 내부 변수 ---
    private enum MovementState { InitialLaunch, ApproachingWaypoint, HomingToCore }
    private MovementState currentState;

    private Transform target;
    private Vector2 initialDirection;
    private float launchTimer;
    
    // --- [수정] 중간 목표 지점(Waypoint) 변수 추가 ---
    private Vector3 waypoint; 
    private const float WAYPOINT_REACH_THRESHOLD = 0.5f; // 중간 지점에 '도착'했다고 판단할 거리


    private void Awake()
    {
        // 이 오브젝트의 레이어를 "EnemyBullet"으로 설정합니다.
        gameObject.layer = LayerMask.NameToLayer("EnemyBullet");
    }

    /// <summary>
    /// 외부(BossSO)에서 호출하여 목표물을 설정합니다.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        // 1. 초기 발사 방향 설정 (360도 무작위)
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        initialDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
        launchTimer = initialLaunchDuration;

        // 초기 방향으로 기체 회전
        transform.rotation = Quaternion.LookRotation(Vector3.forward, initialDirection);

        // 2. [수정] 코어 주변의 무작위 중간 목표 지점(Waypoint) 설정
        waypoint = target.position + (Vector3)(Random.insideUnitCircle.normalized * waypointRadius);

        currentState = MovementState.InitialLaunch;
    }

    void Update()
    {
        // 1. 라이프타임 감소 및 체크
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject); // 시간이 다 되면 스스로 파괴
            return;
        }

        // 목표가 사라지면 현재 방향으로 계속 직진만 합니다.
        if (target == null)
        {
            transform.position += transform.up * speed * Time.deltaTime;
            return;
        }

        switch (currentState)
        {
            case MovementState.InitialLaunch:
                // 1단계: 초기 발사 - 설정된 시간 동안 무작위 방향으로 직진
                transform.position += (Vector3)initialDirection * speed * Time.deltaTime;
                launchTimer -= Time.deltaTime;
                if (launchTimer <= 0)
                {
                    currentState = MovementState.ApproachingWaypoint;
                }
                break;

            case MovementState.ApproachingWaypoint:
                // 2단계: 중간 지점으로 비행
                MoveTowards(waypoint);
                // 중간 지점에 충분히 가까워지면 최종 단계로 전환
                if (Vector2.Distance(transform.position, waypoint) < WAYPOINT_REACH_THRESHOLD)
                {
                    currentState = MovementState.HomingToCore;
                }
                break;

            case MovementState.HomingToCore:
                // 3단계: 실제 목표(코어)를 향해 최종 돌진
                MoveTowards(target.position);
                break;
        }
    }

    private void MoveTowards(Vector3 destination)
    {
        Vector2 direction = (destination - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        transform.position += transform.up * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 행성 타일 또는 코어와 충돌 시 데미지를 주고 파괴됩니다.
        if (collision.CompareTag("Core"))
        {
            // 1. 코어와 충돌 시: Core 컴포넌트의 TakeDamage 호출
            HandleImpactAndDestroy(collision);
        }
        else if (collision.CompareTag("Ground"))
        {
            // 2. 행성 타일과 충돌 시: Planet 매니저의 DamageTile 호출
            HandleImpactAndDestroy(collision);
        }
    }

    /// <summary>
    /// KamikazeSO와 동일한 방식으로, 충돌 대상에게 데미지를 주고 폭발 이펙트 생성 후 자신을 파괴합니다.
    /// </summary>
    private void HandleImpactAndDestroy(Collider2D collision)
    {
        // 1. 즉시 추가 충돌이 일어나지 않도록 콜라이더를 비활성화합니다.
        GetComponent<Collider2D>().enabled = false;

        // 2. 폭발 이펙트를 생성합니다.
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * effectExplosionRadius;
            Destroy(effect, 2f);
        }

        // 3. 충돌 대상에 따라 데미지를 처리합니다.
        if (collision.CompareTag("Core"))
        {
            Core core = collision.GetComponent<Core>();
            core?.TakeDamage(damage);
        }
        else if (collision.CompareTag("Ground"))
        {
            Tilemap tilemap = collision.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                Planet planetManager = tilemap.GetComponentInParent<Planet>();
                if (planetManager != null)
                {
                    Vector3 explosionCenterWorld = transform.position;
                    BoundsInt bounds = tilemap.cellBounds;

                    foreach (var cellPos in bounds.allPositionsWithin)
                    {
                        if (!tilemap.HasTile(cellPos)) continue;
                        Vector3 cellCenterWorld = tilemap.GetCellCenterWorld(cellPos);
                        if (Vector3.Distance(cellCenterWorld, explosionCenterWorld) <= effectExplosionRadius)
                        {
                            planetManager.DamageTile(cellPos, damage);
                        }
                    }
                }
            }
        }

        // 4. 모든 데미지 로직이 실행된 후, 이 프레임의 마지막에 오브젝트를 파괴합니다.
        Destroy(gameObject);
    }
}