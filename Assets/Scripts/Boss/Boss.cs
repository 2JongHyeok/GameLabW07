// BossDescent2D.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Boss : MonoBehaviour
{
    [Header("이동")]
    [Tooltip("초당 하강 속도(유닛/초)")]
    [SerializeField] private float descentSpeed = 1.5f;

    [Header("충돌/파괴 설정")]
    [Tooltip("소행성으로 취급할 태그")]
    [SerializeField] private string asteroidTag = "Asteroid";

    [Tooltip("보스가 파괴를 적용할 레이어들(비워두면 전부 허용)")]
    [SerializeField] private LayerMask destroyLayers;

    Rigidbody2D rb;
    Vector2 startPos;
    float t;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;        // 중력 사용 안 함
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        startPos = rb.position;
    }

    void FixedUpdate()
    {
        t += Time.fixedDeltaTime;

        // 기본 하강
        float dy = -descentSpeed * Time.fixedDeltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (destroyLayers.value != 0)
        {
            if ((destroyLayers.value & (1 << other.gameObject.layer)) == 0)
                return;
        }

        if (!string.IsNullOrEmpty(asteroidTag) && other.CompareTag(asteroidTag))
        {
            Destroy(other.gameObject);
            return;
        }
    }
}
