using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;

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
    [SerializeField] private Color affordableTextColor = Color.white; // 구매 가능 시 텍스트 색
    [SerializeField] private Color unaffordableTextColor = Color.red; // 구매 불가능 시 텍스트 색

    private BaseForgeSO forgeSO;
    private SubBranchType subBranchType; // 이 노드가 속한 서브브랜치
    private int forgeIndexInSameId; // 같은 ForgeId 내에서 몇 번째인지 (0부터 시작)
    private Action<BaseForgeSO> onChargeCompleteCallback;
    private ForgeManager forgeManger;
    private bool isLocked = false; // 잠금 상태
    
    // Tooltip 관련
    private static ForgeTooltipUI tooltipUI;
    
    // 차징 관련
    private bool isCharging = false;
    private float currentChargeTime = 0f;
    private Coroutine chargeCoroutine;
    private bool isHovering = false; // 마우스 호버 상태

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
        bool canAfford = !isLocked && hasEnoughCoal && hasEnoughIron && hasEnoughGold && hasEnoughDiamond;
        
        // 텍스트 색상 변경
        Color textColor = canAfford ? affordableTextColor : unaffordableTextColor;
        
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
            // 재사용 가능한 노드는 항상 구매 가능 (비용만 체크)
            isLocked = false;
        }
        else
        {
            // 일반 노드는 구매 가능 여부 확인
            bool canPurchase = forgeManger.CanPurchaseForge(subBranchType, forgeSO.forgeId, forgeIndexInSameId);
            isLocked = !canPurchase;
        }
        
        SetVisualLocked(isLocked);
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
        
        // 툴팁 갱신 (자원 소모 반영)
        if (tooltipUI != null && isHovering && forgeSO != null)
        {
            // 구매 후 상태 다시 확인 (잠금 상태와 자원 상태 모두 변경되었을 수 있음)
            bool canPurchase = !isLocked;
            tooltipUI.RefreshContent(forgeSO, canPurchase);
        }
        
        // 차징 상태 리셋
        StopCharging();
    }
}
