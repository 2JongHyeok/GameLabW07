using UnityEngine;
[CreateAssetMenu(fileName = "Planet2ShieldMaxHpSO", menuName = "ScriptableObjects/Forge/Planet2/Planet2ShieldMaxHpSO", order = 1)]
public class Planet2ShieldMaxHpSO : BaseForgeSO
{
    public float ShieldMaxHp;
    
    protected override ForgeId GetForgeId() => ForgeId.Planet2ShieldMaxHp;
    
    public override void Apply()
    {
        if (Managers.Instance?.planet2 != null)
        {
            Managers.Instance.Add2TileMaxHP((int)ShieldMaxHp);
        }
    }
}
