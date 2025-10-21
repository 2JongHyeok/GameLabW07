// TargetIndicator.cs
using UnityEngine;

public class TargetIndicator : MonoBehaviour
{
    [Header("기본 설정")]
    [Tooltip("나침반이 가리킬 목표 지점의 좌표")]
    public Vector2 targetPosition = Vector2.zero;
    [Tooltip("우주선 중심으로부터 인디케이터가 떨어질 거리(반지름)")]
    public float indicatorRadius = 2.5f;
    
    [Header("시각 효과 설정")]
    [Tooltip("실제 보여질 그래픽 부분 (자식 오브젝트)")]
    [SerializeField] private Transform indicatorVisual;
    [Tooltip("가까울 때의 색 (좋은 색)")]
    [SerializeField] private Color closeColor = Color.cyan;
    [Tooltip("멀 때의 색 (나쁜 색)")]
    [SerializeField] private Color farColor = Color.red;
    [Tooltip("효과가 적용되기 시작하는 최소 거리")]
    [SerializeField] private float minDistance = 5f;
    [Tooltip("효과가 최대로 적용되는 최대 거리")]
    [SerializeField] private float maxDistance = 50f;

    // --- 이 부분이 추가됐어. 똑똑히 봐. ---
    [Header("크기 제한 설정")]
    [Tooltip("가까울 때의 최대 크기 배율")]
    [Range(0f, 50f)]
    [SerializeField] private float maxScale = 1.0f;

    [Tooltip("아무리 멀어져도 유지할 최소 크기 배율")]
    [Range(0f, 10f)]
    [SerializeField] private float minScale = 0.2f;


    private Transform parentTransform;
    private SpriteRenderer visualSpriteRenderer;
    private Vector3 initialScale;

    void Start()
    {
        parentTransform = transform.parent;
        if (parentTransform == null)
        {
            this.enabled = false;
            return;
        }
        if (indicatorVisual == null)
        {
            this.enabled = false;
            return;
        }
        visualSpriteRenderer = indicatorVisual.GetComponent<SpriteRenderer>();
        initialScale = indicatorVisual.localScale;
    }

    void Update()
    {
        if (parentTransform == null) return;

        // --- 회전 및 위치 로직 (변경 없음) ---
        Vector2 direction = targetPosition - (Vector2)parentTransform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.position = (Vector2)parentTransform.position + direction.normalized * indicatorRadius;
        
        // --- 시각 효과 로직 (수정됨) ---
        float distance = direction.magnitude;
        float t = Mathf.InverseLerp(maxDistance, minDistance, distance);
        t = Mathf.Clamp01(t);

        // --- 이 부분이 핵심이야. 최소/최대 크기를 보간하는 로직으로 바꿨어. ---
        float currentScaleMultiplier = Mathf.Lerp(minScale, maxScale, t);
        indicatorVisual.localScale = initialScale * currentScaleMultiplier;
        
        visualSpriteRenderer.color = Color.Lerp(farColor, closeColor, t);
    }
}