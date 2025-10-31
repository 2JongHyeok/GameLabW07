using System.Collections.Generic;
using UnityEngine;

public class WeaponControlSwitcher : MonoBehaviour
{
    [Header("Planet1 - 수동 연결(선택)")]
    [SerializeField] private Weapon[] planet1Weapons;          // 직접 드래그해도 됨

    [Header("Planet1 - 자동 탐색(권장)")]
    [SerializeField] private Transform[] planet1Roots;         // 보통 Planets/Planet1/Weapons
    [SerializeField] private bool autoDiscoverPlanet1 = true;  // 비활성 포함 전부 스캔(GetComponentsInChildren<T>(true))

    [Header("Planet2 - 수동 연결(선택)")]
    [SerializeField] private Weapon2[] planet2Weapons;         // 여러 개 가능

    [Header("Planet2 - 자동 탐색(선택)")]
    [SerializeField] private Transform[] planet2Roots;         // 필요 시 Planets/Planet2/Weapons
    [SerializeField] private bool autoDiscoverPlanet2 = false;

    [Header("참조")]
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private ViewContext ctx;
    [SerializeField] private Core planet2Core;
    // 내부 캐시
    private readonly List<Weapon> p1Cache = new();
    private readonly List<Weapon2> p2Cache = new();
    private bool cachesBuilt;

    void Awake() { RebuildCaches(); }
    private void Start()
    {
        if (planet2Core != null)
        {
            planet2Core.OnDie += () => SetP2(false);
            planet2Core.OnRevive += () => {
                if (ctx.CurrentView == CameraType.Planet2)
                    SetP2(true);
            };
        }
    }
    void OnEnable()
    {
        if (!ctx) ctx = ViewContext.I;
        if (cameraSwitcher) cameraSwitcher.OnViewChanged += OnViewChanged;
        if (ctx) ctx.OnDockChanged += OnDockChanged;

        Apply(ctx != null ? ctx.CurrentView : CameraType.Planet1,
              ctx != null ? ctx.DockedPlanet : CameraType.Planet1);
    }

    void OnDisable()
    {
        if (cameraSwitcher) cameraSwitcher.OnViewChanged -= OnViewChanged;
        if (ctx) ctx.OnDockChanged -= OnDockChanged;
    }

    // ---- 캐시 재구성 ----
    [ContextMenu("Refresh Caches")]
    public void RebuildCaches()
    {
        p1Cache.Clear();
        p2Cache.Clear();

        // 1) 수동 배열
        if (planet1Weapons != null)
            foreach (var w in planet1Weapons) if (w) AddUnique(p1Cache, w);

        if (planet2Weapons != null)
            foreach (var w in planet2Weapons) if (w) AddUnique(p2Cache, w);

        // 2) 자동 탐색 (비활성 포함)
        if (autoDiscoverPlanet1 && planet1Roots != null)
        {
            foreach (var root in planet1Roots)
            {
                if (!root) continue;
                var found = root.GetComponentsInChildren<Weapon>(true);
                foreach (var w in found) if (w) AddUnique(p1Cache, w);
            }
        }

        if (autoDiscoverPlanet2 && planet2Roots != null)
        {
            foreach (var root in planet2Roots)
            {
                if (!root) continue;
                var found = root.GetComponentsInChildren<Weapon2>(true);
                foreach (var w in found) if (w) AddUnique(p2Cache, w);
            }
        }

        cachesBuilt = true;
    }

    private static void AddUnique<T>(List<T> list, T item)
    {
        if (!list.Contains(item)) list.Add(item);
    }

    // ---- 외부 통지(도킹/구성 변경) ----
    public void OnDockedPlanetChanged(CameraType docked)
    {
        if (ctx) ctx.SetDockedPlanet(docked);
        Apply(ctx != null ? ctx.CurrentView : CameraType.Planet1, docked);
    }

    private void OnDockChanged(CameraType docked)
    {
        Apply(ctx != null ? ctx.CurrentView : CameraType.Planet1, docked);
    }

    private void OnViewChanged(CameraType view)
    {
        if (ctx) ctx.SetCurrentView(view);
        Apply(view, ctx != null ? ctx.DockedPlanet : CameraType.Planet1);
    }

    // ---- 규칙 적용 ----
    private void Apply(CameraType view, CameraType docked)
    {
        if (!cachesBuilt) RebuildCaches();

        bool noDock = (docked == CameraType.SpaceShip);
        if (noDock)
        {
            SetP1(false);
            SetP2(false);
            return;
        }

        // 도킹 상태: "지금 보는 행성(View)"만 On
        SetP1(view == CameraType.Planet1);
        SetP2(view == CameraType.Planet2);
    }

    private void SetP1(bool on)
    {
        foreach (var w in p1Cache)
        {
            if (!w) continue;
            if (on) w.ActivateWeapon(); else w.DeactivateWeapon();
        }
    }

    private void SetP2(bool on)
    {
        if (planet2Core != null && planet2Core.IsDead)
            on = false;
        foreach (var w in p2Cache)
        {
            if (!w) continue;
            if (on) w.ActivateWeapon(); else w.DeactivateWeapon();
        }
    }

    // 서브무기(레벨/콤바인) 변경 후 호출
    public void NotifyWeaponTopologyChanged()
    {
        RebuildCaches();
        Apply(ctx != null ? ctx.CurrentView : CameraType.Planet1,
              ctx != null ? ctx.DockedPlanet : CameraType.Planet1);
    }
}
