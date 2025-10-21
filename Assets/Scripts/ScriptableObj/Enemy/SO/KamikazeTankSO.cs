using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "KamikazeTankSO", menuName = "ScriptableObjects/Enemy/KamikazeTankSO", order = 1)]
public class KamikazeTankSO : EnemyBaseSO
{
    [Header("KamikazeTank Stats")]
    public GameObject ExplosionEffectPrefab;
    public int damage = 10;
    public float explosionRadius = 3f; // 폭발 범위
    public LayerMask damageLayer;      // 데미지 적용할 레이어
    public override void PerformAttack(Enemy enemy)
    {
    }

    public void Explode(Enemy enemy, Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.collider.CompareTag("Core"))
        {
            GameObject ExplodeEffect = Instantiate(
                ExplosionEffectPrefab,
                enemy.transform.position,
                Quaternion.identity
            );
            ExplodeEffect.transform.GetChild(0).localScale = new Vector3(explosionRadius, explosionRadius,1f);
            Destroy(ExplodeEffect, 1f);
            // if (collision.collider.CompareTag("Core"))
            // {
            //     Core core = collision.collider.GetComponent<Core>();
            //     if (core != null)
            //     {
            //         core.TakeDamage(damage);  // Core의 체력 감소 함수 호출
            //     }
            //     return; // Core에 맞았으면 Tilemap 로직은 건너뜀
            // }
            // Core 데미지 처리 (폭발로 인한 Enemy 간 연쇄 방지)
            if (collision.collider.CompareTag("Core"))
            {
                Core core = collision.collider.GetComponent<Core>();
                if (core != null)
                {
                    core.TakeDamage(damage);
                }
            }    
            Tilemap tilemap = collision.collider.GetComponent<Tilemap>();

            // 타일맵과 충돌했을 때만 폭발 처리를 진행합니다.
            if (tilemap != null)
            {
                // 1. 충돌 지점을 폭발의 중심 좌표로 설정합니다.
                Vector3 explosionCenterWorld = enemy.transform.position;

                // 2. 타일맵의 유효 범위(Bounds)를 가져옵니다.
                BoundsInt bounds = tilemap.cellBounds;

                // 3. 타일맵의 모든 셀을 순회하며 폭발 반경 내에 있는지 확인합니다.
                foreach (var cellPos in bounds.allPositionsWithin)
                {
                    // 현재 셀 위치에 타일이 실제로 있는지 확인합니다.
                    if (!tilemap.HasTile(cellPos)) continue;

                    // 타일 셀의 월드 좌표 중심을 가져옵니다.
                    Vector3 cellCenterWorld = tilemap.GetCellCenterWorld(cellPos);

                    // 4. 타일 중심과 폭발 중심 사이의 거리를 계산하여 반경 내에 있는지 확인합니다.
                    if (Vector3.Distance(cellCenterWorld, explosionCenterWorld) <= explosionRadius)
                    {
                        
                        // 5. 폭발 반경 내에 있는 타일에 데미지 이벤트를 개별적으로 보냅니다.
                        // 타일 위치 계산
                        // Vector3 hitPoint = collision.GetContact(0).point;
                        // Vector3Int cellPos2 = tilemap.WorldToCell(hitPoint);
                        // 매니저 찾기
                        Planet manager = FindAnyObjectByType<Planet>();
                        manager?.DamageTile(cellPos, damage);
                        // else에 대한 Debug.LogError는 매번 루프에서 발생하는 것을 막기 위해 생략했습니다.
                    }
                }

            }
            enemy.myPool.Release(enemy.gameObject);
        }
    }
}
