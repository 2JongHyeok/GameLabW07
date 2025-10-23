using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


public class Weapon : MonoBehaviour
{
    [Header("WeaponPivot")]
    [SerializeField] private GameObject weaponPivot;
    
    [Header("Values")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float fireRate = 0.1f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private DockingStation dockingStation;
    

    [Header("Sprite Changes")]  // 외형 변경용
    public SpriteRenderer targetRenderer;   // 없으면 GetComponent로 검색
    public Sprite[] skins;                  // 교체할 스프라이트들
    private int index = -1;

    // === 가속 관련 ===
    [Header("Rotation Acceleration")]
    [SerializeField] private float accelTime = 1f; // 목표 속도까지 걸리는 시간(초)
    private float accelTimer = 0f;                 // 키 홀드 시간
    private float lastDirection = 0f;              // 이전 프레임의 방향(부호만 의미)
    private float nextFireTime = 0f;
    [Range(1, 3)] public int maxBullets = 3;
    public float spacing = 1.0f;    // 총알 사이의 거리
    public int level = 1;
    
    [Header("Gamepad Settings")]
    private Coroutine rumbleCoroutine; // 현재 실행 중인 진동 코루틴
    private float stickDeadZone = 0.1f;      // 스틱 데드존

    void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponentInChildren<SpriteRenderer>(true);
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    void Update()
    {
        if (dockingStation.isSpaceshipMode) return;
        MoveWeapon();
        Fire();
    }

    private void MoveWeapon()
    {
        // 게임 패드 입력 (절대 각도 조준)
        Vector2 rotationInput = Vector2.zero;
        bool gamepadConnected = (Gamepad.current != null);

        if (gamepadConnected)
        {
            rotationInput = Gamepad.current.leftStick.ReadValue();
        }

        // 게임패드 왼쪽 스틱 입력이 있고, 데드존을 넘어섰을 때
        if (gamepadConnected && rotationInput.magnitude > stickDeadZone)
        {
            // 스틱의 방향 벡터를 각도로 변환 - 스프라이트 기본 방향이 위쪽이므로 -90도 보정
            float targetAngle = Mathf.Atan2(rotationInput.y, rotationInput.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);

            // weaponPivot을 목표 각도로 보간 회전
            // rotationSpeed 변수를 최대 회전 속도로 사용
            weaponPivot.transform.rotation = Quaternion.RotateTowards(weaponPivot.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // 패드 사용 시 키보드 가속도 관련 변수 초기화
            accelTimer = 0f;
            lastDirection = 0f;
        }
        
        /*// 키보드 입력 (상대적 회전) - 기존 키보드 로직, 게임패드 연결 안 됨, 또는 스틱 입력이 없거나 데드존 이내일 경우
        else 
        {
            float inputDir = 0f;

            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                inputDir = 1f;
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                inputDir = -1f;

            if (inputDir != 0f)
            {
                // 방향이 바뀌면 가속 타이머 초기화
                if (Mathf.Sign(inputDir) != Mathf.Sign(lastDirection))
                {
                    accelTimer = 0f;
                }

                accelTimer += Time.deltaTime;
                float ramp = Mathf.Clamp01(accelTimer / Mathf.Max(0.0001f, accelTime)); // 0→1
                float currentSpeed = rotationSpeed * ramp;

                float rotationAmount = inputDir * currentSpeed * Time.deltaTime;
                weaponPivot.transform.Rotate(0f, 0f, rotationAmount);

                lastDirection = inputDir;
            }
            else
            {
                // 입력이 없으면 즉시 정지(가속도 리셋)
                accelTimer = 0f;
                lastDirection = 0f;
            }
        }*/
        
        // 게임패드 스틱 입력이 없을 경우 마우스로 조준
        else 
        {
            // 마우스의 스크린 좌표 획득
            Vector3 mouseScreenPos = Input.mousePosition;
            
            // Z값을 카메라에서 무기의 2D 평면까지의 거리로 설정
            mouseScreenPos.z = -Camera.main.transform.position.z + weaponPivot.transform.position.z;

            // 스크린 좌표를 월드 좌표로 변환
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

            // 무기 피벗에서 마우스 월드 위치를 향하는 2D 방향 벡터 계산
            Vector2 direction = (Vector2)mouseWorldPos - (Vector2)weaponPivot.transform.position;

            // 방향 벡터를 각도로 변환하고 스프라이트 방향 보정 적용
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            
            // 피벗 회전 보간
            weaponPivot.transform.rotation = Quaternion.RotateTowards(weaponPivot.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void Fire()
    {
        // 키보드 스페이스바 입력 확인
        bool keyboardFire = Input.GetKey(KeyCode.Space);

        // 게임패드 A 버튼 입력 확인
        bool gamepadFire = false;
        if (Gamepad.current != null)
        {
            gamepadFire = Gamepad.current.buttonSouth.IsPressed();
        }

        // 둘 중 하나라도 눌렸고, 발사 딜레이가 지났다면 발사
        if ((keyboardFire || gamepadFire) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            FireBullet();
        }
    }

    private void FireBullet()
    {
        nextFireTime = Time.time + fireRate;
        if (!bulletPrefab || !firePoint)
        {
            return;
        }

        // 진동 관련
        if (Gamepad.current != null)
        {
            if (rumbleCoroutine != null)
            {
                StopCoroutine(rumbleCoroutine);
            }
            
            rumbleCoroutine = StartCoroutine(Rumble(0.1f, 0.25f, 0.25f));
        }
        
        int count = Mathf.Clamp(level, 1, maxBullets);
        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) * 0.5f) * spacing;
            Vector3 spawnPos = firePoint.position + firePoint.right * offset;
            Instantiate(bulletPrefab, spawnPos, firePoint.rotation);
        }

    }
    
    // 게임패드 진동 코루틴 - duration 초 동안 lowFrequency와 highFrequency로 진동
    private IEnumerator Rumble(float duration, float lowFrequency, float highFrequency)
    {
        if (Gamepad.current == null)
            yield break;

        // 진동 시작
        Gamepad.current.SetMotorSpeeds(lowFrequency, highFrequency);

        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(duration);

        // 진동 정지 (코루틴이 끝날 때 패드 연결이 끊겼을 수도 있으니 다시 한번 체크)
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
        rumbleCoroutine = null; // 코루틴 완료 처리
    }

    #region Function Use At Other Script
    public void ChangeSprite()  // 스프라이트 이미지 바꾸는 함수.
    {
        if (skins != null && skins.Length > 0)
        {
            index = (index + 1) % skins.Length;
            targetRenderer.sprite = skins[index];
        }
    }
    public void UpgradeTurretBulletCount()
    {
        level++;
    }
    #endregion

    #region Getter Setter


    public int GetDamage() { return damage; }
    public void SetDamage(int val) { damage = val; }
    public void AddDamage(int val) { damage += val; }
    public float GetAttackSpeed() { return fireRate; }
    public void SetAttackSpeed(float val) { fireRate = val; }
    public void AddAttackSpeed(float val) { fireRate += val; }
    public float GetCannonSpeed() { return rotationSpeed; }
    public void SetCannonSpeed(float val) { rotationSpeed = val; }
    public void AddCannonSpeed(float val) { rotationSpeed += val; }
    public float GetExplosionRange() { return explosionRadius; }
    public void SetExplosionRange(float val) { explosionRadius = val; }
    public void AddExplosionRange(float val) { explosionRadius += val; }
    #endregion
}
