using UnityEngine;
using System.Collections;

public class RailGun : MonoBehaviour
{
    [Header("공격 설정")]
    public float chargeTime = 5.0f; // 레일건 차징 시간
    public float laserDuration = 0.5f; // 레이저 발사 지속 시간
    public float attackRange = 100f; // 레이저 공격 사거리

    [Header("참조")]
    public Transform muzzlePoint; // 레이저가 발사될 총구 위치 (적 모델의 자식으로 설정)
    public Transform playerTransform; // 플레이어의 Transform (외부에서 할당)

    [Header("레이저 시각 효과")]
    public Material laserMaterial; // 레이저에 사용할 Material (붉은색 발광 등)
    public Color chargeParticleColor = Color.cyan; // 차징 파티클 색상
    public Color laserColor = Color.red; // 레이저 색상

    // === 내부 사용 변수들 ===
    private LineRenderer laserLineRenderer;
    private ParticleSystem chargeParticles; // 차징 효과용 파티클 시스템
    private bool isCharging = false;
    private bool isAttacking = false;
    void Awake()
    {
        // Line Renderer 설정 (레이저 프리미티브)
        laserLineRenderer = gameObject.AddComponent<LineRenderer>();
        laserLineRenderer.material = laserMaterial; // 외부에서 할당된 Material 사용
        laserLineRenderer.startWidth = 0.1f; // 시작 두께
        laserLineRenderer.endWidth = 0.1f;   // 끝 두께
        laserLineRenderer.useWorldSpace = true; // 월드 공간 사용
        laserLineRenderer.enabled = false; // 기본적으로 비활성화

        // 레이저 색상 그라디언트 설정
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 1f); // 일정한 투명도
        laserLineRenderer.colorGradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(laserColor, 0f), new GradientColorKey(laserColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );

