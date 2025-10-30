using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;

public class PlanetButton : MonoBehaviour
{
    [SerializeField] private CinemachineCamera targetCam;
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private CameraType thisType; // 버튼 목적지(Planet1/Planet2/SpaceShip)

    void Start()
    {
        // Planet2 버튼이면 잠금 상태에선 숨김
        if (thisType == CameraType.Planet2 && !ViewContext.I.Planet2Unlocked)
            gameObject.SetActive(false);
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
