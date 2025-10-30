using UnityEngine;
using Unity.Cinemachine; // Unity 6 / Cinemachine 3.x

public class CameraSwitcher : MonoBehaviour
{
    [Header("Cameras (CM 3.x)")]
    [Tooltip("현재 활성 '행성' 시점 카메라 (시작 시 Planet1Cam 할당)")]
    [SerializeField] private CinemachineCamera planetCamera;

    [Tooltip("우주선 추적용 카메라 (SpaceshipCam)")]
    [SerializeField] private CinemachineCamera spaceshipCamera;
    [SerializeField] private Camera minimapCamera;
    public GameObject player;

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
    public System.Action<CameraType> OnViewChanged;

    private const int ActivePriority = 20;
    private const int InactivePriority = 10;
    
    [Header("Minimap")]
    [Tooltip("미니맵 카메라가 플레이어를 따라다니도록 설정합니다.")]
    public bool followPlayer = true;
    public bool isOnMinimap = false;
    [SerializeField] private float minimapFollowSpeed = 5f;
    [Tooltip("플레이어의 이 속도(sqrMagnitude) 이상으로 움직여야 미니맵이 다시 따라갑니다.")]
    [SerializeField] private float movementReFollowThreshold = 0.1f;
    private Rigidbody2D playerRb;
    

    void Start()
    {
        // 시작은 행성 카메라(Planet1Cam)로
        ActivatePlanet(planetCamera);
        targetZoomSize = currentCamera != null ? currentCamera.Lens.OrthographicSize : 5f;
        isOnMinimap = false;
        followPlayer = true;
        //DumpLive();
        
        playerRb = player.GetComponent<Rigidbody2D>();
            

    }
    
    void Update()
    {
        HandleZoom(); // 메인 카메라 줌 처리

        // 플레이어 움직임 감지하여 복귀 처리
        CheckForPlayerMovementToReFollow();
    }

    void LateUpdate()
    {
        if (player != null)
        {
            // 1. 목표 위치 (플레이어 위치) 설정
            Vector3 targetPosition = new Vector3(player.transform.position.x, player.transform.position.y, -20f);

            // 2. followPlayer가 true일 때만 부드럽게 이동 (Lerp)
            if (followPlayer)
            {
                minimapCamera.transform.position = Vector3.Lerp(
                    minimapCamera.transform.position, // 현재 위치
                    targetPosition,                   // 목표 위치
                    Time.deltaTime * minimapFollowSpeed); // 속도
            } 
        }
        
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
        OnViewChanged?.Invoke(CameraType.SpaceShip);
    }

    public void ActivatePlanet(CinemachineCamera planetCam)
    {
        if (planetCam == null)
        {
            Debug.LogWarning("[CameraSwitcher] ActivatePlanet: planetCam is null");
            return;
        }

        //if (spaceshipCamera) spaceshipCamera.Priority.Value = InactivePriority;
        if(currentCamera!= null)
            currentCamera.Priority.Value = InactivePriority;
        planetCam.Priority.Value = ActivePriority;

        planetCamera = planetCam;
        currentCamera = planetCam;
        if (ViewContext.I == null || ViewContext.I.DockedPlanet != CameraType.SpaceShip)
            SpaceshipController.SetIsSpaceShipMode(false);
        targetZoomSize = currentCamera.Lens.OrthographicSize;

        Debug.Log($"[CameraSwitcher] 모드: 행성 ({currentCamera.name}) prio={planetCam.Priority.Value}");
        DumpLive();
        var t = planetCam.GetComponent<CameraTypeTag>()?.type ?? CameraType.Planet1;
        if (ViewContext.I != null)
            ViewContext.I.SetCurrentView(t);
        OnViewChanged?.Invoke(t);
    }

    // ===== 내부 처리 =====

    private void HandleZoom()
    {
        if (currentCamera == null) return;
        if (isOnMinimap) return;
        
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
    }
    
    // --- ▼ 이 함수를 새로 추가하세요 ▼ ---
    /// <summary>
    /// 플레이어가 움직이는지 감지하여 미니맵 추적을 다시 시작합니다.
    /// </summary>
    private void CheckForPlayerMovementToReFollow()
    {
        if (Input.GetMouseButton(2))
        {
            Debug.Log("중앙 버튼 눌림: 미니맵 추적 일시 중지");
            return;
        }
        
        // 이미 따라가고 있거나, Rigidbody가 없으면 검사 안 함
        if (followPlayer || playerRb == null)
        {
            return;
        }

        // 플레이어의 속도(제곱)가 설정한 임계값보다 커지면
        // (sqrMagnitude는 Vector3.Distance보다 연산 비용이 훨씬 쌈)
        if (playerRb.linearVelocity.sqrMagnitude > movementReFollowThreshold)
        {
            Debug.Log("플레이어 움직임 감지: 미니맵 추적 재개");
            // 추적 모드로 다시 변경
            followPlayer = true;
        }
    }
    // --- ▲ 이 함수를 새로 추가하세요 ▲ ---
}
