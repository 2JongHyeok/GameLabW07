using System;
using UnityEngine;

// 이 스크립트를 모든 광물(Ore) 프리팹에 붙여주세요.
public class Ore : MonoBehaviour
{
    [Tooltip("이 광물의 종류를 설정하세요 (예: Coal, Iron)")]
    public OreType oreType;

    [Tooltip("이 광물 덩어리 하나가 몇 개로 취급될지 설정하세요.")]
    public int amount = 1;

    private void OnDestroy()
    {
        Debug.Log("누가 지움?");
    }
}