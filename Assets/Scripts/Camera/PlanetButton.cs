using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;

public class PlanetButton : MonoBehaviour
{
    [SerializeField] private CinemachineCamera targetCam;
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private CameraType thisType;

   

    public void OnClick()
    {
        if (!cameraSwitcher) return;

        if (thisType == CameraType.SpaceShip)
            cameraSwitcher.ActivateSpaceship();
        else
            cameraSwitcher.ActivatePlanet(targetCam);
    }
}
