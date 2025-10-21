using UnityEngine;
using UnityEngine.Tilemaps;
public class EnemyBullet : MonoBehaviour
{
    public int damage = 1;
    public float speed = 10f;
    private Rigidbody2D rb;
    public float lifeTime = 3f;
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.up * speed;
        Destroy(gameObject,lifeTime);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Core"))
        {
            Core core = collision.collider.GetComponent<Core>();
            if (core != null)
            {
                core.TakeDamage(damage);  // Core의 체력 감소 함수 호출
            }
            Destroy(gameObject);
            return; // Core에 맞았으면 Tilemap 로직은 건너뜀
        }
        Tilemap tilemap = collision.collider.GetComponent<Tilemap>();
        if (tilemap != null)
        {
            // 타일 위치 계산
            Vector3 hitPoint = collision.GetContact(0).point;
            Vector3 correctedHitPoint = hitPoint - ((Vector3)collision.GetContact(0).normal * 0.01f);
            // Vector3Int cellPos = tilemap.WorldToCell(correctedHitPoint);
            Vector3Int cellPos2 = tilemap.WorldToCell(correctedHitPoint);
            // 매니저 찾기
            Planet manager = FindAnyObjectByType<Planet>();
            manager?.DamageTile(cellPos2, damage);
            Destroy(gameObject);
        }
        Destroy(gameObject,lifeTime);
    }
}