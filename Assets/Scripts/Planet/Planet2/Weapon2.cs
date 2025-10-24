using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Weapon2 : MonoBehaviour
{
    [Header("Pivot (공전 중심)")]
    public Transform pivotTransform; // 위성이 회전할 중심 Transform

    // -----------------------------------------------------
    [Header("궤도 시각화")]
    public LineRenderer orbitLineRenderer;   // OrbitPath 오브젝트의 LineRenderer 컴포넌트
    [Min(3)] public int resolution = 60;     // 원을 그릴 점의 개수 (loop 사용 시 = positionCount)
    // -----------------------------------------------------

    [Header("위성 설정")]
    public GameObject satellitePrefab;       // 위성 프리팹
    [Range(1, 4)] public int maxSatellites = 4; // 최대 위성 개수
    private readonly List<Satellite> satellites = new(); // 위성 목록

    [Header("공전 범위 및 속도")]
    public float minRadius = 5f;     // 테스트가 쉬운 작은 반지름
    public float maxRadius = 20f;    // 오쏘 카메라에서 보이는 값으로 축소
    public float speedConstantK = 50f; // 속도 비례 상수 K (K/R = 속도)

    [Header("부드러운 움직임 설정")]
    public float smoothSpeed = 5f; // 보간 속도 (값이 클수록 빠르게 목표 반지름에 도달)

    private float currentRadius; // 현재 위성들이 공전하는 반지름 (부드럽게 변화)
    private float targetRadius;  // 마우스 위치에 의해 계산된 목표 반지름 (즉각적 변화)

    // 위성의 상태를 저장하는 내부 클래스
    private class Satellite
    {
        public Transform transform;
        public float angle; // 현재 각도 (Radian)
    }

    void Start()
    {
        if (pivotTransform == null) pivotTransform = transform;

        // 1) 초기 위성 생성 및 상태 초기화
        CreateSatellites();

        // 2) 초기 반지름 설정
        currentRadius = Mathf.Max(0.001f, minRadius);
        targetRadius = currentRadius;

        // 3) 궤도 시각화 초기 설정
        InitializeOrbitPath();

        // 4) 시작 시 한 번 그려주기(에디터에서도 미리 확인)
        UpdateOrbitPath();
    }

    void OnValidate()
    {
        // 에디터 값 변경 시에도 안정적으로 유지
        if (maxSatellites < 1) maxSatellites = 1;
        if (resolution < 3) resolution = 3;
        if (minRadius < 0f) minRadius = 0f;
        if (maxRadius < minRadius) maxRadius = minRadius + 0.001f;
    }

    void CreateSatellites()
    {
        if (satellitePrefab == null) return;
        if (pivotTransform == null) pivotTransform = transform;

        // 기존 생성물 정리
        satellites.Clear();
        for (int i = pivotTransform.childCount - 1; i >= 0; --i)
        {
            var child = pivotTransform.GetChild(i);
            if (Application.isPlaying)
            {
                if (child.name.StartsWith("Satellite_"))
                    Destroy(child.gameObject);
            }
            else
            {
                if (child.name.StartsWith("Satellite_"))
                    DestroyImmediate(child.gameObject);
            }
        }

        float angleStep = 360f / maxSatellites; // 위성 간 초기 각도 간격
        for (int i = 0; i < maxSatellites; i++)
        {
            // 위성을 Pivot의 자식으로 생성
            GameObject satGO = Instantiate(satellitePrefab, pivotTransform);
            satGO.name = "Satellite_" + i;

            Satellite newSat = new Satellite
            {
                transform = satGO.transform,
                // 초기 각도 설정 (Degrees -> Radians)
                angle = (angleStep * i) * Mathf.Deg2Rad
            };
            // 초기 위치도 반영
            float x = currentRadius * Mathf.Cos(newSat.angle);
            float y = currentRadius * Mathf.Sin(newSat.angle);
            newSat.transform.localPosition = new Vector3(x, y, 0f);

            satellites.Add(newSat);
        }
    }

    void InitializeOrbitPath()
    {
        if (orbitLineRenderer == null) return;

        // Pivot 기준 local 좌표를 사용할 것이므로 월드 스페이스 끔
        orbitLineRenderer.useWorldSpace = false;

        // 원을 자동으로 닫도록 loop 사용
        orbitLineRenderer.loop = true;
        orbitLineRenderer.positionCount = Mathf.Max(3, resolution);

        // 두께/코너/캡(가독성 향상)
        if (orbitLineRenderer.startWidth <= 0f && orbitLineRenderer.endWidth <= 0f)
        {
            orbitLineRenderer.startWidth = 0.25f;
            orbitLineRenderer.endWidth = 0.25f;
        }
        orbitLineRenderer.numCornerVertices = 4;
        orbitLineRenderer.numCapVertices = 2;



        // 궤도 오브젝트를 Pivot의 자식으로 두고 로컬 원점 정렬
        if (orbitLineRenderer.transform.parent != pivotTransform)
            orbitLineRenderer.transform.SetParent(pivotTransform, worldPositionStays: false);

        orbitLineRenderer.transform.localPosition = Vector3.zero;
        orbitLineRenderer.transform.localRotation = Quaternion.identity;
        orbitLineRenderer.transform.localScale = Vector3.one;
    }

    void Update()
    {
        // 1) 마우스 위치 기반 목표 반지름 계산 (즉각적)
        CalculateTargetRadiusByMouse();

        // 2) 현재 반지름을 목표 반지름까지 부드럽게 보간
        currentRadius = Mathf.Lerp(currentRadius, targetRadius, Time.deltaTime * smoothSpeed);
        currentRadius = Mathf.Max(0.001f, currentRadius);

        // 3) 궤도 업데이트
        UpdateOrbitPath();

        // 4) 위성 공전 업데이트 (반지름이 클수록 느림)
        float angularSpeed = speedConstantK / currentRadius;

        foreach (var sat in satellites)
        {
            // 각도 업데이트
            sat.angle += angularSpeed * Time.deltaTime;

            // 극좌표계 → Pivot 기준 localPosition
            float x = currentRadius * Mathf.Cos(sat.angle);
            float y = currentRadius * Mathf.Sin(sat.angle);
            sat.transform.localPosition = new Vector3(x, y, 0f);

            // 각도 정규화
            if (sat.angle > Mathf.PI * 2f)
                sat.angle -= Mathf.PI * 2f;
        }
    }

    void CalculateTargetRadiusByMouse()
    {
        if (Camera.main == null || pivotTransform == null)
            return;

        // 마우스 커서의 World 좌표 가져오기
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = pivotTransform.position.z;

        // Pivot과 마우스 커서 사이의 거리 D 계산
        float distance = Vector3.Distance(pivotTransform.position, mousePos);

        // 거리를 최소/최대 반지름 내로 제한
        targetRadius = Mathf.Clamp(distance, minRadius, maxRadius);
    }

    void UpdateOrbitPath()
    {
        if (orbitLineRenderer == null) return;

        int count = Mathf.Max(3, resolution);
        if (orbitLineRenderer.loop == false || orbitLineRenderer.positionCount != count)
            orbitLineRenderer.positionCount = count;

        // 원을 그리기 위한 각도 간격
        float angleStep = (2f * Mathf.PI) / count;

        // loop=true 이므로 0..count-1만 채우면 자동으로 닫힘
        for (int i = 0; i < count; i++)
        {
            float a = angleStep * i;
            float x = currentRadius * Mathf.Cos(a);
            float y = currentRadius * Mathf.Sin(a);

            // useWorldSpace=false → Pivot 로컬 좌표로 기록
            orbitLineRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }
    }
}
