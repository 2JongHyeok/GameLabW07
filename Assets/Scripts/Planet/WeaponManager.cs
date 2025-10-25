using UnityEngine;

public class SubWeaponManager : MonoBehaviour
{
    [Header("일반 무기")]
    [SerializeField] private GameObject subWeapon1;
    [SerializeField] private GameObject subWeapon2;
    [SerializeField] private GameObject subWeapon3;

    [Header("콤바인 무기")]
    [SerializeField] private GameObject subWeapon1Combine;
    [SerializeField] private GameObject subWeapon2Combine;
    [SerializeField] private GameObject subWeapon3Combine;

    private int level = 0;      // 0,1,2만 사용
    private bool toggle = false; // false=일반, true=Combine

    // 내부 배열(중복 제어용)
    private GameObject[] normals;
    private GameObject[] combines;

    void Start()
    {
        // 배열로 묶어서 일괄 제어
        normals = new[] { subWeapon1, subWeapon2, subWeapon3 };
        combines = new[] { subWeapon1Combine, subWeapon2Combine, subWeapon3Combine };

        UpdateWeaponState();
    }

    private void Update()
    {
        // TODO 이부분 나중에 키 바인딩만 뺄 것.
        // if (Input.GetKeyDown(KeyCode.K)) LevelUp();
        if (Input.GetKeyDown(KeyCode.Mouse1)) ToggleCombine();

    }

    // UI 버튼 등에 연결
    public void LevelUp()
    {
        Managers.Instance.WeaponModeText.SetActive(true);
        level = Mathf.Clamp(level + 1, 0, 2);
        UpdateWeaponState();
    }

    // 필요하면 직접 세팅도 제공
    public void SetLevel(int newLevel)
    {
        level = Mathf.Clamp(newLevel, 0, 2);
        UpdateWeaponState();
    }

    // UI의 Combine 버튼에 연결
    public void ToggleCombine()
    {
        toggle = !toggle;
        UpdateWeaponState();
    }

    // ---- 핵심 상태 전환 ----
    private void UpdateWeaponState()
    {
        // 모두 비활성화
        SetAll(normals, false);
        SetAll(combines, false);

        // 레벨별/토글별 활성화 규칙
        switch (level)
        {
            case 0:
                // 전부 꺼짐 (이미 처리됨)
                break;

            case 1:
                if (!toggle)
                {
                    // 일반: subWeapon1만 켜기
                    SetActiveSafe(subWeapon1, true);
                }
                else
                {
                    // Combine: subWeapon1Combine만 켜기
                    SetActiveSafe(subWeapon1Combine, true);
                }
                break;

            case 2:
                if (!toggle)
                {
                    // 일반: subWeapon2, subWeapon3 켜기
                    SetActiveSafe(subWeapon1, true);
                    SetActiveSafe(subWeapon2, true);
                    SetActiveSafe(subWeapon3, true);
                }
                else
                {
                    // Combine: subWeapon2Combine, subWeapon3Combine 켜기
                    SetActiveSafe(subWeapon1Combine, true);
                    SetActiveSafe(subWeapon2Combine, true);
                    SetActiveSafe(subWeapon3Combine, true);
                }
                break;
        }
    }

    // 유틸들
    private void SetAll(GameObject[] arr, bool active)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
            SetActiveSafe(arr[i], active);
    }

    private void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }
}
