using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Planet: MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private MyTileData defaultTileData;
    [SerializeField] private float respawnDelay = 3f;

    // 각 타일별 상태 저장용
    private Dictionary<Vector3Int, int> tileHPs = new Dictionary<Vector3Int, int>();
    private Dictionary<Vector3Int, TileBase> originalTiles = new Dictionary<Vector3Int, TileBase>();

    void Start()
    {
        // 맵 전체 초기화
        foreach (var pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
            {
                originalTiles[pos] = tilemap.GetTile(pos);
                tileHPs[pos] = defaultTileData.maxHP;
            }
        }
    }

    // 타일 데미지 처리
    public void DamageTile(Vector3Int cellPos, int damage)
    {
        if (!tileHPs.ContainsKey(cellPos)) return;
        tileHPs[cellPos] -= damage;

        if (tileHPs[cellPos] <= 0)
        {
            BreakTile(cellPos);
        }
    }

    private void BreakTile(Vector3Int cellPos)
    {
        tilemap.SetTile(cellPos, null); // 타일 제거
        StartCoroutine(RespawnTile(cellPos)); // 일정 시간 후 재생성
    }

    private IEnumerator RespawnTile(Vector3Int cellPos)
    {
        yield return new WaitForSeconds(respawnDelay);


        if (originalTiles.ContainsKey(cellPos))
        {
            tilemap.SetTile(cellPos, originalTiles[cellPos]);
            tileHPs[cellPos] = defaultTileData.maxHP;
        }
    }

    // 외부에서 respawnDelay를 줄이는 메서드
    public void ReduceRespawnDelay(float reductionAmount)
    {
        respawnDelay = Mathf.Max(0.1f, respawnDelay + reductionAmount); // 최소 0.1초
    }

    // 외부에서 타일 최대 HP를 증가시키는 메서드
    public void AddTileMaxHP(int amount)
    {
        defaultTileData.maxHP += amount;

        // 기존 타일들의 HP도 증가 (Dictionary를 순회하면서 수정)
        var positions = new List<Vector3Int>(tileHPs.Keys);
        foreach (var pos in positions)
        {
            tileHPs[pos] += amount;
        }
    }
    
    public void SetDelay(float newDelay)
    {
        respawnDelay = newDelay;
    }
}
