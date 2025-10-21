using UnityEngine;

[CreateAssetMenu(fileName = "ParasiteSO", menuName = "ScriptableObjects/Enemy/ParasiteSO", order = 1)]
public class ParasiteSO : EnemyBaseSO
{
    [Header("Parasite Stats")]
    public GameObject ParasitePrefab;
    public float attackRange = 5f;
    public float bulletSpeed = 10f;
    public float attackCooldown = 2f;

    public override void PerformAttack(Enemy enemy)
    {
        Instantiate(ParasitePrefab, enemy.firePoint.position, enemy.firePoint.rotation);
    }
}
