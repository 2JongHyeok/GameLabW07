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

    private void Awake()
    {
        currentHP = maxHP;
        UpdateHPText();
    }

    private void UpdateHPText()
    {
        if (CoreHpText != null)
        {
            CoreHpText.text = $"Core HP: {currentHP}/{maxHP}";
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        UpdateHPText();

        if (currentHP <= 0)
        {
            currentHP = 0;
            GameOver();
        }
    }

    private void GameOver()
    {
        Destroy(gameObject);
        // [Log] 웨이브 방어 실패 로그 출력
        GameAnalyticsLogger.instance.LogWaveFail(WaveManager.Instance.CurrentWaveIndex, Managers.Instance.core.CurrentHP);
        GameAnalyticsLogger.instance.LogWaveResources(WaveManager.Instance.CurrentWaveIndex, Managers.Instance.inventory.GetWaveResourceStats(WaveManager.Instance.CurrentWaveIndex));
        Managers.Instance.RestartPanel.SetActive(true);
        // 이후에 GameOver 연출이나 Scene 전환 로직을 여기에 추가 가능
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ( inventoryManger != null)
        {
            if(collision.gameObject.TryGetComponent<Ore>(out var ore))
            {
                inventoryManger.AddOre(ore.oreType, ore.amount);
                Destroy(collision.gameObject);
            }
        }
    }
    
    // 1회성 HP 회복 메서드
    public void HealHP(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        UpdateHPText();
    }
    
    // 외부에서 MaxHP 증가 및 UI 갱신
    public void AddMaxHP(int amount)
    {
        maxHP += amount;
        currentHP += amount; // 현재 HP도 함께 증가
        UpdateHPText();
    }
    
    // 외부에서 UI 갱신 호출용
    public void RefreshHPText()
    {
        UpdateHPText();
    }
}
