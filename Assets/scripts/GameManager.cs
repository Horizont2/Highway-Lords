using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; 
using System.Collections.Generic;

// ПРИМІТКА: Класи GameSaveData та UnitSaveData знаходяться у вашому файлі GameSaveData.cs

public enum ResourceType { Gold, Wood, Stone }

public enum UnitCategory 
{ 
    Standard,   // Лицарі, Гвардійці
    Ranged,     // Лучники
    Cavalry,    // Кіннота
    Spearman,   // Списоносці
    Building    // Замок, Стіни
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ==========================================
    // === СИСТЕМА ПРИЦІЛЮВАННЯ ===
    // ==========================================
    [Header("Система прицілювання")]
    public Transform manualTarget; 
    public TargetIndicator targetIndicator; 
    
    public void SetManualTarget(Transform target)
    {
        manualTarget = target;
        if (targetIndicator != null) targetIndicator.Show(target);
    }

    [Header("=== БАЛАНС (НОВИЙ) ===")]
    public float enemyHpGrowth = 1.12f;     
    public float goldRewardGrowth = 1.08f;  
    public float upgradeCostGrowth = 1.20f; 
    
    [HideInInspector] public float globalDamageMultiplier = 1.0f;

    // === МАТРИЦЯ УРОНУ ===
    public static float GetDamageMultiplier(UnitCategory attacker, UnitCategory defender)
    {
        if (attacker == UnitCategory.Spearman && defender == UnitCategory.Cavalry) return 2.0f;
        if (attacker == UnitCategory.Cavalry && (defender == UnitCategory.Standard || defender == UnitCategory.Ranged)) return 1.5f;
        if (attacker == UnitCategory.Cavalry && defender == UnitCategory.Spearman) return 0.5f;
        return 1.0f;
    }

    public static void CreateDamagePopup(Vector3 position, int damageAmount, bool isCritical = false)
    {
        if (Instance != null && Instance.damagePopupPrefab != null)
        {
            GameObject popupObj = Instantiate(Instance.damagePopupPrefab, position, Quaternion.identity);
            DamagePopup popup = popupObj.GetComponent<DamagePopup>();
            if (popup != null) popup.Setup(damageAmount, isCritical);
        }
    }

    [Header("=== НАЛАШТУВАННЯ АВТО-СТАРТУ ===")]
    public float timeBetweenWaves = 5.0f; 
    public bool autoStartWaves = true;    

    [Header("=== UI: СИСТЕМА БУДІВНИЦТВА ===")]
    public GameObject constructionPanel;    
    public GameObject barracksUpgradePanel; 
    
    public Button hammerButton;             
    public Button barracksIconButton;       
    
    public Button buildBarracksBtnInMenu;   
    public TMP_Text barracksBuildPriceText; 
    
    public Button upgradeLimitButton;       
    public TMP_Text upgradeLimitPriceText;  

    public Button unlockSpearmanButton;     
    public TMP_Text unlockSpearmanPriceText;

    [Header("UI: Статистика (НОВЕ)")]
    public TMP_Text estimatedIncomeText; // Сюди перетягніть текст для відображення доходу

    [Header("Налаштування Казарми")]
    public GameObject barracksPrefab;       
    public Transform barracksSpawnPoint;    
    
    public int barracksLevel = 0;           
    public int barracksBaseCap = 5;         
    public int slotsPerLevel = 3;           

    public int barracksCostGold = 200;
    public int barracksCostWood = 100;
    
    private bool isBarracksBuilt = false;       
    private GameObject currentBarracksObject;   

    [Header("=== ШАХТА (GOLD MINE) ===")]
    public GameObject minePrefab;           
    public Transform mineSpawnPoint;        
    public Button buildMineButton;          
    public TMP_Text minePriceText;          
    
    public int mineLevel = 0; 
    
    public int mineBuildCostWood = 150;
    public int mineBuildCostStone = 50;

    public int mineUpgradeBaseWood = 200;
    public int mineUpgradeBaseStone = 100;
    
    private bool isMineBuilt = false;
    private GameObject currentMineObject;

    [Header("Захисні споруди (Spikes)")]
    public GameObject spikesPrefab;
    public Transform spikesSpawnPoint; 
    public int spikesWoodCost = 100;
    public Button buildSpikesButton;   
    public TMP_Text spikesPriceText;
    
    [HideInInspector] public Spikes currentSpikes; 

    [Header("UI: Магазин (Кузня)")]
    public GameObject shopPanel;
    public GameObject spearmanForgeRow; 
    
    public Button upgradeKnightButton;     
    public Button upgradeArcherButton;
    public Button upgradeSpearmanButton; 
    
    public TMP_Text knightLevelText; 
    public TMP_Text knightPriceText; 
    public TMP_Text archerLevelText;
    public TMP_Text archerPriceText; 
    public TMP_Text spearmanLevelText;   
    public TMP_Text spearmanPriceText;   

    [Header("UI: Головний екран")]
    public Button hireKnightButton; 
    public Button hireArcherButton; 
    public Button hireSpearmanButton;    
    
    public Button openShopButton;         
    public Button towerButton;      

    [Header("UI: Прогрес Хвилі")]
    public Slider waveTimerBar; 
    private int totalEnemiesInWave = 1; 
    private int enemiesKilledInWave = 0; 
    
