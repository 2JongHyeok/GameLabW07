using UnityEngine;
using System.Collections.Generic;

public class AutoTurret : MonoBehaviour
{
    [Header("타겟 설정 (2D)")]
    public string targetTag = "Enemy";
    public float scanRange = 10f;
    public LayerMask targetLayers;

    private IAttackStrategy attackStrategy;
    private Transform currentTarget;
    private bool isAttacking;

    private static readonly List<Collider2D> overlapResults = new List<Collider2D>(64);
    private ContactFilter2D contactFilter;

    private void Awake()
    {
        // ContactFilter2D 셋업 (레이어 마스크/트리거 포함 여부 등)
        contactFilter = new ContactFilter2D();
        contactFilter.useLayerMask = true;
        contactFilter.SetLayerMask(targetLayers);
        contactFilter.useTriggers = true; // 트리거도 탐지하려면 true (상황에 맞게)
    }

    public void ActivateTurret(IAttackStrategy strategy)
    {
        if (isAttacking) return;
        attackStrategy = strategy;
        isAttacking = true;
        attackStrategy.StartAttack(this, transform, targetTag);
    }

    public void DeactivateTurret()
    {
        if (!isAttacking) return;
        attackStrategy?.StopAttack(this);
        isAttacking = false;
        currentTarget = null;
    }

    private void OnDisable()
    {
        if (isAttacking)
        {
            attackStrategy?.StopAttack(this);
            isAttacking = false;
        }
        currentTarget = null;
    }

    private void Update()
    {
        if (!isAttacking) return;
        FindNearestEnemy2D_Modern();
    }

    private void FindNearestEnemy2D_Modern()
    {
        // 현재 타겟 유효성 확인
        if (currentTarget != null)
        {
            if (!currentTarget.gameObject.activeInHierarchy ||
                ((Vector2)currentTarget.position - (Vector2)transform.position).sqrMagnitude > scanRange * scanRange)
            {
                currentTarget = null;
            }
        }
        if (currentTarget != null) return;
        overlapResults.Clear();
        int count = Physics2D.OverlapCircle((Vector2)transform.position, scanRange, contactFilter, overlapResults);

        float bestSqr = float.PositiveInfinity;
        Transform best = null;

        for (int i = 0; i < count; i++)
        {

            var col = overlapResults[i];
            if (col == null) continue;

            // 태그 필터(비워두면 무시)
            if (!string.IsNullOrEmpty(targetTag) && !col.CompareTag(targetTag))
                continue;

            float sqr = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {

                bestSqr = sqr;
                best = col.transform;
            }
        }

        currentTarget = best;
    }

    public Transform GetCurrentTarget() => currentTarget;

    public void ForceTargetUpdate()
    {
        currentTarget = null;
        FindNearestEnemy2D_Modern();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, scanRange);
    }
}
