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
                    PausePlanet1UntilPlanet2();
                break;

            case Phase.WaitingPlanet2Activate:
                // 활성화 신호가 오면 Combined로 전환 (아래 Notify에서 처리)
                break;

            case Phase.CombinedPhase:
                if (HasPlanet1CompletedFinal() && HasPlanet2CompletedFinal())
                    phase = Phase.Done;
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

        // 게이트 미완료라도 디버그 플래그가 켜져 있으면 바로 Combined 시작
        if (phase == Phase.WaitingPlanet2Activate || (debugSkipGate && phase == Phase.Planet1Phase1To4))
            StartCombinedPhase();
        else
            Debug.Log("[WaveSync] Planet2 activated signal received, waiting for gate...");
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

        // 카운트다운/스폰 루프를 멈추기 위해 비활성화(내부 상태는 그대로 유지)
        if (planet1) planet1.PauseByCentral();
        Debug.Log("[WaveSync] Gate reached (P1 Wave4 clear). Waiting for Planet2 activation...");
    }

    private void StartCombinedPhase()
    {
        // Planet1 → Wave5 즉시 시작
        if (planet1)
            planet1.ResumeNextWaveByCentral(); // enabled=true + countdown=0

        // Planet2 → Wave1 즉시 시작
        if (planet2)
        {
            planet2.enabled = true;            // 첫 기동 보장
            planet2.ResumeNextWaveByCentral(); // countdown=0
        }

        phase = Phase.CombinedPhase;
        Debug.Log("[WaveSync] Combined start triggered (P1 Wave5 + P2 Wave1).");
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
