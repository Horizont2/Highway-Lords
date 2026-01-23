using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ResourceType { Gold, Wood, Stone }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Зв'язки")]
    public Castle castle;
    public EnemySpawner spawner;
    public GameObject defeatPanel;
    public TMP_Text defeatResourcesText;

    [Header("Зони (Boundaries)")]
    public Transform leftBoundary;  
    public Transform rightBoundary; 

    [Header("Ефекти та Іконки")]
    public GameObject damagePopupPrefab;    
    public GameObject resourcePopupPrefab;  
    public Sprite goldIcon;                 
    public Sprite woodIcon;                 
    public Sprite stoneIcon;                

    [Header("Ресурси")]
    public int gold = 0;
    public int wood = 0;
    public int stone = 0;

    [Header("UI Текст")]
    public TMP_Text goldText;
    public TMP_Text woodText;
    public TMP_Text stoneText;
    public TMP_Text waveText;
    
    // === ЗМІНИ В UI ===
    public TMP_Text knightPriceText;   // Текст ціни найму
    public TMP_Text limitText;         // Текст ліміту (3/5)
    // ==================

    public TMP_Text upgradePriceText;
    public TMP_Text towerPriceText;

    [Header("Баланс: Війська")] // Змінив назву заголовка
    public GameObject knightPrefab;
    public GameObject archerPrefab; // <--- НОВЕ: Префаб лучника
    public Transform unitSpawnPoint; // (Колишній knightSpawnPoint)
    
    public int unitCost = 50; // (Колишній knightCost)
    public int damageLevel = 1;         
    public int knightUpgradeCost = 100;

    [Header("Ліміт військ")]
    public int maxUnits = 5;          // (Колишній maxKnights)
    public int currentUnits = 0;      // (Колишній currentKnights)

    [Header("Баланс: Вежа")]
    public int globalArrowDamage = 20;
    public int towerWoodCost = 50;
    public int towerStoneCost = 20;

    [Header("Баланс: Хвилі")]
    public int currentWave = 1;
    public float difficultyMultiplier = 1.09f; 
    public int baseEnemyHealth = 40;
    public int healthRegenPerWave = 20; 
    public int baseGoldReward = 15; 
    public float goldMultiplier = 1.05f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateUI();
        if (defeatPanel) defeatPanel.SetActive(false);
    }

    // === ВІЗУАЛІЗАЦІЯ ===
    public void ShowDamage(int damageAmount, Vector3 position)
    {
        if (damagePopupPrefab != null)
        {
            GameObject popup = Instantiate(damagePopupPrefab, position, Quaternion.identity);
            popup.GetComponent<DamagePopup>().Setup(damageAmount);
        }
    }

    public void ShowResourcePopup(ResourceType type, int amount, Vector3 position)
    {
        if (resourcePopupPrefab == null) return;

        Sprite icon = null;
        Color color = Color.white;

        switch (type)
        {
            case ResourceType.Gold: icon = goldIcon; color = Color.yellow; break;
            case ResourceType.Wood: icon = woodIcon; color = new Color(0.6f, 0.3f, 0f); break;
            case ResourceType.Stone: icon = stoneIcon; color = Color.gray; break;
        }

        Vector3 spawnPos = position + new Vector3(0, 0.5f, 0);
        GameObject popup = Instantiate(resourcePopupPrefab, spawnPos, Quaternion.identity);
        popup.GetComponent<DamagePopup>().SetupResource(icon, amount, color);
    }

    // === ЕКОНОМІКА ===
    public void AddResource(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Gold: gold += amount; break;
            case ResourceType.Wood: wood += amount; break;
            case ResourceType.Stone: stone += amount; break;
        }
        UpdateUI();
        
        if (type == ResourceType.Gold && amount > 0 && Time.time > 1f)
        {
             if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.coinPickup);
        }
    }

    public int GetDifficultyHealth()
    {
        return Mathf.RoundToInt(baseEnemyHealth * Mathf.Pow(difficultyMultiplier, currentWave));
    }

    public int GetGoldReward()
    {
        return Mathf.RoundToInt(baseGoldReward * Mathf.Pow(goldMultiplier, currentWave));
    }

    // === НАЙМ ВІЙСЬК ( РАНДОМ ) ===
    // Прив'яжи цей метод до кнопки замість старого BuyKnight
    public void HireRandomUnit()
    {
        if (currentUnits >= maxUnits)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
            return;
        }

        if (gold >= unitCost)
        {
            gold -= unitCost;
            unitCost = Mathf.RoundToInt(unitCost * 1.10f); // Збільшення ціни
            
            // --- РАНДОМ: 50% Лицар, 50% Лучник ---
            GameObject newUnit;
            if (Random.Range(0, 2) == 0)
            {
                if (knightPrefab) 
                {
                    newUnit = Instantiate(knightPrefab, unitSpawnPoint.position, Quaternion.identity);
                    Debug.Log("Випав: Лицар");
                }
            }
            else
            {
                if (archerPrefab) 
                {
                    newUnit = Instantiate(archerPrefab, unitSpawnPoint.position, Quaternion.identity);
                    Debug.Log("Випав: Лучник");
                }
            }

            // Збільшуємо лічильник (ВАЖЛИВО: щоб ліміт працював)
            currentUnits++;

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.buyItem);
            
            UpdateUI();
        }
        else
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    // === ПОЛІПШЕННЯ ===
    public void BuyUpgrade() 
    {
        if (gold >= knightUpgradeCost)
        {
            gold -= knightUpgradeCost;
            damageLevel++; 
            knightUpgradeCost = Mathf.RoundToInt(knightUpgradeCost * 1.5f); 

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.buyItem);

            UpdateUI(); 
        }
        else
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    public void UpgradeDamage()
    {
        if (wood >= towerWoodCost && stone >= towerStoneCost)
        {
            wood -= towerWoodCost;
            stone -= towerStoneCost;
            globalArrowDamage += 15; 
            towerWoodCost += 25;
            towerStoneCost += 10;
            UpdateUI(); 

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.buyItem);
        }
        else
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    // === GAME LOOP ===
    public void Defeat()
    {
        Time.timeScale = 0;
        if (defeatPanel)
        {
            defeatPanel.SetActive(true);
            if (defeatResourcesText)
                defeatResourcesText.text = $"Defeat! Kept:\nGold: {gold} | Wood: {wood}";
        }
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.defeat);
    }

    public void OnRetryButton()
    {
        Time.timeScale = 1;
        if (defeatPanel) defeatPanel.SetActive(false);
        spawner.ClearEnemies();
        castle.HealMax();
        
        // 1. Видаляємо Лицарів
        var knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        foreach (var k in knights) Destroy(k.gameObject);

        // 2. Видаляємо Лучників (НОВЕ)
        var archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        foreach (var a in archers) Destroy(a.gameObject);
        
        currentUnits = 0; 
        
        spawner.RestartWave();
        UpdateUI();
    }

    public void NextWave()
    {
        currentWave++;
        if (castle != null) castle.Heal(healthRegenPerWave);
        UpdateUI();
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.waveStart);
    }

    // === ОНОВЛЕННЯ UI ===
    public void UpdateUI()
    {
        if (goldText) goldText.text = gold.ToString();
        if (woodText) woodText.text = wood.ToString();
        if (stoneText) stoneText.text = stone.ToString();
        if (waveText) waveText.text = "Wave " + currentWave;
        
        // Оновлення тексту ЛІМІТУ
        if (limitText != null)
        {
            limitText.text = $"{currentUnits} / {maxUnits}";

            if (knightPriceText) 
                knightPriceText.text = $"Hire Unit\n{unitCost} G";
        }
        else 
        {
            if (knightPriceText) 
                knightPriceText.text = $"Hire Unit\n{unitCost} G\n({currentUnits}/{maxUnits})";
        }

        if (upgradePriceText) 
            upgradePriceText.text = $"Upgrade Tech\n{knightUpgradeCost} G (Lvl {damageLevel})";
        
        if (towerPriceText) 
            towerPriceText.text = $"Tower DMG\n{towerWoodCost} W / {towerStoneCost} S";
    }
}