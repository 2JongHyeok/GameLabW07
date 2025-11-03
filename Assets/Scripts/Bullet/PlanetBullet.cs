using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlanetBullet : MonoBehaviour
{
    [Header("총알 설정")]
    public float speed = 10f;
    public float lifeTime = 3f;
    [SerializeField] private float knockbackStrength = 3f;

    // --- [파편 설정 수정] ---
    [Header("Shrapnel Settings (파편)")]
    [SerializeField] private GameObject shrapnelPrefab; 
    
    [Tooltip("생성할 파편 개수")]
    [SerializeField] private int shrapnelCount = 30; // 30개로 수정
    
    [Tooltip("파편이 퍼지는 총 각도 (예: 60도)")]
    [SerializeField] private float spreadAngle = 60f;
    
    [Tooltip("파편 최소 속도")]
    [SerializeField] private float shrapnelMinSpeed = 8f;
    [Tooltip("파편 최대 속도")]
    [SerializeField] private float shrapnelMaxSpeed = 12f;
    
    [Tooltip("파편 최소 크기 (배율)")]
    [SerializeField] private float shrapnelMinSize = 0.8f;
    [Tooltip("파편 최대 크기 (배율)")]
    [SerializeField] private float shrapnelMaxSize = 1.2f;
    // --- [여기까지 수정] ---

    [SerializeField] private float knockbackBulletScale = 1.4f;
    
    [Header("이벤트 채널")]
    public TileDamageEventChannelSO onTileDamageChannel;

    private Rigidbody2D rb;

    void Start()
    {
        if(Managers.Instance.isUpgradeKnockback)
            speed = speed * 1.5f;
        
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.up * speed;
        Destroy(gameObject, lifeTime);
        
        if(Managers.Instance.isUpgradeKnockback)
            transform.localScale = new Vector3(knockbackBulletScale, knockbackBulletScale, knockbackBulletScale);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("PlanetBullet hit Enemy");

            Enemy enemyScript = collision.GetComponent<Enemy>();
            
            if (enemyScript == null)
            {
                Destroy(gameObject);
                return;
            }
            
            enemyScript.TakeDamage(Managers.Instance.weapon[0].GetDamage(), "PlanetBullet");
            
            bool canKnockback = Managers.Instance.isUpgradeKnockback;
            bool isBoss = (enemyScript.enemyData.enemyType == EnemyType.MainBoss || 
                           enemyScript.enemyData.enemyType == EnemyType.Boss);

            if (canKnockback && !isBoss)
            {
                Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
                enemyScript.ApplyKnockback(knockbackDirection, knockbackStrength);
            }
            
            // 4. [수정] 파편 생성 (이제 무작위 값을 계산해서 생성)
            if(Managers.Instance.isUpgradeShotgun)
                SpawnShrapnel();
            
            // 5. 총알 파괴
            Destroy(gameObject);
        }
    }
    
    // --- [파편 생성 함수 수정] ---
    private void SpawnShrapnel()
    {
        if (shrapnelPrefab == null)
        {
            Debug.LogWarning("Shrapnel Prefab이 할당되지 않았습니다!");
            return;
        }

        // 총알의 현재 각도를 기준으로 삼습니다.
        float baseAngle = transform.eulerAngles.z;
        float halfAngle = spreadAngle / 2f;

        // 설정된 개수(30개)만큼 파편 생성
        for (int i = 0; i < shrapnelCount; i++)
        {
            // 1. 각도 무작위화: 60도 범위 내에서 (-30 ~ +30) 무작위 각도 설정
            float randomAngle = baseAngle + Random.Range(-halfAngle, halfAngle);
            Quaternion spawnRotation = Quaternion.Euler(0, 0, randomAngle);

            // 2. 속도 무작위화
            float randomSpeed = Random.Range(shrapnelMinSpeed, shrapnelMaxSpeed);

            // 3. 크기 무작위화
            float randomSize = Random.Range(shrapnelMinSize, shrapnelMaxSize);
            
            // 4. 파편 생성
            GameObject shrapnelGO = Instantiate(shrapnelPrefab, transform.position, spawnRotation);
            
            // 5. 생성된 파편에 무작위 값 주입 (Initialize 함수 호출)
            Shrapnel shrapnelScript = shrapnelGO.GetComponent<Shrapnel>();
            if (shrapnelScript != null)
            {
                shrapnelScript.Initialize(randomSpeed, randomSize);
            }
            else
            {
                Debug.LogError("Shrapnel Prefab에 Shrapnel.cs 스크립트가 없습니다!");
            }
        }
    }
    // --- [여기까지 수정] ---
}