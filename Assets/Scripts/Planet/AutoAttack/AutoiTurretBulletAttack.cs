using UnityEngine;
using System.Collections;

public class AutoTurretBulletAttack : IAttackStrategy
{
    // ===== 구성 주입(인스펙터/팩토리/Addressables 등으로 전달) =====
    private readonly float baseDamage;     // 기본 데미지
    private readonly float interval;       // 발사 간격(초)
    private readonly GameObject bulletPrefab;

    private Coroutine attackCoroutine;

    /// <summary>
    /// bulletPrefab: 총알 프리팹(외부 주입)
    /// baseDamage  : 기본 데미지(기본 5)
    /// interval    : 발사 간격(기본 0.5초)
    /// </summary>
    public AutoTurretBulletAttack(GameObject bulletPrefab, float baseDamage = 5f, float interval = 0.5f)
    {
        this.bulletPrefab = bulletPrefab;
        this.baseDamage = baseDamage;
        this.interval = interval;
    }

    public void StartAttack(MonoBehaviour host, Transform turretTransform, string _)
    {
        if (attackCoroutine != null) return; // 중복 시작 방지
        var turret = host.GetComponent<AutoTurret>();
        attackCoroutine = host.StartCoroutine(AttackRoutine(turretTransform, turret));
    }

    public void StopAttack(MonoBehaviour host)
    {
        if (attackCoroutine == null) return;
        host.StopCoroutine(attackCoroutine);
        attackCoroutine = null;
    }

    public void Attack(Transform turretTransform, Transform targetEnemy)
    {
        if (!targetEnemy || !bulletPrefab) return;

        // 발사 시점의 방향만 고정되는 비유도 탄
        var go = GameObject.Instantiate(bulletPrefab, turretTransform.position, turretTransform.rotation);

        // Projectile이 초기 속도/수명/관통 등을 내부에서 처리한다고 가정
        go.GetComponent<Projectile>()?.Init(baseDamage, turretTransform);

        // 만약 Projectile이 초기 속도를 안 주는 구조라면:
        // var rb2D = go.GetComponent<Rigidbody2D>();
        // if (rb2D) rb2D.velocity = go.transform.up * initialSpeed; // 스프라이트 '위(+Y)'가 총구인 경우
        // (오른쪽(+X)이 총구면 transform.right 사용)
    }

    private IEnumerator AttackRoutine(Transform turretTransform, AutoTurret turret)
    {
        var wait = new WaitForSeconds(interval); // GC 최소화
        while (true)
        {
            if (!turretTransform || !turret) yield break;

            var target = turret.GetCurrentTarget();
            if (target != null)
            {
                // 2D 전용 회전(부드럽게)
                SmoothLookAt2D(turretTransform, target, 720f, -90f);
                Attack(turretTransform, target);
            }

            yield return wait;
            // 일시정지 중에도 쏘고 싶다면 WaitForSecondsRealtime(interval)로 교체
        }
    }

    /// <summary>
    /// 2D용 부드러운 회전. 스프라이트의 기본 전방이 '위(+Y)'라면 offsetDeg = -90 권장.
    /// 기본 전방이 '오른쪽(+X)'이면 offsetDeg = 0.
    /// </summary>
    private static void SmoothLookAt2D(Transform t, Transform target, float turnRateDegPerSec, float offsetDeg)
    {
        Vector2 dir = (target.position - t.position);
        float desired = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + offsetDeg;
        float current = t.eulerAngles.z;
        float next = Mathf.MoveTowardsAngle(current, desired, turnRateDegPerSec * Time.deltaTime);
        t.rotation = Quaternion.Euler(0f, 0f, next);
    }
}
