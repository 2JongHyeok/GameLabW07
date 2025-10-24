using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DockingStation : MonoBehaviour
{
    [Header("필수 연결 요소")]
    [SerializeField] private InventoryManger inventoryManger;
    [SerializeField] private CameraSwitcher cameraSwitcher;

    [Header("행성 카메라")]
    [Tooltip("이 도킹 스테이션이 속한 행성의 CinemachineCamera")]
    [SerializeField] private CinemachineCamera planetCamera;

    [Header("우주선 정보")]
    [SerializeField] private GameObject dockedShip;

    [Header("출격 설정")]
    [SerializeField] private float departureCircleRadius = 5f;

    [Header("UI 상태 알림")]
    [SerializeField] private BoolVariable canDepartState;
    [SerializeField] private BoolVariable isFlightModeState;


    private SpaceshipCargoSystem cargoSystem;
    [SerializeField] public bool isSpaceshipMode = false;
    private bool isSpaceshipInRange = false;

    private Vector3 nextDeparturePosition;
    private Quaternion nextDepartureRotation;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void Start()
    {
        if (dockedShip != null)
        {
            CalculateNextDeparturePoint(dockedShip.transform.position);
            dockedShip.SetActive(false);
        }
    

        UpdateAllUIStates();
    }

    private void Update()
    {
        // 출격 로직
        if (Input.GetKeyDown(KeyCode.F) && !isSpaceshipMode)
        {
            GameAnalyticsLogger.instance.LogPlayerExitBase();
            GameAnalyticsLogger.instance.isInSpaceShip = true;

            dockedShip.transform.SetPositionAndRotation(nextDeparturePosition, nextDepartureRotation);

            // 행성 → 우주선 모드 전환
            cameraSwitcher?.ToggleCameraMode();

            dockedShip.SetActive(true);
            isSpaceshipMode = true;

            UpdateAllUIStates();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Spaceship")) return;
        cameraSwitcher?.SetPlanetCamera(planetCamera);
        if (isSpaceshipMode)
        {
            cameraSwitcher?.ToggleCameraMode();
            isSpaceshipInRange = true;
            GameAnalyticsLogger.instance.LogPlayerEnterBase();
            GameAnalyticsLogger.instance.isInSpaceShip = false;

            if (cameraSwitcher && planetCamera)
                cameraSwitcher.SetPlanetCamera(planetCamera);

            // 우주선 → 행성 모드 전환
            cameraSwitcher?.ToggleCameraMode();
            isSpaceshipMode = false;

            UpdateAllUIStates();

            dockedShip = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
            cargoSystem = other.GetComponentInParent<SpaceshipCargoSystem>();

            if (cargoSystem != null && inventoryManger != null)
            {
                // cargoSystem.UnloadAllOres(inventoryManger);
            }

            CalculateNextDeparturePoint(dockedShip.transform.position);

            if (dockedShip)
                dockedShip.SetActive(false);

            cargoSystem = null;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Spaceship")) return;
        isSpaceshipInRange = false;
    }

    private void CalculateNextDeparturePoint(Vector3 basisPosition)
    {
        Vector3 direction = (basisPosition - transform.position).normalized;
        if (direction == Vector3.zero) direction = Vector3.up;

        nextDeparturePosition = transform.position + direction * departureCircleRadius;
        nextDepartureRotation = Quaternion.LookRotation(Vector3.forward, direction);
    }

    private void UpdateAllUIStates()
    {
        if (canDepartState != null)
            canDepartState.Value = !isSpaceshipMode;

        if (isFlightModeState != null)
            isFlightModeState.Value = isSpaceshipMode;
    }
}
