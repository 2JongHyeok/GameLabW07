// SatelliteDamage.cs (Unity 6)
// 역할: 위성(Trigger)이 Enemy에 닿으면 데미지 적용.
// 필요 조건: Satellite 오브젝트에 Collider2D(isTrigger=true) 있고,
//            Enemy 또는 Satellite 중 하나에 Rigidbody2D 존재(권장: Satellite에 Kinematic).

using UnityEngine;

[DisallowMultipleComponent]
public class SatelliteDamage : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("피해 소스 표기(로그/디버그용)")]
    [SerializeField] private string damageSourceName = "Satellite";

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 태그 체크(프로젝트 규칙과 동일하게 "Enemy" 사용)
        if (!other.CompareTag("Enemy")) return;

        // Enemy 컴포넌트 가져오기
        var enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        // 총알과 동일한 방식으로 데미지 적용
        int damage = Managers.Instance.weapon[0].GetDamage();
        enemy.TakeDamage(damage, damageSourceName);
    }
}
