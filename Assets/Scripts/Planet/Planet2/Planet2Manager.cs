using UnityEngine;

public class Planet2Manager : MonoBehaviour
{
    public static Planet2Manager instance;
    [SerializeField] private GameObject planet2;
    [SerializeField] private GameObject planet2Sheild;
    [SerializeField] private GameObject planet2DockingStation;
    bool isPlanetActive = false;
    bool isSpaceShipInRange = false;    // 우주선이 행성을 새로 생성할 수 있는 거리 내에 있는지.
    bool hasPlanet2Core = false;

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
    }

    private void Update()
    {
        if (isPlanetActive) return;
        if (Input.GetKeyDown(KeyCode.F) && isSpaceShipInRange && hasPlanet2Core)
        {
            isPlanetActive = true;
            planet2.SetActive(true); 
            planet2Sheild.SetActive(true);
            planet2DockingStation.SetActive(true);
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
