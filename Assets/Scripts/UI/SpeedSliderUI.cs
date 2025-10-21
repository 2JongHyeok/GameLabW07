// SpeedSliderUI.cs
using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트를 사용하기 위해 꼭 필요합니다.

public class SpeedSliderUI : MonoBehaviour
{
    [Header("방송국 연결")]
    [SerializeField] private SpeedDataSO speedData;

    [Header("UI 이미지 연결")]
    [Tooltip("현재 속도를 표시할 흰색 바 이미지")]
    [SerializeField] private Image currentSpeedBar;

    [Tooltip("제한된 최대 속도를 표시할 회색 마커 이미지")]
    [SerializeField] private Image effectiveMaxSpeedIndicator;

    void Update()
    {
        if (speedData == null || currentSpeedBar == null || effectiveMaxSpeedIndicator == null) return;

        // 이론상 최대 속도가 0이면 나누기 오류가 발생하므로 방지합니다.
        if (speedData.AbsoluteMaxSpeed <= 0) return;

        // 현재 속도 바의 채움 정도를 계산합니다. (현재속도 / 이론상최대속도)
        currentSpeedBar.fillAmount = speedData.CurrentSpeed / speedData.AbsoluteMaxSpeed;

        // 유효 최대 속도 마커의 채움 정도를 계산합니다. (유효최대속도 / 이론상최대속도)
        effectiveMaxSpeedIndicator.fillAmount = speedData.EffectiveMaxSpeed / speedData.AbsoluteMaxSpeed;
    }
}