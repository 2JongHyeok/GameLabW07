using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

[DefaultExecutionOrder(-10)]
public class Planet1WaveManager : MonoBehaviour
{
    public static Planet1WaveManager Instance;

    [Header("Wave Pre-start Delays (sec)")]
    [Tooltip("각 웨이브 시작 '직전'에 기다릴 시간(초). 비어있거나 음수면 defaultPreDelay 사용")]
    [SerializeField] private List<float> preStartDelays = new List<float>();

    [Tooltip("preStartDelays에 항목이 없거나 음수일 때 사용할 기본 지연값(초)")]
    [SerializeField] private float defaultPreDelay = 5f;

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
    private bool waveGuard = false;

    [Header("UI")]
    public TMP_Text waveTimerText;
    public TMP_Text enemyCountText;
    public TMP_Text miningInstructionText; // 채굴 안내 텍스트
    public GameObject GameClearPanel;
    public Slider bossHpSlider; // 슬라이더 오브젝트
    public Slider mainBossHpSlider;

    [HideInInspector] public int EnemyCount = 0;
    private int totalEnemiesInWave = 0; // 현재 웨이브의 총 적 수

    [SerializeField] private bool holdAfterGate = false;
    private bool forceStartRequested = false;
    [SerializeField] private bool gateAutoHoldArmed = false;
    // [API] 추가: 중앙에서 게이트 홀드 토글

    [Header("Central Sync (P1)")]
    public bool AllWavesCompleted => currentWaveIndex >= waves.Length;

    // 현재 웨이브와 다음 웨이브 사이(= 적 전멸 && 스폰 종료 && 아직 마지막 웨이브 아님)
    public bool IsBetweenWaves() => (EnemyCount <= 0 && !isSpawning && currentWaveIndex < waves.Length);

    // 읽기 전용: P1의 기본 쿨다운 초
    public float TimeBetweenWaves => timeBetweenWaves;

    // 중앙에서 카운트다운을 잠그고(대기), 동시에 시작 신호가 오면 해제한다
    private bool countdownLockedByCentral = false;
    private bool countdownArmedByCentral = false; // 중앙이 seconds를 넣어줬다는 표시

    [Header("WaveAlert")]
    [SerializeField] private Image alertImage;
    private bool isAlert = false;
    private bool alertOnce = true;
    public TMP_Text textMesh; // 효과를 적용할 TextMeshProUGUI 컴포넌트
    private Color originalColor;     // 원래 색상
    private Vector3 originalScale;   // 원래 크기



    public float GetPreDelayForWaveIndex(int waveIndex)
    {
        if (preStartDelays != null &&
            waveIndex >= 0 &&
            waveIndex < preStartDelays.Count &&
            preStartDelays[waveIndex] >= 0f)
            return preStartDelays[waveIndex];

        return defaultPreDelay;
    }

    // [추가] "다음에 시작될" 웨이브의 인덱스 = currentWaveIndex
    public float GetUpcomingPreDelay()
    {
        int upcoming = waveGuard ? currentWaveIndex + 1 : currentWaveIndex;
        return GetPreDelayForWaveIndex(upcoming);
    }
    public void LockCountdownByCentral(bool v)
    {
        countdownLockedByCentral = v;
        if (v) countdownArmedByCentral = false; // 잠글 땐 무장 해제
    }

    public void StartSimulCountdownFromCentral(float seconds)
    {
        countdown = seconds;
        countdownArmedByCentral = true; // 이제 카운트다운을 돌려도 됨(해제 신호와 함께 사용)
    }
    public void PauseByCentral() => enabled = false;
    public void SetGateHold(bool v) => holdAfterGate = v;

    // 각 EnemyType별 ObjectPool 관리용 딕셔너리
    public Dictionary<EnemyType, IObjectPool<GameObject>> enemyPools = new();

    // 현재 웨이브에서 각 적 타입별 남은 스폰 수
    private Dictionary<EnemyType, int> remainingSpawnCounts = new();

    // 읽기용 현재 웨이브 인덱스
    public int CurrentWaveIndex => currentWaveIndex;

    // 보스 스폰 위치
    public Transform bossSpwanPoint;
    public Transform mainBossSpwanPoint;

    public bool isAfterBossWave = false;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        alertImage.color = new Color(alertImage.color.r, alertImage.color.g, alertImage.color.b, 0f);

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
        
