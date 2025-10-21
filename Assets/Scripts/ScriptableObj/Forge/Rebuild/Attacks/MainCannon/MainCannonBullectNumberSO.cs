using UnityEngine;
[CreateAssetMenu(fileName = "MainCannonBulletNumberSO", menuName = "ScriptableObjects/Forge/Attacks/MainCannon/MainCannonBulletNumberSO", order = 1)]
public class MainCannonBulletNumberSO : BaseForgeSO
{
    protected override ForgeId GetForgeId() => ForgeId.MainCannonBulletNumber;
    
    public override void Apply()
    {

        Managers.Instance.subWeaponManager.LevelUp();
    }
}
