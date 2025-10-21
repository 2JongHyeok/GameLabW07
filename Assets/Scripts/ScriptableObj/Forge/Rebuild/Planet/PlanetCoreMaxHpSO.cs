using UnityEngine;
[CreateAssetMenu(fileName = "PlanetCoreMaxHpSO", menuName = "ScriptableObjects/Forge/Planet/PlanetCoreMaxHpSO", order = 1)]
public class PlanetCoreMaxHpSO : BaseForgeSO
{
    public float CoreMaxHp;
    
    protected override ForgeId GetForgeId() => ForgeId.PlanetCoreMaxHp;
    
    public override void Apply()
    {
        if (Managers.Instance != null)
        {
            Managers.Instance.AddCoreMaxHP((int)CoreMaxHp);
        }
    }
}
