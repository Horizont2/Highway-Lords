using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Оновлення й анімації інтерфейсу оборони.
///
/// Це частина того самого класу GameManager (partial), а не окремий
/// компонент. Поля лишаються в головному файлі: їх 263, і майже всі
/// прив'язані в інспекторі — перенесення в інший клас обнулило б
/// посилання у сцені. Розділення тут фізичне, щоб у файлах можна було
/// орієнтуватися, і воно нічого не змінює ні в поведінці, ні в
/// серіалізації, ні в прив'язках кнопок.
/// </summary>
public partial class GameManager
{
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
            hammerButton, openShopButton, openMetaShopButton, nextWaveButton, barracksIconButton, openMapButton, openQuestButton 
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

        if (closeInfoButton != null) { closeInfoButton.onClick.RemoveAllListeners(); closeInfoButton.onClick.AddListener(ToggleInfoPanel); }
        
        // Змінюємо стару кнопку виходу
        if (exitGameButton != null) { exitGameButton.onClick.RemoveAllListeners(); exitGameButton.onClick.AddListener(ShowExitConfirmation); }
        
        // Підключаємо нові кнопки підтвердження
        if (confirmExitYesBtn != null) { confirmExitYesBtn.onClick.RemoveAllListeners(); confirmExitYesBtn.onClick.AddListener(ConfirmExit); }
        if (confirmExitNoBtn != null) { confirmExitNoBtn.onClick.RemoveAllListeners(); confirmExitNoBtn.onClick.AddListener(HideExitConfirmation); }

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

        // === ФІКС: Заповнюємо розділені тексти (Назва, Стати, Тактика) ===
        // === ФІКС: Заповнюємо розділені тексти ===
        if (ui.nameText != null)
        {
            ui.nameText.text = $"{unitName} LV {level}";
        }

        if (ui.statsText != null)
        {
            // Тільки іконка і базовий урон
            ui.statsText.text = $"<color=#111111><size=120%><voffset=0.1em><sprite name=\"icon_sword\" tint=1></voffset></size></color> {curDmg}";
        }

        if (ui.bonusStatsText != null)
        {
            int diff = nextDmg - curDmg;
            // Зелений бонус окремо!
            ui.bonusStatsText.text = $"<color=#00FF00><b>+{diff}</b></color>";
        }

        if (ui.tacticsText != null)
        {
            ui.tacticsText.text = tactics; 
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
    private IEnumerator AnimatePanel(GameObject panel, bool isOpening)
    {
        if (panel == null) yield break;

        // 1. Запам'ятовуємо справжній розмір панелі (який ти виставив у сцені) при першому відкритті
        if (!originalPanelScales.ContainsKey(panel))
        {
            originalPanelScales[panel] = panel.transform.localScale;
        }

        float duration = 0.15f; // Швидкість анімації
        float elapsed = 0f;

        // 2. Рахуємо анімацію ВІДНОСНО твого оригінального розміру (зменшуємо до 80%)
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

            // Змінюємо і прозорість, і масштаб
            panel.transform.localScale = Vector3.Lerp(startScale, endScale, ease);
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, ease);

            yield return null;
        }

        // 3. Гарантовано повертаємо до твого ідеального розміру
        panel.transform.localScale = endScale;
        cg.alpha = endAlpha;

        if (!isOpening) panel.SetActive(false);
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
    void UpdateBarracksStateUI()
    {
        if (barracksIconButton == null) barracksIconButton = FindUIButton("BarracksButton");
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

        // === ФІКС: Індивідуальне оновлення текстів цін ===
        if (hireKnightCostText) hireKnightCostText.text = $"{GetCurrentKnightCost()} G";
        if (hireArcherCostText) hireArcherCostText.text = $"{GetCurrentArcherCost()} G";
        
        if (hireSpearmanCostText) 
            hireSpearmanCostText.text = isSpearmanUnlocked ? $"{GetCurrentSpearmanCost()} G" : "LOCKED";
            
        if (hireCavalryCostText) 
            hireCavalryCostText.text = isCavalryUnlocked ? $"{GetCurrentCavalryCost()} G" : "LOCKED";

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
    public void UpdateResourcesUI()
    {
        if (goldText) goldText.text = gold.ToString(); 
        if (gloryText) gloryText.text = glory.ToString();
        if (woodText) woodText.text = wood.ToString();
        if (stoneText) stoneText.text = stone.ToString();
    }
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
    private IEnumerator AnimatePriceChange(TMP_Text textElement)
    {
        if (textElement == null) yield break;

        float duration = 0.35f;
        float elapsed = 0f;
        Vector3 originalScale = Vector3.one; 
        Vector3 targetScale = new Vector3(1.3f, 1.3f, 1f); // Збільшення на 30%
        Color originalColor = Color.white; // Базовий колір (можеш змінити, якщо у тебе інший)
        Color popColor = new Color(1f, 0.6f, 0f); // Золото-оранжевий колір при зміні

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            // Ефект Ping-Pong: від 0 до 1 (збільшення) і назад від 1 до 0 (зменшення)
            float scaleT = Mathf.PingPong(progress * 2f, 1f);
            
            textElement.transform.localScale = Vector3.Lerp(originalScale, targetScale, scaleT);
            textElement.color = Color.Lerp(originalColor, popColor, scaleT);
            
            yield return null;
        }

        // Гарантовано повертаємо до нормального стану
        textElement.transform.localScale = originalScale;
        textElement.color = originalColor;
    }
}
