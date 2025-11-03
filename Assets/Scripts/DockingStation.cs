using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DockingStation : MonoBehaviour
{
    [Header("이 스테이션이 속한 '행성'의 카메라")]
    [SerializeField] private CinemachineCamera planetCamera;

    [Header("카메라 스위처 참조")]
    [SerializeField] private CameraSwitcher cameraSwitcher;

    [Header("도킹/출격용 우주선 오브젝트")]
    [SerializeField] private GameObject dockedShip;

    [Header("출격 위치 반경")]
    [SerializeField] private float departureRadius = 5f;

    private Vector3 nextDeparturePosition;
    private Quaternion nextDepartureRotation;

    [Header("UI 상태 알림")]
    [SerializeField] private BoolVariable canDepartState; // 출격 가능 상태
    [SerializeField] private BoolVariable isFlightModeState;
    // [SerializeField] public bool isSpaceshipMode = false; // [수정] 자체 상태 변수 대신 SpaceshipController의 전역 상태를 사용합니다.
    
    [SerializeField]private bool isShipDocked=false;

    [Header("이 행성의 무기")]
    [SerializeField] private Weapon[] planet1Weapon;
    [SerializeField] private Weapon2 planet2Weapon;
    [SerializeField] private AutoTurret planetAutoTurret;
    [SerializeField] private CameraType cameraType;
    private bool dockingInProgress = false; 
    public static DockingStation CurrentDockedStation { get; private set; }
    public void SetShipDockedState(bool val)
    {
        isShipDocked = val;
    }
    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void Start()
    {
        if (dockedShip != null)
        {
            CalculateNextDeparturePoint(dockedShip.transform.position);
            dockedShip.SetActive(false);
        }
        UpdateAllUIStates();
    }

    void Update()
    {
        if (Managers.Instance.IsTutorialActive) return;

        // 예: F키로 출격 (행성 -> 우주선)
        if (Input.GetKeyDown(KeyCode.F) && cameraSwitcher &&
        SpaceshipController.IsSpaceshipMode == false && isShipDocked == true)
        {
            PlayerGoToSpace();
        }
    }
    public bool IsShipDocked()
    {
        return isShipDocked;
    }
    public static void SetCurrentDockedStation(DockingStation station)
    {
        CurrentDockedStation = station;
    }
    public void PlayerGoToSpace()
    {
        isShipDocked = false;
        if (CurrentDockedStation == this)
            CurrentDockedStation = null;
        cameraSwitcher.ActivateSpaceship();
        ViewContext.I.SetCurrentView(CameraType.SpaceShip);
        SpaceshipController.SetIsSpaceShipMode(true);
        Debug.Log(isShipDocked);

        if (dockedShip)
        {
            //[Log] 출격 로그 출력
            GameAnalyticsLogger.instance.LogPlayerExitBase();
            LaunchShip();
            Debug.Log(transform.position + " " + gameObject.name + " " + nextDeparturePosition);
        }
        var wcs = FindFirstObjectByType<WeaponControlSwitcher>();
        wcs?.OnDockedPlanetChanged(CameraType.SpaceShip);
        UpdateAllUIStates();

        if (planet1Weapon != null && planet1Weapon.Length > 0)
        {
            foreach (var weapon in planet1Weapon)
            {
                if (weapon != null)
                    weapon.DeactivateWeapon();
            }
        }
        else if (planet2Weapon != null)
        {
            planet2Weapon.DeactivateWeapon(); // Weapon2에 새 메서드 추가 필요
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Spaceship")) return;
        if (!cameraSwitcher) return;
        if (!SpaceshipController.IsSpaceshipMode) return; // 우주선 모드일 때만 도킹
        if (isShipDocked || dockingInProgress) return;    // 재진입 가드
        dockingInProgress = true;
        SpaceshipController.SetIsSpaceShipMode(false);
        isShipDocked = true;
        CurrentDockedStation = this;
        Debug.Log($"Player가 Planet2에 도킹함");
        ViewContext.I.SetDockedPlanet(cameraType);
        FindFirstObjectByType<WeaponControlSwitcher>()?.OnDockedPlanetChanged(cameraType);
        cameraSwitcher.SetPlanetCamera(planetCamera);
        cameraSwitcher.ActivatePlanet(planetCamera);

        dockedShip = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        StoreShip();
        UpdateAllUIStates();
        dockingInProgress = false;

        GameAnalyticsLogger.instance.LogPlayerEnterBase();

        Debug.Log("[Dock] Planet2 dock start");
    }

  
    // [추가] 우주선 보관 로직
    private void StoreShip()
    {
        if (!dockedShip) return;
        CalculateNextDeparturePoint(dockedShip.transform.position);
        dockedShip.SetActive(false);
        if (planet1Weapon != null && planet1Weapon.Length > 0)
        {
            foreach (var weapon in planet1Weapon)
            {
                if (weapon != null)
                    weapon.ActivateWeapon();
            }
        }
        else if (planet2Weapon != null)
        {
            planet2Weapon.ActivateWeapon();
        }
    }

    // [추가] 우주선 출격 로직
    private void LaunchShip()
    {
        if (!dockedShip) return;
        dockedShip.transform.SetPositionAndRotation(nextDeparturePosition, nextDepartureRotation);
        dockedShip.SetActive(true);
    }
    
    private void UpdateAllUIStates()
    {
        // 1. 출격 가능 상태 업데이트 (기존 로직)
        if (canDepartState != null)
        {
            // [수정] isSpaceshipMode 대신 SpaceshipController.IsSpaceshipMode 사용
            canDepartState.Value = !SpaceshipController.IsSpaceshipMode;
        }

        // 2. 비행 모드 상태 업데이트 (새로운 로직)
        if (isFlightModeState != null)
        {
            // [수정] isSpaceshipMode 대신 SpaceshipController.IsSpaceshipMode 사용
            isFlightModeState.Value = SpaceshipController.IsSpaceshipMode;
        }
    }

    private void CalculateNextDeparturePoint(Vector3 basis)
    {
        var dir = (basis - transform.position).normalized;
        if (dir == Vector3.zero) dir = Vector3.up;

        nextDeparturePosition = transform.position + dir * departureRadius;
        nextDepartureRotation = Quaternion.LookRotation(Vector3.forward, dir);
    }


}
