using UnityEngine;
using UnityEngine.Tilemaps;

public class PlanetBullet : MonoBehaviour
{
    [Header("총알 설정")]
    public float speed = 10f;
    [Tooltip("폭발 반경 내의 각 타일에 입힐 데미지 양입니다.")]
    public float lifeTime = 3f;

    

    [Header("이벤트 채널")]
    public TileDamageEventChannelSO onTileDamageChannel;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.up * speed;
        GameAnalyticsLogger.instance.playerBulletCount++;
        Destroy(gameObject, lifeTime);
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Enemy"))
        {
            GameAnalyticsLogger.instance.playerBulletHitCount++;
            collision.collider.GetComponent<Enemy>().TakeDamage(Managers.Instance.weapon[0].GetDamage(), "PlanetBullet");
            Destroy(gameObject);
        }
        Destroy(gameObject); // 폭발 후 미사일 파괴
    }
}