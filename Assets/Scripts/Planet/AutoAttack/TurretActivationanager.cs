using UnityEngine;

public class TurretActivationManager : MonoBehaviour
{
    [Header("--- 포탑 할당 ---")]
    [Tooltip("Z 키로 활성화될 유도탄 포탑")]
    public AutoTurret guidedMissileTurret;

    [Tooltip("X 키로 활성화될 총알 포탑")]
    public AutoTurret bulletTurret;

    [Tooltip("C 키로 활성화될 레이저 포탑")]
    public AutoTurret laserTurret;


    [Header("--- 프리팹 주입 ---")]
    public GameObject guidedMissilePrefab;
    public GameObject bulletPrefab;
    public GameObject laserPrefab;


    // 공격 전략 인스턴스 (메모리 효율을 위해 한 번만 생성)
    private GuidedMissileAttack missileStrategy;
    private IAttackStrategy bulletStrategy;
    private AutoTurretLaserAttack laserStrategy;

    [SerializeField] private GameObject missileTurretPrefab;
    private bool laserActivated = false;

    void Awake()
    {
        // 각 공격 전략을 미리 인스턴스화합니다.
        missileStrategy = new GuidedMissileAttack(guidedMissilePrefab, baseDamage: 20f, interval: 3f);
        bulletStrategy = new AutoTurretBulletAttack(bulletPrefab, 5f, 0.5f);
        laserStrategy = new AutoTurretLaserAttack(
            laserPrefab,
            10f,
            interval: 5f,
            duration: 0.5f,
            length: 15f,
            maxWidth: 1.0f,
            targetTag: "Enemy"
        );
        missileTurretPrefab.SetActive(false);
    }
    public float GetMissileDamage() => missileStrategy.Damage;
    public void SetMissileDamage(float v) => missileStrategy.Damage = v;

    public float GetMissileInterval() => missileStrategy.Interval;
    public void SetMissileInterval(float v) => missileStrategy.Interval = v;

    // 필요시 증감도 지원
    public void AddMissileDamage(float delta)
    {
        missileStrategy.Damage += delta;
    } 
    public void AddMissileInterval(float delta)
    {
        missileStrategy.Interval += delta;
    } 
    public void AddMissileTurret() {
        missileTurretPrefab.SetActive(true);
        guidedMissileTurret.ActivateTurret(missileStrategy);
    }

    public float GetLaserInterval() => laserStrategy.IntervalSec;
    public void SetLaserInterval(float v) => laserStrategy.IntervalSec = v;
    public float GetLaserDamage() => laserStrategy.Damage;
    public void SetLaserDamage(float v) => laserStrategy.Damage = v;

    public void ActivateLaserTurret()
    {
        if (laserActivated) return;                 // 중복 방지
        if (laserTurret == null)
        {
            Debug.LogError("[Laser] laserTurret 미할당");
            return;
        }
        var go = laserTurret.gameObject;
        if (!go.activeSelf) go.SetActive(true);
        if (!laserTurret.enabled) laserTurret.enabled = true;

        // 그 다음 전략 활성화(코루틴 시작)
        laserTurret.ActivateTurret(laserStrategy);
        laserActivated = true;
        Debug.Log("[Laser] 레이저 포탑 활성화 완료");
    }
    void Update()
    {
        
    }
}