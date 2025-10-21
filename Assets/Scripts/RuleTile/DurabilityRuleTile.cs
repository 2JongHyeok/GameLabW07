// DurabilityRuleTile.cs
using UnityEngine;
using UnityEngine.Tilemaps;

// 에디터의 Create 메뉴에 이 타일을 만들 수 있는 옵션을 추가해줍니다.
[CreateAssetMenu(fileName = "New Durability Rule Tile", menuName = "Tiles/Durability Rule Tile")]
public class DurabilityRuleTile : RuleTile
{
    // ✨ 이제 이 스크립트에는 내구도 데이터만 깔끔하게 남습니다.
    public int maxDurability = 100;
}