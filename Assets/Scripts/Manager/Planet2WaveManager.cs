using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
[DefaultExecutionOrder(-10)]
public class Planet2WaveManager : MonoBehaviour
{
    public static Planet2WaveManager Instance;

    [Header("Enemy Settings")]
    [Tooltip("Enum 순서와 일치해야 합니다!")]
    public GameObject[] enemyPrefabs; // 인덱스 = EnemyType 순서
    public Transform Target;

    [Header("Spawn Settings")]
    [Tooltip("최대 줌아웃 시 카메라 orthographic size")]
    public float maxCameraSize = 20f;
    [Tooltip("카메라 경계에서 얼마나 떨어진 곳에서 스폰할지")]
    public float spawnDistanceOffset = 2f;

    [Header("Spawn Anti-Overlap")]
    [Tooltip("적들이 서로 겹치지 않도록 보장할 최소 거리. 0 이하면 이 기능을 사용하지 않습니다.")]
    [SerializeField] private float minSpawnDistance = 1.0f;
    [Tooltip("최소 거리를 확보하기 위해 새 위치를 찾으려는 최대 시도 횟수")]
    [SerializeField] private int maxSpawnRetries = 10;

    [Header("Wave Settings")]
    [Tooltip("웨이브 데이터 리스트")]
    public WaveSO[] waves;
    [Tooltip("다음 웨이브까지 대기 시간 (초)")]
    public float timeBetweenWaves = 5f;
    private int currentWaveIndex = 0;
    public float countdown = 10f;
    private bool isSpawning = false;
    private bool isFirst = true; // 게임 시작 시 첫 번째 카운트다운인지 확인
    private bool waveEnd = false;
    public int enemyNum = 0;
    private bool hasTriggeredWaveClearAction = false;
    private bool holdAfterGate = false;

    [Header("Spawn Center")]
    [SerializeField] private Transform spawnCenter;

    [Header("UI")]
    public TMP_Text waveTimerText;
    public TMP_Text enemyCountText;
    public TMP_Text miningInstructionText; // 채굴 안내 텍스트
    private bool forceStartRequested = false;

    [HideInInspector] public int EnemyCount = 0;
    private int totalEnemiesInWave = 0; // 현재 웨이브의 총 적 수

    public Dictionary<EnemyType, IObjectPool<GameObject>> enemyPools = new();
    private Dictionary<EnemyType, int> remainingSpawnCounts = new();

    [Header("Core Targets")]
    [SerializeField] private Core planet1Core;        
    [SerializeField] private Core planet2Core;          
    private bool planet2CoreAlive = true;
    [Header("Wave Pre-start Delays (sec)")]
    [Tooltip("각 웨이브 시작 '직전'에 기다릴 시간(초). 비어있거나 음수면 defaultPreDelay 사용")]
    [SerializeField] private List<float> preStartDelays = new List<float>();

    [Tooltip("preStartDelays에 항목이 없거나 음수일 때 사용할 기본 지연값(초)")]
    [SerializeField] private float defaultPreDelay = 5f;
    
    public Slider mainbossHpSlider;

    public float GetPreDelayForWaveIndex(int waveIndex)
    {
        if (preStartDelays != null &&
            waveIndex >= 0 &&
            waveIndex < preStartDelays.Count &&
            preStartDelays[waveIndex] >= 0f)
            return preStartDelays[waveIndex];
        return defaultPreDelay;
    }

    public float GetUpcomingPreDelay() => GetPreDelayForWaveIndex(currentWaveIndex);

    private readonly HashSet<Enemy> activeEnemies = new();

    public int CurrentWaveIndex => currentWaveIndex;

    public Transform bossSpwanPoint;
    public Transform mainBossSpwanPoint;

    [Header("Central Sync (P2)")]
    public bool AllWavesCompleted => currentWaveIndex >= waves.Length;
    public bool IsBetweenWaves() => (EnemyCount <= 0 && !isSpawning && currentWaveIndex < waves.Length);
    public float TimeBetweenWaves => timeBetweenWaves;

    private bool countdownLockedByCentral = false;
    private bool countdownArmedByCentral = false;

