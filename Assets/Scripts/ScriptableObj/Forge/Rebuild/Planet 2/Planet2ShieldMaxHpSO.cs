using UnityEngine;
[CreateAssetMenu(fileName = "Planet2ShieldMaxHpSO", menuName = "ScriptableObjects/Forge/Planet2/Planet2ShieldMaxHpSO", order = 1)]
public class Planet2ShieldMaxHpSO : BaseForgeSO
{
    public float ShieldMaxHp;
    
    protected override ForgeId GetForgeId() => ForgeId.Planet2ShieldMaxHp;
    
    public override void Apply()
    {
        if (Managers.Instance?.planet != null)
        {
            Managers.Instance.AddTileMaxHP((int)ShieldMaxHp);
        }
    }
}
