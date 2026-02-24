using UnityEngine;
using UnityEngine.UI;

public class Castle : MonoBehaviour
{
    [Header("=== БАЛАНС (ЕКСТРЕМАЛЬНИЙ) ===")]
    // Початкове здоров'я (Wave 1)
    public int baseHealth = 100; 
    // Бонус, який додається ДО МАКСИМУМУ тільки при UpgradeCastle()
    public int hpBonusPerUpgrade = 50; 
    
    [Header("=== ЕКОНОМІКА ===")]
    public int castleLevel = 1;
    public int baseUpgradeCost = 150;    // Ціна першого апгрейду
    public float costMultiplier = 1.5f;  // Коефіцієнт подорожчання (x1.5)

    [Header("=== СТАН (Read Only) ===")]
    public int maxHealth;
    public int currentHealth;
    private bool isDead = false;

    [Header("=== UI & КОМПОНЕНТИ ===")]
    public Image healthBarFill; // Зелене кільце/смужка (Filled)
    public Transform spawnPoint; // Точка виходу військ

    void Start()
    {
        // При старті розраховуємо ліміт на основі поточного рівня
        RecalculateMaxHealth();
        
        // Якщо здоров'я ще не задане (новий запуск), лікуємо до максимуму
        if (currentHealth <= 0) currentHealth = maxHealth;

        UpdateUI();
        isDead = false;

        // Реєстрація в GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.castle = this;
            
            if (spawnPoint != null)
                GameManager.Instance.unitSpawnPoint = spawnPoint;
            else
                GameManager.Instance.unitSpawnPoint = transform;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.15f, 0.1f);
        
        if (GameManager.Instance != null) 
            GameManager.Instance.ShowDamage(damage, transform.position + Vector3.up * 2f);

        if (SoundManager.Instance != null && SoundManager.Instance.castleDamage != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.castleDamage);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        
        UpdateUI();
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateUI();
    }

    // Викликається при старті нової хвилі або рестарті
    public void HealMax()
    {
        isDead = false;
        currentHealth = maxHealth;
        UpdateUI();
        Debug.Log("Castle: Healed to " + maxHealth);
    }

    // === СИСТЕМА АПГРЕЙДУ (Викликається з меню Constructions) ===

    public int GetUpgradeCost()
    {
        return Mathf.RoundToInt(baseUpgradeCost * Mathf.Pow(costMultiplier, castleLevel - 1));
    }

    public void UpgradeCastle()
    {
        castleLevel++;
        
        // ВАЖЛИВО: Додаємо бонус до максимуму ТІЛЬКИ ТУТ
        maxHealth += hpBonusPerUpgrade; 
        
        // Повне лікування при покращенні стін
        currentHealth = maxHealth; 

        // Ефекти
        if (SoundManager.Instance != null) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.constructionComplete);

        if (CameraShake.Instance != null) 
            CameraShake.Instance.Shake(0.1f, 0.2f);

        UpdateUI();
        Debug.Log($"Castle Upgraded! Lvl {castleLevel}. New Max HP: {maxHealth}");

        // Зберігаємо гру
        if (GameManager.Instance != null) GameManager.Instance.SaveGame();
    }

    // Використовується тільки при завантаженні або ініціалізації рівня
    void RecalculateMaxHealth()
    {
        maxHealth = baseHealth + ((castleLevel - 1) * hpBonusPerUpgrade);
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }

    // Завантаження стану зі збережень
    public void LoadState(int savedLevel)
    {
        castleLevel = savedLevel;
        if (castleLevel < 1) castleLevel = 1;
        
        RecalculateMaxHealth();
        currentHealth = maxHealth; 
        UpdateUI();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("💀 CASTLE DESTROYED!");
        if (GameManager.Instance != null) GameManager.Instance.Defeat();
    }

    void UpdateUI()
    {
        if (healthBarFill != null && maxHealth > 0)
        {
            healthBarFill.fillAmount = (float)currentHealth / maxHealth;
        }
    }
}