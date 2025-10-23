using UnityEngine;
[CreateAssetMenu(fileName = "Planet2ShieldRegenSpeedSO", menuName = "ScriptableObjects/Forge/Planet2/Planet2ShieldRegenSO", order = 1)]
public class Planet2ShieldRegenSO : BaseForgeSO
{
    public float ShieldRegen;
    
    protected override ForgeId GetForgeId() => ForgeId.Planet2ShieldRegenSpeed;
    
    public override void Apply()
    {
        if (Managers.Instance?.planet != null)
        {
            Managers.Instance.ReduceTileRespawnDelay(ShieldRegen);
        }
    }
}