    [Header("Управління Хвилею")]
    public Button nextWaveButton;   
    public int enemiesAlive = 0;    

    public GameObject defeatPanel;
    public TMP_Text defeatResourcesText;

    [Header("Зв'язки")]
    public Castle castle; 
    public Transform towerTransform;       
    public EnemySpawner spawner; 

    // === СПРАЙТИ ДЛЯ КНОПОК ===
    [Header("Спрайти Кнопок")]
    public Sprite buildButtonSprite;   // Сюди перетягни спрайт "Build"
    public Sprite upgradeButtonSprite; // Сюди перетягни спрайт "Upgrade"

    [Header("Зони")]
    public Transform leftBoundary;
    public Transform rightBoundary;
    public Transform engagementLine;

    [Header("Ефекти")]
    public GameObject upgradeEffectPrefab;
    public GameObject damagePopupPrefab;
    public GameObject resourcePopupPrefab;
    public Sprite goldIcon;
    public Sprite woodIcon;
    public Sprite stoneIcon;

    [Header("Ресурси")]
    public int gold = 0;
    public int wood = 0;
    public int stone = 0;

    [Header("Тексти HUD")]
    public TMP_Text goldText;
    public TMP_Text woodText;
    public TMP_Text stoneText;
    public TMP_Text waveText;
    public TMP_Text hirePriceText;   
    public TMP_Text limitText;         
    public TMP_Text towerPriceText;    

    [Header("Баланс: Війська")]
    public GameObject knightPrefab;
    public GameObject archerPrefab; 
    public GameObject spearmanPrefab; 
    public Transform unitSpawnPoint;
    
    [Header("Ціни на найм")]
    public int knightFixedCost = 50; 
    public int archerFixedCost = 75;
    public int spearmanFixedCost = 60; 
    
    [Header("Рівні Технологій")]
    public int knightLevel = 1;      
    public int archerLevel = 1;      
    public int spearmanLevel = 1;      
    
    public int knightUpgradeCost = 100;
    public int archerUpgradeCost = 120; 
    public int spearmanUpgradeCost = 110; 

    [Header("Розблокування (Unlock)")]
    public bool isSpearmanUnlocked = false; 
    public int spearmanUnlockCost = 500;    

    [Header("Ліміт військ")]
    public int maxUnits = 5;          
    public int currentUnits = 0;      
    [HideInInspector] public bool isResettingUnits = false;

    [Header("Баланс: Вежа")]
    public int towerLevel = 1; 
    public int towerWoodCost = 50;
    public int towerStoneCost = 20;

    [Header("Баланс: Хвилі")]
    public int currentWave = 1;
    public int baseGoldReward = 15; 
    public int baseEnemyHealth = 50; 

    private bool isWaveInProgress = false;

    void Awake() 
    { 
        if (Instance == null) Instance = this; 
        else Destroy(gameObject); 

        LoadGame();
    }

    void Start()
    {
        // === АВТОМАТИЧНЕ НАЛАШТУВАННЯ КНОПОК ===
        SetupAllButtons(); 
        // ======================================

        RecalculateUnits();
        
        enemiesAlive = 0;
        UpdateUI();
        
        if (defeatPanel) defeatPanel.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false); 
        if (constructionPanel) constructionPanel.SetActive(false);
        if (barracksUpgradePanel) barracksUpgradePanel.SetActive(false);
        
        if (waveTimerBar != null)
        {
            waveTimerBar.gameObject.SetActive(true);
            waveTimerBar.maxValue = 1f;
            waveTimerBar.value = 1f;
        }

        UpdateBarracksStateUI();

        if (autoStartWaves)
        {
            StartCoroutine(AutoStartNextWave(3.0f));
        }

