using UnityEngine;
using System.Collections;

public class AutoTurretLaserAttack : IAttackStrategy
{
    // ===== 주입 가능한 파라미터 =====
    private readonly float baseDamage;
    private readonly float interval;       // 쿨타임
    private readonly float duration;       // 빔 유지 시간
    private readonly float maxDistance;    // 레이저 사거리
    private readonly float turnRateDegPerSec;
    private readonly float aimOffsetDeg;   // 스프라이트 전방 보정(위=+Y면 -90)

    private Coroutine attackCoroutine;

    // 빔 시각화용
    private LineRenderer cachedLR;
    private Material lineMaterial; // 선택: 외부 전달 안 하면 기본 머티리얼 생성

    public AutoTurretLaserAttack(
        float baseDamage = 100f,
        float interval = 8.0f,
        float duration = 0.2f,
        float maxDistance = 30f,
        float turnRateDegPerSec = 720f,
        float aimOffsetDeg = -90f,
        Material lineMaterial = null
    )
    {
        this.baseDamage = baseDamage;
        this.interval = interval;
        this.duration = duration;
        this.maxDistance = maxDistance;
        this.turnRateDegPerSec = turnRateDegPerSec;
        this.aimOffsetDeg = aimOffsetDeg;
        this.lineMaterial = lineMaterial;
    }

    public void StartAttack(MonoBehaviour host, Transform turretTransform, string _)
    {
        if (attackCoroutine != null) return;
        var turret = host.GetComponent<AutoTurret>();
        attackCoroutine = host.StartCoroutine(AttackRoutine(host, turretTransform, turret));
    }

    public void StopAttack(MonoBehaviour host)
    {
        if (attackCoroutine == null) return;
        host.StopCoroutine(attackCoroutine);
        attackCoroutine = null;

        // 잔여 라인 정리
        if (cachedLR != null) cachedLR.enabled = false;
    }

    public void Attack(Transform turretTransform, Transform targetEnemy)
    {
        if (!turretTransform || !targetEnemy) return;

        // 1) 2D 회전(부드럽게는 루틴에서 처리 중)
        LookAt2DInstant(turretTransform, targetEnemy, aimOffsetDeg);

        // 2) 히트스캔(Physics2D.Raycast)
        Vector2 origin = turretTransform.position;
        Vector2 dir = (targetEnemy.position - turretTransform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxDistance, ~0); // 레이어는 상황에 맞게 마스크 지정
        Vector3 endPoint = (Vector3)origin + (Vector3)dir * maxDistance;

        if (hit.collider != null)
        {
            endPoint = hit.point;

            // 데미지 적용(게임 구조에 맞게 바꿔도 됨)
            // 예: IDamageable, EnemyHealth 등
            //hit.collider.GetComponent<EnemyScript>()?.TakeDamage(baseDamage);
        }

        // 3) 시각효과(라인표시) 잠깐 켜기
        turretTransform.gameObject.GetComponent<MonoBehaviour>().StartCoroutine(ShowBeam(turretTransform, origin, endPoint));
    }

    private IEnumerator AttackRoutine(MonoBehaviour host, Transform turretTransform, AutoTurret turret)
    {
        var wait = new WaitForSeconds(interval);
        EnsureLineRenderer(turretTransform);

        while (true)
        {
            if (!turretTransform || !turret) yield break;

            var target = turret.GetCurrentTarget();
            if (target != null)
            {
                // 부드러운 조준
                SmoothLookAt2D(turretTransform, target, turnRateDegPerSec, aimOffsetDeg);

                // 발사
                Attack(turretTransform, target);
            }

            yield return wait;
        }
    }

    // ===== 시각효과 =====
    private void EnsureLineRenderer(Transform owner)
    {
        if (cachedLR != null) return;

        cachedLR = owner.GetComponent<LineRenderer>();
        if (cachedLR == null) cachedLR = owner.gameObject.AddComponent<LineRenderer>();

        cachedLR.positionCount = 2;
        cachedLR.enabled = false;
        cachedLR.widthMultiplier = 0.08f;

        if (lineMaterial == null)
        {
            // 기본 머티리얼
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        cachedLR.material = lineMaterial;
        cachedLR.sortingOrder = 1000; // UI 위로 보이게 하고 싶으면 조정
    }

    private IEnumerator ShowBeam(Transform owner, Vector3 start, Vector3 end)
    {
        if (cachedLR == null) EnsureLineRenderer(owner);

        cachedLR.SetPosition(0, start);
        cachedLR.SetPosition(1, end);
        cachedLR.enabled = true;

        yield return new WaitForSeconds(duration);

        if (cachedLR != null) cachedLR.enabled = false;
    }

    // ===== 2D 회전 유틸 =====
    private static void SmoothLookAt2D(Transform t, Transform target, float turnRateDegPerSec, float offsetDeg)
    {
        Vector2 dir = (target.position - t.position);
        float desired = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + offsetDeg;
        float current = t.eulerAngles.z;
        float next = Mathf.MoveTowardsAngle(current, desired, turnRateDegPerSec * Time.deltaTime);
        t.rotation = Quaternion.Euler(0f, 0f, next);
    }

    private static void LookAt2DInstant(Transform t, Transform target, float offsetDeg)
    {
        Vector2 dir = (target.position - t.position);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + offsetDeg;
        t.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
