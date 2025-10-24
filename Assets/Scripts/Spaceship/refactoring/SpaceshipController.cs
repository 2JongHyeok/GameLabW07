using UnityEngine;

// 역할: 모든 Spaceship 컴포넌트들을 연결하고 조율하며, 효과를 직접 제어합니다.
[RequireComponent(typeof(SpaceshipInput))]
[RequireComponent(typeof(SpaceshipMotor))]
public class SpaceshipController : MonoBehaviour
{
    [Header("Gameplay Settings")]
    [SerializeField] private float boostMultiplier = 2f;
    [SerializeField] private float boostRampSpeed = 1f;

    [Header("VFX")]
    [SerializeField] private ParticleSystem thrusterParticle;
    [SerializeField] private ParticleSystem boostParticle1;
    [SerializeField] private ParticleSystem boostParticle2;
    [SerializeField] private ParticleSystem leftThrusterParticle;
    [SerializeField] private ParticleSystem rightThrusterParticle;
    [SerializeField] private ParticleSystem reverseThrusterParticle;

    [Header("SFX")]
    [SerializeField] private AudioSource engineSound;
    [SerializeField] private AudioSource boostSound;
    [SerializeField] private AudioSource gasDirectionSound;
    
    [Header("Audio Settings")]
    [SerializeField] private float engineSoundRampSpeed = 5f;
    [SerializeField] private float gasSoundRampSpeed = 5f;
    [SerializeField] private float engineMaxVolume = 0.5f;
    [SerializeField] private float boostMaxVolume = 0.8f;
    [SerializeField] private float gasMaxVolume = 0.5f;
    private Rigidbody2D rb;
    [Header("총알 발사 설정")]
    [Tooltip("발사할 총알 프리팹")]
    public GameObject bulletPrefab;

    [Tooltip("총알이 생성될 위치 (총구)")]
    public Transform firePoint;

    // 제어할 컴포넌트들
    private SpaceshipInput shipInput;
    private SpaceshipMotor shipMotor;
    private SpaceshipWeapon shipWeapon;

    private bool canControl = true;
    private float currentBoostMultiplier = 1f;

    public static bool IsSpaceshipMode { get; private set; } = false;
    public static void SetIsSpaceShipMode(bool isSpaceShipMode)
    {
        IsSpaceshipMode = isSpaceShipMode;
        // Debug.Log("isSpaceShipMode: "+isSpaceShipMode);
    }

    private void Awake()
    {
        shipInput = GetComponent<SpaceshipInput>();
        shipMotor = GetComponent<SpaceshipMotor>();
        shipWeapon = GetComponent<SpaceshipWeapon>();
        rb = GetComponent<Rigidbody2D>();   // 로그용
    }

    private void Start()
    {
        SetupLoopingAudio(engineSound);
        SetupLoopingAudio(boostSound);
        SetupLoopingAudio(gasDirectionSound);
    }

    private void Update()
    {
        // if (shipInput.ToggleControlPressed)
        // {
        //     canControl = !canControl;
        // }
        if (Input.GetKeyDown(KeyCode.P))
            {
                canControl = !canControl;
            }
        
        // 현재 상태 결정 (연료 체크 없음)
        bool isThrusting = canControl && shipInput.ThrustInput > 0;
        bool isBoosting = isThrusting && shipInput.IsBoosting;
        
        // 부스트 배수 부드럽게 조절
        float desiredBoost = isBoosting ? boostMultiplier : 1f;
        currentBoostMultiplier = Mathf.MoveTowards(currentBoostMultiplier, desiredBoost, boostRampSpeed * Time.deltaTime);

        // 효과 업데이트
        UpdateEffects(isThrusting, isBoosting, canControl ? shipInput.RotateInput : 0);
        HandleShooting();
    }

    private void FixedUpdate()
    {
        if (!canControl) return;

        // 물리 업데이트 (연료 체크 없음)
        shipMotor.Move(shipInput.ThrustInput, currentBoostMultiplier);
        shipMotor.Rotate(shipInput.RotateInput);
        shipMotor.ApplyActiveDeceleration(shipInput.ThrustInput);
        shipMotor.ApplyActiveRotationalDeceleration(shipInput.RotateInput);
        Vector2 currentPosition = rb.position;

        // 프레임 간 이동 거리 누적
        float frameDistance = Vector2.Distance(currentPosition, GameAnalyticsLogger.instance.playerLastPosition);
        GameAnalyticsLogger.instance.playerMoveDistance += frameDistance;

        // (0,0)에서 떨어진 거리 계산
        float distanceFromOrigin = currentPosition.magnitude;
        if (distanceFromOrigin > GameAnalyticsLogger.instance.playerMaxMoveDistance)
            GameAnalyticsLogger.instance.playerMaxMoveDistance = distanceFromOrigin;

        // 다음 프레임 계산을 위해 갱신
        GameAnalyticsLogger.instance.playerLastPosition = currentPosition;
    }
    
    private void UpdateEffects(bool isThrusting, bool isBoosting, float rotateInput)
    {
        // 파티클
        ToggleParticle(thrusterParticle, isThrusting);
        ToggleParticle(boostParticle1, isBoosting);
        ToggleParticle(boostParticle2, isBoosting);
        ToggleParticle(rightThrusterParticle, rotateInput > 0.1f);
        ToggleParticle(leftThrusterParticle, rotateInput < -0.1f);
        ToggleParticle(reverseThrusterParticle, canControl && shipInput.ThrustInput < -0.1f);

        
        // 오디오
    
        bool isApplyingThrust = canControl && Mathf.Abs(shipInput.ThrustInput) > 0.1f;
        float targetEngineVol = (isApplyingThrust && !isBoosting) ? engineMaxVolume : 0f;
        float targetBoostVol = isBoosting ? boostMaxVolume : 0f;
        float targetGasVol = Mathf.Abs(rotateInput) > 0.1f ? gasMaxVolume : 0f;

        engineSound.volume = Mathf.Lerp(engineSound.volume, targetEngineVol, Time.deltaTime * engineSoundRampSpeed);
        boostSound.volume = Mathf.Lerp(boostSound.volume, targetBoostVol, Time.deltaTime * engineSoundRampSpeed);
        gasDirectionSound.volume = Mathf.Lerp(gasDirectionSound.volume, targetGasVol, Time.deltaTime * gasSoundRampSpeed);
    }

    private void ToggleParticle(ParticleSystem particle, bool state)
    {
        if (particle == null) return;
        if (state && !particle.isPlaying) particle.Play();
        else if (!state && particle.isPlaying) particle.Stop();
    }

    private void SetupLoopingAudio(AudioSource audio)
    {
        if (audio != null) { audio.loop = true; audio.volume = 0; audio.Play(); }
    }
    void HandleShooting()
    {
        // 누르고 있는 동안 계속 발사를 '시도'합니다.
        if (canControl && Input.GetKey(KeyCode.Space))
        {
            // 실제 발사 가능 여부는 '손'이 결정합니다. 뇌는 명령만 내릴 뿐.
            if (shipWeapon != null)
            {
                shipWeapon.FireMissile();
            }
        }
    }

}