using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // ## 수정 ##: Slider를 사용하기 위해 추가

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

    [Header("UI Reference")] // ## 수정 ##: 헤더 추가
    [SerializeField] private Slider zoomSlider;
    
    // ## 수정 ##: 슬라이더 이벤트 중복 방지용 플래그
    private bool isSliderUpdating = false; 

    void Start()
    { 
        targetZoomSize = minimapCamera.orthographicSize;
        
        // ## 수정 ##: 슬라이더 초기화 및 리스너 연결
        InitializeSlider();
    }
    
    // ## 수정 ##: 슬라이더 초기화 함수
    private void InitializeSlider()
    {
        if (zoomSlider != null)
        {
            // 슬라이더의 값 범위를 0~1 (정규화된 값)로 설정
            zoomSlider.minValue = 0f;
            zoomSlider.maxValue = 1f;
            
            // ## 수정 ##: 슬라이더 매핑을 반대로 설정 (0=maxZoom, 1=minZoom)
            // 현재 줌 값을 0~1 사이로 변환하여 슬라이더 초기값 설정
            float initialSliderValue = Mathf.InverseLerp(maxZoom, minZoom, targetZoomSize); // min/max 위치 변경
            zoomSlider.value = initialSliderValue;
            
            // 슬라이더 값이 변경될 때 호출될 함수 연결
            zoomSlider.onValueChanged.AddListener(OnSliderValueChanged);

            zoomSlider.value = 0.5f;
        }
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
        // ## 수정 ##: LateUpdate에서는 부드러운 줌 처리만 담당
        
        
        if (!isOnMinimap)
        {
            return;
        }
        
        // 마우스가 미니맵 위에 있을 때만 휠/드래그 처리
        HandleMouseWheelZoom(); // ## 수정 ##: 휠 줌 로직 분리
        HandleMiddleMouseDrag();
        
        HandleSmoothZoom();
    }

    // ## 수정 ##: 마우스 휠 입력 처리 (슬라이더 매핑 반대 적용)
    private void HandleMouseWheelZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            // 1. 목표 줌 크기를 먼저 계산 (기존 로직 유지: 휠 올리면 줌 인)
            float newTargetZoom = targetZoomSize - scrollInput * zoomSpeed;
            newTargetZoom = Mathf.Clamp(newTargetZoom, minZoom, maxZoom);
            
            // 2. ## 수정 ##: 목표 줌 크기를 반대(0=max, 1=min)의 0~1 값으로 변환
            float newSliderValue = Mathf.InverseLerp(maxZoom, minZoom, newTargetZoom); // min/max 위치 변경
            
            // 3. 슬라이더의 value 값을 변경 (이러면 OnSliderValueChanged가 자동 호출됨)
            if (zoomSlider != null)
            {
                zoomSlider.value = newSliderValue;
            }
            else // 슬라이더가 없으면 직접 targetZoomSize 변경
            {
                targetZoomSize = newTargetZoom;
            }
        }
    }
    
    // ## 수정 ##: 슬라이더 값 변경 시 호출될 함수 (슬라이더 매핑 반대 적용)
    public void OnSliderValueChanged(float sliderValue)
    {
        // isSliderUpdating 플래그를 사용하여 무한 루프 방지
        if (isSliderUpdating) return; 
        
        // ## 수정 ##: sliderValue (0~1)를 반대(0=max, 1=min)의 실제 줌 크기로 변환
        targetZoomSize = Mathf.Lerp(maxZoom, minZoom, sliderValue); // min/max 위치 변경
    }
    
    // 부드러운 줌 인/아웃 처리
    private void HandleSmoothZoom()
    {
        // targetZoomSize는 마우스 휠이나 슬라이더에 의해 이미 설정됨
        
        // 현재 카메라 줌 값을 목표 줌 값으로 부드럽게 이동
        minimapCamera.orthographicSize = Mathf.Lerp(
            minimapCamera.orthographicSize, 
            targetZoomSize, 
            Time.unscaledDeltaTime * smoothSpeed);
            
        // ## 수정 ##: 카메라 줌이 변경되면, 이 값을 다시 반대(0=max, 1=min)의 0~1로 변환하여 슬라이더에 반영
        if (zoomSlider != null)
        {
            isSliderUpdating = true; // 슬라이더 값 변경 시작 (이벤트 호출 방지)
            float currentSliderValue = Mathf.InverseLerp(maxZoom, minZoom, minimapCamera.orthographicSize); // min/max 위치 변경
            zoomSlider.value = currentSliderValue;
            isSliderUpdating = false; // 슬라이더 값 변경 완료
        }
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