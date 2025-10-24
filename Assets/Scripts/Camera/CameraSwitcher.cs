using UnityEngine;
using Unity.Cinemachine; // Unity 6 / Cinemachine 3.x

public class CameraSwitcher : MonoBehaviour
{
    [Header("Cameras (CM 3.x)")]
    [Tooltip("현재 활성 '행성' 시점 카메라 (시작 시 Planet1Cam 할당)")]
    [SerializeField] private CinemachineCamera planetCamera;

    [Tooltip("우주선 추적용 카메라 (SpaceshipCam)")]
    [SerializeField] private CinemachineCamera spaceshipCamera;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minPlanetCamZoom = 5f;
    [SerializeField] private float maxPlanetCamZoom = 20f;
    [SerializeField] private float minShipCamZoom = 2f;
    [SerializeField] private float maxShipCamZoom = 20f;

    [Header("Smooth Zoom")]
    [SerializeField] private float smoothSpeed = 5f;

    private CinemachineCamera currentCamera;
    private float targetZoomSize;
    

    private const int ActivePriority = 20;
    private const int InactivePriority = 10;

    void Start()
    {
        // 시작은 행성 카메라(Planet1Cam)로
        ActivatePlanet(planetCamera);
        targetZoomSize = currentCamera != null ? currentCamera.Lens.OrthographicSize : 5f;
        //DumpLive();
    }

    void Update() => HandleZoom();

    void LateUpdate()
    {
        if (currentCamera == null) return;
        var lens = currentCamera.Lens;
        lens.OrthographicSize = Mathf.Lerp(lens.OrthographicSize, targetZoomSize, Time.deltaTime * smoothSpeed);
        currentCamera.Lens = lens;
    }

    // ===== 외부 호출 =====

    /// <summary>행성 <-> 우주선 모드 토글</summary>
    public void ToggleCameraMode()
    {
        if (SpaceshipController.IsSpaceshipMode == true) ActivatePlanet(planetCamera);
        else ActivateSpaceship();
    }

    /// <summary>도킹 스테이션이 자신(행성)의 카메라를 알려줄 때 호출</summary>
    public void SetPlanetCamera(CinemachineCamera newPlanetCamera)
    {
        planetCamera = newPlanetCamera;
        Debug.Log($"[CameraSwitcher] SetPlanetCamera: {(planetCamera ? planetCamera.name : "null")}");
        // 우주선 모드가 아니라면 즉시 해당 행성으로 전환
        if (SpaceshipController.IsSpaceshipMode==false && planetCamera != null)
            ActivatePlanet(planetCamera);
    }

    /// <summary>우주선 모드로 전환</summary>
    public void ActivateSpaceship()
    {
        if (spaceshipCamera == null)
        {
            Debug.LogWarning("[CameraSwitcher] spaceshipCamera is null");
            return;
        }

        if (planetCamera) planetCamera.Priority.Value = InactivePriority;
        spaceshipCamera.Priority.Value = ActivePriority;

        currentCamera = spaceshipCamera;
        SpaceshipController.SetIsSpaceShipMode(true);
        targetZoomSize = currentCamera.Lens.OrthographicSize;

        Debug.Log($"[CameraSwitcher] 모드: 우주선 ({currentCamera.name}) prio={spaceshipCamera.Priority.Value}");
        DumpLive();
    }

    /// <summary>지정한 행성 카메라로 전환</summary>
    public void ActivatePlanet(CinemachineCamera planetCam)
    {
        if (planetCam == null)
        {
            Debug.LogWarning("[CameraSwitcher] ActivatePlanet: planetCam is null");
            return;
        }

        if (spaceshipCamera) spaceshipCamera.Priority.Value = InactivePriority;
        planetCam.Priority.Value = ActivePriority;

        planetCamera = planetCam;
        currentCamera = planetCam;
        SpaceshipController.SetIsSpaceShipMode(false);
        Debug.Log("ActivatePlanet");
        targetZoomSize = currentCamera.Lens.OrthographicSize;

        Debug.Log($"[CameraSwitcher] 모드: 행성 ({currentCamera.name}) prio={planetCam.Priority.Value}");
        DumpLive();
    }

    // ===== 내부 처리 =====

    private void HandleZoom()
    {
        if (currentCamera == null) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        float newSize = targetZoomSize - scroll * zoomSpeed;
        targetZoomSize = SpaceshipController.IsSpaceshipMode
            ? Mathf.Clamp(newSize, minShipCamZoom, maxShipCamZoom)
            : Mathf.Clamp(newSize, minPlanetCamZoom, maxPlanetCamZoom);
    }

    /// <summary>CM 3.x: 각 가상카메라의 IsLive로 라이브 여부 확인</summary>
    private void DumpLive()
    {
        string ship = spaceshipCamera ? $"{spaceshipCamera.name} live={spaceshipCamera.IsLive} prio={spaceshipCamera.Priority.Value}" : "spaceship=null";
        string planet = planetCamera ? $"{planetCamera.name} live={planetCamera.IsLive} prio={planetCamera.Priority.Value}" : "planet=null";
        Debug.Log($"[LiveCheck] {ship} | {planet}");
    }
}
