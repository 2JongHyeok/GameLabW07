// TileDamageEvent.cs
using UnityEngine;

// 이 구조체는 '어디에', '얼마나' 데미지를 줄지에 대한 정보를 담습니다.
[System.Serializable]
public struct TileDamageEvent
{
    public Vector3Int cellPosition; // 데미지를 입힐 타일의 좌표
    public int damageAmount;      // 데미지 양
}