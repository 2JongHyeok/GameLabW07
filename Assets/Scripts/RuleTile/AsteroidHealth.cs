using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class AsteroidHealth : MonoBehaviour
{
    [Header("공유 설정")]
    [Tooltip("모든 소행성이 공유할 색상 설정 SO 파일을 연결해주세요.")]
    public DurabilityColorSettingsSO colorSettings;

    public Tilemap myTilemap { get; private set; }

    private Dictionary<Vector3Int, int> currentDurabilityMap = new Dictionary<Vector3Int, int>();
    private Dictionary<Vector3Int, int> maxDurabilityMap = new Dictionary<Vector3Int, int>();

    void Awake()
    {
        myTilemap = GetComponent<Tilemap>();
        if (myTilemap == null)
        {
        }
    }

    // void Start()
    // {
    //     InitializeDurability();
    // }

    public void InitializeFromGenerator()
    {
        InitializeDurability();
    }


    void InitializeDurability()
    {
        if (myTilemap == null) return;


        myTilemap.CompressBounds();

        // 1. 비어있는 리스트를 먼저 생성합니다.
        List<Vector3Int> positions = new List<Vector3Int>();
        
        // 2. foreach 루프를 돌면서 모든 위치를 리스트에 직접 추가합니다.
        foreach (var pos in myTilemap.cellBounds.allPositionsWithin)
        {
            positions.Add(pos);
        }

        foreach (var pos in positions)
            {
                if (!myTilemap.HasTile(pos)) continue;
                
                TileBase tileBase = myTilemap.GetTile(pos);

                // ✨ --- 여기가 핵심 로직! --- ✨
                // 1. 만약 현재 타일이 '랜덤 스포너 타일'이라면?
                if (tileBase is RandomizedSpawnerTile spawnerTile)
                {
                    // 2. 스포너에게서 확률에 따른 결과 타일을 받아옵니다.
                    TileBase newTile = spawnerTile.GetRandomOutcome();

                    if (newTile != null)
                    {
                        // 3. 현재 위치의 '스포너 타일'을 받아온 '결과 타일'로 교체합니다!
                        myTilemap.SetTile(pos, newTile);
                        // 4. 방금 교체한 새 타일로 tileBase 변수를 업데이트하여, 아래의 기존 로직이 처리할 수 있도록 합니다.
                        tileBase = newTile;
                    }
                    else
                    {
                        // 변할 타일이 없으면 그냥 지워버립니다.
                        myTilemap.SetTile(pos, null);
                        continue; // 아래 로직을 실행할 필요가 없으므로 다음 칸으로 넘어갑니다.
                    }
                }
            int maxDurability = 0;
            myTilemap.SetTileFlags(pos, TileFlags.None);

            if (tileBase is MineralRuleTile mineralTile)
            {
                maxDurability = mineralTile.maxDurability;
                myTilemap.SetColor(pos, mineralTile.mineralColor);
            }
            else if (tileBase is DurabilityRuleTile durabilityTile)
            {
                maxDurability = durabilityTile.maxDurability;
                // 이제 색상 정보를 SO에서 직접 가져옵니다.
                myTilemap.SetColor(pos, colorSettings.GetColorForDurability(maxDurability));
            }

            if (maxDurability > 0)
            {
                maxDurabilityMap[pos] = maxDurability;
                currentDurabilityMap[pos] = maxDurability;
            }
        }
    }
    
    public void ApplyDamage(Vector3Int cellPosition, int damage)
    {
        if (!currentDurabilityMap.ContainsKey(cellPosition)) return;

        var tileBeingDamaged = myTilemap.GetTile(cellPosition);
        int newDurability = currentDurabilityMap[cellPosition] - damage;
        currentDurabilityMap[cellPosition] = newDurability;

        if (newDurability <= 0)
        {
            if (tileBeingDamaged is MineralRuleTile mineralTile && mineralTile.itemDropPrefab != null)
            {
                Vector3 spawnPosition = myTilemap.GetCellCenterWorld(cellPosition);
                GameObject spawnedOre = Instantiate(mineralTile.itemDropPrefab, spawnPosition, Quaternion.identity);

                // 채광량 증가 (광물이 생성되어 채집 가능한 상태가 되었을 때)
                Ore oreComponent = spawnedOre.GetComponent<Ore>();
                
                
                // [Log] 광석 채광량 증가
                if (oreComponent != null && Managers.Instance != null && Managers.Instance.inventory != null)
                {
                    Managers.Instance.inventory.IncrementMinedAmount(oreComponent.oreType, oreComponent.amount);
                }
            }

            myTilemap.SetTile(cellPosition, null);
            currentDurabilityMap.Remove(cellPosition);
            maxDurabilityMap.Remove(cellPosition);
        }
        else
        {
            if (tileBeingDamaged is MineralRuleTile mineralTile)
            {
                // 1. 광석의 원래 색상을 가져와 HSV로 변환합니다.
                Color originalColor = mineralTile.mineralColor;
                Color.RGBToHSV(originalColor, out float h, out float s, out float v_original);

                // 2. 최대 내구도를 가져옵니다.
                float maxDurability = maxDurabilityMap[cellPosition];
                
                // 3. 현재 내구도 비율을 계산합니다.
                float durabilityRatio = newDurability / maxDurability;

                // ✨ --- 여기가 핵심 수정사항! --- ✨
                // 4. 어두워질 최저 밝기를 설정합니다. (예: 원래 밝기의 40%까지)
                float minBrightnessFactor = 0.4f;
                float minBrightness = v_original * minBrightnessFactor;

                // 5. '최소 밝기'와 '원래 밝기' 사이를 내구도 비율에 따라 보간합니다.
                // 이렇게 하면 원래 밝기(v_original)보다 절대 밝아지지 않습니다.
                float newBrightness = Mathf.Lerp(minBrightness, v_original, durabilityRatio);

                // 6. 계산된 새로운 밝기로 색상을 생성하여 적용합니다.
                Color newColor = Color.HSVToRGB(h, s, newBrightness);
                myTilemap.SetColor(cellPosition, newColor);
            }
            else // 일반 돌 타일의 경우
            {
                myTilemap.SetColor(cellPosition, colorSettings.GetColorForDurability(newDurability));
            }
        }
    }
}