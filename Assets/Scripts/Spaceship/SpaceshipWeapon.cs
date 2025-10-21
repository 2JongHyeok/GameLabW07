using UnityEngine;

/// <summary>
/// 우주선의 미사일 발사를 관리하고 업그레이드 가능한 모든 수치를 담당합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SpaceshipWeapon : MonoBehaviour
{
    [Header("미사일 설정")]
    [SerializeField] private GameObject missilePrefab;
    [SerializeField] private Transform firePoint;
    
    [Header("업그레이드 가능 수치")]
    [SerializeField] private int damage = 25;
    [SerializeField] private float fireRate = 0.5f; // 초당 2발
    [SerializeField] private float explosionRadius = 2.0f;

    private float nextFireTime = 0f;
    private Rigidbody2D shipRb;

    void Awake()
    {
        shipRb = GetComponent<Rigidbody2D>();
    }

/// <summary>
    /// 발사 속도를 체크하고 미사일을 생성합니다.
    /// '뇌'로부터 명령을 받았을 때만 호출됩니다.
    /// </summary>
    public void FireMissile() // 이제 외부에서 호출할 수 있도록 public으로 변경합니다.
    {
        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;

            if (missilePrefab != null && firePoint != null)
            {
                GameObject missileObj = Instantiate(missilePrefab, firePoint.position, firePoint.rotation);
                // 2. 생성된 미사일에서 SpaceshipMissile 스크립트를 가져옵니다.
                SpaceshipMissile missileScript = missileObj.GetComponent<SpaceshipMissile>();

                // 3. 스크립트를 찾았다면, 우주선의 현재 속도를 넘겨주며 초기화(Initialize)합니다.
                if (missileScript != null)
                {
                    missileScript.Initialize(shipRb.linearVelocity);
                }

            }
            else
            {
            }
        }
    }

    #region Getter & Setter (업그레이드용)
    // 원래 이름 (다른 시스템 호환)
    public int GetDamage() { return damage; }
    public void SetDamage(int value) { damage = value; }
    public void AddDamage(int amount) { damage += amount; }

    public float GetAttackSpeed() { return fireRate; }
    public void SetAttackSpeed(float value) { fireRate = value; }
    public void AddAttackSpeed(float amount) { fireRate += amount; }

    public float GetExplosionRadius() { return explosionRadius; }
    public void SetExplosionRadius(float value) { explosionRadius = value; }
    public void AddExplosionRadius(float amount) { explosionRadius += amount; }

    // Mining 전용 네이밍 (동일 변수 매핑)
    public int GetMiningDamage() { return damage; }
    public void SetMiningDamage(int value) { damage = value; }
    public void AddMiningDamage(int amount) { damage += amount; }

    public float GetMiningAttackSpeed() { return fireRate; }
    public void SetMiningAttackSpeed(float value) { fireRate = value; }
    public void AddMiningAttackSpeed(float amount) { fireRate += amount; }

    public float GetMiningRadius() { return explosionRadius; }
    public void SetMiningRadius(float value) { explosionRadius = value; }
    public void AddMiningRadius(float amount) { explosionRadius += amount; }
    #endregion
}
