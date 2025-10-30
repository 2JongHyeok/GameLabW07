// ViewContext.cs
using UnityEngine;

public class ViewContext : MonoBehaviour
{
    public static ViewContext I { get; private set; }

    public CameraType CurrentView { get; private set; } = CameraType.Planet1;   // 시작: Planet1 보고 있음
    public CameraType DockedPlanet { get; private set; } = CameraType.Planet1;  // 시작: Planet1에 도킹(=조작 가능)
    public bool Planet2Unlocked { get; private set; } = false;

    public System.Action<CameraType> OnDockChanged;
    public System.Action<bool> OnPlanet2UnlockChanged;

    void Awake() => I = this;

    public void SetCurrentView(CameraType t) => CurrentView = t;

    public void SetDockedPlanet(CameraType t)
    {
        DockedPlanet = t;
        OnDockChanged?.Invoke(t);
    }

    public void SetPlanet2Unlocked(bool v)
    {
        Planet2Unlocked = v;
        OnPlanet2UnlockChanged?.Invoke(v);
    }
}
