using UnityEngine;

[CreateAssetMenu(fileName = "RangerEnemyTankSO", menuName = "ScriptableObjects/Enemy/RangerEnemyTankSO", order = 1)]
public class RangerEnemyTankSO : EnemyBaseSO
{
    [Header("RangerTank Stats")]
    public GameObject bulletPrefab;
    public float attackRange = 5f;
    public float bulletSpeed = 10f;
    public float attackCooldown = 2f;

    public override void PerformAttack(Enemy enemy)
    {
        Instantiate(bulletPrefab, enemy.firePoint.position, enemy.firePoint.rotation);
    }
}
