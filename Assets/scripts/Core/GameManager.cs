using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum ResourceType { Gold, Wood, Stone }

public enum UnitCategory
{
    Standard,   // Лицарі, Гвардійці
    Ranged,     // Лучники
    Cavalry,    // Кіннота
    Spearman,   // Списоносці
    Building    // Замок, Стіни
}

[System.Serializable]
public class UICostGroup
{
    public Image icon1;
    public TMP_Text text1;
    public Image icon2;
    public TMP_Text text2;
}

[System.Serializable]
public class MetaSkillUI
{
    public TMP_Text levelText;
    public TMP_Text descText;
    public TMP_Text costText;
    public Button upgradeBtn;
    public GameObject lockedOverlay; 
}

[System.Serializable]
public class UnitEvolutionUI
{
    public Image[] skinImages;      
    public Sprite lockSprite;        
    [HideInInspector] public GameObject[] generatedLocks; 

    public Image[] progressPips;    
    
    // === ТЕПЕР ТУТ 4 ПОЛЯ ДЛЯ ТЕКСТУ ===
    public TMP_Text nameText;       
    public TMP_Text statsText;         // Сюди піде "⚔️ 32"
    public TMP_Text bonusStatsText;    // Сюди піде "+5"
    public TMP_Text tacticsText;    
    
    public Button upgradeBtn;        
    public UICostGroup costUI;      
}

