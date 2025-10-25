using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

    [Header("References")]
    [SerializeField] private ForgeManager forgeManager;
    [SerializeField] private InventoryManger inventoryManger; // 인벤토리 매니저 추가
    [SerializeField] private ForgeTooltipUI tooltipUI; // Tooltip UI 프리팹 또는 씬의 Tooltip

    // 생성된 UI 요소들을 추적
    private Dictionary<MainBranchType, GameObject> mainBranchUIObjects = new Dictionary<MainBranchType, GameObject>();
    // private Dictionary<SubBranchType, GameObject> subBranchUIObjects = new Dictionary<SubBranchType, GameObject>();
    private Dictionary<BaseForgeSO, GameObject> forgeNodeUIObjects = new Dictionary<BaseForgeSO, GameObject>();

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
        if (mainBranchSO.subBranches != null)
        {
            float currentYPosition = 0f;
            for (int i = 0; i < mainBranchSO.subBranches.Length; i++)
            {
                var result = CreateSubBranchUI(mainBranchSO.subBranches[i], subBranchContainer, currentYPosition, 0);
                currentYPosition = result.nextYPosition;
                totalSubBranchCount += result.createdCount;
            }
        }

        // 실제 생성된 서브 브랜치 개수로 높이 재계산
        // MainBranch 높이 = (서브브랜치개수 × 서브브랜치높이) + ((개수-1) × Y간격)
        float totalGapHeight = totalSubBranchCount > 0 ? (totalSubBranchCount - 1) * subBranchGapY : 0f;
        float calculatedHeight = (totalSubBranchCount * subBranchHeight) + totalGapHeight;
        
        RectTransform mainBranchRect = mainBranchUI.GetComponent<RectTransform>();
        if (mainBranchRect != null)
        {
            mainBranchRect.sizeDelta = new Vector2(mainBranchRect.sizeDelta.x, calculatedHeight);
        }
    }

    private (GameObject subBranchUI, float nextYPosition, int createdCount) CreateSubBranchUI(SubBranchSO subBranchSO, Transform parent, float currentYPosition, int depth)
    {
        if (subBranchPrefab == null)
        {
            return (null, currentYPosition, 0);
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

            // Y 위치만 조정
            subBranchRect.anchoredPosition = new Vector2(originalPosition.x, originalPosition.y + currentYPosition);

        }

        // 노드 컨테이너 찾기 (Grid Layout은 프리팹에 이미 설정되어 있음)
        Transform nodeContainer = subBranchUI.transform.Find("NodeContainer");
        if (nodeContainer == null)
        {
            nodeContainer = subBranchUI.transform;
        }

        // 다음 Y 위치 계산 (서브브랜치 높이 + Y 간격)
        float nextYPosition = currentYPosition - subBranchHeight - subBranchGapY;
        int totalCreatedCount = 1; // 현재 서브브랜치

        // 노드(BaseForgeSO) 생성 - Grid Layout에 순서대로 배치
        if (subBranchSO.baseForgeSOs != null)
        {
            // Depth별로 노드 분류 (1~5)
            const int maxDepth = 5; // 최대 깊이를 5로 정의
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
                        // 1. 후행 브랜치 생성
                        var result = CreateSubBranchUI(lockedSubBranch, parent, nextYPosition, depth + 1);
                        nextYPosition = result.nextYPosition;
                        totalCreatedCount += result.createdCount;
                        
                        
                        
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

        return (subBranchUI, nextYPosition, totalCreatedCount);
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
            
            // 인덱스가 변경되었으므로 UI 갱신
            if (needsRefresh)
            {
                GenerateForgeUI(); // 전체 재생성
                UpdateAllNodeTextColors(); // 재생성 후 텍스트 색상 업데이트
            }
            else
            {
                RefreshAllNodes(); // 잠금 상태만 업데이트
                UpdateAllNodeTextColors(); // 구매 후 텍스트 색상 업데이트
            }
        }
    }
    
    // 모든 노드의 잠금 상태 갱신
    private void RefreshAllNodes()
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
    }
    
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
        // InventoryManger 찾기
        if (inventoryManger == null)
        {
            inventoryManger = FindFirstObjectByType<InventoryManger>();
            
        }
        
        // Tooltip 초기화
        if (tooltipUI != null)
        {
            ForgeNodeUI.SetTooltip(tooltipUI);
            tooltipUI.Hide(); // 시작 시 숨김
        }

        // 런타임 시작 시 자동으로 UI 생성
        GenerateForgeUI();
    }
}
