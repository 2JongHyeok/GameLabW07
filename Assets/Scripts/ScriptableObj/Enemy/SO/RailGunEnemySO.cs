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
        if (Vector2.Distance(enemy.transform.position, enemy.target.position) > attackRange) return;
        
        Coroutine attackCoroutine = enemy.StartCoroutine(RailgunAttackSequence(enemy));
        runningAttacks.Add(enemy, attackCoroutine);
    }

    private IEnumerator RailgunAttackSequence(Enemy enemy)
    {
        // --- 1. 차징 ---
        // (이전과 동일)
        GameObject chargeEffect = null;
        if (chargeEffectPrefab != null)
        {
            chargeEffect = Instantiate(chargeEffectPrefab, enemy.firePoint.position, enemy.firePoint.rotation, enemy.firePoint);
        }

        float chargeTimer = 0f;
        while (chargeTimer < chargeTime)
        {
            if (!enemy.isAttacking)
            {
                if (chargeEffect != null) Destroy(chargeEffect);
                runningAttacks.Remove(enemy);
                yield break;
            }
            Vector2 directionToTarget = (enemy.target.position - enemy.firePoint.position).normalized;
            enemy.firePoint.rotation = Quaternion.LookRotation(Vector3.forward, directionToTarget);

            chargeTimer += Time.deltaTime;
            yield return null;
        }
        if (chargeEffect != null) Destroy(chargeEffect);

        // --- 2. 발사 ---
        if (!enemy.isAttacking)
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
        }

        Vector2 fireDirection = (enemy.target.position - enemy.firePoint.position).normalized;
        Vector2 startPoint = enemy.firePoint.position;

        float laserTimer = 0f;
        
        // --- 데미지 계산 ---
        
        // --- [수정됨] ---
        // 인스펙터에서 설정한 값으로 틱 레이트 설정
        // (0 이하면 0.01f로 강제 보정하여 무한루프 방지)
        float tickRate = Mathf.Max(0.01f, this.damageTickRate); 
        // --- [수정 끝] ---
        
        float nextTickTime = 0f;
        
        // 총 틱 수 계산
        int totalTicks = Mathf.CeilToInt(laserDuration / tickRate);
        if (totalTicks <= 0) totalTicks = 1;

        // 틱당 데미지 계산 (총 데미지 / 틱 수)
        int damagePerTick = Mathf.Max(1, railgunDamage / totalTicks);

        // --- 레이저 발사 루프 (laserDuration 동안 매 프레임 실행) ---
        while (laserTimer < laserDuration)
        {
            laserTimer += Time.deltaTime;

            // 1. 매 프레임 레이캐스트로 '가장 가까운' 타겟 탐색
            RaycastHit2D hit = Physics2D.Raycast(startPoint, fireDirection, attackRange, hitLayers);
            
            Vector2 beamEndPoint = startPoint + fireDirection * attackRange; // 빔의 기본 끝점 (최대 사거리)
            
            // 2. 무언가 맞았다면
            if (hit.collider != null)
            {
                beamEndPoint = hit.point; // 빔의 끝점을 맞은 곳으로 변경

                // 3. 데미지 틱 타이머 확인
                if (laserTimer >= nextTickTime)
                {
                    nextTickTime += tickRate; // 다음 틱 시간 설정
                    
                    // Core를 때렸을 때 (코어는 3x3이 아님)
                    if (hit.collider.CompareTag("Core"))
                    {
                        Core core = hit.collider.GetComponent<Core>();
                        core?.TakeDamage(damagePerTick);
                        Debug.Log($"레일건 Core 타격 (틱 데미지: {damagePerTick})");
                    }
                    // 쉴드(Tilemap)를 때렸을 때
                    else
                    {
                        Tilemap tilemap = hit.collider.GetComponent<Tilemap>();
                        Planet planetManager = tilemap?.GetComponent<Planet>();
                        if (planetManager != null)
                        {
                            // 3x3 범위 데미지 처리
                            Vector3Int centerCellPos = tilemap.WorldToCell(hit.point);

                            for (int xOffset = -1; xOffset <= 1; xOffset++)
                            {
                                for (int yOffset = -1; yOffset <= 1; yOffset++)
                                {
                                    Vector3Int neighborCellPos = new Vector3Int(
                                        centerCellPos.x + xOffset,
                                        centerCellPos.y + yOffset,
                                        centerCellPos.z
                                    );
                                    
                                    planetManager.DamageTile(neighborCellPos, damagePerTick);
                                }
                            }
                            // Debug.Log($"레일건 쉴드 {centerCellPos} 주변 3x3 타격 (틱 데미지: {damagePerTick})");
                        }
                    }
                }
            }
            
            // 4. 레이저 시각 효과 매 프레임 업데이트
            if (laserLine != null)
            {
                laserLine.SetPosition(0, startPoint);
                laserLine.SetPosition(1, beamEndPoint);
            }

            yield return null; // 다음 프레임까지 대기
        }

        // --- 3. 발사 종료 및 정리 ---
        if (laserEffect != null) Destroy(laserEffect);
        runningAttacks.Remove(enemy);
    }
}