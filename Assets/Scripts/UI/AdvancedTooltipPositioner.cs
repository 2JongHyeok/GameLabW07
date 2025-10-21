using UnityEngine;

public class AdvancedTooltipPositioner : MonoBehaviour
{
    [Tooltip("마우스 커서로부터 얼마나 떨어질지 정합니다.")]
    [SerializeField] private Vector2 offset = new Vector2(15f, 15f);

    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void LateUpdate()
    {
        // 1. 마우스 위치에 오프셋을 더해 '희망' 위치를 정합니다.
        Vector2 targetPosition = (Vector2)Input.mousePosition + offset;

        // 2. 툴팁의 실제 픽셀 크기(너비/높이)를 계산합니다. (Canvas 스케일 반영)
        float tooltipWidth = rectTransform.rect.width * canvas.scaleFactor;
        float tooltipHeight = rectTransform.rect.height * canvas.scaleFactor;

        // 3. 핵심: 피봇(Pivot) 위치를 기준으로 툴팁의 '좌/우/아래/위' 경계가 어디인지 계산합니다.
        //   - 피봇의 X값이 0이면 왼쪽 끝, 0.5면 중앙, 1이면 오른쪽 끝입니다.
        //   - (tooltipWidth * rectTransform.pivot.x)는 '왼쪽 끝에서 피봇까지의 거리'를 의미합니다.
        float leftEdge = targetPosition.x - (tooltipWidth * rectTransform.pivot.x);
        float rightEdge = targetPosition.x + (tooltipWidth * (1 - rectTransform.pivot.x));
        float bottomEdge = targetPosition.y - (tooltipHeight * rectTransform.pivot.y);
        float topEdge = targetPosition.y + (tooltipHeight * (1 - rectTransform.pivot.y));

        // 4. 각 경계가 화면을 벗어났는지 확인하고, 벗어난 만큼 위치를 보정합니다.
        if (leftEdge < 0)
        {
            // 왼쪽으로 벗어남 -> 벗어난 만큼 오른쪽으로 민다.
            targetPosition.x -= leftEdge; 
        }
        else if (rightEdge > Screen.width)
        {
            // 오른쪽으로 벗어남 -> 벗어난 만큼 왼쪽으로 민다.
            targetPosition.x -= (rightEdge - Screen.width);
        }

        if (bottomEdge < 0)
        {
            // 아래쪽으로 벗어남 -> 벗어난 만큼 위로 올린다.
            targetPosition.y -= bottomEdge;
        }
        else if (topEdge > Screen.height)
        {
            // 위쪽으로 벗어남 -> 벗어난 만큼 아래로 내린다.
            targetPosition.y -= (topEdge - Screen.height);
        }
        
        // 5. 모든 보정이 끝난 최종 위치를 적용합니다.
        transform.position = targetPosition;
    }
}