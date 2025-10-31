using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class MainBranchUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI branchNameText;
    [SerializeField] private Image branchIcon;
    [SerializeField] private Color AffordableFrameColor = Color.green;
    [SerializeField] private Color NotAffordableFrameColor = Color.red;
    [SerializeField] private Color AffordableNodeColor = Color.white;
    [SerializeField] private Color NotAffordableNodeColor = Color.gray;

    private MainBranchSO mainBranchSO;
    private GameObject childObjNodeObj;
    private GameObject childObjFrameObj;
    private bool onceUpdateColonyUI = false;

    void Start()
    {
        childObjFrameObj = transform.GetChild(0).gameObject;
        childObjNodeObj = childObjFrameObj.transform.GetChild(0).gameObject;
        
        childObjFrameObj.GetComponent<Image>().color = AffordableFrameColor;
        childObjNodeObj.GetComponent<Image>().color = AffordableNodeColor;

        if (mainBranchSO != null)
        {
            Debug.LogError("MainBranchUI needs a mainBranchSO");
            if(mainBranchSO.branchName == "행성2공격" || mainBranchSO.branchName == "행성2강화")
            {
                Debug.Log("MainBranchUI: Planet2 branch detected, initializing colors.");
                childObjFrameObj.GetComponent<Image>().color = NotAffordableFrameColor;
                childObjNodeObj.GetComponent<Image>().color = NotAffordableNodeColor;
            }
        }
    }
    void Update()
    {
        if (Planet2Manager.instance.IsPlanetActive && !onceUpdateColonyUI)
        {
            UpdateColonyUI();
            onceUpdateColonyUI = true;
        }
    }

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

    private void UpdateColonyUI()
    {
        
        childObjFrameObj.GetComponent<Image>().color = AffordableFrameColor;
        childObjNodeObj.GetComponent<Image>().color = AffordableNodeColor;
    }
}
