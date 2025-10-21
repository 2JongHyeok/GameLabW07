using UnityEngine;
using Unity.Cinemachine; // Cinemachine 3.x 이후 버전의 네임스페이스
using UnityEngine.InputSystem; // New Input System을 사용할 경우

public class CameraSwitcher : MonoBehaviour
{
    // 유니티 인스펙터에서 할당
    public CinemachineCamera planetCamera; // 행성 고정 카메라
    public CinemachineCamera spaceshipCamera; // 우주선 추적 카메라

    public float zoomSpeed = 5f; // 줌 속도
    [SerializeField] private float minPlanetCamZoom = 5f; // 최소 줌 크기 (Orthographic Size)
    [SerializeField] private float minShipCamZoom = 2f; // 최소 줌 크기 (Orthographic Size)
    [SerializeField] private float maxShipCamZoom = 20f; // 최대 줌 크기 (Orthographic Size)
    [SerializeField] private float maxPlanetCamZoom = 20f; // 최대 줌 크기 (Orthographic Size)
    [Header("Smooth Zoom Settings")] // 인스펙터에서 구분하기 위한 헤더
    public float smoothSpeed = 5f; // 목표 크기로 부드럽게 이동하는 속도 (Lerp)

    private bool isSpaceshipMode = false;
    private CinemachineCamera currentCamera;
    private float targetZoomSize; // 목표 Orthographic Size를 저장할 변수

    // Cinemachine의 Priority를 사용하여 카메라 전환
    private const int ActivePriority = 20;
    private const int InactivePriority = 10;

    // Unity의 Input System (InputManager)을 사용할 경우: "F" 키
    private const KeyCode SwitchKey = KeyCode.F;
    [SerializeField] ForgeManager forgeManager;
    void Start()
    {
        // 초기에는 행성 카메라를 활성화합니다.
        SwitchToPlanetCamera();
        targetZoomSize = currentCamera.Lens.OrthographicSize;
    }

    void Update()
    {
        // if(forgeManger.forgeUI.isForge)
        // {
        //     return;
        // }
        // 줌 처리 (마우스 휠)
        HandleZoom();
    }
    void LateUpdate()
    {
        if (currentCamera != null)
        {
            var lens = currentCamera.Lens;

            // 현재 크기에서 목표 크기로 smoothSpeed에 맞게 부드럽게 보간
            lens.OrthographicSize = Mathf.Lerp(
                lens.OrthographicSize,
                targetZoomSize,
                Time.deltaTime * smoothSpeed
            );

            currentCamera.Lens = lens;
        }
    }
    // 카메라 모드 전환 메서드
    public void ToggleCameraMode()
    {
        isSpaceshipMode = !isSpaceshipMode;

        if (isSpaceshipMode)
        {
            SwitchToSpaceshipCamera();
        }
        else
        {
            SwitchToPlanetCamera();
        }
    }

    private void SwitchToPlanetCamera()
    {
        spaceshipCamera.Priority = InactivePriority;
        planetCamera.Priority = ActivePriority;
        currentCamera = planetCamera;
        targetZoomSize = currentCamera.Lens.OrthographicSize;
    }

    public void SwitchToSpaceshipCamera()
    {
        planetCamera.Priority = InactivePriority;
        spaceshipCamera.Priority = ActivePriority;
        currentCamera = spaceshipCamera;
        targetZoomSize = currentCamera.Lens.OrthographicSize;
    }

    // 마우스 휠을 사용한 줌 처리
    void HandleZoom()
    {
        if (currentCamera == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            float newSize = targetZoomSize - scroll * zoomSpeed;

            if(isSpaceshipMode)
                targetZoomSize = Mathf.Clamp(newSize, minShipCamZoom, maxShipCamZoom);
            else
                targetZoomSize = Mathf.Clamp(newSize, minPlanetCamZoom, maxPlanetCamZoom);
        }
    }
}