using UnityEngine;

public class PlanetCamManager : MonoBehaviour
{
    #region 변수들
    static public PlanetCamManager instance;

    private CameraType currentCam = CameraType.Planet1;
    #endregion

    #region 유니티 기본 함수들
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }
    #endregion

    public void ChangeCameraType(CameraType type)
    {
        currentCam = type;
    }
}
