// GenerationZoneSettingsSO.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Zone Settings", menuName = "Procedural Gen/Generation Zone Settings")]
public class GenerationZoneSettingsSO : ScriptableObject
{
    [Header("거리 설정")]
    [Tooltip("이 구역이 시작되는 중심으로부터의 최소 거리")]
    public float minDistance = 100f;

    [Tooltip("이 구역이 끝나는 중심으로부터의 최대 거리")]
    public float maxDistance = 500f;

    [Header("스폰 확률 설정")]
    [Tooltip("이 구역의 빈 공간에 소행성이 생성될 기본 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float spawnChance = 0.05f; // 5% 확률

    [Header("소행성 풀 (Pool)")]
    [Tooltip("이 구역에서 스폰될 수 있는 소행성들의 목록과 각각의 확률 가중치")]
    public List<AsteroidSpawnData> asteroidPool;
}