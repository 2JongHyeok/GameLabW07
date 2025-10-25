using UnityEngine;
[CreateAssetMenu(fileName = "Planet2MainCannonUpgradeSO", menuName = "ScriptableObjects/Forge/Attacks/MainCannon/Planet2MainCannonUpgradeSO", order = 1)]
public class Planet2MainCannonUpgradeSO : BaseForgeSO
{
    public float AtkDamage;
    public float AtkSpeed;

    protected override ForgeId GetForgeId() => ForgeId.Planet2MainCannonUpgrade;

    public override void Apply()
    {
        Managers.Instance.AddWeaponDamage((int)AtkDamage);
        Managers.Instance.AddWeaponAttackSpeed(AtkSpeed);
    }
}
