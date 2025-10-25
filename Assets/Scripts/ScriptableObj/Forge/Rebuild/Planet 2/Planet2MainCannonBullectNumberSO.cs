using UnityEngine;
[CreateAssetMenu(fileName = "Planet2MainCannonBulletNumberSO", menuName = "ScriptableObjects/Forge/Attacks/MainCannon/Planet2MainCannonBulletNumberSO", order = 1)]
public class Planet2MainCannonBulletNumberSO : BaseForgeSO
{
    protected override ForgeId GetForgeId() => ForgeId.Planet2MainCannonBulletNumber;
    
    public override void Apply()
    {
        Managers.Instance.weapon2.AddSatellite();
    }
}
