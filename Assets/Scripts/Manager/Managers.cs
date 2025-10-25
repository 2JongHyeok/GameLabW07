using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class Managers : MonoBehaviour
{
    public static Managers Instance { get; private set; }

    public GameObject RestartPanel;
    public GameObject WeaponModeText;

    [Header("Gameplay Services")]
    public InventoryManger inventory;
    public TurretActivationManager turretActivationManager;
    public Weapon[] weapon;
    public SpaceshipMotor spaceshipMotor;
    public SpaceshipWeapon spaceshipWeapon;
    public TilemapManager tilemapManager;
    public Core core;
    public Core core2;
    public Planet planet;
    public Planet planet2;
    public SubWeaponManager subWeaponManager;

    [Header("Options")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool autoResolveInAwake = true;

    [Header("Initial Tunables - Weapon")]
    [SerializeField] private int initialWeaponDamage = 10;
    [SerializeField] private float initialWeaponFireRate = 0.1f;
    [SerializeField] private float initialWeaponRotationSpeed = 100f;
    [SerializeField] private float initialWeaponExplosionRadius = 1.5f;
    [SerializeField] private int initialWeaponBulletLevel = 1; // 기본 발사 수(level)

    [Header("Initial Tunables - Guided Missile")]
    [SerializeField] private float initialMissileDamage = 20f;
    [SerializeField] private float initialMissileInterval = 3f;

    [Header("Initial Tunables - Spaceship Motor")]
    [SerializeField] private float initialThrustPower = 60f;
    [SerializeField] private int initialThrustReductionPerOre = 8; // %

    [Header("Initial Tunables - Spaceship Mining (SpaceshipWeapon)")]
    [SerializeField] private int initialMiningDamage = 25;
    [SerializeField] private float initialMiningAttackSpeed = 0.5f;
    [SerializeField] private float initialMiningRadius = 2.0f;

    [Header("Initial Tunables - Planet Core")]
    [SerializeField] private int initialCoreMaxHP = 100;

    [Header("Initial Tunables - Planet Tiles (Shield)")]
    [SerializeField] private int initialTileMaxHP = 100;
    [SerializeField] private float initialTileRespawnDelay = 3f;


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
        if (autoResolveInAwake) AutoResolveRefs();
    }

    private void Start()
    {
        // Awake에서 자동 주입이 꺼져있는 경우, Start에서라도 확보
        if (!autoResolveInAwake) AutoResolveRefs();
        ApplyInitialTunables();
    }

    // 외부에서 수동으로도 호출 가능
    public void AutoResolveRefs()
    {
        inventory ??= FindAnyObjectByType<InventoryManger>();
        turretActivationManager ??= FindAnyObjectByType<TurretActivationManager>();
        // weapon ??= FindAnyObjectByType<Weapon>();
        spaceshipMotor ??= FindAnyObjectByType<SpaceshipMotor>();
        spaceshipWeapon ??= FindAnyObjectByType<SpaceshipWeapon>();
        tilemapManager ??= FindAnyObjectByType<TilemapManager>();
        // 수동 할당으로 변경
        /*core ??= FindAnyObjectByType<Core>();
        core2 ??= FindAnyObjectByType<Core>();
        planet ??= FindAnyObjectByType<Planet>();
        planet2 ?? FindAnyObjectByType<Planet>(); */ 
        subWeaponManager ??= FindAnyObjectByType<SubWeaponManager>();
    }

    // 씬 시작 시, 강화 기반 수치들의 기준값을 한 번에 셋팅합니다.
    private void ApplyInitialTunables()
    {
        // Weapon 기본값 셋업
        if (weapon != null)
        {
            foreach (var w in weapon)
            {
                w.SetDamage(initialWeaponDamage);
                w.SetAttackSpeed(initialWeaponFireRate);
                w.SetCannonSpeed(initialWeaponRotationSpeed);
                w.SetExplosionRange(initialWeaponExplosionRadius);
                w.level = Mathf.Max(1, initialWeaponBulletLevel);
            }
        }

        // Guided Missile(유도탄) 기본값 셋업
        if (turretActivationManager != null)
        {
            turretActivationManager.SetMissileDamage(initialMissileDamage);
            turretActivationManager.SetMissileInterval(initialMissileInterval);
        }

        // Spaceship Motor 기본값 셋업
        if (spaceshipMotor != null)
        {
            spaceshipMotor.SetThrustPower(initialThrustPower);
            spaceshipMotor.SetAllowedOreCount(initialThrustReductionPerOre);
        }

        // Spaceship Mining(SpaceshipWeapon) 기본값 셋업
        if (spaceshipWeapon != null)
        {
            spaceshipWeapon.SetMiningDamage(initialMiningDamage);
            spaceshipWeapon.SetMiningAttackSpeed(initialMiningAttackSpeed);
            spaceshipWeapon.SetMiningRadius(initialMiningRadius);
        }

        // Planet Core 기본값 셋업
        if (core != null)
        {
            core.maxHP = initialCoreMaxHP;
        }

        // Planet Tiles (Shield) 기본값 셋업 - Planet.cs의 SerializedField 사용
        if (planet != null)
        {
            planet.SetDelay(initialTileRespawnDelay);
        }
        
        if (core2 != null)
        {
            core2.maxHP = initialCoreMaxHP;
        }

        // Planet Tiles (Shield) 기본값 셋업 - Planet.cs의 SerializedField 사용
        if (planet2 != null)
        {
            planet2.SetDelay(initialTileRespawnDelay);
        }
        // (Planet.cs에서 직접 설정하므로 여기서는 별도 설정 불필요)
    }


    // 정적 접근 시 Instance 자동 확보
    private static Managers Ensure()
    {
        if (Instance != null) return Instance;

        Instance = FindAnyObjectByType<Managers>();
        if (Instance == null)
        {
            var go = new GameObject("Managers");
            Instance = go.AddComponent<Managers>();
        }

        if (Instance.dontDestroyOnLoad) DontDestroyOnLoad(Instance.gameObject);
        if (Instance.autoResolveInAwake) Instance.AutoResolveRefs();
        return Instance;
    }

    public void ActiveWeapon(int index)
    {
        weapon[index].gameObject.SetActive(true);
    }
    public void AddWeaponDamage(int damage)
    {
        foreach (var w in weapon)
        {
            w.AddDamage(damage);
        }
    }
    public void AddWeaponAttackSpeed(float rate)
    {
        foreach (var w in weapon)
        {
            w.AddAttackSpeed(rate);
        }
    }

    public void ActiveWeapon(float bulletNumber)
    {
        throw new NotImplementedException();
    }

    // ========== Planet Core 관련 메서드 ==========
    public void AddCoreMaxHP(int amount)
    {
        if (core == null) return;
        core.AddMaxHP(amount);
    }
    
    public void HealCoreHP(int amount)
    {
        if (core == null) return;
        core.HealHP(amount);
    }
    
    public void Add2CoreMaxHP(int amount)
    {
        if (core2 == null) return;
        core2.AddMaxHP(amount);
    }
    
    public void Heal2CoreHP(int amount)
    {
        if (core2 == null) return;
        core2.HealHP(amount);
    }

    // ========== Planet 타일(실드) 관련 메서드 ==========
    public void AddTileMaxHP(int amount)
    {
        if (planet == null) return;
        planet.AddTileMaxHP(amount);
    }
    
    public void ReduceTileRespawnDelay(float reductionAmount)
    {
        if (planet == null) return;
        planet.ReduceRespawnDelay(reductionAmount);
    }
    
    public void Add2TileMaxHP(int amount)
    {
        if (planet2 == null) return;
        planet2.AddTileMaxHP(amount);
    }
    
    public void Reduce2TileRespawnDelay(float reductionAmount)
    {
        if (planet2 == null) return;
        planet2.ReduceRespawnDelay(reductionAmount);
    }

    // ========== 게임 재시작 ==========
    public void RestartGame()
    {
        // DontDestroyOnLoad 오브젝트들을 먼저 정리
        if (Instance != null)
        {
            // Instance를 null로 설정하여 다음 씬에서 새로 생성되도록 함
            Instance = null;
            Destroy(gameObject);
        }
        
        // 현재 활성화된 씬을 다시 로드
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}
