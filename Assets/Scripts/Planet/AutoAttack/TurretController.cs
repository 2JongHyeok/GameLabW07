using UnityEngine;

public class TurretController : MonoBehaviour
{
    public float fireRate = 1f; // 공격 속도 (초당 공격 횟수)
    private float nextFireTime = 0f;
    public GameObject projectilePrefab; // 발사체 Prefab
    public float range = 10f; // 적 탐지 범위
    public float damage = 10f; // 기본 데미지

    private Transform target;

    void Update()
    {
        // 1. 타겟 탐색
        FindTarget();

        // 2. 공격 주기 체크 및 발사
        if (target != null && Time.time >= nextFireTime)
        {
            //sAttack();
            nextFireTime = Time.time + 1f / fireRate;
        }
    }

    void FindTarget()
    {
        // 가장 가까운 적을 찾는 로직 (예: Physics.OverlapSphere)
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, range, LayerMask.GetMask("Enemy"));

        float minDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        foreach (Collider collider in enemiesInRange)
        {
            // Enemy Tag/Layer 확인
            if (collider.CompareTag("Enemy"))
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = collider.transform;
                }
            }
        }
        target = closestEnemy;
    }

    //void Attack()
    //{
    //    // 발사체 생성 및 설정
    //    GameObject projectileGO = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
    //    // 각 공격 유형에 따라 다른 Projectile 스크립트를 GetComponent로 가져와 설정
    //    // 예시: HomingMissile 스크립트에 타겟 설정
    //    if (projectileGO.TryGetComponent<HomingMissile>(out HomingMissile missile))
    //    {
    //        missile.Initialize(target, damage); // 유도탄 초기화
    //    }
    //    else if (projectileGO.TryGetComponent<Bullet>(out Bullet bullet))
    //    {
    //        bullet.Initialize(target.position, damage); // 총알 초기화
    //    }
    //    // ... 레이저의 경우 별도 처리 ...
    //}
}