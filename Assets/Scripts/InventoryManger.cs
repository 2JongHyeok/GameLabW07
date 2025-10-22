using System.Collections.Generic;
using UnityEngine;

public class InventoryManger : MonoBehaviour
{
    public OreSO[] orePools;
    public int[] OreList;
    [SerializeField] private InventoryUI inventoryUI;
    
    // 인게임 광물 획득 총량
    public Dictionary<OreType, int> totalOresCollected = new Dictionary<OreType, int>()
    {
        { OreType.Coal, 0 },
        { OreType.Iron, 0 },
        { OreType.Gold, 0 },
        { OreType.Diamond, 0 }
    };

    private void Start()
    {
        OreList = new int[orePools.Length];
        foreach (var item in orePools)
        {
            OreList[(int)item.oreType] = 0;
        }
        inventoryUI.CreateOreUI(this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            AddOre(OreType.Coal, 100);
            AddOre(OreType.Iron, 100);
            AddOre(OreType.Gold, 100);
            AddOre(OreType.Diamond, 100);
            Debug.Log("Added 100 of each ore type.");
        }
    }
    public void AddOre(OreType oreType, int amount)
    {
        OreList[(int)oreType] += amount;
        inventoryUI.UpdateOreUI(oreType, OreList[(int)oreType]);
        totalOresCollected[oreType] += amount;
    }
    public bool RemoveOre(OreType oreType, int amount)
    {
        if (OreList[(int)oreType] >= amount)
        {
            OreList[(int)oreType] -= amount;
            inventoryUI.UpdateOreUI(oreType, OreList[(int)oreType]);
            return true;
        }
        return false;
    }

    // BaseForgeSO의 비용 체크
    public bool CheckOre(BaseForgeSO forgeSO)
    {
        if (forgeSO == null) return false;
        
        if (OreList[(int)OreType.Coal] < forgeSO.coalCost) return false;
        if (OreList[(int)OreType.Iron] < forgeSO.ironCost) return false;
        if (OreList[(int)OreType.Gold] < forgeSO.goldCost) return false;
        if (OreList[(int)OreType.Diamond] < forgeSO.diamondCost) return false;
        
        return true;
    }
    
    // BaseForgeSO의 비용 차감
    public bool ConsumeOre(BaseForgeSO forgeSO)
    {
        if (!CheckOre(forgeSO)) return false;
        
        RemoveOre(OreType.Coal, forgeSO.coalCost);
        RemoveOre(OreType.Iron, forgeSO.ironCost);
        RemoveOre(OreType.Gold, forgeSO.goldCost);
        RemoveOre(OreType.Diamond, forgeSO.diamondCost);
        
        return true;
    }
    
    // 광물 총 획득량 로그 출력용 메서드
    public string GetTotalOresCollectedAsString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var kvp in totalOresCollected)
        {
            sb.Append($"{kvp.Key.ToString()}: {kvp.Value}");
            sb.Append(", ");
        }
        if (sb.Length >= 2)
        {
            sb.Length -= 2; // 마지막 ", " 제거
        }
        return sb.ToString();
    }
}
