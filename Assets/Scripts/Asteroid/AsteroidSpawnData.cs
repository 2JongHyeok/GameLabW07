// AsteroidSpawnData.cs
using UnityEngine;

[System.Serializable] // 인스펙터에 보이게 하는 어트리뷰트
public class AsteroidSpawnData
{
    [Tooltip("스폰될 소행성 프리팹")]
    public GameObject asteroidPrefab;

    [Tooltip("이 소행성이 선택될 확률 가중치 (높을수록 잘 나옴)")]
    public float weight = 10f;
}