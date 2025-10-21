using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "DurabilityColorSettings", menuName = "Settings/Durability Color Settings")]
public class DurabilityColorSettingsSO : ScriptableObject
{
    [Tooltip("내구도가 높은 순서대로 정렬하지 않아도 괜찮습니다. 자동으로 정렬됩니다.")]
    public DurabilityColorMapping[] colorMappings;

    // SO가 활성화될 때 자동으로 배열을 정렬하는 함수
    private void OnEnable()
    {
        if (colorMappings != null && colorMappings.Length > 0)
        {
            colorMappings = colorMappings.OrderByDescending(mapping => mapping.durabilityThreshold).ToArray();
        }
    }

    /// <summary>
    /// 주어진 내구도에 맞는 색상을 찾아 반환합니다.
    /// </summary>
    public Color GetColorForDurability(int currentDurability)
    {
        if (colorMappings == null || colorMappings.Length == 0)
        {
            return Color.white;
        }

        // OnEnable에서 이미 정렬되었으므로, 바로 사용합니다.
        foreach (var mapping in colorMappings)
        {
            if (currentDurability >= mapping.durabilityThreshold)
            {
                return mapping.color;
            }
        }
        
        return colorMappings.Last().color;
    }
}