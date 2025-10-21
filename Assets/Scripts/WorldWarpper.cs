// WorldWarper.cs
using UnityEngine;
using Unity.Cinemachine; // Cinemachine v3
using System;

[DisallowMultipleComponent]
public class WorldWarper : MonoBehaviour
{
    // 전역 워프 이벤트: 델타를 구독자들에게 브로드캐스트
    public static event Action<Vector3> OnWarped;

    [Header("맵 경계 설정")]
    [Tooltip("우주선이 이 Y좌표보다 위로 가면 아래에서 나타남")]
    public float topBoundary = 50f;

    [Tooltip("우주선이 이 Y좌표보다 아래로 가면 위에서 나타남")]
    public float bottomBoundary = -50f;

    [Tooltip("우주선이 이 X좌표보다 왼쪽으로 가면 오른쪽에서 나타남")]
    public float leftBoundary = -90f;

    [Tooltip("우주선이 이 X좌표보다 오른쪽으로 가면 왼쪽에서 나타남")]
    public float rightBoundary = 90f;

    [Header("Cinemachine (선택)")]
    [Tooltip("시네머신 Follow 타겟. 비우면 이 transform 사용")]
    [SerializeField] private Transform cinemachineFollowTarget;

    void LateUpdate()
    {
        Vector3 oldPos = transform.position;
        Vector3 newPos = oldPos;
        bool warped = false;

        // X축 경계 체크
        if (newPos.x > rightBoundary) { newPos.x = leftBoundary; warped = true; }
        else if (newPos.x < leftBoundary) { newPos.x = rightBoundary; warped = true; }

        // Y축 경계 체크
        if (newPos.y > topBoundary) { newPos.y = bottomBoundary; warped = true; }
        else if (newPos.y < bottomBoundary) { newPos.y = topBoundary; warped = true; }

        if (!warped) return;

        // 1) 실제 워프
        transform.position = newPos;
        Vector3 delta = newPos - oldPos;

        // 2) 짐/로프/기타 구독자들에게 같은 델타 워프 지시
        OnWarped?.Invoke(delta);

        // 3) 카메라도 같은 델타로 즉시 워프 (팬/댐핑 무시)
        Transform target = cinemachineFollowTarget != null ? cinemachineFollowTarget : transform;
        CinemachineCore.OnTargetObjectWarped(target, delta);

        // 4) (물리 동기화: Transform 직접 이동 객체가 있을 경우)
        Physics2D.SyncTransforms();
    }
}