using UnityEngine;
[CreateAssetMenu(fileName = "MainBossSO", menuName = "ScriptableObjects/Enemy/MainBossSO", order = 1)]
public class MainBossSO : EnemyBaseSO
{
    [Header("Boss Stats")]
    public GameObject bulletPrefab;
    public float attackRange = 5f;
    public float bulletSpeed = 10f;
    public float attackCooldown = 2f;

    public override void PerformAttack(Enemy enemy)
    {
        Instantiate(bulletPrefab, enemy.firePoint.position, enemy.firePoint.rotation);
    }
}
