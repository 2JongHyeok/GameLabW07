using UnityEngine;

public class SpaceshipLocationManager : MonoBehaviour
{
    [Header("우주선 내부 오브젝트")]
    public GameObject spaceshipInterior;  // 내부 오브젝트 (렌더링 제어 가능)

    [Header("우주선 내부 문 오브젝트")]
    public GameObject door;  // 🚪 문 오브젝트 추가

    [Header("우주선 조종석 오브젝트")]
    public GameObject cockpit; // 🛸 콕핏(조종석) 오브젝트 추가

    [Header("플레이어 탑승 위치 (우주선 기준 오프셋)")]
    public Vector2 enterOffset;  // 우주선 기준 (x, y) 좌표로 설정

    [Header("플레이어 내리는 위치 오프셋")]
    public Vector2 exitOffset = new Vector2(0, -1.5f);  // (x, y) 기준 오프셋

    private SpriteRenderer interiorRenderer;

    private void Start()
    {
        if (spaceshipInterior != null)
        {
            interiorRenderer = spaceshipInterior.GetComponent<SpriteRenderer>();
            if (interiorRenderer != null)
            {
                interiorRenderer.enabled = false; 
            }
        }
    }

    public GameObject GetDoor()
    {
        return door;
    }

 
    public GameObject GetInterior()
    {
        return spaceshipInterior;
    }

  
    public GameObject GetCockpit()
    {
        return cockpit;
    }

    public Vector2 GetEnterPosition()
    {
        return transform.position + (Vector3)(transform.TransformDirection(enterOffset));
    }

  
    public Vector2 GetExitPosition()
    {
        return transform.position + (Vector3)(transform.TransformDirection(exitOffset));
    }

   
    public void SetInteriorVisible(bool visible)
    {
        if (interiorRenderer != null)
        {
            interiorRenderer.enabled = visible;
        }
    }

    private GameObject playerInCockpit;

    // 플레이어 전달용 메서드
    public void SetPlayerInCockpit(GameObject player)
    {
        playerInCockpit = player;
        // 필요 시 추가 초기화 작업 수행 가능
    }

    public void ExitCockpit()
    {
        if (playerInCockpit == null)
        {
            return;
        }

        // 내부 렌더링 복원
        SetInteriorVisible(true);

        // 플레이어 위치 재설정: cockpit 오브젝트의 위치(자식의 offset이 0인 곳) 사용
        if (cockpit != null)
        {
            playerInCockpit.transform.position = cockpit.transform.position;
        }
        else
        {
            Vector2 exitPosition = GetExitPosition();
            playerInCockpit.transform.position = exitPosition;
        }

        // 플레이어 활성화
        playerInCockpit.SetActive(true);

        // 우주선의 물리 상태(Rigidbody2D)를 플레이어에 적용 (속도, 각속도 전달)
        Rigidbody2D spaceshipRb = GetComponent<Rigidbody2D>();
        Rigidbody2D playerRb = playerInCockpit.GetComponent<Rigidbody2D>();
        if (spaceshipRb != null && playerRb != null)
        {
            playerRb.linearVelocity = spaceshipRb.linearVelocity;
            playerRb.angularVelocity = spaceshipRb.angularVelocity;
        }
        else
        {
        }

        // spaceshipmovement 컴포넌트를 비활성화
        SpaceshipMovement shipMovement = GetComponent<SpaceshipMovement>();
        if (shipMovement != null)
        {
            shipMovement.enabled = false;
        }
        else
        {
        }

        // 메인 카메라를 다시 플레이어의 자식으로 편입시키기
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.SetParent(playerInCockpit.transform, true);
            mainCam.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
        }

        // 저장된 플레이어 정보 초기화 (필요에 따라)
        playerInCockpit = null;
    }

}