    public void LockCountdownByCentral(bool v)
    {
        countdownLockedByCentral = v;
        if (v) countdownArmedByCentral = false;
    }

    public void StartSimulCountdownFromCentral(float seconds)
    {
        countdown = seconds;
        countdownArmedByCentral = true;
    }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
            enemyPools[type] = CreatePool(type);

        if (waveTimerText != null) waveTimerText.text = "";
        if (enemyCountText != null) enemyCountText.text = "";
        if (miningInstructionText != null)
        {
            miningInstructionText.text = "";
            miningInstructionText.color = Color.green;
        }

        if (planet2Core != null)
        {
            planet2CoreAlive = planet2Core.CurrentHP > 0;
            planet2Core.OnDie += HandlePlanet2CoreDie;
            planet2Core.OnRevive += HandlePlanet2CoreRevive;
        }

        // 메인 보스 ui 초기화
        mainbossHpSlider.gameObject.SetActive(false);
        mainbossHpSlider.value = mainbossHpSlider.maxValue;
    }

    private void Update()
    {
        if (isSpawning)
        {
            waveEnd = true;
            if (waveTimerText != null) waveTimerText.text = $"Wave {currentWaveIndex + 1}";
            if (enemyCountText != null) enemyCountText.text = $"Colony : {EnemyCount}";
            if (miningInstructionText != null) { miningInstructionText.color = Color.red; miningInstructionText.text = "적의 공격이다! 기지로 돌아가라!"; }
            return;
        }

        if (EnemyCount > 0 && !isSpawning)
        {
            if (enemyCountText != null) enemyCountText.text = $"Colony : {EnemyCount}";
            if (miningInstructionText != null) { miningInstructionText.color = Color.red; miningInstructionText.text = "적의 공격이다! 기지로 돌아가라!"; }
            return;
        }

        if (EnemyCount <= 0 && !isSpawning)
        {
            if (WaveManager.Instance != null
                && WaveManager.Instance.IsCombinedPhase
                && countdownLockedByCentral
                && !countdownArmedByCentral)
            {
                if (waveTimerText != null) waveTimerText.text = "Waiting other planet...";
                if (enemyCountText != null) enemyCountText.text = "";
                return;
            }
            if (forceStartRequested) { 
                StartCoroutine(SpawnWave()); 
                countdown = timeBetweenWaves;
                isFirst = false; 
                forceStartRequested = false; 
                return; 
            }
            if (currentWaveIndex >= waves.Length)
            {
                // 모든 웨이브가 완료된 후에는 더 이상 로그를 기록하지 않음
                if (waveTimerText != null) waveTimerText.text = "All Waves Completed!";
                if (enemyCountText != null) enemyCountText.text = "";
                if (miningInstructionText != null) miningInstructionText.text = "";
                return;
            }

            if (!hasTriggeredWaveClearAction)
            {
                hasTriggeredWaveClearAction = true;
                if (!isFirst)
                {
                    LogAndResetWaveStats(); // 웨이브 클리어 시점에 로그 기록
                }
            }

            EnemyCount = 0;
            countdown -= Time.deltaTime;

            if (countdown <= 0f)
            {
                bool inCombined = (WaveManager.Instance != null && WaveManager.Instance.IsCombinedPhase);
                if (inCombined && !countdownArmedByCentral)
                    return;
                StartCoroutine(SpawnWave());
                countdownArmedByCentral = false; // [SYNC]
                if (WaveManager.Instance == null || !WaveManager.Instance.IsCombinedPhase)
                {
                    countdown = GetPreDelayForWaveIndex(currentWaveIndex + 1);
                }
                else
                {
                    // 중앙이 두 행성이 같은 프레임에 BetweenWaves가 되었을 때 주입함
                    LockCountdownByCentral(true);     // 내 카운트는 중앙 신호까지 대기
                                                      // countdown은 건드리지 않음
                }
                isFirst = false;
                return;
            }
            else
            {
                if (waveTimerText != null) waveTimerText.text = $"Next Wave {currentWaveIndex + 1} In: {Mathf.Ceil(countdown)}";
                if (enemyCountText != null) enemyCountText.text = "Mining Phase";
                if (miningInstructionText != null)
                {
                    if (isFirst) miningInstructionText.text = "";
                    else { miningInstructionText.color = Color.green; miningInstructionText.text = "자원을 탐색하세요"; } 
                }
            }
        }
    }

    private void LogAndResetWaveStats() // 메서드 이름은 유지하되, 내부 로직을 단순화
    {
        mainbossHpSlider.gameObject.SetActive(false);
        mainbossHpSlider.value = mainbossHpSlider.maxValue;
        
        // 웨이브 완료 및 광물 로그 기록
        // 중복되는 wave 로그 주석 처리
        // GameAnalyticsLogger.instance.LogWaveComplete(Managers.Instance.core.CurrentHP);

        // 웨이브 통계 리셋
        Managers.Instance.inventory.ResetWaveStats();
        GameAnalyticsLogger.instance.UpdateWave();
    }

    // --- 중앙 WaveManager 호환 훅(추가) ---
    public void PauseByCentral() => enabled = false;

    public void ResumeNextWaveByCentral()
    {
        enabled = true;
        countdown = 0f;
    }
    // -------------------------------------

    private IObjectPool<GameObject> CreatePool(EnemyType type)
    {
        return new ObjectPool<GameObject>(
            createFunc: () => CreateEnemy(type),
            actionOnGet: OnGetEnemy,
            actionOnRelease: OnReleaseEnemy,
            actionOnDestroy: OnDestroyEnemy,
            collectionCheck: false,
            defaultCapacity: 10,
            maxSize: 100
        );
    }

    private Vector3 GetRandomSpawnPosition(List<Vector3> recentPositions)
    {
        Camera mainCamera = Camera.main;
        float aspect = mainCamera != null ? mainCamera.aspect : 16f / 9f;
        float horizontalSize = maxCameraSize * aspect;
        float spawnRadius = Mathf.Max(maxCameraSize, horizontalSize) + spawnDistanceOffset;

        Vector3 center = spawnCenter
                     ? spawnCenter.position
                     : (planet2Core ? planet2Core.transform.position : transform.position);

        // 최소 거리 체크 기능이 비활성화되었거나, 비교할 대상이 없으면 바로 위치 반환
        if (minSpawnDistance <= 0 || recentPositions == null || recentPositions.Count == 0)
        {
            return CalculatePositionOnCircle(spawnRadius, center);
        }

        Vector3 newPos;
        for (int i = 0; i < maxSpawnRetries; i++)
        {
            newPos = CalculatePositionOnCircle(spawnRadius, center);
            bool isTooClose = false;
            foreach (var pos in recentPositions)
            {
                if (Vector3.Distance(newPos, pos) < minSpawnDistance)
                {
                    isTooClose = true;
                    break;
                }
            }

            if (!isTooClose) return newPos; // 충분히 멀면 위치 확정
        }

        // 최대 시도 횟수를 초과하면 그냥 마지막 위치 반환 (안전장치)
        return CalculatePositionOnCircle(spawnRadius, center);
    }

    private Vector3 CalculatePositionOnCircle(float radius, Vector3 center)
    {
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float x = Mathf.Cos(randomAngle) * radius;
        float y = Mathf.Sin(randomAngle) * radius;
        return center + new Vector3(x, y, 0);
    }

    private GameObject CreateEnemy(EnemyType type)
    {
        Vector3 spawnPosition = GetRandomSpawnPosition(null);
        if (type == EnemyType.Boss)
        {
            spawnPosition = bossSpwanPoint == null ? GetRandomSpawnPosition(null) : bossSpwanPoint.position;
        }
        else if (type == EnemyType.MainBoss)
        {
            spawnPosition = mainBossSpwanPoint == null ? GetRandomSpawnPosition(null) : mainBossSpwanPoint.position;
        }

        GameObject prefab = enemyPrefabs[(int)type];
        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
        enemy.GetComponent<Enemy>().SetTaget(Target);
        enemy.GetComponent<Enemy>().SetPool(enemyPools[type]); // 자신이 속한 풀 저장
        return enemy;
    }

    private void OnGetEnemy(GameObject enemy)
    {
        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            enemyComponent.ResetState();
            Transform spawnTarget = planet2CoreAlive
                                    ? planet2Core != null ? planet2Core.transform : null
                                    : planet1Core != null ? planet1Core.transform : null;
            if (spawnTarget != null)
                enemyComponent.SetTaget(spawnTarget);
            activeEnemies.Add(enemyComponent);
        }
        enemy.SetActive(true);
    }

    private void OnReleaseEnemy(GameObject enemy)
    {
        enemy.SetActive(false);
        EnemyCount--;

        var e = enemy.GetComponent<Enemy>();
        if (e != null) activeEnemies.Remove(e);
    }

    private void OnDestroyEnemy(GameObject enemy) { }

    private IEnumerator SpawnWave()
    {
        if (currentWaveIndex >= waves.Length)
            yield break;

        isSpawning = true;
        // 중복되는 wave 로그 주석 처리
        // GameAnalyticsLogger.instance.LogWaveStart(Managers.Instance.core.CurrentHP);
        WaveSO currentWave = waves[currentWaveIndex];

        totalEnemiesInWave = currentWave.GetTotalEnemyCount();
        EnemyCount = totalEnemiesInWave;

        // 남은 스폰 수 초기화
        remainingSpawnCounts.Clear();
        foreach (var spawnInfo in currentWave.GetEnemySpawnInfos())
            remainingSpawnCounts[spawnInfo.enemyType] = spawnInfo.count;

        // 🔹 1. 보스 타입 분리
        List<EnemyType> bossTypes = new List<EnemyType> { EnemyType.Boss, EnemyType.MainBoss };

        // 한 스폰 간격 내에서 생성된 위치를 저장할 리스트
        List<Vector3> recentSpawnPositions = new List<Vector3>();

        // 🔹 2. 보스 먼저 스폰
        foreach (var bossType in bossTypes)
        {
            if (!remainingSpawnCounts.ContainsKey(bossType)) continue;

            int count = remainingSpawnCounts[bossType];
            for (int i = 0; i < count; i++)
            {
                if (bossType == EnemyType.MainBoss)
                {
                    mainbossHpSlider.gameObject.SetActive(true);
                    mainbossHpSlider.value = mainbossHpSlider.maxValue;
                }

                Vector3 spawnPos = (bossType == EnemyType.Boss) ? bossSpwanPoint.position : mainBossSpwanPoint.position;
                if (spawnPos == null) spawnPos = GetRandomSpawnPosition(null);

                var pool = enemyPools[bossType];
                GameObject bossObj = pool.Get();
                bossObj.transform.position = spawnPos;

                Enemy enemyComponent = bossObj.GetComponent<Enemy>();
                GameAnalyticsLogger.instance.LogEnemySpawn(
                    enemyComponent.enemyData.enemyType.ToString(),
                    enemyComponent.enemyNum++,
                    spawnPos.ToString());
                remainingSpawnCounts[bossType]--;

                yield return new WaitForSeconds(1f);
            }
        }

        // 🔹 3. 일반 적 스폰
        while (GetTotalRemainingSpawnsExceptBoss(bossTypes) > 0)
        {
            recentSpawnPositions.Clear(); // 매 간격마다 리스트 초기화

            int spawnCount = Random.Range(currentWave.minSpawnPerInterval, currentWave.maxSpawnPerInterval + 1);
            spawnCount = Mathf.Min(spawnCount, GetTotalRemainingSpawnsExceptBoss(bossTypes));

            for (int i = 0; i < spawnCount; i++)
            {
                EnemyType typeToSpawn = SelectRandomEnemyTypeExceptBoss(currentWave, bossTypes);
                if (typeToSpawn == (EnemyType)(-1)) continue;
                
                Vector3 spawnPos = GetRandomSpawnPosition(recentSpawnPositions);
                recentSpawnPositions.Add(spawnPos);
                
                SpawnEnemyFromPool(typeToSpawn, spawnPos);
            }

            yield return new WaitForSeconds(currentWave.spawnInterval);
        }

        isSpawning = false;
        currentWaveIndex++;
        hasTriggeredWaveClearAction = false;
    }

    private void SpawnEnemyFromPool(EnemyType type, Vector3 position)
    {
        var pool = enemyPools[type];
        GameObject enemyObj = pool.Get();
        enemyObj.transform.position = position;

        Enemy enemyComponent = enemyObj.GetComponent<Enemy>();
        GameAnalyticsLogger.instance.LogEnemySpawn(
            enemyComponent.enemyData.enemyType.ToString(),
            enemyComponent.enemyNum++,
            position.ToString());

        remainingSpawnCounts[type]--;
    }

    private int GetTotalRemainingSpawnsExceptBoss(List<EnemyType> bossTypes)
    {
        int total = 0;
        foreach (var kvp in remainingSpawnCounts)
            if (!bossTypes.Contains(kvp.Key))
                total += kvp.Value;
        return total;
    }

    private EnemyType SelectRandomEnemyTypeExceptBoss(WaveSO wave, List<EnemyType> bossTypes)
    {
        List<EnemyType> availableTypes = new List<EnemyType>();
        foreach (var kvp in remainingSpawnCounts)
        {
            if (kvp.Value > 0 && !bossTypes.Contains(kvp.Key))
                availableTypes.Add(kvp.Key);
        }

        if (availableTypes.Count == 0) return (EnemyType)(-1);
        return availableTypes[Random.Range(0, availableTypes.Count)];
    }

    private void OnDestroy()
    {
        if (planet2Core != null)
        {
            planet2Core.OnDie -= HandlePlanet2CoreDie;
            planet2Core.OnRevive -= HandlePlanet2CoreRevive;
        }
    }

    private void HandlePlanet2CoreDie()
    {
        planet2CoreAlive = false;
        if (planet1Core == null) return;

        Transform newTarget = planet1Core.transform;
        foreach (var e in activeEnemies)
        {
            if (!e) continue;
            e.isAttacking = false;
            e.attackTimer = 0f;
            e.SetTaget(newTarget);
        }
    }
    public void ForceStartNextWaveByCentral()
    {
        enabled = true;              // 안전: 혹시 꺼져 있으면 켜기
        holdAfterGate = false;
    }
    private void HandlePlanet2CoreRevive()
    {
        planet2CoreAlive = true;
    }

    /// <summary>
    /// 외부(플레이어의 스킵 버튼 등)에서 다음 웨이브 시작을 앞당기도록 요청합니다.
    /// 남은 카운트다운이 5초보다 클 경우, 5초로 설정합니다.
    /// </summary>
    public void RequestImmediateWaveStart()
    {
        // 웨이브 사이의 쉬는 시간(카운트다운 중)에만 작동합니다.
        if (IsBetweenWaves())
        {
            // 남은 시간이 5초 초과일 때만 작동합니다.
            if (countdown > 11f)
            {
                countdown = 11f;
            }
        }
    }
    private void OnDrawGizmos()
    {
        Camera mainCamera = Camera.main;
        float aspect = mainCamera != null ? mainCamera.aspect : 16f / 9f;
        float horizontalSize = maxCameraSize * aspect;
        float spawnRadius = Mathf.Max(maxCameraSize, horizontalSize) + spawnDistanceOffset;

        Gizmos.color = Color.red;
        int segments = 100;
        float angleStep = 360f / segments;

        Vector3 prevPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * spawnRadius;
            float y = Mathf.Sin(angle) * spawnRadius;
            Vector3 p = new Vector3(x, y, 0f);
            if (i > 0) Gizmos.DrawLine(prevPoint, p);
            prevPoint = p;
        }

        Gizmos.color = Color.yellow;
        float cameraRadius = Mathf.Max(maxCameraSize, horizontalSize);
        prevPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * cameraRadius;
            float y = Mathf.Sin(angle) * cameraRadius;
            Vector3 p = new Vector3(x, y, 0f);
            if (i > 0) Gizmos.DrawLine(prevPoint, p);
            prevPoint = p;
        }
    }
}
