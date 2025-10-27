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
            string branchName = mainBranchSO.branchType.ToString();
            // 특정 브랜치 타입의 경우 이름을 커스텀하게 변경
            if (mainBranchSO.branchType == MainBranchType.PlanetAttacks)
            {
                branchName = "Planet\nAttacks";
            }
            else if (mainBranchSO.branchType == MainBranchType.ColonyAttacks)
            {
                branchName = "Colony\nAttacks";
            }
            
            branchNameText.text = branchName;
        }

        // 브랜치 아이콘 표시 (있다면)
        // if (branchIcon != null && mainBranchSO.icon != null)
        // {
        //     branchIcon.sprite = mainBranchSO.icon;
        // }
    }
}