        // 차징 파티클 시스템 생성 (코드로 동적 생성)
        CreateChargeParticleSystem();
    }

    void Update()
    {
        // 플레이어 추적 및 공격 트리거는 외부 스크립트에서 이미 구현되었다고 가정
        // 예를 들어, 플레이어가 사거리 안에 들어오면 이 스크립트의 StartAttack() 호출
    }

    // 외부 스크립트에서 이 메서드를 호출하여 공격 시작
    public void StartAttack()
    {
        if (!isCharging && !isAttacking)
        {
            StartCoroutine(RailgunAttackSequence());
        }
    }

    private IEnumerator RailgunAttackSequence()
    {
        isCharging = true;
        Debug.Log("레일건 차징 시작!");

        // 1. 차징 시작 (파티클 시스템으로 총구에 에너지 모으는 효과)
        chargeParticles.Play(); // 파티클 재생
        SetChargeParticleSpeedAndSize(0.1f); // 초기 파티클 속도 및 크기 작게

        float currentChargeTime = 0f;
        while (currentChargeTime < chargeTime)
        {
            // 차징 진행도에 따라 파티클 효과 강화
            float chargeProgress = currentChargeTime / chargeTime;
            SetChargeParticleSpeedAndSize(Mathf.Lerp(0.1f, 1.0f, chargeProgress)); // 속도 및 크기 증가
            
            // 플레이어를 향해 총구 회전 (공격 전에 조준을 완료)
            if (playerTransform != null)
            {
                Vector3 directionToPlayer = (playerTransform.position - muzzlePoint.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                muzzlePoint.rotation = Quaternion.Slerp(muzzlePoint.rotation, targetRotation, Time.deltaTime * 5f); // 부드러운 회전
            }

            yield return null; // 한 프레임 대기
            currentChargeTime += Time.deltaTime;
        }

        // 2. 차징 완료 (파티클 시스템 정지 또는 폭발 효과 전환)
        chargeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // 파티클 정지
        // 차징 완료 효과 (예: 잠시 더 밝은 빛, 작은 폭발)는 추가 파티클 시스템으로 구현 가능
        
        isCharging = false;
        isAttacking = true;
        Debug.Log("레일건 발사!");

        // 3. 레이저 발사
        laserLineRenderer.enabled = true; // Line Renderer 활성화

        Vector3 startPoint = muzzlePoint.position;
        Vector3 endPoint;

        // 플레이어 방향으로 레이저 발사
        RaycastHit hit;
        // 총구의 정방향으로 레이저 발사
        if (Physics.Raycast(muzzlePoint.position, muzzlePoint.forward, out hit, attackRange))
        {
            endPoint = hit.point;
            // !!! 여기에 데미지 처리 로직을 넣으세요 !!!
            // 예: hit.collider.GetComponent<PlayerHealth>()?.TakeDamage(damageAmount);
            // hit.collider.gameObject 에 대한 데미지 처리
            Debug.Log($"레이저가 {hit.collider.name} 에 명중! (거리: {hit.distance})");
        }
        else
        {
            // 아무것도 맞지 않으면 사거리 끝까지
            endPoint = muzzlePoint.position + muzzlePoint.forward * attackRange;
            Debug.Log($"레이저 발사, 명중 없음 (사거리 끝까지: {attackRange})");
        }

        laserLineRenderer.SetPosition(0, startPoint);
        laserLineRenderer.SetPosition(1, endPoint);

        // 레이저 발사 지속 시간
        yield return new WaitForSeconds(laserDuration);

        // 4. 공격 종료
        laserLineRenderer.enabled = false; // Line Renderer 비활성화
        isAttacking = false;
        Debug.Log("레일건 공격 종료!");

        // 다음 공격까지 쿨타임 등을 외부에서 제어
    }

    // 차징 파티클 시스템 생성 메서드
    private void CreateChargeParticleSystem()
    {
        // 파티클 시스템 GameObject 생성
        GameObject particleGO = new GameObject("ChargeParticles");
        particleGO.transform.parent = muzzlePoint; // 총구에 자식으로 붙임
        particleGO.transform.localPosition = Vector3.zero; // 총구와 같은 위치
        particleGO.transform.localRotation = Quaternion.identity; // 총구와 같은 회전

        chargeParticles = particleGO.AddComponent<ParticleSystem>();

        var main = chargeParticles.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 1f;
        main.startSize = 0.1f;
        main.maxParticles = 50;
        main.playOnAwake = false; // 수동으로 재생
        main.loop = true; // 차징 동안 계속 방출

        var emission = chargeParticles.emission;
        emission.rateOverTime = 30f; // 초당 30개 방출

        var shape = chargeParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere; // 구 형태로 방출
        shape.radius = 0.1f; // 총구 주변에서 작게 방출

        var renderer = chargeParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard; // 항상 카메라를 바라보게
        // 기본 Material은 Unity Standard Assets -> Particle -> Materials 에 있는 Default-Particle 등을 사용
        // 또는 발광하는 Material을 만들어 할당
        renderer.material = new Material(Shader.Find("Sprites/Default")); // 기본 Material 설정 (이후 인스펙터에서 발광 Material로 교체 추천)
        renderer.material.color = chargeParticleColor; // 파티클 색상 설정

        // 파티클 색상 오버 라이프타임 (점점 밝아지거나 사라지게)
        var colorOverLifetime = chargeParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(chargeParticleColor, 0f), new GradientColorKey(chargeParticleColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        chargeParticles.Stop(); // 처음에는 정지 상태
    }

    // 차징 파티클의 속도와 크기를 조절하는 헬퍼 함수
    private void SetChargeParticleSpeedAndSize(float scale)
    {
        var main = chargeParticles.main;
        main.startSpeed = 1f * scale;
        main.startSize = 0.1f * scale;

        var emission = chargeParticles.emission;
        emission.rateOverTime = 30f * scale; // 방출량도 조절하여 더 강하게 보이도록
    }
}
