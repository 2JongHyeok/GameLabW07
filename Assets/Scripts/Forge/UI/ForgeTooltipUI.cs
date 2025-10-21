using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ForgeTooltipUI : MonoBehaviour
{
    [Header("Basic Info")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    
    [Header("Current Resources (보유)")]
    [SerializeField] private TextMeshProUGUI currentCoalText;
    [SerializeField] private TextMeshProUGUI currentIronText;
    [SerializeField] private TextMeshProUGUI currentGoldText;
    [SerializeField] private TextMeshProUGUI currentDiamondText;
    
    [Header("Cost Resources (소모)")]
    [SerializeField] private TextMeshProUGUI coalCostText;
    [SerializeField] private TextMeshProUGUI ironCostText;
    [SerializeField] private TextMeshProUGUI goldCostText;
    [SerializeField] private TextMeshProUGUI diamondCostText;
    
    [Header("Charge Gauge")]
    [SerializeField] private Slider chargeSlider; // 차징 슬라이더
    
    [Header("Visual Feedback")]
    [SerializeField] private Image backgroundImage; // 툴팁 배경 이미지
    [SerializeField] private Color normalColor = Color.white; // 정상 색상
    [SerializeField] private Color unaffordableColor = new Color(1f, 0.7f, 0.7f, 1f); // 불가능 색상 (연한 빨강)
    
    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(10, 10); // 마우스 커서로부터의 오프셋

    private RectTransform rectTransform;
    private Canvas canvas;
    private InventoryManger inventoryManger;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        // InventoryManger 찾기
        inventoryManger = FindFirstObjectByType<InventoryManger>();
        if (inventoryManger == null)
        {
        }
        
        // 차징 슬라이더 설정
        if (chargeSlider != null)
        {
            chargeSlider.minValue = 0f;
            chargeSlider.maxValue = 1f;
            chargeSlider.value = 0f;
        }
        
        // Tooltip의 모든 Graphic 컴포넌트의 Raycast Target 끄기 (깜빡임 방지)
        DisableRaycastTargets();
        
        Hide();
    }
    
    private void DisableRaycastTargets()
    {
        // 자신과 모든 자식의 Graphic 컴포넌트 찾기
        UnityEngine.UI.Graphic[] graphics = GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        foreach (var graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
    }

    public void Show(BaseForgeSO forgeSO, Vector2 mousePosition, bool canPurchase = true)
    {
        if (forgeSO == null) return;

        gameObject.SetActive(true);
        UpdateContent(forgeSO, canPurchase);
        
        // Canvas 업데이트
        Canvas.ForceUpdateCanvases();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        
        // 차징 슬라이더 초기화
        if (chargeSlider != null)
        {
            chargeSlider.value = 0f;
        }
    }
    
    // 차징 게이지 업데이트
    public void UpdateChargeGauge(float fillAmount)
    {
        if (chargeSlider != null)
        {
            chargeSlider.value = fillAmount;
        }
    }
    
    // 외부에서 콘텐츠만 갱신 (강화 완료 후 자원 변경 반영용)
    public void RefreshContent(BaseForgeSO forgeSO, bool canPurchase = true)
    {
        if (forgeSO != null && gameObject.activeSelf)
        {
            UpdateContent(forgeSO, canPurchase);
        }
    }

    private void UpdateContent(BaseForgeSO forgeSO, bool canPurchase = true)
    {
        // 강화 명
        if (nameText != null)
            nameText.text = forgeSO.upgradeName;
        
        // 강화 설명
        if (descriptionText != null)
            descriptionText.text = forgeSO.upgradeDescription;
        
        // === 현재 자원량 (보유) ===
        if (inventoryManger != null)
        {
            if (currentCoalText != null)
                currentCoalText.text = $"{inventoryManger.OreList[(int)OreType.Coal]}";
            
            if (currentIronText != null)
                currentIronText.text = $"{inventoryManger.OreList[(int)OreType.Iron]}";
            
            if (currentGoldText != null)
                currentGoldText.text = $"{inventoryManger.OreList[(int)OreType.Gold]}";
            
            if (currentDiamondText != null)
                currentDiamondText.text = $"{inventoryManger.OreList[(int)OreType.Diamond]}";
        }
        
        // === 자원 소모량 (비용) ===
        // 비용이 충분한지 체크하여 색상 변경: 충분하면 검은색, 부족하면 빨간색
        bool hasEnoughCoal = inventoryManger != null && inventoryManger.OreList[(int)OreType.Coal] >= forgeSO.coalCost;
        bool hasEnoughIron = inventoryManger != null && inventoryManger.OreList[(int)OreType.Iron] >= forgeSO.ironCost;
        bool hasEnoughGold = inventoryManger != null && inventoryManger.OreList[(int)OreType.Gold] >= forgeSO.goldCost;
        bool hasEnoughDiamond = inventoryManger != null && inventoryManger.OreList[(int)OreType.Diamond] >= forgeSO.diamondCost;
        
        // 모든 자원이 충분한지 체크
        bool canAfford = hasEnoughCoal && hasEnoughIron && hasEnoughGold && hasEnoughDiamond;
        
        // 배경 색상 변경 (구매 불가능하거나 자원 부족하면 빨간 배경)
        if (backgroundImage != null)
        {
            backgroundImage.color = (canPurchase && canAfford) ? normalColor : unaffordableColor;
        }
        
        if (coalCostText != null)
        {
            coalCostText.text = $"{forgeSO.coalCost}";
            coalCostText.color = hasEnoughCoal ? Color.black : Color.red;
        }
        
        if (ironCostText != null)
        {
            ironCostText.text = $"{forgeSO.ironCost}";
            ironCostText.color = hasEnoughIron ? Color.black : Color.red;
        }
        
        if (goldCostText != null)
        {
            goldCostText.text = $"{forgeSO.goldCost}";
            goldCostText.color = hasEnoughGold ? Color.black : Color.red;
        }
        
        if (diamondCostText != null)
        {
            diamondCostText.text = $"{forgeSO.diamondCost}";
            diamondCostText.color = hasEnoughDiamond ? Color.black : Color.red;
        }
    }
}
