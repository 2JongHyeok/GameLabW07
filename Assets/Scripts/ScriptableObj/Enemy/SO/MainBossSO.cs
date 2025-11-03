using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "MainBossSO", menuName = "ScriptableObjects/Enemy/MainBossSO", order = 1)]
public class MainBossSO : EnemyBaseSO
{
    [Header("MainBoss Stats")]
    public GameObject bulletPrefab;
    public float attackRange = 5f;
    public float bulletSpeed = 10f;
    public float attackCooldown = 2f;
    [Tooltip("한 번의 공격에 출격시킬 인터셉터의 수")]
    public int interceptorCount = 5; // 메인 보스는 더 많이 발사하도록 기본값 변경

    public override void PerformAttack(Enemy enemy)
    {
        if (bulletPrefab == null || enemy.firePoint == null) return;

        // 지정된 interceptorCount 만큼 반복하여 인터셉터를 생성합니다.
        for (int i = 0; i < interceptorCount; i++)
        {
            // 1. 인터셉터 프리팹을 발사 지점에 생성합니다.
            GameObject interceptorGO = Instantiate(bulletPrefab, enemy.firePoint.position, Quaternion.identity);

            // 2. 생성된 인터셉터에서 Interceptor 스크립트를 가져옵니다.
            Interceptor interceptor = interceptorGO.GetComponent<Interceptor>();
            if (interceptor != null)
            {
                // 3. 인터셉터에게 공격할 목표(target)를 알려줍니다.
                interceptor.SetTarget(enemy.target);
            }
        }
    }
}
