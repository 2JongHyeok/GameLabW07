using UnityEngine;

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

    void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    void Update()
    {
        if (dockingStation.isSpaceshipMode) return;
        MoveWeapon();
        Fire();
    }

    private void MoveWeapon()
    {
        // 입력 → 회전 방향(+1 시계/반시계 여부는 기존 코드 그대로)
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
    }

    void Fire()
    {
        if (Input.GetKey(KeyCode.Space) && Time.time >= nextFireTime)
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

        int count = Mathf.Clamp(level, 1, maxBullets);
        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) * 0.5f) * spacing;
            Vector3 spawnPos = firePoint.position + firePoint.right * offset;
            Instantiate(bulletPrefab, spawnPos, firePoint.rotation);
        }

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
