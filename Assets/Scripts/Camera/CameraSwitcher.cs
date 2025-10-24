using UnityEngine;
using Unity.Cinemachine;

public class CameraSwitcher : MonoBehaviour
{
    [Header("Spaceship Mode")]
    [SerializeField] private CinemachineCamera spaceshipCamera;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minPlanetCamZoom = 5f;
    [SerializeField] private float minShipCamZoom = 2f;
    [SerializeField] private float maxShipCamZoom = 20f;
    [SerializeField] private float maxPlanetCamZoom = 20f;
    [SerializeField] private float smoothSpeed = 5f;

    [Header("기본 카메라 설정")]
    [SerializeField] private CinemachineCamera defaultPlanetCamera;

    private CinemachineCamera currentCamera;
    private CinemachineCamera currentPlanetCamera;
    private bool isSpaceshipMode = false;
    private float targetZoomSize;

    private const int ActivePriority = 20;
    private const int InactivePriority = 10;

    private void Start()
    {
        if (defaultPlanetCamera != null)
        {
            currentPlanetCamera = defaultPlanetCamera;
            SwitchToCurrentPlanetCamera();
        }
        else
        {
            SwitchToSpaceshipCamera();
        }

        targetZoomSize = currentCamera?.Lens.OrthographicSize ?? 5f;
    }

    private void Update() => HandleZoom();

    private void LateUpdate()
    {
        if (currentCamera == null) return;
        var lens = currentCamera.Lens;
        lens.OrthographicSize = Mathf.Lerp(lens.OrthographicSize, targetZoomSize, Time.deltaTime * smoothSpeed);
        currentCamera.Lens = lens;
    }

    // =============================================================
    // 외부 호출
    // =============================================================
    public void ToggleCameraMode()
    {
        isSpaceshipMode = !isSpaceshipMode;
        if (isSpaceshipMode)
            SwitchToSpaceshipCamera();
        else
            SwitchToCurrentPlanetCamera();
    }

    public void SetPlanetCamera(CinemachineCamera newPlanetCamera)
    {
        // 단순히 참조만 갱신 (즉시 전환 X)
        currentPlanetCamera = newPlanetCamera;
    }

    // =============================================================
    // 내부 처리
    // =============================================================
    private void SwitchToSpaceshipCamera()
    {
        if (currentPlanetCamera != null)
            currentPlanetCamera.Priority = InactivePriority;

        spaceshipCamera.Priority = ActivePriority;
        currentCamera = spaceshipCamera;
        targetZoomSize = spaceshipCamera.Lens.OrthographicSize;
    }

    private void SwitchToCurrentPlanetCamera()
    {
        if (currentPlanetCamera == null)
        {
            Debug.LogWarning("행성 카메라가 설정되어 있지 않습니다!");
            return;
        }

        spaceshipCamera.Priority = InactivePriority;
        currentPlanetCamera.Priority = ActivePriority;
        currentCamera = currentPlanetCamera;
        targetZoomSize = currentPlanetCamera.Lens.OrthographicSize;
    }

    private void HandleZoom()
    {
        if (currentCamera == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0f) return;

        float newSize = targetZoomSize - scroll * zoomSpeed;
        targetZoomSize = isSpaceshipMode
            ? Mathf.Clamp(newSize, minShipCamZoom, maxShipCamZoom)
            : Mathf.Clamp(newSize, minPlanetCamZoom, maxPlanetCamZoom);
    }
}
