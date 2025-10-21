// ContextualUIController.cs
using UnityEngine;

public class ContextualUIController : MonoBehaviour
{
    [Header("감시할 상태")]
    [Tooltip("이 UI가 켜져야 할 조건을 담고 있는 BoolVariable 에셋")]
    [SerializeField] private BoolVariable stateToWatch;

    [Header("제어할 UI 요소")]
    [Tooltip("실제로 켜고 끌 게임 오브젝트 (보통 자기 자신)")]
    [SerializeField] private GameObject uiElement;


    private void Awake()
    {
        // 게임이 시작되면, 다른 어떤 스크립트보다 먼저
        // 내가 감시하는 상태의 값을 false로 확실하게 초기화합니다.
        if (stateToWatch != null)
        {
            stateToWatch.Value = false;
        }
    }

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

        // 감시해야 할 상태(Value)와 현재 UI의 활성화 상태가 다를 때만 변경
        bool shouldBeActive = stateToWatch.Value;
        if (uiElement.activeSelf != shouldBeActive)
        {
            uiElement.SetActive(shouldBeActive);
        }
    }
}