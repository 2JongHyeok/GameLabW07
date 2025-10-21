// ThrustSliderUI.cs
using UnityEngine;
using UnityEngine.UI;

public class ThrustSliderUI : MonoBehaviour
{
    [Header("방송국 연결")]
    [Tooltip("추력 데이터를 실시간으로 받아올 ThrustDataSO를 연결하세요.")]
    [SerializeField] private ThrustDataSO thrustData;

    [Header("UI 요소 연결")]
    [Tooltip("현재 추력을 표시할 메인 슬라이더")]
    [SerializeField] private Slider currentThrustSlider;

    [Tooltip("무게 패널티로 제한된 최대 추력을 표시할 보조 슬라이더 (메인 슬라이더 뒤에 배치)")]
    [SerializeField] private Slider maxThrustIndicatorSlider;

    void Update()
    {
        if (thrustData == null) return;

        // 슬라이더의 최대값은 항상 우주선의 최대 잠재력으로 설정
        currentThrustSlider.maxValue = thrustData.MaxPossibleThrust;
        maxThrustIndicatorSlider.maxValue = thrustData.MaxPossibleThrust;

        // 현재 값들을 방송국에서 받아와 실시간으로 업데이트
        currentThrustSlider.value = thrustData.CurrentThrust;
        maxThrustIndicatorSlider.value = thrustData.EffectiveMaxThrust;
    }
}