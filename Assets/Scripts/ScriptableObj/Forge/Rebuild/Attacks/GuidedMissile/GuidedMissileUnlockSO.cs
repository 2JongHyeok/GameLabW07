using UnityEngine;
[CreateAssetMenu(fileName = "GuidedMissileUnlockSO", menuName = "ScriptableObjects/Forge/Attacks/GuidedMissile/GuidedMissileUnlockSO", order = 1)]
public class GuidedMissileUnlockSO : BaseForgeSO
{
    protected override ForgeId GetForgeId() => ForgeId.GuidedMissileAtkSpeed;

    public override void Apply()
    {
        // 레벨 상승 시 추가 효과가 있으면 여기에
        Managers.Instance?.turretActivationManager?.AddMissileTurret();
    }
}
