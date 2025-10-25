using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

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

    private readonly HashSet<Enemy> activeEnemies = new();

    public int CurrentWaveIndex => currentWaveIndex;

    public Transform bossSpwanPoint;
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
    }

    private void Update()
    {
        if (isSpawning)
        {
            waveEnd = true;
            if (waveTimerText != null) waveTimerText.text = $"Wave {currentWaveIndex + 1}";
            if (enemyCountText != null) enemyCountText.text = $"Enemies: {EnemyCount}";
            if (miningInstructionText != null) { miningInstructionText.color = Color.red; miningInstructionText.text = "적이 오고 있다! 기지로 돌아가라!"; }
            return;
        }

        if (EnemyCount > 0 && !isSpawning)
        {
            if (enemyCountText != null) enemyCountText.text = $"Enemies: {EnemyCount}";
            if (miningInstructionText != null) { miningInstructionText.color = Color.red; miningInstructionText.text = "적이 오고 있다! 기지로 돌아가라!"; }
            return;
        }

        if (EnemyCount <= 0 && !isSpawning)
        {
            if (forceStartRequested) { 
                StartCoroutine(SpawnWave()); 
                countdown = timeBetweenWaves;
                isFirst = false; 
                forceStartRequested = false; 
                return; 
            }
            if (currentWaveIndex >= waves.Length)
            {
                if (waveEnd)
                {
                    GameAnalyticsLogger.instance.LogWaveComplete(Managers.Instance.core.CurrentHP);
                    GameAnalyticsLogger.instance.LogWaveComplete(Managers.Instance.core.CurrentHP);
                    GameAnalyticsLogger.instance.LogWaveResources(Managers.Instance.inventory.GetWaveResourceStats(currentWaveIndex));
                    GameAnalyticsLogger.instance.UpdateWave();
                    waveEnd = false;
                }

                if (waveTimerText != null) waveTimerText.text = "All Waves Completed!";
                if (enemyCountText != null) enemyCountText.text = "Victory!";
                if (miningInstructionText != null) miningInstructionText.text = "";
                return;
            }

            if (!hasTriggeredWaveClearAction)
            {
                hasTriggeredWaveClearAction = true;
                if (!isFirst)
                {
                    GameAnalyticsLogger.instance.LogWaveComplete(Managers.Instance.core.CurrentHP);
                    GameAnalyticsLogger.instance.LogWaveResources(Managers.Instance.inventory.GetWaveResourceStats(currentWaveIndex));
                    GameAnalyticsLogger.instance.UpdateWave();
                }
            }

            EnemyCount = 0;
            countdown -= Time.deltaTime;

            if (countdown <= 0f)
            {
                StartCoroutine(SpawnWave());
                countdown = timeBetweenWaves;
                isFirst = false;
            }
            else
            {
                if (waveTimerText != null) waveTimerText.text = $"Next Wave {currentWaveIndex + 1} In: {Mathf.Ceil(countdown)}";
                if (enemyCountText != null) enemyCountText.text = "Mining Phase";
                if (miningInstructionText != null)
                {
                    if (isFirst) miningInstructionText.text = "";
                    else { miningInstructionText.color = Color.green; miningInstructionText.text = "자원을 탐색하세요"; }

                    if (waveEnd)
                    {
                        Managers.Instance.inventory.ResetWaveStats();
                        waveEnd = false;
                    }
                }
            }
        }
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

    private Vector3 GetRandomSpawnPosition()
    {
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Camera mainCamera = Camera.main;
        float aspect = mainCamera != null ? mainCamera.aspect : 16f / 9f;
        float horizontalSize = maxCameraSize * aspect;
        float spawnRadius = Mathf.Max(maxCameraSize, horizontalSize) + spawnDistanceOffset;
        float x = Mathf.Cos(randomAngle) * spawnRadius;
        float y = Mathf.Sin(randomAngle) * spawnRadius;
        Vector3 center = spawnCenter
                     ? spawnCenter.position
                     : (planet2Core ? planet2Core.transform.position : transform.position);
        return center + new Vector3(x, y, 0f);
    }

    private GameObject CreateEnemy(EnemyType type)
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();

        if (type == EnemyType.Boss)
        {
            spawnPosition = bossSpwanPoint == null ? GetRandomSpawnPosition() : bossSpwanPoint.position;
        }
        else
        {
            spawnPosition = GetRandomSpawnPosition();
        }

        GameObject prefab = enemyPrefabs[(int)type];
        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
        enemy.GetComponent<Enemy>().SetTaget(Target);
        enemy.GetComponent<Enemy>().SetPool(enemyPools[type]); // 자신이 속한 풀 저장
        return enemy;
    }

    private void OnGetEnemy(GameObject enemy)
    {
        enemy.SetActive(true);
        Vector3 spawnPosition = GetRandomSpawnPosition();
        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent != null && enemyComponent.enemyData != null)
        {
            if (enemyComponent.enemyData.enemyType == EnemyType.Boss)
                spawnPosition = bossSpwanPoint == null ? GetRandomSpawnPosition() : bossSpwanPoint.position;
            else
                spawnPosition = GetRandomSpawnPosition();

            enemy.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            enemyComponent.ResetState();
            Transform spawnTarget = planet2CoreAlive
                                    ? planet2Core != null ? planet2Core.transform : null
                                    : planet1Core != null ? planet1Core.transform : null;
            if (spawnTarget != null)
                enemyComponent.SetTaget(spawnTarget);
        }
        if (enemyComponent != null) activeEnemies.Add(enemyComponent);
        GameAnalyticsLogger.instance.LogEnemySpawn(
            enemyComponent.enemyData.enemyType.ToString(),
            enemyComponent.enemyNum++,
            spawnPosition.ToString()
        );
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
        GameAnalyticsLogger.instance.LogWaveStart(Managers.Instance.core.CurrentHP);
        WaveSO currentWave = waves[currentWaveIndex];

        totalEnemiesInWave = currentWave.GetTotalEnemyCount();
        EnemyCount = totalEnemiesInWave;

        remainingSpawnCounts.Clear();
        EnemySpawnInfo[] spawnInfos = currentWave.GetEnemySpawnInfos();
        foreach (var s in spawnInfos)
            remainingSpawnCounts[s.enemyType] = s.count;

        while (GetTotalRemainingSpawns() > 0)
        {
            int spawnCount = Random.Range(currentWave.minSpawnPerInterval, currentWave.maxSpawnPerInterval + 1);
            spawnCount = Mathf.Min(spawnCount, GetTotalRemainingSpawns());

            for (int i = 0; i < spawnCount; i++)
            {
                EnemyType typeToSpawn = SelectRandomEnemyType(currentWave);
                if (typeToSpawn != (EnemyType)(-1))
                {
                    var pool = enemyPools[typeToSpawn];
                    pool.Get();
                    remainingSpawnCounts[typeToSpawn]--;
                }
            }

            yield return new WaitForSeconds(currentWave.spawnInterval);
        }

        isSpawning = false;
        currentWaveIndex++;
        hasTriggeredWaveClearAction = false;
    }

    private int GetTotalRemainingSpawns()
    {
        int total = 0;
        foreach (var count in remainingSpawnCounts.Values) total += count;
        return total;
    }

    private EnemyType SelectRandomEnemyType(WaveSO wave)
    {
        List<EnemyType> available = new List<EnemyType>();
        foreach (var kvp in remainingSpawnCounts)
            if (kvp.Value > 0) available.Add(kvp.Key);

        if (available.Count == 0) return (EnemyType)(-1);
        return available[Random.Range(0, available.Count)];
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
