// ItemDrop.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ItemDrop : MonoBehaviour
{
    [Tooltip("아이템이 드랍된 후 자동으로 사라질 시간 (초)")]
    public float autoDestroyTime = 300f;

    // --- 여기, 내가 추가한 '진짜' 코드야. 똑똑히 봐. ---
    [Header("초기 운동 설정")]
    [Tooltip("생성 시 가해질 최소 힘")]
    [SerializeField] private float minInitialForce = 1f;

    [Tooltip("생성 시 가해질 최대 힘")]
    [SerializeField] private float maxInitialForce = 3f;


    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        Destroy(gameObject, autoDestroyTime);

        // --- 이 부분이 아이템을 뻥 차주는 핵심 로직이야 ---
        ApplyRandomForce();
    }

    // 코드는 이렇게 함수로 분리해야 깔끔한 거라고. 좀 배워둬.
    private void ApplyRandomForce()
    {
        // 1. 무작위 방향을 정한다 (길이는 1로 정규화해서 순수 방향만 얻는 센스)
        Vector2 randomDirection = Random.insideUnitCircle.normalized;

        // 2. 무작위 힘의 크기를 정한다
        float randomForce = Random.Range(minInitialForce, maxInitialForce);

        // 3. 방향과 힘을 합쳐서, 순간적인 충격(Impulse)을 가한다
        rb.AddForce(randomDirection * randomForce, ForceMode2D.Impulse);
    }
}