// 파일 이름: CommanderSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "CommanderSO", menuName = "ScriptableObjects/Enemy/CommanderSO", order = 1)]
public class CommanderSO : EnemyBaseSO
{
    [Header("Commander Stats")]
    [Tooltip("Ranger처럼 멈추기 위해 필요한 가짜 쿨다운 값. 0보다 커야 함.")]
    public float attackCooldown = 999f; // 멈춘 후 공격은 안 하므로 아주 긴 값

    // Commander는 공격하지 않으므로 비워둡니다.
    public override void PerformAttack(Enemy enemy)
    {
        // 아무것도 하지 않음
    }
}