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
    [SerializeField] private InventoryManger inventoryManger;
    [SerializeField] private GameObject dockingSystem;
    [SerializeField] private GameObject autoTurret;
    [SerializeField] private GameObject sucksionZone;
    [SerializeField] private Image alertImage;
    [SerializeField] private DockingStation dockingStation;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private GameObject enemyAttackZone;
    private bool isAlert = false;
    public static event Action<int> OnCoreDied;   // 코어 번호 브로드캐스트
    public bool IsDead => isDead;                 // 외부에서 생존 확인
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
        alertImage.color = new Color(alertImage.color.r, alertImage.color.g, alertImage.color.b, 0f);
    }

    private void Start()
    {
        UpdateHealthBar(); // 초기 체력 표시
    }
    private void UpdateHealthBar()
    {
        float fillAmount = currentHP / (float)maxHP;
        if (healthFillImage != null)
        {
            float scale = Mathf.Lerp(0f, 1f, fillAmount); // HP가 줄수록 작아짐
            healthFillImage.rectTransform.localScale = new Vector3(1f, scale, 1f);
        }

    }
    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;
        OnHpChanged?.Invoke(currentHP);
        UpdateHealthBar();
        StartCoroutine(FadeInAndOut(alertImage, 0.5f, 0.5f));

        if (!isDead && CurrentHP <= 0)
        {
            Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (var enemy in allEnemies)
            {
                if (enemy != null)
                    enemy.isAttacking = false;
            }
            Die();
        }
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
        Debug.Log($"isShipDocked = {dockingStation.IsShipDocked()}, CurrentDockedStation = {DockingStation.CurrentDockedStation}");
        if (isDead) return;
        isDead = true;
        OnDie?.Invoke();
        OnCoreDied?.Invoke(coreNumber);
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
            if (dockingStation.IsShipDocked() && DockingStation.CurrentDockedStation == dockingStation)
            {
                dockingStation.PlayerGoToSpace();
            }
        }
        Debug.Log(DockingStation.CurrentDockedStation);

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
        enemyAttackZone.SetActive(false);
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
        UpdateHealthBar();

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
        enemyAttackZone.SetActive(true);
    }
    public void AddMaxHP(int amount)
    {
        maxHP += amount;
        currentHP += amount;
        OnHpChanged?.Invoke(currentHP);
        UpdateHealthBar();
    }

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
