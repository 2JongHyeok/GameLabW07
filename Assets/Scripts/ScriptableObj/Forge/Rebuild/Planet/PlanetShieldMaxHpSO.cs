using UnityEngine;
[CreateAssetMenu(fileName = "PlanetShieldMaxHpSO", menuName = "ScriptableObjects/Forge/Planet/PlanetShieldMaxHpSO", order = 1)]
public class PlanetShieldMaxHpSO : BaseForgeSO
{
    public float ShieldMaxHp;
    
    protected override ForgeId GetForgeId() => ForgeId.PlanetShieldMaxHp;
    
    public override void Apply()
    {
        if (Managers.Instance?.planet != null)
        {
            Managers.Instance.AddTileMaxHP((int)ShieldMaxHp);
        }
    }
}
