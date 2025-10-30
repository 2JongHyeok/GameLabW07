using UnityEngine;
using System;

// 역할: Rigidbody2D를 제어하여 실제 우주선의 모든 물리적 움직임을 담당.
[RequireComponent(typeof(Rigidbody2D))]
public class SpaceshipMotor : MonoBehaviour
{
    private SpaceshipCargoSystem cargoSystem;

    [Header("Cargo Weight Penalty")]
    [Tooltip("광물 1개당 추력이 몇 퍼센트(%) 감소할지 설정합니다.")]
    [SerializeField] private int allowedOreCount = 5;       // 허용 가능한 광물 수
    [SerializeField] private float overLimitPenaltyPercent = 30f; // 초과 시마다 감소율 (% 단위)

    [Header("Thrust Settings")]
    [SerializeField] private float thrustPower = 2000f;

    [Header("Inertia & Drag Settings")]
    [Tooltip("기본 저항값. 속도에 비례하며 최고 속도에 영향을 줍니다.")]
    [SerializeField] private float movementDrag = 0.5f;

    // --- 여기부터가 당신의 '소믈리에'를 위한 변수들입니다 ---
    [Header("Active Deceleration (The Brakes)")]
    [Tooltip("추진 입력이 없을 때, 반대 방향으로 가하는 제동력의 강도입니다.")]
    [Range(0f, 100f)]
    [SerializeField] private float stoppingPower = 5f;

    [Tooltip("추진 입력이 없을 때, 매 프레임 속도를 몇 퍼센트씩 줄일지 결정합니다. (1 = 미끄러짐 없음, 0.9 = 많이 미끄러짐)")]
    [Range(0f, 1f)]
    [SerializeField] private float glideReduction = 0.98f;

    [Tooltip("이 속도 이하로 떨어지면 강제로 멈춥니다. 미세한 움직임을 방지합니다.")]
    [SerializeField] private float stopThreshold = 0.1f;



    [Header("Rotational Inertia & Drag (회전 운동)")]
    [SerializeField] private float additiveTorque = 10f;
    [Tooltip("회전 저항값. 높을수록 회전이 빨리 멈춥니다.")]
    [SerializeField] private float angularDrag = 1f; // ## 이것이 '일반' 저항값입니다. ##

    // ## 새로운 변수: 강력한 제동용 저항값 ##
    [Tooltip("제동 시(입력 없을 시) 적용할 강력한 회전 저항. 'angularDrag'보다 훨씬 높아야 합니다.")]
    [SerializeField] private float brakingAngularDrag = 15f; 


    [Range(0f, 200f)][SerializeField] private float stoppingTorque = 5f; // (더 이상 사용되지 않음)
    [Range(0f, 1f)][SerializeField] private float rotationalGlideReduction = 0.95f; // (더 이상 사용되지 않음)
    [SerializeField] private float angularStopThreshold = 0.1f;


    [Header("UI 방송 설정")]
    // ... (이하 동일) ...
    [SerializeField] private SpeedDataSO speedData;
    [SerializeField] private BoolVariable isOverweightState;
    [Tooltip("성능이 이 비율(%) 이하로 떨어지면 '과적' 경고가 뜹니다.")]
    [Range(0f, 100f)]
    [SerializeField] private float overweightThresholdPercent = 80f;

    [Header("Knockback Settings")]
    [Tooltip("전방 충돌 시 튕겨나갈 힘의 크기 (Impulse)")]
    [SerializeField] private float knockbackStrength = 10f;
    
    [Tooltip("넉백이 발동되기 위한 최소 충돌 속도")]
    [SerializeField] private float minKnockbackSpeed = 1f;
    
    public event Action OnThrustValueChanged;
    
    public bool isOnMinimap = false;

    public Rigidbody2D Rb { get; private set; }

    private void Awake()
    {
        cargoSystem = GetComponent<SpaceshipCargoSystem>();
        if (cargoSystem == null)
        {
        }
        Rb = GetComponent<Rigidbody2D>();
        Rb.gravityScale = 0;

        Rb.linearDamping = movementDrag;
        Rb.angularDamping = angularDrag; // (초기값 설정)
    }

    private void Update()
    {
        // 매 프레임, 인스펙터의 최신 값을 Rigidbody의 실제 물리 값으로 갱신합니다.
        Rb.linearDamping = movementDrag;
        
        // ## 중요: 이 줄을 주석 처리하거나 삭제합니다. ##
        // Rb.angularDamping = angularDrag; 
        // (이제 이 값은 ApplyActiveRotationalDeceleration에서 제어합니다)
    }

    private void FixedUpdate()
    {
        // ... (FixedUpdate 내용은 동일) ...
        if (speedData == null || isOnMinimap) return;
        float mass = Rb.mass;
        float absoluteMaxSpeed = (movementDrag > 0 && mass > 0) ? thrustPower / (movementDrag * mass) : 0f;
        float effectiveThrust = CalculateEffectiveThrust();
        float effectiveMaxSpeed = (movementDrag > 0 && mass > 0) ? effectiveThrust / (movementDrag * mass) : 0f;
        speedData.CurrentSpeed = Rb.linearVelocity.magnitude;
        speedData.AbsoluteMaxSpeed = absoluteMaxSpeed;
        speedData.EffectiveMaxSpeed = effectiveMaxSpeed;
        bool isOverweight = false;
        if (absoluteMaxSpeed > 0)
        {
            float performanceRatio = effectiveMaxSpeed / absoluteMaxSpeed;
            float thresholdRatio = overweightThresholdPercent / 100f;
            isOverweight = performanceRatio <= thresholdRatio;
        }
        if (isOverweightState.Value != isOverweight)
        {
            isOverweightState.Value = isOverweight;
        }
    }

