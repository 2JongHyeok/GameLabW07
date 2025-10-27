// Core.cs (교체/추가 부분)

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Core : MonoBehaviour
{
    [Header("Core 설정")]
    public int coreNumber = 1;                 // 추가: 코어 번호 (UI 표시용)
    public int maxHP = 100;
    private int currentHP;
    public TMP_Text CoreHpText;
    [SerializeField] private InventoryManger inventoryManger;
    [SerializeField] private GameObject dockingSystem;
    [SerializeField] private GameObject autoTurret;
    [SerializeField] private GameObject sucksionZone;
    [SerializeField] private Image alertImage;
    [SerializeField] private DockingStation dockingStation;
    private bool isAlert = false;
    
    // 현재 체력 읽기용 
    public int CurrentHP => currentHP;
    
    // 이벤트 정의
    public event Action OnDie;
    public event Action<int> OnHpChanged;
    public event Action OnRevive;              //  추가: 부활 이벤트
    private bool isDead = false;

    [Header("Game Over 설정")]
    public bool endGameOnDie = true;           //  추가: Planet2는 false로 설정

    private void Awake()
    {
        currentHP = maxHP;
        UpdateHPText();
        alertImage.color = new Color(alertImage.color.r, alertImage.color.g, alertImage.color.b, 0f);
    }

    private void UpdateHPText()
    {
        if (CoreHpText != null)
            if (coreNumber == 1)
                CoreHpText.text = $"Planet : {currentHP}/{maxHP}";
            else
                CoreHpText.text = $"Colony : {currentHP}/{maxHP}";
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;
        OnHpChanged?.Invoke(currentHP);
        UpdateHPText();
        StartCoroutine(FadeInAndOut(alertImage, 0.5f, 0.5f));

        if (!isDead && CurrentHP <= 0)
            Die();
    }
    
    
    public IEnumerator FadeInAndOut(Image image, float fadeInTime, float fadeOutTime)
    {
        if(isAlert) yield break;
        
        isAlert = true;
        
        // 1. 페이드 인 (Alpha 0 -> 1)
        float elapsedTime = 0f;
        Color originalColor = image.color;
        
        // 현재 알파 값에서 1을 향해 보간
        float startAlpha = originalColor.a; 

        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, 1f, elapsedTime / fadeInTime);
            image.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
            yield return null; // 다음 프레임까지 대기
        }
        
        // 정확히 1로 설정
        image.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);

        // 2. 페이드 아웃 (Alpha 1 -> 0)
        elapsedTime = 0f; // 경과 시간 리셋

        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
            image.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
            yield return null; // 다음 프레임까지 대기
        }

        // 정확히 0으로 설정
        image.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        
        isAlert = false;
    }


    public void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDie?.Invoke();
        HideCore();
        //  Planet1은 true → 기존처럼 GameOver, Planet2는 false → 게임은 계속
        if (endGameOnDie)
        {
            GameOver();
        }
            
        // else: 비파괴 상태로 유지(부활 가능)
    }
    private void HideCore()
    {
        if (dockingStation != null)
        {
            dockingStation.PlayerGoToSpace();
        }
        
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
            renderer.enabled = false;

        var colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
            col.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false; 
        }

        dockingSystem.SetActive(false);
        sucksionZone.SetActive(false);
        autoTurret.SetActive(false);
    }
    private void GameOver()
    {
        GameAnalyticsLogger.instance.LogWaveFail(Managers.Instance.core.CurrentHP);
        GameAnalyticsLogger.instance.LogWaveResources(
            Managers.Instance.inventory.GetWaveResourceStats(Planet1WaveManager.Instance.CurrentWaveIndex));
        Managers.Instance.RestartPanel.SetActive(true);
        Destroy(gameObject);
    }

    // 1회성 HP 회복 메서드
    public void HealHP(int amount)
    {
        int prev = currentHP;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        OnHpChanged?.Invoke(currentHP);
        UpdateHPText();

        //  죽어있던 코어가 0→양수로 회복되면 부활 처리
        if (isDead && prev <= 0 && currentHP > 0)
        {
            isDead = false;
            OnRevive?.Invoke();
            Revive();
        }
    }
    public void Revive()
    {

        // 렌더러 다시 켜기
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = true;

        // 콜라이더 다시 켜기
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = true;

        // Rigidbody 다시 활성화
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.simulated = true;

        // 다른 시스템도 다시 켜기
        dockingSystem.SetActive(true);
        sucksionZone.SetActive(true);
        autoTurret.SetActive(true);
    }
    public void AddMaxHP(int amount)
    {
        maxHP += amount;
        currentHP += amount;
        OnHpChanged?.Invoke(currentHP);
        UpdateHPText();
    }

    public void RefreshHPText() => UpdateHPText();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (inventoryManger != null && collision.gameObject.TryGetComponent<Ore>(out var ore))
        {
             if (collision.gameObject.GetComponent<Ore>().oreType == OreType.PlanetCore) return;
             inventoryManger.AddOre(ore.oreType, ore.amount);
             Destroy(collision.gameObject);
        }
    }
}
