using UnityEngine;
[CreateAssetMenu(fileName = "SpaceShipMoveOrePerSlowSO", menuName = "ScriptableObjects/Forge/SpaceShip/Move/SpaceShipMoveOrePerSlowSO", order = 1)]
public class SpaceShipMoveOrePerSlowSO : BaseForgeSO
{
    public int allowedOreCount;
    
    protected override ForgeId GetForgeId() => ForgeId.SpaceShipMoveOrePerSlow;
    
    public override void Apply()
    {
        if (Managers.Instance?.spaceshipMotor == null) return;
        Managers.Instance.spaceshipMotor.AddAllowedOreCount(allowedOreCount);
    }
}
