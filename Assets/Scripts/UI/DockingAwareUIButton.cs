using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DockingAwareUIButton : MonoBehaviour
{
    public enum Kind { Spaceship, Planet1, Planet2 }

    [SerializeField] private Kind kind;
    [SerializeField] private Button btn;
    [SerializeField] private GameObject rootForVisibility; // Planet2 버튼 숨김/표시용(없으면 this.gameObject)
    [SerializeField] private CameraSwitcher cameraSwitcher;

    void OnEnable()
    {
        if (!btn) btn = GetComponentInChildren<Button>(true);

        if (ViewContext.I != null)
        {
            ViewContext.I.OnDockChanged += HandleDockChanged;
            ViewContext.I.OnPlanet2UnlockChanged += HandleUnlockChanged;
            ViewContext.I.OnViewChanged += HandleViewChanged; 
        }
        // cameraSwitcher 참조는 선택(없어도 동작하도록)
        if (cameraSwitcher != null)
            cameraSwitcher.OnViewChanged += HandleViewChanged;

        Refresh();            // 첫 프레임 즉시 1회 갱신
    }

    void OnDisable()
    {
        if (ViewContext.I != null)
        {
            ViewContext.I.OnDockChanged -= HandleDockChanged;
            ViewContext.I.OnPlanet2UnlockChanged -= HandleUnlockChanged;
            ViewContext.I.OnViewChanged -= HandleViewChanged;
        }
        if (cameraSwitcher != null)
            cameraSwitcher.OnViewChanged -= HandleViewChanged;
    }

    private void HandleDockChanged(CameraType _) => Refresh();
    private void HandleUnlockChanged(bool _) => Refresh();
    private void HandleViewChanged(CameraType _) => Refresh(); 


    void Refresh()
    {
        var ctx = ViewContext.I;
        if (!ctx) return;

        // Planet2 버튼 가시성
        if (kind == Kind.Planet2)
        {
            var go = rootForVisibility ? rootForVisibility : gameObject;
            go.SetActive(ctx.Planet2Unlocked);
            Debug.Log("버튼 킴");
        }

        // 인터랙션 규칙
        bool interactable = true;
        switch (kind)
        {
            case Kind.Spaceship:
                interactable = ((ctx.DockedPlanet == CameraType.SpaceShip) &&(ctx.CurrentView) != CameraType.SpaceShip);
                break;
            case Kind.Planet1:
                interactable = (ctx.CurrentView != CameraType.Planet1);
                break;
            case Kind.Planet2:
                interactable = ctx.Planet2Unlocked && (ctx.CurrentView != CameraType.Planet2);
                break;
        }
        if (btn) btn.interactable = interactable;
        Debug.Log(kind + "는 " + interactable);
    }
}
