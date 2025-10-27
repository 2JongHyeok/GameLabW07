using TMPro;
using UnityEngine;

public class Planet2Manager : MonoBehaviour
{
    public static Planet2Manager instance;
    [SerializeField] private GameObject planet2;
    [SerializeField] private GameObject planet2Sheild;
    [SerializeField] private GameObject planet2DockingStation;
    [SerializeField] private GameObject planet2Hp;
    [SerializeField] private GameObject planet2Wave;
    [SerializeField] private SpaceshipCargoSystem cargoSystem;
    [SerializeField] private InventoryManger inventoryManger;
    [SerializeField] private DockingStation planet2DockingStationScript;
    bool isPlanetActive = false;
    bool isSpaceShipInRange = false;    // 우주선이 행성을 새로 생성할 수 있는 거리 내에 있는지.
    bool hasPlanet2Core = false;
    [SerializeField] private Weapon2 planet2Weapon;

    [Header("UI")]
    [SerializeField] private GameObject constructionPromptUI; // 추가: 건설 안내 UI

    public GameObject VisibleEffect;
    public TMP_Text cantPlanetBuildText;
    
    // hasPlanet2Core 읽기용 변수
    public bool HasPlanet2Core => hasPlanet2Core;
    
    // 행성 활성화 변수
    public bool IsPlanetActive => isPlanetActive;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        planet2.SetActive(false);
        planet2Sheild.SetActive(false);
        planet2DockingStation.SetActive(false);
        planet2Hp.SetActive(false);
        planet2Wave.SetActive(false);

        // 시작 시 건설 안내 UI가 있다면 비활성화합니다.
        if (constructionPromptUI != null)
            constructionPromptUI.SetActive(false);
    }

    private void Update()
    {
        // 행성이 이미 활성화되었다면, 더 이상 이 로직을 실행할 필요가 없습니다.
        if (isPlanetActive)
        {
            // 만약 UI가 켜져 있다면 확실히 꺼줍니다.
            if (constructionPromptUI != null && constructionPromptUI.activeSelf)
                constructionPromptUI.SetActive(false);
            return;
        }

        // [핵심 로직] 건설 가능 조건 = (범위 안에 있는가?) AND (코어를 가지고 있는가?)
        bool canConstruct = isSpaceShipInRange && hasPlanet2Core;

        // UI의 활성화 상태를 현재 건설 가능 상태와 항상 일치시킵니다.
        if (constructionPromptUI != null && constructionPromptUI.activeSelf != canConstruct)
            constructionPromptUI.SetActive(canConstruct);

        // 건설 실행: 건설 가능한 상태에서 F키를 누르면 행성을 활성화합니다.
        if (canConstruct && Input.GetKeyDown(KeyCode.F))
        {
            if (!Managers.Instance.planet1WaveManager.isAfterBossWave)
            {
                return;
            }

            isPlanetActive = true;
            planet2.SetActive(true); 
            planet2Sheild.SetActive(true);
            planet2DockingStation.SetActive(true);
            planet2Hp.SetActive(true);
            planet2Wave.SetActive(true);
            
            var cargoSystem = FindAnyObjectByType<SpaceshipCargoSystem>();
            cargoSystem.CallBreakConnectionForPlanetCore();
            
            GameAnalyticsLogger.instance.LogPlanetCoreActivated();
            VisibleEffect.SetActive(false);
            WaveManager.Instance?.NotifyPlanet2Activated();
            cargoSystem.UnloadAllOres(inventoryManger);
            gameObject.GetComponent<SpriteRenderer>().enabled = false;
            if (planet2Weapon != null)
            {
                planet2Weapon.ActivateWeapon();
            }
            SpaceshipController.SetIsSpaceShipMode(false);
            planet2DockingStationScript.SetShipDockedState(true);
        }
    }
   
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Spaceship")) return;
        isSpaceShipInRange = true;  
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Spaceship")) return;
        isSpaceShipInRange = false;
    }

    public void SetCoreStatus(bool val)
    {
        hasPlanet2Core = val;
    }
}
