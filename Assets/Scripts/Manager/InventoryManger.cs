using System.Collections.Generic;
using UnityEngine;

public class InventoryManger : MonoBehaviour
{
    public OreSO[] orePools;
    public int[] OreList;
    [SerializeField] private InventoryUI inventoryUI;

    // 로그 출력용 디버그 변수
    public int[] minedThisWave;
    public int[] totalMinedSession;
    public int[] depositedThisWave;
    public int[] totalDepositedSession;

    private void Start()
    {
        OreList = new int[orePools.Length];
        minedThisWave = new int[orePools.Length];
        totalMinedSession = new int[orePools.Length];
        depositedThisWave = new int[orePools.Length];
        totalDepositedSession = new int[orePools.Length];

        for (int i = 0; i < orePools.Length; i++)
        {
            OreList[i] = 0;
            minedThisWave[i] = 0;
            totalMinedSession[i] = 0;
            depositedThisWave[i] = 0;
            totalDepositedSession[i] = 0;
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
        depositedThisWave[(int)oreType] += amount;
        totalDepositedSession[(int)oreType] += amount;
        inventoryUI.UpdateOreUI(oreType, OreList[(int)oreType]);
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

    // == [Log] 로그 출력용 추가사항 ==
    // 현재 웨이브 통계 초기화
    public void ResetWaveStats()
    {
        for (int i = 0; i < orePools.Length; i++)
        {
            minedThisWave[i] = 0;
            depositedThisWave[i] = 0;
        }
    }
    
    // 채광량 증가 메서드
    public void IncrementMinedAmount(OreType oreType, int amount)
    {
        minedThisWave[(int)oreType] += amount;
        totalMinedSession[(int)oreType] += amount;
    }

    // 웨이브 종료 시 자원 통계 문자열 반환
    public string GetWaveResourceStats(int waveNumber)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < orePools.Length; i++)
        {
            OreType oreType = (OreType)i;
            sb.Append($"{oreType.ToString()} / ");
            sb.Append($"{totalMinedSession[i]} / ");
            sb.Append($"{minedThisWave[i]} / ");
            sb.Append($"{totalDepositedSession[i]} / ");
            sb.Append($"{depositedThisWave[i]}");
            
            /*
            sb.Append($"total_mined_session: {totalMinedSession[i]} / ");
            sb.Append($"mined_this_wave: {minedThisWave[i]} / ");
            sb.Append($"total_deposited_session: {totalDepositedSession[i]} / ");
            sb.Append($"deposited_this_wave: {depositedThisWave[i]}");
            */
            if (i < orePools.Length - 1)
            {
                sb.Append(" | "); // 각 광물 타입별 구분을 위한 구분자
            }
        }
        return sb.ToString();
    }
}
