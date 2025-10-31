using UnityEngine;
using UnityEngine.EventSystems;

public class MinimapExtend : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("ref")]
    [Tooltip("줌/이동을 제어할 미니맵 카메라")]
    [SerializeField] private Camera minimapCamera;
    [Tooltip("메인 카메라 컨트롤러")]
    [SerializeField] private CameraSwitcher cameraSwitcher;

    [Header("setting")]
    [Tooltip("줌 인/아웃 속도")]
    [SerializeField] private float zoomSpeed = 5f;
    [Tooltip("줌 부드러움 (Lerp 속도)")]
    [SerializeField] private float smoothSpeed = 10f;
    [Tooltip("최소 줌 (가장 가까이)")]
    [SerializeField] [Range(1f, 10f)] private float minZoom = 2f;
    [Tooltip("최대 줌 (가장 멀리)")]
    [SerializeField] [Range(10f, 100f)] private float maxZoom = 20f;
    [Tooltip("휠 버튼 드래그 이동 속도")]
    [SerializeField] private float dragSpeed = 0.05f;

    private bool isOnMinimap = false;
    private float targetZoomSize; // 목표 줌 크기
    private Vector3 lastDragPosition; // 드래그 시작 위치

    void Start()
    { 
        // 시작 시 목표 줌을 현재 줌으로 설정
        targetZoomSize = minimapCamera.orthographicSize;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isOnMinimap = true;
        
        if (cameraSwitcher != null)
        {
            cameraSwitcher.isOnMinimap = true;
            Managers.Instance.spaceshipMotor.isOnMinimap = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isOnMinimap = false;

        if (cameraSwitcher != null)
        {
            cameraSwitcher.isOnMinimap = false;
            Managers.Instance.spaceshipMotor.isOnMinimap = false;
        }
    }

    void LateUpdate()
    {
        if (!isOnMinimap)
        {
            return;
        }

        HandleSmoothZoom();
        HandleMiddleMouseDrag();
    }

    // 부드러운 줌 인/아웃 처리
    private void HandleSmoothZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            targetZoomSize -= scrollInput * zoomSpeed;
            targetZoomSize = Mathf.Clamp(targetZoomSize, minZoom, maxZoom);
        }

        minimapCamera.orthographicSize = Mathf.Lerp(
            minimapCamera.orthographicSize, 
            targetZoomSize, 
            Time.unscaledDeltaTime * smoothSpeed);
    }

    private void HandleMiddleMouseDrag()
    {
        // 1. 휠 버튼을 '누른' 순간
        if (Input.GetMouseButtonDown(2))
        {
            lastDragPosition = Input.mousePosition;
            if (cameraSwitcher != null)
            {
                Debug.Log("Minimap drag started, disabling followPlayer.");
                cameraSwitcher.followPlayer = false;
            }
        }
        // 2. 휠 버튼을 '누르고 있는' 동안
        else if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastDragPosition;
            Vector3 move = new Vector3(-delta.x, -delta.y, 0) * dragSpeed;
            minimapCamera.transform.Translate(move, Space.World);
            lastDragPosition = Input.mousePosition;
        }
    }
}