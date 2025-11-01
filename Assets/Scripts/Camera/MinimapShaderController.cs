using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MinimapShaderController : MonoBehaviour
{
    public Shader replacementShader;
    public string replacementTag = "RenderType"; // 위 쉐이더의 "RenderType" 태그와 맞춥니다.

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void OnEnable()
    {
        if (cam == null) cam = GetComponent<Camera>();

        if (replacementShader != null)
        {
            // 이 카메라가 렌더링할 때 "RenderType" 태그를 가진 쉐이더를
            // 모두 'replacementShader'로 교체합니다.
            cam.SetReplacementShader(replacementShader, replacementTag);
        }
    }

    void OnDisable()
    {
        // 스크립트가 비활성화되면 원래대로 복구합니다.
        cam.ResetReplacementShader();
    }
}