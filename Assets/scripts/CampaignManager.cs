using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CampaignManager : MonoBehaviour
{
    public static CampaignManager Instance { get; private set; }

    [Header("Main Panels")]
    public GameObject mainCampaignPanel; 
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
    private int globalMaxLimit = 80; // Загальний ліміт
    private int limitUpgradeCost = 1500;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (mainCampaignPanel) mainCampaignPanel.SetActive(false);
        if (scoutPanel) scoutPanel.SetActive(false);

        globalMaxLimit = PlayerPrefs.GetInt("CampaignGlobalLimit", 80);
        UpdateLimitUpgradeCost();
    }

    void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            int index = i; 
            if (addButtons.Length > i && addButtons[index] != null) addButtons[index].onClick.AddListener(() => AddUnit(index));
            if (removeButtons.Length > i && removeButtons[index] != null) removeButtons[index].onClick.AddListener(() => RemoveUnit(index));
        }

        if (closeButton) closeButton.onClick.AddListener(ClosePanel);
        if (attackButton) attackButton.onClick.AddListener(LaunchAttack);
        
        if (upgradeLimitBtn) upgradeLimitBtn.onClick.AddListener(UpgradeUnitLimit);
        if (openScoutBtn) openScoutBtn.onClick.AddListener(OpenScoutPanel);
        if (closeScoutBtn) closeScoutBtn.onClick.AddListener(CloseScoutPanel);
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

        if (GameManager.Instance != null && unitCostTexts.Length == 4)
        {
            unitCostTexts[0].text = GameManager.Instance.knightFixedCost + " G";
            unitCostTexts[1].text = GameManager.Instance.archerFixedCost + " G";
            unitCostTexts[2].text = GameManager.Instance.spearmanFixedCost + " G";
            unitCostTexts[3].text = GameManager.Instance.cavalryFixedCost + " G";
        }

        UpdateUI();
        if (mainCampaignPanel) mainCampaignPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        if (mainCampaignPanel) mainCampaignPanel.SetActive(false);
    }

    // Допоміжний метод для підрахунку ВСІХ вибраних юнітів
    int GetTotalSelectedUnits()
    {
        return selectedUnits[0] + selectedUnits[1] + selectedUnits[2] + selectedUnits[3];
    }

    void AddUnit(int index)
    {
        if (GameManager.Instance != null)
        {
            if (index == 2 && !GameManager.Instance.isSpearmanUnlocked) { PlayError(); return; }
            if (index == 3 && (!GameManager.Instance.isCavalryUnlocked || GameManager.Instance.barracksLevel < 3)) { PlayError(); return; }
        }

        // ПЕРЕВІРКА ГЛОБАЛЬНОГО ЛІМІТУ
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
            playerGoldText.text = "YOUR GOLD: " + GameManager.Instance.gold + " G";

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
            if (unitCountersTexts.Length > i && unitCountersTexts[i] != null)
            {
                // Тепер пишемо просто кількість, без "/ 80" під кожним юнітом
                unitCountersTexts[i].text = selectedUnits[i].ToString();
            }
        }

        if (totalCostText) totalCostText.text = "TOTAL COST: " + totalCost + " G";

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

        if (addButtons[0] != null) addButtons[0].interactable = (currentGold >= totalCost + GameManager.Instance.knightFixedCost) && limitNotReached;
        if (addButtons[1] != null) addButtons[1].interactable = (currentGold >= totalCost + GameManager.Instance.archerFixedCost) && limitNotReached;
        if (addButtons[2] != null) addButtons[2].interactable = (currentGold >= totalCost + GameManager.Instance.spearmanFixedCost) && limitNotReached && GameManager.Instance.isSpearmanUnlocked;
        if (addButtons[3] != null) addButtons[3].interactable = (currentGold >= totalCost + GameManager.Instance.cavalryFixedCost) && limitNotReached && GameManager.Instance.isCavalryUnlocked && GameManager.Instance.barracksLevel >= 3;
    }

    void UpdateWinChanceSlider()
    {
        if (currentNode == null || GameManager.Instance == null) return;

        float playerPower = 0;
        playerPower += selectedUnits[0] * (120 + GameManager.Instance.knightLevel * 25);
        playerPower += selectedUnits[1] * (100 + GameManager.Instance.archerLevel * 20);
        playerPower += selectedUnits[2] * (140 + GameManager.Instance.spearmanLevel * 30);
        playerPower += selectedUnits[3] * (220 + GameManager.Instance.cavalryLevel * 45);

        float ratio = playerPower / (float)currentNode.enemyPowerScore;

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
        limitUpgradeCost = 1500 + ((globalMaxLimit - 80) / 5 * 500);
        if (upgradeLimitCostText) upgradeLimitCostText.text = limitUpgradeCost + " G";
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

        CrossSceneData.enemyGuards = currentNode.e_guards;
        CrossSceneData.enemyArchers = currentNode.e_archers;
        CrossSceneData.enemySpearmen = currentNode.e_spearmen;
        CrossSceneData.enemyCavalry = currentNode.e_cavalry;

        CrossSceneData.spentGold = totalCost;
        CrossSceneData.campId = currentNode.campId;
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