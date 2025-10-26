using UnityEngine;
[CreateAssetMenu(fileName = "LaserMissileAtkSpeedSO", menuName = "ScriptableObjects/Forge/Attacks/LaserMissile/LaserMissileAtkSpeedSO", order = 1)]
public class LaserMissileAtkSpeedSO : BaseForgeSO
{
    public float LaserAtkInterval = 0.8f;
    public float LaserAtkDamage = 3f;
    
    protected override ForgeId GetForgeId() => ForgeId.LaserMissileAtkSpeed;
    
    public override void Apply()
    {
        if (Managers.Instance?.turretActivationManager == null) return;
        LaserAtkInterval += Managers.Instance.turretActivationManager.GetLaserInterval();
        LaserAtkDamage += Managers.Instance.turretActivationManager.GetLaserDamage();
        
        Managers.Instance.turretActivationManager.SetLaserInterval(LaserAtkInterval);
        Managers.Instance.turretActivationManager.SetLaserDamage(LaserAtkDamage);
    }
}
