using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// AutoTurretLaserAttack: IAttackStrategy 명시적 구현 포함 (CS0535 방지)
public class AutoTurretLaserAttack : IAttackStrategy
{
    // ------- 주입 파라미터 -------
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private float damage = 10f;     // 1회 피해
    [SerializeField] private float interval = 5f;    // 발사 주기
    [SerializeField] private float duration = 0.5f;  // 빔 유지 시간
    [SerializeField] private float length = 5f;      // 빔 길이(+Y 기준)
    [SerializeField] private float maxWidth = 0.6f;  // 빔 최대 폭(시각/판정 동기)
    [SerializeField] private string targetTag = "Enemy";
    [SerializeField] private LayerMask hitMask;      // 비워두면 전체
    [SerializeField] private float searchRadius = 0f; // 0이면 length 사용

    // ------- 런타임 -------
    private MonoBehaviour host;
    private Coroutine attackCo;
    private WaitForSeconds cachedWait;
    private bool waitDirty = true;

    // GC 최소화를 위한 버퍼
    private static readonly Collider2D[] overlapBuf = new Collider2D[128];


    public AutoTurretLaserAttack(
    GameObject laserPrefab,
    float damage,
    float interval,
    float duration,
    float length,
    float maxWidth,
    string targetTag)
    {
        this.laserPrefab = laserPrefab;
        this.damage = Mathf.Max(0f, damage);
        this.interval = Mathf.Max(0.01f, interval);
        this.duration = Mathf.Max(0.01f, duration);
        this.length = Mathf.Max(0.01f, length);
        this.maxWidth = Mathf.Max(0.01f, maxWidth);
        this.targetTag = targetTag;
        waitDirty = true; // interval 캐시 갱신 플래그
    }

    // ---------------- API ----------------
    public float Damage { get => damage; set => damage = Mathf.Max(0f, value); }
    public float IntervalSec
    {
        get => interval;
        set 
        { 
            float previousValue = interval;
            interval += value; 
            if (interval != previousValue) // 값이 실제로 변경되었을 때만 로그 출력
            {
                Debug.LogWarning($"Laser Interval changed from {previousValue} to {interval} (requested: {value})", host?.gameObject); 
            }
            waitDirty = true; 
        }
    }
    

    // ===== IAttackStrategy: public 구현 =====
    public void StartAttack(MonoBehaviour host, Transform turretTransform, string targetTag)
    {
        if (attackCo != null) return;
        this.host = host;
        if (!string.IsNullOrEmpty(targetTag)) this.targetTag = targetTag;
        attackCo = host.StartCoroutine(AttackRoutine(turretTransform));
    }

    public void StopAttack(MonoBehaviour hostRef)
    {
        if (attackCo == null) return;
        hostRef.StopCoroutine(attackCo);
        attackCo = null;
    }

    public void Attack(Transform turretTransform, Transform targetEnemy)
    {
        if (host == null) return;
        host.StartCoroutine(FireOnce(turretTransform, targetEnemy));
    }

    // ===== IAttackStrategy: 명시적 구현(컴파일러 확실히 인식) =====
    void IAttackStrategy.StartAttack(MonoBehaviour host, Transform turretTransform, string targetTag)
        => StartAttack(host, turretTransform, targetTag);

    void IAttackStrategy.StopAttack(MonoBehaviour host)
        => StopAttack(host);

    void IAttackStrategy.Attack(Transform turretTransform, Transform targetEnemy)
        => Attack(turretTransform, targetEnemy);

    // ---------------- 내부 로직 ----------------
    private IEnumerator AttackRoutine(Transform turretTransform)
    {
        while (true)
        {
            if (!turretTransform) yield break;

            yield return FireOnce(turretTransform, null);

            if (waitDirty || cachedWait == null)
            {
                cachedWait = new WaitForSeconds(interval);
                waitDirty = false;
            }
            yield return cachedWait;
        }
    }

