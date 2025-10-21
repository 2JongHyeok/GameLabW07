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
    private IAttackStrategy laserStrategy;

    [SerializeField] private GameObject missileTurretPrefab;


    void Awake()
    {
        // 각 공격 전략을 미리 인스턴스화합니다.
        missileStrategy = new GuidedMissileAttack(guidedMissilePrefab, baseDamage: 20f, interval: 3f);
        bulletStrategy = new AutoTurretBulletAttack(bulletPrefab, 5f, 0.5f);
        laserStrategy = new AutoTurretLaserAttack();
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
    void Update()
    {
        

        // === 2. X 키 입력: 총알 포탑 활성화 ===
        //if (Input.GetKeyDown(KeyCode.X))
        //{
        //    if (bulletTurret != null)
        //    {
        //        bulletTurret.ActivateTurret(bulletStrategy);
        //        Debug.Log("[X] 총알 포탑이 활성화되었습니다.");
        //    }
        //    else
        //    {
        //        Debug.LogError("[X] 총알 포탑이 할당되지 않았습니다!");
        //    }
        //}

        //// === 3. C 키 입력: 레이저 포탑 활성화 ===
        //if (Input.GetKeyDown(KeyCode.C))
        //{
        //    if (laserTurret != null)
        //    {
        //        laserTurret.ActivateTurret(laserStrategy);
        //        Debug.Log("[C] 레이저 포탑이 활성화되었습니다.");
        //    }
        //    else
        //    {
        //        Debug.LogError("[C] 레이저 포탑이 할당되지 않았습니다!");
        //    }
        //}

        //// (선택 사항) P 키를 눌러 모든 포탑을 비활성화
        //if (Input.GetKeyDown(KeyCode.P))
        //{
        //    guidedMissileTurret?.DeactivateTurret();
        //    bulletTurret?.DeactivateTurret();
        //    laserTurret?.DeactivateTurret();
        //    Debug.Log("모든 포탑이 비활성화되었습니다.");
        //}
    }
}