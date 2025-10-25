using UnityEngine;
[CreateAssetMenu(fileName = "Planet2CoreMaxHpSO", menuName = "ScriptableObjects/Forge/Planet2/Planet2CoreMaxHpSO", order = 1)]
public class Planet2CoreMaxHpSO : BaseForgeSO
{
    public float CoreMaxHp;
    
    protected override ForgeId GetForgeId() => ForgeId.Planet2CoreMaxHp;
    
    public override void Apply()
    {
        if (Managers.Instance != null)
        {
            // 여기 수정하면됨
            Managers.Instance.AddCoreMaxHP((int)CoreMaxHp);
        }
    }
}
