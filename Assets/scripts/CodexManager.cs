using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public enum CodexCategory { Units, Buildings, Enemies }

[System.Serializable]
public class CodexEntry
{
    [Header("Головна Інформація")]
    public string entryName;             
    public string subTitle;              
    public CodexCategory category;
    
    [Header("Зображення")]
    public Sprite listIcon;              
    public Sprite portrait;              
    
    [Header("Опис")]
    [TextArea(3, 6)]
    public string description;           

    [Header("Тактика")]
    public string[] pros;                
    public string[] cons;                

    [Header("Система Зв'язку")]
    [Tooltip("Для військ: Knight, Archer, Spearman, Cavalry. Для будівель: Wall, Tower, Crossbow. Для ворогів: Точне Name з EnemySpawner.")]
    public string unitId;                
    [Tooltip("ПОЧАТКОВЕ ХП (тільки для твоїх військ)")]
    public int manualHp;                 
    [Tooltip("ПОЧАТКОВИЙ УРОН (тільки для твоїх військ)")]
    public int manualDmg;                
}

public class CodexManager : MonoBehaviour
{
    public static CodexManager Instance;

    [Header("Головні панелі")]
    public GameObject codexPanel;
    public Transform codexContentPanel; 

    [Header("Вкладки (Кнопки)")]
    public Button unitsTabBtn;
    public Button buildingsTabBtn;
    public Button enemiesTabBtn;

    [Header("Вкладки (Зображення)")]
    public Image unitsTabImage;
    public Image buildingsTabImage;
    public Image enemiesTabImage;

    [Header("Спрайти Вкладок")]
    public Sprite activeTabSprite;       
    public Sprite inactiveTabSprite;     

    [Header("Ліва Колонка (Список)")]
    public Transform entryListContainer; 
    public GameObject entryButtonPrefab; 

    [Header("Права Колонка (Інформація)")]
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public Image portraitImage;
    public TMP_Text descriptionText;
    public TMP_Text prosText;  
    public TMP_Text consText;  
    
    [Header("Об'єкти Статистики (ВАЖЛИВО)")]
    public GameObject statsContainer;    
    public TMP_Text hpText;
    public GameObject hpIcon;    
    public GameObject hpShadow;  
    public TMP_Text dmgText;
    public GameObject dmgIcon;   
    public GameObject dmgShadow; 

    [Header("База Даних")]
    public List<CodexEntry> database = new List<CodexEntry>();

    private List<GameObject> spawnedButtons = new List<GameObject>();
    private GameObject currentSelectedButton; 

    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    void Start()
    {
        if (codexPanel != null) codexPanel.SetActive(false);
        if (unitsTabBtn) unitsTabBtn.onClick.AddListener(() => OpenCategory(CodexCategory.Units));
        if (buildingsTabBtn) buildingsTabBtn.onClick.AddListener(() => OpenCategory(CodexCategory.Buildings));
        if (enemiesTabBtn) enemiesTabBtn.onClick.AddListener(() => OpenCategory(CodexCategory.Enemies));
    }

    // === ФІКС: Єдине правильне налаштування при відкритті вікна ===
    void OnEnable()
    {
        // Ми ПОВНІСТЮ прибрали зміну localScale. 
        // Тепер панель завжди буде такого розміру, як ти налаштував у сцені!
        
        OpenCategory(CodexCategory.Units);    // Автоматично відкриваємо Юнітів
    }

    public void OpenCategory(CodexCategory category)
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        if (unitsTabImage) unitsTabImage.sprite = (category == CodexCategory.Units) ? activeTabSprite : inactiveTabSprite;
        if (buildingsTabImage) buildingsTabImage.sprite = (category == CodexCategory.Buildings) ? activeTabSprite : inactiveTabSprite;
        if (enemiesTabImage) enemiesTabImage.sprite = (category == CodexCategory.Enemies) ? activeTabSprite : inactiveTabSprite;

        foreach (var btn in spawnedButtons) Destroy(btn);
        spawnedButtons.Clear();
        currentSelectedButton = null;

        foreach (var entry in database)
        {
            if (entry.category == category)
            {
                GameObject newBtnObj = Instantiate(entryButtonPrefab, entryListContainer);
                spawnedButtons.Add(newBtnObj);
                newBtnObj.GetComponent<Image>().color = new Color(0.75f, 0.75f, 0.75f, 1f); 
                TMP_Text btnText = newBtnObj.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = entry.entryName;
                Transform iconTr = newBtnObj.transform.Find("Icon");
                if (iconTr != null && entry.listIcon != null) { Image btnIcon = iconTr.GetComponent<Image>(); if (btnIcon != null) btnIcon.sprite = entry.listIcon; }

                Button btn = newBtnObj.GetComponent<Button>();
                if (btn != null)
                {
                    CodexEntry currentEntry = entry; GameObject thisBtnObj = newBtnObj; 
                    btn.onClick.AddListener(() => { ShowEntryDetails(currentEntry); HighlightListButton(thisBtnObj); });
                }
            }
        }
        
