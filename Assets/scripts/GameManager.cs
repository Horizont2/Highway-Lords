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

    [Header("=== UI: ГОЛОВНІ ПАНЕЛІ (НОВІ) ===")]
    public GameObject constructionPanel;
    public GameObject barracksUpgradePanel;
    public GameObject shopPanel;

    [Header("UI: Панелі (fallback поля)")]
    public GameObject constructionPanelNew;
    public GameObject barracksPanelNew;
    public GameObject shopPanelNew;

    [Header("UI: Кнопки відкриття (ПРИВ'ЯЗАТИ В ІНСПЕКТОРІ)")]
    public Button hammerButton;
    public Button barracksIconButton;
    public Button openShopButton;

    [Header("UI: Кнопки хрестики (ПРИВ'ЯЗАТИ В ІНСПЕКТОРІ)")]
    public Button closeConstructionBtn;
    public Button closeBarracksBtn;
    public Button closeShopBtn;

    [Header("UI: Панель Будівництва (Construction)")]
    public Button buildBarracksBtnInMenu;
    public Button buildSpikesButton;
    public Button buildMineButton;
    public Button towerButton;
    public Button castleUpgradeButton;

    // Нові групи для інтерфейсу вартості
    public UICostGroup barracksCostUI;
    public UICostGroup mineCostUI;
    public UICostGroup spikesCostUI;
    public UICostGroup towerCostUI;
    public UICostGroup castleUpgradeCostUI;

    [Header("UI: Панель Казарм (Barracks)")]
    public Button upgradeLimitButton;
    public UICostGroup upgradeLimitCostUI;
    public Button unlockSpearmanButton;
    public UICostGroup unlockSpearmanCostUI;

    [Header("UI: Панель Кузні (Shop/Forge)")]
    public Button upgradeKnightButton;
    public Button upgradeArcherButton;
    public Button upgradeSpearmanButton;

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
    public int spikesWoodCost = 100;

    [HideInInspector] public Spikes currentSpikes;

    [Header("UI: Головний екран (Найм)")]
    public Button hireKnightButton;
    public Button hireArcherButton;
    public Button hireSpearmanButton;

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
    public TMP_Text towerLevelText; // Текст рівня/шкоди вежі

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
    private int _lastHireFrame = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        LoadGame();
    }

    void Start()
    {
        SetupAllButtonColors();
        ResolveUIRefs();
        ResolveSpearmanLockUI();
        WireExplicitButtons(); // Надійна пряма прив'язка

        RecalculateUnits();

        enemiesAlive = 0;

        // Вимикаємо панелі на старті
        if (constructionPanel) constructionPanel.SetActive(false);
        if (barracksUpgradePanel) barracksUpgradePanel.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false);
        if (defeatPanel) defeatPanel.SetActive(false);

        UpdateUI();
        UpdateBarracksStateUI();
        UpdateSpearmanLockUI();

        if (waveTimerBar != null)
        {
            waveTimerBar.gameObject.SetActive(true);
            waveTimerBar.maxValue = 1f;
            waveTimerBar.value = 1f;
        }

        if (autoStartWaves)
        {
            StartCoroutine(AutoStartNextWave(3.0f));
        }

        StartCoroutine(WaveWatchdog());
    }

    void ResolveUIRefs()
    {
        // HUD buttons
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

        // Panels: prefer set panel, fallback to *_New
        var construction = constructionPanel != null ? constructionPanel : constructionPanelNew;
        var barracks = barracksUpgradePanel != null ? barracksUpgradePanel : barracksPanelNew;
        var shop = shopPanel != null ? shopPanel : shopPanelNew;

        // Construction panel row buttons
        if (buildBarracksBtnInMenu == null) buildBarracksBtnInMenu = FindButtonInRow(construction, "Barracks_Row");
        if (buildSpikesButton == null) buildSpikesButton = FindButtonInRow(construction, "Spikes_Row");
        if (buildMineButton == null) buildMineButton = FindButtonInRow(construction, "Mine_Row");
        if (castleUpgradeButton == null) castleUpgradeButton = FindButtonInRow(construction, "Tower Upgrade_Row", "Castle Upgrade_Row");

        if (castleUpgradeCostUI == null) castleUpgradeCostUI = FindCostGroupInRow(construction, "Tower Upgrade_Row", "Castle Upgrade_Row");

        // Barracks panel
        if (upgradeLimitButton == null) upgradeLimitButton = FindButtonInRow(barracks, "Barracks_Row", "Upgrade Units Limit_Row");
        if (unlockSpearmanButton == null) unlockSpearmanButton = FindButtonInRow(barracks, "Spikes_Row", "Unlock Spearman_Row");

        // Shop panel
        if (upgradeKnightButton == null) upgradeKnightButton = FindButtonInRow(shop, "Barracks_Row", "Knights_Row");
        if (upgradeArcherButton == null) upgradeArcherButton = FindButtonInRow(shop, "Spikes_Row", "Archers_Row");
        if (upgradeSpearmanButton == null) upgradeSpearmanButton = FindButtonInRow(shop, "Mine_Row", "Spearmen_Row");

        // Close buttons (inside panels)
        if (closeConstructionBtn == null) closeConstructionBtn = FindUIButtonInPanel(construction, "CloseButton");
        if (closeBarracksBtn == null) closeBarracksBtn = FindUIButtonInPanel(barracks, "CloseButton");
        if (closeShopBtn == null) closeShopBtn = FindUIButtonInPanel(shop, "CloseButton");
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
        foreach (var tr in t) if (tr.name == name) return tr.GetComponent<Button>();
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
            // pick cost icons/texts by name
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
            if (t.name == name) return t;
        return null;
    }

    void ResolveSpearmanLockUI()
    {
        if (spearmanRowGroup != null) return;
        var shop = shopPanel != null ? shopPanel : shopPanelNew;
        if (shop == null) return;

        // Try find row by text containing "Spearman"
        var texts = shop.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            if (t.text != null && t.text.ToLower().Contains("spear"))
            {
                var row = t.transform;
                // climb to row
                for (int i = 0; i < 3 && row.parent != null; i++) row = row.parent;
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
                break;
            }
        }
    }

    // === ПРЯМА ТА НАДІЙНА ПРИВ'ЯЗКА КНОПОК ===
    void WireExplicitButtons()
    {
        // Відкриття панелей
        if (hammerButton != null)
        {
            hammerButton.onClick.RemoveAllListeners();
            hammerButton.onClick.AddListener(ToggleConstructionMenu);
        }

        if (barracksIconButton != null)
        {
            barracksIconButton.onClick.RemoveAllListeners();
            barracksIconButton.onClick.AddListener(ToggleBarracksUpgradeMenu);
        }

        if (openShopButton != null)
        {
            openShopButton.onClick.RemoveAllListeners();
            openShopButton.onClick.AddListener(ToggleShop);
        }

        // Кнопки закриття (хрестики)
        if (closeConstructionBtn != null)
        {
            closeConstructionBtn.onClick.RemoveAllListeners();
            closeConstructionBtn.onClick.AddListener(ToggleConstructionMenu);
        }

        if (closeBarracksBtn != null)
        {
            closeBarracksBtn.onClick.RemoveAllListeners();
            closeBarracksBtn.onClick.AddListener(ToggleBarracksUpgradeMenu);
        }

        if (closeShopBtn != null)
        {
            closeShopBtn.onClick.RemoveAllListeners();
            closeShopBtn.onClick.AddListener(ToggleShop);
        }

        // Функціонал будівництва
        if (buildBarracksBtnInMenu != null)
        {
            buildBarracksBtnInMenu.onClick.RemoveAllListeners();
            buildBarracksBtnInMenu.onClick.AddListener(BuildOrUpgradeBarracks);
        }

        if (buildMineButton != null)
        {
            buildMineButton.onClick.RemoveAllListeners();
            buildMineButton.onClick.AddListener(BuildOrUpgradeMine);
        }

        if (buildSpikesButton != null)
        {
            buildSpikesButton.onClick.RemoveAllListeners();
            buildSpikesButton.onClick.AddListener(BuildSpikes);
        }

        if (towerButton != null) 
        {
            towerButton.onClick.RemoveAllListeners(); 
            towerButton.onClick.AddListener(UpgradeDamage); 
        }

        if (castleUpgradeButton != null)
        {
            castleUpgradeButton.onClick.RemoveAllListeners();
            castleUpgradeButton.onClick.AddListener(UpgradeCastleHP);
        }

        // Функціонал кузні (апгрейди)
        if (upgradeKnightButton != null)
        {
            upgradeKnightButton.onClick.RemoveAllListeners();
            upgradeKnightButton.onClick.AddListener(UpgradeKnights);
        }

        if (upgradeArcherButton != null)
        {
            upgradeArcherButton.onClick.RemoveAllListeners();
            upgradeArcherButton.onClick.AddListener(UpgradeArchers);
        }

        if (upgradeSpearmanButton != null)
        {
            upgradeSpearmanButton.onClick.RemoveAllListeners();
            upgradeSpearmanButton.onClick.AddListener(UpgradeSpearman);
        }

        // Функціонал казарми
        if (upgradeLimitButton != null)
        {
            upgradeLimitButton.onClick.RemoveAllListeners();
            upgradeLimitButton.onClick.AddListener(BuyUnitLimitUpgrade);
        }

        if (unlockSpearmanButton != null)
        {
            unlockSpearmanButton.onClick.RemoveAllListeners();
            unlockSpearmanButton.onClick.AddListener(UnlockSpearman);
        }

        // Найм
        if (hireKnightButton != null)
        {
            hireKnightButton.onClick.RemoveAllListeners();
            hireKnightButton.onClick.AddListener(HireKnight);
        }

        if (hireArcherButton != null)
        {
            hireArcherButton.onClick.RemoveAllListeners();
            hireArcherButton.onClick.AddListener(HireArcher);
        }

        if (hireSpearmanButton != null)
        {
            hireSpearmanButton.onClick.RemoveAllListeners();
            hireSpearmanButton.onClick.AddListener(HireSpearman);
        }

        // Керування хвилею
        if (nextWaveButton != null)
        {
            nextWaveButton.onClick.RemoveAllListeners();
            nextWaveButton.onClick.AddListener(NextWave);
        }
    }

    // === ЛОГІКА ВІДКРИТТЯ ПАНЕЛЕЙ ===
    public void ToggleConstructionMenu()
    {
        GameObject panel = constructionPanel != null ? constructionPanel : constructionPanelNew;
        if (panel != null)
        {
            bool isActive = !panel.activeSelf;
            panel.SetActive(isActive);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);
            }

            if (isActive)
            {
                if (barracksUpgradePanel != null) barracksUpgradePanel.SetActive(false);
                if (barracksPanelNew != null) barracksPanelNew.SetActive(false);
                if (shopPanel != null) shopPanel.SetActive(false);
                if (shopPanelNew != null) shopPanelNew.SetActive(false);
                UpdateBarracksStateUI();
            }
        }
    }

    public void ToggleBarracksUpgradeMenu()
    {
        GameObject panel = barracksUpgradePanel != null ? barracksUpgradePanel : barracksPanelNew;
        if (panel != null)
        {
            bool isActive = !panel.activeSelf;
            panel.SetActive(isActive);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);
            }

            if (isActive)
            {
                UpdateUpgradeMenuPrice();
                UpdateUI();
                if (constructionPanel != null) constructionPanel.SetActive(false);
                if (constructionPanelNew != null) constructionPanelNew.SetActive(false);
                if (shopPanel != null) shopPanel.SetActive(false);
                if (shopPanelNew != null) shopPanelNew.SetActive(false);
            }
        }
    }

    public void ToggleShop()
    {
        GameObject panel = shopPanel != null ? shopPanel : shopPanelNew;
        if (panel != null)
        {
            bool isActive = !panel.activeSelf;
            panel.SetActive(isActive);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);
            }

            if(isActive)
            {
                if (constructionPanel != null) constructionPanel.SetActive(false);
                if (constructionPanelNew != null) constructionPanelNew.SetActive(false);
                if (barracksUpgradePanel != null) barracksUpgradePanel.SetActive(false);
                if (barracksPanelNew != null) barracksPanelNew.SetActive(false);
            }
        }
    }

    // =========================================================================
    // === СИСТЕМА ОНОВЛЕННЯ UI ЦІН (Вставляє іконки та текст) ===
    // =========================================================================
    public void UpdateCostUIGroup(UICostGroup costUI, ResourceType type1, int cost1, ResourceType type2 = ResourceType.Gold, int cost2 = 0, string overrideText = "")
    {
        if (costUI == null) return;

        // Перевизначення тексту (наприклад "BUILT", "MAX", "UNLOCKED")
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

        // Перший ресурс
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

        // Другий ресурс
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
            if (isWaveInProgress && enemiesAlive > 0)
            {
                GameObject[] realEnemies = GameObject.FindGameObjectsWithTag("Enemy");
                if (realEnemies.Length == 0)
                {
                    enemiesAlive = 0;
                    UpdateUI();
                    isWaveInProgress = false;

                    if (spawner != null) spawner.StopSpawning();

                    if (autoStartWaves)
                    {
                        StartCoroutine(AutoStartNextWave(timeBetweenWaves));
                    }
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
    public int GetDifficultyHealth() { return Mathf.RoundToInt(baseEnemyHealth * Mathf.Pow(enemyHpGrowth, currentWave - 1)); }
    public int GetGoldReward() { return Mathf.RoundToInt(baseGoldReward * Mathf.Pow(goldRewardGrowth, currentWave)); }

    public int GetKnightDamage() { return Mathf.RoundToInt((10 + ((knightLevel - 1) * 5)) * globalDamageMultiplier); }
    public int GetArcherDamage() { return Mathf.RoundToInt((8 + ((archerLevel - 1) * 3)) * globalDamageMultiplier); }
    public int GetSpearmanDamage() { return Mathf.RoundToInt((12 + ((spearmanLevel - 1) * 6)) * globalDamageMultiplier); }
    public int GetTowerDamage() { return Mathf.RoundToInt((25 + ((towerLevel - 1) * 8)) * globalDamageMultiplier); }

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
        if (mineLevel == 0)
        {
            return isWood ? mineBuildCostWood : mineBuildCostStone;
        }
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

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.waveStart, 0.4f);
        }

        if (spawner != null) spawner.StartWave(currentWave);
        UpdateUI();

        isWaveInProgress = true;
    }

    // === БУДІВНИЦТВО ===
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
        int currentCap = GetBarracksCapLimit();
        if (maxUnits >= currentCap)
        {
            UpdateCostUIGroup(upgradeLimitCostUI, ResourceType.Gold, 0, ResourceType.Wood, 0, "MAX");
        }
        else
        {
            UpdateCostUIGroup(upgradeLimitCostUI, ResourceType.Gold, GetSlotUpgradeCost());
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

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.constructionSound);

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
        if (_lastHireFrame == Time.frameCount) return;
        _lastHireFrame = Time.frameCount;

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

    public void UpgradeCastleHP()
    {
        if (castle == null) return;
        int cost = castle.GetUpgradeCost();
        if (gold >= cost)
        {
            gold -= cost;
            castle.UpgradeCastle();
            SaveGame();
            UpdateUI();
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
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
        GameObject[] projectiles = GameObject.FindGameObjectsWithTag("Projectile");
        foreach (var p in projectiles)
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
        Time.timeScale = 0;
        if(defeatPanel) defeatPanel.SetActive(true);
        ClearProjectiles();
        SaveGame();
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.defeat);
    }

    public void OnRetryButton()
    {
        Time.timeScale = 1;
        if(defeatPanel) defeatPanel.SetActive(false);

        if (spawner != null) spawner.StopSpawning();
        if (spawner != null) spawner.ClearEnemies();

        ClearDeadEnemies();
        ClearDeadUnits();

        if (castle == null) castle = FindFirstObjectByType<Castle>();
        if (castle != null) castle.HealMax();

        if (currentSpikes != null) Destroy(currentSpikes.gameObject);
        currentSpikes = null;

        manualTarget = null;
        if(targetIndicator) targetIndicator.Hide();

        ClearProjectiles();

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

        if (spawner != null) spawner.StartWave(currentWave);

        UpdateUI();
        UpdateBarracksStateUI();

        StartCoroutine(RecalculateUnitsNextFrame());
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
        gold = 0;
        wood = 0;
        stone = 0;
        currentWave = 1;
        maxUnits = 5;
        knightLevel = 1;
        archerLevel = 1;
        spearmanLevel = 1;

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

    // === UI ОНОВЛЕННЯ ===
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

        // ОНОВЛЕННЯ ЦІНИ ТА ТЕКСТУ ВЕЖІ
        UpdateCostUIGroup(towerCostUI, ResourceType.Wood, towerWoodCost, ResourceType.Stone, towerStoneCost);
        if (towerLevelText) towerLevelText.text = $"Tower Lvl {towerLevel}\nDMG: {GetTowerDamage()}";

        // ОНОВЛЕННЯ ЦІНИ АПГРЕЙДУ ЗАМКУ (HP)
        if (castleUpgradeCostUI != null && castle != null)
        {
            UpdateCostUIGroup(castleUpgradeCostUI, ResourceType.Gold, castle.GetUpgradeCost());
        }

        if (knightLevelText) knightLevelText.text = $"Knights Lvl {knightLevel}\nDMG: {GetKnightDamage()}";
        if (archerLevelText) archerLevelText.text = $"Archers Lvl {archerLevel}\nDMG: {GetArcherDamage()}";
        if (spearmanLevelText) spearmanLevelText.text = $"Spearmen Lvl {spearmanLevel}\nDMG: {GetSpearmanDamage()}";

        // ОНОВЛЕННЯ ЦІН ПРОКАЧКИ ВІЙСЬК (КУЗНЯ)
        UpdateCostUIGroup(knightUpgradeCostUI, ResourceType.Gold, knightUpgradeCost);
        UpdateCostUIGroup(archerUpgradeCostUI, ResourceType.Gold, archerUpgradeCost);
        UpdateCostUIGroup(spearmanUpgradeCostUI, ResourceType.Gold, spearmanUpgradeCost);

        bool canHireKnight = (gold >= knightFixedCost) && (currentUnits < maxUnits);
        bool canHireArcher = (gold >= archerFixedCost) && (currentUnits < maxUnits);
        bool canHireSpearman = (gold >= spearmanFixedCost) && (currentUnits < maxUnits);

        UpdateButtonState(hireKnightButton, canHireKnight);
        UpdateButtonState(hireArcherButton, canHireArcher);

        if (hireSpearmanButton != null)
        {
            hireSpearmanButton.gameObject.SetActive(isSpearmanUnlocked);
            UpdateButtonState(hireSpearmanButton, canHireSpearman && isSpearmanUnlocked);
        }

        UpdateButtonState(towerButton, (wood >= towerWoodCost && stone >= towerStoneCost));
        if (castleUpgradeButton != null && castle != null)
            UpdateButtonState(castleUpgradeButton, gold >= castle.GetUpgradeCost());
        UpdateButtonState(hammerButton, true);

        UpdateButtonState(upgradeKnightButton, gold >= knightUpgradeCost);
        UpdateButtonState(upgradeArcherButton, gold >= archerUpgradeCost);

        if (upgradeSpearmanButton != null)
        {
            UpdateButtonState(upgradeSpearmanButton, (gold >= spearmanUpgradeCost) && isSpearmanUnlocked);
        }

        if (unlockSpearmanButton != null)
        {
            unlockSpearmanButton.gameObject.SetActive(isBarracksBuilt);

            if (isSpearmanUnlocked)
            {
                UpdateButtonState(unlockSpearmanButton, false);
                UpdateCostUIGroup(unlockSpearmanCostUI, ResourceType.Gold, 0, ResourceType.Wood, 0, "UNLOCKED");
            }
            else
            {
                UpdateButtonState(unlockSpearmanButton, gold >= spearmanUnlockCost);
                UpdateCostUIGroup(unlockSpearmanCostUI, ResourceType.Gold, spearmanUnlockCost);
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
                if (mineLevel == 0) mineImg.sprite = buildButtonSprite;
                else mineImg.sprite = upgradeButtonSprite;
            }

            UpdateCostUIGroup(mineCostUI, ResourceType.Wood, mWood, ResourceType.Stone, mStone);
        }

        if (openShopButton != null) UpdateButtonState(openShopButton, true);

        if (nextWaveButton != null) UpdateButtonState(nextWaveButton, enemiesAlive <= 0);

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

        // === РОЗРАХУНОК ПРИБУТКУ ===
        if (estimatedIncomeText != null && spawner != null)
        {
            int enemiesGold = spawner.GetEstimatedGoldFromEnemies();
            int waveBonus = GetGoldReward();
            int totalEst = enemiesGold + waveBonus;

            estimatedIncomeText.text = $"+{totalEst} G";
        }

        UpdateSpearmanLockUI();
    }

    // === УНІВЕРСАЛЬНИЙ МЕТОД КНОПОК ===
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

    void SetupAllButtonColors()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);

        foreach (Button btn in allButtons)
        {
            ColorBlock colors = btn.colors;
            btn.transition = Selectable.Transition.ColorTint;
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            colors.normalColor = Color.white;
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.colorMultiplier = 1f;
            btn.colors = colors;
        }
    }
}