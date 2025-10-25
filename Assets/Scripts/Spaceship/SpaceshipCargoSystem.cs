// SpaceshipCargoSystem.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// (CollectedOreInfo 클래스는 변경 없음)
public class CollectedOreInfo
{
    public GameObject OreObject { get; }
    public LineRenderer Line { get; }
    public List<GameObject> RopeSegments { get; }

    public CollectedOreInfo(GameObject oreObject, LineRenderer line, List<GameObject> ropeSegments)
    {
        OreObject = oreObject;
        Line = line;
        RopeSegments = ropeSegments;
    }
}

public class SpaceshipCargoSystem : MonoBehaviour
{
    [Header("수집 설정")]
    [SerializeField] private Transform cargoHook;
    [SerializeField] private int maxCargoCount = 5;
    [SerializeField] private CircleCollider2D collectionTrigger;

    [Header("밧줄(Rope) 설정")]
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private GameObject ropeSegmentPrefab;
    [SerializeField] private int numberOfSegments = 10;

    // --- 이 부분이 다시 추가되었습니다! ---
    [Tooltip("밧줄이 끊어지는 최대 직선 거리")]
    [SerializeField] private float maxRopeLength = 25f;

    private List<GameObject> potentialOres = new List<GameObject>();
    private List<CollectedOreInfo> collectedOres = new List<CollectedOreInfo>();
    private Rigidbody2D rb;

    [SerializeField] private int skipChecksFramesAfterWarp = 4;
    private int skipChecksUntilFrame = -1;

    [Header("UI 상태 알림")]
    [SerializeField] private BoolVariable hasPotentialOresState; // 주울 광물 상태
    [SerializeField] private BoolVariable isCarryingOresState;   // 들고 있는 광물 상태

    [Header("하이라이트 설정")]
    [Tooltip("주울 수 있는 광물을 강조할 때 사용할 색상입니다.")]
    [SerializeField] private Color highlightColor = Color.green;
    private GameObject currentlyHighlightedOre; // 현재 하이라이트된 광물을 추적
    private Color originalOreColor = Color.white; // 원래 색상으로 되돌리기 위한 값 (대부분의 스프라이트는 흰색이 기본)
    
    private bool isOnceGetPlanetCore = false; // 행성 핵을 한 번이라도 획득했는지 추적

    public GameObject PlanetCoreObj;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        // 1. WorldWarper 이벤트 구독
        WorldWarper.OnWarped += HandleWorldWarped;

        // 2. [추가] 출격 시, collectedOres 리스트에 남아있는 모든 것(행성 핵)을 다시 활성화합니다.
        foreach (var info in collectedOres)
        {
            if (info.OreObject != null) info.OreObject.SetActive(true);
            if (info.Line != null) info.Line.gameObject.SetActive(true);
            foreach (var segment in info.RopeSegments)
            {
                if (segment != null) segment.SetActive(true);
            }
        }
        
        // 3. [추가] 물리 객체들을 다시 활성화했으므로, 물리 동기화가 필요합니다.
        // (이게 없으면 밧줄이 이상한 곳에 가있을 수 있습니다)
        Physics2D.SyncTransforms();

        // 4. [추가] 밧줄이 갑자기 튀는 것을 방지하기 위해 워프 직후처럼 프레임 스킵을 적용합니다.
        skipChecksUntilFrame = Time.frameCount + skipChecksFramesAfterWarp;