    // 1회 레이저
    private IEnumerator FireOnce(Transform turret, Transform explicitTarget)
    {
        if (!turret) yield break;

        // 1) 표적 선택: 명시 타겟 우선, 없으면 최단거리 탐색
        Transform nearest = explicitTarget ? explicitTarget : FindNearestTarget(turret.position);
        if (!nearest) yield break;

        // 2) 조준(+Y 전방 기준)
        Vector2 aimDir = ((Vector2)nearest.position - (Vector2)turret.position).normalized;
        float angleDeg = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg - 90f;

        // 3) 시각 오브젝트
        GameObject beam = null;
        LineRenderer lr = null;

        if (laserPrefab)
        {
            beam = Object.Instantiate(laserPrefab);
            beam.name = "AutoLaserBeam";
            beam.transform.SetPositionAndRotation(
                turret.position + (Vector3)(aimDir * (length * 0.5f)),
                Quaternion.AngleAxis(angleDeg, Vector3.forward)
            );
            beam.transform.localScale = new Vector3(0.001f, length, 1f); // x=폭, y=길이
        }
        else
        {
            beam = new GameObject("AutoLaserLine");
            lr = beam.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetPosition(0, turret.position);
            lr.SetPosition(1, turret.position + (Vector3)(aimDir * length));
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.textureMode = LineTextureMode.Stretch;
            lr.alignment = LineAlignment.TransformZ;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;
            lr.widthMultiplier = 0.001f;
            if (!lr.material) lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        // 4) 관통 판정 + 1회 피해 보장
        var hitOnce = HashSetPool<Enemy>.Get();
        float t = 0f;

        var filter = new ContactFilter2D();
        filter.NoFilter();
        if (hitMask.value != 0) filter.SetLayerMask(hitMask);
        filter.useTriggers = Physics2D.queriesHitTriggers;

        while (t < duration)
        {
            // 0 -> 1 -> 0 두께 애니메이션
            float f = Mathf.Sin((t / duration) * Mathf.PI);
            float currentWidth = Mathf.Max(0.001f, maxWidth * f);

            // 시각 갱신
            if (lr != null)
            {
                lr.widthMultiplier = currentWidth;
                lr.SetPosition(0, turret.position);
                lr.SetPosition(1, turret.position + (Vector3)(aimDir * length));
            }
            else
            {
                beam.transform.position = turret.position + (Vector3)(aimDir * (length * 0.5f));
                beam.transform.rotation = Quaternion.AngleAxis(angleDeg, Vector3.forward);
                var s = beam.transform.localScale; s.x = currentWidth; s.y = length; beam.transform.localScale = s;
            }

            // OverlapBox 관통 판정
            Vector2 center = (Vector2)turret.position + (aimDir * (length * 0.5f));
            Vector2 size = new Vector2(currentWidth, length);

            int count = Physics2D.OverlapBox(center, size, angleDeg, filter, overlapBuf);
            for (int i = 0; i < count; i++)
            {
                var col = overlapBuf[i];
                if (!col) continue;
                if (!string.IsNullOrEmpty(targetTag) && !col.CompareTag(targetTag)) continue;

                var enemy = col.GetComponentInParent<Enemy>();
                if (!enemy) continue;

                if (hitOnce.Add(enemy))
                    enemy.TakeDamage(Mathf.RoundToInt(damage), "AutoLaser");
            }

            t += Time.deltaTime;
            yield return null;
        }

        HashSetPool<Enemy>.Release(hitOnce);
        Object.Destroy(beam);
    }

    // 최단거리 표적 탐색
    private Transform FindNearestTarget(Vector3 origin)
    {
        int mask = (hitMask.value != 0) ? hitMask.value : ~0;
        float r = (searchRadius > 0f) ? searchRadius : length;

        // r 반경 내 모든 콜라이더를 배열로 수집
        Collider2D[] cols = Physics2D.OverlapCircleAll(origin, r, mask);

        float bestDistSq = float.PositiveInfinity;
        Transform best = null;

        foreach (var col in cols)
        {
            if (!col) continue;
            if (!string.IsNullOrEmpty(targetTag) && !col.CompareTag(targetTag)) continue;

            var enemy = col.GetComponentInParent<Enemy>();
            if (!enemy) continue;

            float d2 = (enemy.transform.position - origin).sqrMagnitude;
            if (d2 < bestDistSq)
            {
                bestDistSq = d2;
                best = enemy.transform;
            }
        }
        return best;
    }

}

// 간단한 HashSet 풀 (중복피격 방지용)
static class HashSetPool<T>
{
    static readonly Stack<HashSet<T>> pool = new Stack<HashSet<T>>();
    public static HashSet<T> Get() => pool.Count > 0 ? pool.Pop() : new HashSet<T>();
    public static void Release(HashSet<T> set) { set.Clear(); pool.Push(set); }
}
