using UnityEngine;
[CreateAssetMenu(fileName = "MainCannonUpgradeSO", menuName = "ScriptableObjects/Forge/Attacks/MainCannon/MainCannonUpgradeSO", order = 1)]
public class MainCannonUpgradeSO : BaseForgeSO
{
    public float AtkDamage;
    public float AtkSpeed;

    protected override ForgeId GetForgeId() => ForgeId.MainCannonUpgrade;

    public override void Apply()
    {
        Managers.Instance.AddWeaponDamage((int)AtkDamage);
        Managers.Instance.AddWeaponAttackSpeed(AtkSpeed);
    }
}
