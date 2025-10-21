using UnityEngine;
[CreateAssetMenu(fileName = "SpaceShipMiningRadiusSO", menuName = "ScriptableObjects/Forge/SpaceShip/Mining/SpaceShipMiningRadiusSO", order = 1)]
public class SpaceShipMiningRadiusSO : BaseForgeSO
{
    public float MiningRadius;
    
    protected override ForgeId GetForgeId() => ForgeId.SpaceShipMiningRadius;
    
    public override void Apply()
    {
        if (Managers.Instance?.spaceshipWeapon != null)
            Managers.Instance.spaceshipWeapon.AddMiningRadius(MiningRadius);
    }
}