        // 보스 체력바 초기화
        bossHpSlider.gameObject.SetActive(false);
        bossHpSlider.value = bossHpSlider.maxValue;
        
        mainBossHpSlider.gameObject.SetActive(false);
        mainBossHpSlider.value = mainBossHpSlider.maxValue;
        
        if (GameClearPanel != null)
            GameClearPanel.SetActive(false);
        
        countdown = GetPreDelayForWaveIndex(0);
        
        // [추가된 부분]
        // 1. textMesh가 인스펙터에서 할당되지 않았다면 waveTimerText를 사용
        if (textMesh == null)
        {
            textMesh = waveTimerText;
        }

        // 2. textMesh의 초기 상태(색상, 크기)를 저장
        if (textMesh != null)
        {
            originalColor = textMesh.color;
            originalScale = textMesh.transform.localScale;
        }
        else
        {
            Debug.LogError("HighlightEffect를 적용할 TextMeshProUGUI가 할당되지 않았습니다!");
        }
    }

    private void Update()
    {
        // 스폰 중이면 웨이브 진행 중 표시
        if (isSpawning)
        {
            waveEnd = true;
            if (waveTimerText != null) waveTimerText.text = $"Wave {currentWaveIndex + 1}";
            if (enemyCountText != null) enemyCountText.text = $"Planet : {EnemyCount}";
            if (miningInstructionText != null)
            {
                miningInstructionText.color = Color.red; // 빨간색으로 변경
                miningInstructionText.text = "적의 공격이다! 기지로 돌아가라!";
                if (currentWaveIndex >= 4)
                {
                    miningInstructionText.text = "적의 공격이다! 즉시 기지로 복귀하라!";
                }
            }
            return;
        }
        // 스폰이 끝났지만 적이 남아있으면 대기
        if (EnemyCount > 0 && !isSpawning)
        {
            if (enemyCountText != null) enemyCountText.text = $"Planet : {EnemyCount}";
            if (miningInstructionText != null)
            {
                miningInstructionText.color = Color.red; // 빨간색으로 변경
                miningInstructionText.text = "적의 공격이다! 기지로 돌아가라!";

                if (currentWaveIndex >= 4)
                {
                    miningInstructionText.text = "적의 공격이다! 즉시 기지로 복귀하라!";
                }
            }
            return;
        }


        // 적이 모두 죽고, 스폰도 끝났으면 카운트다운 시작
        if (EnemyCount <= 0 && !isSpawning)
        {
            //  "모든 웨이브 완료" 검사를 가장 먼저 수행합니다.
            // (이전 프레임에서 waveGuard에 의해 인덱스가 증가한 경우)
            if (currentWaveIndex >= waves.Length)
            {
                if (waveEnd) // waveEnd 플래그로 한 번만 실행되도록 보장
                {
                    // [수정] 마지막 웨이브의 완료 로그는 waveGuard 블록에서 이미 기록되었으므로 여기서는 호출하지 않습니다.
                    GameAnalyticsLogger.instance.UpdateWave();
                    waveEnd = false;
                }

                if (waveTimerText != null) waveTimerText.text = "All Waves Completed!";
                if (enemyCountText != null) enemyCountText.text = "Victory!";
                if (miningInstructionText != null) miningInstructionText.text = "";

                // 보스 체력바가 남아있을 수 있으므로 확실히 숨김
                bossHpSlider.gameObject.SetActive(false);
                mainBossHpSlider.gameObject.SetActive(false);

                if (GameClearPanel != null)
                    GameClearPanel.SetActive(true);

                return; // 모든 로직 종료
            }

            //  웨이브 인덱스 증가 로직을 홀드 로직보다 먼저 수행합니다.
            // (hasTriggeredWaveClearAction 로그 로직도 이 안으로 이동)
            if (waveGuard)
            {
                // [Log] 이전 웨이브 완료 로그 (인덱스 증가 *전*의 값으로 기록)
                if (!hasTriggeredWaveClearAction)
                {
                    hasTriggeredWaveClearAction = true;
                    if (!isFirst) // 첫 웨이브 시작 전(isFirst=true)에는 로그를 기록하지 않음
                    {
                        GameAnalyticsLogger.instance.LogWaveComplete(Managers.Instance.core.CurrentHP);
                        GameAnalyticsLogger.instance.LogWaveResources(Managers.Instance.inventory.GetWaveResourceStats(currentWaveIndex));
                        GameAnalyticsLogger.instance.UpdateWave();
                    }
                }

                currentWaveIndex++; // *여기서* 마지막 웨이브 인덱스가 (Length)가 됩니다.

                if (WaveManager.Instance == null || !WaveManager.Instance.IsCombinedPhase)
                    countdown = GetPreDelayForWaveIndex(currentWaveIndex);

                waveGuard = false;
            }

            // waveGuard에서 인덱스가 방금 증가했다면, "모든 웨이브 완료" 상태가 되었는지 확인.
            if (currentWaveIndex >= waves.Length)
            {
                return;
            }

            if (currentWaveIndex == 4)
            {
                if (!gateAutoHoldArmed)
                {
                    bossHpSlider.gameObject.SetActive(false);
                    bossHpSlider.value = bossHpSlider.maxValue;
                    holdAfterGate = true;
                    gateAutoHoldArmed = true;
                    if (waveTimerText) waveTimerText.text = "보스의 코어로 콜로니를 건설하세요";
                    // [수정] miningInstructionText도 여기서 함께 설정합니다.
                    if (miningInstructionText)
                    {
                        miningInstructionText.color = Color.green;
                        miningInstructionText.text = "상단에 보스의 코어가 나타났습니다";
                    }
                    if (enemyCountText) enemyCountText.text = "Mining Phase";
                    isAfterBossWave = true;
                    return; // 카운트다운 시작 자체를 막아 즉시 대기 상태
                }
                if (holdAfterGate)
                {
                    bossHpSlider.gameObject.SetActive(false);
                    bossHpSlider.value = bossHpSlider.maxValue;
                    if (waveTimerText) waveTimerText.text = "보스의 코어로 콜로니를 건설하세요";
                    // [수정] miningInstructionText도 여기서 함께 설정합니다.
                    if (miningInstructionText)
                    {
                        miningInstructionText.color = Color.green;
                        miningInstructionText.text = "상단에 보스의 코어가 나타났습니다";
                    }
                    if (enemyCountText) enemyCountText.text = "Mining Phase";
                    return; // 여전히 대기
                }
            }

            // 중앙 동기화 대기
            if (WaveManager.Instance != null
                && WaveManager.Instance.IsCombinedPhase
                && countdownLockedByCentral
                && !countdownArmedByCentral)
            {
                if (waveTimerText != null) waveTimerText.text = "Waiting other planet...";
                if (enemyCountText != null) enemyCountText.text = "Mining Phase";
                return;
            }

            // 웨이브 종료 후 보스 체력바 비활성화 및 초기화
            bossHpSlider.gameObject.SetActive(false);
            bossHpSlider.value = bossHpSlider.maxValue;

            mainBossHpSlider.gameObject.SetActive(false);
            mainBossHpSlider.value = mainBossHpSlider.maxValue;

            // ['hasTriggeredWaveClearAction' 관련 로직은 waveGuard 블록 안으로 이동했습니다.
            EnemyCount = 0; // 혹시 모르니 0으로 유지
            countdown -= Time.deltaTime;

            if (countdown <= 10 && countdown >= 5)
            {

                // StartCoroutine(FadeInAndOut(alertImage, 1f, 1f));
                StartCoroutine(HighlightEffect());
            }

            // 카운트다운이 끝나면 다음 웨이브 시작
            if (countdown <= 0f)
            {
                if (forceStartRequested)
                {
                    StartCoroutine(SpawnWave());

                    isFirst = false;
                    forceStartRequested = false;

                    // waveGuard = true; 를 SpawnWave() 호출 시 항상 설정하도록 통일
                    waveGuard = true;
                    return;
                }
                bool inCombined = (WaveManager.Instance != null && WaveManager.Instance.IsCombinedPhase);
                if (inCombined && !countdownArmedByCentral)
                    return;

                // 3) 정상 시작
                StartCoroutine(SpawnWave());
                if (inCombined) countdownArmedByCentral = false; // arm 토큰 소모

                isFirst = false;
                waveGuard = true; // 다음 웨이브가 "시작"했으니, "클리어" 후 인덱스 증가가 가능하도록 설정
                return;
            }
            else
            {
                if (waveTimerText != null) waveTimerText.text = $"Next Wave {currentWaveIndex + 1} In: {Mathf.Ceil(countdown)}";
                if (enemyCountText != null) enemyCountText.text = "Mining Phase";
                if (miningInstructionText != null)
                {
                    // 웨이브 사이에는 초록색으로 자원 탐색 메시지 표시
                    if (currentWaveIndex == 3) // 4 웨이브 직전
                    {
                        miningInstructionText.color = Color.yellow; // 경고의 의미로 노란색 유지
                        miningInstructionText.text = "강력한 적이 출현합니다. 자원을 탐색하세요";
                    }
                    else if (currentWaveIndex == 7) // 8 웨이브 직전
                    {
                        miningInstructionText.color = Color.yellow; // 경고의 의미로 노란색 유지
                        miningInstructionText.text = "적의 총공세가 다가옵니다. 자원을 탐색하세요";
                    }
                    else
                    {
                        // 일반 웨이브 사이에는 초록색으로 자원 탐색 메시지 표시
                        miningInstructionText.color = Color.green;
                        miningInstructionText.text = "자원을 탐색하세요";
                    }

                    if (waveEnd)
                    {
                        // [Log] 웨이브 시작 시 인벤토리 통계 초기화
                        Managers.Instance.inventory.ResetWaveStats();
                        waveEnd = false;
                    }
                }
            }
        }
    }
    
    public IEnumerator FadeInAndOut(Image image, float fadeInTime, float fadeOutTime)
    {
        if (isAlert) yield break;

        isAlert = true;

        // 1. 페이드 인 (Alpha 0 -> 1)
        float elapsedTime = 0f;
        Color originalColor = image.color;

        // 현재 알파 값에서 1을 향해 보간
        float startAlpha = originalColor.a;

        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, 1f, elapsedTime / fadeInTime);
            image.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
            yield return null; // 다음 프레임까지 대기
        }

        // 정확히 1로 설정
        image.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);

        // 2. 페이드 아웃 (Alpha 1 -> 0)
        elapsedTime = 0f; // 경과 시간 리셋

        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
            image.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
            yield return null; // 다음 프레임까지 대기
        }

        // 정확히 0으로 설정
        image.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

        isAlert = false;
    }
    
     /// <summary>
    /// 텍스트 강조 효과를 시작하는 함수입니다.
    /// </summary>
    private IEnumerator HighlightEffect()
    {
        
        if(isAlert) yield break;
        
        isAlert = true;
        
        float duration = 0.5f;       // 전체 효과 지속 시간
        float scaleMultiplier = 1.5f; // 확대될 크기 배율
        float shakeMagnitude = 5.0f; // 흔들림의 강도

        Vector3 originalPosition = textMesh.transform.localPosition; // 원래 위치 저장

        float elapsed = 0.0f;

        // 효과 시작: 커지고, 흔들리고, 빨개지기
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration / 2);

            // 색상 변경 (원래 색 -> 빨간색)
            textMesh.color = Color.Lerp(originalColor, Color.red, progress);

            // 크기 변경 (원래 크기 -> 목표 크기)
            textMesh.transform.localScale = Vector3.Lerp(originalScale, originalScale * scaleMultiplier, progress);

            // 위치 흔들기
            float x = originalPosition.x + Random.Range(-1f, 1f) * shakeMagnitude;
            float y = originalPosition.y + Random.Range(-1f, 1f) * shakeMagnitude;
            textMesh.transform.localPosition = new Vector3(x, y, originalPosition.z);

            yield return null; // 다음 프레임까지 대기
        }

        elapsed = 0.0f; // 시간 초기화

        // 효과 종료: 원래 상태로 복구
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration / 2);

            // 색상 복구 (빨간색 -> 원래 색)
            textMesh.color = Color.Lerp(Color.red, originalColor, progress);

            // 크기 복구 (목표 크기 -> 원래 크기)
            textMesh.transform.localScale = Vector3.Lerp(originalScale * scaleMultiplier, originalScale, progress);

            // 흔들리던 위치를 부드럽게 원래 위치로 복구
            textMesh.transform.localPosition = Vector3.Lerp(textMesh.transform.localPosition, originalPosition, progress);


            yield return null; // 다음 프레임까지 대기
        }

        // 모든 효과가 끝난 후, 최종적으로 원래 상태로 고정
        textMesh.color = originalColor;
        textMesh.transform.localScale = originalScale;
        textMesh.transform.localPosition = originalPosition;
        
        isAlert = false; // 효과 종료 표시
    }

    public void ForceStartNextWaveByCentral()
    {
        enabled = true;              // 안전: 혹시 꺼져 있으면 켜기
        holdAfterGate = false;
    }
    public void ResumeNextWaveByCentral()
    {
        enabled = true;
        countdown = 0f; // 다음 Update에서 즉시 SpawnWave()
    }
    
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
    private Vector3 GetRandomSpawnPosition(List<Vector3> recentPositions)
    {
        Camera mainCamera = Camera.main;
        float aspect = mainCamera != null ? mainCamera.aspect : 16f / 9f;
        float horizontalSize = maxCameraSize * aspect;
        float spawnRadius = Mathf.Max(maxCameraSize, horizontalSize) + spawnDistanceOffset;

        // 최소 거리 체크 기능이 비활성화되었거나, 비교할 대상이 없으면 바로 위치 반환
        if (minSpawnDistance <= 0 || recentPositions == null || recentPositions.Count == 0)
        {
            return CalculatePositionOnCircle(spawnRadius);
        }

        Vector3 newPos;
        for (int i = 0; i < maxSpawnRetries; i++)
        {
            newPos = CalculatePositionOnCircle(spawnRadius);
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
        return CalculatePositionOnCircle(spawnRadius);
    }

    private Vector3 CalculatePositionOnCircle(float radius)
    {
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float x = Mathf.Cos(randomAngle) * radius;
        float y = Mathf.Sin(randomAngle) * radius;
        return new Vector3(x, y, 0);
    }

    private GameObject CreateEnemy(EnemyType type)
    {
        Vector3 spawnPosition = GetRandomSpawnPosition(null);
        

        if (type == EnemyType.Boss) // 보스 스폰 위치 처리
        {
            if (bossSpwanPoint == null)
            {
                spawnPosition = GetRandomSpawnPosition(null); // 보스 스폰 포인트가 없으면 랜덤 위치
            }
            else
            {
                bossHpSlider.gameObject.SetActive(true); // 보스 체력바 활성화
                bossHpSlider.value = bossHpSlider.maxValue;
                spawnPosition = bossSpwanPoint.position; // 보스 스폰 포인트 사용
            }
        }
        else if (type == EnemyType.MainBoss)
        {
            if(mainBossSpwanPoint == null)
            {
                spawnPosition = GetRandomSpawnPosition(null);
            }
            else
            {
                mainBossHpSlider.gameObject.SetActive(true);
                mainBossHpSlider.value = mainBossHpSlider.maxValue;
                spawnPosition = mainBossSpwanPoint.position;
            }
        }
        else
        {
            spawnPosition = GetRandomSpawnPosition(null);
        }
        GameObject prefab = enemyPrefabs[(int)type];
        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
        enemy.GetComponent<Enemy>().SetTaget(Target);
        enemy.GetComponent<Enemy>().SetPool(enemyPools[type]); // 자신이 속한 풀 저장
        return enemy;
    }

    private void OnGetEnemy(GameObject enemy)
    {
        // 위치 설정은 SpawnWave에서 처리하므로, 여기서는 상태 리셋과 활성화만 담당합니다.
        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            enemyComponent.ResetState();
        }
        enemy.SetActive(true);
    }
    private void OnReleaseEnemy(GameObject enemy)
    {
        enemy.SetActive(false);
        EnemyCount--; // 적이 죽을 때마다 카운트 감소
    }
    private void OnDestroyEnemy(GameObject enemy) { }

    private IEnumerator SpawnWave()
    {
        if (currentWaveIndex >= waves.Length)
            yield break;

        isSpawning = true;
        GameAnalyticsLogger.instance.LogWaveStart(Managers.Instance.core.CurrentHP);
        WaveSO currentWave = waves[currentWaveIndex];

        // 현재 웨이브의 총 적 수 계산 및 EnemyCount 설정
        totalEnemiesInWave = currentWave.GetTotalEnemyCount();
        EnemyCount = totalEnemiesInWave;

        // 남은 스폰 수 초기화
        remainingSpawnCounts.Clear();
        EnemySpawnInfo[] spawnInfos = currentWave.GetEnemySpawnInfos();
        foreach (var spawnInfo in spawnInfos)
            remainingSpawnCounts[spawnInfo.enemyType] = spawnInfo.count;

        // 🔹 1. 보스 타입 분리
        List<EnemyType> bossTypes = new List<EnemyType> { EnemyType.Boss, EnemyType.MainBoss };

        // 한 스폰 간격 내에서 생성된 위치를 저장할 리스트
        List<Vector3> recentSpawnPositions = new List<Vector3>();

        // 🔹 2. 일반 적 먼저 스폰
        while (GetTotalRemainingSpawnsExceptBoss(bossTypes) > 0)
        {
            recentSpawnPositions.Clear(); // 매 간격마다 리스트 초기화

            int spawnCount = Random.Range(currentWave.minSpawnPerInterval, currentWave.maxSpawnPerInterval + 1);
            spawnCount = Mathf.Min(spawnCount, GetTotalRemainingSpawnsExceptBoss(bossTypes));

            for (int i = 0; i < spawnCount; i++)
            {
                EnemyType typeToSpawn = SelectRandomEnemyTypeExceptBoss(currentWave, bossTypes);
                if (typeToSpawn == (EnemyType)(-1)) continue;
                
                // 위치를 먼저 계산합니다.
                Vector3 spawnPos = GetRandomSpawnPosition(recentSpawnPositions);
                recentSpawnPositions.Add(spawnPos); // 다음 적이 참고하도록 위치 추가
                
                var pool = enemyPools[typeToSpawn];
                GameObject enemyObj = pool.Get(); // OnGetEnemy가 호출되어 상태가 리셋됩니다.
                enemyObj.transform.position = spawnPos; // 계산된 위치를 적용합니다.

                // [수정] 로그 기록을 이 위치로 이동
                Enemy enemyComponent = enemyObj.GetComponent<Enemy>();
                GameAnalyticsLogger.instance.LogEnemySpawn(
                    enemyComponent.enemyData.enemyType.ToString(),
                    enemyComponent.enemyNum++, // enemyNum은 Enemy.cs의 ResetState에서 초기화됩니다.
                    spawnPos.ToString());
                remainingSpawnCounts[typeToSpawn]--;
            }

            // 다음 스폰까지 대기
            yield return new WaitForSeconds(currentWave.spawnInterval);
        }

        // 🔹 3. 일반 적이 다 나왔으면 → 보스 스폰
        foreach (var bossType in bossTypes)
        {
            if (!remainingSpawnCounts.ContainsKey(bossType)) continue;

            int count = remainingSpawnCounts[bossType];
            for (int i = 0; i < count; i++)
            {
                if (bossType == EnemyType.Boss)
                {
                    bossHpSlider.gameObject.SetActive(true);
                    bossHpSlider.value = bossHpSlider.maxValue;
                }
                else if (bossType == EnemyType.MainBoss)
                {
                    mainBossHpSlider.gameObject.SetActive(true);
                    mainBossHpSlider.value = mainBossHpSlider.maxValue;
                }

                // 보스는 지정된 위치를 사용하므로 겹침 방지 로직이 필요 없습니다.
                Vector3 spawnPos = (bossType == EnemyType.Boss) ? bossSpwanPoint.position : mainBossSpwanPoint.position;
                if (spawnPos == null) spawnPos = GetRandomSpawnPosition(null); // 안전장치

                var pool = enemyPools[bossType];
                GameObject bossObj = pool.Get();
                bossObj.transform.position = spawnPos;

                // [수정] 보스 로그 기록 추가
                Enemy enemyComponent = bossObj.GetComponent<Enemy>();
                GameAnalyticsLogger.instance.LogEnemySpawn(
                    enemyComponent.enemyData.enemyType.ToString(),
                    enemyComponent.enemyNum++,
                    spawnPos.ToString());
                remainingSpawnCounts[bossType]--;

                // 보스들끼리 텀 주고 싶다면 약간 대기
                yield return new WaitForSeconds(1f);
            }
        }

        isSpawning = false;
        hasTriggeredWaveClearAction = false;
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

        if (availableTypes.Count == 0)
            return (EnemyType)(-1);

        return availableTypes[Random.Range(0, availableTypes.Count)];
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
            Vector3 point = new Vector3(x, y, 0f);

            if (i > 0) Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }

        Gizmos.color = Color.yellow;
        float cameraRadius = Mathf.Max(maxCameraSize, horizontalSize);
        prevPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * cameraRadius;
            float y = Mathf.Sin(angle) * cameraRadius;
            Vector3 point = new Vector3(x, y, 0f);

            if (i > 0) Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
}
