using System.Collections;
using UnityEngine;

public class HitFlashEffect : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private Color flashColor = Color.white; // 깜빡일 색상
    [SerializeField] private float flashDuration = 0.1f; // 깜빡임 지속 시간
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }
    
    // 피격 시 호출할 메서드
    public void Flash()
    {
        if (spriteRenderer == null) return;
        
        // 이미 깜빡이는 중이면 코루틴 중단
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        
        flashCoroutine = StartCoroutine(FlashCoroutine());
    }
    
    private IEnumerator FlashCoroutine()
    {
        // 색상을 플래시 색상으로 변경
        spriteRenderer.color = flashColor;
        
        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(flashDuration);
        
        // 원래 색상으로 복구
        spriteRenderer.color = originalColor;
        
        flashCoroutine = null;
    }
    
    // 원래 색상 재설정 (풀에서 재사용 시 필요)
    public void ResetColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
    }
}
