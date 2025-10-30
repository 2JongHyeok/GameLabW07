using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine; 
public class PlanetButton : MonoBehaviour
{
    [Header("전환할 시네머신 카메라")]
    [SerializeField] private CinemachineCamera targetCam;

    [Header("전환 방식 (권장: Priority)")]
    [SerializeField] private bool usePriority = true; // true면 Priority로, false면 활성/비활성 전환
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 10;

    // Button.onClick 에 연결
    public void SwitchToPlanetCamera()
    {
        if (!targetCam)
        {
            Debug.LogWarning("[PlanetButton] targetCam이 비어있습니다.");
            return;
        }

        // 비활성 객체 포함, 씬의 모든 CM 카메라 수집
        var allCams = FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include, FindObjectsSortMode.None
        );

        if (usePriority)
        {
            foreach (var cam in allCams)
                cam.Priority = (cam == targetCam) ? activePriority : inactivePriority;
        }
        else
        {
            // 대안: 게임오브젝트 활성/비활성 전환
            foreach (var cam in allCams)
                cam.gameObject.SetActive(cam == targetCam);
        }

        // 브레인 존재 체크(메인 카메라에 붙어 있어야 블렌드/전환이 적용됨)
        var brain = Camera.main ? Camera.main.GetComponent<CinemachineBrain>()
                                : FindFirstObjectByType<CinemachineBrain>(FindObjectsInactive.Include);
        if (!brain)
            Debug.LogWarning("[PlanetButton] 씬의 카메라에 CinemachineBrain 컴포넌트를 추가하세요.");

        Debug.Log($"[PlanetButton] 카메라 전환: {targetCam.name}");
    }
}
