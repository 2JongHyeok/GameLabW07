using System;
using UnityEngine;

// 이 스크립트를 모든 광물(Ore) 프리팹에 붙여주세요.
public class Ore : MonoBehaviour
{
    [Tooltip("이 광물의 종류를 설정하세요 (예: Coal, Iron)")]
    public OreType oreType;

    [Tooltip("이 광물 덩어리 하나가 몇 개로 취급될지 설정하세요.")]
    public int amount = 1;
    [Header("세계 좌표 경계 (기본값: top=270, bottom=-90, left=-90, right=90)")]
    [SerializeField] private float topBoundary = 150f;
    [SerializeField] private float bottomBoundary = -150f;
    [SerializeField] private float leftBoundary = -100f;
    [SerializeField] private float rightBoundary = 100f;
    [Header("벽 충돌 느낌 옵션")]
    [Tooltip("true면 벽에 닿은 축의 속도를 0으로 고정(미끄러짐 O). false면 반발 계수로 튕김.")]
    [SerializeField] private bool stopAtWall = true;

    [Tooltip("stopAtWall=false일 때만 사용. 0=완전 흡수, 1=완전 탄성(완전 튕김)")]
    [Range(0f, 1f)]
    [SerializeField] private float bounce = 0f;

    private Rigidbody2D rb;

    private void Awake()
    {
        TryGetComponent(out rb);
    }

    private void FixedUpdate()
    {
        Vector3 pos = transform.position;

        bool hitLeft = false, hitRight = false, hitTop = false, hitBottom = false;

        if (pos.x < leftBoundary) { pos.x = leftBoundary; hitLeft = true; }
        if (pos.x > rightBoundary) { pos.x = rightBoundary; hitRight = true; }
        if (pos.y > topBoundary) { pos.y = topBoundary; hitTop = true; }
        if (pos.y < bottomBoundary) { pos.y = bottomBoundary; hitBottom = true; }

        if (rb != null)
        {
            Vector2 v = rb.linearVelocity; // Unity 6 표기. (이전 버전이면 rb.velocity 사용)

            if (hitLeft && v.x < 0f) v.x = stopAtWall ? 0f : -v.x * bounce;
            if (hitRight && v.x > 0f) v.x = stopAtWall ? 0f : -v.x * bounce;
            if (hitTop && v.y > 0f) v.y = stopAtWall ? 0f : -v.y * bounce;
            if (hitBottom && v.y < 0f) v.y = stopAtWall ? 0f : -v.y * bounce;

            rb.linearVelocity = v;
        }

        transform.position = pos;
    }
}