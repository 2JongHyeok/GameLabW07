// ThrustDataSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Thrust Data", menuName = "Spaceship/Thrust Data")]
public class ThrustDataSO : ScriptableObject
{
    [Header("실시간 추력 데이터")]
    [Tooltip("UI 표시용으로 부드럽게 변하는 현재 추력 값")]
    public float CurrentThrust;

    [Tooltip("업그레이드를 포함한 우주선의 최대 추력 (무게 0일 때)")]
    public float MaxPossibleThrust;

    [Tooltip("광물 무게 패널티가 적용된 현재의 유효 최대 추력")]
    public float EffectiveMaxThrust;
}