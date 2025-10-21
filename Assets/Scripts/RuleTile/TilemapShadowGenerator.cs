using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapShadowGenerator : MonoBehaviour
{
    public static TilemapShadowGenerator Instance { get; private set; }

    [Header("연결 설정")]
    [SerializeField]
    private Tilemap mainTilemap;

    [SerializeField]
    private Tilemap shadowTilemap;

    [SerializeField]
    private TileBase shadowTile;

    [Header("그림자 생성 설정")]
    [Tooltip("가장자리로부터 몇 칸까지를 테두리로 남겨둘지 결정합니다.")]
    [SerializeField]
    [Range(1, 10)]
    private int borderSize = 1;

    [Tooltip("실시간 그림자 업데이트 시, 계산 범위를 보정하기 위한 값입니다. 1.0이 기본이며, 그림자가 덜 지워지면 값을 늘리세요.")]
    [SerializeField]
    [Range(1.0f, 50.0f)]
    private float shadowUpdateMultiplier = 1.5f; // 기본값을 1.5로 넉넉하게 설정

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // void Start()
    // {
    //     GenerateShadow();
    // }

    public void GenerateInitialShadow()
    {
        GenerateShadow();
    }


    /// <summary>
    /// 지정된 월드 좌표 반경을 기반으로 주변 그림자를 다시 계산하고 업데이트합니다.
    /// </summary>
    /// <param name="centerPosition">업데이트할 중심 타일 위치</param>
    /// <param name="worldRadius">월드 단위의 폭발 반경</param>
    public void UpdateShadowsAround(Vector3Int centerPosition, float worldRadius)
    {
        // 1. 월드 반경을 타일맵 셀 크기로 나누어 '타일 단위 반경'으로 변환합니다.
        // 셀 크기가 0.2라면, 1.0 반경은 5칸의 타일 반경이 됩니다.
        float radiusInTiles = worldRadius / mainTilemap.cellSize.x;

        // 2. 사용자가 설정한 보정값(Multiplier)을 곱하고 올림하여 최종 반경을 정합니다.
        int finalTileRadius = Mathf.CeilToInt(radiusInTiles * shadowUpdateMultiplier);

        // 3. 업데이트할 전체 범위를 계산합니다. (계산된 반경 + 테두리 크기)
        int checkRadius = finalTileRadius + borderSize;

        BoundsInt checkBounds = new BoundsInt(
            centerPosition.x - checkRadius,
            centerPosition.y - checkRadius,
            0,
            (checkRadius * 2) + 1,
            (checkRadius * 2) + 1,
            1
        );

        foreach (var pos in checkBounds.allPositionsWithin)
        {
            if (mainTilemap.HasTile(pos) && !IsBorderTile(pos))
            {
                shadowTilemap.SetTile(pos, shadowTile);
            }
            else
            {
                shadowTilemap.SetTile(pos, null);
            }
        }
    }

    private bool IsBorderTile(Vector3Int position)
    {
        for (int x = -borderSize; x <= borderSize; x++)
        {
            for (int y = -borderSize; y <= borderSize; y++)
            {
                if (!mainTilemap.HasTile(position + new Vector3Int(x, y, 0)))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // 전체 그림자 생성 함수 (변경 없음)
    public void GenerateShadow()
    {
        if (mainTilemap == null || shadowTilemap == null || shadowTile == null) return;
        shadowTilemap.ClearAllTiles();
        BoundsInt bounds = mainTilemap.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (mainTilemap.HasTile(pos) && !IsBorderTile(pos))
            {
                shadowTilemap.SetTile(pos, shadowTile);
            }
        }
    }
}