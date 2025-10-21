// RandomizedSpawnerTile.cs
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

// 이 메뉴를 통해 Project 창에서 쉽게 '스포너 타일' 에셋을 만들 수 있게 됩니다.
[CreateAssetMenu(fileName = "New Randomized Spawner Tile", menuName = "Tiles/Randomized Spawner Tile")]
public class RandomizedSpawnerTile : Tile // 일반 Tile을 상속받습니다.
{
    [Header("확률 설정")]
    [Tooltip("게임 시작 시 이 타일이 변할 수 있는 타일들의 목록과 확률 가중치입니다.")]
    public List<WeightedTileOutcome> possibleOutcomes;

    /// <summary>
    /// 설정된 확률에 따라 무작위 결과 타일 하나를 반환합니다.
    /// </summary>
    public TileBase GetRandomOutcome()
    {
        if (possibleOutcomes == null || possibleOutcomes.Count == 0)
        {
            return null; // 결과가 없으면 null 반환
        }

        // 1. 모든 가중치의 합계를 구합니다.
        float totalWeight = possibleOutcomes.Sum(outcome => outcome.weight);

        // 2. 0부터 전체 가중치 합계 사이의 랜덤한 숫자를 뽑습니다.
        float randomValue = Random.Range(0, totalWeight);

        // 3. 목록을 순회하며 랜덤 숫자를 깎아내리다가, 0 이하가 되는 순간의 타일을 선택합니다.
        foreach (var outcome in possibleOutcomes)
        {
            if (randomValue <= outcome.weight)
            {
                return outcome.resultingTile;
            }
            randomValue -= outcome.weight;
        }
        
        return null; // 만약을 위한 예외 처리
    }
}