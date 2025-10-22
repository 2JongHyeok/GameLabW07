using UnityEngine;

// 역할: Rigidbody2D를 제어하여 실제 우주선의 모든 물리적 움직임을 담당.
[RequireComponent(typeof(Rigidbody2D))]
public class SpaceshipMotor : MonoBehaviour
{
    private SpaceshipCargoSystem cargoSystem;

    [Header("Cargo Weight Penalty")]
    [Tooltip("광물 1개당 추력이 몇 퍼센트(%) 감소할지 설정합니다. (예: 5 입력 시 5%)")]
    [SerializeField] private float thrustReductionPerOre = 5f;

    [Header("Thrust Settings")]
    [SerializeField] private float thrustPower = 2000f;

    [Header("Inertia & Drag Settings")]
    [Tooltip("기본 저항값. 속도에 비례하며 최고 속도에 영향을 줍니다.")]
    [SerializeField] private float movementDrag = 0.5f;

    // --- 여기부터가 당신의 '소믈리에'를 위한 변수들입니다 ---
    [Header("Active Deceleration (The Brakes)")]
    [Tooltip("추진 입력이 없을 때, 반대 방향으로 가하는 제동력의 강도입니다.")]
    [Range(0f, 20f)]
    [SerializeField] private float stoppingPower = 5f;

    [Tooltip("추진 입력이 없을 때, 매 프레임 속도를 몇 퍼센트씩 줄일지 결정합니다. (1 = 미끄러짐 없음, 0.9 = 많이 미끄러짐)")]
    [Range(0.9f, 1f)]
    [SerializeField] private float glideReduction = 0.98f;

    [Tooltip("이 속도 이하로 떨어지면 강제로 멈춥니다. 미세한 움직임을 방지합니다.")]
    [SerializeField] private float stopThreshold = 0.1f;



    [Header("Rotational Inertia & Drag (회전 운동)")]
    [SerializeField] private float additiveTorque = 10f;
    [Tooltip("회전 저항값. 높을수록 회전이 빨리 멈춥니다.")]
    [SerializeField] private float angularDrag = 1f;


    [Range(0f, 20f)][SerializeField] private float stoppingTorque = 5f;
    [Range(0.9f, 1f)][SerializeField] private float rotationalGlideReduction = 0.95f;
    [SerializeField] private float angularStopThreshold = 0.1f;


    [Header("UI 방송 설정")]
    [SerializeField] private SpeedDataSO speedData;
    [SerializeField] private BoolVariable isOverweightState;
    [Tooltip("성능이 이 비율(%) 이하로 떨어지면 '과적' 경고가 뜹니다.")]
    [Range(0f, 100f)]
    [SerializeField] private float overweightThresholdPercent = 80f;


    public Rigidbody2D Rb { get; private set; }

    private void Awake()
    {
        cargoSystem = GetComponent<SpaceshipCargoSystem>();
        if (cargoSystem == null)
        {
        }
        Rb = GetComponent<Rigidbody2D>();
        Rb.gravityScale = 0;

        // Drag는 물리적으로 더 정확한 '항력'을 의미하므로, 그대로 사용합니다.
        Rb.linearDamping = movementDrag;
        Rb.angularDamping = angularDrag;
    }

    private void Update()
    {
        // 매 프레임, 인스펙터의 최신 값을 Rigidbody의 실제 물리 값으로 갱신합니다.
        // 이제 당신의 '소믈리에' 활동이 즉시 반영될 것이오.
        // 참고: linearDamping보다 drag가 더 정확한 물리 용어라, 그것으로 교체했소.
        Rb.linearDamping = movementDrag;
        Rb.angularDamping = angularDrag;
    }

    private void FixedUpdate()
    {
        if (speedData == null) return;

        float mass = Rb.mass;

        // 1. 이론상 최대 속도를 계산합니다. (최대추력 / 저항)
        float absoluteMaxSpeed = (movementDrag > 0 && mass > 0) ? thrustPower / (movementDrag * mass) : 0f;

        // 2. 무게가 적용된 유효 추력을 계산합니다.
        float effectiveThrust = CalculateEffectiveThrust();

        // 3. 무게가 적용된 유효 최대 속도를 계산합니다. (유효추력 / 저항)
        float effectiveMaxSpeed = (movementDrag > 0 && mass > 0) ? effectiveThrust / (movementDrag * mass) : 0f;

        // 4. 모든 계산된 값을 방송국(SO)에 업데이트합니다.
        speedData.CurrentSpeed = Rb.linearVelocity.magnitude;
        speedData.AbsoluteMaxSpeed = absoluteMaxSpeed;
        speedData.EffectiveMaxSpeed = effectiveMaxSpeed;
        bool isOverweight = false;
        if (absoluteMaxSpeed > 0)
        {
            // 현재 성능 비율을 계산합니다 (예: 75 / 100 = 0.75)
            float performanceRatio = effectiveMaxSpeed / absoluteMaxSpeed;
            // 설정된 임계치 비율을 계산합니다 (예: 80% -> 0.8)
            float thresholdRatio = overweightThresholdPercent / 100f;

            // 현재 성능이 임계치 '이하'일 경우 과적으로 판단합니다.
            isOverweight = performanceRatio <= thresholdRatio;
        }

        // 상태가 변경되었을 때만 방송을 보냅니다.
        if (isOverweightState.Value != isOverweight)
        {
            isOverweightState.Value = isOverweight;
        }


    }

