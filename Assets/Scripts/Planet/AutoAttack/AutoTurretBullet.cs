using UnityEngine;

public class AutoTurretBullet : Projectile
{
    public float speed = 20f;
    public float lifeTime = 3f; // 3초 후 자동 삭제

    public override void Init(float dmg, Transform shooterTransform)
    {
        base.Init(dmg, shooterTransform);
        Destroy(gameObject, lifeTime); // 생성 후 lifeTime 초 뒤에 삭제 예약
    }

    void Update()
    {
        // 총알은 직진만 합니다.
        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);
    }
}