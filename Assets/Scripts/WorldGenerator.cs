using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class WorldGenerator : MonoBehaviour
{
    [Header("핵심 연결 요소")]
    [Tooltip("모든 소행성이 그려질 메인 월드 타일맵")]
    [SerializeField] private Tilemap worldTilemap;

    [Header("월드 생성 설정")]
    [Tooltip("중심(0,0)에서부터 생성할 월드의 전체 반경")]
    [SerializeField] private float generationRadius = 1000f;

    [Tooltip("소행성을 배치할 격자의 크기. 작을수록 촘촘하게 검사합니다.")]
    [SerializeField] private int gridCellSize = 30;

    [Header("구역(Zone) 설정")]
    [Tooltip("생성할 모든 구역의 설정값(SO)들을 여기에 등록하세요.")]
    [SerializeField] private List<GenerationZoneSettingsSO> zoneSettings;

    [Tooltip("월드 타일맵에 연결된 AsteroidHealth 스크립트")]
    [SerializeField] private AsteroidHealth asteroidHealth;
    [Tooltip("씬에 있는 TilemapShadowGenerator 스크립트")]
    [SerializeField] private TilemapShadowGenerator shadowGenerator;

    // 게임이 시작될 때 월드 생성을 자동으로 실행합니다.
    void Start()
    {
        GenerateWorld();
    }

    /// <summary>
    /// 절차적 월드 생성을 시작하는 메인 함수입니다.
    /// </summary>
    public void GenerateWorld()
    {
        // 테스트를 위해 기존 타일을 모두 지웁니다.
        worldTilemap.ClearAllTiles();

        // generationRadius와 gridCellSize에 따라 격자를 순회합니다.
        for (float x = -generationRadius; x < generationRadius; x += gridCellSize)
        {
            for (float y = -generationRadius; y < generationRadius; y += gridCellSize)
            {
                Vector2 currentPosition = new Vector2(x, y);

                // 1. 현재 위치가 어떤 구역에 속하는지 확인합니다.
                float distanceFromCenter = Vector2.Distance(currentPosition, Vector2.zero);
                GenerationZoneSettingsSO currentZone = GetZoneForDistance(distanceFromCenter);

                // 유효한 구역이 아니면 (예: 중심의 빈 공간) 건너뜁니다.
                if (currentZone == null) continue;

                // 2. 이 위치에 소행성을 생성할지 확률(spawnChance)에 따라 결정합니다.
                if (Random.value > currentZone.spawnChance) continue;

                // 3. 이 구역의 소행성 풀에서 어떤 소행성을 생성할지 확률(weight)에 따라 선택합니다.
                GameObject asteroidPrefabToSpawn = SelectRandomAsteroid(currentZone.asteroidPool);
                if (asteroidPrefabToSpawn == null) continue;

                // 4. 생성하기 전에, 해당 위치가 비어있는지 확인합니다. (겹침 방지)
                CircleCollider2D prefabCollider = asteroidPrefabToSpawn.GetComponent<CircleCollider2D>();
                if (prefabCollider == null)
                {
                }
                else if (Physics2D.OverlapCircle(currentPosition, prefabCollider.radius))
                {
                    // 이미 무언가 있다면 건너뜁니다.
                    continue;
                }

                // 5. 모든 조건을 통과했으면, 소행성을 월드 타일맵에 '도장'처럼 찍습니다.
                StampAsteroid(currentPosition, asteroidPrefabToSpawn);
            }
        }
        // 1. 월드 생성이 끝났으니, AsteroidHealth에게 타일 초기화를 지시합니다.
        if (asteroidHealth != null)
        {
            asteroidHealth.InitializeFromGenerator();
        }

        // 2. 타일 초기화까지 끝났으니, ShadowGenerator에게 그림자 생성을 지시합니다.
        if (shadowGenerator != null)
        {
            shadowGenerator.GenerateInitialShadow();
        }


    }

    /// <summary>
    /// 주어진 거리에 해당하는 구역 설정(SO)을 찾아 반환합니다.
    /// </summary>
    private GenerationZoneSettingsSO GetZoneForDistance(float distance)
    {
        foreach (var zone in zoneSettings)
        {
            if (distance >= zone.minDistance && distance < zone.maxDistance)
            {
                return zone;
            }
        }
        return null; // 해당하는 구역이 없으면 null을 반환합니다.
    }

    /// <summary>
    /// 주어진 소행성 풀에서 설정된 가중치에 따라 무작위로 소행성 프리팹 하나를 선택합니다.
    /// </summary>
    private GameObject SelectRandomAsteroid(List<AsteroidSpawnData> pool)
    {
        if (pool == null || pool.Count == 0) return null;

        float totalWeight = pool.Sum(data => data.weight);
        float randomValue = Random.Range(0, totalWeight);

        foreach (var data in pool)
        {
            if (randomValue <= data.weight)
            {
                return data.asteroidPrefab;
            }
            randomValue -= data.weight;
        }
        return null;
    }

    /// <summary>
    /// 선택된 소행성 프리팹의 모양을 월드 타일맵의 특정 위치에 그대로 복사합니다.
    /// 이때 무작위로 회전 및 반전 변환을 적용합니다.
    /// </summary>
    private void StampAsteroid(Vector2 worldPosition, GameObject asteroidPrefab)
    {
        Tilemap prefabTilemap = asteroidPrefab.GetComponentInChildren<Tilemap>();
        if (prefabTilemap == null)
        {
            Debug.LogError($"{asteroidPrefab.name} 프리팹 안에 Tilemap이 없습니다!");
            return;
        }

        // --- 여기가 핵심 로직! (더 직관적인 방식으로 변경) ---
        // 1. 어떤 변환을 적용할지 무작위로 결정합니다.
        int rotationIndex = Random.Range(0, 4); // 0: 0도, 1: 90도, 2: 180도, 3: 270도
        bool mirrorX = Random.value > 0.5f;     // 수평 반전 여부
        bool mirrorY = Random.value > 0.5f;     // 수직 반전 여부

        // 디버깅을 위해 어떤 변환이 선택되었는지 확인하고 싶다면 아래 주석을 해제하세요.
        // Debug.Log($"Spawning with Rotation: {rotationIndex * 90} deg, MirrorX: {mirrorX}, MirrorY: {mirrorY}");

        // 프리팹 타일맵의 모든 타일 정보를 순회합니다.
        foreach (var pos in prefabTilemap.cellBounds.allPositionsWithin)
        {
            if (prefabTilemap.HasTile(pos))
            {
                TileBase tile = prefabTilemap.GetTile(pos);
                Vector3Int currentPos = pos;
                
                // 2. 결정된 값에 따라 타일의 상대 위치를 직접 계산합니다.
                // 2-1. 회전 적용
                switch (rotationIndex)
                {
                    case 1: // 90도
                        currentPos = new Vector3Int(-pos.y, pos.x, pos.z);
                        break;
                    case 2: // 180도
                        currentPos = new Vector3Int(-pos.x, -pos.y, pos.z);
                        break;
                    case 3: // 270도
                        currentPos = new Vector3Int(pos.y, -pos.x, pos.z);
                        break;
                    // case 0 (0도)는 아무것도 하지 않습니다.
                }

                // 2-2. 반전 적용
                if (mirrorX)
                {
                    currentPos.x *= -1;
                }
                if (mirrorY)
                {
                    currentPos.y *= -1;
                }

                // 3. 월드 타일맵에 찍힐 최종 위치를 계산하여 타일을 찍습니다.
                Vector3Int targetPos = worldTilemap.WorldToCell(worldPosition) + currentPos;
                worldTilemap.SetTile(targetPos, tile);
            }
        }
    }
}