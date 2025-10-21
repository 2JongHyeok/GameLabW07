using UnityEngine;

/// <summary>
/// BoolVariable의 상태를 감시하여, 그 값의 '반대'로 UI 요소를 활성화/비활성화합니다.
/// (예: BoolVariable.Value가 true이면, UI를 끈다)
/// </summary>
public class InvertedContextualUIController : MonoBehaviour
{
    [Header("감시할 상태")]
    [Tooltip("이 UI가 꺼져야 할 조건을 담고 있는 BoolVariable 에셋")]
    [SerializeField] private BoolVariable stateToWatch;

    [Header("제어할 UI 요소")]
    [Tooltip("실제로 켜고 끌 게임 오브젝트 (보통 자기 자신)")]
    [SerializeField] private GameObject uiElement;

    // 초기 상태는 다른 스크립트(ContextualUIController)가 관리하도록 
    // Awake()에서 상태를 초기화하지 않습니다.

    void Start()
    {
        // 시작할 때 초기 상태를 한번 반영해줍니다.
        UpdateVisibility();
    }

    void Update()
    {
        // 매 프레임 상태를 확인하여 UI의 활성화 여부를 결정합니다.
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (stateToWatch == null || uiElement == null) return;

        // 여기가 핵심!
        // shouldBeActive 앞에 '!' (NOT 연산자)를 붙여서 신호를 반대로 적용합니다.
        bool shouldBeActive = !stateToWatch.Value; 

        // 현재 UI의 활성화 상태와 달라야 할 때만 변경하여 성능을 아낍니다.
        if (uiElement.activeSelf != shouldBeActive)
        {
            uiElement.SetActive(shouldBeActive);
        }
    }
}