    // [추가] 유효 추력을 계산하는 로직을 별도 함수로 분리하여 재사용성을 높입니다.
    private float CalculateEffectiveThrust()
    {
        float multiplier = 1.0f;
        if (cargoSystem != null)
        {
            int oreCount = cargoSystem.GetCollectedOreCount();
            int penaltyWeight = oreCount * (oreCount + 1) / 2;
            float totalReductionPercent = penaltyWeight * thrustReductionPerOre;
            multiplier = 1.0f - (totalReductionPercent / 100.0f);
            multiplier = Mathf.Max(0f, multiplier);
        }
        return thrustPower * multiplier;
    }

    public void Move(float thrustInput, float boostMultiplier)
    {
        if (Mathf.Abs(thrustInput) > 0.01f)
        {
            // 이제 힘을 가할 때도 매번 유효 추력을 계산해서 적용합니다.
            float forceToApply = CalculateEffectiveThrust();
            Rb.AddForce(transform.up * forceToApply * thrustInput * boostMultiplier, ForceMode2D.Force);
        }
    }
    // Rotate 함수는 변경할 필요가 없습니다.
    public void Rotate(float rotateInput)
    {
        if (Mathf.Abs(rotateInput) > 0.1f)
        {
            Rb.AddTorque(-rotateInput * additiveTorque);
        }
    }

    // --- 이 새로운 함수가 바로 '하이브리드 제동'의 핵심입니다 ---
    public void ApplyActiveDeceleration(float thrustInput)
    {
        // 1. 추진 입력이 없을 때만 제동 로직을 실행합니다.
        if (Mathf.Abs(thrustInput) < 0.1f)
        {
            // 2. 방법 A: 현재 속도의 반대 방향으로 '제동력'을 가합니다.
            if (Rb.linearVelocity.sqrMagnitude > 0) // 움직이고 있을 때만
            {
                Vector2 counterForce = -Rb.linearVelocity.normalized * stoppingPower;
                Rb.AddForce(counterForce, ForceMode2D.Force);
            }

            // 3. 방법 C: 현재 속도를 매 프레임 '비율'로 감소시킵니다.
            Rb.linearVelocity *= glideReduction;

            // 4. 속도가 거의 0에 가까워지면, 강제로 멈춰서 미세한 떨림을 방지합니다.
            if (Rb.linearVelocity.magnitude < stopThreshold)
            {
                Rb.linearVelocity = Vector2.zero;
            }
        }
    }
    public void ApplyActiveRotationalDeceleration(float rotateInput)
    {
        if (Mathf.Abs(rotateInput) < 0.1f)
        {
            if (Mathf.Abs(Rb.angularVelocity) > 0)
            {
                // 현재 회전 방향의 반대로 '제동 토크'를 가합니다.
                float counterTorque = -Mathf.Sign(Rb.angularVelocity) * stoppingTorque;
                Rb.AddTorque(counterTorque);
            }
            // 현재 각속도를 '비율'로 감소시킵니다.
            Rb.angularVelocity *= rotationalGlideReduction;
            if (Mathf.Abs(Rb.angularVelocity) < angularStopThreshold)
            {
                Rb.angularVelocity = 0f;
            }
        }
    }
    // --- 바로 이 부분이 정비사들을 위한 '접근 통로'요, 왓슨 ---
    #region Getter & Setter (업그레이드용)

    // --- 직선 운동 관련 ---
    public float GetThrustPower() { return thrustPower; }
    public void SetThrustPower(float value) { thrustPower = value; }
    public void AddThrustPower(float amount) { thrustPower += amount; }

    public float GetMovementDrag() { return movementDrag; }
    public void SetMovementDrag(float value) { movementDrag = value; }
    public void AddMovementDrag(float amount) { movementDrag += amount; }

    public float GetStoppingPower() { return stoppingPower; }
    public void SetStoppingPower(float value) { stoppingPower = value; }
    public void AddStoppingPower(float amount) { stoppingPower += amount; }

    public float GetGlideReduction() { return glideReduction; }
    public void SetGlideReduction(float value) { glideReduction = Mathf.Clamp(value, 0.9f, 1f); }
    public void AddGlideReduction(float amount) { glideReduction = Mathf.Clamp(glideReduction + amount, 0.9f, 1f); }


    // --- 회전 운동 관련 ---
    public float GetAdditiveTorque() { return additiveTorque; }
    public void SetAdditiveTorque(float value) { additiveTorque = value; }
    public void AddAdditiveTorque(float amount) { additiveTorque += amount; }

    public float GetAngularDrag() { return angularDrag; }
    public void SetAngularDrag(float value) { angularDrag = value; }
    public void AddAngularDrag(float amount) { angularDrag += amount; }

    public float GetStoppingTorque() { return stoppingTorque; }
    public void SetStoppingTorque(float value) { stoppingTorque = value; }
    public void AddStoppingTorque(float amount) { stoppingTorque += amount; }

    public float GetRotationalGlideReduction() { return rotationalGlideReduction; }
    public void SetRotationalGlideReduction(float value) { rotationalGlideReduction = Mathf.Clamp(value, 0.9f, 1f); }
    public void AddRotationalGlideReduction(float amount) { rotationalGlideReduction = Mathf.Clamp(rotationalGlideReduction + amount, 0.9f, 1f); }


// ★ 추가: 광물당 추력 감소율 업그레이드를 위한 Getter & Setter
    public float GetThrustReductionPerOre() { return thrustReductionPerOre; }
    public void SetThrustReductionPerOre(float value) { thrustReductionPerOre = Mathf.Max(0f, value); } // 0% 미만으로 내려가지 않도록 보정
    public void AddThrustReductionPerOre(float amount) { SetThrustReductionPerOre(thrustReductionPerOre + amount); }

    #endregion


}