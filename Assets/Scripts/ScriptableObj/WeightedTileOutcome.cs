// WeightedTileOutcome.cs
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable] // 이 어트리뷰트가 있어야 인스펙터 창에 보입니다.
public class WeightedTileOutcome
{
    [Tooltip("실제로 생성될 타일 (예: CoalOre_Tile, Stone_Tile 등)")]
    public TileBase resultingTile;

    [Tooltip("이 타일이 선택될 확률 가중치. 높을수록 잘 나옵니다. (꼭 합이 100일 필요 없음)")]
    public float weight = 10f;
}