        // 5. [추가] UI 상태를 다시 업데이트합니다.
        UpdateCarryingState();
    }

    void OnDisable()
    {
        // 1. WorldWarper 이벤트 구독 해제 (기존 로직)
        WorldWarper.OnWarped -= HandleWorldWarped;

        // 2. [수정] 도킹 시, 리스트를 순회하며 일반 광물과 행성 핵을 다르게 처리합니다.
        for (int i = collectedOres.Count - 1; i >= 0; i--)
        {
            CollectedOreInfo info = collectedOres[i];
            
            // 안전 장치
            if (info == null || info.OreObject == null)
            {
                // 정보가 깨졌으면 리스트에서 일단 제거
                if (info != null) collectedOres.RemoveAt(i); 
                continue;
            }

            // 행성 핵인지 확인
            if (info.OreObject.TryGetComponent<Ore>(out var ore) && ore.oreType == OreType.PlanetCore)
            {
                // [행성 핵]: 파괴하지 않고 숨깁니다.
                info.OreObject.SetActive(false);
                if (info.Line != null) info.Line.gameObject.SetActive(false);
                foreach (var segment in info.RopeSegments)
                {
                    if (segment != null) segment.SetActive(false);
                }
                // 리스트에서 제거하지 않습니다!
            }
            else
            {
                // [일반 광물]: BreakConnection을 호출하여 파괴하고 리스트에서 제거합니다.
                // (BreakConnection이 리스트를 수정하므로 뒤에서부터 순회하는 것이 필수)
                BreakConnection(info);
            }
        }
        // 루프가 끝나면 collectedOres 리스트에는 '행성 핵'만 (비활성화된 상태로) 남아있게 됩니다.

        // 3. 하이라이트가 남아있었다면 확실하게 제거합니다.
        ClearHighlight();

        // 4. UI 상태를 '없음'으로 되돌립니다.
        if (hasPotentialOresState != null)
        {
            hasPotentialOresState.Value = false;
        }
        if (isCarryingOresState != null)
        {
            isCarryingOresState.Value = false; // 도킹 중에는 '안들고있음'
        }
    }
    
    /// <summary>
    /// 현재 행성 핵을 수집한(보유한) 상태인지 확인합니다.
    /// </summary>
    public bool HasPlanetCore()
    {
        // collectedOres 리스트에서 PlanetCore 타입의 광물이 있는지 확인합니다.
        return collectedOres.Any(info => 
            info.OreObject != null && 
            info.OreObject.TryGetComponent<Ore>(out var ore) && 
            ore.oreType == OreType.PlanetCore
        );
    }

    void Update()
    {
        UpdatePotentialOresState();
    
        UpdateHighlight();

        if (Input.GetKeyDown(KeyCode.E)) CollectNearestOre();
        if (Input.GetKeyDown(KeyCode.Q)) DropLastCollectedOre();
    }

    private void UpdatePotentialOresState()
    {
        if (hasPotentialOresState == null) return;
        
        // 현재 상태와 계산된 상태가 다를 때만 값을 변경합니다. (효율성!)
        bool hasOres = potentialOres.Count > 0;
        if (hasPotentialOresState.Value != hasOres)
        {
            hasPotentialOresState.Value = hasOres;
        }
    }

    void LateUpdate()
    {
        // 워프는 WorldWarper.LateUpdate()에서 발생
        // 그 프레임에 HandleWorldWarped() → SyncTransforms()까지 끝난 뒤
        // 여기서 거리 체크/라인 업데이트를 수행하면 순서가 안전하다.
        UpdateAndCheckConnections_Late();
    }

    private void UpdateHighlight()
    {
        // 1. 목록에 있는 광물 중 파괴된 것이 있다면 먼저 정리합니다.
        potentialOres.RemoveAll(item => item == null);

        // 2. 주울 수 있는 후보(potentialOres) 중에서, 이미 수집한(collectedOres) 광물은 제외합니다.
        var highlightCandidates = potentialOres.Where(p_ore => 
            !collectedOres.Any(c_ore => c_ore.OreObject == p_ore)
        ).ToList();

        // 3. 하이라이트 후보가 없다면, 기존 하이라이트를 끄고 함수를 종료합니다.
        if (highlightCandidates.Count == 0)
        {
            ClearHighlight();
            return;
        }

        // 4. 하이라이트 후보 중에서 가장 가까운 광물을 찾습니다.
        GameObject nearestOre = highlightCandidates
            .OrderBy(ore => Vector2.Distance(transform.position, ore.transform.position))
            .FirstOrDefault();

        // 5. 새로 찾은 가장 가까운 광물이 기존에 하이라이트된 광물과 다르다면, 교체해줍니다.
        if (nearestOre != currentlyHighlightedOre)
        {
            ClearHighlight();
            
            if (nearestOre != null)
            {
                var sr = nearestOre.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    //  여기가 핵심! 
                    // 하이라이트 색으로 바꾸기 '직전에' 광물의 현재 색상을 저장합니다.
                    originalOreColor = sr.color;

                    // 이제 하이라이트를 적용합니다.
                    sr.color = highlightColor;
                    currentlyHighlightedOre = nearestOre;
                }
            }
        }
    }

    // [추가] 하이라이트를 끄는 역할을 전담하는 함수 (버그 방지에 매우 중요!)
    private void ClearHighlight()
    {
        if (currentlyHighlightedOre != null)
        {
            var sr = currentlyHighlightedOre.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = originalOreColor; // 원래 색상으로 복원
            }
            currentlyHighlightedOre = null; // 추적 변수 비우기
        }
    }


    private void CollectNearestOre()
    {
        if (collectedOres.Count >= maxCargoCount) return;

        // 현재 하이라이트된 광물이 수집 대상 1순위입니다.
        GameObject oreToCollect = currentlyHighlightedOre;

        // 만약 하이라이트된 광물이 없거나, 어떤 이유로든 주울 수 없는 상태라면 (안전장치)
        // 원래 로직대로 유효한 후보 중에서 가장 가까운 것을 다시 찾습니다.
        if (oreToCollect == null || !potentialOres.Contains(oreToCollect) || collectedOres.Any(c_ore => c_ore.OreObject == oreToCollect))
        {
            var validOres = potentialOres.Where(p_ore => !collectedOres.Any(c_ore => c_ore.OreObject == p_ore)).ToList();
            if(validOres.Count == 0) return; // 주울 것이 완전히 없으면 종료

            oreToCollect = validOres
                .OrderBy(ore => Vector2.Distance(transform.position, ore.transform.position))
                .FirstOrDefault();
        }
        
        if (oreToCollect == null) return;
        
        // 수집 직전에 하이라이트를 확실히 끕니다.
        ClearHighlight();
        
        potentialOres.Remove(oreToCollect);
        
        // --- 이하 로프 생성 로직 ---
        List<GameObject> ropeSegments = new List<GameObject>();
        Rigidbody2D previousSegmentRB = this.rb;

        Vector2 hookPos = cargoHook.position;
        Vector2 orePos = oreToCollect.transform.position;
        float totalDistance = Vector2.Distance(hookPos, orePos);
        Vector2 direction = (orePos - hookPos).normalized;
        float segmentLength = totalDistance / (numberOfSegments + 1);
        
        Vector2 shipLocalHook = rb.transform.InverseTransformPoint(cargoHook.position);

        for (int i = 0; i < numberOfSegments; i++)
        {
            Vector2 spawnPos = hookPos + direction * segmentLength * i;
            GameObject segmentObj = Instantiate(ropeSegmentPrefab, spawnPos, Quaternion.identity);
            ropeSegments.Add(segmentObj);
            
            var segRB = segmentObj.GetComponent<Rigidbody2D>();
            if (segRB == null) segRB = segmentObj.AddComponent<Rigidbody2D>();

            HingeJoint2D joint = segmentObj.GetComponent<HingeJoint2D>();
            if (joint == null) joint = segmentObj.AddComponent<HingeJoint2D>();

            joint.connectedBody = previousSegmentRB;

            if (i == 0)
            {
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = shipLocalHook;
            }
            else
            {
                joint.autoConfigureConnectedAnchor = true;
            }
            previousSegmentRB = segRB;
        }

        var oreRB = oreToCollect.GetComponent<Rigidbody2D>();
        if (oreRB == null) oreRB = oreToCollect.AddComponent<Rigidbody2D>();

        HingeJoint2D oreJoint = oreToCollect.GetComponent<HingeJoint2D>();
        if (oreJoint == null) oreJoint = oreToCollect.AddComponent<HingeJoint2D>();
        oreJoint.connectedBody = previousSegmentRB;
        oreJoint.autoConfigureConnectedAnchor = true;

        GameObject lineObj = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
        LineRenderer line = lineObj.GetComponent<LineRenderer>();
        collectedOres.Add(new CollectedOreInfo(oreToCollect, line, ropeSegments));

        if(oreToCollect.TryGetComponent<Ore>(out var oreComponent))
        {
            if(oreComponent.oreType == OreType.PlanetCore)
            {
                Planet2Manager.instance.SetCoreStatus(true);
            }
        }
        UpdateCarryingState();
    }
    
    
    private void DropLastCollectedOre()
    {
        if (collectedOres.Count == 0) return;

        // 1. 연결을 끊기 전에, 버려질 광물 오브젝트에 대한 참조를 미리 저장합니다.
        CollectedOreInfo lastOreInfo = collectedOres.Last();
        GameObject oreObject = lastOreInfo.OreObject;

        // 2. 기존 로직대로 연결을 끊습니다.
        BreakConnection(lastOreInfo);

        // 3. 방금 버린 광물 오브젝트가 여전히 존재하는지 확인합니다.
        if (oreObject != null)
        {
            // 4. 버려진 광물의 콜라이더가 우리의 수집 트리거 콜라이더와 여전히 닿아 있는지 확인합니다.
            Collider2D oreCollider = oreObject.GetComponent<Collider2D>();
            if (oreCollider != null && collectionTrigger.IsTouching(oreCollider))
            {
                // 5. 닿아있다면, 수집 가능한 목록(potentialOres)에 다시 추가해줍니다.
                //    (중복 추가를 방지하기 위해 목록에 없는지 먼저 확인)
                if (!potentialOres.Contains(oreObject))
                {
                    potentialOres.Add(oreObject);
                }
            }
        }
        UpdateCarryingState();
    }


    private void UpdateAndCheckConnections_Late()
    {
        for (int i = collectedOres.Count - 1; i >= 0; i--)
        {
            CollectedOreInfo oreInfo = collectedOres[i];

            if (oreInfo.OreObject == null)
            {
                BreakConnection(oreInfo);
                continue;
            }

            // 워프 직후 몇 프레임 스킵 (이미 HandleWorldWarped에서 설정)
            if (Time.frameCount <= skipChecksUntilFrame)
            {
                UpdateLine(oreInfo);
                continue;
            }

            float distance = Vector2.Distance(cargoHook.position, oreInfo.OreObject.transform.position);
            if (distance > maxRopeLength)
            {
                BreakConnection(oreInfo);
                continue;
            }

            UpdateLine(oreInfo);
        }
    }


    private void UpdateLine(CollectedOreInfo oreInfo)
    {
        var line = oreInfo.Line;
        var segments = oreInfo.RopeSegments;
        if (line == null) return;

        var alive = segments.Where(s => s != null).ToList();

        line.positionCount = alive.Count + 1; // 세그먼트들 + 마지막 광물
        for (int j = 0; j < alive.Count; j++)
            line.SetPosition(j, alive[j].transform.position);

        line.SetPosition(alive.Count, oreInfo.OreObject.transform.position);
    }

    // (BreakConnection, OnTriggerEnter2D, OnTriggerExit2D 함수는 변경 없음)
    // 이 메서드를 통째로 교체하세요.
    /*private void BreakConnection(CollectedOreInfo oreInfo)
    {
        if (oreInfo == null) return;
        
        // 행성 핵은 무시
        if(oreInfo.OreObject.GetComponent<Ore>().oreType == OreType.PlanetCore)
        {
            return;
        }


        // [추가] 연결이 끊어지는 광물이 하이라이트된 광물이었다면, 하이라이트를 끕니다. (버그 방지)
        if (oreInfo.OreObject != null && oreInfo.OreObject == currentlyHighlightedOre)
        {
            ClearHighlight();
        }
        
        if (oreInfo.OreObject != null)
        {
            // HingeJoint2D가 없을 수도 있으니, 확인 후 파괴합니다.
            var joint = oreInfo.OreObject.GetComponent<HingeJoint2D>();
            if (joint != null)
            {
                Destroy(joint);
            }
        }

        foreach (var segment in oreInfo.RopeSegments)
        {
            if (segment != null) Destroy(segment);
        }

        if (oreInfo.Line != null)
        {
            Destroy(oreInfo.Line.gameObject);
        }
        
        
        collectedOres.Remove(oreInfo);
        UpdateCarryingState();
    }*/

    private void BreakConnection(CollectedOreInfo oreInfo)
    {
        if (oreInfo == null) return;
        
        // --- 이하 로직은 '일반 광물' 또는 '파괴된 행성 핵'일 때만 실행됩니다 ---

        if (oreInfo.OreObject != null && oreInfo.OreObject == currentlyHighlightedOre)
        {
            ClearHighlight();
        }
        
        // [핵심 수정 2] 'null'이 아닐 때만 GetComponent를 호출합니다.
        // (파괴된 행성 핵의 경우 oreInfo.OreObject가 null이므로 이 블록은 건너뜁니다)
        if (oreInfo.OreObject != null)
        {
            var joint = oreInfo.OreObject.GetComponent<HingeJoint2D>();
            if (joint != null)
            {
                Destroy(joint);
            }
        }

        // 밧줄과 라인은 오브젝트가 파괴되었더라도(null이라도) 
        // 정보(oreInfo) 자체는 남아있으므로 정리해야 합니다.
        foreach (var segment in oreInfo.RopeSegments)
        {
            if (segment != null) Destroy(segment);
        }

        if (oreInfo.Line != null)
        {
            Destroy(oreInfo.Line.gameObject);
        }
        
        // [핵심 수정 3] 
        // 이제 '일반 광물' 또는 '파괴된 행성 핵'의 정보가 리스트에서 안전하게 제거됩니다.
        collectedOres.Remove(oreInfo);
        UpdateCarryingState();
    }
    /// <summary>
    /// 외부에서 특정 광물 오브젝트와의 연결을 끊도록 요청하는 공개 함수입니다.
    /// </summary>
    /// <param name="oreObject">연결을 끊고자 하는 광물 게임 오브젝트</param>
    public void BreakConnectionForOre(GameObject oreObject)
    {
        if (oreObject == null) return;

        // collectedOres 리스트에서 요청받은 광물과 일치하는 CollectedOreInfo를 찾습니다.
        CollectedOreInfo infoToBreak = collectedOres.FirstOrDefault(info => info.OreObject == oreObject);

        // 만약 찾았다면, 기존에 있던 private BreakConnection 함수를 호출하여 안전하게 연결을 끊습니다.
        if (infoToBreak != null)
        {
            BreakConnection(infoToBreak);
        }
    }   


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ore"))
        {
            if (!potentialOres.Contains(other.gameObject)) potentialOres.Add(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ore"))
        {
            potentialOres.Remove(other.gameObject);
        }
    }
    // --- 이 함수를 스크립트 맨 아래 (닫히는 괄호 '}' 바로 앞)에 추가해 ---
    public void UnloadAllOres(InventoryManger inventory)
    {
        if (inventory == null)
        {
            return;
        }

        // 가지고 있는 모든 광물 정보를 순회한다.
        foreach (var oreInfo in collectedOres)
        {
            // 광물 오브젝트가 존재하는지 다시 한번 확인하는 건 기본이지.
            if (oreInfo.OreObject != null)
            {
                Ore oreComponent = oreInfo.OreObject.GetComponent<Ore>();
                if (oreComponent != null)
                {
                    if (oreComponent.oreType == OreType.PlanetCore) return; // 행성 핵은 무시
                    // 1. 인벤토리에 광물을 추가.
                    inventory.AddOre(oreComponent.oreType, oreComponent.amount);
                }

                // 2. 이제 쓸모없어진 광물 게임 오브젝트를 파괴.
                Destroy(oreInfo.OreObject);
            }

            // 3. 광물에 연결됐던 모든 밧줄 마디들도 파괴.
            foreach (var segment in oreInfo.RopeSegments)
            {
                Destroy(segment);
            }

            // 4. 눈에 보이던 선(Line Renderer)도 파괴.
            if (oreInfo.Line != null)
            {
                Destroy(oreInfo.Line.gameObject);
            }
        }

        // 5. 모든 짐을 내렸으니, 수집 목록을 깨끗하게 비운다.
        collectedOres.Clear();
        UpdateCarryingState();
    }

    private void UpdateCarryingState()
    {
        if (isCarryingOresState != null)
        {
            isCarryingOresState.Value = collectedOres.Count > 0;
        }
    }

    // 다른 스크립트에서 현재 수집한 광물 개수를 물어볼 수 있도록 통로를 열어줍니다.
    public int GetCollectedOreCount()
    {
        return collectedOres.Count;
    }

    private void HandleWorldWarped(Vector3 delta)
    {
        // 세그먼트/광물 모두 같은 델타로 이동
        foreach (var oreInfo in collectedOres)
        {
            foreach (var seg in oreInfo.RopeSegments)
            {
                if (seg == null) continue;
                var srb = seg.GetComponent<Rigidbody2D>();
                if (srb) srb.position += (Vector2)delta;
                else seg.transform.position += delta;
            }

            if (oreInfo.OreObject != null)
            {
                var orb = oreInfo.OreObject.GetComponent<Rigidbody2D>();
                if (orb) orb.position += (Vector2)delta;
                else oreInfo.OreObject.transform.position += delta;
            }
        }

        // 물리 동기화 (힌지/충돌 안정화)
        Physics2D.SyncTransforms();

        // 이 프레임 포함 N프레임 길이 체크 스킵
        skipChecksUntilFrame = Time.frameCount + skipChecksFramesAfterWarp;
    }

}