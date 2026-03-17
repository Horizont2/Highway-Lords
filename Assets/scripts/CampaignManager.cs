using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections; 
using System.Collections.Generic;

public class CampaignManager : MonoBehaviour
{
    public static CampaignManager Instance { get; private set; }

    [Header("Main Panels")]
    public GameObject mainCampaignPanel; // Сюди кидай ГОЛОВНИЙ об'єкт (де є темна тінь-фон)
    public Transform campaignContentPanel; // Сюди кидай ПЕРГАМЕНТ (щоб анімувався тільки він)
    public GameObject scoutPanel; 

    [Header("Information Texts")]
    public TMP_Text titleText; 
    public TMP_Text enemyPowerText;
    public TMP_Text goldRewardText, woodRewardText, stoneRewardText;

    [Header("Unit Management (Knight, Archer, Spear, Cav)")]
    public TMP_Text[] unitCountersTexts; 
    public Button[] addButtons;
    public Button[] removeButtons;
    public TMP_Text[] unitCostTexts;
    
    public TMP_Text[] unitDamageTexts; 
    public TMP_Text[] unitHealthTexts; 
    public Image[] unitAvatars;        

    [Header("Bottom UI")]
    public Slider winChanceSlider; 
    public TMP_Text winChanceLabel; 
    public TMP_Text totalCostText;
    public TMP_Text playerGoldText; 
    public Button attackButton;
    public Button closeButton;

    [Header("Limit Upgrades")]
    public Button upgradeLimitBtn;
    public TMP_Text upgradeLimitCostText;
    public TMP_Text currentLimitDisplay; 

    [Header("Scout")]
    public Button openScoutBtn;
    public Button closeScoutBtn;
    public TMP_Text scoutInfoText; 

    private int[] selectedUnits = new int[4];
    private MapNode currentNode;
    private int globalMaxLimit = 15; 
    private int limitUpgradeCost = 1500;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (mainCampaignPanel) mainCampaignPanel.SetActive(false);
        if (scoutPanel) scoutPanel.SetActive(false);

