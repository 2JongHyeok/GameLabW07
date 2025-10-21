using UnityEngine;
[CreateAssetMenu(fileName = "PlanetHpRegenAmountSO", menuName = "ScriptableObjects/Forge/Planet/PlanetHpRegenAmountSO", order = 1)]
public class PlanetHpRegenAmountSO : BaseForgeSO, IReuse
{
    public float HpRegenAmount;
    
    // IReuse 인터페이스 구현 - 재사용 가능 (체력 회복을 무한으로 구매 가능)
    public bool IsReusable => true;
    
    protected override ForgeId GetForgeId() => ForgeId.PlanetHpRegenAmount;
    
    public override void Apply()
    {
        if (Managers.Instance != null)
        {
            Managers.Instance.HealCoreHP((int)HpRegenAmount);
        }
    }
}
