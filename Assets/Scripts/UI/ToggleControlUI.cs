using UnityEngine;
using UnityEngine.UI;

public class ToggleControlUI : MonoBehaviour
{
    public GameObject controlUI1;
    public GameObject controlUI2;
    
    bool isUI1Toggled = false;
    bool isUI2Toggled = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // ui의 canvas group 컴포넌트의 alpha를 토글하는 함수
    public void OnToggleButtonClick()
    {
        if (isUI1Toggled)
        {
            controlUI1.GetComponent<CanvasGroup>().alpha = 1;
            isUI1Toggled = false;
        }
        else if (!isUI1Toggled)
        {
            controlUI1.GetComponent<CanvasGroup>().alpha = 0;
            isUI1Toggled = true;
        }
        
        if(isUI2Toggled)
        {
            controlUI2.GetComponent<CanvasGroup>().alpha = 1;
            isUI2Toggled = false;
        }
        else if(!isUI2Toggled)
        {
            controlUI2.GetComponent<CanvasGroup>().alpha = 0;
            isUI2Toggled = true;
        }
    }
}
