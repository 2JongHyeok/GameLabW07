using UnityEngine;
using UnityEngine.Tilemaps;

public class SpaceshipMissile : MonoBehaviour
{
    [Header("기본 설정")]
    public float speed = 15f;
    public float lifeTime = 5f;

    // 더 이상 이벤트 채널을 사용하지 않습니다.
    // [Header("이벤트 채널")]
    // public TileDamageEventChannelSO onTileDamageChannel;

    private Rigidbody2D rb;
    [Header("이펙트 설정")]
    [Tooltip("충돌 시 생성할 파편 파티클 프리팹")]
    [SerializeField] private ParticleSystem debrisParticlePrefab;
    [Header("색상 감지 설정")]
    [Tooltip("타일 색상을 찾기 위해 충돌 지점 주변을 얼마나 넓게 탐색할지 정합니다. (단위: 셀)")]
    [Range(0, 5)] // 0: 중앙 1칸, 1: 3x3, 2: 5x5
    [SerializeField] private int colorSearchRadius = 1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// SpaceshipWeapon이 미사일을 생성한 직후 호출하여 초기 속도를 설정해주는 함수입니다.
    /// </summary>
    /// <param name="shipVelocity">미사일이 발사되는 순간의 우주선 속도</param>
    public void Initialize(Vector2 shipVelocity)
    {
        // ★ 핵심: 우주선의 현재 속도 + 미사일 자체의 발사 속도 = 최종 초기 속도
        rb.linearVelocity = shipVelocity + (Vector2)transform.up * speed;
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        AsteroidHealth targetAsteroid = collision.collider.GetComponentInParent<AsteroidHealth>();

        if (targetAsteroid != null)
        {
            Vector2 explosionCenterWorld = collision.GetContact(0).point;
            Tilemap targetTilemap = targetAsteroid.myTilemap;

            if (debrisParticlePrefab != null)
            {
                Color foundColor = Color.clear;
                bool colorFound = false;

                Vector3Int impactCell = targetTilemap.WorldToCell(explosionCenterWorld);

                // ✨ --- 핵심 개선 로직: 전방 탐색 --- ✨
                // 1. 미사일이 날아온 방향을 기억합니다.
                Vector2 missileDirection = rb.linearVelocity.normalized;

                for (int x = -colorSearchRadius; x <= colorSearchRadius; x++)
                {
                    for (int y = -colorSearchRadius; y <= colorSearchRadius; y++)
                    {
                        Vector3Int offset = new Vector3Int(x, y, 0);
                        Vector3Int cellToCheck = impactCell + offset;
                        
                        // 2. [추가] 현재 확인하려는 셀이 '뒤쪽'인지 판별합니다.
                        //    중심에서 현재 셀로의 방향 벡터를 구합니다.
                        Vector2 directionToCell = (Vector2)(targetTilemap.GetCellCenterWorld(cellToCheck) - (Vector3)explosionCenterWorld);

                        //    만약 미사일 방향과 셀 방향의 내적이 음수이면, 그 셀은 '뒤쪽'에 있다는 의미입니다.
                        if (Vector2.Dot(missileDirection, directionToCell.normalized) < 0 && directionToCell.sqrMagnitude > 0.1f)
                        {
                            continue; // 뒤쪽이면 무시하고 다음 셀로 넘어갑니다!
                        }
                        // ✨ --- 여기까지 --- ✨

                        if (targetTilemap.HasTile(cellToCheck))
                        {
                            Color potentialColor = targetTilemap.GetColor(cellToCheck);
                            
                            if (potentialColor.a > 0.1f)
                            {
                                foundColor = potentialColor;
                                colorFound = true;
                                goto Found; 
                            }
                        }
                    }
                }

                Found:

                    if (colorFound)
                    {
                        Vector3 particleSpawnPosition = explosionCenterWorld;
                        ParticleSystem debrisInstance = Instantiate(debrisParticlePrefab, particleSpawnPosition, Quaternion.identity);
                        
                        var main = debrisInstance.main;
                        main.startColor = foundColor;
                    }
            }

            targetTilemap.CompressBounds();
            BoundsInt bounds = targetTilemap.cellBounds;

            foreach (var cellPos in bounds.allPositionsWithin)
            {
                if (!targetTilemap.HasTile(cellPos)) continue;

                Vector3 cellCenterWorld = targetTilemap.GetCellCenterWorld(cellPos);

                // 폭발 범위 내에 있는지 확인
                if (Vector3.Distance(cellCenterWorld, explosionCenterWorld) <= Managers.Instance.spaceshipWeapon.GetExplosionRadius())
                {
                    // 이벤트 방송 대신, 타겟 소행성의 ApplyDamage 함수를 직접 호출합니다.
                    targetAsteroid.ApplyDamage(cellPos, Managers.Instance.spaceshipWeapon.GetDamage());
                }
            }
            if (TilemapShadowGenerator.Instance != null)
            {
                        Vector3Int explosionCenterCell = targetAsteroid.myTilemap.WorldToCell(collision.GetContact(0).point);
                        float explosionRadius = Managers.Instance.spaceshipWeapon.GetExplosionRadius();

                        // 월드 단위의 float 반경을 그대로 전달합니다.
                        TilemapShadowGenerator.Instance.UpdateShadowsAround(explosionCenterCell, explosionRadius);

            }
            Destroy(gameObject);

            }
        }
    }

