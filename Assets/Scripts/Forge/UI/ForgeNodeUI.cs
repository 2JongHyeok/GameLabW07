using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;
using Unity.VisualScripting;

public class ForgeNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI References")]
    [SerializeField] private Image nodeIcon;
    [SerializeField] private Slider chargeSlider; // 차징 게이지 슬라이더 (optional)
    [SerializeField] private CanvasGroup canvasGroup; // 잠금 상태 표시용 (optional)
    [SerializeField] private TextMeshProUGUI upgradeNameText; // 업그레이드 이름 텍스트
    
    [Header("Ore Cost Text")]
    [SerializeField] private TextMeshProUGUI coalText;
    [SerializeField] private TextMeshProUGUI ironText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI diamondText;
    
    [Header("Charge Settings")]
    [SerializeField] private float chargeTime = 0.5f; // 차징 완료 시간 (초)
    [SerializeField] private float lockedAlpha = 0.5f; // 잠긴 상태의 투명도
    
    [Header("Affordability Colors")]
    [SerializeField] private Color affordableTextColor = Color.black; // 구매 가능 시 텍스트 색
    [SerializeField] private Color unaffordableTextColor = Color.red; // 구매 불가능 시 텍스트 색
    [SerializeField] private Color alreadyOwnedTextColor = Color.white; // 이미 소유한 경우 텍스트 색

    private BaseForgeSO forgeSO;
    private SubBranchType subBranchType; // 이 노드가 속한 서브브랜치
    private int forgeIndexInSameId; // 같은 ForgeId 내에서 몇 번째인지 (0부터 시작)
    private Action<BaseForgeSO> onChargeCompleteCallback;
    private ForgeManager forgeManger;
    private bool isLocked = false; // 잠금 상태
    public bool canPurchase = false; // 구매 가능 상태
    public bool canAfford = false;
    
    // Tooltip 관련
    private static ForgeTooltipUI tooltipUI;
    
    // 차징 관련
    private bool isCharging = false;
    private float currentChargeTime = 0f;
    private Coroutine chargeCoroutine;
    private bool isHovering = false; // 마우스 호버 상태
    
    [Tooltip("변경할 색상")]
    public Color targetColor = Color.green;
    public Color targetNodeColor = Color.white;

    [Header("머티리얼 로드 설정")]
    [Tooltip("Assets/Resources/ 폴더 기준의 머티리얼 경로.\n(예: MyMaterials/Arrow_Green)")]
    [SerializeField] private string materialPathInResources = "Text 0";
    
    private Material loadedMaterial;
    
    public bool isOwned = false;

    void Awake()
    {
        // 1. Resources 폴더에서 머티리얼을 불러옵니다.
        //    (경로를 사용하며, 파일 확장자 .mat는 제외합니다.)
        if (!string.IsNullOrEmpty(materialPathInResources))
        {
            loadedMaterial = Resources.Load<Material>(materialPathInResources);

            if (loadedMaterial == null)
            {
                Debug.LogError($"[Resources] 머티리얼 로드 실패! 경로를 확인하세요: Assets/Resources/{materialPathInResources}");
            }
        }
    }
    
    public void Initialize(BaseForgeSO forgeData, SubBranchType subBranch, int indexInSameId, ForgeManager manager, Action<BaseForgeSO> onChargeComplete)
    {
        forgeSO = forgeData;
        subBranchType = subBranch;
        forgeIndexInSameId = indexInSameId;
        forgeManger = manager;
        onChargeCompleteCallback = onChargeComplete;

        // 차징 슬라이더 초기화
        if (chargeSlider != null)
        {
            chargeSlider.minValue = 0f;
            chargeSlider.maxValue = 1f;
            chargeSlider.value = 0f;
            chargeSlider.gameObject.SetActive(false);
        }
        
        // CanvasGroup 자동 추가
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        UpdateUI();
        UpdateLockState();
    }
    
    private void UpdateUI()
    {
        if (forgeSO == null) return;

        // 업그레이드 이름 표시
        if (upgradeNameText != null)
        {
            upgradeNameText.text = forgeSO.upgradeName;
        }

        // 아이콘 표시 (있다면)
        // if (nodeIcon != null && forgeSO.icon != null)
        // {
        //     nodeIcon.sprite = forgeSO.icon;
        // }
        
        // 광석 비용 표시
        if (coalText != null)
            coalText.text = forgeSO.coalCost.ToString();
        
        if (ironText != null)
            ironText.text = forgeSO.ironCost.ToString();
        
        if (goldText != null)
            goldText.text = forgeSO.goldCost.ToString();
        
        if (diamondText != null)
            diamondText.text = forgeSO.diamondCost.ToString();
    }

    // 외부에서 버튼 활성화/비활성화
    public void SetInteractable(bool interactable)
    {
        enabled = interactable;
    }

    // 노드 정보 업데이트 (예: 비용이나 상태가 변경되었을 때)
    public void RefreshUI()
    {
        UpdateUI();
        UpdateLockState();
    }
    
    // 구매 가능 여부에 따라 텍스트 색상 업데이트
    public void UpdateAffordabilityTextColor()
    {
        if (forgeSO == null) return;
        
        // 인벤토리 매니저 가져오기
        InventoryManger inventoryManger = Managers.Instance?.inventory;
        if (inventoryManger == null) return;
        
        // 자원이 충분한지 체크
        bool hasEnoughCoal = inventoryManger.OreList[(int)OreType.Coal] >= forgeSO.coalCost;
        bool hasEnoughIron = inventoryManger.OreList[(int)OreType.Iron] >= forgeSO.ironCost;
        bool hasEnoughGold = inventoryManger.OreList[(int)OreType.Gold] >= forgeSO.goldCost;
        bool hasEnoughDiamond = inventoryManger.OreList[(int)OreType.Diamond] >= forgeSO.diamondCost;
        
        // 모든 자원이 충분하고 잠겨있지 않으면 구매 가능
        this.canAfford = !isLocked && hasEnoughCoal && hasEnoughIron && hasEnoughGold && hasEnoughDiamond;
        
        // 텍스트 색상 변경
        Color textColor = canAfford ? affordableTextColor : unaffordableTextColor;
        if (canAfford)
        {
            ChangeNodeFrameColor();
        }
        if(isOwned)
        {
            textColor = alreadyOwnedTextColor;
            canvasGroup.alpha = 1f;
        }
        
        if (coalText != null)
            coalText.color = textColor;
        
        if (ironText != null)
            ironText.color = textColor;
        
        if (goldText != null)
            goldText.color = textColor;
        
        if (diamondText != null)
            diamondText.color = textColor;
        
        if (upgradeNameText != null)
            upgradeNameText.color = textColor;
    }

    private void OnDisable()
    {
        if (isCharging)
        {
            StopCharging();
        }
    }
    // 잠금 상태 업데이트
    private void UpdateLockState()
    {
        if (forgeSO == null || forgeManger == null)
        {
            isLocked = true;
            SetVisualLocked(true);
            return;
        }
        
        // 재사용 가능한 노드인지 확인
        bool isReusable = forgeSO is IReuse reuse && reuse.IsReusable;
        
        if (isReusable)
        {
            if (forgeSO.forgeId == ForgeId.Planet2HpRegenAmount)
            {
                // 행성 2 체력 재생 노드는 행성 2가 활성화되어야 구매 가능
                if (Planet2Manager.instance != null && !Planet2Manager.instance.IsPlanetActive)
                {
                    isLocked = true;
                    SetVisualLocked(true);
                    return;
                }
            }
            // 재사용 가능한 노드는 항상 구매 가능 (비용만 체크)
            isLocked = false;
        }
        else
        {
            // 일반 노드는 구매 가능 여부 확인
            canPurchase = forgeManger.CanPurchaseForge(subBranchType, forgeSO.forgeId, forgeIndexInSameId);
            isLocked = !canPurchase;
        }
        
        SetVisualLocked(isLocked);
        
        /*// 특정 노드는 특수조건 적용 - 행성 2 관련
        if(forgeSO.forgeId == ForgeId.Planet2CoreMaxHp || forgeSO.forgeId == ForgeId.Planet2ShieldMaxHp
                                                       || forgeSO.forgeId == ForgeId.Planet2HpRegenAmount || forgeSO.forgeId == ForgeId.Planet2ShieldRegenSpeed
                                                       || forgeSO.forgeId == ForgeId.Planet2MainCannonUpgrade || forgeSO.forgeId == ForgeId.Planet2MainCannonBulletNumber)
        {
            
            if (Planet2Manager.instance.IsPlanetActive)
            {
                isLocked = false;
                SetVisualLocked(false);
            }
            else
            {
                isLocked = true;
                SetVisualLocked(true);
            }
        }*/
    }
    
    // 시각적 잠금 상태 설정
    private void SetVisualLocked(bool locked)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = locked ? lockedAlpha : 1f;
        }
    }

    // Tooltip 설정
    public static void SetTooltip(ForgeTooltipUI tooltip)
    {
        tooltipUI = tooltip;
    }

    // IPointerEnterHandler 구현
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        
        if (forgeSO != null && tooltipUI != null)
        {
            // 구매 가능 여부 전달 (잠기지 않았으면 구매 가능)
            bool canPurchase = !isLocked;
            tooltipUI.Show(forgeSO, eventData.position, canPurchase);
        }
    }

    // IPointerExitHandler 구현
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        
        // 차징 중이 아닐 때만 툴팁 숨김
        if (!isCharging && tooltipUI != null)
        {
            tooltipUI.Hide();
        }
    }

    // IPointerDownHandler 구현 - 마우스 좌클릭 시작
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && forgeSO != null)
        {
            // 잠긴 노드는 차징 불가
            if (isLocked)
            {
                return;
            }
            
            // 자원 부족하면 차징 불가
            if (!CanAfford())
            {
                return;
            }
            
            // 차징 시작
            StartCharging();
        }
    }
    
    // 자원이 충분한지 확인
    private bool CanAfford()
    {
        InventoryManger inventoryManger = Managers.Instance?.inventory;
        if (inventoryManger == null)
        {
            return false;
        }
        
        // 비용 체크
        if (inventoryManger.OreList[(int)OreType.Coal] < forgeSO.coalCost) return false;
        if (inventoryManger.OreList[(int)OreType.Iron] < forgeSO.ironCost) return false;
        if (inventoryManger.OreList[(int)OreType.Gold] < forgeSO.goldCost) return false;
        if (inventoryManger.OreList[(int)OreType.Diamond] < forgeSO.diamondCost) return false;
        
        return true;
    }

    // IPointerUpHandler 구현 - 마우스 좌클릭 해제
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            StopCharging();
        }
    }

    // 차징 시작
    private void StartCharging()
    {
        if (isCharging) return;
        
        isCharging = true;
        currentChargeTime = 0f;
        
        // 차징 슬라이더 표시
        if (chargeSlider != null)
        {
            chargeSlider.gameObject.SetActive(true);
            chargeSlider.value = 0f;
        }
        
        // Tooltip 차징 게이지도 초기화
        if (tooltipUI != null)
        {
            tooltipUI.UpdateChargeGauge(0f);
        }
        
        // 차징 코루틴 시작
        if (chargeCoroutine != null)
        {
            StopCoroutine(chargeCoroutine);
        }
        chargeCoroutine = StartCoroutine(ChargeCoroutine());
    }

    // 차징 중단
    private void StopCharging()
    {
        if (!isCharging) return;
        
        isCharging = false;
        
        // 차징 슬라이더 숨김
        if (chargeSlider != null)
        {
            chargeSlider.gameObject.SetActive(false);
            chargeSlider.value = 0f;
        }
        
        // Tooltip 차징 게이지도 초기화
        if (tooltipUI != null)
        {
            tooltipUI.UpdateChargeGauge(0f);
            
            // 차징 종료 후 마우스가 노드 밖에 있으면 툴팁 숨김
            if (!isHovering)
            {
                tooltipUI.Hide();
            }
        }
        
        // 코루틴 중단
        if (chargeCoroutine != null)
        {
            StopCoroutine(chargeCoroutine);
            chargeCoroutine = null;
        }
    }

    // 차징 코루틴
    private IEnumerator ChargeCoroutine()
    {
        while (currentChargeTime < chargeTime)
        {
            // Time.timeScale의 영향을 받지 않는 unscaledDeltaTime 사용 (포지 패널에서 timeScale=0이므로)
            currentChargeTime += Time.unscaledDeltaTime;
            
            float fillAmount = currentChargeTime / chargeTime;
            
            // 노드 차징 슬라이더 업데이트
            if (chargeSlider != null)
            {
                chargeSlider.value = fillAmount;
            }
            
            // Tooltip 차징 게이지도 동시에 업데이트
            if (tooltipUI != null)
            {
                tooltipUI.UpdateChargeGauge(fillAmount);
            }
            
            yield return null;
        }
        
        // 차징 완료
        OnChargeComplete();
    }

    // 차징 완료 시 호출
    private void OnChargeComplete()
    {
        // 재사용 가능한 노드인지 확인
        bool isReusable = forgeSO is IReuse reuse && reuse.IsReusable;
        
        // 콜백 실행 (강화 적용)
        onChargeCompleteCallback?.Invoke(forgeSO);
        
        // 재사용 가능한 노드는 구매 후에도 계속 사용 가능
        if (isReusable)
        {
            // 잠금 상태 유지 (항상 구매 가능)
            UpdateLockState();
        }
        
        ChangeNextArrowsColor();
        ChangeNodeColor();
        
        // 툴팁 갱신 (자원 소모 반영)
        if (tooltipUI != null && isHovering && forgeSO != null)
        {
            // 구매 후 상태 다시 확인 (잠금 상태와 자원 상태 모두 변경되었을 수 있음)
            bool canPurchase = !isLocked;
            isOwned = true;
            UpdateAffordabilityTextColor();
            tooltipUI.RefreshContent(forgeSO, canPurchase);
        }
        
        
        // 차징 상태 리셋
        StopCharging();
    }
    
    /// <summary>
    /// 현재 노드 다음에 오는 ArrowBody들의 색상과 머티리얼을 변경합니다.
    /// </summary>
    public void ChangeNextArrowsColor()
    {
        Transform parentTransform = transform.parent;
        if (parentTransform == null)
        {
            Debug.LogError("부모 오브젝트를 찾을 수 없습니다.");
            return;
        }

        // 1. 부모로부터 내 순서(인덱스) 찾기
        int myIndex = transform.GetSiblingIndex();

        // 2. 내 다음 형제(Sibling)부터 순차적으로 탐색
        // (i = myIndex + 1 부터 시작)
        for (int i = myIndex + 1; i < parentTransform.childCount; i++)
        {
            Transform nextSibling = parentTransform.GetChild(i);

            // 3. 다음 형제가 "ArrowBody"인지 확인 (태그 사용 권장)
            // 참고: ArrowBody 오브젝트의 태그를 "ArrowBody"로 설정해야 합니다.
            if (nextSibling.name.StartsWith("ArrowBody")) 
            // 만약 태그 대신 이름으로 검사하고 싶다면:
            // if (nextSibling.gameObject.name.StartsWith("ArrowBody"))
            {
                // 탐색한 오브젝트 확인용 로그
                Debug.Log($"[{gameObject.name}]의 다음 형제 [{nextSibling.name}]는 ArrowBody입니다.");
                // 4. ArrowBody가 맞다면 Renderer 컴포넌트 찾기
                Image arrowImage = nextSibling.GetComponentInChildren<Image>();
                // 참조한 이미지 컴포넌트
                Debug.Log(arrowImage.name);
                
                if (arrowImage != null)
                {
                    Debug.Log($"[{gameObject.name}]의 다음 화살표 [{nextSibling.name}] 색상 변경 중...");

                    // 5. 색상 및 머티리얼 변경
                    // 머티리얼의 '인스턴스'를 생성하여 색상만 변경합니다.
                    arrowImage.color = targetColor; 
                    
                    if (loadedMaterial != null)
                    {
                        arrowImage.material = loadedMaterial;
                    }

                }
            }
            else
            {
                // 6. ArrowBody가 아닌 오브젝트(아마도 다음 노드)를 만나면 즉시 중단
                Debug.Log($"[{nextSibling.name}] 발견. 화살표 탐색을 중단합니다.");
                break; 
            }
        }
    }
    
    public void ChangeNodeFrameColor()
    {
        for (int i = 0; i < 4; i++)
        {
            Transform imageFrame = transform.GetChild(i);
            
            if (imageFrame.name.StartsWith("Image")) 
            {
                Image arrowImage = imageFrame.GetComponent<Image>();
                
                if (arrowImage != null)
                {
                    arrowImage.color = targetColor; 
                    
                    if (loadedMaterial != null)
                    {
                        arrowImage.material = loadedMaterial;
                    }
                }
            }
            else
            {
                break; 
            }
        }
    }

    public void ChangeNodeColor()
    {
        bool isReusable = forgeSO is IReuse reuse && reuse.IsReusable;
        if (isReusable)
        {
            return;
        }
        Transform imageNode = transform.GetChild(4);
        Image nodeImage = imageNode.GetComponent<Image>();
        nodeImage.color = targetNodeColor;
    }
}
