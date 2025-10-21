// EventChannelSO.cs
using UnityEngine;
using UnityEngine.Events;

// 제네릭(Generic)을 사용하여 어떤 타입의 데이터든 받을 수 있는 방송국 설계도
public abstract class EventChannelSO<T> : ScriptableObject
{
    public UnityAction<T> OnEventRaised;

    public void RaiseEvent(T value)
    {
        OnEventRaised?.Invoke(value);
    }
}