using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
public class SpaceshipMovement : MonoBehaviour
{
    [Header("우주선 이동 설정")]
    public float thrustPower = 2000f;    // 기본 추진력 (W 키)
    public float maxSpeed = 10000f;      // 최대 속도 제한

    [Header("추진력 조절 설정")]
    public float thrustRampUpSpeed = 1f;    // 가속 시 속도 (값이 작을수록 딜레이 큼)
    public float thrustRampDownSpeed = 1f;  // 감속 속도
    public float boostMultiplier = 2f;      // 최대 부스트 배수

    // 부스트 효과의 점진적 상승을 위한 변수
    public float boostRampSpeed = 1f;       // 부스트가 서서히 상승하는 속도
    private float currentBoostMultiplier = 1f;  // 현재 적용되는 부스트 배수 (1부터 시작)

    private Rigidbody2D rb;
    private float currentThrust = 0f;       // 현재 추진력 (0~1)
    private bool isBoosting = false;        // 부스트 활성화 여부

    [Header("회전 설정 (두 변수 방식)")]
    public float directRotationSpeed = 30f;
    public float additiveTorque = 10f;
    private const float velocityThreshold = 0.01f;
    private const float angularVelocityThreshold = 0.1f;
    private bool directRotationActive = false;
    private float desiredRotation;

    [Header("추진기 효과 설정")]
    public ParticleSystem thrusterParticle;       // 기본 추진 파티클 (W키)
    public ParticleSystem boostParticle1;           // 부스트 효과 파티클 1
    public ParticleSystem boostParticle2;           // 부스트 효과 파티클 2
    public ParticleSystem leftThrusterParticle;     // 좌회전 효과 파티클 (D키)
    public ParticleSystem rightThrusterParticle;    // 우회전 효과 파티클 (A키)

    [Header("Audio Settings")]
    public AudioSource engineSound;       // 기본 추진 사운드 (Loop)
    public AudioSource boostSound;        // 부스트 사운드 (Loop)
    public AudioSource gasDirectionSound; // 회전 시 가스 분출 사운드 (Loop)

    [Header("Audio Ramp Settings")]
    public float engineSoundRampSpeed = 5f;
    public float gasSoundRampSpeed = 5f;

    [Header("Audio Maximum Volumes")]
    public float engineMaxVolume = 0.5f;
    public float boostMaxVolume = 0.8f;
    public float gasMaxVolume = 0.5f;

    [Header("연료 시스템")]
    public float fuelMax = 100f;              // 연료 최대 용량
    public float fuelConsumptionRate = 1f;    // 기본 연료 소모량 (부스트 사용 시 boostMultiplier와 곱해짐)
    public float currentFuel;                // 현재 연료량
    public Slider fuelGauge;                  // 연료 게이지 UI (슬라이더)
    // 입력 상태 추적
    private bool prevTurnKey = false;
    public SpaceshipLocationManager spaceshipLocationManager; // 에디터에서 할당

    public TMP_Text speedText; 
    public RectTransform directionArrow; 
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>(); 
        rb.gravityScale = 0;
        rb.linearDamping = 0;
        rb.angularDamping = 0;

        desiredRotation = rb.rotation;

