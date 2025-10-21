// TileDamageEventChannelSO.cs
using UnityEngine;

// EventChannelSO<T>를 상속받아 TileDamageEvent 타입 전용 채널을 만듭니다.
[CreateAssetMenu(menuName = "Events/Tile Damage Event Channel")]
public class TileDamageEventChannelSO : EventChannelSO<TileDamageEvent>
{
    // 내용은 비어있어도 됩니다. 상속만으로 모든 기능이 구현됩니다.
}