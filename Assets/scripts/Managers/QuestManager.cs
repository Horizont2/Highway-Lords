using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public enum QuestType { KillEnemies, EarnGold, SpendGold, HireUnits, UseSkills, ReachWave, UpgradeCastle }
public enum RewardType { Gold, Gems }

[System.Serializable]
public class QuestInfo
{
    public string questID;
    public string questName;
    public string questDescription;
    public QuestType type;
    public int targetAmount;
    public RewardType rewardType;
    public int baseRewardAmount;
    public bool isDynamicReward; 
    public Sprite questIcon;
}

[System.Serializable]
public class ActiveQuest
{
    public string questID;
    public int currentProgress;
    public bool isClaimed;
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Керування вікном (Open/Close)")]
    public Button openPanelButton;       
    public Button closePanelButton;      
    public UIPulseEffect openButtonPulse; 

    [Header("Скрол")]
    public ScrollRect mainScrollRect; // Перетягнеш сюди Bottom_ScrollArea

    [Header("Головна Панель")]
    public GameObject questsPanel;
    public TMP_Text timerText;

    [Header("Вкладки (Tabs)")]
    public Button tabDailyButton;
    public Button tabAchievementsButton;
    public GameObject top7DaysTrackObj;   
    public GameObject dailyContainerObj;  
    public GameObject achievementsContainerObj; 

    [Header("7-Day Login Track")]
    public Image[] dayNodes;              
    public Sprite nodeFilledSprite;
    public Sprite nodeEmptySprite;
    public Button claimLoginButton;       
    public TMP_Text loginRewardText;
    
    [Header("Префаби та Контейнери")]
    public Transform dailyContainer;
    public Transform achievementsContainer;
    public GameObject questRowPrefab;

    [Header("Бази Даних")]
    public List<QuestInfo> allDailyQuests = new List<QuestInfo>();
    public List<QuestInfo> allAchievements = new List<QuestInfo>();
    public int dailyQuestsPerDay = 3;

    [Header("Іконки нагород")]
    public Sprite goldIcon;
    public Sprite gemIcon;

    private List<ActiveQuest> activeDailies = new List<ActiveQuest>();
    private List<ActiveQuest> activeAchievements = new List<ActiveQuest>();
    private List<GameObject> spawnedRows = new List<GameObject>();

    private int consecutiveDays = 1;
    private bool todayLoginClaimed = false;
    private bool isShowingDaily = true;
    
    // ДОДАНО: Для збереження розмірів панелі під час анімації
    private Dictionary<GameObject, Vector3> originalPanelScales = new Dictionary<GameObject, Vector3>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (tabDailyButton) tabDailyButton.onClick.AddListener(() => SwitchTab(true));
        if (tabAchievementsButton) tabAchievementsButton.onClick.AddListener(() => SwitchTab(false));
        if (claimLoginButton) claimLoginButton.onClick.AddListener(ClaimDailyLogin);
        if (openPanelButton) openPanelButton.onClick.AddListener(TogglePanel);
        if (closePanelButton) closePanelButton.onClick.AddListener(TogglePanel);

