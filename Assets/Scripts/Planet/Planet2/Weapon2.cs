using System.Collections.Generic;
using UnityEngine;

public class Weapon2 : MonoBehaviour
{
    [Header("Pivot (공전 중심)")]
    public Transform pivotTransform; // 위성이 회전할 중심 Transform

    [Header("위성 설정")]
    public GameObject satellitePrefab; // 위성 프리팹
    [Range(1, 4)]
    public int maxSatellites = 4; // 최대 위성 개수
    private List<Satellite> satellites = new List<Satellite>(); // 위성 목록

    [Header("공전 범위 및 속도")]
    public float minRadius = 50f; // 최소 공전 반지름
    public float maxRadius = 250f; // 최대 공전 반지름
    public float speedConstantK = 50f; // 속도 비례 상수 K (K/R = 속도)

    [Header("부드러운 움직임 설정")]
    public float smoothSpeed = 5f; // 보간 속도 (값이 클수록 빠르게 목표 반지름에 도달)

    private float currentRadius; // 현재 위성들이 공전하는 반지름
    private float targetRadius;  // 마우스 위치에 의해 계산된 목표 반지름
    // 위성의 상태를 저장하는 내부 클래스
    private class Satellite
    {
        public Transform transform;
        public float angle; // 현재 각도 (Radian)
    }

    void Start()
    {
        // 1. 초기 위성 생성
        CreateSatellites();
        // 초기 반지름 설정
        currentRadius = minRadius;
    }

    void CreateSatellites()
    {
        float angleStep = 360f / maxSatellites; // 위성 간 초기 각도 간격
        for (int i = 0; i < maxSatellites; i++)
        {
            GameObject satGO = Instantiate(satellitePrefab, pivotTransform);
            satGO.name = "Satellite_" + i;

            Satellite newSat = new Satellite
            {
                transform = satGO.transform,
                // 초기 각도 설정 (Deg. -> Rad.)
                angle = (angleStep * i) * Mathf.Deg2Rad
            };
            satellites.Add(newSat);
        }
    }

    void Update()
    {
        // 2. 마우스 커서 위치에 따른 공전 반지름(R) 조절
        CalculateTargetRadiusByMouse();
        currentRadius = Mathf.Lerp(currentRadius, targetRadius, Time.deltaTime * smoothSpeed);
        // 3. 각 위성 공전 업데이트
        float angularSpeed = speedConstantK / currentRadius; // 각속도 (반지름에 반비례)

        foreach (var sat in satellites)
        {
            // 각도 업데이트 (시간과 속도를 곱함)
            sat.angle += angularSpeed * Time.deltaTime;

            // 각도에 따른 새로운 위치 계산 (Pivot 기준)
            float x = currentRadius * Mathf.Cos(sat.angle);
            float y = currentRadius * Mathf.Sin(sat.angle);

            sat.transform.localPosition = new Vector3(x, y, 0);

            // 각도가 2*PI (360도)를 넘으면 초기화 (필수는 아니지만 정리 차원)
            if (sat.angle > Mathf.PI * 2f)
            {
                sat.angle -= Mathf.PI * 2f;
            }
        }
    }
    void CalculateTargetRadiusByMouse()
    {
        // 마우스 커서의 World 좌표 가져오기
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        // Pivot과 마우스 커서 사이의 거리 D 계산
        float distance = Vector3.Distance(pivotTransform.position, mousePos);

        // 거리를 최소/최대 반지름 내로 제한 (Clamp)
        // 이 값을 즉시 currentRadius에 할당하지 않고 targetRadius에 저장합니다.
        targetRadius = Mathf.Clamp(distance, minRadius, maxRadius);
    }
}
