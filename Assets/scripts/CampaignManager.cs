using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections; 
using System.Linq;

public class CampaignManager : MonoBehaviour
{
    public static CampaignManager Instance { get; private set; }

    [Header("Main Panels")]
    public GameObject mainCampaignPanel; 
    public Transform campaignContentPanel; 
    public GameObject scoutPanel; 
    public Transform canvasTransform;

    [Header("Information Texts")]
    public TMP_Text titleText; 
    public TMP_Text enemyPowerText;
    public TMP_Text goldRewardText, woodRewardText, stoneRewardText;

    [Header("Smart Scroll Shop (8 Cards)")]
    public GameObject[] shopCardObjects; 
    public RectTransform[] shopCardRects;
    public Button[] shopAddButtons; 
    public TMP_Text[] shopPowerTexts;
    public TMP_Text[] shopCostTexts;
    public Sprite[] unitAvatars;
    public RectTransform scrollContentRect;

    [Header("Legion Slots (9 Slots)")]
    public Button[] legionSlotButtons; 
    public Image[] legionSlotImages;
    public GameObject flyingIconPrefab;

    [Header("Bottom UI")]
    public Slider winChanceSlider; 
    public TMP_Text winChanceLabel; 
    public TMP_Text totalCostText;
    public TMP_Text playerGoldText; 
    public Button attackButton;
    public Button closeButton;
    public TMP_Text currentLimitDisplay; 
    public TMP_Text yourPowerText; 

    [Header("Limit Upgrades")]
    public Button upgradeLimitBtn;
    public TMP_Text upgradeLimitCostText;

    [Header("Scout")]
    public Button openScoutBtn;
    public Button closeScoutBtn;
    public TMP_Text scoutInfoText; 

