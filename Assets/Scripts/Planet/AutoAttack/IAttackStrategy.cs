using UnityEngine;

// 모든 공격 전략이 구현해야 할 인터페이스
public interface IAttackStrategy
{
    // 공격 로직을 실행하는 메서드
    void Attack(Transform turretTransform, Transform targetEnemy);

    // 공격을 시작/활성화하는 메서드 (코루틴 시작 등에 사용)
    void StartAttack(MonoBehaviour host, Transform turretTransform, string targetTag);

    // 공격을 중지/비활성화하는 메서드
    void StopAttack(MonoBehaviour host);
}
