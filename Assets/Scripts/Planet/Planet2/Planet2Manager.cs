using UnityEngine;

public class Planet2Manager : MonoBehaviour
{
    public static Planet2Manager instance;
    [SerializeField] private GameObject planet2;
    bool isPlanetActive = false;
    bool isSpaceShipInRange = false;    // 우주선이 행성을 새로 생성할 수 있는 거리 내에 있는지.
    bool hasPlanet2Core = false;

    void Start()
    {
        planet2.SetActive(false);
    }

    private void Update()
    {
        if (isPlanetActive) return;
        if (Input.GetKeyDown(KeyCode.F) && isSpaceShipInRange && hasPlanet2Core)
        {
            isPlanetActive = true;
            planet2.SetActive(true);
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

    public void SetCoreStatus(bool val) // TODO : 효재한테. Core먹었을때랑 Core가 연결 끊어졌을때. 어느 함수가 불리는지 물어보고. 해당 위치에 삽입하기.
    {
        hasPlanet2Core = val;
    }
}
