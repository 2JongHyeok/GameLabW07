using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 10f;

    // 이 투사체를 발사한 포탑의 Transform (필요에 따라)
    protected Transform shooter;

    public virtual void Init(float dmg, Transform shooterTransform)
    {
        damage = dmg;
        shooter = shooterTransform;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // Enemy 태그를 가진 오브젝트와 충돌했을 때
        if (other.CompareTag("Enemy"))
        {
            // 예시: 데미지를 입히는 로직 (EnemyScript에 TakeDamage 함수가 있다고 가정)
            // other.GetComponent<EnemyScript>()?.TakeDamage(damage); 

            // 일반 총알이나 유도탄은 충돌 후 파괴됩니다.
            Destroy(gameObject);
        }
    }
}