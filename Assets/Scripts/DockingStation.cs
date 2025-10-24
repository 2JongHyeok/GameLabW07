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
    [SerializeField] public bool isSpaceshipMode = false;
    
    [SerializeField]private bool isShipDocked=false;

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
        // 예: F키로 출격 (행성 -> 우주선)
        if (Input.GetKeyDown(KeyCode.F) && cameraSwitcher && 
            SpaceshipController.IsSpaceshipMode==false&& isShipDocked==true)
        {
            isShipDocked = false;
            Debug.Log(isShipDocked);
            cameraSwitcher.ActivateSpaceship();
            SpaceshipController.SetIsSpaceShipMode(true);
            if (dockedShip)
            {
                Debug.Log(transform.position+" "+gameObject.name+" "+ nextDeparturePosition);
                dockedShip.transform.SetPositionAndRotation(nextDeparturePosition, nextDepartureRotation);
                dockedShip.SetActive(true);
                isSpaceshipMode = true;

                UpdateAllUIStates();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Spaceship")) return;
        if (!cameraSwitcher) return;
        Debug.Log("IsSpaceshipMode"+SpaceshipController.IsSpaceshipMode);
        // 우주선 모드일 때만 도킹 처리 (중복 토글/오차 방지)
        if (SpaceshipController.IsSpaceshipMode)
        {
            SpaceshipController.SetIsSpaceShipMode(false);
            isShipDocked = true;
            Debug.Log(isShipDocked);
            // 1) 이 행성의 카메라 지정
            cameraSwitcher.SetPlanetCamera(planetCamera);
            // 2) 즉시 행성 시점으로 전환
            cameraSwitcher.ActivatePlanet(planetCamera);

            // 우주선 보관 및 다음 출격 위치 계산
            dockedShip = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
            CalculateNextDeparturePoint(dockedShip.transform.position);
            if (dockedShip) dockedShip.SetActive(false);
            UpdateAllUIStates();

            Debug.Log($"[DockingStation] 도킹 완료: {(planetCamera ? planetCamera.name : "null")}");
        }
    }

    public void DockShip(GameObject ship)
    {

    }
    
    private void UpdateAllUIStates()
    {
        // 1. 출격 가능 상태 업데이트 (기존 로직)
        if (canDepartState != null)
        {
            // isSpaceshipMode가 '아닐 때' 출격 가능
            canDepartState.Value = !isSpaceshipMode;
        }

        // 2. 비행 모드 상태 업데이트 (새로운 로직)
        if (isFlightModeState != null)
        {
            // isSpaceshipMode가 '맞을 때' 비행 모드
            isFlightModeState.Value = isSpaceshipMode;
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
