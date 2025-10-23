using UnityEngine;

public enum SubBranchType
{
    None,
    //Planet 
    PlanetCoreMaxHp,
    PlanetHpRegenAmount,
    PlanetShieldMaxHp,
    PlanetShieldRegenSpeed,
    // Attacks MainCannon
    MainCannonUpgrade,
    MainCannonBulletNumber,
    // Attacks GuidedMissile
    GuidedMissile,
    // SpaceShip Mining
    SpaceShipMiningUpgrade,
    SpaceShipMiningRadius,
    // SpaceShip Move
    SpaceShipMoveMaxSpeed,
    SpaceShipMoveOrePerSlow,
    //Planet 
    Planet2CoreMaxHp,
    Planet2HpRegenAmount,
    Planet2ShieldMaxHp,
    Planet2ShieldRegenSpeed,
}
[CreateAssetMenu(fileName = "SubBranchSO", menuName = "ScriptableObjects/Forge/Branch/SubBranchSO", order = 1)]
public class SubBranchSO : BranchSO
{
    public SubBranchType subBranchType;
    public BaseForgeSO[] baseForgeSOs;
}
