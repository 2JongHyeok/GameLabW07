// MineralRuleTile.cs
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Mineral Rule Tile", menuName = "Tiles/Mineral Rule Tile")]
public class MineralRuleTile : RuleTile
{
    [Header("광물 설정")]
    [Tooltip("광물의 최대 내구도")]
    public int maxDurability = 50;

    [Tooltip("광물의 고유 색상. 게임 시작 시 이 색상으로 고정됩니다.")]
    public Color mineralColor = Color.white;

    [Tooltip("파괴되었을 때 생성될 아이템 프리팹")]
    public GameObject itemDropPrefab;

    // 타일이 맵에 그려질 때(에디터 포함) 호출되는 함수
    // 이 타일의 기본 데이터를 설정합니다.
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        // RuleTile의 기본 기능은 그대로 실행 (스프라이트, 규칙 등)
        base.GetTileData(position, tilemap, ref tileData);

        // ✨ 핵심: 이 타일의 색상은 우리가 설정한 mineralColor를 사용하도록 강제합니다.
        // 이렇게 하면 에디터와 게임 시작 시 모두 이 색으로 보입니다.
        tileData.color = this.mineralColor;
    }
}