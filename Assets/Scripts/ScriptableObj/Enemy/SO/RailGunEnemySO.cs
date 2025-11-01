using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "RailGunEnemySO", menuName = "ScriptableObjects/Enemy/RailGunEnemySO", order = 1)]
public class RailGunEnemySO : EnemyBaseSO
{
    [Header("Railgun Stats")]
    public float attackRange = 15f;
    public float attackCooldown = 7f; 
    public float chargeTime = 5.0f;
    public float laserDuration = 0.5f; 
    public int railgunDamage = 1000; // 레이저 지속시간 동안의 총 데미지

    // --- [새로 추가된 변수] ---
    [Tooltip("레이저가 데미지를 주는 간격(초). 값을 높이면 틱이 줄어들어 약해지고, 낮추면 틱이 많아져 강해집니다.")]
    public float damageTickRate = 0.05f; // 기본값 0.05초
    // --- [추가 끝] ---
    
    [Header("Physics")]
    public LayerMask hitLayers; // Raycast가 충돌할 레이어들 (Enemy 자신은 제외)

    [Header("Effects Prefabs")]
    public GameObject chargeEffectPrefab;
    public GameObject laserEffectPrefab;  

    private Dictionary<Enemy, Coroutine> runningAttacks = new Dictionary<Enemy, Coroutine>();

    public override void PerformAttack(Enemy enemy)
    {
        if (runningAttacks.ContainsKey(enemy)) return; 
        // if (Vector2.Distance(enemy.transform.position, enemy.target.position) > attackRange) return;
        
        Coroutine attackCoroutine = enemy.StartCoroutine(RailgunAttackSequence(enemy));
        runningAttacks.Add(enemy, attackCoroutine);
    }

    
    private IEnumerator RailgunAttackSequence(Enemy enemy)
    {
        // --- 1. 차징 ---
        GameObject chargeEffect = null;
        if (chargeEffectPrefab != null)
        {
            chargeEffect = Instantiate(chargeEffectPrefab, enemy.firePoint.position, enemy.firePoint.rotation, enemy.firePoint);
            
            // [수정] Enemy에 차징 이펙트 등록
            if (enemy != null) enemy.activeSpecialEffect = chargeEffect;
        }

        float chargeTimer = 0f;
        float actualChargeTime = this.chargeTime; 

        if (EnemyBuffManager.Instance != null && EnemyBuffManager.Instance.IsCommanderActive)
        {
            actualChargeTime *= EnemyBuffManager.Instance.railgunChargeTimeMultiplier;
        }
        
        while (chargeTimer < actualChargeTime)
        {
            if (enemy == null || enemy.isDead || !enemy.isAttacking)
            {
                if (chargeEffect != null) Destroy(chargeEffect);
                // [수정] Enemy 참조 클리어
                if (enemy != null) 
                {
                    enemy.activeSpecialEffect = null; 
                    runningAttacks.Remove(enemy);
                }
                yield break;
            }
            
            Vector2 directionToTarget = (enemy.target.position - enemy.firePoint.position).normalized;
            enemy.firePoint.rotation = Quaternion.LookRotation(Vector3.forward, directionToTarget);

            chargeTimer += Time.deltaTime;
            yield return null;
        }
        
        // 차징 완료 후 이펙트 파괴 및 참조 클리어
        if (chargeEffect != null) Destroy(chargeEffect);
        if (enemy != null) enemy.activeSpecialEffect = null;

        // --- 2. 발사 ---
        if (enemy == null || enemy.isDead || !enemy.isAttacking)
        {
            runningAttacks.Remove(enemy);
            yield break;
        }

        GameObject laserEffect = null;
        LineRenderer laserLine = null;
        if (laserEffectPrefab != null)
        {
            laserEffect = Instantiate(laserEffectPrefab, enemy.firePoint.position, enemy.firePoint.rotation);
            laserLine = laserEffect.GetComponent<LineRenderer>();
            
            // [수정] Enemy에 레이저 이펙트 등록
            if (enemy != null) enemy.activeSpecialEffect = laserEffect; 
        }

        // ... (레이저 발사 while 루프 및 데미지 로직은 이전과 동일) ...
        Vector2 fireDirection = (enemy.target.position - enemy.firePoint.position).normalized;
        Vector2 startPoint = enemy.firePoint.position;
        float laserTimer = 0f;
        float tickRate = Mathf.Max(0.01f, this.damageTickRate); 
        float nextTickTime = 0f;
        int totalTicks = Mathf.CeilToInt(laserDuration / tickRate);
        if (totalTicks <= 0) totalTicks = 1;
        int damagePerTick = Mathf.Max(1, railgunDamage / totalTicks);

        while (laserTimer < laserDuration)
        {
            // [중요] 사망 시 루프 탈출
            if (enemy == null || enemy.isDead || !enemy.gameObject.activeInHierarchy)
            {
                break; 
            }

            laserTimer += Time.deltaTime;

            // ... (Raycast 및 데미지 처리 로직) ...
            RaycastHit2D hit = Physics2D.Raycast(startPoint, fireDirection, attackRange, hitLayers);
            Vector2 beamEndPoint = startPoint + fireDirection * attackRange; 
            if (hit.collider != null)
            {
                beamEndPoint = hit.point; 
                if (laserTimer >= nextTickTime)
                {
                    nextTickTime += tickRate; 
                    if (hit.collider.CompareTag("Core"))
                    {
                        Core core = hit.collider.GetComponent<Core>();
                        core?.TakeDamage(damagePerTick);
                    }
                    else
                    {
                        Tilemap tilemap = hit.collider.GetComponent<Tilemap>();
                        Planet planetManager = tilemap?.GetComponent<Planet>();
                        if (planetManager != null)
                        {
                            Vector3Int centerCellPos = tilemap.WorldToCell(hit.point);
                            for (int xOffset = -1; xOffset <= 1; xOffset++)
                            {
                                for (int yOffset = -1; yOffset <= 1; yOffset++)
                                {
                                    Vector3Int neighborCellPos = new Vector3Int(centerCellPos.x + xOffset, centerCellPos.y + yOffset, centerCellPos.z);
                                    planetManager.DamageTile(neighborCellPos, damagePerTick);
                                }
                            }
                        }
                    }
                }
            }
            if (laserLine != null)
            {
                laserLine.SetPosition(0, startPoint);
                laserLine.SetPosition(1, beamEndPoint);
            }
            // ... (여기까지 동일) ...

            yield return null; 
        }

        // --- 3. 발사 종료 및 정리 ---
        // (OnDisable이 이미 처리했을 수도 있지만, 정상 종료를 위해 여기서도 파괴)
        if (laserEffect != null) Destroy(laserEffect);
        
        if (enemy != null)
        {
            // [수정] Enemy 참조 클리어
            enemy.activeSpecialEffect = null;
            runningAttacks.Remove(enemy);
        }
    }
    
    public override void OnEnemyDisabled(Enemy enemy)
    {
        if (enemy != null && runningAttacks.ContainsKey(enemy))
        {
            // 코루틴도 확실하게 중지시킵니다.
            Coroutine coroutineToStop = runningAttacks[enemy];
            if (coroutineToStop != null)
            {
                enemy.StopCoroutine(coroutineToStop);
            }
            
            // 딕셔너리에서 제거
            runningAttacks.Remove(enemy);
            Debug.Log($"RailGunEnemy {enemy.enemyNum}가 비활성화되어 딕셔너리에서 제거됨.");
        }
    }
}