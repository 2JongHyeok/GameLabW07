// 파일 이름: RammerEnemySO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "RammerEnemySO", menuName = "ScriptableObjects/Enemy/RammerEnemySO", order = 1)]
public class RammerEnemySO : EnemyBaseSO
{
    [Header("Rammer Stats")]
    
    [Tooltip("쉴드(타일)에 가하는 데미지")]
    public int shieldDamage = 100;

    [Tooltip("충돌 후 뒤로 밀려나는 거리")]
    public float knockbackDistance = 2f;
    
    [Tooltip("뒤로 밀려나는 데 걸리는 시간(초)")]
    public float knockbackDuration = 0.2f;

    // Enemy.cs의 ResetState()가 이 값을 읽어갑니다.
    [Tooltip("충돌 후, 후퇴했다가 다시 돌진하기까지 대기하는 시간(초)")]
    public float attackCooldown = 1.5f;
    
    /// <summary>
    /// EnemyBaseSO의 추상 메서드 구현
    /// Rammer는 Update()에서 멈추지 않고 HandleKamikazeCollision()에서
    /// 물리 충돌로 공격하므로, 이 함수는 사용되지 않습니다.
    /// </summary>
    public override void PerformAttack(Enemy enemy)
    {
        // 의도적으로 비워 둡니다.
    }
}