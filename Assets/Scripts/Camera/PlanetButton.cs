using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;

public class PlanetButton : MonoBehaviour
{
    [SerializeField] private CinemachineCamera targetCam;
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private CameraType thisType;
    [SerializeField] private Image highlightImage;

    private void Start()
    {
        // 시작 시 이미지 비활성화
        if (highlightImage != null)
        {
            bool isCurrent = (ViewContext.I != null && ViewContext.I.CurrentView == thisType);
            highlightImage.enabled = isCurrent;
        }

        // ViewContext가 변경될 때마다 하이라이트 갱신
        if (ViewContext.I != null)
            ViewContext.I.OnViewChanged += OnViewChanged;
    }
    private void OnDestroy()
    {
        if (ViewContext.I != null)
            ViewContext.I.OnViewChanged -= OnViewChanged;
    }

    private void OnViewChanged(CameraType newView)
    {
        // 현재 보고 있는 View와 버튼의 타입이 같다면 이미지 On
        bool active = (newView == thisType);
        if (highlightImage != null)
            highlightImage.enabled = active;
    }
    public void OnClick()
    {
        if (!cameraSwitcher) return;

        if (thisType == CameraType.SpaceShip)
            cameraSwitcher.ActivateSpaceship();
        else
            cameraSwitcher.ActivatePlanet(targetCam);
    }
}