        CheckDailyReset();
        LoadAchievements();
        SwitchTab(true); 
    }

    void Update()
    {
        if (questsPanel != null && questsPanel.activeSelf && timerText != null && isShowingDaily)
        {
            UpdateTimerText();
        }
    }

    // ==========================================
    // ЛОГІКА ДНІВ ТА КАЛЕНДАРЯ
    // ==========================================
    private void CheckDailyReset()
    {
        string lastLoginStr = PlayerPrefs.GetString("LastLoginDate", "");
        DateTime currentDate = DateTime.Now.Date;

        consecutiveDays = PlayerPrefs.GetInt("ConsecutiveDays", 1);
        todayLoginClaimed = PlayerPrefs.GetInt("TodayLoginClaimed", 0) == 1;

        if (string.IsNullOrEmpty(lastLoginStr))
        {
            GenerateNewDailyQuests();
            SaveLoginData(currentDate, 1, false);
        }
        else
        {
            DateTime lastLogin = DateTime.Parse(lastLoginStr);
            TimeSpan difference = currentDate - lastLogin;

            if (difference.Days == 1)
            {
                consecutiveDays++;
                if (consecutiveDays > 7) consecutiveDays = 1; 
                
                GenerateNewDailyQuests();
                SaveLoginData(currentDate, consecutiveDays, false);
            }
            else if (difference.Days > 1)
            {
                consecutiveDays = 1;
                GenerateNewDailyQuests();
                SaveLoginData(currentDate, consecutiveDays, false);
            }
            else
            {
                LoadDailyQuests();
            }
        }
        Update7DayUI();
    }

    private void SaveLoginData(DateTime date, int days, bool claimed)
    {
        PlayerPrefs.SetString("LastLoginDate", date.ToString("yyyy-MM-dd"));
        PlayerPrefs.SetInt("ConsecutiveDays", days);
        PlayerPrefs.SetInt("TodayLoginClaimed", claimed ? 1 : 0);
        
        consecutiveDays = days;
        todayLoginClaimed = claimed;
        PlayerPrefs.Save();
    }

    private void UpdateTimerText()
    {
        DateTime now = DateTime.Now;
        DateTime tomorrow = now.Date.AddDays(1);
        TimeSpan timeLeft = tomorrow - now;
        timerText.text = $"Refreshes in: {timeLeft.Hours:D2}:{timeLeft.Minutes:D2}:{timeLeft.Seconds:D2}";
    }

    private void Update7DayUI()
    {
        if (dayNodes.Length < 7) return;

        for (int i = 0; i < 7; i++)
        {
            if (i < consecutiveDays - 1) 
            {
                if (nodeFilledSprite) dayNodes[i].sprite = nodeFilledSprite;
                dayNodes[i].color = Color.white;
            }
            else if (i == consecutiveDays - 1)
            {
                dayNodes[i].sprite = todayLoginClaimed ? nodeFilledSprite : nodeEmptySprite;
                dayNodes[i].color = todayLoginClaimed ? Color.white : new Color(1f, 0.9f, 0.5f, 1f); 
            }
            else
            {
                if (nodeEmptySprite) dayNodes[i].sprite = nodeEmptySprite;
                dayNodes[i].color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }

        if (claimLoginButton != null)
        {
            claimLoginButton.interactable = !todayLoginClaimed;
            claimLoginButton.GetComponentInChildren<TMP_Text>().text = todayLoginClaimed ? "CLAIMED" : "CLAIM";
            
            int reward = consecutiveDays == 7 ? 25 : 100 * consecutiveDays;
            string type = consecutiveDays == 7 ? "Gems" : "Gold";
            if(loginRewardText) loginRewardText.text = $"+{reward} {type}";
        }
    }

    private void ClaimDailyLogin()
    {
        if (todayLoginClaimed) return;

        if (consecutiveDays == 7) 
        {
            if (ChestManager.Instance != null) ChestManager.Instance.GiveChest();
        }
        else 
        {
            GameManager.Instance.AddResource(ResourceType.Gold, 100 * consecutiveDays);
        }

        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.coinPickup);

        SaveLoginData(DateTime.Now.Date, consecutiveDays, true);
        GameManager.Instance.UpdateMetaUI();
        Update7DayUI();
        UpdatePulseEffect();
    }

    // ==========================================
    // ВСТАВКИ ТА UI
    // ==========================================
    public void SwitchTab(bool toDaily)
    {
        isShowingDaily = toDaily;
        
        // ГАРАНТОВАНО вмикаємо смугу 7 днів для обох вкладок
        if (top7DaysTrackObj != null) top7DaysTrackObj.SetActive(true); 
        
        if (dailyContainerObj) dailyContainerObj.SetActive(toDaily);
        if (achievementsContainerObj) achievementsContainerObj.SetActive(!toDaily);

        if (tabDailyButton) tabDailyButton.GetComponent<Image>().color = toDaily ? Color.white : Color.gray;
        if (tabAchievementsButton) tabAchievementsButton.GetComponent<Image>().color = !toDaily ? Color.white : Color.gray;

        if (mainScrollRect != null) 
            mainScrollRect.content = toDaily ? dailyContainer.GetComponent<RectTransform>() : achievementsContainer.GetComponent<RectTransform>();

        RefreshUI();
    }

    private void GenerateNewDailyQuests()
    {
        activeDailies.Clear();
        List<QuestInfo> pool = new List<QuestInfo>(allDailyQuests);
        
        for (int i = 0; i < dailyQuestsPerDay; i++)
        {
            if (pool.Count == 0) break;
            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            QuestInfo selected = pool[randomIndex];
            
            activeDailies.Add(new ActiveQuest { questID = selected.questID, currentProgress = 0, isClaimed = false });
            pool.RemoveAt(randomIndex);
        }
        SaveDailies();
    }

    public void RefreshUI()
    {
        foreach (var obj in spawnedRows) Destroy(obj);
        spawnedRows.Clear();

        List<ActiveQuest> currentList = isShowingDaily ? activeDailies : activeAchievements;
        List<QuestInfo> db = isShowingDaily ? allDailyQuests : allAchievements;
        Transform container = isShowingDaily ? dailyContainer : achievementsContainer;

        float startYOffset = 30f; 
        float rowSpacing = 110f;  
        int rowIndex = 0;

        foreach (var active in currentList)
        {
            QuestInfo info = db.Find(q => q.questID == active.questID);
            if (info == null) continue;

            GameObject row = Instantiate(questRowPrefab, container);
            spawnedRows.Add(row);

            // Налаштування позиції
            RectTransform rt = row.GetComponent<RectTransform>();
            Vector2 originalSize = rt.sizeDelta; 
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = originalSize; 
            rt.anchoredPosition = new Vector2(0, -startYOffset - (rowIndex * rowSpacing)); 
            rowIndex++;

            // ==========================================
            // НОВИЙ ПІДХІД: Беремо компонент напряму!
            // ==========================================
            QuestRowUI rowUI = row.GetComponent<QuestRowUI>();
            if (rowUI != null)
            {
                // Динамічна нагорода
                int finalReward = info.baseRewardAmount;
                if (info.isDynamicReward && info.rewardType == RewardType.Gold && GameManager.Instance != null)
                {
                    float multiplier = 1f + (GameManager.Instance.currentWave * 0.15f);
                    finalReward = Mathf.RoundToInt(finalReward * multiplier);
                }

                // Заповнюємо тексти та іконки
                if (rowUI.titleText) rowUI.titleText.text = info.questName;
                if (rowUI.descText) rowUI.descText.text = info.questDescription; // ТЕПЕР ОПИС ТОЧНО ПРАЦЮВАТИМЕ!
                
                if (rowUI.progressText) rowUI.progressText.text = $"{active.currentProgress} / {info.targetAmount}";
                if (rowUI.progressSlider)
                {
                    rowUI.progressSlider.maxValue = info.targetAmount;
                    rowUI.progressSlider.value = active.currentProgress;
                }
                
                if (rowUI.questIcon && info.questIcon) rowUI.questIcon.sprite = info.questIcon;
                
                if (rowUI.rewardText) rowUI.rewardText.text = $"+{finalReward}";
                if (rowUI.rewardIcon) rowUI.rewardIcon.sprite = (info.rewardType == RewardType.Gold) ? goldIcon : gemIcon;

                // Налаштування кнопки
                if (rowUI.claimBtn != null)
                {
                    rowUI.claimBtn.onClick.RemoveAllListeners(); // Очищаємо старі кліки
                    
                    if (active.isClaimed)
                    {
                        rowUI.claimBtn.interactable = false;
                        if (rowUI.btnText) rowUI.btnText.text = "CLAIMED";
                    }
                    else if (active.currentProgress >= info.targetAmount)
                    {
                        rowUI.claimBtn.interactable = true;
                        if (rowUI.btnText) rowUI.btnText.text = "CLAIM!";
                        ActiveQuest currentActive = active; 
                        rowUI.claimBtn.onClick.AddListener(() => ClaimReward(currentActive, info, finalReward));
                    }
                    else
                    {
                        rowUI.claimBtn.interactable = false;
                        if (rowUI.btnText) rowUI.btnText.text = "CLAIM";
                    }
                }
            }
        }
        
        RectTransform containerRT = container.GetComponent<RectTransform>();
        containerRT.sizeDelta = new Vector2(containerRT.sizeDelta.x, startYOffset + (currentList.Count * rowSpacing) + 50f);
        UpdatePulseEffect();
    }

    private void ClaimReward(ActiveQuest active, QuestInfo info, int finalReward)
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.coinPickup);

        if (info.rewardType == RewardType.Gold) GameManager.Instance.AddResource(ResourceType.Gold, finalReward);
        else if (info.rewardType == RewardType.Gems) 
        {
            GameManager.Instance.gems += finalReward;
            GameManager.Instance.UpdateMetaUI();
        }

        active.isClaimed = true;
        if (isShowingDaily) SaveDailies();
        else SaveAchievements();
        
        RefreshUI();
    }

    public void AddQuestProgress(QuestType type, int amount)
    {
        bool changed = false;

        foreach (var active in activeDailies)
        {
            if (active.isClaimed) continue;
            QuestInfo info = allDailyQuests.Find(q => q.questID == active.questID);
            if (info != null && info.type == type && active.currentProgress < info.targetAmount)
            {
                active.currentProgress = Mathf.Min(active.currentProgress + amount, info.targetAmount);
                changed = true;
            }
        }

        foreach (var active in activeAchievements)
        {
            if (active.isClaimed) continue;
            QuestInfo info = allAchievements.Find(q => q.questID == active.questID);
            if (info != null && info.type == type && active.currentProgress < info.targetAmount)
            {
                active.currentProgress = Mathf.Min(active.currentProgress + amount, info.targetAmount);
                changed = true;
            }
        }

        if (changed)
        {
            SaveDailies();
            SaveAchievements();
            if (questsPanel != null && questsPanel.activeSelf) RefreshUI();
        }
    }

    private void SaveDailies()
    {
        PlayerPrefs.SetInt("DailiesCount", activeDailies.Count);
        for (int i = 0; i < activeDailies.Count; i++)
        {
            PlayerPrefs.SetString($"Daily_{i}_ID", activeDailies[i].questID);
            PlayerPrefs.SetInt($"Daily_{i}_Prog", activeDailies[i].currentProgress);
            PlayerPrefs.SetInt($"Daily_{i}_Claim", activeDailies[i].isClaimed ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    private void LoadDailyQuests()
    {
        activeDailies.Clear();
        int count = PlayerPrefs.GetInt("DailiesCount", 0);
        for (int i = 0; i < count; i++)
        {
            activeDailies.Add(new ActiveQuest {
                questID = PlayerPrefs.GetString($"Daily_{i}_ID", ""),
                currentProgress = PlayerPrefs.GetInt($"Daily_{i}_Prog", 0),
                isClaimed = PlayerPrefs.GetInt($"Daily_{i}_Claim", 0) == 1
            });
        }
    }

    private void LoadAchievements()
    {
        activeAchievements.Clear();
        foreach (var info in allAchievements)
        {
            int prog = PlayerPrefs.GetInt($"Achieve_{info.questID}_Prog", 0);
            bool claimed = PlayerPrefs.GetInt($"Achieve_{info.questID}_Claim", 0) == 1;
            activeAchievements.Add(new ActiveQuest { questID = info.questID, currentProgress = prog, isClaimed = claimed });
        }
    }

    private void SaveAchievements()
    {
        foreach (var active in activeAchievements)
        {
            PlayerPrefs.SetInt($"Achieve_{active.questID}_Prog", active.currentProgress);
            PlayerPrefs.SetInt($"Achieve_{active.questID}_Claim", active.isClaimed ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    // ==========================================
    // АНІМАЦІЯ ВІДКРИТТЯ / ЗАКРИТТЯ ПАНЕЛІ
    // ==========================================
    public void TogglePanel()
    {
        // ДОДАНО: Звук кліку при відкритті та закритті (гучність 2.0f як у інших кнопок)
        if (SoundManager.Instance != null && SoundManager.Instance.clickSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 2.0f);
        }

        if (questsPanel != null)
        {
            bool isOpening = !questsPanel.activeSelf;
            if (isOpening) 
            {
                GameManager.Instance.CloseAllPanels();
                CheckDailyReset();
                RefreshUI();
                StartCoroutine(AnimatePanel(questsPanel, true));
            }
            else
            {
                StartCoroutine(AnimatePanel(questsPanel, false));
            }
        }
        UpdatePulseEffect();
    }

    private IEnumerator AnimatePanel(GameObject panel, bool isOpening)
    {
        if (panel == null) yield break;

        if (!originalPanelScales.ContainsKey(panel))
        {
            originalPanelScales[panel] = panel.transform.localScale;
        }

        float duration = 0.15f; 
        float elapsed = 0f;

        Vector3 baseScale = originalPanelScales[panel];
        Vector3 startScale = isOpening ? baseScale * 0.8f : baseScale;
        Vector3 endScale = isOpening ? baseScale : baseScale * 0.8f;

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.gameObject.AddComponent<CanvasGroup>();

        float startAlpha = isOpening ? 0f : 1f;
        float endAlpha = isOpening ? 1f : 0f;

        if (isOpening) panel.SetActive(true);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / duration;
            float ease = isOpening ? 1f - Mathf.Pow(1f - t, 3f) : t * t * t; 

            panel.transform.localScale = Vector3.Lerp(startScale, endScale, ease);
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, ease);

            yield return null;
        }

        panel.transform.localScale = endScale;
        cg.alpha = endAlpha;

        if (!isOpening) panel.SetActive(false);
    }

    public void UpdatePulseEffect()
    {
        if (openButtonPulse == null) return;
        
        bool hasReward = false;
        
        if (!todayLoginClaimed) hasReward = true;
        
        if (!hasReward) {
            foreach (var active in activeDailies) {
                if (!active.isClaimed) {
                    QuestInfo info = allDailyQuests.Find(q => q.questID == active.questID);
                    if (info != null && active.currentProgress >= info.targetAmount) { hasReward = true; break; }
                }
            }
        }
        
        if (!hasReward) {
            foreach (var active in activeAchievements) {
                if (!active.isClaimed) {
                    QuestInfo info = allAchievements.Find(q => q.questID == active.questID);
                    if (info != null && active.currentProgress >= info.targetAmount) { hasReward = true; break; }
                }
            }
        }
        
        bool isPanelClosed = (questsPanel == null || !questsPanel.activeSelf);
        openButtonPulse.SetPulse(hasReward && isPanelClosed);
    }
}