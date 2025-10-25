using System.Collections.Generic;
using UnityEngine;

public class ForgeManager : MonoBehaviour
{
    [Header("UI Panel")]
    [SerializeField] private GameObject forgePanel; // 포지 UI 패널
    [SerializeField] private ForgeUI forgeUI; // ForgeUI 참조
    
    [Header("Branch Configuration")]
    public MainBranchSO[] mainBranches;
    public Dictionary<MainBranchType, MainBranchSO> MainBranch = new Dictionary<MainBranchType, MainBranchSO>();
    public Dictionary<SubBranchType, SubBranchSO> SubBranch = new Dictionary<SubBranchType, SubBranchSO>();
    public Dictionary<SubBranchType, SubBranchSO> LockedSubBranch = new Dictionary<SubBranchType, SubBranchSO>();
    public HashSet<SubBranchType> UnlockedSubBranches = new HashSet<SubBranchType>(); // 해금된 서브브랜치

    [Header("상태 방송")]
    [SerializeField] private BoolVariable isForgeOpenState;

    // 각 ForgeId의 현재 레벨 (몇 번째 강화까지 구매했는지)
    private int[] forgeLevel = new int[System.Enum.GetValues(typeof(ForgeId)).Length];
    
    // 각 ForgeId가 몇 개의 강화 단계를 가지는지 캐싱
    private Dictionary<ForgeId, int> forgeTotalLevels = new Dictionary<ForgeId, int>();