        globalMaxLimit = PlayerPrefs.GetInt("CampaignGlobalLimit", 15);
        UpdateLimitUpgradeCost();
    }

    void Start()
    {
        // --- ФІКС КНОПОК МІНУС ТА ПЛЮС ---
        // Жорстке призначення замість циклу for, щоб уникнути втрати індексів делегатами в Unity
        if (addButtons.Length > 0 && addButtons[0] != null) { addButtons[0].onClick.RemoveAllListeners(); addButtons[0].onClick.AddListener(() => AddUnit(0)); }
        if (addButtons.Length > 1 && addButtons[1] != null) { addButtons[1].onClick.RemoveAllListeners(); addButtons[1].onClick.AddListener(() => AddUnit(1)); }
        if (addButtons.Length > 2 && addButtons[2] != null) { addButtons[2].onClick.RemoveAllListeners(); addButtons[2].onClick.AddListener(() => AddUnit(2)); }
        if (addButtons.Length > 3 && addButtons[3] != null) { addButtons[3].onClick.RemoveAllListeners(); addButtons[3].onClick.AddListener(() => AddUnit(3)); }

        if (removeButtons.Length > 0 && removeButtons[0] != null) { removeButtons[0].onClick.RemoveAllListeners(); removeButtons[0].onClick.AddListener(() => RemoveUnit(0)); }
        if (removeButtons.Length > 1 && removeButtons[1] != null) { removeButtons[1].onClick.RemoveAllListeners(); removeButtons[1].onClick.AddListener(() => RemoveUnit(1)); }
        if (removeButtons.Length > 2 && removeButtons[2] != null) { removeButtons[2].onClick.RemoveAllListeners(); removeButtons[2].onClick.AddListener(() => RemoveUnit(2)); }
        if (removeButtons.Length > 3 && removeButtons[3] != null) { removeButtons[3].onClick.RemoveAllListeners(); removeButtons[3].onClick.AddListener(() => RemoveUnit(3)); }

        if (closeButton) closeButton.onClick.AddListener(ClosePanel);
        if (attackButton) attackButton.onClick.AddListener(LaunchAttack);
        
        if (upgradeLimitBtn) upgradeLimitBtn.onClick.AddListener(UpgradeUnitLimit);
        if (openScoutBtn) openScoutBtn.onClick.AddListener(OpenScoutPanel);
        if (closeScoutBtn) closeScoutBtn.onClick.AddListener(CloseScoutPanel);

        if (CrossSceneData.isReturningFromBattle)
        {
            CrossSceneData.isReturningFromBattle = false;
            if (AnimatedBattleResult.Instance != null)
            {
                AnimatedBattleResult.Instance.ShowResult(
                    CrossSceneData.lastBattleWon, 
                    CrossSceneData.rewardGold, 
                    CrossSceneData.rewardWood, 
                    CrossSceneData.rewardStone
                );
            }
        }
    }

    public void ResetCampaignLimits()
    {
        PlayerPrefs.DeleteKey("CampaignGlobalLimit");
        globalMaxLimit = 15; 
        for (int i = 0; i < 4; i++) selectedUnits[i] = 0;
        UpdateLimitUpgradeCost();
        UpdateUI();
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        Debug.Log("Слоти армії та вибір скинуто до 15!");
    }

    public void OpenPanel(MapNode node)
    {
        currentNode = node;

        if (titleText) titleText.text = "ATTACK: " + node.campName.ToUpper();
        if (enemyPowerText) enemyPowerText.text = "ENEMY POWER: " + node.enemyPowerScore;
        if (goldRewardText) goldRewardText.text = node.rewardGold.ToString();
        if (woodRewardText) woodRewardText.text = node.rewardWood.ToString();
        if (stoneRewardText) stoneRewardText.text = node.rewardStone.ToString();

        for (int i = 0; i < 4; i++) selectedUnits[i] = 0;

        SyncUnitStats(); 
        UpdateUI();
        
        if (mainCampaignPanel) 
        {
            mainCampaignPanel.SetActive(true); // Темний фон з'являється миттєво
            
            // Анімуємо тільки пергамент
            Transform targetPanel = campaignContentPanel != null ? campaignContentPanel : mainCampaignPanel.transform;
            StartCoroutine(AnimatePanel(targetPanel, true));
        }
    }

    public void ClosePanel()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        
        if (mainCampaignPanel) 
        {
            Transform targetPanel = campaignContentPanel != null ? campaignContentPanel : mainCampaignPanel.transform;
            StartCoroutine(AnimatePanel(targetPanel, false));
        }
    }

    // --- МАГІЯ АНІМАЦІЇ UI ---
    private IEnumerator AnimatePanel(Transform panel, bool show)
    {
        float duration = 0.25f;
        float time = 0f;

        Vector3 startScale = show ? new Vector3(0.8f, 0.8f, 1f) : Vector3.one;
        Vector3 endScale = show ? Vector3.one : new Vector3(0.8f, 0.8f, 1f);

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.gameObject.AddComponent<CanvasGroup>();

        float startAlpha = show ? 0f : 1f;
        float endAlpha = show ? 1f : 0f;

        if (show) panel.localScale = startScale;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime; 
            float t = time / duration;
            
            float ease = show ? (t * t * (3f - 2f * t)) : (t * t); 

            panel.localScale = Vector3.Lerp(startScale, endScale, ease);
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            
            yield return null;
        }

        panel.localScale = endScale;
        cg.alpha = endAlpha;

        // Важливо: після закінчення анімації зникнення вимикаємо всю панель з фоном
        if (!show && mainCampaignPanel != null) mainCampaignPanel.SetActive(false);
    }

    void SyncUnitStats()
    {
        if (GameManager.Instance == null) return;

        int k_lvl = GameManager.Instance.knightLevel;
        if (unitDamageTexts.Length > 0 && unitDamageTexts[0]) unitDamageTexts[0].text = (35 + (k_lvl * 7)).ToString(); 
        if (unitHealthTexts.Length > 0 && unitHealthTexts[0]) unitHealthTexts[0].text = (120 + (k_lvl * 20)).ToString();  
        if (unitCostTexts.Length > 0 && unitCostTexts[0]) unitCostTexts[0].text = GameManager.Instance.knightFixedCost.ToString();
        if (unitAvatars.Length > 0 && unitAvatars[0]) unitAvatars[0].color = Color.white;

        int a_lvl = GameManager.Instance.archerLevel;
        if (unitDamageTexts.Length > 1 && unitDamageTexts[1]) unitDamageTexts[1].text = (25 + (a_lvl * 5)).ToString();
        if (unitHealthTexts.Length > 1 && unitHealthTexts[1]) unitHealthTexts[1].text = (60 + (a_lvl * 10)).ToString();
        if (unitCostTexts.Length > 1 && unitCostTexts[1]) unitCostTexts[1].text = GameManager.Instance.archerFixedCost.ToString();
        if (unitAvatars.Length > 1 && unitAvatars[1]) unitAvatars[1].color = Color.white;

        if (GameManager.Instance.isSpearmanUnlocked)
        {
            int s_lvl = GameManager.Instance.spearmanLevel;
            if (unitDamageTexts.Length > 2 && unitDamageTexts[2]) unitDamageTexts[2].text = (30 + (s_lvl * 6)).ToString();
            if (unitHealthTexts.Length > 2 && unitHealthTexts[2]) unitHealthTexts[2].text = (90 + (s_lvl * 15)).ToString();
            if (unitCostTexts.Length > 2 && unitCostTexts[2]) unitCostTexts[2].text = GameManager.Instance.spearmanFixedCost.ToString();
            if (unitAvatars.Length > 2 && unitAvatars[2]) unitAvatars[2].color = Color.white;
        }
        else
        {
            if (unitDamageTexts.Length > 2 && unitDamageTexts[2]) unitDamageTexts[2].text = "-";
            if (unitHealthTexts.Length > 2 && unitHealthTexts[2]) unitHealthTexts[2].text = "-";
            if (unitCostTexts.Length > 2 && unitCostTexts[2]) unitCostTexts[2].text = "-";
            if (unitAvatars.Length > 2 && unitAvatars[2]) unitAvatars[2].color = Color.black; 
        }

        bool cavUnlocked = GameManager.Instance.isCavalryUnlocked && GameManager.Instance.barracksLevel >= 3;
        if (cavUnlocked)
        {
            int c_lvl = GameManager.Instance.cavalryLevel;
            if (unitDamageTexts.Length > 3 && unitDamageTexts[3]) unitDamageTexts[3].text = (40 + (c_lvl * 8)).ToString();
            if (unitHealthTexts.Length > 3 && unitHealthTexts[3]) unitHealthTexts[3].text = (150 + (c_lvl * 25)).ToString();
            if (unitCostTexts.Length > 3 && unitCostTexts[3]) unitCostTexts[3].text = GameManager.Instance.cavalryFixedCost.ToString();
            if (unitAvatars.Length > 3 && unitAvatars[3]) unitAvatars[3].color = Color.white;
        }
        else
        {
            if (unitDamageTexts.Length > 3 && unitDamageTexts[3]) unitDamageTexts[3].text = "-";
            if (unitHealthTexts.Length > 3 && unitHealthTexts[3]) unitHealthTexts[3].text = "-";
            if (unitCostTexts.Length > 3 && unitCostTexts[3]) unitCostTexts[3].text = "-";
            if (unitAvatars.Length > 3 && unitAvatars[3]) unitAvatars[3].color = Color.black;
        }
    }

    int GetTotalSelectedUnits() { return selectedUnits[0] + selectedUnits[1] + selectedUnits[2] + selectedUnits[3]; }

    void AddUnit(int index)
    {
        if (GameManager.Instance != null)
        {
            if (index == 2 && !GameManager.Instance.isSpearmanUnlocked) { PlayError(); return; }
            if (index == 3 && (!GameManager.Instance.isCavalryUnlocked || GameManager.Instance.barracksLevel < 3)) { PlayError(); return; }
        }

        if (GetTotalSelectedUnits() < globalMaxLimit)
        {
            selectedUnits[index]++;
            if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
            UpdateUI();
        }
        else PlayError();
    }

    void RemoveUnit(int index)
    {
        if (selectedUnits[index] > 0)
        {
            selectedUnits[index]--;
            if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        int totalUnits = GetTotalSelectedUnits();

        if (currentLimitDisplay) currentLimitDisplay.text = $"GLOBAL LIMIT: {totalUnits} / <color=#44FF44>{globalMaxLimit}</color>";

        if (playerGoldText != null && GameManager.Instance != null)
            playerGoldText.text = "YOUR GOLD: " + GameManager.Instance.gold.ToString();

        int totalCost = 0;
        if (GameManager.Instance != null)
        {
            totalCost += selectedUnits[0] * GameManager.Instance.knightFixedCost;
            totalCost += selectedUnits[1] * GameManager.Instance.archerFixedCost;
            totalCost += selectedUnits[2] * GameManager.Instance.spearmanFixedCost;
            totalCost += selectedUnits[3] * GameManager.Instance.cavalryFixedCost;
        }

        for (int i = 0; i < 4; i++)
        {
            bool isLocked = false;
            if (i == 2 && (GameManager.Instance == null || !GameManager.Instance.isSpearmanUnlocked)) isLocked = true;
            if (i == 3 && (GameManager.Instance == null || !GameManager.Instance.isCavalryUnlocked || GameManager.Instance.barracksLevel < 3)) isLocked = true;

            if (unitCountersTexts.Length > i && unitCountersTexts[i] != null)
            {
                unitCountersTexts[i].text = isLocked ? "-" : selectedUnits[i].ToString();
            }

            if (removeButtons.Length > i && removeButtons[i] != null)
            {
                bool canRemove = (!isLocked && selectedUnits[i] > 0);
                
                removeButtons[i].interactable = canRemove;

                // --- ЖОРСТКИЙ ФІКС КЛІКІВ ТА ВІЗУАЛУ ---
                CanvasGroup cg = removeButtons[i].GetComponent<CanvasGroup>();
                if (cg == null) cg = removeButtons[i].gameObject.AddComponent<CanvasGroup>();
                
                cg.alpha = canRemove ? 1f : 0.4f; // Робимо яскравою, якщо можна відняти
                cg.blocksRaycasts = canRemove;    // Примусово вмикаємо реєстрацію кліків мишки
                cg.interactable = canRemove;
            }
        }

        if (totalCostText) totalCostText.text = "TOTAL COST: " + totalCost.ToString();

        bool canAfford = GameManager.Instance != null && GameManager.Instance.gold >= totalCost;
        bool hasArmy = totalUnits > 0;
        
        if (attackButton) attackButton.interactable = canAfford && hasArmy;

        UpdateWinChanceSlider();
        UpdateButtonStates(canAfford, totalCost);
    }

    void UpdateButtonStates(bool canAfford, int totalCost)
    {
        if (GameManager.Instance == null) return;
        int currentGold = GameManager.Instance.gold;
        int totalUnits = GetTotalSelectedUnits();
        bool limitNotReached = totalUnits < globalMaxLimit;

        if (addButtons.Length > 0 && addButtons[0] != null) addButtons[0].interactable = (currentGold >= totalCost + GameManager.Instance.knightFixedCost) && limitNotReached;
        if (addButtons.Length > 1 && addButtons[1] != null) addButtons[1].interactable = (currentGold >= totalCost + GameManager.Instance.archerFixedCost) && limitNotReached;
        if (addButtons.Length > 2 && addButtons[2] != null) addButtons[2].interactable = (currentGold >= totalCost + GameManager.Instance.spearmanFixedCost) && limitNotReached && GameManager.Instance.isSpearmanUnlocked;
        if (addButtons.Length > 3 && addButtons[3] != null) addButtons[3].interactable = (currentGold >= totalCost + GameManager.Instance.cavalryFixedCost) && limitNotReached && GameManager.Instance.isCavalryUnlocked && GameManager.Instance.barracksLevel >= 3;
    }

    void UpdateWinChanceSlider()
    {
        if (currentNode == null || GameManager.Instance == null) return;

        float playerPower = 0;
        playerPower += selectedUnits[0] * (1f + Mathf.Max(0, GameManager.Instance.knightLevel - 1) * 0.4f);
        playerPower += selectedUnits[1] * (1f + Mathf.Max(0, GameManager.Instance.archerLevel - 1) * 0.4f);
        playerPower += selectedUnits[2] * (1f + Mathf.Max(0, GameManager.Instance.spearmanLevel - 1) * 0.4f);
        playerPower += selectedUnits[3] * (1f + Mathf.Max(0, GameManager.Instance.cavalryLevel - 1) * 0.4f);

        float ratio = playerPower / Mathf.Max(1f, currentNode.enemyPowerScore);

        if (winChanceSlider)
        {
            winChanceSlider.minValue = 0f;
            winChanceSlider.maxValue = 2f;
            winChanceSlider.value = Mathf.Clamp(ratio, 0f, 2f);
        }

        if (winChanceLabel)
        {
            if (ratio < 0.7f) { winChanceLabel.text = "<color=#FF4444>LOW</color>"; }
            else if (ratio < 1.3f) { winChanceLabel.text = "<color=#FFDD44>MEDIUM</color>"; }
            else { winChanceLabel.text = "<color=#44FF44>HIGH</color>"; }
        }
    }

    void OpenScoutPanel()
    {
        if (currentNode == null) return;
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);

        if (scoutInfoText)
        {
            scoutInfoText.text = $"SCOUT REPORT:\n\n" +
                                 $"Guards (Infantry): {currentNode.e_guards}\n" +
                                 $"Archers: {currentNode.e_archers}\n" +
                                 $"Spearmen: {currentNode.e_spearmen}\n" +
                                 $"Cavalry: {currentNode.e_cavalry}";
        }
        if (scoutPanel) scoutPanel.SetActive(true);
    }

    void CloseScoutPanel()
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        if (scoutPanel) scoutPanel.SetActive(false);
    }

    void UpdateLimitUpgradeCost()
    {
        limitUpgradeCost = 1500 + ((globalMaxLimit - 15) / 5 * 500);
        if (upgradeLimitCostText) upgradeLimitCostText.text = limitUpgradeCost.ToString();
    }

    void UpgradeUnitLimit()
    {
        if (GameManager.Instance != null && GameManager.Instance.gold >= limitUpgradeCost)
        {
            GameManager.Instance.gold -= limitUpgradeCost;
            globalMaxLimit += 5;
            
            PlayerPrefs.SetInt("CampaignGlobalLimit", globalMaxLimit);
            PlayerPrefs.Save();

            if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);
            
            UpdateLimitUpgradeCost();
            UpdateUI();
        }
        else PlayError();
    }

    void LaunchAttack()
    {
        if (GameManager.Instance == null || currentNode == null) return;

        int totalCost = (selectedUnits[0] * GameManager.Instance.knightFixedCost) +
                        (selectedUnits[1] * GameManager.Instance.archerFixedCost) +
                        (selectedUnits[2] * GameManager.Instance.spearmanFixedCost) +
                        (selectedUnits[3] * GameManager.Instance.cavalryFixedCost);

        GameManager.Instance.gold -= totalCost;
        GameManager.Instance.SaveGame(); 
        
        CrossSceneData.knightsCount = selectedUnits[0];
        CrossSceneData.archersCount = selectedUnits[1];
        CrossSceneData.spearmenCount = selectedUnits[2];
        CrossSceneData.cavalryCount = selectedUnits[3];

        if (GameManager.Instance.knightSkins != null && GameManager.Instance.knightSkins.Length > 0)
            CrossSceneData.knightSkin = GameManager.Instance.knightSkins[Mathf.Clamp(GameManager.Instance.knightLevel, 0, GameManager.Instance.knightSkins.Length - 1)];

        if (GameManager.Instance.archerSkins != null && GameManager.Instance.archerSkins.Length > 0)
            CrossSceneData.archerSkin = GameManager.Instance.archerSkins[Mathf.Clamp(GameManager.Instance.archerLevel, 0, GameManager.Instance.archerSkins.Length - 1)];

        if (GameManager.Instance.spearmanSkins != null && GameManager.Instance.spearmanSkins.Length > 0)
            CrossSceneData.spearmanSkin = GameManager.Instance.spearmanSkins[Mathf.Clamp(GameManager.Instance.spearmanLevel, 0, GameManager.Instance.spearmanSkins.Length - 1)];

        if (GameManager.Instance.cavalrySkins != null && GameManager.Instance.cavalrySkins.Length > 0)
            CrossSceneData.cavalrySkin = GameManager.Instance.cavalrySkins[Mathf.Clamp(GameManager.Instance.cavalryLevel, 0, GameManager.Instance.cavalrySkins.Length - 1)];

        CrossSceneData.enemyGuards = currentNode.e_guards;
        CrossSceneData.enemyArchers = currentNode.e_archers;
        CrossSceneData.enemySpearmen = currentNode.e_spearmen;
        CrossSceneData.enemyCavalry = currentNode.e_cavalry;

        CrossSceneData.spentGold = totalCost;
        CrossSceneData.campId = currentNode.campId.ToString();
        CrossSceneData.campLevel = currentNode.campLevel;
        CrossSceneData.campName = currentNode.campName;

        CrossSceneData.rewardGold = currentNode.rewardGold;
        CrossSceneData.rewardWood = currentNode.rewardWood;
        CrossSceneData.rewardStone = currentNode.rewardStone;

        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.waveStart);
        
        SceneManager.LoadScene("SiegeBattleScene");
    }

    void PlayError() { if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.error); }
}