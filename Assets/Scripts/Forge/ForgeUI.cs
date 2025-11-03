using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ForgeUI : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private GameObject mainBranchPrefab;
    [SerializeField] private GameObject subBranchPrefab;
    [SerializeField] private GameObject forgeNodePrefab; // 실제 강화 노드 프리팹
    [SerializeField] private GameObject arrowBodyPrefab; // 화살표 몸통 프리팹 (빈 칸용)
    [SerializeField] private GameObject arrowCornerPrefab; // 꺾인 화살표 프리팹 (후행 브랜치용)

    [Header("UI Container")]
    [SerializeField] private Transform mainBranchContainer; // 메인 브랜치들이 생성될 부모
    
    [Header("Layout Settings")]
    // 서브 브랜치 설정
    [SerializeField] private float subBranchHeight = 100f;        // 서브 브랜치 높이
    [SerializeField] private float subBranchGapY = 30f;           // 서브 브랜치 간 Y 간격
    
    // 메인 브랜치 설정
    [SerializeField] private float baseMainBranchHeight = 100f;   // 메인 브랜치 기본 높이

    // [추가] 가로 스크롤 계산을 위한 변수
    [Space(10)]
    [Tooltip("후행 브랜치가 생성될 때마다 들여쓰기할 X축 거리")]
    [SerializeField] private float subBranchIndentX = 50f;
    [Tooltip("SubBranch UI의 기본 너비 (가로 스크롤 계산의 기준값)")]
    [SerializeField] private float baseSubBranchWidth = 700f; // 예: NodeContainer의 기본 너비

    [Header("References")]
    [SerializeField] private ForgeManager forgeManager;
    [SerializeField] private InventoryManger inventoryManger; // 인벤토리 매니저 추가
    [SerializeField] private ForgeTooltipUI tooltipUI; // Tooltip UI 프리팹 또는 씬의 Tooltip

    // 생성된 UI 요소들을 추적
    private Dictionary<MainBranchType, GameObject> mainBranchUIObjects = new Dictionary<MainBranchType, GameObject>();
    // private Dictionary<SubBranchType, GameObject> subBranchUIObjects = new Dictionary<SubBranchType, GameObject>();
    private Dictionary<BaseForgeSO, GameObject> forgeNodeUIObjects = new Dictionary<BaseForgeSO, GameObject>();
    
    [Header("Global Upgrade Panel")]
    [Tooltip("업그레이드 가능 알림 패널")]
    [SerializeField] private GameObject enableUpgradePanel;

    // --- [수정된 부분] ---
    // (기존 코드 유지)
    // --- [수정 완료] ---
    private CanvasGroup enableUpgradePanelCanvasGroup;

    public void GenerateForgeUI()
    {
        ClearExistingUI();

        if (forgeManager == null || forgeManager.mainBranches == null)
        {
            return;
        }

        // 각 메인 브랜치에 대해 UI 생성
        foreach (var mainBranch in forgeManager.mainBranches)
        {
            CreateMainBranchUI(mainBranch);
        }
        
        // [추가] UI 생성 직후 상태 갱신
        RefreshAllNodes();
        UpdateAllNodeTextColors();
    }
    
    public void ClearForgeUI()
    {
        ClearExistingUI();
    }

    private void CreateMainBranchUI(MainBranchSO mainBranchSO)
    {
        if (mainBranchPrefab == null || mainBranchContainer == null)
        {
            return;
        }

        // 메인 브랜치 UI 생성
        GameObject mainBranchUI = Instantiate(mainBranchPrefab, mainBranchContainer);
        mainBranchUIObjects[mainBranchSO.branchType] = mainBranchUI;

        // 메인 브랜치 이름 설정
        mainBranchUI.name = $"MainBranch_{mainBranchSO.branchType}";

        // 메인 브랜치 UI 컴포넌트 가져오기 (있다면)
        var mainBranchUIComponent = mainBranchUI.GetComponent<MainBranchUI>();
        if (mainBranchUIComponent != null)
        {
            mainBranchUIComponent.Initialize(mainBranchSO);
        }
        
        // 서브 브랜치 컨테이너 찾기 (프리팹 내부에 "SubBranchContainer"라는 이름의 Transform이 있다고 가정)
        Transform subBranchContainer = mainBranchUI.transform.Find("SubBranchContainer");
        if (subBranchContainer == null)
        {
            // 없으면 메인 브랜치 자체를 컨테이너로 사용
            subBranchContainer = mainBranchUI.transform;
        }

        // 서브 브랜치 생성 (LockedSubBranch 포함하여 동적으로 생성)
        int totalSubBranchCount = 0;
        int maxDepthInBranch = 0; // [추가] 이 메인 브랜치 내의 최대 깊이를 추적

        if (mainBranchSO.subBranches != null)
        {
            float currentYPosition = 0f;
            for (int i = 0; i < mainBranchSO.subBranches.Length; i++)
            {
                // [수정] 반환값에 maxDepth 추가됨
                var result = CreateSubBranchUI(mainBranchSO.subBranches[i], subBranchContainer, currentYPosition, 0);
                
                currentYPosition = result.nextYPosition;
                totalSubBranchCount += result.createdCount;
                
                // [추가] 반환된 최대 깊이로 갱신
                if (result.maxDepth > maxDepthInBranch)
                {
                    maxDepthInBranch = result.maxDepth;
                }
            }
        }

        // (기존 로직) 실제 생성된 서브 브랜치 개수로 높이 재계산
        float totalGapHeight = totalSubBranchCount > 0 ? (totalSubBranchCount - 1) * subBranchGapY : 0f;
        float calculatedHeight = (totalSubBranchCount * subBranchHeight) + totalGapHeight;
        
        RectTransform mainBranchRect = mainBranchUI.GetComponent<RectTransform>();
        if (mainBranchRect != null)
        {
            // [추가] 가로 너비 계산
            // 너비 = 기본 브랜치 너비 + (최대 깊이 * 들여쓰기 간격)
            float calculatedWidth = baseSubBranchWidth + (maxDepthInBranch * subBranchIndentX);
            Debug.Log($"MainBranch {mainBranchSO.branchType} - Calculated Width: {calculatedWidth}, Height: {calculatedHeight}");
            // [수정] 계산된 너비(calculatedWidth)와 높이(calculatedHeight)를 함께 설정
            mainBranchRect.sizeDelta = new Vector2(calculatedWidth, calculatedHeight);
        }
    }

    // [수정] 반환 타입에 int maxDepth 추가
    private (GameObject subBranchUI, float nextYPosition, int createdCount, int maxDepth) CreateSubBranchUI(SubBranchSO subBranchSO, Transform parent, float currentYPosition, int depth)
    {
        if (subBranchPrefab == null)
        {
            // [수정] 반환 타입에 맞게 현재 depth 반환
            return (null, currentYPosition, 0, depth);
        }

        // 서브 브랜치 UI 생성
        GameObject subBranchUI = Instantiate(subBranchPrefab, parent);

        // 서브 브랜치 이름 설정
        string branchPrefix = depth > 0 ? "Locked" : "";
        subBranchUI.name = $"{branchPrefix}SubBranch_{subBranchSO.subBranchType}";

        // 노드 개수 세기
        int nodeCount = subBranchSO.baseForgeSOs != null ? subBranchSO.baseForgeSOs.Length : 0;
        
        // 서브 브랜치 위치 조정 (크기는 프리팹에 설정된 값 사용)
        RectTransform subBranchRect = subBranchUI.GetComponent<RectTransform>();
        if (subBranchRect != null)
        {
            // 프리팹의 기존 위치 저장
            Vector2 originalPosition = subBranchRect.anchoredPosition;

            // [추가] Depth에 따라 X 위치(들여쓰기) 계산
            float newXPosition = originalPosition.x + (depth * subBranchIndentX);

            // [수정] Y 위치 및 계산된 X 위치 적용
            subBranchRect.anchoredPosition = new Vector2(newXPosition, originalPosition.y + currentYPosition);
        }

        // 노드 컨테이너 찾기 (Grid Layout은 프리팹에 이미 설정되어 있음)
        Transform nodeContainer = subBranchUI.transform.Find("NodeContainer");
        if (nodeContainer == null)
        {
            nodeContainer = subBranchUI.transform;
        }

        // (기존 로직) 다음 Y 위치 계산 (서브브랜치 높이 + Y 간격)
        float nextYPosition = currentYPosition - subBranchHeight - subBranchGapY;
        int totalCreatedCount = 1; // 현재 서브브랜치
        int maxDepthEncountered = depth; // [추가] 최대 깊이 추적 변수 (현재 깊이로 초기화)

        // 노드(BaseForgeSO) 생성 - Grid Layout에 순서대로 배치
        if (subBranchSO.baseForgeSOs != null)
        {
            // Depth별로 노드 분류 (1~10)
            const int maxDepth = 10; // (기존에 10으로 수정한 것 유지)
            Dictionary<int, List<BaseForgeSO>> nodesByDepth = new Dictionary<int, List<BaseForgeSO>>();
            for (int d = 1; d <= maxDepth; d++)
            {
                nodesByDepth[d] = new List<BaseForgeSO>();
            }
            
            // 각 ForgeId가 몇 번째인지 카운트
            Dictionary<ForgeId, int> forgeIdCount = new Dictionary<ForgeId, int>();
            
            foreach (var forgeSO in subBranchSO.baseForgeSOs)
            {
                int nodeDepth = Mathf.Clamp(forgeSO.depth, 1, maxDepth);
                nodesByDepth[nodeDepth].Add(forgeSO);
            }
            
            // 노드가 있는 최대 Depth 찾기
            int maxDepthWithNodes = 0;
            for (int d = maxDepth; d >= 1; d--)
            {
                if (nodesByDepth[d].Count > 0)
                {
                    maxDepthWithNodes = d;
                    break;
                }
            }
            
            // Depth 1~maxDepthWithNodes까지만 순회 (빈 칸 최소화)
            for (int currentDepth = 1; currentDepth <= maxDepthWithNodes; currentDepth++)
            {
                if (nodesByDepth[currentDepth].Count > 0)
                {
                    // 이 Depth에 노드가 있으면 모두 생성
                    foreach (var forgeSO in nodesByDepth[currentDepth])
                    {
                        // 같은 ForgeId 내에서 몇 번째인지 계산
                        if (!forgeIdCount.ContainsKey(forgeSO.forgeId))
                        {
                            forgeIdCount[forgeSO.forgeId] = 0;
                        }
                        int indexInSameId = forgeIdCount[forgeSO.forgeId];
                        forgeIdCount[forgeSO.forgeId]++;
                        
                        CreateForgeNodeUI(forgeSO, subBranchSO.subBranchType, indexInSameId, nodeContainer, depth);
                    }
                }
                else
                {
                    // 해당 Depth에 노드가 없으면 화살표 몸통 배치
                    CreateArrowBody(nodeContainer);
                }
            }
            
            // postSubBranches 처리 (실제 후행 브랜치 생성)
            foreach (var forgeSO in subBranchSO.baseForgeSOs)
            {
                if (forgeSO.postSubBranches != null && forgeSO.postSubBranches.Length > 0)
                {
                    foreach (var lockedSubBranch in forgeSO.postSubBranches)
                    {
                        // 1. 후행 브랜치 생성 (depth + 1 전달)
                        // [수정] 4개의 값을 반환받음
                        var result = CreateSubBranchUI(lockedSubBranch, parent, nextYPosition, depth + 1); 
                        
                        nextYPosition = result.nextYPosition;
                        totalCreatedCount += result.createdCount;

                        // [추가] 재귀 호출에서 반환된 최대 깊이로 갱신
                        if (result.maxDepth > maxDepthEncountered)
                        {
                            maxDepthEncountered = result.maxDepth;
                        }
                        
                        // 2. 생성된 후행 브랜치에서 NodeContainer 찾기
                        if (result.subBranchUI != null)
                        {
                            // 자식 구조 확인
                            
                            for (int i = 0; i < result.subBranchUI.transform.childCount; i++)
                            {
                            }
                            
                            Transform postNodeContainer = result.subBranchUI.transform.Find("NodeContainer");
                            if (postNodeContainer == null)
                            {
                                // NodeContainer가 없으면 SubBranch 자체를 사용
                                postNodeContainer = result.subBranchUI.transform;
                            }
                            
                            // 3. 후행 브랜치의 NodeContainer에 꺾인 화살표 생성
                            GameObject cornerArrow = CreateCornerArrowAndReturn(postNodeContainer);
                            
                            // 4. 생성된 꺾인 화살표를 맨 앞(첫 번째 자식)으로 이동
                            if (cornerArrow != null)
                            {
                                cornerArrow.transform.SetAsFirstSibling();
                            }
                        }
                        else
                        {
                        }
                    }
                }
            }
        }
        
        // [수정] 계산된 최대 깊이(maxDepthEncountered)를 반환
        return (subBranchUI, nextYPosition, totalCreatedCount, maxDepthEncountered);
    }

    private void CreateForgeNodeUI(BaseForgeSO forgeSO, SubBranchType subBranchType, int indexInSameId, Transform parent, int depth)
    {
        if (forgeNodePrefab == null)
        {
            return;
        }

        // 노드 UI 생성 (Grid Layout이 자동으로 위치 조정)
        GameObject nodeUI = Instantiate(forgeNodePrefab, parent);
        forgeNodeUIObjects[forgeSO] = nodeUI;

        // 노드 이름 설정
        nodeUI.name = $"Node_{forgeSO.forgeId}_{forgeSO.upgradeName}_Depth{forgeSO.depth}";

        // 노드 UI 컴포넌트 가져오기
        var nodeUIComponent = nodeUI.GetComponent<ForgeNodeUI>();
        if (nodeUIComponent != null)
        {
            nodeUIComponent.Initialize(forgeSO, subBranchType, indexInSameId, forgeManager, OnForgeNodeClicked);
        }
        else
        {
            // ForgeNodeUI 컴포넌트가 없으면 버튼에 직접 리스너 추가
            Button button = nodeUI.GetComponent<Button>();
            if (button == null)
            {
                button = nodeUI.GetComponentInChildren<Button>();
            }

            if (button != null)
            {
                button.onClick.AddListener(() => OnForgeNodeClicked(forgeSO));
            }
            else
            {
            }
        }
        
        if(nodeUIComponent.isOwned)
        {
            nodeUIComponent.ChangeNextArrowsColor();
            nodeUIComponent.ChangeNodeColor();
            nodeUIComponent.ChangeNodeFrameColor();
        }
    }

    // 빈 노드 생성 (노드가 없는 Depth용)
    private void CreateEmptyNode(Transform parent)
    {
        GameObject emptyNode = new GameObject("EmptySlot");
        emptyNode.transform.SetParent(parent, false);
        emptyNode.AddComponent<RectTransform>();
    }
    
    // 화살표 몸통 생성 (노드가 없는 Depth용)
    private void CreateArrowBody(Transform parent)
    {
        if (arrowBodyPrefab != null)
        {
            GameObject arrowBody = Instantiate(arrowBodyPrefab, parent);
            arrowBody.name = "ArrowBody";
        }
        else
        {
            // 화살표 몸통 프리팹이 없으면 빈 칸
            CreateEmptyNode(parent);
        }
    }
    
    // 꺾인 화살표 생성 (↓, 후행 브랜치용)
    private void CreateCornerArrow(Transform parent)
    {
        if (arrowCornerPrefab != null)
        {
            GameObject cornerArrow = Instantiate(arrowCornerPrefab, parent);
            cornerArrow.name = "ArrowCorner";
        }
        else
        {
            // 꺾인 화살표 프리팹이 없으면 빈 칸
            CreateEmptyNode(parent);
        }
    }
    
    // 꺾인 화살표 생성 및 GameObject 반환
    private GameObject CreateCornerArrowAndReturn(Transform parent)
    {
        if (arrowCornerPrefab != null)
        {
            GameObject cornerArrow = Instantiate(arrowCornerPrefab, parent);
            cornerArrow.name = "ArrowCorner";
            return cornerArrow;
        }
        else
        {
            // 꺾인 화살표 프리팹이 없으면 빈 칸 생성 후 null 반환
            CreateEmptyNode(parent);
            return null;
        }
    }

    // 노드 버튼이 클릭되었을 때 호출되는 콜백 (차징 완료 시)
    private void OnForgeNodeClicked(BaseForgeSO forgeSO)
    {
        if (forgeSO == null) return;
        
        // 인벤토리 매니저 확인
        if (inventoryManger == null)
        {
            inventoryManger = FindFirstObjectByType<InventoryManger>();

        }
        
        // 비용 체크
        if (!inventoryManger.CheckOre(forgeSO))
        {
            return;
        }
        
        // 비용 차감
        if (inventoryManger.ConsumeOre(forgeSO))
        {
            
            // ForgeManger를 통해 강화 적용
            forgeManager.ForgeApply(forgeSO);
            
            // UI 갱신 (후행 브랜치 언락 or 인덱스 변경)
            bool needsRefresh = false;
            
            // postSubBranches가 있으면 UI 재생성
            if (forgeSO.postSubBranches != null && forgeSO.postSubBranches.Length > 0)
            {
                needsRefresh = true;
            }
            
            // [수정] 후행 브랜치가 열릴 때는 UI의 전체 너비/높이를
            // 다시 계산해야 하므로, RefreshAllNodes()가 아닌
            // GenerateForgeUI()를 호출해야 합니다.
            if (needsRefresh)
            {
                // GenerateForgeUI(); // 전체 재생성 (이 함수가 Refresh/UpdateTextColors를 호출)
            }
            else
            {
                RefreshAllNodes(); // 잠금 상태만 업데이트
                UpdateAllNodeTextColors(); // 구매 후 텍스트 색상 업데이트
            }
        }
    }
    
    // 모든 노드의 잠금 상태 갱신
    public void RefreshAllNodes()
    {
        foreach (var nodeUI in forgeNodeUIObjects.Values)
        {
            if (nodeUI != null)
            {
                var nodeComponent = nodeUI.GetComponent<ForgeNodeUI>();
                if (nodeComponent != null)
                {
                    nodeComponent.RefreshUI();
                }
            }
        }
        
        UpdateUpgradeablePanelStatus();
    }
    
    // --- [핵심 수정된 메서드] ---
    // (기존 코드 유지)
    private void UpdateUpgradeablePanelStatus()
    {
        if (enableUpgradePanelCanvasGroup == null) return;

        bool anyUpgradeable = false; 
        
        foreach (var nodeUI in forgeNodeUIObjects.Values)
        {
            if (nodeUI != null)
            {
                var nodeComponent = nodeUI.GetComponent<ForgeNodeUI>();
                
                if (nodeComponent != null && nodeComponent.canPurchase && nodeComponent.canAfford)
                {
                    anyUpgradeable = true;
                    break; 
                }
            }
        }

        enableUpgradePanelCanvasGroup.alpha = anyUpgradeable ? 1f : 0f;
    }
    // --- [수정 완료] ---

    
    // 모든 노드의 구매 가능 여부에 따라 텍스트 색상 업데이트
    public void UpdateAllNodeTextColors()
    {
        foreach (var nodeUI in forgeNodeUIObjects.Values)
        {
            if (nodeUI != null)
            {
                var nodeComponent = nodeUI.GetComponent<ForgeNodeUI>();
                if (nodeComponent != null)
                {
                    nodeComponent.UpdateAffordabilityTextColor();
                }
            }
        }
    }

    private void ClearExistingUI()
    {
        // 기존 UI 요소들 제거 (런타임 전용)
        foreach (var ui in mainBranchUIObjects.Values)
        {
            if (ui != null)
            {
                Destroy(ui);
            }
        }
        mainBranchUIObjects.Clear();
        
        // subBranchUIObjects.Clear();
        forgeNodeUIObjects.Clear();
    }

    void Start()
    {
        // (기존 코드 유지)
        if (enableUpgradePanel == null)
        {
            enableUpgradePanel = GameObject.FindGameObjectWithTag("EnableUpgradePanel");
        }
        
        if (enableUpgradePanel != null)
        {
            enableUpgradePanelCanvasGroup = enableUpgradePanel.GetComponent<CanvasGroup>();
            if (enableUpgradePanelCanvasGroup == null)
            {
                enableUpgradePanelCanvasGroup = enableUpgradePanel.AddComponent<CanvasGroup>();
            }
            
            enableUpgradePanelCanvasGroup.alpha = 0f; 
            
            enableUpgradePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("ForgeUI: enableUpgradePanel이 할당되지 않았습니다.");
        }
        
        if (inventoryManger == null)
        {
            inventoryManger = FindFirstObjectByType<InventoryManger>();
        }
        
        if (tooltipUI != null)
        {
            ForgeNodeUI.SetTooltip(tooltipUI);
            tooltipUI.Hide(); // 시작 시 숨김
        }

        // 런타임 시작 시 자동으로 UI 생성
        GenerateForgeUI();
        // [수정] GenerateForgeUI()가 Refresh/UpdateTextColors를 호출하도록 변경했으므로 
        // Start()에서는 GenerateForgeUI()만 호출하면 됩니다.
    }

    void Update()
    {
        // [참고] 매 프레임 Refresh/UpdateTextColors를 호출하는 것은
        // 성능에 큰 부담을 줄 수 있습니다.
        // 자원 획득, 노드 구매 등 '이벤트'가 발생할 때만 호출하는 것이 좋습니다.
        RefreshAllNodes();  
        UpdateAllNodeTextColors();
    }
}