    private void Start()
    {
        // forgeLevel 초기화
        for (int i = 0; i < forgeLevel.Length; i++)
        {
            forgeLevel[i] = 0;
        }
        
        for (int i = 0; i < mainBranches.Length; i++)
        {
            MainBranch.Add(mainBranches[i].branchType, mainBranches[i]);
            FindSubBranches(mainBranches[i].subBranches);
            
            // 메인 브랜치의 직속 서브브랜치는 처음부터 해금됨
            if (mainBranches[i].subBranches != null)
            {
                foreach (var subBranch in mainBranches[i].subBranches)
                {
                    UnlockedSubBranches.Add(subBranch.subBranchType);
                    
                    // 각 ForgeId별 최대 레벨 캐싱
                    CacheForgeLevels(subBranch);
                }
            }
        }
        
        // 패널 초기 상태는 비활성화
        if (forgePanel != null)
        {
            forgePanel.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Tab 키를 누르면 포지 패널 토글 (켰다 껐다)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (forgePanel != null)
            {
                if (forgePanel.activeSelf)
                {
                    CloseForgePanel();
                }
                else
                {
                    OpenForgePanel();
                }
            }
        }
        
        // ESC 키를 누르면 포지 패널 닫기만
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (forgePanel != null && forgePanel.activeSelf)
            {
                CloseForgePanel();
            }
        }
    }
    
    // 포지 패널 열기
    private void OpenForgePanel()
    {
        if (forgePanel != null)
        {
            forgePanel.SetActive(true);
            Time.timeScale = 0f; // 시간 정지

            if (isForgeOpenState != null)
            {
                isForgeOpenState.Value = true;
            }
            
            // 패널이 열릴 때 모든 노드의 텍스트 색상 업데이트
            if (forgeUI != null)
            {
                forgeUI.UpdateAllNodeTextColors();
                forgeUI.RefreshAllNodes();
            }
        }
    }
    
    // 포지 패널 닫기
    private void CloseForgePanel()
    {
        if (forgePanel != null)
        {
            forgePanel.SetActive(false);
            Time.timeScale = 1f; // 시간 재개

            if (isForgeOpenState != null)
            {
                isForgeOpenState.Value = false;
            }
        }
    }
    
    // 서브브랜치의 ForgeId별 최대 레벨 캐싱
    private void CacheForgeLevels(SubBranchSO subBranch)
    {
        if (subBranch.baseForgeSOs == null) return;
        
        // ForgeId별로 몇 개씩 있는지 카운트
        Dictionary<ForgeId, int> forgeCount = new Dictionary<ForgeId, int>();
        
        foreach (var forge in subBranch.baseForgeSOs)
        {
            if (forge != null)
            {
                if (!forgeCount.ContainsKey(forge.forgeId))
                {
                    forgeCount[forge.forgeId] = 0;
                }
                forgeCount[forge.forgeId]++;
            }
        }
        
        // forgeTotalLevels에 저장 (최대값 사용)
        foreach (var kvp in forgeCount)
        {
            if (!forgeTotalLevels.ContainsKey(kvp.Key))
            {
                forgeTotalLevels[kvp.Key] = kvp.Value;
            }
            else
            {
                // 다른 서브브랜치에도 있을 경우 최대값 사용
                forgeTotalLevels[kvp.Key] = Mathf.Max(forgeTotalLevels[kvp.Key], kvp.Value);
            }
        }
        
        // 재귀적으로 postSubBranches도 캐싱
        foreach (var forge in subBranch.baseForgeSOs)
        {
            if (forge != null && forge.postSubBranches != null)
            {
                foreach (var postSubBranch in forge.postSubBranches)
                {
                    CacheForgeLevels(postSubBranch);
                }
            }
        }
    }
    //서브 브랜치 탐색용 재귀 함수
    private void FindSubBranches(SubBranchSO[] subBranches)
    {
        foreach (var subBranch in subBranches)
        {
            if (!SubBranch.ContainsKey(subBranch.subBranchType))
            {
                SubBranch.Add(subBranch.subBranchType, subBranch);
            }
            for (int j = 0; j < subBranch.baseForgeSOs.Length; j++)
            {
                // forgeDictionary.Add(subBranch.baseForgeSOs[j].ForgeId, subBranch.baseForgeSOs[j]);
                if (subBranch.baseForgeSOs[j].postSubBranches != null && subBranch.baseForgeSOs[j].postSubBranches.Length > 0)
                {
                    foreach (var postSubBranch in subBranch.baseForgeSOs[j].postSubBranches)
                    {
                        if (!LockedSubBranch.ContainsKey(postSubBranch.subBranchType))
                        {
                            LockedSubBranch.Add(postSubBranch.subBranchType, postSubBranch);
                        }
                    }
                    FindSubBranches(subBranch.baseForgeSOs[j].postSubBranches);
                }
            }
        }
    }
    //특정 레벨의 강화 가져오기
    public BaseForgeSO GetForgeSO(ForgeId forgeId, int level)
    {
        if (SubBranch.Count == 0) return null;
        if (SubBranch.ContainsKey((SubBranchType)(int)forgeId))
        {
            SubBranchSO subBranch = SubBranch[(SubBranchType)(int)forgeId];
            return subBranch.baseForgeSOs[level];
        }
        return null;
    }
    //강화할 때 호출
    public void ForgeApply(BaseForgeSO forgeSO)
    {
        forgeSO?.Apply();
        GameAnalyticsLogger.instance.LogBuildUpgrade( forgeSO.upgradeName);
        // ForgeId의 레벨 증가
        forgeLevel[(int)forgeSO.forgeId]++;
        
        int currentLevel = forgeLevel[(int)forgeSO.forgeId];
        int maxLevel = forgeTotalLevels.ContainsKey(forgeSO.forgeId) ? forgeTotalLevels[forgeSO.forgeId] : 1;
        
        
        // 후행 서브브랜치 해금
        if(forgeSO.postSubBranches != null && forgeSO.postSubBranches.Length > 0)
        {
            foreach (var postSubBranch in forgeSO.postSubBranches)
            {
                if (!UnlockedSubBranches.Contains(postSubBranch.subBranchType))
                {
                    UnlockedSubBranches.Add(postSubBranch.subBranchType);
                    CacheForgeLevels(postSubBranch); // 새로 해금된 브랜치의 레벨도 캐싱
                }
            }
        }
    }
    
    // ForgeId의 현재 레벨 가져오기 (몇 번째까지 구매했는지, 0부터 시작)
    public int GetForgeLevel(ForgeId forgeId)
    {
        return forgeLevel[(int)forgeId];
    }
    
    // ForgeId의 최대 레벨 가져오기
    public int GetForgeMaxLevel(ForgeId forgeId)
    {
        if (forgeTotalLevels.ContainsKey(forgeId))
        {
            return forgeTotalLevels[forgeId];
        }
        return 0;
    }
    
    // 서브브랜치가 해금되었는지 확인
    public bool IsSubBranchUnlocked(SubBranchType subBranchType)
    {
        return UnlockedSubBranches.Contains(subBranchType);
    }
    
    // 특정 강화가 구매 가능한지 확인
    // forgeIndex: 같은 ForgeId 내에서 몇 번째 강화인지 (0부터 시작)
    public bool CanPurchaseForge(SubBranchType subBranchType, ForgeId forgeId, int forgeIndexInSameId)
    {
        // 1. 서브브랜치가 해금되어 있어야 함
        if (!IsSubBranchUnlocked(subBranchType))
            return false;
        
        /*// 2. 현재 ForgeId의 레벨이 forgeIndexInSameId와 일치해야 함
        int currentLevel = GetForgeLevel(forgeId);
        return currentLevel == forgeIndexInSameId;*/
        int currentLevel = GetForgeLevel(forgeId);
        bool basePurchasable = currentLevel == forgeIndexInSameId;

        // 3. Planet2 관련 노드에 대한 추가 조건
        bool isPlanet2RelatedForge =
            forgeId == ForgeId.Planet2CoreMaxHp ||
            forgeId == ForgeId.Planet2ShieldMaxHp ||
            forgeId == ForgeId.Planet2HpRegenAmount ||
            forgeId == ForgeId.Planet2ShieldRegenSpeed ||
            forgeId == ForgeId.Planet2MainCannonUpgrade ||
            forgeId == ForgeId.Planet2MainCannonBulletNumber;

        if (isPlanet2RelatedForge)
        {
            // Planet2Manager.instance가 null이 아니고, IsPlanetActive가 false이면 구매 불가능
            // Planet2Manager.instance는 싱글톤이므로 직접 접근합니다.
            if (Planet2Manager.instance != null && !Planet2Manager.instance.IsPlanetActive)
            {
                return false; // Planet2가 활성화되지 않았으면 구매 불가
            }
        }

        return basePurchasable;
    }
}
