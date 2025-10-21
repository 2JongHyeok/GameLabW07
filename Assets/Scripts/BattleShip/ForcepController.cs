// using UnityEngine;
// using System.Collections.Generic;

// [RequireComponent(typeof(Rigidbody2D))]
// [RequireComponent(typeof(DistanceJoint2D))]
// public class ForcepController : MonoBehaviour
// {
//     [HideInInspector] // 인스펙터 창에서는 숨깁니다. WinchController가 설정해줄 것이기 때문입니다.
//     public InventoryManger inventoryManger;
//     [Header("원점(0,0) 중력 설정")]
//     public float gravityAcceleration = 9.81f;
//     public float minRadius = 0.1f;
//     public bool useInverseSquare = true;

//     [Header("좌우 이동")]
//     public float horizontalForce = 50f;

//     // --- 시각 효과 ---
//     private SpriteRenderer spriteRenderer;
//     private Color originalColor;
//     private Color detectColor = Color.yellow;
//     private Color grabColor = Color.red;

//     // --- 내부 변수 ---
//     private Rigidbody2D rb;
//     private List<Collider2D> detectedOres = new List<Collider2D>();
//     private List<GameObject> grabbedOres = new List<GameObject>();
//     private bool isGrabbing = false;
//     public bool isGrounded = false;

//     void Start()
//     {
//         rb = GetComponent<Rigidbody2D>();
//         rb.gravityScale = 0f;
//         spriteRenderer = GetComponent<SpriteRenderer>();
//         if (spriteRenderer != null) originalColor = spriteRenderer.color;
//     }
    
//     void Update()
//     {
//         // 스페이스바를 누르면 잡거나 놓습니다.
//         if (Input.GetKeyDown(KeyCode.Space))
//         {
//             if (isGrabbing)
//             {
//                 // 이미 무언가를 잡고 있다면 -> 놓아줍니다.
//                 ReleaseOres();
//             }
//             else if (detectedOres.Count > 0)
//             {
//                 // 잡고 있지 않고, 감지된 광물이 있다면 -> 잡습니다.
//                 GrabOres();
//             }
//         }
//     }

//     void FixedUpdate()
//     {
//         // 잡고 있지 않을 때만 중력 및 이동 처리
//         if (!isGrabbing)
//         {
//             ApplyCentralGravity();
//             HandleHorizontalMovement();
//         }
//     }

//     // [추가] A/D 키를 이용한 좌우 이동 로직
//     private void HandleHorizontalMovement()
//     {
//         if (Input.GetKey(KeyCode.A))
//         {
//             rb.AddForce(Vector2.left * horizontalForce * Time.fixedDeltaTime, ForceMode2D.Impulse);
//         }

//         if (Input.GetKey(KeyCode.D))
//         {
//             rb.AddForce(Vector2.right * horizontalForce * Time.fixedDeltaTime, ForceMode2D.Impulse);
//         }
//     }
// // [추가] 다른 Collider와 충돌을 시작했을 때 호출되는 함수
//     private void OnCollisionEnter2D(Collision2D collision)
//     {
//         // 충돌한 오브젝트의 태그가 "Ground"라면
//         if (collision.gameObject.CompareTag("Ground"))
//         {
//             isGrounded = true;
//         }
//     }

//     // [추가] 충돌하고 있던 Collider와 떨어졌을 때 호출되는 함수
//     private void OnCollisionExit2D(Collision2D collision)
//     {
//         // 떨어진 오브젝트의 태그가 "Ground"라면
//         if (collision.gameObject.CompareTag("Ground"))
//         {
//             isGrounded = false;
//         }
//     }

//     // 트리거 영역에 ORE가 들어왔을 때
//     void OnTriggerEnter2D(Collider2D other)
//     {
//         if (other.gameObject.layer == LayerMask.NameToLayer("Ore"))
//         {
//             if (!detectedOres.Contains(other))
//             {
//                 detectedOres.Add(other);

//                 // 시각적 피드백: 잡을 수 있는 대상이 있으면 색 변경
//                 if (spriteRenderer != null && !isGrabbing)
//                 {
//                     spriteRenderer.color = detectColor;
//                 }
//             }
//         }
//     }

//     // 트리거 영역에서 ORE가 나갔을 때
//     void OnTriggerExit2D(Collider2D other)
//     {
//         if (other.gameObject.layer == LayerMask.NameToLayer("Ore"))
//         {
//             Debug.Log(other.name + " 광물이 감지 영역을 벗어났습니다.");
//             detectedOres.Remove(other);

//             // 시각적 피드백: 잡을 대상이 없으면 원래 색으로 복귀
//             if (spriteRenderer != null && detectedOres.Count == 0 && !isGrabbing)
//             {
//                 spriteRenderer.color = originalColor;
//             }
//         }
//     }

