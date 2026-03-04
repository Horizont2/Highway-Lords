using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

// === НОВИЙ КЛАС ДЛЯ ГРУПИ ЦІН (Іконки + Текст) ===
[System.Serializable]
public class UICostGroup
{
    public Image icon1;
    public TMP_Text text1;
    public Image icon2;
    public TMP_Text text2;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("=== МЕТА-ПРОГРЕСІЯ (ТАЛАНТИ) ===")]
    public int gems = 0;
    public int metaCastleHpLevel = 0;
    public int metaDiscountLevel = 0;
    public int metaIncomeLevel = 0;

    [Header("Прогрес Кристалів (Шкала)")]
    public int currentKills = 0;
    public int killsToNextGem = 25; // Початкова кількість вбивств для 1 гема
    public Slider gemProgressBar;
    public TMP_Text gemProgressText;

    [Header("UI Мета-Прогресії")]
    public TMP_Text gemsText;
    public GameObject metaShopPanel;
    public TMP_Text metaHpText;
    public TMP_Text metaDiscountText;
    public TMP_Text metaIncomeText;
    public Button openMetaShopButton;
    public Button closeMetaShopButton;

    [Header("Formation Settings")]
    public Transform formationStartPoint; // Початок червоної зони
    public float rowSpacing = 1.5f;       // Відстань між рядами (X)
    public float columnSpacing = 0.8f;    // Відстань між юнітами в ряду (Y)

    // Списки для відстеження живих юнітів
    private List<Knight> activeKnights = new List<Knight>();
    private List<Spearman> activeSpearmen = new List<Spearman>();
    private List<Archer> activeArchers = new List<Archer>();

    [Header("Continuous Wave UI")]
    public GameObject tickPrefab;      // Префаб маленької лінії-мітки
    public Transform tickContainer;    // Дочірній об'єкт Slider (зазвичай Fill Area)
    public float waveDuration = 60f;   // Скільки триває хвиля в секундах
    private float waveTimer = 0f;
    private List<float> currentWaveMilestones = new List<float>(); // Значення від 0 до 1

    private bool isFirstWaveStarted = false;
    private bool isWaitingForNextWave = false;

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

    public static float GetDamageMultiplier(UnitCategory attacker, UnitCategory defender)
    {
        if (attacker == UnitCategory.Standard && defender == UnitCategory.Ranged)   return 1.5f;  
        if (attacker == UnitCategory.Standard && defender == UnitCategory.Spearman) return 0.75f; 

        if (attacker == UnitCategory.Spearman && defender == UnitCategory.Cavalry)  return 2.0f;  
        if (attacker == UnitCategory.Spearman && defender == UnitCategory.Ranged)   return 0.75f; 

        if (attacker == UnitCategory.Ranged   && defender == UnitCategory.Spearman) return 1.5f;  
        if (attacker == UnitCategory.Ranged   && defender == UnitCategory.Standard) return 0.75f; 

        if (attacker == UnitCategory.Cavalry  && (defender == UnitCategory.Standard || defender == UnitCategory.Ranged)) return 1.5f; 
        if (attacker == UnitCategory.Cavalry  && defender == UnitCategory.Spearman) return 0.5f;  

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

    [Header("=== UI: ГОЛОВНІ ПАНЕЛІ ===")]
    public GameObject constructionPanel;
    public GameObject barracksUpgradePanel;
    public GameObject shopPanel;
    public GameObject infoPanel; 

    // === НОВЕ: ФОНОВЕ ЗАТЕМНЕННЯ ===
    [Header("UI: Затемнення фону (Пауза)")]
    public GameObject darkOverlay;

    [Header("UI: Defeat Overlay")]
    public TMPro.TMP_Text defeatText; 

    [Header("UI: Панелі (fallback поля)")]
    public GameObject constructionPanelNew;
    public GameObject barracksPanelNew;
    public GameObject shopPanelNew;

    [Header("UI: Кнопки відкриття")]
    public Button hammerButton;
    public Button barracksIconButton;
    public Button openShopButton;
    public Button openInfoButton; 

    [Header("UI: Кнопки хрестики")]
    public Button closeConstructionBtn;
    public Button closeBarracksBtn;
    public Button closeShopBtn;
    public Button closeInfoButton; 

    [Header("UI: Панель Будівництва (Construction)")]
    public Button buildBarracksBtnInMenu;
    public Button buildSpikesButton;
    public Button buildMineButton;
    public Button towerButton;
    public Button castleUpgradeButton;

    public TMP_Text barracksInfoText;  
    public TMP_Text mineInfoText;      
    public TMP_Text castleInfoText;    

    public UICostGroup barracksCostUI;
    public UICostGroup mineCostUI;
    public UICostGroup spikesCostUI;
    public UICostGroup towerCostUI;
    public UICostGroup castleUpgradeCostUI;

    [Header("UI: Панель Казарм (Barracks)")]
    public Button upgradeLimitButton;
    public UICostGroup upgradeLimitCostUI;
    public TMP_Text upgradeLimitInfoText; 
    public Button unlockSpearmanButton;
    public UICostGroup unlockSpearmanCostUI;
    public CanvasGroup unlockSpearmanRowGroup;

    [Header("UI: Панель Кузні (Shop/Forge)")]
    public Button upgradeKnightButton;
    public Button upgradeArcherButton;
    public Button upgradeSpearmanButton;
    
    [Header("UI: Wall Archers (Лучники на стіні)")]
    public Button upgradeWallArcherButton;
    public TMP_Text wallArcherLevelText;
    public UICostGroup wallArcherUpgradeCostUI;

    public TMP_Text knightLevelText;
    public TMP_Text archerLevelText;
    public TMP_Text spearmanLevelText;

    public UICostGroup knightUpgradeCostUI;
    public UICostGroup archerUpgradeCostUI;
    public UICostGroup spearmanUpgradeCostUI;

    [Header("UI: Spearman lock (Списоносець)")]
    public CanvasGroup spearmanRowGroup;
    public GameObject spearmanLockIcon;
    public bool dimSpearmanWhenLocked = true;

    [Header("UI: Арбалетна Башта (Crossbow Tower)")]
    public Button upgradeCrossbowDamageButton;
    public Button upgradeCrossbowReloadButton;
    
    public TMP_Text crossbowDamageText;
    public TMP_Text crossbowReloadText;

    public UICostGroup crossbowDamageCostUI;
    public UICostGroup crossbowReloadCostUI;

    [Header("UI: Статистика")]
    public TMP_Text estimatedIncomeText;

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
    public int spikesWoodCost = 50;

    [HideInInspector] public Spikes currentSpikes;

    [Header("UI: Головний екран (Найм)")]
    public Button hireKnightButton;
    public Button hireArcherButton;
    public Button hireSpearmanButton;

    [Header("UI: Прогрес Хвилі")]
    public Slider waveTimerBar;

    [Header("Управління Хвилею")]
    public Button nextWaveButton;
    public int enemiesAlive = 0;

    public GameObject defeatPanel;
    public TMP_Text defeatResourcesText;

    [Header("Зв'язки")]
    [HideInInspector] public bool isDefeated = false;
    public Wall castle;
    public Transform towerTransform;
    public EnemySpawner spawner;

    [Header("Спрайти Кнопок")]
    public Sprite buildButtonSprite;
    public Sprite upgradeButtonSprite;

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
    public TMP_Text towerLevelText;

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
    public int wallArcherLevel = 1; 

    public int knightUpgradeCost = 100;
    public int archerUpgradeCost = 120;
    public int spearmanUpgradeCost = 110;
    public int wallArcherUpgradeCost = 150; 

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

    [Header("Баланс: Арбалетна Башта (Crossbow Tower)")]
    public int crossbowDamageLevel = 1;
    public int crossbowReloadLevel = 1;
    
    public int crossbowBaseDamage = 25;
    public float crossbowBaseReloadTime = 2.5f; 

    public int crossbowDamageCostGold = 150;
    public int crossbowReloadCostGold = 150;

    [Header("Баланс: Хвилі")]
    public int currentWave = 1;
    public int baseGoldReward = 15;
    public int baseEnemyHealth = 50;

    private bool isWaveInProgress = false;
    private float _lastHireTime = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadGame();
    }

    void Start()
    {
        if (goldText != null)
        {
            goldText.gameObject.SetActive(true);
            if (goldText.transform.parent != null)
            {
                goldText.transform.parent.gameObject.SetActive(true);
            }
        }

        ResolveUIRefs();
        ResolveSpearmanLockUI();
        WireExplicitButtons();

        RecalculateUnits();

        enemiesAlive = 0;

        if (constructionPanel) constructionPanel.SetActive(false);
        if (barracksUpgradePanel) barracksUpgradePanel.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false);
        if (infoPanel) infoPanel.SetActive(false);
        if (metaShopPanel) metaShopPanel.SetActive(false);
        if (defeatPanel) defeatPanel.SetActive(false);

        // Переконуємось, що на старті гри затемнення немає і час іде
        if (darkOverlay != null) darkOverlay.SetActive(false);
        Time.timeScale = 1f;

        UpdateUI();
        UpdateBarracksStateUI();
        UpdateSpearmanLockUI();
        UpdateMetaUI();

        if (waveTimerBar != null)
        {
            waveTimerBar.gameObject.SetActive(true);
            waveTimerBar.maxValue = 1f;
            waveTimerBar.value = 0f;
        }

        if (autoStartWaves && !isWaitingForNextWave)
        {
            isWaitingForNextWave = true;
            StartCoroutine(AutoStartNextWave(3.0f));
        }

        StartCoroutine(WaveWatchdog());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            DeleteSave();
        }

        if (isWaveInProgress)
        {
            waveTimer += Time.deltaTime;
            float progress = waveTimer / waveDuration;

            if (waveTimerBar != null)
            {
                waveTimerBar.value = 1f - progress;
            }

            for (int i = currentWaveMilestones.Count - 1; i >= 0; i--)
            {
                if (progress >= currentWaveMilestones[i])
                {
                    SpawnEnemySquad();
                    currentWaveMilestones.RemoveAt(i);

                    if (tickContainer != null && tickContainer.childCount > 0)
                    {
                        Destroy(tickContainer.GetChild(0).gameObject);
                    }
                }
            }

            if (progress >= 1f)
            {
                isWaveInProgress = false;
                if (waveTimerBar != null) waveTimerBar.value = 0f;
            }
        }
    }

    void ResolveUIRefs()
    {
        if (hammerButton == null) hammerButton = FindUIButton("BuildButton");
        if (barracksIconButton == null) barracksIconButton = FindUIButton("BarracksButton");
        if (openShopButton == null) openShopButton = FindUIButton("ForgeButton");
        if (towerButton == null) towerButton = FindUIButton("TowerButton");

        if (hireKnightButton == null) hireKnightButton = FindUIButton("HireKnight");
        if (hireArcherButton == null) hireArcherButton = FindUIButton("HireArcher");
        if (hireSpearmanButton == null) hireSpearmanButton = FindUIButton("HireSpearman");

        if (goldText == null) goldText = FindTMP("GoldText");
        if (woodText == null) woodText = FindTMP("WoodText");
        if (stoneText == null) stoneText = FindTMP("StoneText");
        if (waveText == null) waveText = FindTMP("WaveText");

        var construction = constructionPanel != null ? constructionPanel : constructionPanelNew;
        var barracks = barracksUpgradePanel != null ? barracksUpgradePanel : barracksPanelNew;
        var shop = shopPanel != null ? shopPanel : shopPanelNew;

        if (buildBarracksBtnInMenu == null) buildBarracksBtnInMenu = FindButtonInRow(construction, "Barracks_Row");
        if (buildSpikesButton == null) buildSpikesButton = FindButtonInRow(construction, "Spikes_Row");
        if (buildMineButton == null) buildMineButton = FindButtonInRow(construction, "Mine_Row");
        if (castleUpgradeButton == null) castleUpgradeButton = FindButtonInRow(construction, "Tower Upgrade_Row", "Castle Upgrade_Row");

        if (castleUpgradeCostUI == null) castleUpgradeCostUI = FindCostGroupInRow(construction, "Tower Upgrade_Row", "Castle Upgrade_Row");

        if (upgradeLimitButton == null) upgradeLimitButton = FindButtonInRow(barracks, "Barracks_Row", "Upgrade Units Limit_Row");
        if (unlockSpearmanButton == null) unlockSpearmanButton = FindButtonInRow(barracks, "Spikes_Row", "Unlock Spearman_Row");

        if (upgradeKnightButton == null) upgradeKnightButton = FindButtonInRow(shop, "Barracks_Row", "Knights_Row");
        if (upgradeArcherButton == null) upgradeArcherButton = FindButtonInRow(shop, "Spikes_Row", "Archers_Row");
        if (upgradeSpearmanButton == null) upgradeSpearmanButton = FindButtonInRow(shop, "Mine_Row", "Spearmen_Row");
        
        if (upgradeWallArcherButton == null) upgradeWallArcherButton = FindButtonInRow(shop, "WallArcher_Row", "WallArchers_Row");

        if (closeConstructionBtn == null) closeConstructionBtn = FindUIButtonInPanel(construction, "CloseButton");
        if (closeBarracksBtn == null) closeBarracksBtn = FindUIButtonInPanel(barracks, "CloseButton");
        if (closeShopBtn == null) closeShopBtn = FindUIButtonInPanel(shop, "CloseButton");

        if (unlockSpearmanButton != null && unlockSpearmanRowGroup == null)
        {
            Transform rowTransform = unlockSpearmanButton.transform;
            bool foundRow = false;

            int depth = 0;
            while (rowTransform.parent != null && depth < 4)
            {
                if (rowTransform.name.ToLower().Contains("row"))
                {
                    foundRow = true;
                    break;
                }
                rowTransform = rowTransform.parent;
                depth++;
            }

            if (foundRow)
            {
                unlockSpearmanRowGroup = rowTransform.GetComponent<CanvasGroup>();
                if (unlockSpearmanRowGroup == null) unlockSpearmanRowGroup = rowTransform.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (waveTimerBar != null && tickContainer == null)
        {
            tickContainer = waveTimerBar.transform.Find("Background");
            if (tickContainer == null) tickContainer = waveTimerBar.transform;
        }
    }

    Button FindUIButton(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.GetComponent<Button>() : null;
    }

    TMP_Text FindTMP(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) return null;
        return go.GetComponent<TMP_Text>();
    }

    Button FindUIButtonInPanel(GameObject panel, string name)
    {
        if (panel == null) return null;
        var t = panel.GetComponentsInChildren<Transform>(true);
        foreach (var tr in t)
        {
            if (tr.name == name) return tr.GetComponent<Button>();
        }
        return null;
    }

    Button FindButtonInRow(GameObject panel, params string[] rowNames)
    {
        if (panel == null) return null;
        foreach (var row in rowNames)
        {
            var t = FindChild(panel.transform, row);
            if (t != null)
            {
                var btn = t.GetComponentInChildren<Button>(true);
                if (btn != null) return btn;
            }
        }
        return null;
    }

    UICostGroup FindCostGroupInRow(GameObject panel, params string[] rowNames)
    {
        if (panel == null) return null;
        foreach (var row in rowNames)
        {
            var t = FindChild(panel.transform, row);
            if (t == null) continue;
            var icons = t.GetComponentsInChildren<Image>(true);
            var texts = t.GetComponentsInChildren<TMP_Text>(true);

            var iconList = icons.Where(i => i.name.Contains("CostIcon")).ToArray();
            var textList = texts.Where(tt => tt.name.Contains("CostText")).ToArray();
            if (iconList.Length > 0 || textList.Length > 0)
            {
                var g = new UICostGroup();
                g.icon1 = iconList.Length > 0 ? iconList[0] : null;
                g.text1 = textList.Length > 0 ? textList[0] : null;
                g.icon2 = iconList.Length > 1 ? iconList[1] : null;
                g.text2 = textList.Length > 1 ? textList[1] : null;
                return g;
            }
        }
        return null;
    }

    Transform FindChild(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }
        return null;
    }

    void ResolveSpearmanLockUI()
    {
        if (spearmanRowGroup != null) return;
        var shop = shopPanel != null ? shopPanel : shopPanelNew;
        if (shop == null) return;

        var texts = shop.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            if (t.text != null && t.text.ToLower().Contains("spear"))
            {
                Transform row = t.transform;
                bool foundRow = false;

                for (int i = 0; i < 4 && row.parent != null; i++)
                {
                    if (row.name.ToLower().Contains("row"))
                    {
                        foundRow = true;
                        break;
                    }
                    row = row.parent;
                }

                if (foundRow)
                {
                    spearmanRowGroup = row.GetComponent<CanvasGroup>();
                    if (spearmanRowGroup == null) spearmanRowGroup = row.gameObject.AddComponent<CanvasGroup>();

                    if (spearmanLockIcon == null)
                    {
                        var lockGo = new GameObject("LockIcon", typeof(RectTransform), typeof(Image));
                        lockGo.transform.SetParent(row, false);
                        var rt = lockGo.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0.9f, 0.5f);
                        rt.anchorMax = new Vector2(0.9f, 0.5f);
                        rt.sizeDelta = new Vector2(28, 28);
                        spearmanLockIcon = lockGo;
                    }
                }
                break;
            }
        }
    }

    void WireExplicitButtons()
    {
        if (hammerButton != null) { hammerButton.onClick.RemoveAllListeners(); hammerButton.onClick.AddListener(ToggleConstructionMenu); }
        if (barracksIconButton != null) { barracksIconButton.onClick.RemoveAllListeners(); barracksIconButton.onClick.AddListener(ToggleBarracksUpgradeMenu); }
        if (openShopButton != null) { openShopButton.onClick.RemoveAllListeners(); openShopButton.onClick.AddListener(ToggleShop); }
        if (closeConstructionBtn != null) { closeConstructionBtn.onClick.RemoveAllListeners(); closeConstructionBtn.onClick.AddListener(ToggleConstructionMenu); }
        if (closeBarracksBtn != null) { closeBarracksBtn.onClick.RemoveAllListeners(); closeBarracksBtn.onClick.AddListener(ToggleBarracksUpgradeMenu); }
        if (closeShopBtn != null) { closeShopBtn.onClick.RemoveAllListeners(); closeShopBtn.onClick.AddListener(ToggleShop); }

        if (openInfoButton != null) { openInfoButton.onClick.RemoveAllListeners(); openInfoButton.onClick.AddListener(ToggleInfoPanel); }
        if (closeInfoButton != null) { closeInfoButton.onClick.RemoveAllListeners(); closeInfoButton.onClick.AddListener(ToggleInfoPanel); }

        if (openMetaShopButton != null) { openMetaShopButton.onClick.RemoveAllListeners(); openMetaShopButton.onClick.AddListener(ToggleMetaShop); }
        if (closeMetaShopButton != null) { closeMetaShopButton.onClick.RemoveAllListeners(); closeMetaShopButton.onClick.AddListener(ToggleMetaShop); }

        if (buildBarracksBtnInMenu != null) { buildBarracksBtnInMenu.onClick.RemoveAllListeners(); buildBarracksBtnInMenu.onClick.AddListener(BuildOrUpgradeBarracks); }
        if (buildMineButton != null) { buildMineButton.onClick.RemoveAllListeners(); buildMineButton.onClick.AddListener(BuildOrUpgradeMine); }
        if (buildSpikesButton != null) { buildSpikesButton.onClick.RemoveAllListeners(); buildSpikesButton.onClick.AddListener(BuildSpikes); }
        if (towerButton != null) { towerButton.onClick.RemoveAllListeners(); towerButton.onClick.AddListener(UpgradeDamage); }
        if (castleUpgradeButton != null) { castleUpgradeButton.onClick.RemoveAllListeners(); castleUpgradeButton.onClick.AddListener(UpgradeCastleHP); }
        if (upgradeKnightButton != null) { upgradeKnightButton.onClick.RemoveAllListeners(); upgradeKnightButton.onClick.AddListener(UpgradeKnights); }
        if (upgradeArcherButton != null) { upgradeArcherButton.onClick.RemoveAllListeners(); upgradeArcherButton.onClick.AddListener(UpgradeArchers); }
        if (upgradeSpearmanButton != null) { upgradeSpearmanButton.onClick.RemoveAllListeners(); upgradeSpearmanButton.onClick.AddListener(UpgradeSpearman); }
        
        if (upgradeWallArcherButton != null) { upgradeWallArcherButton.onClick.RemoveAllListeners(); upgradeWallArcherButton.onClick.AddListener(UpgradeWallArchers); }

        if (upgradeLimitButton != null) { upgradeLimitButton.onClick.RemoveAllListeners(); upgradeLimitButton.onClick.AddListener(BuyUnitLimitUpgrade); }
        if (unlockSpearmanButton != null) { unlockSpearmanButton.onClick.RemoveAllListeners(); unlockSpearmanButton.onClick.AddListener(UnlockSpearman); }
        
        if (upgradeCrossbowDamageButton != null) { upgradeCrossbowDamageButton.onClick.RemoveAllListeners(); upgradeCrossbowDamageButton.onClick.AddListener(UpgradeCrossbowDamage); }
        if (upgradeCrossbowReloadButton != null) { upgradeCrossbowReloadButton.onClick.RemoveAllListeners(); upgradeCrossbowReloadButton.onClick.AddListener(UpgradeCrossbowReload); }

        if (hireKnightButton != null) { hireKnightButton.onClick.RemoveAllListeners(); hireKnightButton.onClick.AddListener(HireKnight); }
        if (hireArcherButton != null) { hireArcherButton.onClick.RemoveAllListeners(); hireArcherButton.onClick.AddListener(HireArcher); }
        if (hireSpearmanButton != null) { hireSpearmanButton.onClick.RemoveAllListeners(); hireSpearmanButton.onClick.AddListener(HireSpearman); }
        if (nextWaveButton != null) { nextWaveButton.onClick.RemoveAllListeners(); nextWaveButton.onClick.AddListener(NextWave); }
    }

    // =========================================================
    // === НОВА ЛОГІКА ДЛЯ ПЕРЕВІРКИ ПАНЕЛЕЙ ТА ПАУЗИ ===
    // =========================================================
    public bool IsAnyPanelOpen()
    {
        if (constructionPanel != null && constructionPanel.activeSelf) return true;
        if (constructionPanelNew != null && constructionPanelNew.activeSelf) return true;
        
        if (barracksUpgradePanel != null && barracksUpgradePanel.activeSelf) return true;
        if (barracksPanelNew != null && barracksPanelNew.activeSelf) return true;
        
        if (shopPanel != null && shopPanel.activeSelf) return true;
        if (shopPanelNew != null && shopPanelNew.activeSelf) return true;
        
        if (infoPanel != null && infoPanel.activeSelf) return true;
        if (metaShopPanel != null && metaShopPanel.activeSelf) return true;

        return false;
    }

    public void UpdatePauseAndOverlay()
    {
        bool anyOpen = IsAnyPanelOpen();

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(anyOpen);
        }

        if (!isDefeated)
        {
            Time.timeScale = anyOpen ? 0f : 1f;
        }
    }
    // =========================================================

    public void UpdateFormationPositions()
    {
        AssignPositionsToGroup(activeKnights.Cast<MonoBehaviour>().ToList(), 0);
        AssignPositionsToGroup(activeSpearmen.Cast<MonoBehaviour>().ToList(), 1);
        AssignPositionsToGroup(activeArchers.Cast<MonoBehaviour>().ToList(), 3);
    }

    void AssignPositionsToGroup(List<MonoBehaviour> units, int groupIndex)
    {
        if (formationStartPoint == null) return;

        for (int i = 0; i < units.Count; i++)
        {
            int rowInGroup = i / 4;
            int colInGroup = i % 4;

            float posX = formationStartPoint.position.x - (groupIndex + rowInGroup) * rowSpacing;
            float posY = formationStartPoint.position.y + (colInGroup * columnSpacing) - (1.5f * columnSpacing);

            Vector3 targetPos = new Vector3(posX, posY, 0);

            units[i].SendMessage("SetFormationPosition", targetPos, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void OnUnitDeath(GameObject unitObj, string type)
    {
        if (type == "Knight") activeKnights.Remove(unitObj.GetComponent<Knight>());
        else if (type == "Spearman") activeSpearmen.Remove(unitObj.GetComponent<Spearman>());
        else if (type == "Archer") activeArchers.Remove(unitObj.GetComponent<Archer>());

        currentUnits--;
        UpdateUI();
        UpdateFormationPositions();
    }

    IEnumerator AutoStartNextWave(float delay)
    {
        yield return new WaitForSeconds(delay);
        NextWave();
    }

    public void NextWave()
    {
        if (enemiesAlive > 0)
        {
            isWaitingForNextWave = false;
            return;
        }

        isWaitingForNextWave = false;

        if (isFirstWaveStarted)
        {
            currentWave++;
        }
        isFirstWaveStarted = true;

        waveTimer = 0;
        isWaveInProgress = true;

        if (castle != null) castle.HealMax();
        manualTarget = null;
        if(targetIndicator) targetIndicator.Hide();

        if (spawner != null) spawner.PrepareForWave(currentWave);

        GenerateWaveMilestones();
        SaveGame();
        UpdateUI();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.waveStart, 0.4f);
        }

        if (metaIncomeLevel > 0)
        {
            int bonusGold = metaIncomeLevel * 5;
            AddResource(ResourceType.Gold, bonusGold);
            ShowResourcePopup(ResourceType.Gold, bonusGold, castle.transform.position + Vector3.up * 3f);
        }

        if (spawner != null)
        {
            spawner.SpawnSquad(false);
        }
    }

    void GenerateWaveMilestones()
    {
        currentWaveMilestones.Clear();
        if (tickContainer != null)
        {
            foreach (Transform child in tickContainer)
            {
                Destroy(child.gameObject);
            }
        }

        int baseSquadCount = 2 + (currentWave / 5);
        int extraRandom = 1;

        if (currentWave % 4 == 0 && currentWave % 10 != 0)
        {
            extraRandom = Random.Range(2, 4);
        }

        int totalSquads = baseSquadCount + Random.Range(0, extraRandom + 1);
        totalSquads = Mathf.Clamp(totalSquads, 1, 6);

        int milestonesCount = totalSquads - 1;

        for (int i = 1; i <= milestonesCount; i++)
        {
            float m = (float)i / (milestonesCount + 1);
            currentWaveMilestones.Add(m);

            if (tickPrefab != null && tickContainer != null)
            {
                GameObject tick = Instantiate(tickPrefab, tickContainer);
                RectTransform rt = tick.GetComponent<RectTransform>();

                float visualPos = 1f - m;
                rt.anchorMin = new Vector2(visualPos, 0.5f);
                rt.anchorMax = new Vector2(visualPos, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }
        }

        if (currentWave % 10 == 0 || currentWave % 25 == 0) 
            currentWaveMilestones.Add(0.95f);
    }

    void SpawnEnemySquad()
    {
        bool isBossMoment = (currentWave % 10 == 0 || currentWave % 25 == 0) && currentWaveMilestones.Count <= 1;
        if (spawner != null) spawner.SpawnSquad(isBossMoment);
    }

    public void ToggleMetaShop()
    {
        if (metaShopPanel != null)
        {
            bool isActive = !metaShopPanel.activeSelf;
            metaShopPanel.SetActive(isActive);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);
            }

            if (isActive)
            {
                UpdateMetaUI();
                if (constructionPanel != null) constructionPanel.SetActive(false);
                if (constructionPanelNew != null) constructionPanelNew.SetActive(false);
                if (barracksUpgradePanel != null) barracksUpgradePanel.SetActive(false);
                if (barracksPanelNew != null) barracksPanelNew.SetActive(false);
                if (shopPanel != null) shopPanel.SetActive(false);
                if (shopPanelNew != null) shopPanelNew.SetActive(false);
                if (infoPanel != null) infoPanel.SetActive(false);
            }
            
            UpdatePauseAndOverlay(); // Оновлюємо стан паузи
        }
    }

    public void AddKillProgress(int amount)
    {
        currentKills += amount;
        bool leveledUp = false;

        while (currentKills >= killsToNextGem)
        {
            currentKills -= killsToNextGem;
            gems++;
            killsToNextGem = Mathf.RoundToInt(killsToNextGem * 1.15f); 
            leveledUp = true;
        }

        if (leveledUp && SoundManager.Instance != null && SoundManager.Instance.coinPickup != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.coinPickup); 
        }

        UpdateMetaUI();
    }

    public void BuyMetaCastleHP()
    {
        int cost = 10 + (metaCastleHpLevel * 5);
        if (gems >= cost)
        {
            gems -= cost;
            metaCastleHpLevel++;

            if (castle != null)
            {
                castle.LoadState(castle.wallLevel);
            }

            SaveGame();
            UpdateMetaUI();

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);
            }
        }
    }

    public void BuyMetaDiscount()
    {
        int cost = 15 + (metaDiscountLevel * 10);
        if (gems >= cost && metaDiscountLevel < 15)
        {
            gems -= cost;
            metaDiscountLevel++;
            knightFixedCost = Mathf.Max(10, 50 - (metaDiscountLevel * 2));
            archerFixedCost = Mathf.Max(10, 75 - (metaDiscountLevel * 2));
            spearmanFixedCost = Mathf.Max(10, 60 - (metaDiscountLevel * 2));

            SaveGame();
            UpdateMetaUI();
            UpdateUI();

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);
            }
        }
    }

    public void BuyMetaIncome()
    {
        int cost = 20 + (metaIncomeLevel * 15);
        if (gems >= cost)
        {
            gems -= cost;
            metaIncomeLevel++;

            SaveGame();
            UpdateMetaUI();

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);
            }
        }
    }

    public void UpdateMetaUI()
    {
        if (gemsText != null) gemsText.text = gems.ToString() + " 💎";
        if (gemProgressBar != null)
        {
            gemProgressBar.maxValue = killsToNextGem;
            gemProgressBar.value = currentKills;
        }
        if (gemProgressText != null) gemProgressText.text = $"{currentKills} / {killsToNextGem}";

        if (metaHpText != null) metaHpText.text = $"Castle HP (+50)\nLvl {metaCastleHpLevel}\nCost: {10 + (metaCastleHpLevel * 5)} 💎";

        if (metaDiscountText != null)
        {
            if (metaDiscountLevel >= 15) metaDiscountText.text = "Unit Discount\nMAX LEVEL";
            else metaDiscountText.text = $"Unit Discount (-2G)\nLvl {metaDiscountLevel}\nCost: {15 + (metaDiscountLevel * 10)} 💎";
        }

        if (metaIncomeText != null) metaIncomeText.text = $"Passive Income (+5G)\nLvl {metaIncomeLevel}\nCost: {20 + (metaIncomeLevel * 15)} 💎";
    }

    public void ToggleConstructionMenu()
    {
        GameObject panel = constructionPanel != null ? constructionPanel : constructionPanelNew;
        if (panel != null)
        {
            bool isActive = !panel.activeSelf;
            panel.SetActive(isActive);

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);

            if (isActive)
            {
                if (barracksUpgradePanel != null) barracksUpgradePanel.SetActive(false);
                if (barracksPanelNew != null) barracksPanelNew.SetActive(false);
                if (shopPanel != null) shopPanel.SetActive(false);
                if (shopPanelNew != null) shopPanelNew.SetActive(false);
                if (infoPanel != null) infoPanel.SetActive(false);
                if (metaShopPanel != null) metaShopPanel.SetActive(false);
                UpdateBarracksStateUI();
            }
            
            UpdatePauseAndOverlay(); // Оновлюємо стан паузи
        }
    }

    public void ToggleBarracksUpgradeMenu()
    {
        GameObject panel = barracksUpgradePanel != null ? barracksUpgradePanel : barracksPanelNew;
        if (panel != null)
        {
            bool isActive = !panel.activeSelf;
            panel.SetActive(isActive);

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);

            if (isActive)
            {
                UpdateUpgradeMenuPrice();
                UpdateUI();
                if (constructionPanel != null) constructionPanel.SetActive(false);
                if (constructionPanelNew != null) constructionPanelNew.SetActive(false);
                if (shopPanel != null) shopPanel.SetActive(false);
                if (shopPanelNew != null) shopPanelNew.SetActive(false);
                if (infoPanel != null) infoPanel.SetActive(false);
                if (metaShopPanel != null) metaShopPanel.SetActive(false);
            }
            
            UpdatePauseAndOverlay(); // Оновлюємо стан паузи
        }
    }

    public void ToggleShop()
    {
        GameObject panel = shopPanel != null ? shopPanel : shopPanelNew;
        if (panel != null)
        {
            bool isActive = !panel.activeSelf;
            panel.SetActive(isActive);

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);

            if(isActive)
            {
                if (constructionPanel != null) constructionPanel.SetActive(false);
                if (constructionPanelNew != null) constructionPanelNew.SetActive(false);
                if (barracksUpgradePanel != null) barracksUpgradePanel.SetActive(false);
                if (barracksPanelNew != null) barracksPanelNew.SetActive(false);
                if (infoPanel != null) infoPanel.SetActive(false);
                if (metaShopPanel != null) metaShopPanel.SetActive(false);
            }
            
            UpdatePauseAndOverlay(); // Оновлюємо стан паузи
        }
    }

    public void ToggleInfoPanel()
    {
        if (infoPanel != null)
        {
            bool isActive = !infoPanel.activeSelf;
            infoPanel.SetActive(isActive);

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);

            if (isActive)
            {
                if (constructionPanel != null) constructionPanel.SetActive(false);
                if (constructionPanelNew != null) constructionPanelNew.SetActive(false);
                if (barracksUpgradePanel != null) barracksUpgradePanel.SetActive(false);
                if (barracksPanelNew != null) barracksPanelNew.SetActive(false);
                if (shopPanel != null) shopPanel.SetActive(false);
                if (shopPanelNew != null) shopPanelNew.SetActive(false);
                if (metaShopPanel != null) metaShopPanel.SetActive(false);
            }
            
            UpdatePauseAndOverlay(); // Оновлюємо стан паузи
        }
    }

    public void UpdateCostUIGroup(UICostGroup costUI, ResourceType type1, int cost1, ResourceType type2 = ResourceType.Gold, int cost2 = 0, string overrideText = "")
    {
        if (costUI == null) return;

        if (!string.IsNullOrEmpty(overrideText))
        {
            if (costUI.icon1 != null) costUI.icon1.gameObject.SetActive(false);
            if (costUI.icon2 != null) costUI.icon2.gameObject.SetActive(false);
            if (costUI.text2 != null) costUI.text2.gameObject.SetActive(false);

            if (costUI.text1 != null)
            {
                costUI.text1.gameObject.SetActive(true);
                costUI.text1.text = overrideText;
                costUI.text1.alignment = TextAlignmentOptions.Center;
            }
            return;
        }

        if (costUI.icon1 != null && costUI.text1 != null)
        {
            if (cost1 > 0)
            {
                costUI.icon1.gameObject.SetActive(true);
                costUI.text1.gameObject.SetActive(true);
                costUI.icon1.sprite = GetResourceIcon(type1);
                costUI.text1.text = cost1.ToString();
                costUI.text1.alignment = TextAlignmentOptions.Left;
            }
            else
            {
                costUI.icon1.gameObject.SetActive(false);
                costUI.text1.gameObject.SetActive(false);
            }
        }

        if (costUI.icon2 != null && costUI.text2 != null)
        {
            if (cost2 > 0)
            {
                costUI.icon2.gameObject.SetActive(true);
                costUI.text2.gameObject.SetActive(true);
                costUI.icon2.sprite = GetResourceIcon(type2);
                costUI.text2.text = cost2.ToString();
                costUI.text2.alignment = TextAlignmentOptions.Left;
            }
            else
            {
                costUI.icon2.gameObject.SetActive(false);
                costUI.text2.gameObject.SetActive(false);
            }
        }
    }

    private Sprite GetResourceIcon(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Gold: return goldIcon;
            case ResourceType.Wood: return woodIcon;
            case ResourceType.Stone: return stoneIcon;
            default: return null;
        }
    }

    IEnumerator WaveWatchdog()
    {
        while (true)
        {
            yield return new WaitForSeconds(2.0f);

            if (enemiesAlive > 0)
            {
                GameObject[] realEnemies = GameObject.FindGameObjectsWithTag("Enemy");

                if (realEnemies.Length == 0)
                {
                    enemiesAlive = 0;
                    UpdateUI();

                    if (!isWaveInProgress && autoStartWaves && !isWaitingForNextWave)
                    {
                        isWaitingForNextWave = true;
                        StartCoroutine(AutoStartNextWave(timeBetweenWaves));
                    }
                }
                else if (enemiesAlive != realEnemies.Length)
                {
                    enemiesAlive = realEnemies.Length;
                    UpdateUI();
                }
            }
            else if (!isWaveInProgress && autoStartWaves && isFirstWaveStarted && !isWaitingForNextWave)
            {
                GameObject[] realEnemies = GameObject.FindGameObjectsWithTag("Enemy");

                if (realEnemies.Length == 0)
                {
                    isWaitingForNextWave = true;
                    StartCoroutine(AutoStartNextWave(timeBetweenWaves));
                }
            }
        }
    }

    void RecalculateUnits()
    {
        var knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        var archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        var spearmen = FindObjectsByType<Spearman>(FindObjectsSortMode.None);

        activeKnights = knights.Where(k => !k.CompareTag("Untagged")).ToList();
        activeArchers = archers.Where(a => !a.CompareTag("Untagged")).ToList();
        activeSpearmen = spearmen.Where(s => !s.CompareTag("Untagged")).ToList();

        currentUnits = activeKnights.Count + activeArchers.Count + activeSpearmen.Count;
        UpdateFormationPositions();
    }

    public int GetDifficultyHealth() { return Mathf.RoundToInt(baseEnemyHealth * Mathf.Pow(enemyHpGrowth, currentWave - 1)); }
    public int GetGoldReward() { return Mathf.RoundToInt(baseGoldReward * Mathf.Pow(goldRewardGrowth, currentWave)); }

    // Поточні значення
    public int GetKnightDamage()   { return GetKnightDamageAtLevel(knightLevel); }
    public int GetArcherDamage()   { return GetArcherDamageAtLevel(archerLevel); }
    public int GetSpearmanDamage() { return GetSpearmanDamageAtLevel(spearmanLevel); }
    public int GetTowerDamage()    { return GetTowerDamageAtLevel(towerLevel); }
    public int GetWallArcherDamage() { return GetWallArcherDamageAtLevel(wallArcherLevel); } 

    public int GetCrossbowDamage() { return GetCrossbowDamageAtLevel(crossbowDamageLevel); }
    public float GetCrossbowReloadTime() { return GetCrossbowReloadAtLevel(crossbowReloadLevel); }

    // Значення для будь-якого рівня
    public int GetKnightDamageAtLevel(int level)   { return Mathf.RoundToInt((10 + ((level   - 1) * 5)) * globalDamageMultiplier); }
    public int GetArcherDamageAtLevel(int level)   { return Mathf.RoundToInt((8  + ((level   - 1) * 3)) * globalDamageMultiplier); }
    public int GetSpearmanDamageAtLevel(int level) { return Mathf.RoundToInt((12 + ((level   - 1) * 6)) * globalDamageMultiplier); }
    public int GetWallArcherDamageAtLevel(int level) { return Mathf.RoundToInt((10 + ((level - 1) * 4)) * globalDamageMultiplier); } 
    public int GetTowerDamageAtLevel(int level)    { return Mathf.RoundToInt((25 + ((level   - 1) * 8)) * globalDamageMultiplier); }

    public int GetCrossbowDamageAtLevel(int level) { return Mathf.RoundToInt((crossbowBaseDamage + ((level - 1) * 12)) * globalDamageMultiplier); }
    public float GetCrossbowReloadAtLevel(int level) { return Mathf.Max(0.5f, crossbowBaseReloadTime - ((level - 1) * 0.2f)); }

    public int GetWallArcherSkinIndex()
    {
        return (wallArcherLevel - 1) / 5; 
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
        float multiplier = Mathf.Pow(1.5f, mineLevel - 1); 
        int baseCost = isWood ? mineUpgradeBaseWood : mineUpgradeBaseStone; 
        return Mathf.RoundToInt(baseCost * multiplier); 
    }
    
    private void UpdateConstructionInfoUI()
    {
        if (barracksInfoText != null)
        {
            if (barracksLevel <= 0) barracksInfoText.text = "+Unlock units / +cap";
            else
            {
                int minCap = barracksBaseCap;
                int maxCap = GetBarracksCapLimit();
                barracksInfoText.text = $"Units cap: {minCap} → {maxCap}";
            }
        }

        if (mineInfoText != null)
        {
            if (!isMineBuilt || mineLevel <= 0) mineInfoText.text = "+Gold income";
            else mineInfoText.text = $"Gold income: Lvl {mineLevel}";
        }

        if (castleInfoText != null && castle != null)
        {
            int currentHp = castle.maxHealth;
            int nextHp = currentHp + castle.hpBonusPerUpgrade;
            castleInfoText.text = $"HP: {currentHp} → {nextHp}";
        }
    }
    
    public void RegisterEnemy() 
    {
        enemiesAlive++;
        UpdateUI();
    }

    public void UnregisterEnemy()
    {
        enemiesAlive--;
        if(enemiesAlive < 0) enemiesAlive = 0;

        AddKillProgress(1);
        UpdateUI();

        if (manualTarget != null && !manualTarget.gameObject.activeInHierarchy)
        {
            manualTarget = null;
        }

        if (enemiesAlive == 0 && !isWaveInProgress && !isWaitingForNextWave)
        {
            if (waveTimerBar != null) waveTimerBar.value = 0f;

            if (autoStartWaves)
            {
                isWaitingForNextWave = true;
                StartCoroutine(AutoStartNextWave(timeBetweenWaves));
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
                {
                    Instantiate(upgradeEffectPrefab, currentBarracksObject.transform.position, Quaternion.identity);
                }
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
                {
                    Instantiate(upgradeEffectPrefab, currentMineObject.transform.position, Quaternion.identity);
                }
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
        if (barracksIconButton == null) barracksIconButton = FindUIButton("BarracksButton");
        if (barracksIconButton) barracksIconButton.gameObject.SetActive(barracksLevel > 0);

        if (buildBarracksBtnInMenu)
        {
            int costG = GetBarracksBuildingUpgradeCost(true);
            int costW = GetBarracksBuildingUpgradeCost(false);

            bool canAfford = gold >= costG && wood >= costW;
            UpdateButtonState(buildBarracksBtnInMenu, canAfford);

            Image btnImg = buildBarracksBtnInMenu.GetComponent<Image>();
            if (btnImg != null)
            {
                if (barracksLevel == 0) btnImg.sprite = buildButtonSprite;
                else btnImg.sprite = upgradeButtonSprite;
            }

            UpdateCostUIGroup(barracksCostUI, ResourceType.Gold, costG, ResourceType.Wood, costW);
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

            if (SoundManager.Instance != null && SoundManager.Instance.unitUpgradeSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);

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
        int currentCap = GetBarracksCapLimit();

        if (maxUnits >= currentCap)
        {
            UpdateCostUIGroup(upgradeLimitCostUI, ResourceType.Gold, 0, ResourceType.Wood, 0, "MAX");

            if (upgradeLimitInfoText != null)
            {
                upgradeLimitInfoText.text =
                    $"Units cap: {maxUnits} (MAX)\n" +
                    "Upgrade Barracks to unlock more unit slots";
            }
        }
        else
        {
            UpdateCostUIGroup(upgradeLimitCostUI, ResourceType.Gold, GetSlotUpgradeCost());

            if (upgradeLimitInfoText != null)
            {
                int nextLimit = maxUnits + 1;
                upgradeLimitInfoText.text = $"Units cap: {maxUnits} → {nextLimit}";
            }
        }

        if (isSpearmanUnlocked)
        {
            UpdateCostUIGroup(unlockSpearmanCostUI, ResourceType.Gold, 0, ResourceType.Wood, 0, "UNLOCKED");
        }
        else
        {
            UpdateCostUIGroup(unlockSpearmanCostUI, ResourceType.Gold, spearmanUnlockCost);
        }

        UpdateSpearmanLockUI();
    }

    void UpdateSpearmanLockUI()
    {
        bool unlocked = isSpearmanUnlocked;

        if (spearmanLockIcon != null)
        {
            spearmanLockIcon.gameObject.SetActive(!unlocked);
        }

        if (spearmanRowGroup != null)
        {
            if (dimSpearmanWhenLocked)
            {
                spearmanRowGroup.alpha = unlocked ? 1f : 0.45f;
            }

            spearmanRowGroup.interactable = unlocked;
            spearmanRowGroup.blocksRaycasts = unlocked;
        }
    }

    public void UnlockSpearman()
    {
        if (isSpearmanUnlocked) return;

        if (gold >= spearmanUnlockCost)
        {
            gold -= spearmanUnlockCost;
            isSpearmanUnlocked = true;

            if (SoundManager.Instance != null && SoundManager.Instance.unitUpgradeSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);

            SaveGame();
            UpdateUI();
            UpdateUpgradeMenuPrice();
            UpdateSpearmanLockUI();
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    public void UpgradeKnights()
    {
        if(gold >= knightUpgradeCost)
        {
            gold -= knightUpgradeCost;
            knightLevel++;
            knightUpgradeCost = (int)(knightUpgradeCost * upgradeCostGrowth);

            if (SoundManager.Instance != null && SoundManager.Instance.unitUpgradeSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);

            SaveGame();
            UpdateUI();
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    public void UpgradeArchers()
    {
        if(gold >= archerUpgradeCost)
        {
            gold -= archerUpgradeCost;
            archerLevel++;
            archerUpgradeCost = (int)(archerUpgradeCost * upgradeCostGrowth);

            if (SoundManager.Instance != null && SoundManager.Instance.unitUpgradeSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);

            SaveGame();
            UpdateUI();
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    public void UpgradeSpearman()
    {
        if(gold >= spearmanUpgradeCost)
        {
            gold -= spearmanUpgradeCost;
            spearmanLevel++;
            spearmanUpgradeCost = (int)(spearmanUpgradeCost * upgradeCostGrowth);

            if (SoundManager.Instance != null && SoundManager.Instance.unitUpgradeSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);

            SaveGame();
            UpdateUI();
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }
    
    public void UpgradeWallArchers()
    {
        if(gold >= wallArcherUpgradeCost)
        {
            gold -= wallArcherUpgradeCost;
            wallArcherLevel++;
            wallArcherUpgradeCost = (int)(wallArcherUpgradeCost * upgradeCostGrowth);

            if (SoundManager.Instance != null && SoundManager.Instance.unitUpgradeSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);

            WallArcher[] archersOnWall = FindObjectsByType<WallArcher>(FindObjectsSortMode.None);
            foreach(var wa in archersOnWall)
            {
                wa.UpdateStats();
            }

            SaveGame();
            UpdateUI();
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    public void UpgradeCrossbowDamage()
    {
        if (gold >= crossbowDamageCostGold)
        {
            gold -= crossbowDamageCostGold;
            crossbowDamageLevel++;
            crossbowDamageCostGold = (int)(crossbowDamageCostGold * upgradeCostGrowth);

            if (SoundManager.Instance != null && SoundManager.Instance.unitUpgradeSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);

            SaveGame();
            UpdateUI();
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    public void UpgradeCrossbowReload()
    {
        if (gold >= crossbowReloadCostGold)
        {
            gold -= crossbowReloadCostGold;
            crossbowReloadLevel++;
            crossbowReloadCostGold = (int)(crossbowReloadCostGold * upgradeCostGrowth);

            if (SoundManager.Instance != null && SoundManager.Instance.unitUpgradeSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);

            SaveGame();
            UpdateUI();
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    public void HireKnight()
    {
        GameObject unit = TryHireUnit(knightPrefab, knightFixedCost);

        if (unit != null)
        {
            activeKnights.Add(unit.GetComponent<Knight>());
            UpdateFormationPositions();
        }
    }

    public void HireArcher()
    {
        GameObject unit = TryHireUnit(archerPrefab, archerFixedCost);

        if (unit != null)
        {
            activeArchers.Add(unit.GetComponent<Archer>());
            UpdateFormationPositions();
        }
    }

    public void HireSpearman()
    {
        GameObject unit = TryHireUnit(spearmanPrefab, spearmanFixedCost);

        if (unit != null)
        {
            activeSpearmen.Add(unit.GetComponent<Spearman>());
            UpdateFormationPositions();
        }
    }

    private GameObject TryHireUnit(GameObject prefab, int cost)
    {
        if (Time.time - _lastHireTime < 0.1f) return null;

        _lastHireTime = Time.time;

        if(currentUnits >= maxUnits || gold < cost) return null;

        gold -= cost;
        currentUnits++;
        UpdateUI();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);

        GameObject newUnit = Instantiate(prefab, unitSpawnPoint.position, Quaternion.identity);

        SaveGame();
        return newUnit;
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

            if (upgradeEffectPrefab != null && towerTransform != null)
            {
                Instantiate(upgradeEffectPrefab, towerTransform.position, Quaternion.identity);
            }

            if (SoundManager.Instance != null && SoundManager.Instance.unitUpgradeSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);
            else if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.constructionSound);

            SaveGame();
            UpdateUI();
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    public void UpgradeCastleHP()
    {
        if (castle == null) return;

        int cost = castle.GetUpgradeCost();

        if (gold >= cost)
        {
            gold -= cost;
            castle.UpgradeCastle();

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.constructionSound);

            SaveGame();
            UpdateUI();
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

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
            case ResourceType.Gold:
                icon = goldIcon;
                color = Color.yellow;
                break;
            case ResourceType.Wood:
                icon = woodIcon;
                color = new Color(0.6f,0.3f,0f);
                break;
            case ResourceType.Stone:
                icon = stoneIcon;
                color = Color.gray;
                break;
        }

        GameObject popup = Instantiate(resourcePopupPrefab, position + Vector3.up, Quaternion.identity);
        popup.GetComponent<DamagePopup>().SetupResource(icon, amount, color);
    }

    public void AddResource(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Gold: gold += amount; break;
            case ResourceType.Wood: wood += amount; break;
            case ResourceType.Stone: stone += amount; break;
        }

        if(amount > 0) SaveGame();

        UpdateUI();

        if (type == ResourceType.Gold && amount > 0 && Time.time > 1f && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.coinPickup);
        }
    }

    void ClearProjectiles()
    {
        foreach (var p in GameObject.FindGameObjectsWithTag("Projectile"))
        {
            Destroy(p);
        }
    }

    void ClearDeadUnits()
    {
        foreach (var k in FindObjectsByType<Knight>(FindObjectsSortMode.None))
            if (k.CompareTag("Untagged")) Destroy(k.gameObject);

        foreach (var a in FindObjectsByType<Archer>(FindObjectsSortMode.None))
            if (a.CompareTag("Untagged")) Destroy(a.gameObject);

        foreach (var s in FindObjectsByType<Spearman>(FindObjectsSortMode.None))
            if (s.CompareTag("Untagged")) Destroy(s.gameObject);
    }

    void ClearDeadEnemies()
    {
        foreach (var e in FindObjectsByType<EnemyStats>(FindObjectsSortMode.None))
            if (e.CompareTag("Untagged")) Destroy(e.gameObject);

        foreach (var go in GameObject.FindGameObjectsWithTag("Untagged"))
        {
            if (go.GetComponent<EnemySpearman>() || go.GetComponent<EnemyHorse>() || go.GetComponent<EnemyArcher>() || go.GetComponent<Guard>() || go.GetComponent<Boss>() || go.GetComponent<Cart>())
            {
                Destroy(go);
            }
        }
    }

    IEnumerator RecalculateUnitsNextFrame()
    {
        yield return null;
        RecalculateUnits();
        UpdateUI();
    }

    public void Defeat()
    {
        Time.timeScale = 1f;

        if(defeatPanel) defeatPanel.SetActive(true);

        if (defeatText != null) defeatText.text = "Avanpost control was lost";

        ClearProjectiles();
        SaveGame();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.defeat);

        StartCoroutine(DefeatAutoRetry());
    }

    private IEnumerator DefeatAutoRetry()
    {
        float duration = 0.8f; 
        float t = 0f;

        CanvasGroup cg = null;
        if (defeatPanel != null)
        {
            cg = defeatPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = defeatPanel.AddComponent<CanvasGroup>();
        }

        if (cg != null)
        {
            cg.alpha = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }
            cg.alpha = 1f;
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.8f);
        }

        yield return new WaitForSecondsRealtime(2.5f);

        OnRetryButton();
    }

    public void OnRetryButton()
    {
        Time.timeScale = 1;
        isDefeated = false;
        if(defeatPanel) defeatPanel.SetActive(false);

        if (gold < 300) gold = 300;

        if (spawner != null) spawner.StopSpawning();
        if (spawner != null) spawner.ClearEnemies();

        ClearDeadEnemies();
        ClearDeadUnits();

        if (castle == null) castle = FindFirstObjectByType<Wall>();
        if (castle != null) castle.HealMax();

        if (currentSpikes != null) Destroy(currentSpikes.gameObject);
        currentSpikes = null;

        manualTarget = null;
        if(targetIndicator) targetIndicator.Hide();

        ClearProjectiles();
        isResettingUnits = true;

        foreach(var u in FindObjectsByType<Knight>(FindObjectsSortMode.None)) Destroy(u.gameObject);
        activeKnights.Clear();

        foreach(var a in FindObjectsByType<Archer>(FindObjectsSortMode.None)) Destroy(a.gameObject);
        activeArchers.Clear();

        foreach(var s in FindObjectsByType<Spearman>(FindObjectsSortMode.None)) Destroy(s.gameObject);
        activeSpearmen.Clear();

        if (currentMineObject != null) Destroy(currentMineObject);
        if (isMineBuilt) SpawnMineObject();

        currentUnits = 0;
        enemiesAlive = 0;
        isResettingUnits = false;
        isWaitingForNextWave = false;

        waveTimer = 0f;
        if (waveTimerBar != null)
        {
            waveTimerBar.gameObject.SetActive(true);
            waveTimerBar.value = 0f;
        }

        if (spawner != null) spawner.PrepareForWave(currentWave);

        GenerateWaveMilestones();
        isWaveInProgress = true;

        UpdateUI();
        UpdateBarracksStateUI();
        
        UpdatePauseAndOverlay(); // Оновлюємо стан паузи (вимикаємо, якщо панелі закриті)
        StartCoroutine(RecalculateUnitsNextFrame());
    }

    public void SaveGame()
    {
        PlayerPrefs.SetInt("SavedGold", gold);
        PlayerPrefs.SetInt("SavedWood", wood);
        PlayerPrefs.SetInt("SavedStone", stone);
        PlayerPrefs.SetInt("SavedWave", currentWave);

        PlayerPrefs.SetInt("SavedKnightLevel", knightLevel);
        PlayerPrefs.SetInt("SavedArcherLevel", archerLevel);
        PlayerPrefs.SetInt("SavedSpearmanLevel", spearmanLevel);
        PlayerPrefs.SetInt("SavedWallArcherLevel", wallArcherLevel); 

        PlayerPrefs.SetInt("SavedKnightCost", knightUpgradeCost);
        PlayerPrefs.SetInt("SavedArcherCost", archerUpgradeCost);
        PlayerPrefs.SetInt("SavedSpearmanCost", spearmanUpgradeCost);
        PlayerPrefs.SetInt("SavedWallArcherCost", wallArcherUpgradeCost); 

        PlayerPrefs.SetInt("SavedMaxUnits", maxUnits);
        PlayerPrefs.SetInt("SavedBarracksLevel", barracksLevel);
        PlayerPrefs.SetInt("SavedSpearmanUnlocked", isSpearmanUnlocked ? 1 : 0);

        PlayerPrefs.SetInt("SavedMineBuilt", isMineBuilt ? 1 : 0);
        PlayerPrefs.SetInt("SavedMineLevel", mineLevel);

        if (castle != null) PlayerPrefs.SetInt("SavedCastleLevel", castle.wallLevel);

        PlayerPrefs.SetInt("SavedTowerLevel", towerLevel);
        PlayerPrefs.SetInt("SavedTowerWoodCost", towerWoodCost);
        PlayerPrefs.SetInt("SavedTowerStoneCost", towerStoneCost);

        PlayerPrefs.SetInt("SavedCrossbowDmgLevel", crossbowDamageLevel);
        PlayerPrefs.SetInt("SavedCrossbowReloadLevel", crossbowReloadLevel);
        PlayerPrefs.SetInt("SavedCrossbowDmgCost", crossbowDamageCostGold);
        PlayerPrefs.SetInt("SavedCrossbowReloadCost", crossbowReloadCostGold);

        PlayerPrefs.SetInt("SavedGems", gems);
        PlayerPrefs.SetInt("SavedCurrentKills", currentKills);
        PlayerPrefs.SetInt("SavedKillsToNextGem", killsToNextGem);
        PlayerPrefs.SetInt("MetaCastleHP", metaCastleHpLevel);
        PlayerPrefs.SetInt("MetaDiscount", metaDiscountLevel);
        PlayerPrefs.SetInt("MetaIncome", metaIncomeLevel);

        SaveUnits();
        PlayerPrefs.Save();
    }

    void SaveUnits()
    {
        GameSaveData data = new GameSaveData();

        foreach (var k in FindObjectsByType<Knight>(FindObjectsSortMode.None))
        {
            if (k.CompareTag("Untagged")) continue;
            UnitSaveData u = new UnitSaveData();
            u.unitType = "Knight";
            u.posX = k.transform.position.x;
            u.posY = k.transform.position.y;
            u.currentHealth = k.currentHealth;
            data.units.Add(u);
        }

        foreach (var a in FindObjectsByType<Archer>(FindObjectsSortMode.None))
        {
            if (a.CompareTag("Untagged")) continue;
            UnitSaveData u = new UnitSaveData();
            u.unitType = "Archer";
            u.posX = a.transform.position.x;
            u.posY = a.transform.position.y;
            u.currentHealth = a.currentHealth;
            data.units.Add(u);
        }

        foreach (var s in FindObjectsByType<Spearman>(FindObjectsSortMode.None))
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
        wallArcherLevel = PlayerPrefs.GetInt("SavedWallArcherLevel", 1); 

        knightUpgradeCost = PlayerPrefs.GetInt("SavedKnightCost", 100);
        archerUpgradeCost = PlayerPrefs.GetInt("SavedArcherCost", 120);
        spearmanUpgradeCost = PlayerPrefs.GetInt("SavedSpearmanCost", 110);
        wallArcherUpgradeCost = PlayerPrefs.GetInt("SavedWallArcherCost", 150); 

        maxUnits = PlayerPrefs.GetInt("SavedMaxUnits", 5);
        barracksLevel = PlayerPrefs.GetInt("SavedBarracksLevel", 0);
        if (barracksLevel > 0) isBarracksBuilt = true;

        isSpearmanUnlocked = PlayerPrefs.GetInt("SavedSpearmanUnlocked", 0) == 1;
        if (barracksLevel == 0)
        {
            isSpearmanUnlocked = false;
        }

        isMineBuilt = PlayerPrefs.GetInt("SavedMineBuilt", 0) == 1;
        mineLevel = PlayerPrefs.GetInt("SavedMineLevel", 0);
        if (mineLevel > 0) isMineBuilt = true;

        towerLevel = PlayerPrefs.GetInt("SavedTowerLevel", 1);
        towerWoodCost = PlayerPrefs.GetInt("SavedTowerWoodCost", 50);
        towerStoneCost = PlayerPrefs.GetInt("SavedTowerStoneCost", 20);

        crossbowDamageLevel = PlayerPrefs.GetInt("SavedCrossbowDmgLevel", 1);
        crossbowReloadLevel = PlayerPrefs.GetInt("SavedCrossbowReloadLevel", 1);
        crossbowDamageCostGold = PlayerPrefs.GetInt("SavedCrossbowDmgCost", 150);
        crossbowReloadCostGold = PlayerPrefs.GetInt("SavedCrossbowReloadCost", 150);

        gems = PlayerPrefs.GetInt("SavedGems", 0);
        currentKills = PlayerPrefs.GetInt("SavedCurrentKills", 0);
        killsToNextGem = PlayerPrefs.GetInt("SavedKillsToNextGem", 25);
        if (killsToNextGem < 25) killsToNextGem = 25; 
        metaCastleHpLevel = PlayerPrefs.GetInt("MetaCastleHP", 0);
        metaDiscountLevel = PlayerPrefs.GetInt("MetaDiscount", 0);
        metaIncomeLevel = PlayerPrefs.GetInt("MetaIncome", 0);

        knightFixedCost = Mathf.Max(10, 50 - (metaDiscountLevel * 2));
        archerFixedCost = Mathf.Max(10, 75 - (metaDiscountLevel * 2));
        spearmanFixedCost = Mathf.Max(10, 60 - (metaDiscountLevel * 2));

        if (isBarracksBuilt) SpawnBarracksObject();
        if (isMineBuilt) SpawnMineObject();

        if (castle == null) castle = FindFirstObjectByType<Wall>();

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
        gold = 0;
        wood = 0;
        stone = 0;
        currentWave = 1;
        maxUnits = 5;
        knightLevel = 1;
        archerLevel = 1;
        spearmanLevel = 1;
        wallArcherLevel = 1; 
        wallArcherUpgradeCost = 150; 

        isSpearmanUnlocked = false;
        isMineBuilt = false;
        mineLevel = 0;

        isBarracksBuilt = false;
        barracksLevel = 0;
        enemiesAlive = 0;
        currentUnits = 0;
        isWaitingForNextWave = false;
        
        crossbowDamageLevel = 1;
        crossbowReloadLevel = 1;
        crossbowDamageCostGold = 150;
        crossbowReloadCostGold = 150;

        gems = 0;
        currentKills = 0;
        killsToNextGem = 25;
        metaCastleHpLevel = 0;
        metaDiscountLevel = 0;
        metaIncomeLevel = 0;

        if (currentBarracksObject != null) Destroy(currentBarracksObject);
        if (currentMineObject != null) Destroy(currentMineObject);
        if (currentSpikes != null) Destroy(currentSpikes.gameObject);

        currentSpikes = null;
        manualTarget = null;
        if(targetIndicator) targetIndicator.Hide();

        if (waveTimerBar != null) waveTimerBar.value = 0f;

        UpdateUI();
        UpdateBarracksStateUI();
        UpdateMetaUI();
        UpdatePauseAndOverlay();

        Debug.Log("Збереження видалено! (Кеш очищено)");
    }

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

        UpdateCostUIGroup(towerCostUI, ResourceType.Wood, towerWoodCost, ResourceType.Stone, towerStoneCost);

        if (towerLevelText)
        {
            int currentTowerDmg = GetTowerDamageAtLevel(towerLevel);
            int nextTowerDmg    = GetTowerDamageAtLevel(towerLevel + 1);
            towerLevelText.text = $"Tower Lvl {towerLevel}\nDMG: {currentTowerDmg} → <color=#66FF66>{nextTowerDmg}</color>\n{towerWoodCost} W / {towerStoneCost} S";
        }
        
        if (crossbowDamageText != null)
        {
            int curDmg = GetCrossbowDamageAtLevel(crossbowDamageLevel);
            int nextDmg = GetCrossbowDamageAtLevel(crossbowDamageLevel + 1);
            
            int percentDmg = Mathf.RoundToInt(((float)(nextDmg - curDmg) / curDmg) * 100f);

            crossbowDamageText.text = 
                $"DMG: {curDmg} → <color=#66FF66>{nextDmg}</color>\n" +
                $"<size=80%>+{percentDmg}% damage</size>";
        }

        if (crossbowReloadText != null)
        {
            float curRel = GetCrossbowReloadAtLevel(crossbowReloadLevel);
            float nextRel = GetCrossbowReloadAtLevel(crossbowReloadLevel + 1);
            
            int percentRel = Mathf.RoundToInt(((curRel - nextRel) / curRel) * 100f);

            crossbowReloadText.text = 
                $"Reload: {curRel:F2}s → <color=#66FF66>{nextRel:F2}s</color>\n" +
                $"<size=80%>-{percentRel}% reload time</size>";
        }

        UpdateCostUIGroup(crossbowDamageCostUI, ResourceType.Gold, crossbowDamageCostGold);
        UpdateCostUIGroup(crossbowReloadCostUI, ResourceType.Gold, crossbowReloadCostGold);

        UpdateButtonState(upgradeCrossbowDamageButton, gold >= crossbowDamageCostGold);
        UpdateButtonState(upgradeCrossbowReloadButton, (gold >= crossbowReloadCostGold) && (GetCrossbowReloadAtLevel(crossbowReloadLevel) > 0.5f)); 

        if (castleUpgradeCostUI != null && castle != null)
        {
            UpdateCostUIGroup(castleUpgradeCostUI, ResourceType.Gold, castle.GetUpgradeCost());
        }

        UpdateConstructionInfoUI();

        if (knightLevelText)
        {
            int currentDmg = GetKnightDamageAtLevel(knightLevel);
            int nextDmg    = GetKnightDamageAtLevel(knightLevel + 1);
            knightLevelText.text =
                $"Knights Lvl {knightLevel}\nDMG: {currentDmg} → <color=#66FF66>{nextDmg}</color>\n" +
                "<size=70%><color=#66FF66>Counters Archers</color>\n" +
                "<color=#FF6666>Vulnerable to Spearmen</color></size>";
        }

        if (archerLevelText)
        {
            int currentDmg = GetArcherDamageAtLevel(archerLevel);
            int nextDmg    = GetArcherDamageAtLevel(archerLevel + 1);
            archerLevelText.text =
                $"Archers Lvl {archerLevel}\nDMG: {currentDmg} → <color=#66FF66>{nextDmg}</color>\n" +
                "<size=70%><color=#66FF66>Counters Spearmen</color>\n" +
                "<color=#FF6666>Vulnerable to Knights & Cavalry</color></size>";
        }

        if (spearmanLevelText)
        {
            int currentDmg = GetSpearmanDamageAtLevel(spearmanLevel);
            int nextDmg    = GetSpearmanDamageAtLevel(spearmanLevel + 1);
            spearmanLevelText.text =
                $"Spearmen Lvl {spearmanLevel}\nDMG: {currentDmg} → <color=#66FF66>{nextDmg}</color>\n" +
                "<size=70%><color=#66FF66>Counters Cavalry (x2 DMG)</color>\n" +
                "<color=#FF6666>Vulnerable to Archers</color></size>";
        }
        
        if (wallArcherLevelText != null)
        {
            int currentDmg = GetWallArcherDamageAtLevel(wallArcherLevel);
            int nextDmg    = GetWallArcherDamageAtLevel(wallArcherLevel + 1);
            
            int levelsUntilSkin = 5 - ((wallArcherLevel - 1) % 5);

            wallArcherLevelText.text =
                $"Wall Archers Lvl {wallArcherLevel}\n" +
                $"DMG: {currentDmg} → <color=#66FF66>{nextDmg}</color>\n" +
                $"<size=70%><color=#FFCC00>New Skin in {levelsUntilSkin} level(s)!</color></size>";
        }

        UpdateCostUIGroup(knightUpgradeCostUI, ResourceType.Gold, knightUpgradeCost);
        UpdateCostUIGroup(archerUpgradeCostUI, ResourceType.Gold, archerUpgradeCost);
        UpdateCostUIGroup(spearmanUpgradeCostUI, ResourceType.Gold, spearmanUpgradeCost);
        
        UpdateCostUIGroup(wallArcherUpgradeCostUI, ResourceType.Gold, wallArcherUpgradeCost);

        bool canHireKnight = (gold >= knightFixedCost) && (currentUnits < maxUnits);
        bool canHireArcher = (gold >= archerFixedCost) && (currentUnits < maxUnits);
        bool canHireSpearman = (gold >= spearmanFixedCost) && (currentUnits < maxUnits);

        UpdateButtonState(hireKnightButton, canHireKnight);
        UpdateButtonState(hireArcherButton, canHireArcher);

        if (hireSpearmanButton != null)
        {
            bool canSeeSpearman = isSpearmanUnlocked && barracksLevel > 0;
            hireSpearmanButton.gameObject.SetActive(canSeeSpearman);
            UpdateButtonState(hireSpearmanButton, canHireSpearman && canSeeSpearman);
        }

        UpdateButtonState(towerButton, (wood >= towerWoodCost && stone >= towerStoneCost));

        if (castleUpgradeButton != null && castle != null)
        {
            UpdateButtonState(castleUpgradeButton, gold >= castle.GetUpgradeCost());
        }

        UpdateButtonState(hammerButton, true);
        UpdateButtonState(upgradeKnightButton, gold >= knightUpgradeCost);
        UpdateButtonState(upgradeArcherButton, gold >= archerUpgradeCost);
        
        UpdateButtonState(upgradeWallArcherButton, gold >= wallArcherUpgradeCost);

        if (upgradeSpearmanButton != null)
        {
            UpdateButtonState(upgradeSpearmanButton, (gold >= spearmanUpgradeCost) && isSpearmanUnlocked);
        }

        if (unlockSpearmanButton != null)
        {
            if (unlockSpearmanRowGroup != null)
            {
                unlockSpearmanRowGroup.gameObject.SetActive(isBarracksBuilt);
            }
            else
            {
                unlockSpearmanButton.gameObject.SetActive(isBarracksBuilt);
            }

            if (isSpearmanUnlocked)
            {
                UpdateButtonState(unlockSpearmanButton, false);
                UpdateCostUIGroup(unlockSpearmanCostUI, ResourceType.Gold, 0, ResourceType.Wood, 0, "UNLOCKED");

                if (unlockSpearmanRowGroup != null)
                {
                    unlockSpearmanRowGroup.alpha = 0.5f;
                    unlockSpearmanRowGroup.interactable = false;
                    unlockSpearmanRowGroup.blocksRaycasts = false;
                }
            }
            else
            {
                UpdateButtonState(unlockSpearmanButton, gold >= spearmanUnlockCost);
                UpdateCostUIGroup(unlockSpearmanCostUI, ResourceType.Gold, spearmanUnlockCost);

                if (unlockSpearmanRowGroup != null)
                {
                    unlockSpearmanRowGroup.alpha = 1f;
                    unlockSpearmanRowGroup.interactable = true;
                    unlockSpearmanRowGroup.blocksRaycasts = true;
                }
            }
        }

        if (buildBarracksBtnInMenu != null)
        {
            int costG = GetBarracksBuildingUpgradeCost(true);
            int costW = GetBarracksBuildingUpgradeCost(false);
            UpdateButtonState(buildBarracksBtnInMenu, gold >= costG && wood >= costW);
            UpdateBarracksStateUI();
        }

        if (barracksIconButton != null)
        {
            barracksIconButton.gameObject.SetActive(barracksLevel > 0);
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
                if (mineLevel == 0) mineImg.sprite = buildButtonSprite;
                else mineImg.sprite = upgradeButtonSprite;
            }

            UpdateCostUIGroup(mineCostUI, ResourceType.Wood, mWood, ResourceType.Stone, mStone);
        }

        if (openShopButton != null)
        {
            UpdateButtonState(openShopButton, true);
        }

        if (nextWaveButton != null)
        {
            UpdateButtonState(nextWaveButton, enemiesAlive <= 0 && !isWaveInProgress && !isWaitingForNextWave);
        }

        if (buildSpikesButton != null)
        {
            bool canBuild = (wood >= spikesWoodCost) && (currentSpikes == null);
            UpdateButtonState(buildSpikesButton, canBuild);

            if (currentSpikes != null)
            {
                UpdateCostUIGroup(spikesCostUI, ResourceType.Wood, 0, ResourceType.Stone, 0, "BUILT");
            }
            else
            {
                UpdateCostUIGroup(spikesCostUI, ResourceType.Wood, spikesWoodCost);
            }
        }

        if (estimatedIncomeText != null && spawner != null)
        {
            int enemiesGold = spawner.GetEstimatedGoldFromEnemies();
            int waveBonus = GetGoldReward();
            int totalEst = enemiesGold + waveBonus;

            estimatedIncomeText.text = $"+{totalEst} G";
        }

        UpdateSpearmanLockUI();
    }

    void UpdateButtonState(Button btn, bool isActive)
    {
        if (btn == null) return;

        btn.interactable = isActive;
        btn.transition = Selectable.Transition.ColorTint;

        // Затемнення для всіх елементів усередині кнопки
        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = isActive ? 1f : 0.5f;
        }

        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = Color.white;

        ColorBlock cb = btn.colors;
        Color targetColor = isActive ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
        Color pressedColor = isActive ? new Color(0.7f, 0.7f, 0.7f, 1f) : targetColor;

        cb.normalColor = targetColor;
        cb.highlightedColor = targetColor;
        cb.pressedColor = pressedColor;
        cb.selectedColor = targetColor;
        cb.disabledColor = targetColor;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.05f;

        btn.colors = cb;
    }
}