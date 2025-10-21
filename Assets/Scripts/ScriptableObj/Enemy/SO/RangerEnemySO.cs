using UnityEngine;

[CreateAssetMenu(fileName = "RangerEnemySO", menuName = "ScriptableObjects/Enemy/RangerEnemySO", order = 1)]
public class RangerEnemySO : EnemyBaseSO
{
    [Header("Ranger Stats")]
    public GameObject bulletPrefab;
    public float attackRange = 5f;
    public float bulletSpeed = 10f;
    public float attackCooldown = 2f;

    public override void PerformAttack(Enemy enemy)
    {
        Instantiate(bulletPrefab, enemy.firePoint.position, enemy.firePoint.rotation);
    }
}
