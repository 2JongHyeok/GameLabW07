using UnityEngine;

[CreateAssetMenu(fileName = "New Wave", menuName = "Wave System/Wave Data")]
public class WaveSO : ScriptableObject
{

    [Header("Enemy Composition")]
    [Space(10)]
    public int rangerCount = 0;
    public int rangerTankCount = 0;
    public int kamikazeCount = 0;
    public int kamikazeTankCount = 0;
    public int parasiteCount = 0;

    [Header("Spawn Timing")]
    [Tooltip("스폰 간격 (초)")]
    public float spawnInterval = 2f;

    [Tooltip("한 번에 스폰할 최소 적 수")]
    public int minSpawnPerInterval = 1;

    [Tooltip("한 번에 스폰할 최대 적 수")]
    public int maxSpawnPerInterval = 3;

    // 이 웨이브에서 스폰할 총 적의 수 계산
    public int GetTotalEnemyCount()
    {
        return rangerCount + rangerTankCount + kamikazeCount + kamikazeTankCount + parasiteCount;
    }

    // 특정 적 타입의 스폰 수 가져오기
    public int GetEnemyCount(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Ranger:
                return rangerCount;
            case EnemyType.RangerTank:
                return rangerTankCount;
            case EnemyType.Kamikaze:
                return kamikazeCount;
            case EnemyType.KamikazeTank:
                return kamikazeTankCount;
            case EnemyType.Parasite:
                return parasiteCount;
            default:
                return 0;
        }
    }

    // EnemySpawnInfo 배열로 변환 (WaveManager와의 호환성 유지)
    public EnemySpawnInfo[] GetEnemySpawnInfos()
    {
        System.Collections.Generic.List<EnemySpawnInfo> infos = new System.Collections.Generic.List<EnemySpawnInfo>();

        if (rangerCount > 0)
            infos.Add(new EnemySpawnInfo { enemyType = EnemyType.Ranger, count = rangerCount });
        
        if (rangerTankCount > 0)
            infos.Add(new EnemySpawnInfo { enemyType = EnemyType.RangerTank, count = rangerTankCount });
        
        if (kamikazeCount > 0)
            infos.Add(new EnemySpawnInfo { enemyType = EnemyType.Kamikaze, count = kamikazeCount });
        
        if (kamikazeTankCount > 0)
            infos.Add(new EnemySpawnInfo { enemyType = EnemyType.KamikazeTank, count = kamikazeTankCount });
        
        if (parasiteCount > 0)
            infos.Add(new EnemySpawnInfo { enemyType = EnemyType.Parasite, count = parasiteCount });

        return infos.ToArray();
    }
}

[System.Serializable]
public class EnemySpawnInfo
{
    [Tooltip("스폰할 적의 종류")]
    public EnemyType enemyType;

    [Tooltip("이 타입의 적을 총 몇 마리 스폰할지")]
    public int count;
}