        StartCoroutine(WaveWatchdog());
    }

    IEnumerator WaveWatchdog()
    {
        while (true)
        {
            yield return new WaitForSeconds(2.0f);
            if (isWaveInProgress && enemiesAlive > 0)
            {
                GameObject[] realEnemies = GameObject.FindGameObjectsWithTag("Enemy");
                if (realEnemies.Length == 0)
                {
                    Debug.LogWarning("Watchdog: Forced wave end.");
                    enemiesAlive = 0;
                    UpdateUI();
                    isWaveInProgress = false;
                    
                    // Хвиля завершена через Watchdog
                    if (spawner != null) spawner.StopSpawning();

                    if (autoStartWaves) StartCoroutine(AutoStartNextWave(timeBetweenWaves));
                }
                else if (enemiesAlive != realEnemies.Length)
                {
                    enemiesAlive = realEnemies.Length;
                    UpdateUI();
                }
            }
        }
    }

    void RecalculateUnits()
    {
        var knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        var archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        var spearmen = FindObjectsByType<Spearman>(FindObjectsSortMode.None); 
        currentUnits = knights.Length + archers.Length + spearmen.Length;
    }

    // === БАЛАНС ===
    public int GetDifficultyHealth()
    {
        return Mathf.RoundToInt(baseEnemyHealth * Mathf.Pow(enemyHpGrowth, currentWave - 1));
    }

    public int GetGoldReward() 
    { 
        return Mathf.RoundToInt(baseGoldReward * Mathf.Pow(goldRewardGrowth, currentWave)); 
    }

    public int GetKnightDamage()
    {
        int baseDmg = 10 + ((knightLevel - 1) * 5);
        return Mathf.RoundToInt(baseDmg * globalDamageMultiplier);
    }

    public int GetArcherDamage()
    {
        int baseDmg = 8 + ((archerLevel - 1) * 3);
        return Mathf.RoundToInt(baseDmg * globalDamageMultiplier);
    }

    public int GetSpearmanDamage()
    {
        int baseDmg = 12 + ((spearmanLevel - 1) * 6);
        return Mathf.RoundToInt(baseDmg * globalDamageMultiplier);
    }

    public int GetTowerDamage()
    {
        int baseDmg = 25 + ((towerLevel - 1) * 8);
        return Mathf.RoundToInt(baseDmg * globalDamageMultiplier);
    }

    public int GetBarracksCapLimit()
    {
        if (barracksLevel == 0) return 0;
        return barracksBaseCap + ((barracksLevel - 1) * slotsPerLevel);
    }

    public int GetBarracksBuildingUpgradeCost(bool isGold)
    {
        float multiplier = Mathf.Pow(1.5f, barracksLevel); 
        int cost = isGold ? barracksCostGold : barracksCostWood;
        return Mathf.RoundToInt(cost * multiplier);
    }

    public int GetMineUpgradeCost(bool isWood)
    {
        if (mineLevel == 0) return isWood ? mineBuildCostWood : mineBuildCostStone;
        else
        {
            float multiplier = Mathf.Pow(1.5f, mineLevel - 1);
            int baseCost = isWood ? mineUpgradeBaseWood : mineUpgradeBaseStone;
            return Mathf.RoundToInt(baseCost * multiplier);
        }
    }

    // === ЛОГІКА ХВИЛІ ===
    public void InitWaveProgress(int totalEnemies)
    {
        totalEnemiesInWave = totalEnemies;
        enemiesKilledInWave = 0;

        if (waveTimerBar != null)
        {
            waveTimerBar.gameObject.SetActive(true);
            waveTimerBar.maxValue = 1f;
            waveTimerBar.value = 1f;
        }
    }

    public void RegisterEnemy() 
    { 
        enemiesAlive++; 
        isWaveInProgress = true;
        UpdateUI(); 
    }
    
    public void UnregisterEnemy() 
    { 
        enemiesAlive--; 
        enemiesKilledInWave++; 
        
        if (waveTimerBar != null && totalEnemiesInWave > 0)
        {
            float progress = 1f - ((float)enemiesKilledInWave / totalEnemiesInWave);
            waveTimerBar.value = progress;
        }

        if(enemiesAlive < 0) enemiesAlive = 0; 
        UpdateUI(); 

        if (manualTarget != null && !manualTarget.gameObject.activeInHierarchy)
        {
             manualTarget = null;
        }

        if (enemiesAlive == 0 && isWaveInProgress)
        {
            isWaveInProgress = false;
            
            // Ховаємо панель ворогів, бо хвиля скінчилась
            if (spawner != null) spawner.StopSpawning();

            if (waveTimerBar != null) waveTimerBar.value = 1f;

            if (autoStartWaves)
            {
                StartCoroutine(AutoStartNextWave(timeBetweenWaves));
            }
        }
    }

    IEnumerator AutoStartNextWave(float delay)
    {
        yield return new WaitForSeconds(delay);
        NextWave();
    }

    public void NextWave()
    {
        if (enemiesAlive > 0) return;

        if (!isWaveInProgress && enemiesAlive == 0 && Time.timeSinceLevelLoad > 5f)
        {
             currentWave++;
        }

        if (castle != null) castle.HealMax(); 
        
        manualTarget = null;
        if(targetIndicator) targetIndicator.Hide();

        SaveGame(); 
        
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.waveStart, 0.4f);
        
        // ВАЖЛИВО: Спочатку запускаємо хвилю (спавнер готує список), потім оновлюємо UI
        if (spawner != null) spawner.StartWave(currentWave);
        UpdateUI();
        
        isWaveInProgress = true;
    }

    // === БУДІВНИЦТВО ===
    public void ToggleConstructionMenu()
    {
        if (constructionPanel)
        {
            bool isActive = !constructionPanel.activeSelf;
            constructionPanel.SetActive(isActive);
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);

            if (isActive) 
            { 
                barracksUpgradePanel.SetActive(false); 
                shopPanel.SetActive(false); 
                UpdateBarracksStateUI(); 
            }
        }
    }

    public void BuildOrUpgradeBarracks()
    {
        int costG = GetBarracksBuildingUpgradeCost(true);
        int costW = GetBarracksBuildingUpgradeCost(false);

        if (gold >= costG && wood >= costW)
        {
            gold -= costG; 
            wood -= costW;
            
            if (barracksLevel == 0)
            {
                SpawnBarracksObject();
                barracksLevel = 1;
                maxUnits = barracksBaseCap; 
                isBarracksBuilt = true;
            }
            else
            {
                barracksLevel++;
                if (upgradeEffectPrefab != null && currentBarracksObject != null)
                    Instantiate(upgradeEffectPrefab, currentBarracksObject.transform.position, Quaternion.identity);
            }
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.constructionSound); 
            
            SaveGame(); 
            UpdateUI(); 
            UpdateBarracksStateUI();
        }
        else 
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    public void BuildBarracks() { BuildOrUpgradeBarracks(); }

    public void BuildSpikes()
    {
        if (currentSpikes != null) return; 

        if (wood >= spikesWoodCost)
        {
            wood -= spikesWoodCost;
            
            if (spikesPrefab && spikesSpawnPoint)
            {
                Instantiate(spikesPrefab, spikesSpawnPoint.position, Quaternion.identity);
            }
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.constructionSound);
            
            SaveGame();
            UpdateUI();
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    void SpawnBarracksObject() 
    { 
        if (barracksPrefab && barracksSpawnPoint) 
        { 
            if (currentBarracksObject != null) Destroy(currentBarracksObject); 
            currentBarracksObject = Instantiate(barracksPrefab, barracksSpawnPoint.position, Quaternion.identity); 
        } 
    }
    
    public void BuildOrUpgradeMine()
    {
        int costW = GetMineUpgradeCost(true);
        int costS = GetMineUpgradeCost(false);

        if (wood >= costW && stone >= costS)
        {
            wood -= costW;
            stone -= costS;

            if (mineLevel == 0)
            {
                isMineBuilt = true;
                mineLevel = 1;
                SpawnMineObject();
            }
            else
            {
                mineLevel++;
                if (upgradeEffectPrefab != null && currentMineObject != null)
                    Instantiate(upgradeEffectPrefab, currentMineObject.transform.position, Quaternion.identity);
            }

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.constructionSound);
            
            SaveGame();
            UpdateUI();
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    public void BuildMine() { BuildOrUpgradeMine(); }

    void SpawnMineObject()
    {
        if (minePrefab && mineSpawnPoint)
        {
            if (currentMineObject != null) Destroy(currentMineObject);
            currentMineObject = Instantiate(minePrefab, mineSpawnPoint.position, Quaternion.identity);
        }
    }

    void UpdateBarracksStateUI()
    {
        if (barracksIconButton) barracksIconButton.gameObject.SetActive(barracksLevel > 0); 
        
        if (buildBarracksBtnInMenu) 
        {
            int costG = GetBarracksBuildingUpgradeCost(true);
            int costW = GetBarracksBuildingUpgradeCost(false);
            
            bool canAfford = gold >= costG && wood >= costW;
            UpdateButtonState(buildBarracksBtnInMenu, canAfford);

            // ЗМІНА СПРАЙТА КНОПКИ (BUILD / UPGRADE)
            Image btnImg = buildBarracksBtnInMenu.GetComponent<Image>();
            if (btnImg != null)
            {
                if (barracksLevel == 0)
                    btnImg.sprite = buildButtonSprite;
                else
                    btnImg.sprite = upgradeButtonSprite;
            }

            if (barracksLevel == 0)
            {
                if (barracksBuildPriceText) barracksBuildPriceText.text = $"{costG} G / {costW} W";
            }
            else
            {
                if (barracksBuildPriceText) barracksBuildPriceText.text = $"{costG} G / {costW} W\nMax Cap: {GetBarracksCapLimit() + slotsPerLevel}";
            }
        }
    }

    public void ToggleBarracksUpgradeMenu()
    {
        if (barracksUpgradePanel) 
        {
            bool isActive = !barracksUpgradePanel.activeSelf;
            barracksUpgradePanel.SetActive(isActive);
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);
            
            if (isActive) 
            { 
                UpdateUpgradeMenuPrice(); 
                UpdateUI(); 
                constructionPanel.SetActive(false); 
                shopPanel.SetActive(false); 
            }
        }
    }

    public void BuyUnitLimitUpgrade()
    {
        int currentCap = GetBarracksCapLimit();
        if (maxUnits >= currentCap)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
            return; 
        }

        int cost = GetSlotUpgradeCost();
        if (gold >= cost) 
        {
            gold -= cost; 
            maxUnits++; 
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.constructionSound);
            
            SaveGame(); 
            UpdateUI(); 
            UpdateUpgradeMenuPrice();
        }
        else 
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    int GetSlotUpgradeCost() 
    { 
        int u = maxUnits - 5; 
        if (u < 0) u = 0; 
        return 100 + (u * 50); 
    }
    
    void UpdateUpgradeMenuPrice() 
    { 
        if (upgradeLimitPriceText)
        {
            int currentCap = GetBarracksCapLimit();
            if (maxUnits >= currentCap)
            {
                upgradeLimitPriceText.text = "MAX"; 
            }
            else
            {
                upgradeLimitPriceText.text = $"Limit +1   ({GetSlotUpgradeCost()} G)"; 
            }
        }

        if (unlockSpearmanPriceText)
        {
            unlockSpearmanPriceText.text = $"Unlock Spear ({spearmanUnlockCost} G)";
        }
    }

    public void UnlockSpearman()
    {
        if (isSpearmanUnlocked) return;

        if (gold >= spearmanUnlockCost)
        {
            gold -= spearmanUnlockCost;
            isSpearmanUnlocked = true;
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.constructionSound);
            
            SaveGame();
            UpdateUI(); 
            UpdateUpgradeMenuPrice();
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    public void ToggleShop() 
    { 
        if (shopPanel)
        {
            bool isActive = !shopPanel.activeSelf;
            shopPanel.SetActive(isActive);
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);
            
            if(isActive) 
            { 
                constructionPanel.SetActive(false); 
                barracksUpgradePanel.SetActive(false); 
            }
        }
    }
    
    public void UpgradeKnights() 
    { 
        if(gold >= knightUpgradeCost)
        {
            gold -= knightUpgradeCost; 
            knightLevel++; 
            knightUpgradeCost = (int)(knightUpgradeCost * upgradeCostGrowth); 
            SaveGame(); 
            UpdateUI();
        } 
    }
    
    public void UpgradeArchers() 
    { 
        if(gold >= archerUpgradeCost)
        {
            gold -= archerUpgradeCost; 
            archerLevel++; 
            archerUpgradeCost = (int)(archerUpgradeCost * upgradeCostGrowth); 
            SaveGame(); 
            UpdateUI();
        } 
    }

    public void UpgradeSpearman() 
    { 
        if(gold >= spearmanUpgradeCost) 
        { 
            gold -= spearmanUpgradeCost; 
            spearmanLevel++; 
            spearmanUpgradeCost = (int)(spearmanUpgradeCost * upgradeCostGrowth); 
            SaveGame(); 
            UpdateUI(); 
        } 
    }
    
    public void HireKnight() { TryHireUnit(knightPrefab, knightFixedCost); }
    public void HireArcher() { TryHireUnit(archerPrefab, archerFixedCost); }
    public void HireSpearman() { TryHireUnit(spearmanPrefab, spearmanFixedCost); } 
    
    private void TryHireUnit(GameObject prefab, int cost) 
    { 
        if(currentUnits >= maxUnits || gold < cost) return;
        
        gold -= cost; 
        currentUnits++; 
        UpdateUI();

        Instantiate(prefab, unitSpawnPoint.position, Quaternion.identity); 
        
        SaveGame(); 
    }
    
    public void UpgradeDamage() 
    { 
        if(wood >= towerWoodCost && stone >= towerStoneCost)
        { 
            wood -= towerWoodCost; 
            stone -= towerStoneCost; 
            
            towerLevel++; 
            
            towerWoodCost = (int)(towerWoodCost * upgradeCostGrowth); 
            towerStoneCost = (int)(towerStoneCost * upgradeCostGrowth); 
            SaveGame(); 
            UpdateUI();
        } 
    }

    // === ЕФЕКТИ ===
    public void ShowDamage(int damageAmount, Vector3 position) 
    { 
        CreateDamagePopup(position, damageAmount, false);
    }
    
    public void ShowResourcePopup(ResourceType type, int amount, Vector3 position) 
    { 
        if (resourcePopupPrefab == null) return; 
        
        Sprite icon = null; 
        Color color = Color.white; 
        
        switch (type) 
        { 
            case ResourceType.Gold: icon = goldIcon; color=Color.yellow; break; 
            case ResourceType.Wood: icon=woodIcon; color=new Color(0.6f,0.3f,0f); break; 
            case ResourceType.Stone: icon=stoneIcon; color=Color.gray; break; 
        } 
        
        GameObject popup = Instantiate(resourcePopupPrefab, position + Vector3.up, Quaternion.identity); 
        popup.GetComponent<DamagePopup>().SetupResource(icon, amount, color); 
    }
    
    public void AddResource(ResourceType type, int amount) 
    { 
        switch (type) 
        { 
            case ResourceType.Gold: gold+=amount; break; 
            case ResourceType.Wood: wood+=amount; break; 
            case ResourceType.Stone: stone+=amount; break; 
        } 
        
        if(amount > 0) SaveGame(); 
        
        UpdateUI(); 
        
        if (type == ResourceType.Gold && amount > 0 && Time.time > 1f && SoundManager.Instance != null) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.coinPickup); 
    }

    public void Defeat() 
    { 
        Time.timeScale = 0; 
        if( $args[0].Value + "`n        ClearProjectiles();`n" 
        SaveGame(); 
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.defeat); 
    }
    
    public void OnRetryButton() 
    { 
        Time.timeScale = 1; 
        if(defeatPanel) defeatPanel.SetActive(false); 
        
        // Зупиняємо спавн і ховаємо панель перед очищенням
        if (spawner != null) spawner.StopSpawning(); 
        if (spawner != null) spawner.ClearEnemies(); 
        
        if (castle == null) castle = FindFirstObjectByType<Castle>();
        if (castle != null) castle.HealMax(); 

        if (currentSpikes != null) Destroy(currentSpikes.gameObject);
        currentSpikes = null;

        manualTarget = null;
        if(targetIndicator) targetIndicator.Hide();

        // === Під час ресету не рахуємо OnDestroy() як смерть юніта ===
        isResettingUnits = true;
        
        var units = FindObjectsByType<Knight>(FindObjectsSortMode.None); 
        foreach(var u in units) Destroy(u.gameObject); 
        
        var archs = FindObjectsByType<Archer>(FindObjectsSortMode.None); 
        foreach(var a in archs) Destroy(a.gameObject); 
        
        var spearmen = FindObjectsByType<Spearman>(FindObjectsSortMode.None); 
        foreach(var s in spearmen) Destroy(s.gameObject); 
        
        if (currentMineObject != null) Destroy(currentMineObject); 
        if (isMineBuilt) SpawnMineObject(); 

        currentUnits = 0; 
        enemiesAlive = 0; 
        isResettingUnits = false;
        
        if (waveTimerBar != null)
        {
            waveTimerBar.gameObject.SetActive(true);
            waveTimerBar.value = 1f;
        }

        // Запускаємо нову хвилю (це знову покаже панель)
        if (spawner != null) spawner.StartWave(currentWave); 
        
        UpdateUI(); 
        UpdateBarracksStateUI(); 
    }

    // === SAVE / LOAD ===
    public void SaveGame()
    {
        PlayerPrefs.SetInt("SavedGold", gold);
        PlayerPrefs.SetInt("SavedWood", wood);
        PlayerPrefs.SetInt("SavedStone", stone);
        PlayerPrefs.SetInt("SavedWave", currentWave);
        
        PlayerPrefs.SetInt("SavedKnightLevel", knightLevel);
        PlayerPrefs.SetInt("SavedArcherLevel", archerLevel);
        PlayerPrefs.SetInt("SavedSpearmanLevel", spearmanLevel); 
        
        PlayerPrefs.SetInt("SavedKnightCost", knightUpgradeCost);
        PlayerPrefs.SetInt("SavedArcherCost", archerUpgradeCost);
        PlayerPrefs.SetInt("SavedSpearmanCost", spearmanUpgradeCost); 
        
        PlayerPrefs.SetInt("SavedMaxUnits", maxUnits);
        
        PlayerPrefs.SetInt("SavedBarracksLevel", barracksLevel);
        PlayerPrefs.SetInt("SavedSpearmanUnlocked", isSpearmanUnlocked ? 1 : 0);
        PlayerPrefs.SetInt("SavedMineBuilt", isMineBuilt ? 1 : 0);
        PlayerPrefs.SetInt("SavedMineLevel", mineLevel);

        if (castle != null) PlayerPrefs.SetInt("SavedCastleLevel", castle.castleLevel);
        
        PlayerPrefs.SetInt("SavedTowerLevel", towerLevel); 
        PlayerPrefs.SetInt("SavedTowerWoodCost", towerWoodCost);
        PlayerPrefs.SetInt("SavedTowerStoneCost", towerStoneCost);

        SaveUnits();

        PlayerPrefs.Save();
    }

    void SaveUnits()
    {
        GameSaveData data = new GameSaveData();

        Knight[] knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        foreach (var k in knights)
        {
            if (k.CompareTag("Untagged")) continue;
            UnitSaveData u = new UnitSaveData();
            u.unitType = "Knight";
            u.posX = k.transform.position.x;
            u.posY = k.transform.position.y;
            u.currentHealth = k.currentHealth; 
            data.units.Add(u);
        }

        Archer[] archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        foreach (var a in archers)
        {
            if (a.CompareTag("Untagged")) continue;
            UnitSaveData u = new UnitSaveData();
            u.unitType = "Archer";
            u.posX = a.transform.position.x;
            u.posY = a.transform.position.y;
            u.currentHealth = a.currentHealth; 
            data.units.Add(u);
        }

        Spearman[] spearmen = FindObjectsByType<Spearman>(FindObjectsSortMode.None);
        foreach (var s in spearmen)
        {
            if (s.CompareTag("Untagged")) continue;
            UnitSaveData u = new UnitSaveData();
            u.unitType = "Spearman";
            u.posX = s.transform.position.x;
            u.posY = s.transform.position.y;
            u.currentHealth = s.currentHealth;
            data.units.Add(u);
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("SavedArmyData", json);
    }

    public void LoadGame()
    {
        gold = PlayerPrefs.GetInt("SavedGold", 0);
        wood = PlayerPrefs.GetInt("SavedWood", 0);
        stone = PlayerPrefs.GetInt("SavedStone", 0);
        currentWave = PlayerPrefs.GetInt("SavedWave", 1);
        
        knightLevel = PlayerPrefs.GetInt("SavedKnightLevel", 1);
        archerLevel = PlayerPrefs.GetInt("SavedArcherLevel", 1);
        spearmanLevel = PlayerPrefs.GetInt("SavedSpearmanLevel", 1); 
        
        knightUpgradeCost = PlayerPrefs.GetInt("SavedKnightCost", 100);
        archerUpgradeCost = PlayerPrefs.GetInt("SavedArcherCost", 120);
        spearmanUpgradeCost = PlayerPrefs.GetInt("SavedSpearmanCost", 110); 
        
        maxUnits = PlayerPrefs.GetInt("SavedMaxUnits", 5);
        
        barracksLevel = PlayerPrefs.GetInt("SavedBarracksLevel", 0);
        if (barracksLevel > 0) isBarracksBuilt = true; 
        
        isSpearmanUnlocked = PlayerPrefs.GetInt("SavedSpearmanUnlocked", 0) == 1;
        
        isMineBuilt = PlayerPrefs.GetInt("SavedMineBuilt", 0) == 1;
        mineLevel = PlayerPrefs.GetInt("SavedMineLevel", 0);
        if (mineLevel > 0) isMineBuilt = true;

        towerLevel = PlayerPrefs.GetInt("SavedTowerLevel", 1);
        towerWoodCost = PlayerPrefs.GetInt("SavedTowerWoodCost", 50);
        towerStoneCost = PlayerPrefs.GetInt("SavedTowerStoneCost", 20);

        if (isBarracksBuilt) SpawnBarracksObject();
        if (isMineBuilt) SpawnMineObject();

        if (castle == null) castle = FindFirstObjectByType<Castle>();
        
        if (castle != null)
        {
            int savedCastleLvl = PlayerPrefs.GetInt("SavedCastleLevel", 1);
            castle.LoadState(savedCastleLvl); 
        }

        LoadUnits(); 
    }

    void LoadUnits()
    {
        if (PlayerPrefs.HasKey("SavedArmyData"))
        {
            string json = PlayerPrefs.GetString("SavedArmyData");
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            
            currentUnits = 0; 

            foreach (UnitSaveData u in data.units)
            {
                GameObject prefabToSpawn = null;
                if (u.unitType == "Knight") prefabToSpawn = knightPrefab;
                else if (u.unitType == "Archer") prefabToSpawn = archerPrefab;
                else if (u.unitType == "Spearman") prefabToSpawn = spearmanPrefab;

                if (prefabToSpawn != null)
                {
                    Vector3 pos = new Vector3(u.posX, u.posY, 0);
                    GameObject newUnit = Instantiate(prefabToSpawn, pos, Quaternion.identity);
                    
                    if (u.unitType == "Knight") newUnit.GetComponent<Knight>().LoadState(u.currentHealth);
                    else if (u.unitType == "Archer") newUnit.GetComponent<Archer>().LoadState(u.currentHealth);
                    else if (u.unitType == "Spearman") newUnit.GetComponent<Spearman>().LoadState(u.currentHealth);

                    currentUnits++;
                }
            }
            UpdateUI();
        }
    }

    private void OnApplicationQuit() { SaveGame(); }
    private void OnApplicationPause(bool pauseStatus) { if (pauseStatus) SaveGame(); }
    
    [ContextMenu("Delete Save File")]
    public void DeleteSave()
    {
        PlayerPrefs.DeleteAll();
        gold = 0; wood = 0; stone = 0;
        currentWave = 1; maxUnits = 5;
        knightLevel = 1; archerLevel = 1; spearmanLevel = 1; 
        
        isSpearmanUnlocked = false; 
        isMineBuilt = false; 
        mineLevel = 0; 
        
        isBarracksBuilt = false;
        barracksLevel = 0;
        
        enemiesAlive = 0;
        currentUnits = 0;
        
        if (currentBarracksObject != null) Destroy(currentBarracksObject);
        if (currentMineObject != null) Destroy(currentMineObject); 
        if (currentSpikes != null) Destroy(currentSpikes.gameObject);
        currentSpikes = null;
        
        manualTarget = null;
        if(targetIndicator) targetIndicator.Hide();
        
        if (waveTimerBar != null) waveTimerBar.value = 1f;

        UpdateUI();
        UpdateBarracksStateUI();
    }

    // === UI ===
    public void UpdateUI()
    {
        if (goldText) goldText.text = gold.ToString();
        if (woodText) woodText.text = wood.ToString();
        if (stoneText) stoneText.text = stone.ToString();
        if (waveText) waveText.text = "Wave " + currentWave;
        
        if (limitText != null) limitText.text = $"{currentUnits} / {maxUnits}";
        
        if (hirePriceText) 
        {
            string spearPrice = isSpearmanUnlocked ? $"{spearmanFixedCost}G" : "LOCKED";
            hirePriceText.text = $"Knight: {knightFixedCost}G\nSpear: {spearPrice}\nArcher: {archerFixedCost}G";
        }
        
        if (towerPriceText) towerPriceText.text = $"Tower Lvl {towerLevel}\nDMG: {GetTowerDamage()}\n{towerWoodCost} W / {towerStoneCost} S";
        if (knightLevelText) knightLevelText.text = $"Knights Lvl {knightLevel}\nDMG: {GetKnightDamage()}";
        if (archerLevelText) archerLevelText.text = $"Archers Lvl {archerLevel}\nDMG: {GetArcherDamage()}";
        
        if (spearmanLevelText) spearmanLevelText.text = $"Spearmen Lvl {spearmanLevel}\nDMG: {GetSpearmanDamage()}";
        if (spearmanPriceText) spearmanPriceText.text = $"{spearmanUpgradeCost} G";

        if (knightPriceText) knightPriceText.text = $"{knightUpgradeCost} G";
        if (archerPriceText) archerPriceText.text = $"{archerUpgradeCost} G";

        bool canHireKnight = (gold >= knightFixedCost) && (currentUnits < maxUnits);
        bool canHireArcher = (gold >= archerFixedCost) && (currentUnits < maxUnits);
        bool canHireSpearman = (gold >= spearmanFixedCost) && (currentUnits < maxUnits); 
        
        UpdateButtonState(hireKnightButton, canHireKnight);
        UpdateButtonState(hireArcherButton, canHireArcher);
        
        if (hireSpearmanButton != null)
        {
            hireSpearmanButton.gameObject.SetActive(isSpearmanUnlocked);
            UpdateButtonState(hireSpearmanButton, canHireSpearman);
        }
        
        UpdateButtonState(towerButton, (wood >= towerWoodCost && stone >= towerStoneCost));
        UpdateButtonState(hammerButton, true);

        UpdateButtonState(upgradeKnightButton, gold >= knightUpgradeCost);
        UpdateButtonState(upgradeArcherButton, gold >= archerUpgradeCost);
        
        if (upgradeSpearmanButton != null)
        {
            upgradeSpearmanButton.gameObject.SetActive(isSpearmanUnlocked);
            UpdateButtonState(upgradeSpearmanButton, gold >= spearmanUpgradeCost);
        }

        if (spearmanForgeRow != null)
        {
            spearmanForgeRow.SetActive(isSpearmanUnlocked);
        }

        if (unlockSpearmanButton != null)
        {
            unlockSpearmanButton.gameObject.SetActive(isBarracksBuilt);

            if (isSpearmanUnlocked)
            {
                UpdateButtonState(unlockSpearmanButton, false);
                if (unlockSpearmanPriceText) unlockSpearmanPriceText.text = "UNLOCKED";
            }
            else
            {
                UpdateButtonState(unlockSpearmanButton, gold >= spearmanUnlockCost);
                if (unlockSpearmanPriceText) unlockSpearmanPriceText.text = $"Unlock Spear ({spearmanUnlockCost} G)";
            }
        }

        if (buildBarracksBtnInMenu != null)
        {
            int costG = GetBarracksBuildingUpgradeCost(true);
            int costW = GetBarracksBuildingUpgradeCost(false);
            UpdateButtonState(buildBarracksBtnInMenu, gold >= costG && wood >= costW);
            
            UpdateBarracksStateUI(); 
        }

        if (upgradeLimitButton != null)
        {
            int cap = GetBarracksCapLimit();
            bool canBuySlot = (gold >= GetSlotUpgradeCost()) && (maxUnits < cap);
            UpdateButtonState(upgradeLimitButton, canBuySlot);
        }

        if (buildMineButton != null)
        {
            int mWood = GetMineUpgradeCost(true);
            int mStone = GetMineUpgradeCost(false);
            
            bool canAffordMine = (wood >= mWood) && (stone >= mStone);
            UpdateButtonState(buildMineButton, canAffordMine);

            Image mineImg = buildMineButton.GetComponent<Image>();
            if (mineImg != null)
            {
                if (mineLevel == 0)
                    mineImg.sprite = buildButtonSprite;
                else
                    mineImg.sprite = upgradeButtonSprite;
            }

            if (mineLevel == 0)
            {
                if (minePriceText) minePriceText.text = $"{mWood} W / {mStone} S";
            }
            else
            {
                if (minePriceText) minePriceText.text = $"Lvl {mineLevel+1}\n{mWood} W / {mStone} S";
            }
        }

        if (openShopButton != null) UpdateButtonState(openShopButton, true);
        
        if (nextWaveButton != null) UpdateButtonState(nextWaveButton, enemiesAlive <= 0);

        if (buildSpikesButton != null)
        {
            bool canBuild = (wood >= spikesWoodCost) && (currentSpikes == null);
            UpdateButtonState(buildSpikesButton, canBuild);
            
            if (spikesPriceText)
            {
                if (currentSpikes != null) spikesPriceText.text = "BUILT";
                else spikesPriceText.text = $"{spikesWoodCost} Wood";
            }
        }

        // === НОВЕ: РОЗРАХУНОК ПРИБУТКУ ===
        if (estimatedIncomeText != null && spawner != null)
        {
            // 1. Золото за ворогів (з методу, що ми написали в Spawner)
            int enemiesGold = spawner.GetEstimatedGoldFromEnemies();
            
            // 2. Золото за проходження хвилі
            int waveBonus = GetGoldReward();

            int totalEst = enemiesGold + waveBonus;

            estimatedIncomeText.text = $"+{totalEst} G";
        }
    }
    
    // === УНІВЕРСАЛЬНИЙ МЕТОД ===
    void UpdateButtonState(Button btn, bool isActive) 
    { 
        if (btn == null) return; 
        btn.interactable = isActive; 
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.color = isActive ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f); 
        }
    }

    // === АВТОМАТИЧНЕ НАЛАШТУВАННЯ КНОПОК ===
    void SetupAllButtons()
    {
        // Знаходимо всі активні кнопки на сцені
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);

        foreach (Button btn in allButtons)
        {
            // Отримуємо поточні налаштування кольорів кнопки
            ColorBlock colors = btn.colors;

            // Вмикаємо режим зміни кольору (Transition)
            btn.transition = Selectable.Transition.ColorTint;

            // Задаємо колір натискання (Pressed) - робимо його темно-сірим
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f); 
            
            // Скидаємо Normal Color на білий, щоб не було конфліктів
            colors.normalColor = Color.white;

            // Встановлюємо disabledColor такий самий, як ми робимо вручну в UpdateButtonState,
            // щоб все виглядало однаково
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 1f);

            // Множник кольору
            colors.colorMultiplier = 1f;

            // Застосовуємо зміни назад до кнопки
            btn.colors = colors;
        }
    }
}
