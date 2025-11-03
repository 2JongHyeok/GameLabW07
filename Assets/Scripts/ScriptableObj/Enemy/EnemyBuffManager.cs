// 파일 이름: EnemyBuffManager.cs
using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-20)]
public class EnemyBuffManager : MonoBehaviour
{
    public static EnemyBuffManager Instance;

    [Header("Commander Buff Stats")] [Tooltip("적에게 부여할 쉴드(보호막) 체력")]
    public int commanderShieldHP = 10;

    [Tooltip("공격 속도 증가 배율 (예: 1.5 = 50% 빨라짐)")]
    public float attackSpeedMultiplier = 1.5f; // 이 값은 1보다 커야 함

    [Tooltip("Rammer 돌진 빈도 증가 배율 (예: 1.5 = 50% 빨라짐)")]
    public float rammerFrequencyMultiplier = 1.5f; // 이 값은 1보다 커야 함

    [Tooltip("RailGun 차징 시간 감소 배율 (예: 0.7 = 30% 빨라짐)")]
    public float railgunChargeTimeMultiplier = 0.7f; // 이 값은 1보다 작아야 함

    // 현재 Commander가 활성화되었는지 여부
    public bool IsCommanderActive { get; private set; } = false;

    // 현재 맵에 있는 모든 적 리스트 (새로 스폰되거나 죽을 때마다 갱신)
    private HashSet<Enemy> activeEnemies = new HashSet<Enemy>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Commander가 맵에 등장하여 버프를 활성화할 때 호출됩니다.
    /// </summary>
    public void ActivateCommanderBuffs()
    {
        if (IsCommanderActive) return; // 이미 다른 커맨더가 활성화됨

        IsCommanderActive = true;
        Debug.Log("Commander 버프 활성화!");

        // 맵에 이미 존재하는 모든 적에게 버프 적용
        foreach (Enemy enemy in activeEnemies)
        {
            ApplyBuffs(enemy);
        }
    }

    /// <summary>
    /// Commander가 죽어서 버프를 비활성화할 때 호출됩니다.
    /// </summary>
    public void DeactivateCommanderBuffs()
    {
        if (!IsCommanderActive) return;

        IsCommanderActive = false;
        Debug.Log("Commander 버프 비활성화!");

        // 맵에 있는 모든 적의 버프와 쉴드를 즉시 제거
        foreach (Enemy enemy in activeEnemies)
        {
            RemoveBuffs(enemy);
        }
    }

    /// <summary>
    /// 적이 스폰될 때(ResetState) 호출되어 리스트에 등록합니다.
    /// </summary>
    public void RegisterEnemy(Enemy enemy)
    {
        if (activeEnemies.Add(enemy))
        {
            // 적이 등록될 때, Commander가 이미 활성화 상태라면 즉시 버프 적용
            if (IsCommanderActive)
            {
                ApplyBuffs(enemy);
            }
        }
    }

    /// <summary>
    /// 적이 죽을 때(TakeDamage) 호출되어 리스트에서 제거합니다.
    /// </summary>
    public void UnregisterEnemy(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
    }

    // --- 개별 적에게 버프를 적용하는 헬퍼 함수 ---
    private void ApplyBuffs(Enemy enemy)
    {
        if (enemy.enemyData.enemyType == EnemyType.Parasite || 
            enemy.enemyData.enemyType == EnemyType.Boss || 
            enemy.enemyData.enemyType == EnemyType.MainBoss ||
            enemy.enemyData.enemyType == EnemyType.Commander)
        {
            // Parasite, Boss, MainBoss, Commander는 버프 대상에서 제외
            return;
        }

        // 1. 쉴드 부여
        enemy.shieldHP = commanderShieldHP; 
        enemy.GetComponent<SpriteRenderer>().color = Color.cyan;
        
        // TODO: 쉴드 이펙트 켜는 로직 (enemy.shieldEffect.SetActive(true);)

        // 2. 스탯 버프 적용 (SO 원본이 아닌, Enemy 인스턴스의 값을 수정)
        // (Enemy.cs의 ResetState에서 이미 기본값을 로드했음)
        switch (enemy.enemyData.enemyType)
        {
            case EnemyType.Ranger:
            case EnemyType.RangerTank:
            case EnemyType.Parasite:
                // 공격 속도: 쿨다운을 줄임
                enemy.attackCooldown /= attackSpeedMultiplier;
                break;
            
            case EnemyType.RailGun:
                // 차징 시간 단축 (RailGunSO가 직접 이 값을 참조하도록 수정 필요 - Step 4)
                // (공격 속도를 위해 쿨다운도 줄여줌)
                enemy.attackCooldown /= attackSpeedMultiplier;
                break;
                
            case EnemyType.Rammer:
                // 돌진 빈도: 쿨다운을 줄임
                enemy.attackCooldown /= rammerFrequencyMultiplier;
                break;
        }
    }

    // --- 개별 적에게서 버프를 제거하는 헬퍼 함수 ---
    private void RemoveBuffs(Enemy enemy)
    {
        // 1. 쉴드 즉시 제거
        enemy.shieldHP = 0;
        enemy.ResetSpriteColor(); // 적의 색상을 원래대로 복원
        // TODO: 쉴드 이펙트 끄는 로직 (enemy.shieldEffect.SetActive(false);)

        // 2. 스탯 버프 되돌리기 (SO의 원본 값으로 덮어쓰기)
        // (Enemy.cs의 ResetState에 있는 로직과 동일)
        enemy.ResetAttackCooldownFromSO(); 
    }
}