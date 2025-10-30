using UnityEngine;
using UnityEngine.UI;

public class DockingAwareUIButton : MonoBehaviour
{
    public enum Kind { Spaceship, Planet1, Planet2 }

    [SerializeField] private Kind kind;
    [SerializeField] private Button btn;
    [SerializeField] private GameObject rootForVisibility; // Planet2 버튼 숨김/표시용(없으면 this.gameObject)

    void Awake()
    {
        if (!btn) btn = GetComponent<Button>();
        ViewContext.I.OnDockChanged += _ => Refresh();
        ViewContext.I.OnPlanet2UnlockChanged += _ => Refresh();
    }

    void Start() => Refresh();

    void OnDestroy()
    {
        if (ViewContext.I != null)
        {
            ViewContext.I.OnDockChanged -= _ => Refresh(); // 람다 저장 안되니 안전하게 null 체크만
            ViewContext.I.OnPlanet2UnlockChanged -= _ => Refresh();
        }
    }

    void Refresh()
    {
        var ctx = ViewContext.I;
        if (!ctx) return;

        // Planet2 버튼 가시성
        if (kind == Kind.Planet2)
        {
            var go = rootForVisibility ? rootForVisibility : gameObject;
            go.SetActive(ctx.Planet2Unlocked);
        }

        // 인터랙션 규칙
        bool interactable = true;
        switch (kind)
        {
            case Kind.Spaceship:
                // 4) 도킹 시 우주선 버튼 비활성
                interactable = (ctx.DockedPlanet == CameraType.SpaceShip);
                break;
            case Kind.Planet1:
                // 5) Planet1에 도킹한 상태면 Planet1 버튼 비활성
                interactable = (ctx.DockedPlanet != CameraType.Planet1);
                break;
            case Kind.Planet2:
                // Planet2에 도킹한 상태면 Planet2 버튼 비활성
                interactable = (ctx.DockedPlanet != CameraType.Planet2) && ctx.Planet2Unlocked;
                break;
        }
        if (btn) btn.interactable = interactable;
    }
}