    // ... (CalculateEffectiveThrust, Move, Rotate 함수는 동일) ...
    private float CalculateEffectiveThrust()
    {
        float multiplier = 1.0f;

        if (cargoSystem != null)
        {
            int oreCount = cargoSystem.GetCollectedOreCount();
            if (oreCount <= allowedOreCount)
            {
                multiplier = 1.0f;
            }
            else
            {
                int excessCount = oreCount - allowedOreCount;
                float reductionPercent = excessCount * overLimitPenaltyPercent;
                multiplier = 1.0f - (reductionPercent / 100f);
                multiplier = Mathf.Max(0f, multiplier);
            }
        }
        return thrustPower * multiplier;
    }

    public void Move(float thrustInput, float boostMultiplier)
    {
        if (isOnMinimap) return;
        
        if (Mathf.Abs(thrustInput) > 0.01f)
        {
            float forceToApply = CalculateEffectiveThrust();
            Rb.AddForce(transform.up * forceToApply * thrustInput * boostMultiplier, ForceMode2D.Force);
        }
    }
    public void Rotate(float rotateInput)
    {
        if (isOnMinimap) return;
        
        if (Mathf.Abs(rotateInput) > 0.01f)
        {
            Rb.AddTorque(-rotateInput * additiveTorque);
        }
    }

    // ... (ApplyActiveDeceleration 함수는 동일) ...
    public void ApplyActiveDeceleration(float thrustInput)
    {
        if (isOnMinimap) return;
        
        if (Mathf.Abs(thrustInput) < 0.1f)
        {
            if (Rb.linearVelocity.sqrMagnitude > 0) // 움직이고 있을 때만
            {
                Vector2 counterForce = -Rb.linearVelocity.normalized * stoppingPower;
                Rb.AddForce(counterForce, ForceMode2D.Force);
            }
            Rb.linearVelocity *= glideReduction;
            if (Rb.linearVelocity.magnitude < stopThreshold)
            {
                Rb.linearVelocity = Vector2.zero;
            }
        }
    }
    
    // ## --- 이 함수를 완전히 교체합니다 --- ##
    public void ApplyActiveRotationalDeceleration(float rotateInput, float thrustInput = 0f)
    {
        if (isOnMinimap) return;
        
        // 1. 회전 입력이 있을 때 (데드존 밖)
        if (Mathf.Abs(rotateInput) > 0.01f)
        {
            // '일반' 회전 저항(angularDrag)을 사용해 부드럽게 회전하도록 합니다.
            Rb.angularDamping = angularDrag;
        }
        // 2. 회전 입력이 없을 때 (데드존 안, rotateInput == 0f)
        else
        {
            // '강력한 제동' 회전 저항(brakingAngularDrag)을 사용해
            // 즉시 회전을 멈추도록 합니다.
            Rb.angularDamping = brakingAngularDrag;
        }

        // 3. (공통) 속도가 아주 느려지면 강제로 멈춰서 미세한 떨림을 방지합니다.
        // (이 로직은 입력이 없을 때만 작동해야 함)
        if (Mathf.Abs(rotateInput) < 0.01f && Mathf.Abs(Rb.angularVelocity) < angularStopThreshold)
        {
            Rb.angularVelocity = 0f;
        }
    }
    // --- 교체 끝 ---
    
    
    #region Getter & Setter (업그레이드용)
    // ... (Getter & Setter 동일) ...
    // --- 직선 운동 관련 ---
    public float GetThrustPower() { return thrustPower; }
    public void SetThrustPower(float value) {  thrustPower = value; }
    public void AddThrustPower(float amount) { OnThrustValueChanged?.Invoke(); thrustPower += amount; }
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
    public int GetAllowedOreCount() { return allowedOreCount; }
    public void SetAllowedOreCount(int value) { allowedOreCount = Mathf.Max(0, value); }
    public void AddAllowedOreCount(int amount) { allowedOreCount = Mathf.Max(0, allowedOreCount + amount); }
    public float GetOverLimitPenaltyPercent() { return overLimitPenaltyPercent; }
    public void SetOverLimitPenaltyPercent(float value) { overLimitPenaltyPercent = Mathf.Clamp(value, 0f, 100f); }
    public void AddOverLimitPenaltyPercent(float amount) { overLimitPenaltyPercent = Mathf.Clamp(overLimitPenaltyPercent + amount, 0f, 100f); }

    #endregion

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ... (OnCollisionEnter2D 동일) ...
        if (Rb.linearVelocity.magnitude < minKnockbackSpeed)
        {
            return;
        }
        Vector2 collisionNormal = collision.contacts[0].normal;
        float dot = Vector2.Dot(transform.up, collisionNormal);
        if (dot < -0.1f)
        {
            Rb.AddForce(collisionNormal * knockbackStrength, ForceMode2D.Impulse);
        }
    }
}
