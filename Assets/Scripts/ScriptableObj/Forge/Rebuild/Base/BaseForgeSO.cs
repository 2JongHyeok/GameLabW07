using Unity.VisualScripting;
using UnityEngine;
public enum ForgeId
{
    None,
    //Planet 
    PlanetCoreMaxHp,
    PlanetHpRegenAmount,
    PlanetShieldMaxHp,
    PlanetShieldRegenSpeed,
    // Attacks MainCannon
    MainCannonUpgrade,
    MainCannonBulletNumber,
    // Attacks GuidedMissile
    GuidedMissileUnlock,
    GuidedMissileAtkSpeed,
    // SpaceShip Mining
    SpaceShipMiningUpgrade,
    SpaceShipMiningRadius,
    // SpaceShip Move
    SpaceShipMoveMaxSpeed,
    SpaceShipMoveOrePerSlow,

    //Planet2 (새로 추가)
    Planet2CoreMaxHp,
    Planet2HpRegenAmount,
    Planet2ShieldMaxHp,
    Planet2ShieldRegenSpeed
}
public abstract class BaseForgeSO : ScriptableObject
{
    [Header("식별 정보 (자동 설정)")]
    public ForgeId forgeId;
    
    [Header("기본 정보")]
    public string upgradeName;
    public string upgradeDescription;
    
    [Range(1, 4)]
    public int depth = 1;

    // 고정 4종 광석 비용
    public int coalCost = 0;
    public int ironCost = 0;
    public int goldCost = 0;
    public int diamondCost = 0;
    
    [Header("선행/후행 서브브랜치")]
    public SubBranchSO[] postSubBranches;

    // 자식 클래스가 구현해야 하는 메서드: forgeId 반환
    protected abstract ForgeId GetForgeId();

    // 핵심: SO가 효과를 적용하는 훅(최초 해금 1회/레벨 적용 공용)
    public abstract void Apply();
    
    // Unity 에디터에서 값이 변경되거나 스크립트가 로드될 때 자동 호출
    protected virtual void OnValidate()
    {
        forgeId = GetForgeId();
    }
    
    // 비용 확인용 헬퍼 메서드
    public int GetCost(OreType oreType)
    {
        return oreType switch
        {
            OreType.Coal => coalCost,
            OreType.Iron => ironCost,
            OreType.Gold => goldCost,
            OreType.Diamond => diamondCost,
            _ => 0
        };
    }
}
