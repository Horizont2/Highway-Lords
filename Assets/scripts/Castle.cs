using UnityEngine;
using UnityEngine.UI;

public class Castle : MonoBehaviour
{
    [Header("=== БАЛАНС (ЕКСТРЕМАЛЬНИЙ) ===")]
    public int baseHealth = 100; 
    public int hpBonusPerUpgrade = 50;

    public HealthBarSegments hpSegments;
    
    [Header("=== ЕКОНОМІКА ===")]
    public int castleLevel = 1;
    public int baseUpgradeCost = 150;    
    public float costMultiplier = 1.5f;  

    [Header("=== СТАН (Read Only) ===")]
    public int maxHealth;
    public int currentHealth;
    private bool isDead = false;

    [Header("=== UI & КОМПОНЕНТИ ===")]
    public Image healthBarFill; 
    public Transform spawnPoint; 

    void Start()
    {
        RecalculateMaxHealth();
        
        if (currentHealth <= 0) currentHealth = maxHealth;

        UpdateUI();
        isDead = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.castle = this;
            
            if (spawnPoint != null)
                GameManager.Instance.unitSpawnPoint = spawnPoint;
            else
                GameManager.Instance.unitSpawnPoint = transform;
        }

        if (hpSegments != null)
        {
            hpSegments.UpdateSegments(maxHealth);
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

    public void HealMax()
    {
        isDead = false;
        currentHealth = maxHealth;
        UpdateUI();
        Debug.Log("Castle: Healed to " + maxHealth);
    }

    public int GetUpgradeCost()
    {
        return Mathf.RoundToInt(baseUpgradeCost * Mathf.Pow(costMultiplier, castleLevel - 1));
    }

    public void UpgradeCastle()
    {
        castleLevel++;
        
        maxHealth += hpBonusPerUpgrade; 
        currentHealth = maxHealth; 

        if (SoundManager.Instance != null) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.constructionComplete);

        if (CameraShake.Instance != null) 
            CameraShake.Instance.Shake(0.1f, 0.2f);

        UpdateUI();
        Debug.Log($"Castle Upgraded! Lvl {castleLevel}. New Max HP: {maxHealth}");

        if (hpSegments != null)
        {
            hpSegments.UpdateSegments(maxHealth); 
        }

        if (GameManager.Instance != null) GameManager.Instance.SaveGame();
    }

    void RecalculateMaxHealth()
    {
        maxHealth = baseHealth + ((castleLevel - 1) * hpBonusPerUpgrade);
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }

    public void LoadState(int savedLevel)
    {
        castleLevel = savedLevel;
        if (castleLevel < 1) castleLevel = 1;
        
        RecalculateMaxHealth();
        currentHealth = maxHealth; 
        UpdateUI();

        // ДОДАНО: Оновлюємо рисочки після завантаження гри!
        if (hpSegments != null)
        {
            hpSegments.UpdateSegments(maxHealth);
        }
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