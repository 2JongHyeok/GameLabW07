// Core.cs (교체/추가 부분)

using System;
using TMPro;
using UnityEngine;

public class Core : MonoBehaviour
{
    [Header("Core 설정")]
    public int maxHP = 100;
    private int currentHP;
    public TMP_Text CoreHpText;
    [SerializeField] private InventoryManger inventoryManger;

    // 현재 체력 읽기용 
    public int CurrentHP => currentHP;

    // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    // 이벤트 정의
    public event Action OnDie;
    public event Action<int> OnHpChanged;
    public event Action OnRevive;              //  추가: 부활 이벤트
    private bool isDead = false;
    // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

    [Header("Game Over 설정")]
    public bool endGameOnDie = true;           //  추가: Planet2는 false로 설정

    private void Awake()
    {
        currentHP = maxHP;
        UpdateHPText();
    }

    private void UpdateHPText()
    {
        if (CoreHpText != null)
            CoreHpText.text = $"Core HP: {currentHP}/{maxHP}";
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;
        OnHpChanged?.Invoke(currentHP);
        UpdateHPText();

        if (!isDead && CurrentHP <= 0)
            Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDie?.Invoke();

        //  Planet1은 true → 기존처럼 GameOver, Planet2는 false → 게임은 계속
        if (endGameOnDie)
            GameOver();
        // else: 비파괴 상태로 유지(부활 가능)
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
        }
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
