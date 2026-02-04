using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; 
using System.Collections.Generic; // Для списків (List)

public enum ResourceType { Gold, Wood, Stone }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ==========================================
    // === НОВЕ: СИСТЕМА ПРИЦІЛЮВАННЯ ===
    // ==========================================
    [Header("Система прицілювання")]
    public Transform manualTarget; // Ціль, яку вибрав гравець
    public TargetIndicator targetIndicator; // Посилання на об'єкт курсора (червоний круг)
    
    public void SetManualTarget(Transform target)
    {
        manualTarget = target;
        if (targetIndicator != null) targetIndicator.Show(target);
    }
    // ==========================================

    [Header("=== БАЛАНС: ЕКОНОМІКА І СКЛАДНІСТЬ ===")]
    public float enemyHpGrowth = 1.10f;     
    public float goldRewardGrowth = 1.08f;  
    public float upgradeCostGrowth = 1.20f; 
    
    // Експоненти для сили
    private float knightPowerExp = 2.1f;    
    private float archerPowerExp = 1.95f;   
    private float towerPowerExp = 2.2f;     

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

    [Header("Налаштування Казарми")]
    public GameObject barracksPrefab;       
    public Transform barracksSpawnPoint;    
    public int barracksCostGold = 200;
    public int barracksCostWood = 100;
    
    private bool isBarracksBuilt = false;      
    private GameObject currentBarracksObject;  

    // === КОЛЮЧКИ (SPIKES) ===
    [Header("Захисні споруди (Spikes)")]
    public GameObject spikesPrefab;
    public Transform spikesSpawnPoint; // Точка перед замком
    public int spikesWoodCost = 100;
    public Button buildSpikesButton;   // Кнопка в UI
    public TMP_Text spikesPriceText;
    
    [HideInInspector] public Spikes currentSpikes; // Посилання на активні колючки

    [Header("UI: Магазин (Кузня)")]
    public GameObject shopPanel;           
    public Button upgradeKnightButton;     
    public Button upgradeArcherButton;     
    public TMP_Text knightLevelText; 
    public TMP_Text knightPriceText; 
    public TMP_Text archerLevelText;
    public TMP_Text archerPriceText; 

    [Header("UI: Головний екран")]
    public Button hireKnightButton; 
    public Button hireArcherButton; 
    public Button openShopButton;         
    public Button towerButton;      
    
    [Header("Управління Хвилею")]
    public Button nextWaveButton;   
    public int enemiesAlive = 0;    

    public GameObject defeatPanel;
    public TMP_Text defeatResourcesText;

    [Header("Зв'язки")]
    public Castle castle; 
    public Transform towerTransform;       
    public EnemySpawner spawner; 

    [Header("Спрайти кнопок")]
    public Sprite buttonActiveSprite;      
    public Sprite buttonDisabledSprite;    

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
    public Transform unitSpawnPoint;
    
    // === НОВЕ: ФІКСОВАНІ ЦІНИ ===
    [Header("Ціни на найм (Фіксовані)")]
    public int knightFixedCost = 50; 
    public int archerFixedCost = 75;
    // public int unitCost = 50; // СТАРА ЗМІННА (ВИДАЛЕНА/ЗАКОМЕНТОВАНА)
    
    [Header("Рівні Технологій")]
    public int knightLevel = 1;      
    public int archerLevel = 1;      
    public int knightUpgradeCost = 100;
    public int archerUpgradeCost = 120; 

    [Header("Ліміт військ")]
    public int maxUnits = 5;          
    public int currentUnits = 0;      

    [Header("Баланс: Вежа")]
    public int towerLevel = 1; 
    public int towerWoodCost = 50;
    public int towerStoneCost = 20;

    [Header("Баланс: Хвилі")]
    public int currentWave = 1;
    public int baseGoldReward = 15; 
    public int baseEnemyHealth = 40; 

    private bool isWaveInProgress = false;

    void Awake() 
    { 
        if (Instance == null) Instance = this; 
        else Destroy(gameObject); 

        LoadGame();
    }

    void Start()
    {
        enemiesAlive = 0;
        UpdateUI();
        
        if (defeatPanel) defeatPanel.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false); 
        if (constructionPanel) constructionPanel.SetActive(false);
        if (barracksUpgradePanel) barracksUpgradePanel.SetActive(false);
        
        UpdateBarracksStateUI();

        if (autoStartWaves)
        {
            StartCoroutine(AutoStartNextWave(3.0f));
        }
    }

    // =========================================================
    //              МАТЕМАТИКА БАЛАНСУ
    // =========================================================

    public int GetDifficultyHealth()
    {
        return Mathf.RoundToInt(baseEnemyHealth * Mathf.Pow(enemyHpGrowth, currentWave));
    }

    public int GetGoldReward() 
    { 
        return Mathf.RoundToInt(baseGoldReward * Mathf.Pow(goldRewardGrowth, currentWave)); 
    }

    public int GetKnightDamage()
    {
        return 10 + (int)Mathf.Pow(knightLevel, knightPowerExp);
    }

    public int GetArcherDamage()
    {
        return 8 + (int)Mathf.Pow(archerLevel, archerPowerExp);
    }

    public int GetTowerDamage()
    {
        return 30 + (int)Mathf.Pow(towerLevel, towerPowerExp);
    }

    // =========================================================

    // === УПРАВЛІННЯ ВОРОГАМИ ===
    public void RegisterEnemy() 
    { 
        enemiesAlive++; 
        isWaveInProgress = true;
        UpdateUI(); 
    }

    public void UnregisterEnemy() 
    { 
        enemiesAlive--; 
        if(enemiesAlive < 0) enemiesAlive = 0; 
        UpdateUI(); 

        // Якщо ціль, яку ми вибрали, померла - скидаємо вибір
        if (manualTarget != null && !manualTarget.gameObject.activeInHierarchy)
        {
             manualTarget = null;
        }

        if (enemiesAlive == 0 && isWaveInProgress)
        {
            isWaveInProgress = false;
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

    // === ХВИЛІ ===
    public void NextWave()
    {
        if (enemiesAlive > 0) return;

        if (!isWaveInProgress && enemiesAlive == 0 && Time.timeSinceLevelLoad > 5f)
        {
             currentWave++;
        }

        if (castle != null) 
        {
            castle.HealMax(); 
        }
        
        // Скидаємо ручну ціль при новій хвилі
        manualTarget = null;
        if(targetIndicator) targetIndicator.Hide();

        SaveGame(); 
        UpdateUI();
        
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.waveStart);
        
        if (spawner != null) spawner.StartWave(currentWave);
        
        isWaveInProgress = true;
    }

    // === БУДІВНИЦТВО КАЗАРМИ ===
    public void ToggleConstructionMenu()
    {
        if (constructionPanel)
        {
            bool isActive = !constructionPanel.activeSelf;
            constructionPanel.SetActive(isActive);
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);

            if (isActive) 
            { 
                barracksUpgradePanel.SetActive(false); 
                shopPanel.SetActive(false); 
            }
        }
    }

    public void BuildBarracks()
    {
        if (isBarracksBuilt) return; 

        if (gold >= barracksCostGold && wood >= barracksCostWood)
        {
            gold -= barracksCostGold; 
            wood -= barracksCostWood;
            
            SpawnBarracksObject();
            isBarracksBuilt = true;
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.constructionSound); 
            if (constructionPanel) constructionPanel.SetActive(false);
            
            SaveGame(); 
            UpdateUI(); 
            UpdateBarracksStateUI();
        }
        else 
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    // === БУДІВНИЦТВО КОЛЮЧОК (SPIKES) ===
    public void BuildSpikes()
    {
        // Не будуємо, якщо вже є
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
    
    void UpdateBarracksStateUI()
    {
        if (barracksIconButton) barracksIconButton.gameObject.SetActive(isBarracksBuilt); 
        
        if (buildBarracksBtnInMenu) 
        {
            if (isBarracksBuilt) 
            { 
                SetButtonInteractableOnly(buildBarracksBtnInMenu, false); 
                if (barracksBuildPriceText) barracksBuildPriceText.text = "BUILT"; 
            }
            else 
            { 
                if (barracksBuildPriceText) barracksBuildPriceText.text = $"{barracksCostGold} G / {barracksCostWood} W"; 
            }
        }
    }

    // === АПГРЕЙД ЛІМІТУ ===
    public void ToggleBarracksUpgradeMenu()
    {
        if (barracksUpgradePanel) 
        {
            bool isActive = !barracksUpgradePanel.activeSelf;
            barracksUpgradePanel.SetActive(isActive);
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
            
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
        int cost = GetBarracksUpgradeCost();
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

    int GetBarracksUpgradeCost() 
    { 
        int u = maxUnits - 5; 
        if (u < 0) u = 0; 
        return 100 + (u * 50); 
    }
    
    void UpdateUpgradeMenuPrice() 
    { 
        if (upgradeLimitPriceText) upgradeLimitPriceText.text = $"Limit +1   ({GetBarracksUpgradeCost()} G)"; 
    }

    // === МАГАЗИН І АПГРЕЙДИ ===
    public void ToggleShop() 
    { 
        if (shopPanel) 
        {
            bool isActive = !shopPanel.activeSelf;
            shopPanel.SetActive(isActive);
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
            
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
    
    // === ОНОВЛЕНО: НАЙМ ВІЙСЬК (Фіксовані ціни) ===
    public void HireKnight() { TryHireUnit(knightPrefab, knightFixedCost); }
    public void HireArcher() { TryHireUnit(archerPrefab, archerFixedCost); }
    
    private void TryHireUnit(GameObject prefab, int cost) 
    { 
        if(currentUnits < maxUnits && gold >= cost)
        { 
            gold -= cost; 
            Instantiate(prefab, unitSpawnPoint.position, Quaternion.identity); 
            currentUnits++; 
            // МНОЖЕННЯ ЦІНИ ПРИБРАНО!
            SaveGame(); 
            UpdateUI();
        } 
    }
    
    public void UpgradeDamage() // Апгрейд Вежі
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

    // === ПРОГРАШ І РЕСТАРТ ===
    public void Defeat() 
    { 
        Time.timeScale = 0; 
        if(defeatPanel) defeatPanel.SetActive(true); 
        SaveGame(); 
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.defeat); 
    }
    
    public void OnRetryButton() 
    { 
        Time.timeScale = 1; 
        if(defeatPanel) defeatPanel.SetActive(false); 
        
        spawner.StopSpawning(); 
        spawner.ClearEnemies(); 
        
        if(castle) castle.HealMax(); 

        // Очищаємо колючки
        if (currentSpikes != null) Destroy(currentSpikes.gameObject);
        currentSpikes = null;

        // Очищаємо ціль
        manualTarget = null;
        if(targetIndicator) targetIndicator.Hide();
        
        var units = FindObjectsByType<Knight>(FindObjectsSortMode.None); 
        foreach(var u in units) Destroy(u.gameObject); 
        
        var archs = FindObjectsByType<Archer>(FindObjectsSortMode.None); 
        foreach(var a in archs) Destroy(a.gameObject); 
        
        currentUnits = 0; 
        enemiesAlive = 0; 
        
        spawner.StartWave(currentWave); 
        
        UpdateUI(); 
        UpdateBarracksStateUI(); 
    }

    // === ЗБЕРЕЖЕННЯ І ЗАВАНТАЖЕННЯ ===
    public void SaveGame()
    {
        // 1. Стандартні ресурси
        PlayerPrefs.SetInt("SavedGold", gold);
        PlayerPrefs.SetInt("SavedWood", wood);
        PlayerPrefs.SetInt("SavedStone", stone);
        PlayerPrefs.SetInt("SavedWave", currentWave);
        PlayerPrefs.SetInt("SavedKnightLevel", knightLevel);
        PlayerPrefs.SetInt("SavedArcherLevel", archerLevel);
        PlayerPrefs.SetInt("SavedKnightCost", knightUpgradeCost);
        PlayerPrefs.SetInt("SavedArcherCost", archerUpgradeCost);
        
        // unitCost БІЛЬШЕ НЕ ЗБЕРІГАЄМО (ціни фіксовані)

        PlayerPrefs.SetInt("SavedMaxUnits", maxUnits);
        PlayerPrefs.SetInt("SavedIsBarracksBuilt", isBarracksBuilt ? 1 : 0);
        PlayerPrefs.SetInt("SavedTowerLevel", towerLevel); 
        PlayerPrefs.SetInt("SavedTowerWoodCost", towerWoodCost);
        PlayerPrefs.SetInt("SavedTowerStoneCost", towerStoneCost);

        // 2. === ЗБЕРІГАЄМО АРМІЮ ===
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
        knightUpgradeCost = PlayerPrefs.GetInt("SavedKnightCost", 100);
        archerUpgradeCost = PlayerPrefs.GetInt("SavedArcherCost", 120);
        
        // Ціни на найм беремо з фіксованих змінних, не з PlayerPrefs

        maxUnits = PlayerPrefs.GetInt("SavedMaxUnits", 5);
        isBarracksBuilt = PlayerPrefs.GetInt("SavedIsBarracksBuilt", 0) == 1;
        
        towerLevel = PlayerPrefs.GetInt("SavedTowerLevel", 1);
        towerWoodCost = PlayerPrefs.GetInt("SavedTowerWoodCost", 50);
        towerStoneCost = PlayerPrefs.GetInt("SavedTowerStoneCost", 20);

        if (isBarracksBuilt) SpawnBarracksObject();

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

                if (prefabToSpawn != null)
                {
                    Vector3 pos = new Vector3(u.posX, u.posY, 0);
                    GameObject newUnit = Instantiate(prefabToSpawn, pos, Quaternion.identity);
                    
                    if (u.unitType == "Knight") newUnit.GetComponent<Knight>().LoadState(u.currentHealth);
                    else if (u.unitType == "Archer") newUnit.GetComponent<Archer>().LoadState(u.currentHealth);

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
        knightLevel = 1; archerLevel = 1; towerLevel = 1;
        isBarracksBuilt = false;
        enemiesAlive = 0;
        currentUnits = 0;
        
        if (currentBarracksObject != null) Destroy(currentBarracksObject);
        if (currentSpikes != null) Destroy(currentSpikes.gameObject);
        currentSpikes = null;
        
        // Очищаємо ціль
        manualTarget = null;
        if(targetIndicator) targetIndicator.Hide();

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
        
        // ОНОВЛЕНО: Відображення двох цін
        if (hirePriceText) 
        {
            hirePriceText.text = $"Knight: {knightFixedCost}G\nArcher: {archerFixedCost}G";
        }
        
        if (towerPriceText) towerPriceText.text = $"Tower Lvl {towerLevel}\nDMG: {GetTowerDamage()}\n{towerWoodCost} W / {towerStoneCost} S";
        if (knightLevelText) knightLevelText.text = $"Knights Lvl {knightLevel}\nDMG: {GetKnightDamage()}";
        if (archerLevelText) archerLevelText.text = $"Archers Lvl {archerLevel}\nDMG: {GetArcherDamage()}";

        if (knightPriceText) knightPriceText.text = $"{knightUpgradeCost} G";
        if (archerPriceText) archerPriceText.text = $"{archerUpgradeCost} G";

        // ОНОВЛЕНО: Окремі перевірки для кнопок найму
        bool canHireKnight = (gold >= knightFixedCost) && (currentUnits < maxUnits);
        bool canHireArcher = (gold >= archerFixedCost) && (currentUnits < maxUnits);
        
        SetButtonInteractableOnly(hireKnightButton, canHireKnight);
        SetButtonInteractableOnly(hireArcherButton, canHireArcher);
        
        SetButtonInteractableOnly(towerButton, (wood >= towerWoodCost && stone >= towerStoneCost));
        SetButtonInteractableOnly(hammerButton, true); 

        SetButtonInteractableOnly(upgradeKnightButton, gold >= knightUpgradeCost);
        SetButtonInteractableOnly(upgradeArcherButton, gold >= archerUpgradeCost);

        if (!isBarracksBuilt && buildBarracksBtnInMenu != null)
        {
            bool canBuild = gold >= barracksCostGold && wood >= barracksCostWood;
            SetButtonInteractableOnly(buildBarracksBtnInMenu, canBuild);
        }

        if (upgradeLimitButton != null)
        {
            int cost = GetBarracksUpgradeCost();
            SetButtonInteractableOnly(upgradeLimitButton, gold >= cost);
        }

        if (openShopButton != null)
        {
            bool canAffordAny = (gold >= knightUpgradeCost) || (gold >= archerUpgradeCost);
            SetForgeButtonState(openShopButton, canAffordAny);
        }
        
        if (nextWaveButton != null)
        {
            SetButtonInteractableOnly(nextWaveButton, enemiesAlive <= 0);
        }

        // === КНОПКА КОЛЮЧОК ===
        if (buildSpikesButton != null)
        {
            bool canBuild = (wood >= spikesWoodCost) && (currentSpikes == null);
            buildSpikesButton.interactable = canBuild;
            
            if (spikesPriceText)
            {
                if (currentSpikes != null) spikesPriceText.text = "BUILT";
                else spikesPriceText.text = $"{spikesWoodCost} Wood";
            }
        }
    }
    
    void SetButtonInteractableOnly(Button btn, bool isActive) 
    { 
        if (btn == null) return; 
        btn.interactable = isActive; 
    }

    void SetForgeButtonState(Button btn, bool hasUpgrade) 
    { 
        if (btn == null) return; 
        btn.interactable = true; 
        Image img = btn.GetComponent<Image>();
        if(img)
        {
            img.sprite = hasUpgrade ? buttonActiveSprite : buttonDisabledSprite;
            img.color = Color.white; 
        }
    }
}