public partial class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [HideInInspector] public bool isCampaignBattle = false; 

    [Header("=== МЕТА-ПРОГРЕСІЯ (КРИСТАЛИ) ===")]
    public int gems = 0;
    
    public int metaFortifiedWalls = 0;   
    public int metaPrecisionBows = 0;    
    public int metaVolleyBarrage = 0;    
    public int metaTrophyBounty = 0;     
    public int metaEfficientCarts = 0;   
    public int metaMendingMasonry = 0;   

    [Header("Прогрес Кристалів (Шкала)")]
    public int currentKills = 0;
    public int killsToNextGem = 25; 
    public Slider gemProgressBar;
    public TMP_Text gemProgressText;

    [Header("UI Мета-Прогресії")]
    public TMP_Text gemsText;
    public TMP_Text topGemsText; 
    public GameObject metaShopPanel;
    
    [Header("Слоти Мета-Навичок (UI)")]
    public MetaSkillUI uiFortifiedWalls;
    public MetaSkillUI uiPrecisionBows;
    public MetaSkillUI uiVolleyBarrage;
    public MetaSkillUI uiTrophyBounty;
    public MetaSkillUI uiEfficientCarts;
    public MetaSkillUI uiMendingMasonry;

    public Button openMetaShopButton;
    public Button closeMetaShopButton;

    [Header("Formation Settings")]
    public Transform formationStartPoint; 
    public float rowSpacing = 1.5f;        
    public float columnSpacing = 0.8f;    

    private List<Knight> activeKnights = new List<Knight>();
    private List<Spearman> activeSpearmen = new List<Spearman>();
    private List<Cavalry> activeCavalry = new List<Cavalry>(); 
    private List<Archer> activeArchers = new List<Archer>();

    [Header("Continuous Wave UI")]
    public GameObject tickPrefab;      
    public Transform tickContainer;    
    public float waveDuration = 60f;   
    private float waveTimer = 0f;
    private List<float> currentWaveMilestones = new List<float>(); 

    private bool isFirstWaveStarted = false;
    private bool isWaitingForNextWave = false;

    [HideInInspector] public bool isCinematicActive = false;

    [Header("Система прицілювання")]
    public Transform manualTarget;
    public TargetIndicator targetIndicator;

    private Coroutine uiFadeCoroutine; 
    private Dictionary<RectTransform, Vector2> originalBtnPositions = new Dictionary<RectTransform, Vector2>();

    [Header("UI: Панель Вежі (Анімація)")]
    public RectTransform towerUpgradePanelRect;
    public Button towerToggleButton;        
    public Transform towerArrowIcon;        
    public float towerOpenPosX = 0f;        
    public float towerClosedPosX = 180f;    
    
    private bool isTowerPanelOpen = true;
    private Coroutine towerPanelCoroutine;

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
            GameObject popupObj = PoolRegistry.Spawn(Instance.damagePopupPrefab, position, Quaternion.identity);
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

    [Header("=== UI: GAME GUIDE (INFO PANEL) ===")]
    public TMP_Text[] guideTextElements;
    [TextArea(2, 4)]
    public string[] guideMessages = new string[]
    {
        "<b><color=#1E88E5>COMBAT:</color></b> <color=#3E2723>Knights counter Archers. Spearmen deal <color=#E53935>x2 DMG</color> to Cavalry. Archers beat Spearmen.</color>",
        "<b><color=#E65100>ECONOMY:</color></b> <color=#3E2723>Build a Mine early for passive Gold. Upgrade Barracks to unlock units and increase army cap.</color>",
        "<b><color=#43A047>DEFENSE:</color></b> <color=#3E2723>Upgrade Castle HP and Tower DMG to survive longer. Crossbows provide heavy automated defense.</color>",
        "<b><color=#8E24AA>SKILLS:</color></b> <color=#3E2723>Call the Supply Cart on cooldown for extra resources. Use Rain of Arrows to wipe out huge enemy waves.</color>",
        "<b><color=#00ACC1>META PROGRESSION:</color></b> <color=#3E2723>Earn Gems by killing enemies. Spend them on permanent Talents in the Meta Shop.</color>"
    };

    [Header("=== ПАНЕЛЬ НАЛАШТУВАНЬ ===")]
    public GameObject settingsPanel;    
    public Button openSettingsButton;   
    public Button closeSettingsBtn;     

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
    public Button requestCartButton; 
    public Button volleyBarrageButton; 
    public Button openMapButton;
    public Button openQuestButton;
    
    [Header("Ефекти пульсації кнопок")]
    public UIPulseEffect shopPulse;
    public UIPulseEffect barracksPulse;
    public UIPulseEffect buildPulse;

    [Header("UI: Кнопки хрестики")]
    public Button closeConstructionBtn;
    public Button closeBarracksBtn;
    public Button closeShopBtn;
    public Button closeInfoButton; 

    [Header("Системні кнопки")]
    public Button exitGameButton;
    public Button continueButton;

    [Header("Підтвердження Виходу")]
    public GameObject exitConfirmPanel;
    public Button confirmExitYesBtn;
    public Button confirmExitNoBtn;

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
    
    public Button unlockCavalryButton;
    public UICostGroup unlockCavalryCostUI;
    public CanvasGroup unlockCavalryRowGroup;

    [Header("Скіни Юнітів (Еволюція)")]
    public Sprite[] knightSkins;
    public Sprite[] archerSkins;
    public Sprite[] spearmanSkins;
    public Sprite[] cavalrySkins; 

    [Header("UI: Нова Панель Еволюції")]
    public UnitEvolutionUI knightEvoUI;
    public UnitEvolutionUI archerEvoUI;
    public UnitEvolutionUI spearmanEvoUI;
    public UnitEvolutionUI cavalryEvoUI; 

    public Button upgradeWallArcherButton;
    public TMP_Text wallArcherLevelText;
    public UICostGroup wallArcherUpgradeCostUI;

    [Header("UI: Блокування Юнітів (Замочки)")]
    public CanvasGroup spearmanRowGroup;
    public GameObject spearmanLockIcon;
    public CanvasGroup cavalryRowGroup;
    public GameObject cavalryLockIcon;
    public bool dimUnitWhenLocked = true;

    [Header("=== АРБАЛЕТНА БАШТА (Crossbow Tower) ===")]
    public bool isCrossbowBuilt = false;
    public int crossbowBuildCostWood = 200;
    public int crossbowBuildCostStone = 100;
    
    public GameObject[] crossbowTowers; 
    
    public Button buildCrossbowButton;
    public UICostGroup buildCrossbowCostUI;

    public Button upgradeCrossbowDamageButton;
    public Button upgradeCrossbowReloadButton;
    public TMP_Text crossbowDamageText;
    public TMP_Text crossbowReloadText;
    public UICostGroup crossbowDamageCostUI;
    public UICostGroup crossbowReloadCostUI;

    public int crossbowDamageLevel = 1;
    public int crossbowReloadLevel = 1;
    public int crossbowBaseDamage = 25;
    public float crossbowBaseReloadTime = 2.5f; 
    private int crossbowDamageCostGold = 150;
    private int crossbowReloadCostGold = 150;

    [Header("UI: Статистика і Предікт")]
    public TMP_Text estimatedIncomeText;
    public GameObject goldPredictPanel; 
    public GameObject enemyPredictPanel; 

    private Coroutine topHudCoroutine;
    private Dictionary<RectTransform, Vector2> originalTopPos = new Dictionary<RectTransform, Vector2>();
    private Dictionary<GameObject, Vector3> originalPanelScales = new Dictionary<GameObject, Vector3>();
    private float topSlideDist = 300f; 

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
    public Button hireCavalryButton; 

    public TMP_Text hireKnightCostText;
    public TMP_Text hireArcherCostText;
    public TMP_Text hireSpearmanCostText;
    public TMP_Text hireCavalryCostText;

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
    public int glory = 0; // ДОДАНО: Валюта Слави

    [Header("Тексти HUD")]
    public TMP_Text goldText;
    public TMP_Text woodText;
    public TMP_Text stoneText;
    public TMP_Text gloryText; // ДОДАНО: Текст для Слави
    public TMP_Text gloryPopupText; // Анімація +X Слави (додай у Інспекторі!)
    public TMP_Text waveText;
    public TMP_Text hirePriceText;
    public TMP_Text limitText;
    public TMP_Text towerLevelText;

    [Header("Баланс: Війська")]
    public GameObject knightPrefab;
    public GameObject archerPrefab;
    public GameObject spearmanPrefab;
    public GameObject cavalryPrefab; 
    public Transform unitSpawnPoint;

    public int knightFixedCost = 50;
    public int archerFixedCost = 75;
    public int spearmanFixedCost = 60;
    public int cavalryFixedCost = 120; 

    [Header("=== БАЗОВІ СТАТИ (Для Кодексу і Гри) ===")]
    public int knightBaseHp = 120;
    public int knightBaseDmg = 12;

    public int archerBaseHp = 60;
    public int archerBaseDmg = 8;

    public int spearmanBaseHp = 90;
    public int spearmanBaseDmg = 15;

    public int cavalryBaseHp = 150;
    public int cavalryBaseDmg = 25;

    [Header("Базові стати Будівель")]
    public int wallBaseHp = 150;
    public int towerBaseHp = 0;

    [Header("Рівні Технологій")]
    public int knightLevel = 1;
    public int archerLevel = 1;
    public int spearmanLevel = 1;
    public int cavalryLevel = 1; 
    public int wallArcherLevel = 1; 

    private int knightUpgradeCost = 100;
    private int archerUpgradeCost = 120;
    private int spearmanUpgradeCost = 110;
    private int cavalryUpgradeCost = 180; 
    private int wallArcherUpgradeCost = 150; 

    [Header("Розблокування (Unlock)")]
    public bool isSpearmanUnlocked = false;
    public int spearmanUnlockCost = 500;
    
    public bool isCavalryUnlocked = false; 
    public int cavalryUnlockCost = 800;

    [Header("Ліміт військ")]
    public int maxUnits = 5;
    public int currentUnits = 0;
    [HideInInspector] public bool isResettingUnits = false;

    [Header("Баланс: Вежа")]
    public int towerLevel = 1;
    private int towerWoodCost = 50;
    private int towerStoneCost = 20;

    [Header("Баланс: Хвилі")]
    public int currentWave = 1;
    public int baseGoldReward = 15;
    public int baseEnemyHealth = 50;

    private bool isWaveInProgress = false;
    private float _lastHireTime = 0f;
    private float lastInfoPanelToggleTime = 0f;
    private int killsForGloryCounter = 0;
    private Vector2 defaultGloryPopupPos; // Початкова позиція попапу // ДОДАНО: Лічильник для розрахунку слави

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // --- РОЗУМНИЙ РЕЖИМ ---
        isCampaignBattle = (SceneManager.GetActiveScene().name == "SiegeBattleScene");

        if (isCampaignBattle) 
        {
            isWaveInProgress = true; 
        }

        LoadGame();
    }

    void Start()
    {
        // Ховаємо попап слави на старті і запам'ятовуємо його місце
        if (gloryPopupText != null)
        {
            defaultGloryPopupPos = gloryPopupText.rectTransform.anchoredPosition;
            gloryPopupText.gameObject.SetActive(false);
        }

        // === ФІКС: СПАВН АРМІЇ У РЕЖИМІ НАПАДУ ===
        // === ФІКС: СПАВН АРМІЇ У РЕЖИМІ НАПАДУ ===
        if (isCampaignBattle) 
        {
            // Армією в SiegeBattleScene керує виключно BattleManager.
            // Тут раніше був паралельний спавн, який до того ж множив на 5
            // уже помножену кількість (CrossSceneData.*Count вже містить юнітів,
            // а не загони). Працював він лише тому, що префаби були не призначені.
            return; 
        }

        // --- ДАЛІ ЙДЕ ТВІЙ СТАРИЙ КОД ---
        if (goldText != null)
        {
            goldText.gameObject.SetActive(true);
            if (goldText.transform.parent != null) goldText.transform.parent.gameObject.SetActive(true);
        }
        
        // ... решта коду Start() залишається без змін ...

        ResolveUIRefs();
        ResolveUnitLockUI(); 
        WireExplicitButtons();
        SetupMetaButtons();
        RecalculateUnits();
        
        InitializeGameGuide(); 
        UpdateCrossbowVisibility();

        if (towerToggleButton != null)
        {
            towerToggleButton.onClick.RemoveAllListeners();
            towerToggleButton.onClick.AddListener(OnTowerToggleClicked);
        }

        if (towerUpgradePanelRect != null)
        {
            towerUpgradePanelRect.gameObject.SetActive(true);
            CanvasGroup cg = towerUpgradePanelRect.GetComponent<CanvasGroup>();
            if (cg == null) cg = towerUpgradePanelRect.gameObject.AddComponent<CanvasGroup>();
            
            isTowerPanelOpen = true;
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
            towerUpgradePanelRect.anchoredPosition = new Vector2(towerOpenPosX, towerUpgradePanelRect.anchoredPosition.y);
            
            if (towerArrowIcon != null) towerArrowIcon.localRotation = Quaternion.Euler(0, 0, 180f);
        }

        enemiesAlive = 0;
        CloseAllPanels();
        if (defeatPanel) defeatPanel.SetActive(false);

        ToggleWaveHUD(false, true);

        UpdateUI();
        UpdateBarracksStateUI();
        UpdateUnitLockUI(); 
        UpdateMetaUI();

        if (waveTimerBar != null)
        {
            waveTimerBar.maxValue = 1f;
            waveTimerBar.value = 0f;
        }

        TriggerUIFade(true);

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

        if (isCampaignBattle) return;

        if (isWaveInProgress && !isCinematicActive)
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
                CheckWaveState();
            }
        }
    }

    void InitializeGameGuide()
    {
        if (guideTextElements != null && guideTextElements.Length > 0)
        {
            for (int i = 0; i < Mathf.Min(guideTextElements.Length, guideMessages.Length); i++)
            {
                if (guideTextElements[i] != null) guideTextElements[i].text = guideMessages[i];
            }
        }
    }

    public void SetManualTarget(Transform target)
    {
        manualTarget = target;
        if (targetIndicator != null) targetIndicator.Show(target);
    }

    private void OnWaveFullyCleared()
    {
        ToggleWaveHUD(false); 
        TriggerUIFade(true);  

        // ДОДАНО: Бонус Слави за завершення хвилі
        AddGlory(currentWave * 2);

        if (SoundManager.Instance != null) 
        {
            SoundManager.Instance.PlayIdleMusic();
            if (SoundManager.Instance.victoryMusicStinger != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.victoryMusicStinger, 1.0f);
            if (SoundManager.Instance.victoryCries != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.victoryCries, 0.7f);
        }

        if (Camera.main != null)
        {
            CameraController camCtrl = Camera.main.GetComponent<CameraController>();
            if (camCtrl != null) camCtrl.ReturnToBase();
        }
    }

    void SetupMetaButtons()
    {
        if (uiFortifiedWalls != null && uiFortifiedWalls.upgradeBtn != null) uiFortifiedWalls.upgradeBtn.onClick.AddListener(() => BuyMetaSkill(ref metaFortifiedWalls, 1, 10, 5));
        if (uiPrecisionBows != null && uiPrecisionBows.upgradeBtn != null) uiPrecisionBows.upgradeBtn.onClick.AddListener(() => BuyMetaSkill(ref metaPrecisionBows, 2, 15, 10));
        if (uiVolleyBarrage != null && uiVolleyBarrage.upgradeBtn != null) uiVolleyBarrage.upgradeBtn.onClick.AddListener(() => BuyMetaSkill(ref metaVolleyBarrage, 3, 25, 15));
        if (uiTrophyBounty != null && uiTrophyBounty.upgradeBtn != null) uiTrophyBounty.upgradeBtn.onClick.AddListener(() => BuyMetaSkill(ref metaTrophyBounty, 4, 10, 5));
        if (uiEfficientCarts != null && uiEfficientCarts.upgradeBtn != null) uiEfficientCarts.upgradeBtn.onClick.AddListener(() => BuyMetaSkill(ref metaEfficientCarts, 5, 15, 10));
        if (uiMendingMasonry != null && uiMendingMasonry.upgradeBtn != null) uiMendingMasonry.upgradeBtn.onClick.AddListener(() => BuyMetaSkill(ref metaMendingMasonry, 6, 20, 15));
    }

    void BuyMetaSkill(ref int skillLevel, int skillId, int baseCost, int costPerLevel)
    {
        int currentCost = baseCost + (skillLevel * costPerLevel);
        if (gems >= currentCost)
        {
            gems -= currentCost;
            skillLevel++;
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);
            
            if (skillId == 1 && castle != null) castle.LoadState(castle.wallLevel);

            SaveGame();
            UpdateMetaUI();
            UpdateUI();
        }
        else if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
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

    Transform FindChild(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }
        return null;
    }

    public void UpdateFormationPositions()
    {
        if (formationStartPoint == null) return;

        int unitsPerColumn = 4 + (currentUnits / 10);
        unitsPerColumn = Mathf.Clamp(unitsPerColumn, 4, 8); 

        float currentXOffset = 0f;

        currentXOffset = AssignPositionsToGroupDynamic(activeKnights.Cast<MonoBehaviour>().ToList(), currentXOffset, unitsPerColumn);
        currentXOffset = AssignPositionsToGroupDynamic(activeSpearmen.Cast<MonoBehaviour>().ToList(), currentXOffset, unitsPerColumn);
        currentXOffset = AssignPositionsToGroupDynamic(activeCavalry.Cast<MonoBehaviour>().ToList(), currentXOffset, unitsPerColumn); 
        currentXOffset = AssignPositionsToGroupDynamic(activeArchers.Cast<MonoBehaviour>().ToList(), currentXOffset, unitsPerColumn);
    }

    private float AssignPositionsToGroupDynamic(List<MonoBehaviour> units, float startXOffset, int unitsPerColumn)
    {
        if (units.Count == 0) return startXOffset; 

        for (int i = 0; i < units.Count; i++)
        {
            int column = i / unitsPerColumn; 
            int row = i % unitsPerColumn;    

            float posX = formationStartPoint.position.x - startXOffset - (column * rowSpacing);
            
            float startY = formationStartPoint.position.y + ((unitsPerColumn - 1) * columnSpacing) / 2f;
            float posY = startY - (row * columnSpacing);

            Vector3 targetPos = new Vector3(posX, posY, 0);

            units[i].SendMessage("SetFormationPosition", targetPos, SendMessageOptions.DontRequireReceiver);
        }

        int columnsUsed = (units.Count - 1) / unitsPerColumn + 1;
        return startXOffset + (columnsUsed * rowSpacing) + 0.5f; 
    }

    // Спільний буфер: вороги питають список юнітів гравця щокадру,
    // і алокувати новий масив на кожен запит немає сенсу.
    private readonly List<Transform> playerUnitBuffer = new List<Transform>();
    private int playerUnitBufferFrame = -1;

    /// <summary>
    /// Живі юніти гравця одним списком.
    ///
    /// Замінює чотири FindObjectsByType поспіль у кожного ворога: при 20
    /// ворогах, які перецілюються 4 рази на секунду, це було 320 повних
    /// сканувань сцени щосекунди. Тут — готовий список, що перебудовується
    /// щонайбільше раз на кадр.
    ///
    /// Список віддається напряму, без копії. Не тримай його між кадрами
    /// і перевіряй елементи на null: юніт може загинути вже після збирання.
    /// </summary>
    public IReadOnlyList<Transform> GetPlayerUnits()
    {
        if (playerUnitBufferFrame == Time.frameCount) return playerUnitBuffer;
        playerUnitBufferFrame = Time.frameCount;

        playerUnitBuffer.Clear();
        AppendAlive(activeKnights, playerUnitBuffer);
        AppendAlive(activeSpearmen, playerUnitBuffer);
        AppendAlive(activeArchers, playerUnitBuffer);
        AppendAlive(activeCavalry, playerUnitBuffer);

        return playerUnitBuffer;
    }

    private static void AppendAlive<T>(List<T> source, List<Transform> buffer) where T : MonoBehaviour
    {
        if (source == null) return;
        foreach (var unit in source)
        {
            if (unit == null || unit.CompareTag("Untagged")) continue;
            buffer.Add(unit.transform);
        }
    }

    public void RecalculateUnits()
    {
        var knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        var archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        var spearmen = FindObjectsByType<Spearman>(FindObjectsSortMode.None);
        var cavalry = FindObjectsByType<Cavalry>(FindObjectsSortMode.None); 

        activeKnights = knights.Where(k => !k.CompareTag("Untagged")).ToList();
        activeArchers = archers.Where(a => !a.CompareTag("Untagged")).ToList();
        activeSpearmen = spearmen.Where(s => !s.CompareTag("Untagged")).ToList();
        activeCavalry = cavalry.Where(c => !c.CompareTag("Untagged")).ToList(); 

        currentUnits = activeKnights.Count + activeArchers.Count + activeSpearmen.Count + activeCavalry.Count;
        UpdateFormationPositions();
    }

    public void OnUnitDeath(GameObject unitObj, string type)
    {
        if (type == "Knight") activeKnights.Remove(unitObj.GetComponent<Knight>());
        else if (type == "Spearman") activeSpearmen.Remove(unitObj.GetComponent<Spearman>());
        else if (type == "Archer") activeArchers.Remove(unitObj.GetComponent<Archer>());
        else if (type == "Cavalry") activeCavalry.Remove(unitObj.GetComponent<Cavalry>()); 

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
        if (enemiesAlive > 0 && isWaveInProgress) return;

        isWaitingForNextWave = false;

        CloseAllPanels();
        TriggerUIFade(false);
        ToggleWaveHUD(true);

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
            SoundManager.Instance.PlayBattleMusic();
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

        if (currentWave % 10 == 0) currentWaveMilestones.Add(0.95f);
    }

    void SpawnEnemySquad()
    {
        bool isBossMoment = (currentWave % 10 == 0) && currentWaveMilestones.Count <= 1;
        if (spawner != null) spawner.SpawnSquad(isBossMoment);
    }

    public void ToggleMetaShop()
    {
        if (metaShopPanel == null) return;
        
        bool isOpening = !metaShopPanel.activeSelf;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);

        if (isOpening) { UpdateMetaUI(); CloseAllPanels(); StartCoroutine(AnimatePanel(metaShopPanel, true)); }
        else { StartCoroutine(AnimatePanel(metaShopPanel, false)); }
        
        UpdateUI();
    }  

    public Sprite GetUnitSkin(UnitCategory category, int level)
    {
        int skinIdx = (level - 1) / 5;
        Sprite[] skins = null;

        switch(category)
        {
            case UnitCategory.Standard: skins = knightSkins; break;
            case UnitCategory.Ranged: skins = archerSkins; break;
            case UnitCategory.Spearman: skins = spearmanSkins; break;
            case UnitCategory.Cavalry: skins = cavalrySkins; break; 
        }

        if (skins == null || skins.Length == 0) return null;
        return skins[Mathf.Clamp(skinIdx, 0, skins.Length - 1)];
    }

    public void ToggleConstructionMenu()
    {
        GameObject panel = constructionPanel != null ? constructionPanel : constructionPanelNew;
        if (panel == null) return;
        
        bool isOpening = !panel.activeSelf;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);

        if (isOpening) { CloseAllPanels(); StartCoroutine(AnimatePanel(panel, true)); UpdateBarracksStateUI(); }
        else { StartCoroutine(AnimatePanel(panel, false)); }
        
        UpdateUI();
    }

    public void ToggleBarracksUpgradeMenu()
    {
        GameObject panel = barracksUpgradePanel != null ? barracksUpgradePanel : barracksPanelNew;
        if (panel == null) return;
        
        bool isOpening = !panel.activeSelf;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);

        if (isOpening) { UpdateUpgradeMenuPrice(); CloseAllPanels(); StartCoroutine(AnimatePanel(panel, true)); }
        else { StartCoroutine(AnimatePanel(panel, false)); }
        
        UpdateUI();
    }

    // Універсальна анімація відкриття/закриття вікон
    // Універсальна анімація відкриття/закриття вікон (ТІЛЬКИ ПРОЗОРІСТЬ)
    // Універсальна анімація відкриття/закриття вікон (Із збереженням твого Scale)

    public void ToggleShop()
    {
        GameObject panel = shopPanel != null ? shopPanel : shopPanelNew;
        if (panel == null) return;
        
        bool isOpening = !panel.activeSelf;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);

        if (isOpening) { CloseAllPanels(); StartCoroutine(AnimatePanel(panel, true)); }
        else { StartCoroutine(AnimatePanel(panel, false)); }
        
        UpdateUI();
    }

    public void ToggleInfoPanel()
    {
        if (infoPanel == null) return;

        bool isOpening = !infoPanel.activeSelf;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);

        if (isOpening) { CloseAllPanels(); StartCoroutine(AnimatePanel(infoPanel, true)); Time.timeScale = 0f; }
        else { StartCoroutine(AnimatePanel(infoPanel, false)); Time.timeScale = 1f; }
        
        UpdateUI();
    }

    public void ToggleSettingsPanel()
    {
        if (settingsPanel == null) return;

        bool isOpening = !settingsPanel.activeSelf;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);

        if (isOpening) { CloseAllPanels(); StartCoroutine(AnimatePanel(settingsPanel, true)); Time.timeScale = 0f; }
        else { ResumeGame(); } // ResumeGame закриє вікно
        
        UpdateUI();
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
            CheckWaveState();
        }
    }

    private void CheckWaveState()
    {
        if (isDefeated) return;

        if (!isFirstWaveStarted) return; 

        GameObject[] realEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        enemiesAlive = realEnemies.Length;

        if (enemiesAlive > 0) return;

        if (isWaveInProgress)
        {
            if (currentWaveMilestones.Count > 0) return; 

            isWaveInProgress = false;
            if (waveTimerBar != null) waveTimerBar.value = 0f;
        }

        if (!isWaitingForNextWave)
        {
            isWaitingForNextWave = true; 
            
            OnWaveFullyCleared(); 

            if (autoStartWaves)
            {
                StartCoroutine(AutoStartNextWave(timeBetweenWaves));
            }
        }
    }

    public int GetDifficultyHealth() 
    { 
        return GetScaledEnemyHealth(baseEnemyHealth); 
    }

    public int GetScaledEnemyHealth(int enemyBaseHealth) 
    { 
        int hp = EconomyConfig.GetEnemyHealth(enemyBaseHealth, currentWave); 
        if (currentWave <= 1) return Mathf.Max(10, hp / 5); 
        return hp; 
    }

    public float GetEnemyDamageMultiplier() 
    {
        if (currentWave <= 1) return 0.5f;
        return 1.0f + (currentWave * 0.05f) + (Mathf.Pow(currentWave, 2) * 0.001f);
    }
    
    public int GetGoldReward() 
    { 
        int baseRwd = EconomyConfig.GetEnemyGoldDrop(baseGoldReward, currentWave);
        float multiplier = 1f + (metaTrophyBounty * 0.05f); 
        return Mathf.RoundToInt(baseRwd * multiplier);
    }

    public int GetKnightDamage()   { return GetKnightDamageAtLevel(knightLevel); }
    public int GetArcherDamage()   { return GetArcherDamageAtLevel(archerLevel); }
    public int GetSpearmanDamage() { return GetSpearmanDamageAtLevel(spearmanLevel); }
    public int GetCavalryDamage()  { return GetCavalryDamageAtLevel(cavalryLevel); } 
    public int GetTowerDamage()    { return GetTowerDamageAtLevel(towerLevel); }
    public int GetWallArcherDamage() { return GetWallArcherDamageAtLevel(wallArcherLevel); } 
    public int GetCrossbowDamage() { return GetCrossbowDamageAtLevel(crossbowDamageLevel); }
    public float GetCrossbowReloadTime() { return GetCrossbowReloadAtLevel(crossbowReloadLevel); }

    public int GetKnightDamageAtLevel(int level)   { return Mathf.RoundToInt(EconomyConfig.GetUnitDamage(12, level) * globalDamageMultiplier); }
    public int GetSpearmanDamageAtLevel(int level) { return Mathf.RoundToInt(EconomyConfig.GetUnitDamage(15, level) * globalDamageMultiplier); }
    public int GetCavalryDamageAtLevel(int level)  { return Mathf.RoundToInt(EconomyConfig.GetUnitDamage(25, level) * globalDamageMultiplier); } 
    public int GetTowerDamageAtLevel(int level)    { return Mathf.RoundToInt(EconomyConfig.GetUnitDamage(25, level) * globalDamageMultiplier); }
    
    public int GetArcherDamageAtLevel(int level)   
    { 
        int baseDmg = EconomyConfig.GetUnitDamage(8, level);
        float buff = 1f + (metaPrecisionBows * 0.15f);
        return Mathf.RoundToInt(baseDmg * buff * globalDamageMultiplier); 
    }
    
    public int GetWallArcherDamageAtLevel(int level) 
    { 
        int baseDmg = EconomyConfig.GetUnitDamage(10, level);
        float buff = 1f + (metaPrecisionBows * 0.15f);
        return Mathf.RoundToInt(baseDmg * buff * globalDamageMultiplier); 
    } 

    public int GetCrossbowDamageAtLevel(int level) 
    { 
        int baseDmg = EconomyConfig.GetUnitDamage(crossbowBaseDamage, level);
        float buff = 1f + (metaPrecisionBows * 0.15f);
        return Mathf.RoundToInt(baseDmg * buff * globalDamageMultiplier); 
    }
    
    public float GetCrossbowReloadAtLevel(int level) 
    { 
        return Mathf.Max(1.2f, crossbowBaseReloadTime - ((level - 1) * 0.02f)); 
    }

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
        int baseCost = isGold ? barracksCostGold : barracksCostWood;
        return EconomyConfig.GetUpgradeCost(baseCost, barracksLevel);
    }

    public int GetMineUpgradeCost(bool isWood) 
    { 
        if (mineLevel == 0) return isWood ? mineBuildCostWood : mineBuildCostStone; 
        int baseCost = isWood ? mineUpgradeBaseWood : mineUpgradeBaseStone; 
        return baseCost + (mineLevel * 40) + Mathf.RoundToInt(Mathf.Pow(mineLevel, 2) * 2f); 
    }

    public int GetTowerWoodCost(int level) { return 50 + (level * 15) + Mathf.RoundToInt(Mathf.Pow(level, 2) * 0.5f); }
    public int GetTowerStoneCost(int level) { return 20 + (level * 10) + Mathf.RoundToInt(Mathf.Pow(level, 2) * 0.2f); }

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
        if (QuestManager.Instance != null) QuestManager.Instance.AddQuestProgress(QuestType.KillEnemies, 1);
        UpdateUI();

        if (manualTarget != null && !manualTarget.gameObject.activeInHierarchy)
        {
            manualTarget = null;
        }

        CheckWaveState();
    }

    public void BuildOrUpgradeBarracks()
    {
        int costG = GetBarracksBuildingUpgradeCost(true);
        int costW = GetBarracksBuildingUpgradeCost(false);

        if (IsTutorialTarget(buildBarracksBtnInMenu))
        {
            if (gold < costG) gold = costG;
            if (wood < costW) wood = costW;
        }

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

    void SpawnMineObject()
    {
        if (minePrefab && mineSpawnPoint)
        {
            if (currentMineObject != null) Destroy(currentMineObject);
            currentMineObject = Instantiate(minePrefab, mineSpawnPoint.position, Quaternion.identity);
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
        return 100 + (u * 50) + Mathf.RoundToInt(Mathf.Pow(u, 2) * 1f);
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
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }
    
    public void UnlockCavalry()
    {
        if (isCavalryUnlocked) return;
        
        if (barracksLevel < 3)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
            return;
        }

        if (gold >= cavalryUnlockCost)
        {
            gold -= cavalryUnlockCost;
            isCavalryUnlocked = true;

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

    public void UpgradeKnights()
    {
        knightUpgradeCost = EconomyConfig.GetUpgradeCost(150, knightLevel);
        if(gold >= knightUpgradeCost)
        {
            gold -= knightUpgradeCost;
            knightLevel++;

            if (SoundManager.Instance != null && SoundManager.Instance.unitUpgradeSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);

            SaveGame();
            UpdateUI();
            RefreshAllUnitSkins("Knight");

            if (hireKnightCostText != null) StartCoroutine(AnimatePriceChange(hireKnightCostText));
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    public void UpgradeArchers()
    {
        archerUpgradeCost = EconomyConfig.GetUpgradeCost(120, archerLevel);
        if(gold >= archerUpgradeCost)
        {
            gold -= archerUpgradeCost;
            archerLevel++;

            if (SoundManager.Instance != null && SoundManager.Instance.unitUpgradeSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);

            SaveGame();
            UpdateUI();
            RefreshAllUnitSkins("Archer");

            if (hireArcherCostText != null) StartCoroutine(AnimatePriceChange(hireArcherCostText));
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    public void UpgradeSpearman()
    {
        spearmanUpgradeCost = EconomyConfig.GetUpgradeCost(140, spearmanLevel);
        if(gold >= spearmanUpgradeCost)
        {
            gold -= spearmanUpgradeCost;
            spearmanLevel++;

            if (SoundManager.Instance != null && SoundManager.Instance.unitUpgradeSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);

            SaveGame();
            UpdateUI();
            RefreshAllUnitSkins("Spearman");

            if (hireSpearmanCostText != null) StartCoroutine(AnimatePriceChange(hireSpearmanCostText));
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    public void UpgradeCavalry()
    {
        cavalryUpgradeCost = EconomyConfig.GetUpgradeCost(180, cavalryLevel);
        if(gold >= cavalryUpgradeCost)
        {
            gold -= cavalryUpgradeCost;
            cavalryLevel++;

            if (SoundManager.Instance != null && SoundManager.Instance.unitUpgradeSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);

            SaveGame();
            UpdateUI();
            RefreshAllUnitSkins("Cavalry");

            if (hireCavalryCostText != null) StartCoroutine(AnimatePriceChange(hireCavalryCostText));
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
        }
    }

    private void RefreshAllUnitSkins(string unitType)
    {
        if (unitType == "Knight")
        {
            foreach (var u in activeKnights) u.SendMessage("UpdateSkin", SendMessageOptions.DontRequireReceiver);
        }
        else if (unitType == "Archer")
        {
            foreach (var u in activeArchers) u.SendMessage("UpdateSkin", SendMessageOptions.DontRequireReceiver);
        }
        else if (unitType == "Spearman")
        {
            foreach (var u in activeSpearmen) u.SendMessage("UpdateSkin", SendMessageOptions.DontRequireReceiver);
        }
        else if (unitType == "Cavalry") 
        {
            foreach (var u in activeCavalry) u.SendMessage("UpdateSkin", SendMessageOptions.DontRequireReceiver);
        }
    }
    
    public void BuildCrossbow()
    {
        if (isCrossbowBuilt) return;
        if (wood >= crossbowBuildCostWood && stone >= crossbowBuildCostStone)
        {
            wood -= crossbowBuildCostWood;
            stone -= crossbowBuildCostStone;
            isCrossbowBuilt = true;

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.constructionSound);
            
            UpdateCrossbowVisibility();
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
        wallArcherUpgradeCost = EconomyConfig.GetUpgradeCost(200, wallArcherLevel);
        if(gold >= wallArcherUpgradeCost)
        {
            gold -= wallArcherUpgradeCost;
            wallArcherLevel++;

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
        crossbowDamageCostGold = EconomyConfig.GetUpgradeCost(150, crossbowDamageLevel);
        if (gold >= crossbowDamageCostGold)
        {
            gold -= crossbowDamageCostGold;
            crossbowDamageLevel++;

            if (SoundManager.Instance != null && SoundManager.Instance.unitUpgradeSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);

            UpdateCrossbowVisibility(); 
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
        crossbowReloadCostGold = 50 + (crossbowReloadLevel * 20) + Mathf.RoundToInt(Mathf.Pow(crossbowReloadLevel, 2) * 0.5f);
        if (gold >= crossbowReloadCostGold)
        {
            gold -= crossbowReloadCostGold;
            crossbowReloadLevel++;

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

    // Динамічні ціни (кожен рівень додає +10-20 золота до вартості найму)
    public int GetCurrentKnightCost() { return knightFixedCost + ((knightLevel - 1) * 10); }
    public int GetCurrentArcherCost() { return archerFixedCost + ((archerLevel - 1) * 12); }
    public int GetCurrentSpearmanCost() { return spearmanFixedCost + ((spearmanLevel - 1) * 10); }
    public int GetCurrentCavalryCost() { return cavalryFixedCost + ((cavalryLevel - 1) * 20); }

    public void HireKnight()
    {
        GameObject unit = TryHireUnit(knightPrefab, GetCurrentKnightCost());
        if (unit != null)
        {
            activeKnights.Add(unit.GetComponent<Knight>());
            UpdateFormationPositions();
        }
    }

    public void HireArcher()
    {
        int cost = GetCurrentArcherCost();
        if (IsTutorialTarget(hireArcherButton))
        {
            if (gold < cost) 
            {
                gold = cost;
                UpdateUI(); 
            }
        }

        GameObject unit = TryHireUnit(archerPrefab, cost);
        if (unit != null)
        {
            activeArchers.Add(unit.GetComponent<Archer>());
            UpdateFormationPositions();
        }
    }

    public void HireSpearman()
    {
        GameObject unit = TryHireUnit(spearmanPrefab, GetCurrentSpearmanCost());
        if (unit != null)
        {
            activeSpearmen.Add(unit.GetComponent<Spearman>());
            UpdateFormationPositions();
        }
    }

    public void HireCavalry()
    {
        GameObject unit = TryHireUnit(cavalryPrefab, GetCurrentCavalryCost());
        if (unit != null)
        {
            activeCavalry.Add(unit.GetComponent<Cavalry>());
            UpdateFormationPositions();
        }
    }

    private GameObject TryHireUnit(GameObject prefab, int cost)
    {
        if (Time.time - _lastHireTime < 0.1f) return null;
        _lastHireTime = Time.time;

        if(currentUnits >= maxUnits || gold < cost) return null;

        gold -= cost;
        if (QuestManager.Instance != null) {
            QuestManager.Instance.AddQuestProgress(QuestType.SpendGold, cost);
            QuestManager.Instance.AddQuestProgress(QuestType.HireUnits, 1);
        }
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

        GameObject popup = PoolRegistry.Spawn(resourcePopupPrefab, position + Vector3.up, Quaternion.identity);
        DamagePopup popupScript = popup != null ? popup.GetComponent<DamagePopup>() : null;
        if (popupScript != null) popupScript.SetupResource(icon, amount, color);
    }

    public void AddResource(ResourceType type, int amount)
    {
        if (type == ResourceType.Gold && amount > 0 && QuestManager.Instance != null) 
            QuestManager.Instance.AddQuestProgress(QuestType.EarnGold, amount);

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
        foreach (var p in GameObject.FindGameObjectsWithTag("Projectile")) Destroy(p);
    }

    void ClearDeadUnits()
    {
        foreach (var k in FindObjectsByType<Knight>(FindObjectsSortMode.None))
            if (k.CompareTag("Untagged")) Destroy(k.gameObject);

        foreach (var a in FindObjectsByType<Archer>(FindObjectsSortMode.None))
            if (a.CompareTag("Untagged")) Destroy(a.gameObject);

        foreach (var s in FindObjectsByType<Spearman>(FindObjectsSortMode.None))
            if (s.CompareTag("Untagged")) Destroy(s.gameObject);
            
        foreach (var c in FindObjectsByType<Cavalry>(FindObjectsSortMode.None)) 
            if (c.CompareTag("Untagged")) Destroy(c.gameObject);
    }

    void ClearDeadEnemies()
    {
        foreach (var e in FindObjectsByType<EnemyStats>(FindObjectsSortMode.None))
            if (e.CompareTag("Untagged")) Destroy(e.gameObject);

        foreach (var go in GameObject.FindGameObjectsWithTag("Untagged"))
        {
            if (go.GetComponent<EnemySpearman>() || go.GetComponent<EnemyHorse>() || go.GetComponent<EnemyArcher>() || go.GetComponent<Guard>() || go.GetComponent<Boss>() || go.GetComponent<BatteringRam>())
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
        isDefeated = true; 
        isWaveInProgress = false; 
        Time.timeScale = 1f;

        if(defeatPanel) defeatPanel.SetActive(true);
        if (defeatText != null) defeatText.text = "Avanpost control was lost";

        if (enemyPredictPanel != null) enemyPredictPanel.SetActive(false);
        if (waveTimerBar != null) waveTimerBar.gameObject.SetActive(false);
        if (waveText != null) waveText.gameObject.SetActive(false);

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
        
        foreach(var c in FindObjectsByType<Cavalry>(FindObjectsSortMode.None)) Destroy(c.gameObject); 
        activeCavalry.Clear();

        if (currentMineObject != null) Destroy(currentMineObject);
        if (isMineBuilt) SpawnMineObject();

        currentUnits = 0;
        enemiesAlive = 0;
        isResettingUnits = false;
        
        isWaitingForNextWave = false; 
        isWaveInProgress = false;

        isFirstWaveStarted = false; 

        waveTimer = 0f;
        if (waveTimerBar != null)
        {
            waveTimerBar.value = 0f;
        }

        if (spawner != null) spawner.PrepareForWave(currentWave);

        ToggleWaveHUD(false, true); 
        TriggerUIFade(true);  

        UpdateUI();
        UpdateBarracksStateUI();
        
        if (SoundManager.Instance != null) SoundManager.Instance.PlayIdleMusic(); 
        
        StartCoroutine(RecalculateUnitsNextFrame());
    }

    public void ShowExitConfirmation()
    {
        if (exitConfirmPanel != null)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);
            StartCoroutine(AnimatePanel(exitConfirmPanel, true));
        }
    }

    public void HideExitConfirmation()
    {
        if (exitConfirmPanel != null)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);
            StartCoroutine(AnimatePanel(exitConfirmPanel, false));
        }
    }

    public void ConfirmExit()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);
        SaveGame();
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void ResumeGame()
    {
        // Анімація закриття для вікон, що ставлять гру на паузу
        if (settingsPanel != null && settingsPanel.activeSelf) StartCoroutine(AnimatePanel(settingsPanel, false));
        if (infoPanel != null && infoPanel.activeSelf) StartCoroutine(AnimatePanel(infoPanel, false));
        
        Time.timeScale = 1f; 
        UpdateUI();
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);
    }

    void SpawnReturningUnits(GameObject prefab, int count, string type)
    {
        if (prefab == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            // Спавнимо юніта на стандартній точці бази
            GameObject newUnit = Instantiate(prefab, unitSpawnPoint.position, Quaternion.identity);

            // Розподіляємо по активних списках
            if (type == "Knight") activeKnights.Add(newUnit.GetComponent<Knight>());
            else if (type == "Archer") activeArchers.Add(newUnit.GetComponent<Archer>());
            else if (type == "Spearman") activeSpearmen.Add(newUnit.GetComponent<Spearman>());
            else if (type == "Cavalry") activeCavalry.Add(newUnit.GetComponent<Cavalry>());

            currentUnits++;
        }
    }

    private void OnApplicationQuit() { SaveGame(); }
    private void OnApplicationPause(bool pauseStatus) { if (pauseStatus) SaveGame(); }


    public bool IsTutorialTarget(Button btn)
    {
        if (TutorialManager.Instance == null || btn == null) return false;
        if (TutorialManager.Instance.currentStepIndex >= TutorialManager.Instance.steps.Count) return false;
        
        RectTransform targetUI = TutorialManager.Instance.steps[TutorialManager.Instance.currentStepIndex].targetUI;
        if (targetUI == null) return false;

        if (btn.transform == targetUI || btn.transform.IsChildOf(targetUI) || targetUI.IsChildOf(btn.transform)) return true;

        string targetName = targetUI.name.ToLower();
        string btnName = btn.name.ToLower();

        if (targetName.Contains("barracks") && btnName.Contains("barracks")) return true;
        if (targetName.Contains("cart") && btnName.Contains("cart")) return true;
        if (targetName.Contains("request") && btnName.Contains("request")) return true;
        if (targetName.Contains("build") && btnName.Contains("build")) return true;
        if (targetName.Contains("hammer") && btnName.Contains("build")) return true;
        if (targetName.Contains("construction") && btnName.Contains("build")) return true;
        if (targetName.Contains("mine") && btnName.Contains("mine")) return true;

        return false;
    }

    // --- 1. ФІКС: Оновлення ресурсів та Слави ---
    // --- ФІКС: Нарахування Слави ---
    // Анімація вилітання Слави

    public void AddGlory(int amount)
    {
        if (amount <= 0) return;
        glory += amount;
        SaveSystem.SetInt("PlayerGlory", glory);
        UpdateResourcesUI();

        // Запускаємо анімацію "+X"
        if (gloryPopupText != null)
        {
            StopCoroutine("AnimateGloryPopup"); 
            StartCoroutine("AnimateGloryPopup", amount);
        }
    }

    // --- ФІКС: Оновлення ресурсів (замінено playerGoldText на goldText) ---

    // --- ФІКС: Нарахування вбивств і конвертація у Славу ---
    public void AddKillProgress(int amount)
    {
        currentKills += amount;
        killsForGloryCounter += amount;

        // Кожні 5 вбитих ворогів дають 1 Славу
        if (killsForGloryCounter >= 5)
        {
            AddGlory(killsForGloryCounter / 5);
            killsForGloryCounter %= 5;
        }

        while (currentKills >= killsToNextGem)
        {
            currentKills -= killsToNextGem;
            gems++;
            killsToNextGem = Mathf.RoundToInt(killsToNextGem * 1.15f); 
        }

        UpdateMetaUI();
    }

    // --- Оновлення вікна мета-навичок ---

    // === ДОДАНО: Анімація зміни ціни ===
}