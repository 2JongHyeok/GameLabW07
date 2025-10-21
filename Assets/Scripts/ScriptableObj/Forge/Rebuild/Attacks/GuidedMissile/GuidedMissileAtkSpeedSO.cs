using UnityEngine;
[CreateAssetMenu(fileName = "GuidedMissileAtkSpeedSO", menuName = "ScriptableObjects/Forge/Attacks/GuidedMissile/GuidedMissileAtkSpeedSO", order = 1)]
public class GuidedMissileAtkSpeedSO : BaseForgeSO
{
    public float AtkSpeed;
    
    protected override ForgeId GetForgeId() => ForgeId.GuidedMissileAtkSpeed;
    
    public override void Apply()
    {
        if (Managers.Instance?.turretActivationManager == null) return;
        Managers.Instance.turretActivationManager.AddMissileInterval(AtkSpeed);
    }
}