        if (engineSound != null)
        {
            engineSound.loop = true;
            engineSound.volume = 0f;
            engineSound.Play();
        }
        if (boostSound != null)
        {
            boostSound.loop = true;
            boostSound.volume = 0f;
            boostSound.Play();
        }
        if (gasDirectionSound != null)
        {
            gasDirectionSound.loop = true;
            gasDirectionSound.volume = 0f;
            gasDirectionSound.Play();
        }
        // ✅ 부스터 UI(Slider)를 자동으로 찾아서 연결
        fuelGauge = GameObject.Find("BoosterBar")?.GetComponent<Slider>();
        if (fuelGauge != null)
        {
                fuelGauge.maxValue = fuelMax;
                fuelGauge.value = currentFuel;
        }
        else
        {
        }
    }
    private void Update()
    {
        // 1) 입력 체크: W(추진), Shift(부스트), A/D(회전)
        bool thrustKey = Input.GetKey(KeyCode.W);
        bool boostKey = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool turnLeft = Input.GetKey(KeyCode.A);
        bool turnRight = Input.GetKey(KeyCode.D);
        bool turnKey = turnLeft || turnRight;

        // "isBoosting"은 "연료가 0이 아니고 + (W + Shift)키 눌림"일 때만 true
        // -> 나중에 파티클/사운드/FixedUpdate 등에서 사용
        isBoosting = (currentFuel > 0f) && thrustKey && boostKey;

        if (fuelGauge != null)
        {
            fuelGauge.value = currentFuel;  // 연료가 0이어도 그대로 표시
        }

        // 2) E키 눌러서 플레이어/우주선 출입 (원본 로직 유지)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (spaceshipLocationManager != null)
            {
                spaceshipLocationManager.ExitCockpit(); 
            }
            else
            {
            }
        }

        // 3) [회전 로직]은 연료와 관계없이 처리
        //    -> 회전 파티클 & 사운드
        if (turnLeft)
        {
            if (rightThrusterParticle != null && !rightThrusterParticle.isPlaying)
                rightThrusterParticle.Play();
        }
        else
        {
            if (rightThrusterParticle != null && rightThrusterParticle.isPlaying)
                rightThrusterParticle.Stop();
        }

        if (turnRight)
        {
            if (leftThrusterParticle != null && !leftThrusterParticle.isPlaying)
                leftThrusterParticle.Play();
        }
        else
        {
            if (leftThrusterParticle != null && leftThrusterParticle.isPlaying)
                leftThrusterParticle.Stop();
        }

        // 회전 사운드 (gasDirectionSound)
        if (turnKey && !prevTurnKey)
        {
            // 회전 키를 새로 눌렀을 때 재생
            if (gasDirectionSound != null)
            {
                gasDirectionSound.Stop();
                gasDirectionSound.time = 0f;
                gasDirectionSound.Play();
            }
        }
        // 볼륨 Lerp(가스 사운드)
        if (turnKey)
        {
            if (gasDirectionSound != null)
                gasDirectionSound.volume = Mathf.Lerp(gasDirectionSound.volume, gasMaxVolume, Time.deltaTime * gasSoundRampSpeed);
        }
        else
        {
            if (gasDirectionSound != null)
                gasDirectionSound.volume = Mathf.Lerp(gasDirectionSound.volume, 0f, Time.deltaTime * gasSoundRampSpeed);
        }
        prevTurnKey = turnKey;

        // 6) 속도 UI & 화살표
        if (speedText != null)
        {
            float speed = rb.linearVelocity.magnitude * 3.6f;
            speedText.text = speed.ToString("F1") + " km/h";
        }

        if (directionArrow != null)
        {
            float speed = rb.linearVelocity.magnitude;
            directionArrow.gameObject.SetActive(speed > 0.1f);
            if (speed > 0.001f)
            {
                float velocityAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                directionArrow.rotation = Quaternion.Euler(0, 0, velocityAngle);
            }
        }

        // 4) 만약 연료가 0 이하라면, 메인 추진/부스트 관련 로직은 중지
        if (currentFuel <= 0f)
        {
            // (A) 메인 추진 파티클, 엔진 사운드 중지
            if (thrusterParticle != null && thrusterParticle.isPlaying)
                thrusterParticle.Stop();
            if (engineSound != null)
            {
                engineSound.Stop();
                engineSound.volume = 0f;
            }

            // (B) 부스트 파티클, 사운드 중지
            if (boostParticle1 != null && boostParticle1.isPlaying)
                boostParticle1.Stop();
            if (boostParticle2 != null && boostParticle2.isPlaying)
                boostParticle2.Stop();
            if (boostSound != null)
            {
                boostSound.Stop();
                boostSound.volume = 0f;
            }
            

            // (D) 부스트 상태 false
            isBoosting = false;

            // 주추진(W) 관련 사운드 볼륨도 0으로 수렴
            // 여기서 return → 회전 로직은 이미 위에서 처리했으므로 OK
            return;
        }

        // 5) [연료가 남아있을 때] 메인 추진/부스트 처리
        // 메인 추진 파티클
        if (thrustKey)
        {
            if (thrusterParticle != null && !thrusterParticle.isPlaying)
                thrusterParticle.Play();
        }
        else
        {
            if (thrusterParticle != null && thrusterParticle.isPlaying)
                thrusterParticle.Stop();
        }

        // 부스트 파티클
        if (isBoosting)
        {
            if (boostParticle1 != null && !boostParticle1.isPlaying)
                boostParticle1.Play();
            if (boostParticle2 != null && !boostParticle2.isPlaying)
                boostParticle2.Play();
        }
        else
        {
            if (boostParticle1 != null && boostParticle1.isPlaying)
                boostParticle1.Stop();
            if (boostParticle2 != null && boostParticle2.isPlaying)
                boostParticle2.Stop();
        }

        // 엔진/부스트 사운드 볼륨 처리
        //   5-1) thrustKey + not boosting
        if (thrustKey && !isBoosting)
        {
            if (engineSound != null)
                engineSound.volume = Mathf.Lerp(engineSound.volume, engineMaxVolume, Time.deltaTime * engineSoundRampSpeed);
            if (boostSound != null)
                boostSound.volume = Mathf.Lerp(boostSound.volume, 0f, Time.deltaTime * engineSoundRampSpeed);
        }
        //   5-2) isBoosting
        else if (isBoosting)
        {
            if (boostSound != null)
                boostSound.volume = Mathf.Lerp(boostSound.volume, boostMaxVolume, Time.deltaTime * engineSoundRampSpeed);
            if (engineSound != null)
                engineSound.volume = Mathf.Lerp(engineSound.volume, 0f, Time.deltaTime * engineSoundRampSpeed);
        }
        //   5-3) thrustKey X
        else
        {
            if (engineSound != null)
                engineSound.volume = Mathf.Lerp(engineSound.volume, 0f, Time.deltaTime * engineSoundRampSpeed);
            if (boostSound != null)
                boostSound.volume = Mathf.Lerp(boostSound.volume, 0f, Time.deltaTime * engineSoundRampSpeed);
        }


        // 7) 연료 소모 (W키, 부스트키)
        if (thrustKey)
        {
            // 기본 소모
            currentFuel -= fuelConsumptionRate * Time.deltaTime;

            // 부스트 추가 소모
            if (boostKey)
            {
                currentFuel -= fuelConsumptionRate * (boostMultiplier - 1f) * Time.deltaTime;
            }

            // 0 이하로 내려가면 0으로
            if (currentFuel < 0f)
                currentFuel = 0f;
        }

        // 8) 연료 슬라이더 갱신
        if (fuelGauge != null)
        {
            fuelGauge.value = currentFuel;
        }
        // 1) 연료가 0보다 크고 + W 누름 + 엔진 사운드가 정지 상태라면
        if (currentFuel > 0f && thrustKey && engineSound != null && !engineSound.isPlaying)
        {
            engineSound.time = 0f; // 처음부터 재생
            engineSound.Play();
        }

        // 2) 부스트도 같은 방식 (isBoosting && !boostSound.isPlaying)
        if (currentFuel > 0f && isBoosting && boostSound != null && !boostSound.isPlaying)
        {
            boostSound.time = 0f;
            boostSound.Play();
        }
    }

    private void FixedUpdate()
    {
        // ─────────────────────────────
        // 1) 회전 로직 (연료가 없어도 가능)
        // ─────────────────────────────
        float turnInput = 0f;
        if (Input.GetKey(KeyCode.A)) turnInput = 1f;
        else if (Input.GetKey(KeyCode.D)) turnInput = -1f;

        bool turnKey = (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D));

        if (turnKey)
        {
            if (!directRotationActive)
            {
                // 속도/각속도가 매우 낮다면, 직접 회전 모드로 전환
                if (rb.linearVelocity.sqrMagnitude < velocityThreshold && Mathf.Abs(rb.angularVelocity) < angularVelocityThreshold)
                {
                    directRotationActive = true;
                    desiredRotation = rb.rotation;
                }
            }

            if (directRotationActive)
            {
                desiredRotation += turnInput * directRotationSpeed * Time.deltaTime;
                rb.MoveRotation(desiredRotation);
                rb.angularVelocity = 0f;
            }
            else
            {
                rb.AddTorque(turnInput * additiveTorque);
            }
        }
        else
        {
            directRotationActive = false;
        }

        // ─────────────────────────────
        // 2) 연료가 0 이하이면 추진 로직 중단
        // ─────────────────────────────
        if (currentFuel <= 0f)
        {
            currentThrust = 0f;             // 추진력 0
            currentBoostMultiplier = 1f;    // 부스트 배수 기본값
            return; // 더 이상 힘을 가하지 않고 종료
        }

        // ─────────────────────────────
        // 3) 연료가 남아있을 때만 추진력 계산
        // ─────────────────────────────
        // (A) 목표 Thrust 설정: W키 눌림 여부
        float targetThrust = Input.GetKey(KeyCode.W) ? 1f : 0f;

        // (B) Ramp Speed 결정: 부스트 중이면 thrustRampUpSpeed * boostMultiplier, 아니면 기본
        float rampSpeed = isBoosting ? thrustRampUpSpeed * boostMultiplier : thrustRampUpSpeed;

        // (C) 현재 Thrust를 목표값으로 서서히 보정 (올라갈 땐 rampSpeed, 내려갈 땐 thrustRampDownSpeed)
        float thrustAdjustSpeed = (currentThrust < targetThrust) ? rampSpeed : thrustRampDownSpeed;
        currentThrust = Mathf.MoveTowards(currentThrust, targetThrust, thrustAdjustSpeed * Time.deltaTime);

        // (D) 부스트 배수도 서서히 보정
        float desiredBoost = isBoosting ? boostMultiplier : 1f;
        currentBoostMultiplier = Mathf.MoveTowards(currentBoostMultiplier, desiredBoost, boostRampSpeed * Time.deltaTime);

        // ─────────────────────────────
        // 4) 실제 힘 적용
        // ─────────────────────────────
        float effectiveThrust = currentThrust * currentBoostMultiplier;
        if (effectiveThrust > 0f)
        {
            rb.AddForce(transform.up * thrustPower * effectiveThrust, ForceMode2D.Force);
        }

        // ─────────────────────────────
        // 5) 최대 속도 제한
        // ─────────────────────────────
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
}