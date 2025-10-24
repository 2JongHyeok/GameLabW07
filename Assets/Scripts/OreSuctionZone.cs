using System.Collections;
using UnityEngine;

public class OreSuctionZone : MonoBehaviour
{
    [Header("흡입 설정")]
    [Tooltip("광물이 빨려들어갈 목표 지점 (예: Vector2.zero 또는 다른 Transform.position)")]
    [SerializeField] private Vector2 suctionTarget = Vector2.zero;

    [Tooltip("흡입 속도")]
    [SerializeField] private float suctionSpeed = 5f;

    [Tooltip("흡입 중 회전 속도(선택)")]
    [SerializeField] private float spinSpeed = 180f;

    [Tooltip("흡입 완료 시 파괴할지 여부")]
    [SerializeField] private bool destroyOnComplete = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 오브젝트가 "Ore" 레이어에 있는지 확인
        if (other.gameObject.layer != LayerMask.NameToLayer("Ore"))
            return;
        
        GameObject oreObj = other.gameObject;
        
        
        // [개선] 씬에 있는 우주선 카고 시스템을 찾습니다.
        var cargoSystem = FindAnyObjectByType<SpaceshipCargoSystem>();
        if (cargoSystem != null)
        {
            // [개선] 위험한 리플렉션 대신, 새로 만든 공개 함수를 호출하여 안전하게 연결 해제를 요청합니다.
            cargoSystem.BreakConnectionForOre(oreObj);
        }

        if (oreObj.tag == "PlanetCore")
        {
            oreObj.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            return;
        }
        
        
        // 만약 광물이 다른 곳에서 먼저 파괴되었을 수 있으니, 확인 후 코루틴을 시작합니다.
        if (oreObj != null)
        {    
            // 광물이 (0,0)으로 부드럽게 끌려가도록 코루틴 시작
            StartCoroutine(SuckToCenter(oreObj));
        }
    }

    private IEnumerator SuckToCenter(GameObject ore)
    {
        Rigidbody2D rb = ore.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0;       // 중력 영향 제거
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
        }

        while (ore != null)
        {
            Vector2 pos = ore.transform.position;
            Vector2 dir = (suctionTarget - pos);
            float dist = dir.magnitude;

            if (dist < 0.1f)
                break;

            dir.Normalize();
            ore.transform.position += (Vector3)(dir * suctionSpeed * Time.deltaTime);
            ore.transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);

            yield return null;
        }

        // 목표 도달 시 처리
        if (destroyOnComplete && ore != null)
        {
 
            Object.Destroy(ore);
                   
        }
    }
}
