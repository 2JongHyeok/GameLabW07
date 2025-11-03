using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Shrapnel : MonoBehaviour
{
    [Header("파편 설정")]
    // [삭제] speed 변수는 이제 PlanetBullet에서 받아옵니다.
    [SerializeField] private int damage = 5; 
    [SerializeField] private float lifetime = 1.0f; 
    [SerializeField] private float knockbackStrength = 1.5f; 

    private Rigidbody2D rb;
    private bool canCollide = false;

    // Start() 대신 Awake()를 사용하여 Rigidbody를 미리 찾아둡니다.
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; 
    }

    /// <summary>
    /// [핵심] PlanetBullet이 파편을 생성한 직후 이 함수를 호출하여
    /// 무작위 속도와 크기를 설정해줍니다.
    /// </summary>
    /// <param name="newSpeed">총알이 정해준 무작위 속도</param>
    /// <param name="newScale">총알이 정해준 무작위 크기</param>
    public void Initialize(float newSpeed, float newScale)
    {
        // 1. 무작위 속도 적용
        rb.linearVelocity = transform.up * newSpeed; 
        
        // 2. 무작위 크기 적용
        transform.localScale = Vector3.one * newScale;

        // 3. 수명 설정
        Destroy(gameObject, lifetime);
        
        // 4. 충돌 유예 코루틴 시작
        StartCoroutine(ActivateCollisionDelay());
    }

    // [기존과 동일] 충돌 유예 코루틴
    private IEnumerator ActivateCollisionDelay()
    {
        yield return new WaitForSeconds(0.05f);
        canCollide = true; 
    }

    // [기존과 동일] 충돌 처리
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!canCollide)
        {
            return;
        }

        if (collision.CompareTag("Enemy"))
        {
            Enemy enemyScript = collision.GetComponent<Enemy>();
            if (enemyScript == null)
            {
                Destroy(gameObject);
                return;
            }

            // 1. 파편 데미지
            enemyScript.TakeDamage(damage, "Shrapnel");
            
            // 2. 넉백 적용 여부 판단
            bool canKnockback = Managers.Instance.isUpgradeKnockback;
            bool isBoss = (enemyScript.enemyData.enemyType == EnemyType.MainBoss || 
                           enemyScript.enemyData.enemyType == EnemyType.Boss);

            if (canKnockback && !isBoss)
            {
                Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
                enemyScript.ApplyKnockback(knockbackDirection, knockbackStrength);
            }
            
            // 3. 파편 파괴
            Destroy(gameObject);
        }
    }
}