using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class WallTier
{
    public int unlockLevel;
    public Sprite stage1_Full;      
    public Sprite stage2_Damaged;   
    public Sprite stage3_Critical;  
    public Sprite stage4_Destroyed; 
}

public class Wall : MonoBehaviour
{
    public int baseHealth = 100; 
    public int hpBonusPerUpgrade = 50;
    public HealthBarSegments hpSegments;
    
    public int wallLevel = 1;
    public int baseUpgradeCost = 150;    
    public float costMultiplier = 1.5f;  

    public SpriteRenderer wallSpriteRenderer;
    public List<WallTier> wallTiers; 
    private WallTier currentTier;

    public int maxHealth;
    public int currentHealth;
    public bool isDead = false;

    public Image healthBarFill; 
    public Transform spawnPoint; 

    private float regenTimer = 0f;

    void Start()
    {
        RecalculateMaxHealth();
        if (currentHealth <= 0) currentHealth = maxHealth;

        gameObject.tag = "Castle"; 
        isDead = false;

        UpdateVisuals();
        UpdateUI();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.castle = this; 
            GameManager.Instance.unitSpawnPoint = spawnPoint != null ? spawnPoint : transform;
        }

        if (hpSegments != null) hpSegments.UpdateSegments(maxHealth);
    }

    void Update()
    {
        if (isDead || GameManager.Instance == null) return;

        // ПАСИВНА РЕГЕНЕРАЦІЯ УВІМКНЕНА!
        int masonryLvl = GameManager.Instance.metaMendingMasonry;
        
        if (masonryLvl > 0 && currentHealth < maxHealth)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= 1f)
            {
                regenTimer = 0f;
                int healAmount = Mathf.RoundToInt((maxHealth * 0.01f) * masonryLvl);
                if (healAmount < 1) healAmount = 1;

                currentHealth += healAmount;
                if (currentHealth > maxHealth) currentHealth = maxHealth;

                UpdateVisuals();
                UpdateUI();
            }
        }
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

    public void UpgradeCastle() 
    {
        wallLevel++;
        RecalculateMaxHealth(); 
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
        
        if (GameManager.Instance != null)
        {
            // БОНУС ДО ХП ВІД КРИСТАЛІВ УВІМКНЕНО!
            maxHealth += (GameManager.Instance.metaFortifiedWalls * 200);
        }

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

    void UpdateVisuals()
    {
        if (wallSpriteRenderer == null || wallTiers.Count == 0) return;

        currentTier = wallTiers[0];
        foreach (var tier in wallTiers)
        {
            if (wallLevel >= tier.unlockLevel) currentTier = tier;
        }

        float hpPercent = (float)currentHealth / maxHealth;

        if (hpPercent <= 0f) wallSpriteRenderer.sprite = currentTier.stage4_Destroyed;
        else if (hpPercent <= 0.5f) wallSpriteRenderer.sprite = currentTier.stage3_Critical;
        else if (hpPercent <= 0.75f) wallSpriteRenderer.sprite = currentTier.stage2_Damaged;
        else wallSpriteRenderer.sprite = currentTier.stage1_Full;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        UpdateVisuals(); 
        gameObject.tag = "Untagged"; 
        if (GameManager.Instance != null) GameManager.Instance.Defeat();
    }

    void UpdateUI()
    {
        if (healthBarFill != null && maxHealth > 0)
            healthBarFill.fillAmount = (float)currentHealth / maxHealth;
    }
}