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
    public string entryName;             // Напр. "KNIGHT"
    public string subTitle;              // Напр. "Sturdy Melee Defender"
    public CodexCategory category;
    
    [Header("Зображення")]
    public Sprite listIcon;              // Маленька іконка для кнопки в списку
    public Sprite portrait;              // Великий квадратний портрет
    
    [Header("Опис")]
    [TextArea(3, 6)]
    public string description;           // Текст історії/лоре

    [Header("Тактика (Плюси і Мінуси)")]
    public string[] pros;                // Список плюсів (зелені)
    public string[] cons;                // Список мінусів (червоні)

    [Header("Статистика (Авто або Вручну)")]
    [Tooltip("Впиши 'Knight', 'Archer', 'Spearman' або 'Cavalry' щоб брати стати гравця. Інакше залиш пустим.")]
    public string unitId;                
    public int manualHp;                 // ХП для ворогів або будівель
    public int manualDmg;                // Урон для ворогів або будівель
}

public class CodexManager : MonoBehaviour
{
    public static CodexManager Instance;

    [Header("Головні панелі")]
    public GameObject codexPanel;
    public Transform codexContentPanel; 

    [Header("Кнопки Вкладок")]
    public Button unitsTabBtn;
    public Button buildingsTabBtn;
    public Button enemiesTabBtn;

    [Header("Ліва Колонка (Список)")]
    public Transform entryListContainer; 
    public GameObject entryButtonPrefab; 

    [Header("Права Колонка (Інформація)")]
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public Image portraitImage;
    
    [Header("Стати")]
    public TMP_Text hpText;
    public TMP_Text dmgText;
    
    [Header("Опис і Тактика")]
    public TMP_Text descriptionText;
    public TMP_Text prosText;  // Текстовий блок для плюсів (Лівий стовпчик)
    public TMP_Text consText;  // Текстовий блок для мінусів (Правий стовпчик)

    [Header("База Даних")]
    public List<CodexEntry> database = new List<CodexEntry>();

    private List<GameObject> spawnedButtons = new List<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (codexPanel != null) codexPanel.SetActive(false);

        if (unitsTabBtn) unitsTabBtn.onClick.AddListener(() => OpenCategory(CodexCategory.Units));
        if (buildingsTabBtn) buildingsTabBtn.onClick.AddListener(() => OpenCategory(CodexCategory.Buildings));
        if (enemiesTabBtn) enemiesTabBtn.onClick.AddListener(() => OpenCategory(CodexCategory.Enemies));
    }

    public void OpenCodex()
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        
        if (codexPanel)
        {
            codexPanel.SetActive(true);
            OpenCategory(CodexCategory.Units); 

            Transform targetPanel = codexContentPanel != null ? codexContentPanel : codexPanel.transform;
            targetPanel.localScale = new Vector3(0.8f, 0.8f, 1f);
            StartCoroutine(ScaleAnim(targetPanel, Vector3.one));
            
            Time.timeScale = 0f; 
        }
    }

    public void CloseCodex()
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        if (codexPanel) codexPanel.SetActive(false);
        Time.timeScale = 1f; 
    }

    IEnumerator ScaleAnim(Transform t, Vector3 target)
    {
        float time = 0;
        Vector3 start = t.localScale;
        while(time < 0.2f)
        {
            time += Time.unscaledDeltaTime;
            t.localScale = Vector3.Lerp(start, target, time / 0.2f);
            yield return null;
        }
        t.localScale = target;
    }

    public void OpenCategory(CodexCategory category)
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);

        // Очищення старого списку
        foreach (var btn in spawnedButtons) Destroy(btn);
        spawnedButtons.Clear();

        bool isFirst = true;
        foreach (var entry in database)
        {
            if (entry.category == category)
            {
                GameObject newBtnObj = Instantiate(entryButtonPrefab, entryListContainer);
                spawnedButtons.Add(newBtnObj);

                // Налаштування кнопки списку (текст + іконка)
                TMP_Text btnText = newBtnObj.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = entry.entryName;

                // Якщо в префабі кнопки є додатковий Image для іконки (шукаємо його по імені "Icon")
                Transform iconTr = newBtnObj.transform.Find("Icon");
                if (iconTr != null && entry.listIcon != null)
                {
                    Image btnIcon = iconTr.GetComponent<Image>();
                    if (btnIcon != null) btnIcon.sprite = entry.listIcon;
                }

                Button btn = newBtnObj.GetComponent<Button>();
                if (btn != null)
                {
                    CodexEntry currentEntry = entry; 
                    btn.onClick.AddListener(() => ShowEntryDetails(currentEntry));
                }

                if (isFirst)
                {
                    ShowEntryDetails(entry);
                    isFirst = false;
                }
            }
        }
    }

    void ShowEntryDetails(CodexEntry entry)
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);

        if (titleText) titleText.text = entry.entryName.ToUpper();
        if (subtitleText) subtitleText.text = entry.subTitle;
        if (portraitImage) portraitImage.sprite = entry.portrait;
        if (descriptionText) descriptionText.text = entry.description;

        // Генерація Плюсів (Зеленим зі стрілочкою вгору)
        if (prosText)
        {
            string prosStr = "";
            foreach (string p in entry.pros)
            {
                prosStr += $"<color=#4CAF50>↑</color> {p}\n";
            }
            prosText.text = prosStr;
        }

        // Генерація Мінусів (Червоним зі стрілочкою вниз)
        if (consText)
        {
            string consStr = "";
            foreach (string c in entry.cons)
            {
                consStr += $"<color=#F44336>↓</color> {c}\n";
            }
            consText.text = consStr;
        }

        // Динамічна або ручна статистика
        int hp = entry.manualHp;
        int dmg = entry.manualDmg;

        if (GameManager.Instance != null && !string.IsNullOrEmpty(entry.unitId))
        {
            if (entry.unitId == "Knight") { hp = 120 + (GameManager.Instance.knightLevel * 20); dmg = GameManager.Instance.GetKnightDamage(); }
            else if (entry.unitId == "Archer") { hp = 60 + (GameManager.Instance.archerLevel * 10); dmg = GameManager.Instance.GetArcherDamage(); }
            else if (entry.unitId == "Spearman") { hp = 90 + (GameManager.Instance.spearmanLevel * 15); dmg = GameManager.Instance.GetSpearmanDamage(); }
            else if (entry.unitId == "Cavalry") { hp = 150 + (GameManager.Instance.cavalryLevel * 25); dmg = GameManager.Instance.GetCavalryDamage(); }
        }

        if (hpText) hpText.text = $"HP: {hp}";
        if (dmgText) dmgText.text = $"DMG: {dmg}";
    }
}