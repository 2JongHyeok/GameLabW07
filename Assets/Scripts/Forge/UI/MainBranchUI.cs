using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainBranchUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI branchNameText;
    [SerializeField] private Image branchIcon;

    private MainBranchSO mainBranchSO;

    public void Initialize(MainBranchSO branchSO)
    {
        mainBranchSO = branchSO;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (mainBranchSO == null) return;

        // 브랜치 이름 표시
        if (branchNameText != null)
        {
            branchNameText.text = mainBranchSO.branchType.ToString();
        }

        // 브랜치 아이콘 표시 (있다면)
        // if (branchIcon != null && mainBranchSO.icon != null)
        // {
        //     branchIcon.sprite = mainBranchSO.icon;
        // }
    }
}
