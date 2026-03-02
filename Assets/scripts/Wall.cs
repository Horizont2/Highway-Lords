using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class WallTier
{
    [Tooltip("Рівень стіни, з якого починається цей візуал (напр. 1, 11, 21)")]
    public int unlockLevel;
    
    [Header("Стадії пошкодження (Спрайти)")]
    public Sprite stage1_Full;      // 100% - 75% HP
    public Sprite stage2_Damaged;   // 75% - 50% HP
    public Sprite stage3_Critical;  // 50% - 1% HP
    public Sprite stage4_Destroyed; // 0% HP (Ворота вибиті)
}

public class Wall : MonoBehaviour
{
    [Header("=== БАЛАНС СТІНИ ===")]
    public int baseHealth = 100; 
    public int hpBonusPerUpgrade = 50;
    public HealthBarSegments hpSegments;
    
    [Header("=== ЕКОНОМІКА ===")]
    public int wallLevel = 1;
    public int baseUpgradeCost = 150;    
    public float costMultiplier = 1.5f;  

    [Header("=== ВІЗУАЛ (ТИРИ ТА ПОШКОДЖЕННЯ) ===")]
    public SpriteRenderer wallSpriteRenderer;
    public List<WallTier> wallTiers; // Налаштуйте в інспекторі (напр. 5 тирів)
    private WallTier currentTier;

    [Header("=== СТАН (Read Only) ===")]
    public int maxHealth;
    public int currentHealth;
    public bool isDead = false;

    [Header("=== UI & КОМПОНЕНТИ ===")]
    public Image healthBarFill; 
    public Transform spawnPoint; 

    void Start()
    {
        RecalculateMaxHealth();
        if (currentHealth <= 0) currentHealth = maxHealth;

        gameObject.tag = "Castle"; // Залишаємо цей тег, щоб вороги йшли сюди
        isDead = false;

        UpdateVisuals();
        UpdateUI();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.castle = this; // GM все ще думає, що це замок (щоб не ламати код)
            GameManager.Instance.unitSpawnPoint = spawnPoint != null ? spawnPoint : transform;
        }

        if (hpSegments != null) hpSegments.UpdateSegments(maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth > 0 && CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.15f, 0.1f);
        
        if (GameManager.Instance != null) 
            GameManager.Instance.ShowDamage(damage, transform.position + Vector3.up * 2f);

        if (SoundManager.Instance != null && SoundManager.Instance.castleDamage != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.castleDamage);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        
        UpdateVisuals();
        UpdateUI();
    }

    public void HealMax()
    {
        isDead = false;
        currentHealth = maxHealth;
        gameObject.tag = "Castle"; 

        UpdateVisuals();
        UpdateUI();
    }

    public int GetUpgradeCost()
    {
        return Mathf.RoundToInt(baseUpgradeCost * Mathf.Pow(costMultiplier, wallLevel - 1));
    }

    public void UpgradeCastle() // Залишив назву для GameManager
    {
        wallLevel++;
        maxHealth += hpBonusPerUpgrade; 
        currentHealth = maxHealth; 

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.constructionComplete);
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.1f, 0.2f);

        UpdateVisuals();
        UpdateUI();

        if (hpSegments != null) hpSegments.UpdateSegments(maxHealth); 
        if (GameManager.Instance != null) GameManager.Instance.SaveGame();
    }

    void RecalculateMaxHealth()
    {
        maxHealth = baseHealth + ((wallLevel - 1) * hpBonusPerUpgrade);
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }

    public void LoadState(int savedLevel)
    {
        wallLevel = savedLevel;
        if (wallLevel < 1) wallLevel = 1;
        
        RecalculateMaxHealth();
        currentHealth = maxHealth; 
        
        UpdateVisuals();
        UpdateUI();

        if (hpSegments != null) hpSegments.UpdateSegments(maxHealth);
    }

    // === НОВА ЛОГІКА ВІЗУАЛУ ===
    void UpdateVisuals()
    {
        if (wallSpriteRenderer == null || wallTiers.Count == 0) return;

        // 1. Визначаємо поточний ТИР стіни (кожні 10 лвлів)
        currentTier = wallTiers[0];
        foreach (var tier in wallTiers)
        {
            if (wallLevel >= tier.unlockLevel) currentTier = tier;
        }

        // 2. Визначаємо стадію пошкодження
        float hpPercent = (float)currentHealth / maxHealth;

        if (hpPercent <= 0f) 
            wallSpriteRenderer.sprite = currentTier.stage4_Destroyed;
        else if (hpPercent <= 0.5f) 
            wallSpriteRenderer.sprite = currentTier.stage3_Critical;
        else if (hpPercent <= 0.75f) 
            wallSpriteRenderer.sprite = currentTier.stage2_Damaged;
        else 
            wallSpriteRenderer.sprite = currentTier.stage1_Full;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        
        UpdateVisuals(); // Покажемо 4-й кадр (зруйновані ворота)
        gameObject.tag = "Untagged"; // Вороги побіжать далі

        if (GameManager.Instance != null) GameManager.Instance.Defeat();
    }

    void UpdateUI()
    {
        if (healthBarFill != null && maxHealth > 0)
            healthBarFill.fillAmount = (float)currentHealth / maxHealth;
    }
}