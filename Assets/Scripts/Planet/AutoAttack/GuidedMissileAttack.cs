using System.Collections;
using UnityEngine;

public class GuidedMissileAttack : IAttackStrategy
{
    public   float baseDamage;
    public float interval;
    private  GameObject missilePrefab;
    private Coroutine attackCoroutine;
    private WaitForSeconds cachedWait;
    private bool waitDirty = true; // interval이 바뀌면 true
    public GuidedMissileAttack(GameObject missilePrefab, float baseDamage = 20f, float interval = 3f)
    {
        this.missilePrefab = missilePrefab;
        this.baseDamage = baseDamage;
        this.interval = interval;
        waitDirty = true;
    }
    public float Damage
    {
        get => baseDamage;
        set => baseDamage = Mathf.Max(0f, value);
    }

    public float Interval
    {
        get => interval;
        set
        {
            interval = Mathf.Max(0.01f, value);
            waitDirty = true; // 다음 루프부터 새로운 간격 반영
        }
    }
    public void StartAttack(MonoBehaviour host, Transform turretTransform, string _)
    {
        if (attackCoroutine != null) return;
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
        if (!targetEnemy || !missilePrefab) return;
        var go = GameObject.Instantiate(missilePrefab, turretTransform.position, turretTransform.rotation);
        var missile = go.GetComponent<GuidedMissile>();
        if (missile == null) return;

        missile.SetTarget(targetEnemy, turretTransform.GetComponent<AutoTurret>());
        missile.Init(baseDamage, turretTransform);
    }
    static void SmoothLookAt2D(Transform t, Transform target, float turnRateDegPerSec = 360f, float offsetDeg = -90f)
    {
        Vector2 dir = (target.position - t.position);
        float desired = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + offsetDeg;
        float current = t.eulerAngles.z;
        float next = Mathf.MoveTowardsAngle(current, desired, turnRateDegPerSec * Time.deltaTime);
        t.rotation = Quaternion.Euler(0f, 0f, next);
    }
    private IEnumerator AttackRoutine(Transform turretTransform, AutoTurret turret)
    {
        while (true)
        {
            if (!turretTransform || !turret) yield break;

            var target = turret.GetCurrentTarget();
            if (target != null)
            {
                // 2D라면 Atan2 버전 사용 권장
                SmoothLookAt2D(turretTransform, target, 360f, -90f);
                Attack(turretTransform, target);
            }
            
            // interval 변경 시 새로운 WaitForSeconds 생성
            if (waitDirty)
            {
                cachedWait = new WaitForSeconds(interval);
                waitDirty = false;
            }
            
            yield return cachedWait;
        }
    }

    #region Getter Setter
    public float GetMissileDamage() => Damage;
    public void SetMissileDamage(float val) => Damage = val;
    public void AddMissileDamage(float val) => Damage += val;
    public float GetMissileInterval() => Interval;
    public void SetMissileInterval(float val) => Interval = val;
    public void AddMissileInterval(float val) => Interval += val;
    #endregion
}