        if (spawnedButtons.Count > 0)
        {
            CodexEntry firstEntry = database.Find(e => e.category == category);
            if (firstEntry != null)
            {
                ShowEntryDetails(firstEntry);
                HighlightListButton(spawnedButtons[0]);
            }
        }
    }

    void HighlightListButton(GameObject btnObj)
    {
        if (currentSelectedButton != null) { Image oldBg = currentSelectedButton.GetComponent<Image>(); if (oldBg != null) oldBg.color = new Color(0.75f, 0.75f, 0.75f, 1f); currentSelectedButton.transform.localScale = Vector3.one; }
        currentSelectedButton = btnObj;
        Image newBg = currentSelectedButton.GetComponent<Image>();
        if (newBg != null) newBg.color = Color.white; 
        currentSelectedButton.transform.localScale = new Vector3(1.05f, 1.05f, 1f); 
    }

    void ShowEntryDetails(CodexEntry entry)
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        if (titleText) titleText.text = entry.entryName.ToUpper();
        if (subtitleText) subtitleText.text = entry.subTitle;
        if (portraitImage) portraitImage.sprite = entry.portrait;
        if (descriptionText) descriptionText.text = entry.description;

        if (prosText) { string prosStr = ""; foreach (string p in entry.pros) prosStr += $"<voffset=0.2em><sprite name=\"Arrow_Green\"></voffset> {p}\n"; prosText.text = prosStr; }
        if (consText) { string consStr = ""; foreach (string c in entry.cons) consStr += $"<voffset=0.2em><sprite name=\"Arrow_Red\"></voffset> {c}\n"; consText.text = consStr; }

        int hp = 0;
        int dmg = 0;

        if (GameManager.Instance != null && !string.IsNullOrEmpty(entry.unitId))
        {
            if (entry.category == CodexCategory.Units)
            {
                int level = 1;
                if (entry.unitId == "Knight") level = GameManager.Instance.knightLevel;
                else if (entry.unitId == "Archer") level = GameManager.Instance.archerLevel;
                else if (entry.unitId == "Spearman") level = GameManager.Instance.spearmanLevel;
                else if (entry.unitId == "Cavalry") level = GameManager.Instance.cavalryLevel;

                hp = EconomyConfig.GetUnitMaxHealth(entry.manualHp, level);
                dmg = EconomyConfig.GetUnitDamage(entry.manualDmg, level);
            }
            else if (entry.category == CodexCategory.Buildings)
            {
                if (entry.unitId == "Wall") {
                    hp = (GameManager.Instance.castle != null) ? GameManager.Instance.castle.maxHealth : GameManager.Instance.wallBaseHp;
                    dmg = 0;
                }
                else if (entry.unitId == "Tower") {
                    hp = GameManager.Instance.towerBaseHp;
                    dmg = GameManager.Instance.GetTowerDamage();
                }
                else if (entry.unitId == "Crossbow") {
                    hp = 0; 
                    dmg = GameManager.Instance.GetCrossbowDamage();
                }
            }
        }
        
        if (entry.category == CodexCategory.Enemies && EnemySpawner.Instance != null && !string.IsNullOrEmpty(entry.unitId))
        {
            EnemyConfig enemyData = EnemySpawner.Instance.allEnemies.Find(e => e.name.Trim().Equals(entry.unitId.Trim(), System.StringComparison.OrdinalIgnoreCase));
            if (enemyData != null)
            {
                int wave = EnemySpawner.Instance.GetCurrentWave();
                hp = EconomyConfig.GetEnemyHealth(enemyData.baseHp, wave);
                if (wave <= 1) hp = Mathf.Max(10, hp / 5); 
                dmg = EconomyConfig.GetEnemyDamage(enemyData.baseDamage, wave);
            }
        }

        bool hasHp = hp > 0;
        bool hasDmg = dmg > 0;

        if (statsContainer) statsContainer.SetActive(hasHp || hasDmg);
        
        if (hpText) { hpText.gameObject.SetActive(hasHp); hpText.text = $"HP: {hp}"; }
        if (hpIcon) hpIcon.SetActive(hasHp);
        if (hpShadow) hpShadow.SetActive(hasHp); 
        
        if (dmgText) { dmgText.gameObject.SetActive(hasDmg); dmgText.text = $"DMG: {dmg}"; }
        if (dmgIcon) dmgIcon.SetActive(hasDmg);
        if (dmgShadow) dmgShadow.SetActive(hasDmg); 
    }
}