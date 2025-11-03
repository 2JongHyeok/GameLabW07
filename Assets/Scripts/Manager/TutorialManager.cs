using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Image tutorialImageDisplay; // 튜토리얼 이미지를 표시할 Image 컴포넌트
    public Button nextButton;          // 다음 버튼
    public Button prevButton;          // 이전 버튼
    public Button skipButton;          // 스킵/게임시작 버튼 (하나로 통일)

    [Header("Tutorial Images")]
    public List<Sprite> tutorialSprites; // 8개의 튜토리얼 Sprite 리스트

    // --- 내부 상태 변수 ---
    private int currentImageIndex = 0;
    private int totalImageCount;
    private TextMeshProUGUI skipButtonText; // 스킵 버튼의 텍스트 컴포넌트

    void Start()
    {
        // 튜토리얼 이미지의 총 개수를 설정
        totalImageCount = tutorialSprites.Count;

        skipButtonText = skipButton.GetComponentInChildren<TextMeshProUGUI>();
        if (skipButtonText == null)
        {
            Debug.LogError("Skip 버튼의 자식 오브젝트에서 TextMeshProUGUI 컴포넌트를 찾을 수 없습니다. 연결되었는지 확인하세요.");
        }

  
        nextButton.onClick.AddListener(OnNextButtonClicked);
        prevButton.onClick.AddListener(OnPrevButtonClicked);
        skipButton.onClick.AddListener(OnGameStartOrSkipClicked);

        UpdateTutorialScreen();
    }

    private void UpdateTutorialScreen()
    {
        if (tutorialSprites.Count > 0 && currentImageIndex >= 0 && currentImageIndex < totalImageCount)
        {
            tutorialImageDisplay.sprite = tutorialSprites[currentImageIndex];
        }

        bool isLastPage = currentImageIndex == totalImageCount - 1;

        prevButton.gameObject.SetActive(currentImageIndex > 0);

        nextButton.gameObject.SetActive(!isLastPage);

        if (skipButtonText != null)
        {
            if (isLastPage)
            {
                skipButtonText.text = "GameStart";
            }
            else
            {
                skipButtonText.text = "Skip\n Tutorial";
            }
        }
    }

    private void OnNextButtonClicked()
    {
        if (currentImageIndex < totalImageCount - 1)
        {
            currentImageIndex++;
            UpdateTutorialScreen();
        }
    }

    private void OnPrevButtonClicked()
    {
        if (currentImageIndex > 0)
        {
            currentImageIndex--;
            UpdateTutorialScreen();
        }
    }

    private void OnGameStartOrSkipClicked()
    {
        tutorialImageDisplay.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false );
        prevButton.gameObject.SetActive(false );
        skipButton.gameObject.SetActive(false ); 
        Debug.Log("게임을 시작합니다!");
        Managers.Instance.IsTutorialActive = false;
    }
}