using UnityEngine;
[CreateAssetMenu(fileName = "SpaceShipMoveMaxSpeedSO", menuName = "ScriptableObjects/Forge/SpaceShip/Move/SpaceShipMoveMaxSpeedSO", order = 1)]
public class SpaceShipMoveMaxSpeedSO : BaseForgeSO
{
    public float MaxSpeed;
    
    protected override ForgeId GetForgeId() => ForgeId.SpaceShipMoveMaxSpeed;
    
    public override void Apply()
    {
        if (Managers.Instance?.spaceshipMotor == null) return;
        Managers.Instance.spaceshipMotor.AddThrustPower(MaxSpeed);
    }
}
