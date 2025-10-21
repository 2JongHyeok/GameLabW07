// SpeedDataSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Speed Data", menuName = "Spaceship/Speed Data")]
public class SpeedDataSO : ScriptableObject
{
    [Header("실시간 속도 데이터")]
    [Tooltip("우주선의 현재 속도")]
    public float CurrentSpeed;

    [Tooltip("무게 0일 때의 이론상 최대 속도 (고정값)")]
    public float AbsoluteMaxSpeed;

    [Tooltip("광물 무게 패널티가 적용된 현재의 유효 최대 속도")]
    public float EffectiveMaxSpeed;
}