using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

// 이 클래스는 SO에서 사용하므로 그대로 둡니다.
[System.Serializable]
public class DurabilityColorMapping
{
    [Tooltip("이 내구도 수치 '이상'일 경우 적용될 색상입니다.")]
    public int durabilityThreshold;
    [Tooltip("원래 타일 텍스처를 유지하려면 흰색에 가깝게 설정하세요.")]
    public Color color = Color.white;
}

public class TilemapManager : MonoBehaviour
{
    [Header("수동 설정")]
    [Tooltip("여기에 직접 추가한 타일맵들만 관리합니다.")]
    public Tilemap[] targetTilemaps; 

    [Header("공유 설정")]
    [Tooltip("모든 타일맵이 공유할 색상 설정 SO 파일을 연결해주세요.")]
    public DurabilityColorSettingsSO colorSettings;

    [Header("이벤트 채널 구독")]
    public TileDamageEventChannelSO onTileDamageChannel;

    private Dictionary<Vector3Int, int> currentDurabilityMap = new Dictionary<Vector3Int, int>();
    private Dictionary<Vector3Int, int> maxDurabilityMap = new Dictionary<Vector3Int, int>();

    private void OnEnable()
    {
        if (onTileDamageChannel != null)
        {
            onTileDamageChannel.OnEventRaised += ReceiveDamage;
        }
    }

    private void OnDisable()
    {
        if (onTileDamageChannel != null)
        {
            onTileDamageChannel.OnEventRaised -= ReceiveDamage;
        }
    }

    void Start()
    {
        InitializeDurability();
    }
    
    void InitializeDurability()
    {
        if (targetTilemaps == null || targetTilemaps.Length == 0)
        {
            return;
        }
        if (colorSettings == null)
        {
            return;
        }
        
        foreach (var tilemap in targetTilemaps)
        {
            if (tilemap == null) continue;
            
            tilemap.CompressBounds();
            foreach (var pos in tilemap.cellBounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(pos)) continue;
                var tileBase = tilemap.GetTile(pos);
                int maxDurability = 0;
                tilemap.SetTileFlags(pos, TileFlags.None);

                if (tileBase is MineralRuleTile mineralTile)
                {
                    maxDurability = mineralTile.maxDurability;
                    tilemap.SetColor(pos, mineralTile.mineralColor);
                }
                else if (tileBase is DurabilityRuleTile durabilityTile)
                {
                    maxDurability = durabilityTile.maxDurability;
                    // SO에서 색상 정보를 가져와 적용합니다.
                    tilemap.SetColor(pos, colorSettings.GetColorForDurability(maxDurability));
                }

                if (maxDurability > 0 && !maxDurabilityMap.ContainsKey(pos))
                {
                    maxDurabilityMap[pos] = maxDurability;
                    currentDurabilityMap[pos] = maxDurability;
                }
            }
        }
    }
    
    private void ReceiveDamage(TileDamageEvent damageEvent)
    {
        DamageTile(damageEvent.cellPosition, damageEvent.damageAmount);
    }
    
    public void DamageTile(Vector3Int cellPosition, int damage)
    {
        if (!currentDurabilityMap.ContainsKey(cellPosition)) return;
        Tilemap targetTilemap = GetTilemapAtPosition(cellPosition);
        if (targetTilemap == null) return;
        
        var tileBeingDamaged = targetTilemap.GetTile(cellPosition);
        int newDurability = currentDurabilityMap[cellPosition] - damage;
        currentDurabilityMap[cellPosition] = newDurability;

        if (newDurability <= 0)
        {
            if (tileBeingDamaged is MineralRuleTile mineralTile && mineralTile.itemDropPrefab != null)
            {
                Vector3 spawnPosition = targetTilemap.GetCellCenterWorld(cellPosition);
                Instantiate(mineralTile.itemDropPrefab, spawnPosition, Quaternion.identity);
            }
            targetTilemap.SetTile(cellPosition, null);
            currentDurabilityMap.Remove(cellPosition);
            maxDurabilityMap.Remove(cellPosition);
        }
        else
        {
            if (!(tileBeingDamaged is MineralRuleTile))
            {
                // 데미지를 입었을 때도 SO에서 색상 정보를 가져옵니다.
                targetTilemap.SetColor(cellPosition, colorSettings.GetColorForDurability(newDurability));
            }
        }
    }
    
    private Tilemap GetTilemapAtPosition(Vector3Int cellPosition)
    {
        foreach (var tilemap in targetTilemaps)
        {
            if (tilemap != null && tilemap.HasTile(cellPosition))
            {
                return tilemap;
            }
        }
        return null;
    }
}