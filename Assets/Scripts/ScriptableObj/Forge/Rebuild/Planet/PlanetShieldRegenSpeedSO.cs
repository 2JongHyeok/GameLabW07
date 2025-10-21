using UnityEngine;
[CreateAssetMenu(fileName = "PlanetShieldRegenSpeedSO", menuName = "ScriptableObjects/Forge/Planet/PlanetShieldRegenSO", order = 1)]
public class PlanetShieldRegenSO : BaseForgeSO
{
    public float ShieldRegen;
    
    protected override ForgeId GetForgeId() => ForgeId.PlanetShieldRegenSpeed;
    
    public override void Apply()
    {
        if (Managers.Instance?.planet != null)
        {
            Managers.Instance.ReduceTileRespawnDelay(ShieldRegen);
        }
    }
}