    private int[] currentSquadSlots = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1 };
    private int[] squadCosts = new int[8];
    private int[] squadPowers = new int[8];
    private int[] selectedUnitsCount = new int[8];

    private MapNode currentNode;
    private bool isAnimating = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (mainCampaignPanel) mainCampaignPanel.SetActive(false);
        if (scoutPanel) scoutPanel.SetActive(false);

        if (legionSlotButtons != null && legionSlotButtons.Length > 0)
        {
            for (int i = 0; i < 9; i++)
            {
                if (legionSlotImages != null && i < legionSlotImages.Length && legionSlotImages[i] != null)
                    legionSlotImages[i].enabled = false;
                    
                if (i < legionSlotButtons.Length && legionSlotButtons[i] != null)
                {
                    int slotIndex = i; 
                    legionSlotButtons[i].onClick.RemoveAllListeners();
                    legionSlotButtons[i].onClick.AddListener(() => RemoveSquadFromSlot(slotIndex));
                }
            }
        }

        if (shopAddButtons != null && shopAddButtons.Length > 0)
        {
            for (int i = 0; i < shopAddButtons.Length; i++)
            {
                int unitIndex = i;
                if (shopAddButtons[i] != null)
                {
                    shopAddButtons[i].onClick.RemoveAllListeners();
                    shopAddButtons[i].onClick.AddListener(() => AddUnit(unitIndex));
                }
            }
        }
    }

    void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (attackButton != null) attackButton.onClick.AddListener(LaunchAttack);
        if (openScoutBtn != null) openScoutBtn.onClick.AddListener(OpenScoutPanel);
        if (closeScoutBtn != null) closeScoutBtn.onClick.AddListener(CloseScoutPanel);

        if (CrossSceneData.isReturningFromBattle)
        {
            CrossSceneData.isReturningFromBattle = false;
        }
    }

    public void ResetCampaignLimits()
    {
        for (int i = 0; i < 9; i++) currentSquadSlots[i] = -1;
        if (legionSlotImages != null)
        {
            foreach (var img in legionSlotImages) if (img != null) img.enabled = false;
        }
        UpdateUI();
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
    }

    public void OpenPanel(MapNode node)
    {
        currentNode = node;

        if (titleText) titleText.text = "ATTACK: " + node.campName.ToUpper();
        if (enemyPowerText) enemyPowerText.text = "ENEMY POWER: " + node.enemyPowerScore;
        if (goldRewardText) goldRewardText.text = node.rewardGold.ToString();
        if (woodRewardText) woodRewardText.text = node.rewardWood.ToString();
        if (stoneRewardText) stoneRewardText.text = node.rewardStone.ToString();
        
        for (int i = 0; i < 9; i++) currentSquadSlots[i] = -1;
        if (legionSlotImages != null)
        {
            foreach (var img in legionSlotImages) if (img != null) img.enabled = false;
        }

        CalculateStatsAndPrices();
        RefreshShopVisibility(); 
        UpdateUI();
        
        if (mainCampaignPanel) 
        {
            mainCampaignPanel.SetActive(true); 
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

    private IEnumerator AnimatePanel(Transform panel, bool show)
    {
        float duration = 0.2f; float time = 0f;
        Vector3 startScale = show ? new Vector3(0.8f, 0.8f, 1f) : Vector3.one;
        Vector3 endScale = show ? Vector3.one : new Vector3(0.8f, 0.8f, 1f);

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.gameObject.AddComponent<CanvasGroup>();

        float startAlpha = show ? 0f : 1f; float endAlpha = show ? 1f : 0f;
        if (show) panel.localScale = startScale;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime; float t = time / duration;
            panel.localScale = Vector3.Lerp(startScale, endScale, t);
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }
        panel.localScale = endScale; cg.alpha = endAlpha;
        if (!show && mainCampaignPanel != null) mainCampaignPanel.SetActive(false);
    }

    void CalculateStatsAndPrices()
    {
        if (GameManager.Instance == null) return;

        int[] lvls = { GameManager.Instance.knightLevel, GameManager.Instance.archerLevel, GameManager.Instance.spearmanLevel, GameManager.Instance.cavalryLevel };
        int[] baseDmgs = { 35, 25, 30, 40 };
        int[] baseHPs = { 120, 60, 90, 150 };
        int[] baseCosts = { 250, 375, 300, 600 }; 
        int[] costPerLevel = { 10, 15, 10, 25 }; 

        for (int i = 0; i < 4; i++)
        {
            int currentDmg = baseDmgs[i] + (lvls[i] * 7);
            int currentHp = baseHPs[i] + (lvls[i] * 20);
            
            squadPowers[i] = currentDmg + Mathf.RoundToInt(currentHp * 0.2f); 
            squadCosts[i] = baseCosts[i] + ((lvls[i] - 1) * costPerLevel[i]);
        }

        // === ФІКС: Точні стати найманців (такі ж, як у BattleManager) ===
        int cityBonus = (currentNode != null) ? currentNode.campLevel * 15 : 0;
        
        int[] mercHPs = { 200, 100, 150, 250 };   // Knight, Archer, Spearman, Cavalry
        int[] mercDmgs = { 45, 35, 40, 55 };

        for (int i = 0; i < 4; i++)
        {
            int mercIndex = i + 4; // Індекси 4, 5, 6, 7
            squadPowers[mercIndex] = mercDmgs[i] + Mathf.RoundToInt(mercHPs[i] * 0.2f) + cityBonus;
            squadCosts[mercIndex] = 0; // Найманці безкоштовні (бо вже куплені в таверні)
        }

        if (shopPowerTexts != null)
        {
            for (int i = 0; i < 8; i++)
            {
                if (i < shopPowerTexts.Length && shopPowerTexts[i] != null) 
                    shopPowerTexts[i].text = $"POWER: {squadPowers[i]}";
            }
        }
    }

    void RefreshShopVisibility()
    {
        if (GameManager.Instance == null || shopCardObjects == null || shopCardObjects.Length == 0) return;

        bool[] shouldBeVisible = new bool[8];
        shouldBeVisible[0] = true; 
        shouldBeVisible[1] = true; 
        shouldBeVisible[2] = GameManager.Instance.isSpearmanUnlocked;
        shouldBeVisible[3] = GameManager.Instance.isCavalryUnlocked && GameManager.Instance.barracksLevel >= 3;
        shouldBeVisible[4] = PlayerPrefs.GetInt("Merc_Knight_Battles", 0) > 0;
        shouldBeVisible[5] = PlayerPrefs.GetInt("Merc_Archer_Battles", 0) > 0;
        shouldBeVisible[6] = PlayerPrefs.GetInt("Merc_Spearman_Battles", 0) > 0;
        shouldBeVisible[7] = PlayerPrefs.GetInt("Merc_Cavalry_Battles", 0) > 0;

        for (int i = 0; i < 9; i++)
        {
            if (currentSquadSlots[i] != -1 && currentSquadSlots[i] < 8)
            {
                shouldBeVisible[currentSquadSlots[i]] = false; 
            }
        }

        for (int i = 0; i < 8; i++)
        {
            if (i < shopCardObjects.Length && shopCardObjects[i] != null)
            {
                shopCardObjects[i].SetActive(shouldBeVisible[i]);
            }
        }

        if (scrollContentRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContentRect);
    }

    int GetTotalSelectedSquads()
    {
        int count = 0;
        for (int i = 0; i < 9; i++) if (currentSquadSlots[i] != -1) count++;
        return count;
    }

    void AddUnit(int unitIndex)
    {
        if (isAnimating) return;

        if (GetTotalSelectedSquads() < 9)
        {
            int emptySlotIndex = -1;
            for (int i = 0; i < 9; i++) { if (currentSquadSlots[i] == -1) { emptySlotIndex = i; break; } }

            if (emptySlotIndex != -1)
            {
                if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
                StartCoroutine(AnimateSquadToSlot(unitIndex, emptySlotIndex, false));
            }
        }
        else PlayError();
    }

    void RemoveSquadFromSlot(int slotIndex)
    {
        if (isAnimating || currentSquadSlots[slotIndex] == -1) return;

        int unitIndex = currentSquadSlots[slotIndex];
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        
        StartCoroutine(AnimateSquadToSlot(unitIndex, slotIndex, true));
    }

    IEnumerator AnimateSquadToSlot(int unitIndex, int slotIndex, bool isRemoving)
    {
        isAnimating = true;

        Vector3 startPos = canvasTransform != null ? canvasTransform.position : Vector3.zero;
        Vector3 endPos = startPos;

        if (isRemoving)
        {
            currentSquadSlots[slotIndex] = -1;
            if (legionSlotImages != null && slotIndex < legionSlotImages.Length && legionSlotImages[slotIndex] != null) 
                legionSlotImages[slotIndex].enabled = false;
            
            RefreshShopVisibility(); 
        }

        if (legionSlotButtons != null && slotIndex < legionSlotButtons.Length && legionSlotButtons[slotIndex] != null)
        {
            if (isRemoving) startPos = legionSlotButtons[slotIndex].transform.position;
            else endPos = legionSlotButtons[slotIndex].transform.position;
        }

        if (shopCardRects != null && unitIndex < shopCardRects.Length && shopCardRects[unitIndex] != null)
        {
            if (isRemoving) endPos = shopCardRects[unitIndex].position;
            else startPos = shopCardRects[unitIndex].position;
        }

        GameObject flyingIcon = null;
        if (flyingIconPrefab != null && canvasTransform != null)
        {
            flyingIcon = Instantiate(flyingIconPrefab, canvasTransform);
            
            RectTransform flyRect = flyingIcon.GetComponent<RectTransform>();
            if (flyRect != null) flyRect.sizeDelta = new Vector2(90f, 90f); 
            flyingIcon.transform.localScale = Vector3.one; 
            
            Image flyImg = flyingIcon.GetComponent<Image>();
            if (unitAvatars != null && unitIndex < unitAvatars.Length && unitAvatars[unitIndex] != null) 
            {
                flyImg.sprite = unitAvatars[unitIndex];
            }
            flyingIcon.transform.position = startPos;
        }

        if (!isRemoving)
        {
            currentSquadSlots[slotIndex] = unitIndex; 
            RefreshShopVisibility();
        }

        if (flyingIcon != null)
        {
            float duration = 0.25f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (flyingIcon != null)
                    flyingIcon.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                yield return null;
            }
            if (flyingIcon != null) Destroy(flyingIcon);
        }

        if (!isRemoving)
        {
            if (legionSlotImages != null && slotIndex < legionSlotImages.Length && legionSlotImages[slotIndex] != null)
            {
                if (unitAvatars != null && unitIndex < unitAvatars.Length && unitAvatars[unitIndex] != null)
                {
                    legionSlotImages[slotIndex].sprite = unitAvatars[unitIndex];
                    legionSlotImages[slotIndex].enabled = true;
                    legionSlotImages[slotIndex].color = Color.white;
                }
            }
        }

        isAnimating = false; 
        UpdateUI(); 
    }

    void UpdateUI()
    {
        int totalSquads = GetTotalSelectedSquads();

        if (currentLimitDisplay) currentLimitDisplay.text = $"SQUADS: {totalSquads} / <color=#44FF44>9</color>";

        if (playerGoldText != null && GameManager.Instance != null)
            playerGoldText.text = "YOUR GOLD: " + GameManager.Instance.gold.ToString();

        for (int i = 0; i < 8; i++) selectedUnitsCount[i] = 0;
        for (int i = 0; i < 9; i++) { if (currentSquadSlots[i] != -1 && currentSquadSlots[i] < 8) selectedUnitsCount[currentSquadSlots[i]]++; }

        int totalCost = 0;
        int totalPower = 0;

        for (int i = 0; i < 4; i++) 
        {
            string prefsKey = (i == 0) ? "FreeKnights" : (i == 1) ? "FreeArchers" : (i == 2) ? "FreeSpearmen" : "FreeCavalry";
            int freeSquads = PlayerPrefs.GetInt(prefsKey, 0) / 5;
            int squadsToBuy = Mathf.Max(0, selectedUnitsCount[i] - freeSquads);
            totalCost += squadsToBuy * squadCosts[i];

            if (shopCostTexts != null && i < shopCostTexts.Length && shopCostTexts[i] != null)
            {
                if (freeSquads > 0) shopCostTexts[i].text = $"<color=#44FF44>FREE ({freeSquads})</color>\n{squadCosts[i]}G";
                else shopCostTexts[i].text = $"{squadCosts[i]}G";
            }
        }

        // === ФІКС: 6 літер у кольорі (#FFAAAA) та менший розмір тексту ===
        for (int i = 4; i < 8; i++)
        {
            if (shopCostTexts != null && i < shopCostTexts.Length && shopCostTexts[i] != null)
                shopCostTexts[i].text = "<color=#FFAAAA><size=80%>MERCENARY</size></color>";
        }

        for (int i = 0; i < 9; i++)
        {
            if (currentSquadSlots[i] != -1 && currentSquadSlots[i] < 8) totalPower += squadPowers[currentSquadSlots[i]];
        }

        if (totalCostText) totalCostText.text = "TOTAL COST: " + totalCost.ToString() + "G";
        if (yourPowerText) yourPowerText.text = "Your Power: " + totalPower.ToString();

        int wavesCount = (currentNode != null) ? currentNode.campLevel : 1;
        float waveMultiplier = 1f + ((wavesCount - 1) * 0.5f);
        float enemyPowerAdjusted = (currentNode != null) ? (currentNode.enemyPowerScore * waveMultiplier) : 1f;

        if (enemyPowerText) enemyPowerText.text = "ENEMY POWER: " + Mathf.RoundToInt(enemyPowerAdjusted);

        float ratio = (float)totalPower / Mathf.Max(1f, enemyPowerAdjusted);

        if (winChanceSlider)
        {
            winChanceSlider.minValue = 0f;
            winChanceSlider.maxValue = 2f;
            winChanceSlider.value = Mathf.Clamp(ratio, 0f, 2f);
        }

        if (winChanceLabel)
        {
            string prefix = "WIN CHANCE: ";
            if (ratio < 0.6f) { winChanceLabel.text = prefix + "<color=#FF4444>LOW</color>"; }
            else if (ratio < 1.1f) { winChanceLabel.text = prefix + "<color=#FFDD44>MEDIUM</color>"; }
            else { winChanceLabel.text = prefix + "<color=#44FF44>HIGH</color>"; }
        }

        bool canAfford = GameManager.Instance != null && GameManager.Instance.gold >= totalCost;
        if (attackButton) attackButton.interactable = canAfford && totalSquads > 0 && !isAnimating;

        UpdateButtonStates(totalCost);
    }

    void UpdateButtonStates(int totalCost)
    {
        if (GameManager.Instance == null || shopAddButtons == null) return;
        int currentGold = GameManager.Instance.gold;
        
        bool limitNotReached = GetTotalSelectedSquads() < 9; 

        for (int i = 0; i < 4; i++) 
        {
            if (i < shopAddButtons.Length && shopAddButtons[i] != null)
            {
                string prefsKey = (i == 0) ? "FreeKnights" : (i == 1) ? "FreeArchers" : (i == 2) ? "FreeSpearmen" : "FreeCavalry";
                int freeSquads = PlayerPrefs.GetInt(prefsKey, 0) / 5;
                bool canAfford = (selectedUnitsCount[i] < freeSquads) || (currentGold >= totalCost + squadCosts[i]);
                shopAddButtons[i].interactable = limitNotReached && canAfford && !isAnimating;
            }
        }

        for (int i = 4; i < 8; i++) 
        {
            if (i < shopAddButtons.Length && shopAddButtons[i] != null)
            {
                shopAddButtons[i].interactable = limitNotReached && !isAnimating;
            }
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
    
    void UpgradeUnitLimit() { PlayError(); }

    void LaunchAttack()
    {
        if (GameManager.Instance == null || currentNode == null) return;

        int totalCost = 0;
        for (int i = 0; i < 4; i++) 
        {
            string prefsKey = (i == 0) ? "FreeKnights" : (i == 1) ? "FreeArchers" : (i == 2) ? "FreeSpearmen" : "FreeCavalry";
            int freeSquads = PlayerPrefs.GetInt(prefsKey, 0) / 5;
            int squadsToBuy = Mathf.Max(0, selectedUnitsCount[i] - freeSquads);
            totalCost += squadsToBuy * squadCosts[i];
        }

        PlayerPrefs.SetInt("FreeKnights", Mathf.Max(0, PlayerPrefs.GetInt("FreeKnights", 0) - (selectedUnitsCount[0] * 5)));
        PlayerPrefs.SetInt("FreeArchers", Mathf.Max(0, PlayerPrefs.GetInt("FreeArchers", 0) - (selectedUnitsCount[1] * 5)));
        PlayerPrefs.SetInt("FreeSpearmen", Mathf.Max(0, PlayerPrefs.GetInt("FreeSpearmen", 0) - (selectedUnitsCount[2] * 5)));
        PlayerPrefs.SetInt("FreeCavalry", Mathf.Max(0, PlayerPrefs.GetInt("FreeCavalry", 0) - (selectedUnitsCount[3] * 5)));

        GameManager.Instance.gold -= totalCost;
        GameManager.Instance.SaveGame(); 
        
        CrossSceneData.knightsCount = selectedUnitsCount[0] * 5;
        CrossSceneData.archersCount = selectedUnitsCount[1] * 5;
        CrossSceneData.spearmenCount = selectedUnitsCount[2] * 5;
        CrossSceneData.cavalryCount = selectedUnitsCount[3] * 5;

        CrossSceneData.useMercKnights = currentSquadSlots.Contains(4);
        CrossSceneData.useMercArchers = currentSquadSlots.Contains(5);
        CrossSceneData.useMercSpearmen = currentSquadSlots.Contains(6);
        CrossSceneData.useMercCavalry = currentSquadSlots.Contains(7);

        CrossSceneData.squadSlots = (int[])currentSquadSlots.Clone();

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
        
        LoadingManager lm = Object.FindFirstObjectByType<LoadingManager>(FindObjectsInactive.Include);
        if (lm != null) { lm.gameObject.SetActive(true); lm.LoadScene("SiegeBattleScene"); }
        else SceneManager.LoadScene("SiegeBattleScene"); 
    }

    void PlayError() { if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.error); }
}