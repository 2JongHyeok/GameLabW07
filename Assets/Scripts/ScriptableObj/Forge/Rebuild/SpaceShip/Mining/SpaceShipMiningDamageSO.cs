using UnityEngine;
[CreateAssetMenu(fileName = "SpaceShipMiningUpgradeSO", menuName = "ScriptableObjects/Forge/SpaceShip/Mining/SpaceShipMiningUpgradeSO", order = 1)]
public class SpaceShipMiningUpgradeSO : BaseForgeSO
{
    public float MiningDamage;
    public float MiningSpeed;
    
    protected override ForgeId GetForgeId() => ForgeId.SpaceShipMiningUpgrade;
    
    public override void Apply()
    {
            Managers.Instance.spaceshipWeapon.AddDamage((int)MiningDamage);
        Managers.Instance.spaceshipWeapon.AddAttackSpeed(MiningSpeed);
    }
}
