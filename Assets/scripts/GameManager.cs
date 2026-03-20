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
    
    public TMP_Text statsText;      
    public Button upgradeBtn;        
    public UICostGroup costUI;      
}

public class GameManager : MonoBehaviour
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
            // ВКАЖИ ТУТ: Скільки фізичних юнітів має бути в одному загоні?
            int unitsPerSquad = 5; 

            // Множимо вибір (1 або 0) на кількість юнітів у загоні (5)
            SpawnReturningUnits(knightPrefab, CrossSceneData.knightsCount * unitsPerSquad, "Knight");
            SpawnReturningUnits(archerPrefab, CrossSceneData.archersCount * unitsPerSquad, "Archer");
            SpawnReturningUnits(spearmanPrefab, CrossSceneData.spearmenCount * unitsPerSquad, "Spearman");
            SpawnReturningUnits(cavalryPrefab, CrossSceneData.cavalryCount * unitsPerSquad, "Cavalry");
            
            // Оновлюємо формацію, щоб вони гарно вишикувалися
            StartCoroutine(RecalculateUnitsNextFrame());
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

    public void SetCinematicUI(bool isCinematic)
    {
        isCinematicActive = isCinematic;

        if (goldText != null && goldText.canvas != null)
        {
            CanvasGroup mainCg = goldText.canvas.GetComponent<CanvasGroup>();
            if (mainCg == null) mainCg = goldText.canvas.gameObject.AddComponent<CanvasGroup>();
            
            mainCg.alpha = isCinematic ? 0f : 1f;
            mainCg.interactable = !isCinematic;
            mainCg.blocksRaycasts = !isCinematic;
        }

        if (isCinematic) CloseAllPanels();
        else TriggerUIFade(!isWaveInProgress); 
    }

    public void OnTowerToggleClicked()
    {
        if (isWaveInProgress || isCinematicActive) return; 
        isTowerPanelOpen = !isTowerPanelOpen;
        AnimateTowerPanel(isTowerPanelOpen, true);
    }

    private void AnimateTowerPanel(bool open, bool interactable)
    {
        if (towerUpgradePanelRect == null) return;
        if (towerPanelCoroutine != null) StopCoroutine(towerPanelCoroutine);
        towerPanelCoroutine = StartCoroutine(SlideTowerPanelRoutine(open, interactable));
    }

    private IEnumerator SlideTowerPanelRoutine(bool open, bool interactable)
    {
        CanvasGroup cg = towerUpgradePanelRect.GetComponent<CanvasGroup>();
        if (cg == null) cg = towerUpgradePanelRect.gameObject.AddComponent<CanvasGroup>();

        cg.interactable = interactable;
        cg.blocksRaycasts = interactable;

        float duration = 0.3f;
        float t = 0f;

        Vector2 startPos = towerUpgradePanelRect.anchoredPosition;
        Vector2 targetPos = new Vector2(open ? towerOpenPosX : towerClosedPosX, startPos.y);

        float startAlpha = cg.alpha;
        float targetAlpha = interactable ? 1f : 0f; 

        Quaternion startRot = towerArrowIcon != null ? towerArrowIcon.localRotation : Quaternion.identity;
        Quaternion targetRot = Quaternion.Euler(0, 0, open ? 180f : 0f);

        if (SoundManager.Instance != null && SoundManager.Instance.clickSound != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / duration;
            float smooth = progress * (2f - progress); 

            towerUpgradePanelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smooth);
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, smooth);
            
            if (towerArrowIcon != null) 
                towerArrowIcon.localRotation = Quaternion.Lerp(startRot, targetRot, smooth);

            yield return null;
        }

        towerUpgradePanelRect.anchoredPosition = targetPos;
        cg.alpha = targetAlpha;
        if (towerArrowIcon != null) towerArrowIcon.localRotation = targetRot;
    }

    public void SetManualTarget(Transform target)
    {
        manualTarget = target;
        if (targetIndicator != null) targetIndicator.Show(target);
    }

    private void CacheTopUI()
    {
        if (waveTimerBar != null) originalTopPos[waveTimerBar.GetComponent<RectTransform>()] = waveTimerBar.GetComponent<RectTransform>().anchoredPosition;
        if (waveText != null) originalTopPos[waveText.GetComponent<RectTransform>()] = waveText.GetComponent<RectTransform>().anchoredPosition;
        if (enemyPredictPanel != null) originalTopPos[enemyPredictPanel.GetComponent<RectTransform>()] = enemyPredictPanel.GetComponent<RectTransform>().anchoredPosition;
    }

    private void ToggleWaveHUD(bool showWave, bool instant = false)
    {
        if (originalTopPos.Count == 0) CacheTopUI(); 

        if (topHudCoroutine != null) StopCoroutine(topHudCoroutine);

        if (instant)
        {
            SetWaveElementsActive(showWave);
            SetPredictElementsActive(showWave); 
            SnapTopElements(showWave);
        }
        else
        {
            topHudCoroutine = StartCoroutine(AnimateTopHUD(showWave));
        }
    }

    private IEnumerator AnimateTopHUD(bool waveStarting)
    {
        float duration = 0.35f;

        List<RectTransform> waveEls = new List<RectTransform>();
        if (waveTimerBar != null) waveEls.Add(waveTimerBar.GetComponent<RectTransform>());
        if (waveText != null && (waveTimerBar == null || !waveText.transform.IsChildOf(waveTimerBar.transform))) waveEls.Add(waveText.GetComponent<RectTransform>());

        List<RectTransform> predEls = new List<RectTransform>();
        if (enemyPredictPanel != null) predEls.Add(enemyPredictPanel.GetComponent<RectTransform>());

        if (waveStarting) 
        {
            SetWaveElementsActive(true);
            SetPredictElementsActive(true);

            SetGroupStartOffset(waveEls, true); 
            SetGroupStartOffset(predEls, true); 

            yield return StartCoroutine(SlideGroup(waveEls, true, duration));
            yield return StartCoroutine(SlideGroup(predEls, true, duration));
        }
        else 
        {
            yield return StartCoroutine(SlideGroup(predEls, false, duration));
            SetPredictElementsActive(false);

            yield return StartCoroutine(SlideGroup(waveEls, false, duration));
            SetWaveElementsActive(false);
        }
    }

    private void SetWaveElementsActive(bool active)
    {
        if (waveTimerBar != null) waveTimerBar.gameObject.SetActive(active);
        if (waveText != null) waveText.gameObject.SetActive(active);
    }

    private void SetPredictElementsActive(bool active)
    {
        if (enemyPredictPanel != null) enemyPredictPanel.SetActive(active);
    }

    private void SetGroupStartOffset(List<RectTransform> group, bool hidden)
    {
        foreach(var rt in group)
        {
            if (rt == null || !originalTopPos.ContainsKey(rt)) continue;
            rt.anchoredPosition = hidden ? originalTopPos[rt] + new Vector2(0, topSlideDist) : originalTopPos[rt];
        }
    }

    private void SnapTopElements(bool waveActive)
    {
        List<RectTransform> waveEls = new List<RectTransform>();
        if (waveTimerBar != null) waveEls.Add(waveTimerBar.GetComponent<RectTransform>());
        if (waveText != null && (waveTimerBar == null || !waveText.transform.IsChildOf(waveTimerBar.transform))) waveEls.Add(waveText.GetComponent<RectTransform>());

        List<RectTransform> predEls = new List<RectTransform>();
        if (enemyPredictPanel != null) predEls.Add(enemyPredictPanel.GetComponent<RectTransform>());

        SetGroupStartOffset(waveEls, !waveActive);
        SetGroupStartOffset(predEls, !waveActive);
    }

    private IEnumerator SlideGroup(List<RectTransform> elements, bool slideIn, float duration)
    {
        float t = 0;
        while(t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / duration;
            float smooth = progress * progress * (3f - 2f * progress);

            foreach(var rt in elements)
            {
                if (rt == null || !originalTopPos.ContainsKey(rt)) continue;
                Vector2 orig = originalTopPos[rt];
                Vector2 hidden = orig + new Vector2(0, topSlideDist);
                rt.anchoredPosition = Vector2.Lerp(slideIn ? hidden : orig, slideIn ? orig : hidden, smooth);
            }
            yield return null;
        }

        foreach(var rt in elements)
        {
            if (rt == null || !originalTopPos.ContainsKey(rt)) continue;
            rt.anchoredPosition = slideIn ? originalTopPos[rt] : originalTopPos[rt] + new Vector2(0, topSlideDist);
        }
    }

    public void TriggerUIFade(bool show)
    {
        if (uiFadeCoroutine != null) StopCoroutine(uiFadeCoroutine);
        uiFadeCoroutine = StartCoroutine(SlideUIButtons(show));

        isTowerPanelOpen = show;
        AnimateTowerPanel(show, show);
    }

    private IEnumerator SlideUIButtons(bool show)
    {
        float duration = 0.5f; 
        float slideDistance = 400f; 
        float t = 0f;

        Button[] buttonsToAnimate = new Button[] { 
            hammerButton, openShopButton, openMetaShopButton, nextWaveButton, barracksIconButton, openMapButton 
        };
        
        List<RectTransform> rects = new List<RectTransform>();
        List<CanvasGroup> cgs = new List<CanvasGroup>();

        if (show)
        {
            foreach (var btn in buttonsToAnimate)
            {
                if (btn == null) continue;
                if (btn == barracksIconButton && barracksLevel == 0) continue; 
                btn.gameObject.SetActive(true);
            }
        }

        foreach (var btn in buttonsToAnimate)
        {
            if (btn == null || !btn.gameObject.activeSelf) continue;

            RectTransform rect = btn.GetComponent<RectTransform>();
            if (rect != null)
            {
                rects.Add(rect);
                if (!originalBtnPositions.ContainsKey(rect))
                {
                    originalBtnPositions[rect] = rect.anchoredPosition; 
                }
            }

            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
            cgs.Add(cg);

            cg.interactable = show;
            cg.blocksRaycasts = show;
        }

        float startLerp = show ? 0f : 1f;
        float targetLerp = show ? 1f : 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            float smoothStep = progress * (2f - progress); 
            float currentLerp = Mathf.Lerp(startLerp, targetLerp, smoothStep);

            for (int i = 0; i < rects.Count; i++)
            {
                Vector2 origPos = originalBtnPositions[rects[i]];
                rects[i].anchoredPosition = new Vector2(origPos.x, origPos.y - (1f - currentLerp) * slideDistance);
                cgs[i].alpha = currentLerp; 
            }
            yield return null;
        }

        for (int i = 0; i < rects.Count; i++)
        {
            Vector2 origPos = originalBtnPositions[rects[i]];
            rects[i].anchoredPosition = show ? origPos : new Vector2(origPos.x, origPos.y - slideDistance);
            cgs[i].alpha = show ? 1f : 0f;
        }

        if (!show)
        {
            foreach (var btn in buttonsToAnimate)
            {
                if (btn != null) btn.gameObject.SetActive(false);
            }
        }
        else
        {
            UpdateUI(); 
        }
    }

    public void CloseAllPanels()
    {
        if (constructionPanel != null) constructionPanel.SetActive(false);
        if (constructionPanelNew != null) constructionPanelNew.SetActive(false);
        if (barracksUpgradePanel != null) barracksUpgradePanel.SetActive(false);
        if (barracksPanelNew != null) barracksPanelNew.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (shopPanelNew != null) shopPanelNew.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        if (metaShopPanel != null) metaShopPanel.SetActive(false);
        
        // ДОДАНО ЗАКРИТТЯ ІНШИХ ВІКОН
        if (CampaignManager.Instance != null && CampaignManager.Instance.mainCampaignPanel != null)
            CampaignManager.Instance.mainCampaignPanel.SetActive(false);
        if (TavernManager.Instance != null && TavernManager.Instance.tavernPanel != null)
            TavernManager.Instance.CloseTavern();
        
        if (settingsPanel != null && Time.timeScale != 0) settingsPanel.SetActive(false);
        
        Time.timeScale = 1f;
        UpdateUI(); 
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

    void ResolveUIRefs()
    {
        if (hammerButton == null) hammerButton = FindUIButton("BuildButton");
        if (barracksIconButton == null) barracksIconButton = FindUIButton("BarracksButton");
        if (openShopButton == null) openShopButton = FindUIButton("ForgeButton");
        if (towerButton == null) towerButton = FindUIButton("TowerButton");

        if (hireKnightButton == null) hireKnightButton = FindUIButton("HireKnight");
        if (hireArcherButton == null) hireArcherButton = FindUIButton("HireArcher");
        if (hireSpearmanButton == null) hireSpearmanButton = FindUIButton("HireSpearman");
        if (hireCavalryButton == null) hireCavalryButton = FindUIButton("HireCavalry"); 
        
        if (volleyBarrageButton == null) volleyBarrageButton = FindUIButton("VolleyBarrageButton");
        if (volleyBarrageButton == null) volleyBarrageButton = FindUIButton("VolleyButton");

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
        
        if (buildCrossbowButton == null) buildCrossbowButton = FindButtonInRow(construction, "Crossbow_Row");
        if (buildCrossbowCostUI == null) buildCrossbowCostUI = FindCostGroupInRow(construction, "Crossbow_Row");

        if (castleUpgradeCostUI == null) castleUpgradeCostUI = FindCostGroupInRow(construction, "Tower Upgrade_Row", "Castle Upgrade_Row");

        if (upgradeLimitButton == null) upgradeLimitButton = FindButtonInRow(barracks, "Barracks_Row", "Upgrade Units Limit_Row");
        
        if (unlockSpearmanButton == null) unlockSpearmanButton = FindButtonInRow(barracks, "Unlock Spearman_Row", "Spearman_Row");
        if (unlockCavalryButton == null) unlockCavalryButton = FindButtonInRow(barracks, "Unlock Cavalry_Row", "Cavalry_Row");

        if (upgradeWallArcherButton == null) upgradeWallArcherButton = FindButtonInRow(shop, "WallArcher_Row", "WallArchers_Row");

        if (closeConstructionBtn == null) closeConstructionBtn = FindUIButtonInPanel(construction, "CloseButton");
        if (closeBarracksBtn == null) closeBarracksBtn = FindUIButtonInPanel(barracks, "CloseButton");
        if (closeShopBtn == null) closeShopBtn = FindUIButtonInPanel(shop, "CloseButton");

        InitRowGroup(unlockSpearmanButton, ref unlockSpearmanRowGroup);
        InitRowGroup(unlockCavalryButton, ref unlockCavalryRowGroup);

        if (waveTimerBar != null && tickContainer == null)
        {
            tickContainer = waveTimerBar.transform.Find("Background");
            if (tickContainer == null) tickContainer = waveTimerBar.transform;
        }
    }
    
    void InitRowGroup(Button btn, ref CanvasGroup rowGroup)
    {
        if (btn != null && rowGroup == null)
        {
            Transform rowTransform = btn.transform;
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
                rowGroup = rowTransform.GetComponent<CanvasGroup>();
                if (rowGroup == null) rowGroup = rowTransform.gameObject.AddComponent<CanvasGroup>();
            }
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

    void ResolveUnitLockUI()
    {
        var shop = shopPanel != null ? shopPanel : shopPanelNew;
        if (shop == null) return;

        var texts = shop.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            if (t.text == null) continue;
            
            string lowerText = t.text.ToLower();
            if (lowerText.Contains("spear") && spearmanRowGroup == null)
            {
                CreateLockForShopRow(t.transform, ref spearmanRowGroup, ref spearmanLockIcon);
            }
            else if ((lowerText.Contains("cavalry") || lowerText.Contains("horse")) && cavalryRowGroup == null)
            {
                CreateLockForShopRow(t.transform, ref cavalryRowGroup, ref cavalryLockIcon);
            }
        }
    }
    
    void CreateLockForShopRow(Transform textTransform, ref CanvasGroup group, ref GameObject lockIcon)
    {
        Transform row = textTransform;
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
            group = row.GetComponent<CanvasGroup>();
            if (group == null) group = row.gameObject.AddComponent<CanvasGroup>();

            if (lockIcon == null)
            {
                var lockGo = new GameObject("LockIcon", typeof(RectTransform), typeof(Image));
                lockGo.transform.SetParent(row, false);
                var rt = lockGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.9f, 0.5f);
                rt.anchorMax = new Vector2(0.9f, 0.5f);
                rt.sizeDelta = new Vector2(28, 28);
                lockIcon = lockGo;
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
        
        if (exitGameButton != null) { exitGameButton.onClick.RemoveAllListeners(); exitGameButton.onClick.AddListener(ExitGame); }

        if (openMetaShopButton != null) { openMetaShopButton.onClick.RemoveAllListeners(); openMetaShopButton.onClick.AddListener(ToggleMetaShop); }
        if (closeMetaShopButton != null) { closeMetaShopButton.onClick.RemoveAllListeners(); closeMetaShopButton.onClick.AddListener(ToggleMetaShop); }

        if (buildBarracksBtnInMenu != null) { buildBarracksBtnInMenu.onClick.RemoveAllListeners(); buildBarracksBtnInMenu.onClick.AddListener(BuildOrUpgradeBarracks); }
        if (buildMineButton != null) { buildMineButton.onClick.RemoveAllListeners(); buildMineButton.onClick.AddListener(BuildOrUpgradeMine); }
        if (buildSpikesButton != null) { buildSpikesButton.onClick.RemoveAllListeners(); buildSpikesButton.onClick.AddListener(BuildSpikes); }
        if (towerButton != null) { towerButton.onClick.RemoveAllListeners(); towerButton.onClick.AddListener(UpgradeDamage); }
        if (castleUpgradeButton != null) { castleUpgradeButton.onClick.RemoveAllListeners(); castleUpgradeButton.onClick.AddListener(UpgradeCastleHP); }
        
        if (buildCrossbowButton != null) { buildCrossbowButton.onClick.RemoveAllListeners(); buildCrossbowButton.onClick.AddListener(BuildCrossbow); }

        if (knightEvoUI != null && knightEvoUI.upgradeBtn != null) { knightEvoUI.upgradeBtn.onClick.RemoveAllListeners(); knightEvoUI.upgradeBtn.onClick.AddListener(UpgradeKnights); }
        if (archerEvoUI != null && archerEvoUI.upgradeBtn != null) { archerEvoUI.upgradeBtn.onClick.RemoveAllListeners(); archerEvoUI.upgradeBtn.onClick.AddListener(UpgradeArchers); }
        if (spearmanEvoUI != null && spearmanEvoUI.upgradeBtn != null) { spearmanEvoUI.upgradeBtn.onClick.RemoveAllListeners(); spearmanEvoUI.upgradeBtn.onClick.AddListener(UpgradeSpearman); }
        if (cavalryEvoUI != null && cavalryEvoUI.upgradeBtn != null) { cavalryEvoUI.upgradeBtn.onClick.RemoveAllListeners(); cavalryEvoUI.upgradeBtn.onClick.AddListener(UpgradeCavalry); } 

        if (upgradeWallArcherButton != null) { upgradeWallArcherButton.onClick.RemoveAllListeners(); upgradeWallArcherButton.onClick.AddListener(UpgradeWallArchers); }

        if (upgradeLimitButton != null) { upgradeLimitButton.onClick.RemoveAllListeners(); upgradeLimitButton.onClick.AddListener(BuyUnitLimitUpgrade); }
        if (unlockSpearmanButton != null) { unlockSpearmanButton.onClick.RemoveAllListeners(); unlockSpearmanButton.onClick.AddListener(UnlockSpearman); }
        if (unlockCavalryButton != null) { unlockCavalryButton.onClick.RemoveAllListeners(); unlockCavalryButton.onClick.AddListener(UnlockCavalry); }
        
        if (upgradeCrossbowDamageButton != null) { upgradeCrossbowDamageButton.onClick.RemoveAllListeners(); upgradeCrossbowDamageButton.onClick.AddListener(UpgradeCrossbowDamage); }
        if (upgradeCrossbowReloadButton != null) { upgradeCrossbowReloadButton.onClick.RemoveAllListeners(); upgradeCrossbowReloadButton.onClick.AddListener(UpgradeCrossbowReload); }

        if (hireKnightButton != null) { hireKnightButton.onClick.RemoveAllListeners(); hireKnightButton.onClick.AddListener(HireKnight); }
        if (hireArcherButton != null) { hireArcherButton.onClick.RemoveAllListeners(); hireArcherButton.onClick.AddListener(HireArcher); }
        if (hireSpearmanButton != null) { hireSpearmanButton.onClick.RemoveAllListeners(); hireSpearmanButton.onClick.AddListener(HireSpearman); }
        if (hireCavalryButton != null) { hireCavalryButton.onClick.RemoveAllListeners(); hireCavalryButton.onClick.AddListener(HireCavalry); } 
        
        if (nextWaveButton != null) { nextWaveButton.onClick.RemoveAllListeners(); nextWaveButton.onClick.AddListener(NextWave); }
        if (continueButton != null) { continueButton.onClick.RemoveAllListeners(); continueButton.onClick.AddListener(ResumeGame); }
        
        if (openSettingsButton != null) { openSettingsButton.onClick.RemoveAllListeners(); openSettingsButton.onClick.AddListener(ToggleSettingsPanel); }
        if (closeSettingsBtn != null) { closeSettingsBtn.onClick.RemoveAllListeners(); closeSettingsBtn.onClick.AddListener(ToggleSettingsPanel); }
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
        if (metaShopPanel != null)
        {
            bool isActive = !metaShopPanel.activeSelf;
            metaShopPanel.SetActive(isActive);

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);

            if (isActive)
            {
                UpdateMetaUI();
                CloseAllPanels();
                metaShopPanel.SetActive(true); 
            }
        }
        UpdateUI();
    }

    void UpdateMetaSlot(MetaSkillUI ui, int currentLevel, int baseCost, int costPerLevel, string lvlStr, string descStr, bool isUnlocked)
    {
        if (ui == null) return;
        
        int cost = baseCost + (currentLevel * costPerLevel);
        if (ui.levelText != null) ui.levelText.text = lvlStr;
        
        if (!isUnlocked)
        {
            if (ui.descText != null) ui.descText.text = "<color=#FF6666>Requires Mine building</color>";
            if (ui.costText != null) ui.costText.text = "---"; 
            UpdateButtonState(ui.upgradeBtn, false); 
            if (ui.lockedOverlay != null) ui.lockedOverlay.SetActive(true); 
        }
        else
        {
            if (ui.descText != null) ui.descText.text = descStr;
            if (ui.costText != null) ui.costText.text = cost.ToString();
            
            bool canAfford = gems >= cost;
            UpdateButtonState(ui.upgradeBtn, canAfford); 
            if (ui.lockedOverlay != null) ui.lockedOverlay.SetActive(false); 
        }
    }

    void UpdateUnitEvoUI(UnitEvolutionUI ui, int level, Sprite[] skins, string unitName, int curDmg, int nextDmg, string tactics, int cost, bool canAfford, bool isUnlocked)
    {
        if (ui == null) return;

        if ((ui.generatedLocks == null || ui.generatedLocks.Length != ui.skinImages.Length) && ui.skinImages != null)
        {
            ui.generatedLocks = new GameObject[ui.skinImages.Length];
            for (int i = 0; i < ui.skinImages.Length; i++)
            {
                if (ui.skinImages[i] != null && ui.lockSprite != null)
                {
                    GameObject lockObj = new GameObject("AutoLock_" + i);
                    lockObj.transform.SetParent(ui.skinImages[i].transform, false);
                    
                    Image lockImg = lockObj.AddComponent<Image>();
                    lockImg.sprite = ui.lockSprite;
                    lockImg.preserveAspect = true; 
                    
                    RectTransform rt = lockObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(35f, 35f); 
                    
                    ui.generatedLocks[i] = lockObj;
                    lockObj.SetActive(false); 
                }
            }
        }

        int levelsPerSkin = 5;
        int maxSkins = skins != null ? skins.Length : 0;
        
        int curSkinIdx = (level - 1) / levelsPerSkin;
        int activePips = ((level - 1) % levelsPerSkin) + 1;

        if (ui.skinImages != null)
        {
            for (int i = 0; i < ui.skinImages.Length; i++)
            {
                if (ui.skinImages[i] != null)
                {
                    if (skins != null && i < skins.Length && skins[i] != null)
                    {
                        ui.skinImages[i].sprite = skins[i];
                    }

                    if (i <= curSkinIdx) 
                    {
                        ui.skinImages[i].color = Color.white; 
                        
                        if (ui.generatedLocks != null && i < ui.generatedLocks.Length && ui.generatedLocks[i] != null) 
                            ui.generatedLocks[i].SetActive(false); 
                    }
                    else 
                    {
                        ui.skinImages[i].color = new Color(0.15f, 0.15f, 0.15f, 1f); 
                        
                        if (ui.generatedLocks != null && i < ui.generatedLocks.Length && ui.generatedLocks[i] != null) 
                            ui.generatedLocks[i].SetActive(true); 
                    }
                }
            }
        }

        if (ui.progressPips != null)
        {
            for (int i = 0; i < ui.progressPips.Length; i++)
            {
                if (ui.progressPips[i] != null)
                {
                    if (curSkinIdx >= maxSkins - 1 && maxSkins > 0 && activePips == levelsPerSkin) 
                    {
                        ui.progressPips[i].color = new Color(0.45f, 0.75f, 0.15f, 1f);
                    }
                    else if (i < activePips)
                    {
                        ui.progressPips[i].color = new Color(0.45f, 0.75f, 0.15f, 1f); 
                    }
                    else
                    {
                        ui.progressPips[i].color = new Color(0.3f, 0.25f, 0.2f, 1f); 
                    }
                }
            }
        }

        if (ui.statsText != null)
        {
            ui.statsText.text = $"<color=#3E2723><size=140%><b>{unitName} LV {level}</b></size></color>\n" +
                                $"<color=#4A2E1B><size=95%><color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_sword\" tint=1></voffset></size></color> DMG: {curDmg} → </color><color=#4CAF50>{nextDmg}</color></size>\n" +
                                $"<size=80%>{tactics}</size>";
        }

        if (!isUnlocked)
        {
            UpdateButtonState(ui.upgradeBtn, false);
            UpdateCostUIGroup(ui.costUI, ResourceType.Gold, 0, ResourceType.Wood, 0, "LOCKED");
        }
        else
        {
            UpdateButtonState(ui.upgradeBtn, canAfford);
            UpdateCostUIGroup(ui.costUI, ResourceType.Gold, cost);
        }
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
        if (panel != null)
        {
            bool isActive = !panel.activeSelf;
            panel.SetActive(isActive);

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);

            if (isActive)
            {
                CloseAllPanels();
                panel.SetActive(true);
                UpdateBarracksStateUI();
            }
        }
        UpdateUI();
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
                CloseAllPanels();
                panel.SetActive(true);
            }
        }
        UpdateUI();
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
                CloseAllPanels();
                panel.SetActive(true);
            }
        }
        UpdateUI();
    }

    public void ToggleInfoPanel()
    {
        if (infoPanel != null)
        {
            bool isOpening = !infoPanel.activeSelf;

            if (isOpening)
            {
                CloseAllPanels(); 
                infoPanel.SetActive(true); // ЄДИНЕ місце, де панель вмикається
                Time.timeScale = 0f; 
            }
            else
            {
                infoPanel.SetActive(false); // ЄДИНЕ місце, де панель вимикається
                Time.timeScale = 1f; 
            }

            if (SoundManager.Instance != null) 
                SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);
        }
        UpdateUI();
    }

    public void ToggleSettingsPanel()
    {
        if (settingsPanel == null) return;

        bool isOpening = !settingsPanel.activeSelf;

        if (isOpening)
        {
            CloseAllPanels(); 
            settingsPanel.SetActive(true); 
            Time.timeScale = 0f; 
        }
        else
        {
            ResumeGame(); 
        }

        if (SoundManager.Instance != null) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);
            
        UpdateUI();
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
            costUI.icon1.gameObject.SetActive(true);
            costUI.text1.gameObject.SetActive(true);
            costUI.icon1.sprite = GetResourceIcon(type1);
            costUI.text1.text = cost1.ToString();
            costUI.text1.alignment = TextAlignmentOptions.Left;
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
    
    private void UpdateConstructionInfoUI()
    {
        if (barracksInfoText != null)
        {
            if (barracksLevel <= 0) barracksInfoText.text = "<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_helmet\" tint=1></voffset></size></color> +Unlock units / +cap";
            else
            {
                int minCap = barracksBaseCap;
                int maxCap = GetBarracksCapLimit();
                barracksInfoText.text = $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_helmet\" tint=1></voffset></size></color> Units cap: {minCap} → {maxCap}";
            }
        }

        if (mineInfoText != null)
        {
            if (!isMineBuilt || mineLevel <= 0) mineInfoText.text = "<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_coin\" tint=1></voffset></size></color> +Gold income";
            else mineInfoText.text = $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_coin\" tint=1></voffset></size></color> Gold income: Lvl {mineLevel}";
        }

        if (castleInfoText != null && castle != null)
        {
            int currentHp = castle.maxHealth;
            int nextHp = currentHp + castle.hpBonusPerUpgrade;
            castleInfoText.text = $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_heart\" tint=1></voffset></size></color> HP: {currentHp} → {nextHp}";
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

    void UpdateBarracksStateUI()
    {
        if (barracksIconButton == null) barracksIconButton = FindUIButton("BarracksButton");
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

    void UpdateUpgradeMenuPrice()
    {
        int currentCap = GetBarracksCapLimit();

        if (maxUnits >= currentCap)
        {
            UpdateCostUIGroup(upgradeLimitCostUI, ResourceType.Gold, 0, ResourceType.Wood, 0, "MAX");

            if (upgradeLimitInfoText != null)
            {
                upgradeLimitInfoText.text =
                    $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_helmet\" tint=1></voffset></size></color> Units cap: {maxUnits} (MAX)\n" +
                    "Upgrade Barracks to unlock more unit slots";
            }
        }
        else
        {
            UpdateCostUIGroup(upgradeLimitCostUI, ResourceType.Gold, GetSlotUpgradeCost());

            if (upgradeLimitInfoText != null)
            {
                int nextLimit = maxUnits + 1;
                upgradeLimitInfoText.text = $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_helmet\" tint=1></voffset></size></color> Units cap: {maxUnits} → {nextLimit}";
            }
        }

        UpdateCostUIGroup(unlockSpearmanCostUI, ResourceType.Gold, isSpearmanUnlocked ? 0 : spearmanUnlockCost, ResourceType.Wood, 0, isSpearmanUnlocked ? "UNLOCKED" : "");
        
        string cavStatus = "";
        if (isCavalryUnlocked) cavStatus = "UNLOCKED";
        else if (barracksLevel < 3) cavStatus = "REQ LVL 3";
        UpdateCostUIGroup(unlockCavalryCostUI, ResourceType.Gold, isCavalryUnlocked ? 0 : cavalryUnlockCost, ResourceType.Wood, 0, cavStatus);

        UpdateUnitLockUI();
    }

    void UpdateUnitLockUI()
    {
        if (spearmanLockIcon != null) spearmanLockIcon.gameObject.SetActive(!isSpearmanUnlocked);
        if (spearmanRowGroup != null)
        {
            if (dimUnitWhenLocked) spearmanRowGroup.alpha = isSpearmanUnlocked ? 1f : 0.45f;
            spearmanRowGroup.interactable = isSpearmanUnlocked;
            spearmanRowGroup.blocksRaycasts = isSpearmanUnlocked;
        }
        
        if (cavalryLockIcon != null) cavalryLockIcon.gameObject.SetActive(!isCavalryUnlocked);
        if (cavalryRowGroup != null)
        {
            if (dimUnitWhenLocked) cavalryRowGroup.alpha = isCavalryUnlocked ? 1f : 0.45f;
            cavalryRowGroup.interactable = isCavalryUnlocked;
            cavalryRowGroup.blocksRaycasts = isCavalryUnlocked;
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

    public void UpdateCrossbowVisibility()
    {
        if (crossbowTowers == null || crossbowTowers.Length == 0) return;
        
        for(int i = 0; i < crossbowTowers.Length; i++)
        {
            if (crossbowTowers[i] != null) crossbowTowers[i].SetActive(false);
        }

        if (!isCrossbowBuilt) return;

        if (crossbowTowers.Length > 0 && crossbowTowers[0] != null) 
            crossbowTowers[0].SetActive(true); 
        
        if (crossbowDamageLevel >= 10 && crossbowTowers.Length > 1 && crossbowTowers[1] != null) 
            crossbowTowers[1].SetActive(true); 
        
        if (crossbowDamageLevel >= 25 && crossbowTowers.Length > 2 && crossbowTowers[2] != null) 
            crossbowTowers[2].SetActive(true); 
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

    public void ExitGame()
    {
        SaveGame();
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void ResumeGame()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        
        Time.timeScale = 1f; 
        
        UpdateUI();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);
    }

    public void SaveGame()
    {
        PlayerPrefs.SetInt("SavedGold", gold);
        PlayerPrefs.SetInt("PlayerGlory", glory); // ДОДАНО: ЗБЕРЕЖЕННЯ СЛАВИ
        PlayerPrefs.SetInt("SavedWood", wood);
        PlayerPrefs.SetInt("SavedStone", stone);
        PlayerPrefs.SetInt("SavedWave", currentWave);

        PlayerPrefs.SetInt("SavedKnightLevel", knightLevel);
        PlayerPrefs.SetInt("SavedArcherLevel", archerLevel);
        PlayerPrefs.SetInt("SavedSpearmanLevel", spearmanLevel);
        PlayerPrefs.SetInt("SavedCavalryLevel", cavalryLevel); 
        PlayerPrefs.SetInt("SavedWallArcherLevel", wallArcherLevel); 

        PlayerPrefs.SetInt("SavedMaxUnits", maxUnits);
        PlayerPrefs.SetInt("SavedBarracksLevel", barracksLevel);
        PlayerPrefs.SetInt("SavedSpearmanUnlocked", isSpearmanUnlocked ? 1 : 0);
        PlayerPrefs.SetInt("SavedCavalryUnlocked", isCavalryUnlocked ? 1 : 0); 

        PlayerPrefs.SetInt("SavedMineBuilt", isMineBuilt ? 1 : 0);
        PlayerPrefs.SetInt("SavedMineLevel", mineLevel);

        if (castle != null) PlayerPrefs.SetInt("SavedCastleLevel", castle.wallLevel);

        PlayerPrefs.SetInt("SavedTowerLevel", towerLevel);

        PlayerPrefs.SetInt("SavedCrossbowBuilt", isCrossbowBuilt ? 1 : 0);
        PlayerPrefs.SetInt("SavedCrossbowDmgLevel", crossbowDamageLevel);
        PlayerPrefs.SetInt("SavedCrossbowReloadLevel", crossbowReloadLevel);

        PlayerPrefs.SetInt("SavedGems", gems);
        PlayerPrefs.SetInt("SavedCurrentKills", currentKills);
        PlayerPrefs.SetInt("SavedKillsToNextGem", killsToNextGem);
        
        PlayerPrefs.SetInt("MetaFortifiedWalls", metaFortifiedWalls);
        PlayerPrefs.SetInt("MetaPrecisionBows", metaPrecisionBows);
        PlayerPrefs.SetInt("MetaVolleyBarrage", metaVolleyBarrage);
        PlayerPrefs.SetInt("MetaTrophyBounty", metaTrophyBounty);
        PlayerPrefs.SetInt("MetaEfficientCarts", metaEfficientCarts);
        PlayerPrefs.SetInt("MetaMendingMasonry", metaMendingMasonry);

        // ФІКС: Більше ніяких поламаних збережень на сцені битви
        if (!isCampaignBattle)
        {
            SaveUnits();
        }
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
        
        foreach (var c in FindObjectsByType<Cavalry>(FindObjectsSortMode.None)) 
        {
            if (c.CompareTag("Untagged")) continue;
            UnitSaveData u = new UnitSaveData();
            u.unitType = "Cavalry";
            u.posX = c.transform.position.x;
            u.posY = c.transform.position.y;
            u.currentHealth = c.currentHealth;
            data.units.Add(u);
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("SavedArmyData", json);
    }

    public void LoadGame()
    {
        gold = PlayerPrefs.GetInt("SavedGold", 100); 
        glory = PlayerPrefs.GetInt("PlayerGlory", 0); // ДОДАНО: ЗАВАНТАЖЕННЯ СЛАВИ
        wood = PlayerPrefs.GetInt("SavedWood", 0);
        stone = PlayerPrefs.GetInt("SavedStone", 0);
        currentWave = PlayerPrefs.GetInt("SavedWave", 1);

        knightLevel = PlayerPrefs.GetInt("SavedKnightLevel", 1);
        archerLevel = PlayerPrefs.GetInt("SavedArcherLevel", 1);
        spearmanLevel = PlayerPrefs.GetInt("SavedSpearmanLevel", 1);
        cavalryLevel = PlayerPrefs.GetInt("SavedCavalryLevel", 1); 
        wallArcherLevel = PlayerPrefs.GetInt("SavedWallArcherLevel", 1); 

        knightUpgradeCost = EconomyConfig.GetUpgradeCost(150, knightLevel);
        archerUpgradeCost = EconomyConfig.GetUpgradeCost(120, archerLevel);
        spearmanUpgradeCost = EconomyConfig.GetUpgradeCost(140, spearmanLevel);
        cavalryUpgradeCost = EconomyConfig.GetUpgradeCost(180, cavalryLevel); 
        wallArcherUpgradeCost = EconomyConfig.GetUpgradeCost(200, wallArcherLevel); 

        maxUnits = PlayerPrefs.GetInt("SavedMaxUnits", 5);
        barracksLevel = PlayerPrefs.GetInt("SavedBarracksLevel", 0);
        if (barracksLevel > 0) isBarracksBuilt = true;

        isSpearmanUnlocked = PlayerPrefs.GetInt("SavedSpearmanUnlocked", 0) == 1;
        isCavalryUnlocked = PlayerPrefs.GetInt("SavedCavalryUnlocked", 0) == 1; 
        
        if (barracksLevel == 0) 
        {
            isSpearmanUnlocked = false;
            isCavalryUnlocked = false;
        }

        isMineBuilt = PlayerPrefs.GetInt("SavedMineBuilt", 0) == 1;
        mineLevel = PlayerPrefs.GetInt("SavedMineLevel", 0);
        if (mineLevel > 0) isMineBuilt = true;

        towerLevel = PlayerPrefs.GetInt("SavedTowerLevel", 1);
        towerWoodCost = GetTowerWoodCost(towerLevel);
        towerStoneCost = GetTowerStoneCost(towerLevel);

        isCrossbowBuilt = PlayerPrefs.GetInt("SavedCrossbowBuilt", 0) == 1;
        crossbowDamageLevel = PlayerPrefs.GetInt("SavedCrossbowDmgLevel", 1);
        crossbowReloadLevel = PlayerPrefs.GetInt("SavedCrossbowReloadLevel", 1);
        
        crossbowDamageCostGold = EconomyConfig.GetUpgradeCost(150, crossbowDamageLevel);
        crossbowReloadCostGold = 50 + (crossbowReloadLevel * 20) + Mathf.RoundToInt(Mathf.Pow(crossbowReloadLevel, 2) * 0.5f);

        gems = PlayerPrefs.GetInt("SavedGems", 0);
        currentKills = PlayerPrefs.GetInt("SavedCurrentKills", 0);
        killsToNextGem = PlayerPrefs.GetInt("SavedKillsToNextGem", 25);
        if (killsToNextGem < 25) killsToNextGem = 25; 
        
        metaFortifiedWalls = PlayerPrefs.GetInt("MetaFortifiedWalls", 0);
        metaPrecisionBows = PlayerPrefs.GetInt("MetaPrecisionBows", 0);
        metaVolleyBarrage = PlayerPrefs.GetInt("MetaVolleyBarrage", 0);
        metaTrophyBounty = PlayerPrefs.GetInt("MetaTrophyBounty", 0);
        metaEfficientCarts = PlayerPrefs.GetInt("MetaEfficientCarts", 0);
        metaMendingMasonry = PlayerPrefs.GetInt("MetaMendingMasonry", 0);

        knightFixedCost = 50;
        archerFixedCost = 75;
        spearmanFixedCost = 60;
        cavalryFixedCost = 120; 

        // ФІКС: Юніти спавняться ТІЛЬКИ на основній базі
        if (!isCampaignBattle)
        {
            if (isBarracksBuilt) SpawnBarracksObject();
            if (isMineBuilt) SpawnMineObject();

            if (castle == null) castle = FindFirstObjectByType<Wall>();

            if (castle != null)
            {
                int savedCastleLvl = PlayerPrefs.GetInt("SavedCastleLevel", 1);
                castle.LoadState(savedCastleLvl);
            }

            // --- НОВИЙ КОД: ЗБЕРЕЖЕННЯ АРМІЇ У "ПУЛ", А НЕ НА БАЗУ ---
            if (CrossSceneData.isReturningFromBattle)
            {
                // Додаємо врятованих юнітів до твого резерву (Pool)
                PlayerPrefs.SetInt("PoolKnights", PlayerPrefs.GetInt("PoolKnights", 0) + CrossSceneData.knightsCount);
                PlayerPrefs.SetInt("PoolArchers", PlayerPrefs.GetInt("PoolArchers", 0) + CrossSceneData.archersCount);
                PlayerPrefs.SetInt("PoolSpearmen", PlayerPrefs.GetInt("PoolSpearmen", 0) + CrossSceneData.spearmenCount);
                PlayerPrefs.SetInt("PoolCavalry", PlayerPrefs.GetInt("PoolCavalry", 0) + CrossSceneData.cavalryCount);
                PlayerPrefs.Save();

                CrossSceneData.isReturningFromBattle = false; // Скидаємо статус
                LoadUnits(); // Завантажуємо звичайних захисників бази
            }
            else
            {
                LoadUnits(); // Звичайне завантаження
            }
        }
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
                else if (u.unitType == "Cavalry") prefabToSpawn = cavalryPrefab; 

                if (prefabToSpawn != null)
                {
                    Vector3 pos = new Vector3(u.posX, u.posY, 0);
                    GameObject newUnit = Instantiate(prefabToSpawn, pos, Quaternion.identity);

                    if (u.unitType == "Knight") newUnit.GetComponent<Knight>().LoadState(u.currentHealth);
                    else if (u.unitType == "Archer") newUnit.GetComponent<Archer>().LoadState(u.currentHealth);
                    else if (u.unitType == "Spearman") newUnit.GetComponent<Spearman>().LoadState(u.currentHealth);
                    else if (u.unitType == "Cavalry") newUnit.GetComponent<Cavalry>().LoadState(u.currentHealth); 

                    currentUnits++;
                }
            }
            UpdateUI();
        }
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

    [ContextMenu("Delete Save File")]
    public void DeleteSave()
    {
        PlayerPrefs.DeleteAll();
        gold = 100; 
        glory = 0; // ДОДАНО ОЧИЩЕННЯ СЛАВИ
        wood = 0;
        stone = 0;
        currentWave = 1;
        maxUnits = 5;
        knightLevel = 1;
        archerLevel = 1;
        spearmanLevel = 1;
        cavalryLevel = 1; 
        wallArcherLevel = 1; 

        isSpearmanUnlocked = false;
        isCavalryUnlocked = false; 
        isMineBuilt = false;
        mineLevel = 0;

        isBarracksBuilt = false;
        barracksLevel = 0;
        enemiesAlive = 0;
        currentUnits = 0;
        isWaitingForNextWave = false;

        isFirstWaveStarted = false; 
        
        towerLevel = 1;
        towerWoodCost = GetTowerWoodCost(towerLevel);
        towerStoneCost = GetTowerStoneCost(towerLevel);

        isCrossbowBuilt = false;
        crossbowDamageLevel = 1;
        crossbowReloadLevel = 1;

        gems = 0;
        currentKills = 0;
        killsToNextGem = 25;
        
        metaFortifiedWalls = 0;
        metaPrecisionBows = 0;
        metaVolleyBarrage = 0;
        metaTrophyBounty = 0;
        metaEfficientCarts = 0;
        metaMendingMasonry = 0;

        if (currentBarracksObject != null) Destroy(currentBarracksObject);
        if (currentMineObject != null) Destroy(currentMineObject);
        if (currentSpikes != null) Destroy(currentSpikes.gameObject);

        currentSpikes = null;
        manualTarget = null;
        if(targetIndicator) targetIndicator.Hide();

        if (waveTimerBar != null) waveTimerBar.value = 0f;

        knightUpgradeCost = EconomyConfig.GetUpgradeCost(150, knightLevel);
        archerUpgradeCost = EconomyConfig.GetUpgradeCost(120, archerLevel);
        spearmanUpgradeCost = EconomyConfig.GetUpgradeCost(140, spearmanLevel);
        cavalryUpgradeCost = EconomyConfig.GetUpgradeCost(180, cavalryLevel); 
        wallArcherUpgradeCost = EconomyConfig.GetUpgradeCost(200, wallArcherLevel); 

        UpdateCrossbowVisibility();
        UpdateUI();
        UpdateBarracksStateUI();
        UpdateMetaUI();

        if (CampaignManager.Instance != null)
        {
            CampaignManager.Instance.ResetCampaignLimits();
        }

        Debug.Log("Збереження видалено! (Кеш очищено)");
    }

    public void UpdateUI()
    {
        knightUpgradeCost = EconomyConfig.GetUpgradeCost(150, knightLevel);
        archerUpgradeCost = EconomyConfig.GetUpgradeCost(120, archerLevel);
        spearmanUpgradeCost = EconomyConfig.GetUpgradeCost(140, spearmanLevel);
        cavalryUpgradeCost = EconomyConfig.GetUpgradeCost(180, cavalryLevel); 
        wallArcherUpgradeCost = EconomyConfig.GetUpgradeCost(200, wallArcherLevel);
        crossbowDamageCostGold = EconomyConfig.GetUpgradeCost(150, crossbowDamageLevel);
        
        crossbowReloadCostGold = 50 + (crossbowReloadLevel * 20) + Mathf.RoundToInt(Mathf.Pow(crossbowReloadLevel, 2) * 0.5f);

        towerWoodCost = GetTowerWoodCost(towerLevel);
        towerStoneCost = GetTowerStoneCost(towerLevel);

        if (goldText) goldText.text = gold.ToString();
        if (gloryText) gloryText.text = glory.ToString(); // ДОДАНО ВІДОБРАЖЕННЯ СЛАВИ
        if (woodText) woodText.text = wood.ToString();
        if (stoneText) stoneText.text = stone.ToString();
        if (waveText) waveText.text = "Wave " + currentWave;

        if (limitText != null) limitText.text = $"{currentUnits} / {maxUnits}";

        if (hirePriceText)
        {
            string spearPrice = isSpearmanUnlocked ? $"{GetCurrentSpearmanCost()}G" : "LOCKED";
            string cavPrice = isCavalryUnlocked ? $"{GetCurrentCavalryCost()}G" : "LOCKED"; 
            hirePriceText.text = $"Knight: {GetCurrentKnightCost()}G\nSpear: {spearPrice}\nArcher: {GetCurrentArcherCost()}G\nCav: {cavPrice}";
        }

        UpdateCostUIGroup(towerCostUI, ResourceType.Wood, towerWoodCost, ResourceType.Stone, towerStoneCost);

        if (towerLevelText)
        {
            int currentTowerDmg = GetTowerDamageAtLevel(towerLevel);
            int nextTowerDmg    = GetTowerDamageAtLevel(towerLevel + 1);
            towerLevelText.text = $"Tower Lvl {towerLevel}\n<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_sword\" tint=1></voffset></size></color> DMG: {currentTowerDmg} → <color=#008800>{nextTowerDmg}</color>\n{towerWoodCost} W / {towerStoneCost} S";
        }
        
        if (crossbowDamageText != null)
        {
            int curDmg = GetCrossbowDamageAtLevel(crossbowDamageLevel);
            int nextDmg = GetCrossbowDamageAtLevel(crossbowDamageLevel + 1);
            
            int percentDmg = Mathf.RoundToInt(((float)(nextDmg - curDmg) / curDmg) * 100f);

            string extraInfo = "";
            if (crossbowDamageLevel < 10) 
                extraInfo = "<color=#FFCC00>2nd Tower at Lvl 10!</color>";
            else if (crossbowDamageLevel < 25) 
                extraInfo = "<color=#FFCC00>3rd Tower at Lvl 25!</color>";
            else 
                extraInfo = "<color=#008800>Max Towers Reached!</color>";

            crossbowDamageText.text = 
                $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_sword\" tint=1></voffset></size></color> DMG: {curDmg} → <color=#008800>{nextDmg}</color>\n" +
                $"<size=80%>+{percentDmg}% damage</size>\n" +
                $"<size=70%>{extraInfo}</size>";
        }

        if (crossbowReloadText != null)
        {
            float curRel = GetCrossbowReloadAtLevel(crossbowReloadLevel);
            float nextRel = GetCrossbowReloadAtLevel(crossbowReloadLevel + 1);
            
            int percentRel = Mathf.RoundToInt(((curRel - nextRel) / curRel) * 100f);

            if (curRel <= 1.201f)
            {
                crossbowReloadText.text = 
                    $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_time\" tint=1></voffset></size></color> Reload: {curRel:F2}s (MAX)\n" +
                    $"<size=80%>Max Speed Reached</size>";
            }
            else
            {
                crossbowReloadText.text = 
                    $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_time\" tint=1></voffset></size></color> Reload: {curRel:F2}s → <color=#008800>{nextRel:F2}s</color>\n" +
                    $"<size=80%>-{percentRel}% reload time</size>";
            }
        }
        if (towerUpgradePanelRect != null)
        {
            towerUpgradePanelRect.gameObject.SetActive(isCrossbowBuilt);
        }

        UpdateCostUIGroup(crossbowDamageCostUI, ResourceType.Gold, crossbowDamageCostGold);
        UpdateCostUIGroup(crossbowReloadCostUI, ResourceType.Gold, crossbowReloadCostGold);

        // === ФІКС: Ховаємо цілі рядки з кнопками апгрейду, якщо балісту ще не побудовано ===
        if (upgradeCrossbowDamageButton != null && upgradeCrossbowDamageButton.transform.parent != null)
            upgradeCrossbowDamageButton.transform.parent.gameObject.SetActive(isCrossbowBuilt);
            
        if (upgradeCrossbowReloadButton != null && upgradeCrossbowReloadButton.transform.parent != null)
            upgradeCrossbowReloadButton.transform.parent.gameObject.SetActive(isCrossbowBuilt);

        UpdateButtonState(upgradeCrossbowDamageButton, isCrossbowBuilt && gold >= crossbowDamageCostGold);
        UpdateButtonState(upgradeCrossbowReloadButton, isCrossbowBuilt && (gold >= crossbowReloadCostGold) && (GetCrossbowReloadAtLevel(crossbowReloadLevel) > 1.201f)); 

        if (castleUpgradeCostUI != null && castle != null)
        {
            UpdateCostUIGroup(castleUpgradeCostUI, ResourceType.Gold, castle.GetUpgradeCost());
        }

        UpdateConstructionInfoUI();

        UpdateUnitEvoUI(knightEvoUI, knightLevel, knightSkins, "Knights", GetKnightDamageAtLevel(knightLevel), GetKnightDamageAtLevel(knightLevel + 1), 
            "<color=#008800>Counters Archers</color>\n<color=#FF6666>Vulnerable to Spearmen</color>", knightUpgradeCost, gold >= knightUpgradeCost, true);

        UpdateUnitEvoUI(archerEvoUI, archerLevel, archerSkins, "Archers", GetArcherDamageAtLevel(archerLevel), GetArcherDamageAtLevel(archerLevel + 1), 
            "<color=#008800>Counters Spearmen</color>\n<color=#FF6666>Vulnerable to Knights & Cavalry</color>", archerUpgradeCost, gold >= archerUpgradeCost, true);

        UpdateUnitEvoUI(spearmanEvoUI, spearmanLevel, spearmanSkins, "Spearmen", GetSpearmanDamageAtLevel(spearmanLevel), GetSpearmanDamageAtLevel(spearmanLevel + 1), 
            "<color=#008800>Counters Cavalry (x2 DMG)</color>\n<color=#FF6666>Vulnerable to Archers</color>", spearmanUpgradeCost, gold >= spearmanUpgradeCost, isSpearmanUnlocked);

        UpdateUnitEvoUI(cavalryEvoUI, cavalryLevel, cavalrySkins, "Cavalry", GetCavalryDamageAtLevel(cavalryLevel), GetCavalryDamageAtLevel(cavalryLevel + 1), 
            "<color=#008800>Fast & high HP</color>\n<color=#FF6666>Vulnerable to Spearmen</color>", cavalryUpgradeCost, gold >= cavalryUpgradeCost, isCavalryUnlocked);

        if (wallArcherLevelText != null)
        {
            int currentDmg = GetWallArcherDamageAtLevel(wallArcherLevel);
            int nextDmg    = GetWallArcherDamageAtLevel(wallArcherLevel + 1);
            int levelsUntilSkin = 5 - ((wallArcherLevel - 1) % 5);

            wallArcherLevelText.text =
                $"Wall Archers Lvl {wallArcherLevel}\n" +
                $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_sword\" tint=1></voffset></size></color> DMG: {currentDmg} → <color=#008800>{nextDmg}</color>\n" +
                $"<size=70%><color=#FFCC00>New Skin in {levelsUntilSkin} level(s)!</color></size>";
        }

        UpdateCostUIGroup(wallArcherUpgradeCostUI, ResourceType.Gold, wallArcherUpgradeCost);

        if (requestCartButton != null)
        {
            bool isTutorialTargetNow = IsTutorialTarget(requestCartButton);
            bool tutorialAllows = true;
            
            if (TutorialManager.Instance != null && TutorialManager.Instance.currentStepIndex < 3) 
            {
                tutorialAllows = false;
            }

            bool canUseCart = isTutorialTargetNow || (tutorialAllows && !isWaveInProgress);
            UpdateButtonState(requestCartButton, canUseCart);
            
            CanvasGroup cartCg = requestCartButton.GetComponent<CanvasGroup>();
            if (cartCg != null)
            {
                cartCg.blocksRaycasts = isTutorialTargetNow || (tutorialAllows && !isWaveInProgress);
            }

            Animator cartAnim = requestCartButton.GetComponent<Animator>();
            if (cartAnim != null) cartAnim.enabled = isTutorialTargetNow || tutorialAllows;

            UIPulseEffect cartPulse = requestCartButton.GetComponent<UIPulseEffect>();
            if (cartPulse != null) cartPulse.enabled = isTutorialTargetNow || tutorialAllows;
        }

        if (volleyBarrageButton != null)
        {
            volleyBarrageButton.gameObject.SetActive(true);
            
            foreach(Image childImg in volleyBarrageButton.GetComponentsInChildren<Image>(true))
            {
                if (!childImg.enabled) childImg.enabled = true;
                
                Color c = childImg.color;
                if (c.a < 0.05f)
                {
                    c.a = 1f;
                    childImg.color = c;
                }
            }
        }

        bool canHireKnight = (gold >= GetCurrentKnightCost()) && (currentUnits < maxUnits);
        bool canHireArcher = (gold >= GetCurrentArcherCost()) && (currentUnits < maxUnits);
        bool canHireSpearman = (gold >= GetCurrentSpearmanCost()) && (currentUnits < maxUnits);
        bool canHireCavalry = (gold >= GetCurrentCavalryCost()) && (currentUnits < maxUnits); 

        UpdateButtonState(hireKnightButton, canHireKnight);
        UpdateButtonState(hireArcherButton, canHireArcher);

        if (hireSpearmanButton != null)
        {
            bool canSeeSpearman = isSpearmanUnlocked && barracksLevel > 0;
            if (!isWaitingForNextWave) 
            {
                hireSpearmanButton.gameObject.SetActive(canSeeSpearman);
            }
            UpdateButtonState(hireSpearmanButton, canHireSpearman && canSeeSpearman);
        }

        if (hireCavalryButton != null)
        {
            bool canSeeCavalry = isCavalryUnlocked && barracksLevel >= 3;
            if (!isWaitingForNextWave) 
            {
                hireCavalryButton.gameObject.SetActive(canSeeCavalry);
            }
            UpdateButtonState(hireCavalryButton, canHireCavalry && canSeeCavalry);
        }

        UpdateButtonState(towerButton, (wood >= towerWoodCost && stone >= towerStoneCost));

        if (castleUpgradeButton != null && castle != null)
        {
            UpdateButtonState(castleUpgradeButton, gold >= castle.GetUpgradeCost());
        }

        if (buildCrossbowButton != null)
        {
            bool canBuildCrossbow = (wood >= crossbowBuildCostWood) && (stone >= crossbowBuildCostStone);
            UpdateButtonState(buildCrossbowButton, !isCrossbowBuilt && canBuildCrossbow);
            
            if (isCrossbowBuilt) UpdateCostUIGroup(buildCrossbowCostUI, ResourceType.Wood, 0, ResourceType.Stone, 0, "BUILT");
            else UpdateCostUIGroup(buildCrossbowCostUI, ResourceType.Wood, crossbowBuildCostWood, ResourceType.Stone, crossbowBuildCostStone);
        }

        UpdateButtonState(hammerButton, true);
        UpdateButtonState(upgradeWallArcherButton, gold >= wallArcherUpgradeCost);

        if (unlockSpearmanButton != null)
        {
            if (unlockSpearmanRowGroup != null) unlockSpearmanRowGroup.gameObject.SetActive(isBarracksBuilt);
            else unlockSpearmanButton.gameObject.SetActive(isBarracksBuilt);

            if (isSpearmanUnlocked)
            {
                UpdateButtonState(unlockSpearmanButton, false);
            }
            else
            {
                UpdateButtonState(unlockSpearmanButton, gold >= spearmanUnlockCost);
            }
        }
        
        if (unlockCavalryButton != null)
        {
            if (unlockCavalryRowGroup != null) unlockCavalryRowGroup.gameObject.SetActive(isBarracksBuilt);
            else unlockCavalryButton.gameObject.SetActive(isBarracksBuilt);

            if (isCavalryUnlocked || barracksLevel < 3)
            {
                UpdateButtonState(unlockCavalryButton, false);
            }
            else
            {
                UpdateButtonState(unlockCavalryButton, gold >= cavalryUnlockCost);
            }
        }

        if (buildBarracksBtnInMenu != null)
        {
            int costG = GetBarracksBuildingUpgradeCost(true);
            int costW = GetBarracksBuildingUpgradeCost(false);
            UpdateButtonState(buildBarracksBtnInMenu, gold >= costG && wood >= costW);

            UpdateCostUIGroup(barracksCostUI, ResourceType.Gold, costG, ResourceType.Wood, costW);

            Image barracksImg = buildBarracksBtnInMenu.GetComponent<Image>();
            if (barracksImg != null)
            {
                if (barracksLevel == 0) barracksImg.sprite = buildButtonSprite;
                else barracksImg.sprite = upgradeButtonSprite;
            }
        }

        if (barracksIconButton != null)
        {
            if (barracksLevel == 0)
            {
                barracksIconButton.gameObject.SetActive(false);
            }
            else if (!isWaveInProgress) 
            {
                barracksIconButton.gameObject.SetActive(true);
            }
        }

        if (upgradeLimitButton != null)
        {
            int cap = GetBarracksCapLimit();
            bool canBuySlot = (gold >= GetSlotUpgradeCost()) && (maxUnits < cap);
            UpdateButtonState(upgradeLimitButton, canBuySlot);
        }

        if (buildMineButton != null)
        {
            int mW = GetMineUpgradeCost(true);
            int mStone = GetMineUpgradeCost(false);

            bool canAffordMine = (wood >= mW) && (stone >= mStone);
            UpdateButtonState(buildMineButton, canAffordMine);

            Image mineImg = buildMineButton.GetComponent<Image>();
            if (mineImg != null)
            {
                if (mineLevel == 0) mineImg.sprite = buildButtonSprite;
                else mineImg.sprite = upgradeButtonSprite;
            }

            UpdateCostUIGroup(mineCostUI, ResourceType.Wood, mW, ResourceType.Stone, mStone);
        }

        if (openShopButton != null) UpdateButtonState(openShopButton, true);

        if (nextWaveButton != null) UpdateButtonState(nextWaveButton, enemiesAlive <= 0 && !isWaveInProgress);

        if (buildSpikesButton != null)
        {
            bool canBuild = (wood >= spikesWoodCost) && (currentSpikes == null);
            UpdateButtonState(buildSpikesButton, canBuild);

            if (currentSpikes != null) UpdateCostUIGroup(spikesCostUI, ResourceType.Wood, 0, ResourceType.Stone, 0, "BUILT");
            else UpdateCostUIGroup(spikesCostUI, ResourceType.Wood, spikesWoodCost);
        }

        if (estimatedIncomeText != null && spawner != null)
        {
            int enemiesGold = spawner.GetEstimatedGoldFromEnemies();
            int waveBonus = GetGoldReward();
            int totalEst = enemiesGold + waveBonus;
            estimatedIncomeText.text = $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_coin\" tint=1></voffset></size></color> +{totalEst} G";
        }

        UpdateUnitLockUI();

        if (shopPulse != null)
        {
            bool canUpgradeInShop = (gold >= knightUpgradeCost) || 
                                    (gold >= archerUpgradeCost) || 
                                    ((gold >= spearmanUpgradeCost) && isSpearmanUnlocked) || 
                                    ((gold >= cavalryUpgradeCost) && isCavalryUnlocked) || 
                                    (gold >= wallArcherUpgradeCost);

            bool isShopClosed = (shopPanel == null || !shopPanel.activeSelf) && (shopPanelNew == null || !shopPanelNew.activeSelf);
            shopPulse.SetPulse(canUpgradeInShop && isShopClosed);
        }

        if (barracksPulse != null && barracksLevel > 0)
        {
            int cap = GetBarracksCapLimit();
            bool canBuySlot = (gold >= GetSlotUpgradeCost()) && (maxUnits < cap);
            bool canUnlockSpear = (gold >= spearmanUnlockCost) && !isSpearmanUnlocked;
            bool canUnlockCav = (gold >= cavalryUnlockCost) && !isCavalryUnlocked && barracksLevel >= 3;
            
            bool canUpgradeInBarracks = canBuySlot || canUnlockSpear || canUnlockCav;
            
            bool isBarracksClosed = (barracksUpgradePanel == null || !barracksUpgradePanel.activeSelf) && (barracksPanelNew == null || !barracksPanelNew.activeSelf);
            barracksPulse.SetPulse(canUpgradeInBarracks && isBarracksClosed);
        }

        if (buildPulse != null)
        {
            int cG = GetBarracksBuildingUpgradeCost(true);
            int cW = GetBarracksBuildingUpgradeCost(false);
            bool canBuildBarracks = (gold >= cG && wood >= cW);
            
            int mW = GetMineUpgradeCost(true);
            int mStone = GetMineUpgradeCost(false); 
            bool canBuildMine = (wood >= mW && stone >= mStone);

            bool canBuildSpikes = (wood >= spikesWoodCost) && (currentSpikes == null);
            bool canUpgradeCastle = castle != null && (gold >= castle.GetUpgradeCost());
            
            bool canBuildAnything = canBuildBarracks || canBuildMine || canBuildSpikes || canUpgradeCastle;

            bool isBuildClosed = (constructionPanel == null || !constructionPanel.activeSelf) && (constructionPanelNew == null || !constructionPanelNew.activeSelf);
            buildPulse.SetPulse(canBuildAnything && isBuildClosed);
        }
    }

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

    void UpdateButtonState(Button btn, bool isActive)
    {
        if (btn == null) return;

        bool isCurrentTutorialTarget = IsTutorialTarget(btn);
        bool isFutureTutorialTarget = false;

        if (TutorialManager.Instance != null && TutorialManager.Instance.currentStepIndex < TutorialManager.Instance.steps.Count)
        {
            RectTransform btnRect = btn.GetComponent<RectTransform>();
            for (int i = TutorialManager.Instance.currentStepIndex + 1; i < TutorialManager.Instance.steps.Count; i++)
            {
                RectTransform futureTarget = TutorialManager.Instance.steps[i].targetUI;
                if (futureTarget != null && (btn.transform == futureTarget || btn.transform.IsChildOf(futureTarget) || futureTarget.IsChildOf(btn.transform)))
                {
                    // === ФІКС: Додаємо кнопки прокачки до списку "вільних", щоб гравець міг качатись сам ===
                    if (btn != hireArcherButton && btn != hireKnightButton && 
                        btn != openShopButton && btn != openMapButton &&
                        btn != knightEvoUI.upgradeBtn && btn != archerEvoUI.upgradeBtn && 
                        btn != spearmanEvoUI.upgradeBtn && btn != cavalryEvoUI.upgradeBtn)
                    {
                        isFutureTutorialTarget = true;
                    }
                    break;
                }
            }
        }

        if (isCurrentTutorialTarget)
        {
            isActive = true; 
            
            if (TutorialManager.Instance != null && TutorialManager.Instance.currentStepIndex < TutorialManager.Instance.steps.Count)
            {
                RectTransform targetUI = TutorialManager.Instance.steps[TutorialManager.Instance.currentStepIndex].targetUI;
                if (targetUI != null)
                {
                    Transform curr = btn.transform;
                    while (curr != null)
                    {
                        CanvasGroup parentCg = curr.GetComponent<CanvasGroup>();
                        if (parentCg != null)
                        {
                            parentCg.interactable = true;
                            parentCg.blocksRaycasts = true;
                            parentCg.alpha = 1f;
                        }
                        
                        if (curr == targetUI) break; 
                        curr = curr.parent;
                    }
                }
            }
        }
        else if (isFutureTutorialTarget)
        {
            isActive = false; 
        }

        btn.interactable = isActive;
        btn.transition = Selectable.Transition.ColorTint;

        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = isActive ? 1f : 0.5f;
        cg.blocksRaycasts = isActive;
        
        cg.interactable = isActive;

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

    // --- 1. ФІКС: Оновлення ресурсів та Слави ---
    // --- ФІКС: Нарахування Слави ---
    // Анімація вилітання Слави
    private IEnumerator AnimateGloryPopup(int amount)
    {
        if (gloryPopupText == null) yield break;

        gloryPopupText.gameObject.SetActive(true);
        gloryPopupText.text = $"+{amount}";
        
        CanvasGroup cg = gloryPopupText.GetComponent<CanvasGroup>();
        if (cg == null) cg = gloryPopupText.gameObject.AddComponent<CanvasGroup>();
        
        RectTransform rt = gloryPopupText.rectTransform;
        rt.anchoredPosition = defaultGloryPopupPos; 
        
        float duration = 1.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            rt.anchoredPosition = defaultGloryPopupPos + new Vector2(0, progress * 40f);
            cg.alpha = 1f - progress;
            
            yield return null;
        }
        
        gloryPopupText.gameObject.SetActive(false);
        rt.anchoredPosition = defaultGloryPopupPos; 
    }

    public void AddGlory(int amount)
    {
        if (amount <= 0) return;
        glory += amount;
        PlayerPrefs.SetInt("PlayerGlory", glory);
        UpdateResourcesUI();

        // Запускаємо анімацію "+X"
        if (gloryPopupText != null)
        {
            StopCoroutine("AnimateGloryPopup"); 
            StartCoroutine("AnimateGloryPopup", amount);
        }
    }

    // --- ФІКС: Оновлення ресурсів (замінено playerGoldText на goldText) ---
    public void UpdateResourcesUI()
    {
        if (goldText) goldText.text = gold.ToString(); 
        if (gloryText) gloryText.text = glory.ToString();
        if (woodText) woodText.text = wood.ToString();
        if (stoneText) stoneText.text = stone.ToString();
    }

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
    public void UpdateMetaUI()
    {
        if (gemsText != null) gemsText.text = gems.ToString();
        if (topGemsText != null) topGemsText.text = gems.ToString();
        
        if (gemProgressBar != null)
        {
            gemProgressBar.minValue = 0f; 
            gemProgressBar.maxValue = killsToNextGem;
            gemProgressBar.value = currentKills;
        }
        if (gemProgressText != null) gemProgressText.text = $"{currentKills} / {killsToNextGem}";

        UpdateMetaSlot(uiFortifiedWalls, metaFortifiedWalls, 10, 5, "Lv " + metaFortifiedWalls, 
            $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_heart\" tint=1></voffset></size></color> Max HP: +{metaFortifiedWalls * 200} → <color=#008800>+{(metaFortifiedWalls + 1) * 200}</color>", true);
            
        UpdateMetaSlot(uiPrecisionBows, metaPrecisionBows, 15, 10, "Lv " + metaPrecisionBows, 
            $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_sword\" tint=1></voffset></size></color> Arrow DMG: +{metaPrecisionBows * 15}% → <color=#008800>+{(metaPrecisionBows + 1) * 15}%</color>", true);
            
        UpdateMetaSlot(uiVolleyBarrage, metaVolleyBarrage, 25, 15, "Lv " + metaVolleyBarrage, 
            $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_arrows\" tint=1></voffset></size></color> Extra Arrows: +{metaVolleyBarrage} → <color=#008800>+{metaVolleyBarrage + 1}</color>", true);
            
        UpdateMetaSlot(uiTrophyBounty, metaTrophyBounty, 10, 5, "Lv " + metaTrophyBounty, 
            $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_coin\" tint=1></voffset></size></color> Gold Drop: +{metaTrophyBounty * 5}% → <color=#008800>+{(metaTrophyBounty + 1) * 5}%</color>", true);
            
        bool isMineUnlocked = (mineLevel > 0 || isMineBuilt);
        UpdateMetaSlot(uiEfficientCarts, metaEfficientCarts, 15, 10, "Lv " + metaEfficientCarts, 
            $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_cart\" tint=1></voffset></size></color> Cart Ore: +{metaEfficientCarts * 10}% → <color=#008800>+{(metaEfficientCarts + 1) * 10}%</color>", isMineUnlocked);
            
        UpdateMetaSlot(uiMendingMasonry, metaMendingMasonry, 20, 15, "Lv " + metaMendingMasonry, 
            $"<color=#111111><size=150%><voffset=1.0em><sprite name=\"icon_heart\" tint=1></voffset></size></color> HP Regen: {metaMendingMasonry}%/s → <color=#008800>{metaMendingMasonry + 1}%/s</color>", true);
    }
}