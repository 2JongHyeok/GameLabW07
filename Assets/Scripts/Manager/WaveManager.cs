// WaveManager.cs (Unity 6)
// - Planet2 "활성화" 자동 감지(컴포넌트 enabled 변화 / GameObject 활성화 감지)
// - 디버그: 게이트 스킵, 강제 동시 시작(F9/ContextMenu)
// - 동시 시작 시 각 매니저의 Resume 훅 사용(더 안전)

using UnityEngine;

[DefaultExecutionOrder(-100)]
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Planet Wave Managers")]
    [SerializeField] private Planet1WaveManager planet1;
    [SerializeField] private Planet2WaveManager planet2;

    [Header("Gate & Final (1-based)")]
    [SerializeField] private int planet1GateWave = 4; // P1 1~4까지 독자 진행
    [SerializeField] private int planet1FinalWave = 8; // P1 최종
    [SerializeField] private int planet2FinalWave = 4; // P2 최종

    [Header("State (ReadOnly)")]
    [SerializeField] private Phase phase = Phase.Planet1Phase1To4;
    [SerializeField] private bool planet2Activated;
    [SerializeField] private bool planet1PausedAfterGate;

    [Header("QA/Debug")]
    [Tooltip("Planet2 활성화를 자동 감지(컴포넌트 enabled 변화를 포함)")]
    [SerializeField] private bool autoDetectPlanet2Activation = true;
    [Tooltip("게이트(P1 Wave4 완료)를 건너뛰고 즉시 Combined 시작 허용")]
    [SerializeField] private bool debugSkipGate = false;
    [Tooltip("디버그 강제 동시 시작 핫키")]
    [SerializeField] private KeyCode debugForceCombinedKey = KeyCode.F9;

    private bool lastPlanet2Enabled; // 활성화 변화 감지용

    [Header("Combined Sync")]
    [SerializeField] private bool syncInterWaveCooldown = true;

    // <= 0 이면 자동(Max(P1,P2)), 양수면 강제 동일 쿨다운(예: 7f)
    [SerializeField] private float combinedCooldownOverride = -1f;

    private bool simulCountdownArmed = false;

    // 외부에서 읽기용
    public bool IsCombinedPhase => phase == Phase.CombinedPhase;

    private enum Phase { Planet1Phase1To4, WaitingPlanet2Activate, CombinedPhase, Done }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // P2 조기 실행 방지: 초기엔 컴포넌트를 꺼둔다(시작 타이밍을 중앙이 쥔다)
        if (planet2)
        {
            lastPlanet2Enabled = planet2.enabled;
            planet2.enabled = false;
        }
    }

    private void Update()
    {
        // --- (1) Planet2 활성화 "자동 감지" ---
        if (autoDetectPlanet2Activation && !planet2Activated && planet2 != null)
        {
            // 케이스A: 외부에서 Planet2WaveManager.enabled를 true로 바꿨다
            if (!lastPlanet2Enabled && planet2.enabled)
                NotifyPlanet2Activated();

            // 케이스B: GameObject가 나중에 SetActive(true) 되었다
            if (planet2.gameObject.activeInHierarchy && !planet2.enabled && phase == Phase.WaitingPlanet2Activate)
            {
                // 아직 컴포넌트는 꺼져 있어도, 행성 오브젝트가 켜졌다면 ‘활성화 이벤트’로 간주
                NotifyPlanet2Activated();
            }

            lastPlanet2Enabled = planet2.enabled;
        }

        // --- (2) 일반 상태 머신 ---
        switch (phase)
        {
            case Phase.Planet1Phase1To4:
                if (HasPlanet1FinishedGateWave())
                {
                    if (planet2Activated || debugSkipGate)
                        StartCombinedPhase(); 
                    else
                        PausePlanet1UntilPlanet2(); // 기존: 대기 상태로 전환
                }
                break;

            case Phase.WaitingPlanet2Activate:
                // 활성화 신호가 오면 Combined로 전환 (아래 Notify에서 처리)
                break;

            case Phase.CombinedPhase:
                if (HasPlanet1CompletedFinal() && HasPlanet2CompletedFinal())
                    phase = Phase.Done;
                if (!syncInterWaveCooldown) break;

                bool p1Valid = planet1 != null && !planet1.AllWavesCompleted;
                bool p2Valid = planet2 != null && !planet2.AllWavesCompleted;

                // 하나라도 아직 진행 중이라면(완주 전이라면) 동기화 로직 가동
                if (p1Valid || p2Valid)
                {
                    bool p1Between = planet1 != null && planet1.IsBetweenWaves();
                    bool p2Between = planet2 != null && planet2.IsBetweenWaves();

                    // (1) 둘 다 BetweenWaves가 아니면 개별 쿨다운을 잠가서 기다리게 한다
                    if (!(p1Between && p2Between))
                    {
                        if (planet1 != null) planet1.LockCountdownByCentral(true);
                        if (planet2 != null) planet2.LockCountdownByCentral(true);
                        simulCountdownArmed = false; // 다시 무장 가능 상태로 되돌림
                    }
                    // (2) 둘 다 BetweenWaves인 “그 프레임”에 동시에 카운트다운을 무장 & 해제
                    else if (!simulCountdownArmed)
                    {
                        float cd = combinedCooldownOverride > 0f
                            ? combinedCooldownOverride
                            : Mathf.Max(
                                planet1 != null ? planet1.TimeBetweenWaves : 0f,
                                planet2 != null ? planet2.TimeBetweenWaves : 0f
                              );

                        if (planet1 != null)
                        {
                            planet1.StartSimulCountdownFromCentral(cd);
                            planet1.LockCountdownByCentral(false); // 해제 → 로컬 Update에서 같은 프레임부터 감소
                        }
                        if (planet2 != null)
                        {
                            planet2.StartSimulCountdownFromCentral(cd);
                            planet2.LockCountdownByCentral(false);
                        }
                        simulCountdownArmed = true;
                    }
                }
                break;

            case Phase.Done:
                break;
        }

        // --- (3) 디버그 핫키 ---
        if (Input.GetKeyDown(debugForceCombinedKey))
            ForceCombinedStartNow();
    }

    // 외부에서 호출: Planet2가 "활성화"되는 순간에 한 줄
    public void NotifyPlanet2Activated()
    {
        planet2Activated = true;
        if (planet1 != null) planet1.SetGateHold(true);
        if (phase == Phase.WaitingPlanet2Activate
        || HasPlanet1FinishedGateWave()
        || debugSkipGate)
        {
            StartCombinedPhase();
            return;
        }
        Debug.Log("[WaveSync] Planet2 activated. Gate not yet cleared; holding P1 at Wave4.");
    }

    // Planet1이 Wave4를 끝냈고(0-based index >= 4), 적이 전멸했는지
    private bool HasPlanet1FinishedGateWave()
    {
        if (!planet1) return false;
        bool clearedGate = planet1.CurrentWaveIndex >= planet1GateWave; // 1~4 완료 시 >=4
        bool noEnemies = planet1.EnemyCount <= 0;
        return clearedGate && noEnemies;
    }

    private void PausePlanet1UntilPlanet2()
    {
        if (planet1PausedAfterGate) return;
        planet1PausedAfterGate = true;
        phase = Phase.WaitingPlanet2Activate;

        planet1.SetGateHold(true);
        // 카운트다운/스폰 루프를 멈추기 위해 비활성화(내부 상태는 그대로 유지)
        if (planet1) planet1.PauseByCentral();
        Debug.Log("[WaveSync] Gate reached (P1 Wave4 clear). Waiting for Planet2 activation...");
    }

    private void StartCombinedPhase()
    {
        // Planet1 → Wave5 즉시 시작
        if (planet1)
        {
            planet1.SetGateHold(false);            // ← 잠금 해제 (중요)
            planet1.ForceStartNextWaveByCentral(); // ← 즉시 SpawnWave() 예약
        }

        // Planet2 → Wave1 즉시 시작
        if (planet2)
        {
            if (!planet2.gameObject.activeSelf)
                planet2.gameObject.SetActive(true);

            planet2.enabled = true;
            planet2.ForceStartNextWaveByCentral(); // ← 즉시 SpawnWave() 예약

            Debug.Log("[WaveSync] Combined start triggered (P1 Wave5 + P2 Wave1).");
        }
        phase = Phase.CombinedPhase;
    }

    private bool HasPlanet1CompletedFinal()
    {
        if (!planet1) return true;
        return planet1.CurrentWaveIndex >= planet1FinalWave && planet1.EnemyCount <= 0;
    }

    private bool HasPlanet2CompletedFinal()
    {
        if (!planet2) return true;
        return planet2.CurrentWaveIndex >= planet2FinalWave && planet2.EnemyCount <= 0;
    }

    // ---- 디버그 지원 ----
    [ContextMenu("DEBUG/Force Combined Start Now")]
    public void ForceCombinedStartNow()
    {
        // 게이트 여부와 무관하게 즉시 Combined 진입
        StartCombinedPhase();
    }
}
