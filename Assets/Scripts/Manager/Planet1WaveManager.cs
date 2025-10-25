using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
public class Planet1WaveManager : MonoBehaviour
{
    public static Planet1WaveManager Instance;

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
    [Header("UI")]
    public TMP_Text waveTimerText;
    public TMP_Text enemyCountText;
    public TMP_Text miningInstructionText; // 채굴 안내 텍스트

    [HideInInspector] public int EnemyCount = 0;
    private int totalEnemiesInWave = 0; // 현재 웨이브의 총 적 수

    // 각 EnemyType별 ObjectPool 관리용 딕셔너리
    public Dictionary<EnemyType, IObjectPool<GameObject>> enemyPools = new();

    // 현재 웨이브에서 각 적 타입별 남은 스폰 수
    private Dictionary<EnemyType, int> remainingSpawnCounts = new();
    
    // 읽기용 현재 웨이브 인덱스
    public int CurrentWaveIndex => currentWaveIndex;
    
    // 보스 스폰 위치
    public Transform bossSpwanPoint;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // enum 기반 풀 초기화
        foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
        {
            enemyPools[type] = CreatePool(type);
        }
        
        // 게임 시작 시 UI 텍스트 비우기
        if (waveTimerText != null)
            waveTimerText.text = "";
        if (enemyCountText != null)
            enemyCountText.text = "";
        if (miningInstructionText != null)
        {
            miningInstructionText.text = "";
            miningInstructionText.color = Color.green; // 초록색으로 설정
        }
    }

    private void Update()
    {
        // // 웨이브가 모두 끝났으면 더 이상 스폰하지 않음
        // if (currentWaveIndex >= waves.Length)
        // {
        //     if (EnemyCount <= 0)
        //     {
        //         waveTimerText.text = "All Waves Completed!";
        //         enemyCountText.text = "Victory!";
        //         if (miningInstructionText != null)
        //             miningInstructionText.text = "";
        //     }
        //     else
        //     {
        //         waveTimerText.text = $"Final Wave";
        //         enemyCountText.text = $"Enemies: {EnemyCount}";
        //         if (miningInstructionText != null)
        //             miningInstructionText.text = "";
        //     }
        //     return;
        // }

        // 스폰 중이면 웨이브 진행 중 표시
        if (isSpawning)
        {
            waveEnd = true;
            waveTimerText.text = $"Wave {currentWaveIndex + 1}";
            enemyCountText.text = $"Enemies: {EnemyCount}";
            if (miningInstructionText != null)
            {
                miningInstructionText.color = Color.red; // 빨간색으로 변경
                miningInstructionText.text = "적이 오고 있다! 기지로 돌아가라!";
            }
            return;
        }

        // 스폰이 끝났지만 적이 남아있으면 대기
        if (EnemyCount > 0 && !isSpawning)
        {
            enemyCountText.text = $"Enemies: {EnemyCount}";
            if (miningInstructionText != null)
            {
                miningInstructionText.color = Color.red; // 빨간색으로 변경
                miningInstructionText.text = "적이 오고 있다! 기지로 돌아가라!";
            }
            return;
        }

        // 적이 모두 죽고, 스폰도 끝났으면 카운트다운 시작
        if (EnemyCount <= 0 && !isSpawning)
        {
            // 마지막 웨이브까지 모두 클리어한 경우
            if (currentWaveIndex >= waves.Length)
            {
                if (waveEnd) // waveEnd 플래그로 한 번만 실행되도록 보장
                {
                    GameAnalyticsLogger.instance.LogWaveComplete(Managers.Instance.core.CurrentHP); 
                    // 리소스 로그는 아래 hasTriggeredWaveClearAction에서 처리
                    // [수정] 마지막 웨이브 클리어 시 로그 기록
                    GameAnalyticsLogger.instance.LogWaveComplete(Managers.Instance.core.CurrentHP);
                    GameAnalyticsLogger.instance.LogWaveResources(Managers.Instance.inventory.GetWaveResourceStats(currentWaveIndex));
                    GameAnalyticsLogger.instance.UpdateWave();
                    waveEnd = false;
                }

                waveTimerText.text = "All Waves Completed!";
                enemyCountText.text = "Victory!";
                if (miningInstructionText != null)
                {
                    miningInstructionText.text = "";
                }
                return; // 모든 로직 종료
            }

            if (!hasTriggeredWaveClearAction)
            {
                hasTriggeredWaveClearAction = true;
                if (!isFirst) // 첫 웨이브 시작 전(isFirst=true)에는 로그를 기록하지 않음
                {
                    // [Log] 이전 웨이브 완료 로그 및 자원 통계 기록
                    GameAnalyticsLogger.instance.LogWaveComplete(Managers.Instance.core.CurrentHP);
                    GameAnalyticsLogger.instance.LogWaveResources(Managers.Instance.inventory.GetWaveResourceStats(currentWaveIndex));
                    GameAnalyticsLogger.instance.UpdateWave();
                }
            }
            EnemyCount = 0;
            countdown -= Time.deltaTime;

            // 카운트다운이 끝나면 다음 웨이브 시작
            if (countdown <= 0f)
            {
                StartCoroutine(SpawnWave());
                countdown = timeBetweenWaves;
                isFirst = false; // 첫 번째 웨이브가 시작되면 더 이상 첫 시작이 아님
            }
            else
            {
                waveTimerText.text = $"Next Wave {currentWaveIndex + 1} In: {Mathf.Ceil(countdown)}";
                enemyCountText.text = "Mining Phase";
                if (miningInstructionText != null)
                {
                    if (isFirst)
                    {
                        // 첫 시작 시에는 자원 탐색 메시지 표시 안 함
                        miningInstructionText.text = "";
                    }
                    else
                    {
                        // 웨이브 사이에는 초록색으로 자원 탐색 메시지 표시
                        miningInstructionText.color = Color.green;
                        miningInstructionText.text = "자원을 탐색하세요";
                    }

                    if (waveEnd)
                    {
                        // GameAnalyticsLogger.instance.waveCount = CurrentWaveIndex; [각주 2]
                        GameAnalyticsLogger.instance.LogPlayerDefend(
                            GameAnalyticsLogger.instance.playerBulletCount,
                            GameAnalyticsLogger.instance.playerBulletHitCount);
                        GameAnalyticsLogger.instance.playerBulletCount = 0;
                        GameAnalyticsLogger.instance.playerBulletHitCount = 0;
                        // [Log] 웨이브 시작 시 인벤토리 통계 초기화
                        Managers.Instance.inventory.ResetWaveStats();
                        waveEnd = false;
                    }
                }
            }
        }
    }

    // 특정 타입의 풀 생성
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

    // 카메라 밖 원의 랜덤 위치 계산
    private Vector3 GetRandomSpawnPosition()
    {
        // 랜덤 각도 (0 ~ 360도)
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        // 카메라의 aspect ratio를 고려한 실제 반지름 계산
        // orthographic size는 세로 방향의 절반 크기
        Camera mainCamera = Camera.main;
        float aspect = mainCamera != null ? mainCamera.aspect : 16f / 9f;
        
        // 카메라 경계를 벗어나는 거리 (더 긴 축 기준 + offset)
        float horizontalSize = maxCameraSize * aspect;
        float spawnRadius = Mathf.Max(maxCameraSize, horizontalSize) + spawnDistanceOffset;
        
        // 원의 둘레 위 랜덤 포인트
        float x = Mathf.Cos(randomAngle) * spawnRadius;
        float y = Mathf.Sin(randomAngle) * spawnRadius;
        
        // 월드 좌표 반환 (중심은 0,0)
        return new Vector3(x, y, 0f);
    }

    private GameObject CreateEnemy(EnemyType type)
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();

        if (type == EnemyType.Boss) // 보스 스폰 위치 처리
        {
            if (bossSpwanPoint == null)
            {
                spawnPosition = GetRandomSpawnPosition(); // 보스 스폰 포인트가 없으면 랜덤 위치
            }
            else
            {
                spawnPosition = bossSpwanPoint.position; // 보스 스폰 포인트 사용
            }
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
        // 체력 및 상태 초기화
        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent != null && enemyComponent.enemyData != null)
        {
            if (enemyComponent.enemyData.enemyType == EnemyType.Boss) // 보스 스폰 위치 처리
            {
                if (bossSpwanPoint == null)
                {
                    spawnPosition = GetRandomSpawnPosition();
                }
                else
                {
                    spawnPosition = bossSpwanPoint.position;
                }
            }
            else
            {
                spawnPosition = GetRandomSpawnPosition();
            }
            enemy.transform.SetPositionAndRotation(
                spawnPosition,
                Quaternion.identity
            );
            
            enemyComponent.ResetState(); // 모든 상태 초기화
        }
        GameAnalyticsLogger.instance.LogEnemySpawn(enemyComponent.enemyData.enemyType.ToString()
            ,enemyComponent.enemyNum++ , spawnPosition.ToString());

        // 스폰 시에는 카운트 증가 안 함 (미리 설정된 값 사용)
    }

    private void OnReleaseEnemy(GameObject enemy)
    {
        enemy.SetActive(false);
        EnemyCount--; // 적이 죽을 때마다 카운트 감소
    }

    private void OnDestroyEnemy(GameObject enemy)
    {
        // EnemyCount--;
    }

    private IEnumerator SpawnWave()
    {
        if (currentWaveIndex >= waves.Length)
            yield break;
        
        isSpawning = true;
        // [Log] 웨이브 시작 로그 출력 
        GameAnalyticsLogger.instance.LogWaveStart(Managers.Instance.core.CurrentHP);
        WaveSO currentWave = waves[currentWaveIndex];

        // 현재 웨이브의 총 적 수 계산 및 EnemyCount 설정
        totalEnemiesInWave = currentWave.GetTotalEnemyCount();
        EnemyCount = totalEnemiesInWave; // 전체 적 수로 시작
        
        // 현재 웨이브의 남은 스폰 수 초기화
        remainingSpawnCounts.Clear();
        EnemySpawnInfo[] spawnInfos = currentWave.GetEnemySpawnInfos();
        foreach (var spawnInfo in spawnInfos)
        {
            remainingSpawnCounts[spawnInfo.enemyType] = spawnInfo.count;
        }

        // 모든 적이 스폰될 때까지 반복
        while (GetTotalRemainingSpawns() > 0)
        {
            // 이번 간격에 스폰할 적의 수 결정 (min ~ max 사이)
            int spawnCount = Random.Range(
                currentWave.minSpawnPerInterval, 
                currentWave.maxSpawnPerInterval + 1
            );

            // 남은 적 수보다 많이 스폰하지 않도록 제한
            spawnCount = Mathf.Min(spawnCount, GetTotalRemainingSpawns());

            // 동시에 여러 적 스폰
            for (int i = 0; i < spawnCount; i++)
            {
                EnemyType typeToSpawn = SelectRandomEnemyType(currentWave);
                if (typeToSpawn != (EnemyType)(-1)) // 유효한 타입이면
                {
                    var pool = enemyPools[typeToSpawn];
                    pool.Get();
                    remainingSpawnCounts[typeToSpawn]--;
                }
            }

            // 다음 스폰까지 대기
            yield return new WaitForSeconds(currentWave.spawnInterval);
        }

        isSpawning = false;
        currentWaveIndex++;
        hasTriggeredWaveClearAction = false;
    }

    // 남은 총 스폰 수 계산
    private int GetTotalRemainingSpawns()
    {
        int total = 0;
        foreach (var count in remainingSpawnCounts.Values)
        {
            total += count;
        }
        return total;
    }

    // 남은 스폰 수가 있는 타입 중 랜덤 선택
    private EnemyType SelectRandomEnemyType(WaveSO wave)
    {
        // 스폰 가능한 적 타입만 필터링
        List<EnemyType> availableTypes = new List<EnemyType>();
        
        // remainingSpawnCounts에서 남은 수가 있는 타입만 추가
        foreach (var kvp in remainingSpawnCounts)
        {
            if (kvp.Value > 0)
            {
                availableTypes.Add(kvp.Key);
            }
        }

        if (availableTypes.Count == 0)
            return (EnemyType)(-1); // 스폰 가능한 적이 없음

        // 단순 랜덤 선택
        return availableTypes[Random.Range(0, availableTypes.Count)];
    }

    // Scene 뷰에서 스폰 범위를 시각화
    private void OnDrawGizmos()
    {
        // 카메라의 aspect ratio를 고려한 실제 반지름 계산
        Camera mainCamera = Camera.main;
        float aspect = mainCamera != null ? mainCamera.aspect : 16f / 9f;
        
        // 카메라 경계를 벗어나는 거리 (더 긴 축 기준 + offset)
        float horizontalSize = maxCameraSize * aspect;
        float spawnRadius = Mathf.Max(maxCameraSize, horizontalSize) + spawnDistanceOffset;
        
        // 기즈모 색상 설정
        Gizmos.color = Color.red;
        
        // 원을 그리기 위해 선분들로 근사
        int segments = 100;
        float angleStep = 360f / segments;
        
        Vector3 prevPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * spawnRadius;
            float y = Mathf.Sin(angle) * spawnRadius;
            Vector3 point = new Vector3(x, y, 0f);
            
            if (i > 0)
            {
                Gizmos.DrawLine(prevPoint, point);
            }
            
            prevPoint = point;
        }
        
        // 카메라 경계도 표시 (선택적)
        Gizmos.color = Color.yellow;
        float cameraRadius = Mathf.Max(maxCameraSize, horizontalSize);
        
        prevPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * cameraRadius;
            float y = Mathf.Sin(angle) * cameraRadius;
            Vector3 point = new Vector3(x, y, 0f);
            
            if (i > 0)
            {
                Gizmos.DrawLine(prevPoint, point);
            }
            
            prevPoint = point;
        }
    }
}