//     private void GrabOres()
//     {
//         isGrabbing = true;
//         rb.linearDamping = 2f;
//         rb.angularDamping = 2f;

//         if (spriteRenderer != null) spriteRenderer.color = grabColor;

//         // [핵심] 1. 감지된 광물 리스트의 "복사본"을 만듭니다.
//         List<Collider2D> oresToGrab = new List<Collider2D>(detectedOres);

//         // [핵심] 2. 원본 감지 리스트는 즉시 비워서, 더 이상 트리거 이벤트의 영향을 받지 않게 합니다.
//         detectedOres.Clear();

//         // [핵심] 3. 이제 원본이 아닌 "복사본" 리스트를 순회하며 안전하게 처리합니다.
//         foreach (var oreCollider in oresToGrab)
//         {
//             // oreCollider가 null이거나 파괴된 경우를 대비한 안전장치
//             if (oreCollider == null) continue;

//             GameObject oreObject = oreCollider.gameObject;
//             if (oreObject != null)
//             {
//                 // 콜라이더 비활성화
//                 oreCollider.enabled = false;

//                 Rigidbody2D oreRb = oreObject.GetComponent<Rigidbody2D>();
//                 if (oreRb != null)
//                 {
//                     oreRb.isKinematic = true;
//                     oreRb.linearVelocity = Vector2.zero;
//                 }

//                 oreObject.transform.SetParent(this.transform);

//                 ItemDrop oreGravity = oreObject.GetComponent<ItemDrop>();
//                 if (oreGravity != null) oreGravity.enabled = false;

//                 // 최종적으로 잡은 아이템 목록에 추가
//                 grabbedOres.Add(oreObject);
//             }
//         }
//     }
//     private void ReleaseOres()
//     {
//         isGrabbing = false;
//         rb.linearDamping = 0f;
//         rb.angularDamping = 0.05f;

//         if (spriteRenderer != null) spriteRenderer.color = originalColor;

//         foreach (var oreObject in grabbedOres)
//         {
//             if (oreObject != null)
//             {
//                 oreObject.transform.SetParent(null);

//                 // [수정] 해당 오브젝트의 "모든" 콜라이더를 찾아서 켭니다.
//                 Collider2D[] allColliders = oreObject.GetComponents<Collider2D>();
//                 foreach (var col in allColliders)
//                 {
//                     col.enabled = true;
//                 }

//                 Rigidbody2D oreRb = oreObject.GetComponent<Rigidbody2D>();
//                 if (oreRb != null)
//                 {
//                     oreRb.isKinematic = false;
//                 }

//                 ItemDrop oreGravity = oreObject.GetComponent<ItemDrop>();
//                 if (oreGravity != null) oreGravity.enabled = true;
//             }
//         }
//         grabbedOres.Clear();
//     }

//     public void ProcessAndDestroy()
//     {
//          foreach (var ore in grabbedOres) { AddToInventory(ore); Destroy(ore); } Destroy(gameObject); 
//     }
//     private void AddToInventory(GameObject oreObject)
//     {
//         // 1. inventoryManger 참조가 제대로 설정되었는지 확인합니다.
//         if (inventoryManger == null)
//         {
//             Debug.LogError("Forcep에 InventoryManger가 연결되지 않았습니다! WinchController를 확인하세요.");
//             return;
//         }

//         // 2. 전달받은 광물 오브젝트에서 'Ore' 컴포넌트(신분증)를 찾습니다.
//         Ore oreInfo = oreObject.GetComponent<Ore>();

//         // 3. 신분증이 있는지 확인합니다.
//         if (oreInfo != null)
//         {
//             // 4. 저장해둔 inventoryManger 참조를 이용해 AddOre 함수를 호출합니다.
//             inventoryManger.AddOre(oreInfo.oreType, oreInfo.amount);
//             Debug.Log(oreInfo.oreType + " " + oreInfo.amount + "개를 인벤토리에 추가했습니다.");
//         }
//         else
//         {
//             Debug.LogWarning(oreObject.name + "에 Ore.cs 컴포넌트가 없어서 인벤토리에 추가할 수 없습니다!");
//         }
//     }
//     private void ApplyCentralGravity()
//     {
//         Vector2 toOrigin = -((Vector2)transform.position);
//         float dist = toOrigin.magnitude;
//           if (dist < 0.001f) return; Vector2 dir = toOrigin.normalized;
//         float accel = gravityAcceleration;
//             if (useInverseSquare) {
//             float r = Mathf.Max(dist, minRadius);
//             accel = gravityAcceleration / (r * r);
//               } Vector2 force = dir * (rb.mass * accel);
//         rb.AddForce(force, ForceMode2D.Force); 
//     }
// }