using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Збереження й завантаження прогресу.
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
    public void SaveGame()
    {
        SaveSystem.SetInt("SavedGold", gold);
        SaveSystem.SetInt("PlayerGlory", glory); // ДОДАНО: ЗБЕРЕЖЕННЯ СЛАВИ
        SaveSystem.SetInt("SavedWood", wood);
        SaveSystem.SetInt("SavedStone", stone);
        SaveSystem.SetInt("SavedWave", currentWave);

        SaveSystem.SetInt("SavedKnightLevel", knightLevel);
        SaveSystem.SetInt("SavedArcherLevel", archerLevel);
        SaveSystem.SetInt("SavedSpearmanLevel", spearmanLevel);
        SaveSystem.SetInt("SavedCavalryLevel", cavalryLevel); 
        SaveSystem.SetInt("SavedWallArcherLevel", wallArcherLevel); 

        SaveSystem.SetInt("SavedMaxUnits", maxUnits);
        SaveSystem.SetInt("SavedBarracksLevel", barracksLevel);
        SaveSystem.SetInt("SavedSpearmanUnlocked", isSpearmanUnlocked ? 1 : 0);
        SaveSystem.SetInt("SavedCavalryUnlocked", isCavalryUnlocked ? 1 : 0); 

        SaveSystem.SetInt("SavedMineBuilt", isMineBuilt ? 1 : 0);
        SaveSystem.SetInt("SavedMineLevel", mineLevel);

        if (castle != null) SaveSystem.SetInt("SavedCastleLevel", castle.wallLevel);

        SaveSystem.SetInt("SavedTowerLevel", towerLevel);

        SaveSystem.SetInt("SavedCrossbowBuilt", isCrossbowBuilt ? 1 : 0);
        SaveSystem.SetInt("SavedCrossbowDmgLevel", crossbowDamageLevel);
        SaveSystem.SetInt("SavedCrossbowReloadLevel", crossbowReloadLevel);

        SaveSystem.SetInt("SavedGems", gems);
        SaveSystem.SetInt("SavedCurrentKills", currentKills);
        SaveSystem.SetInt("SavedKillsToNextGem", killsToNextGem);
        
        SaveSystem.SetInt("MetaFortifiedWalls", metaFortifiedWalls);
        SaveSystem.SetInt("MetaPrecisionBows", metaPrecisionBows);
        SaveSystem.SetInt("MetaVolleyBarrage", metaVolleyBarrage);
        SaveSystem.SetInt("MetaTrophyBounty", metaTrophyBounty);
        SaveSystem.SetInt("MetaEfficientCarts", metaEfficientCarts);
        SaveSystem.SetInt("MetaMendingMasonry", metaMendingMasonry);

        // ФІКС: Більше ніяких поламаних збережень на сцені битви
        if (!isCampaignBattle)
        {
            SaveUnits();
        }
        SaveSystem.Save();
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
        SaveSystem.SetString("SavedArmyData", json);
    }
    public void LoadGame()
    {
        gold = SaveSystem.GetInt("SavedGold", 100); 
        glory = SaveSystem.GetInt("PlayerGlory", 0); // ДОДАНО: ЗАВАНТАЖЕННЯ СЛАВИ
        wood = SaveSystem.GetInt("SavedWood", 0);
        stone = SaveSystem.GetInt("SavedStone", 0);
        currentWave = SaveSystem.GetInt("SavedWave", 1);

        knightLevel = SaveSystem.GetInt("SavedKnightLevel", 1);
        archerLevel = SaveSystem.GetInt("SavedArcherLevel", 1);
        spearmanLevel = SaveSystem.GetInt("SavedSpearmanLevel", 1);
        cavalryLevel = SaveSystem.GetInt("SavedCavalryLevel", 1); 
        wallArcherLevel = SaveSystem.GetInt("SavedWallArcherLevel", 1); 

        knightUpgradeCost = EconomyConfig.GetUpgradeCost(150, knightLevel);
        archerUpgradeCost = EconomyConfig.GetUpgradeCost(120, archerLevel);
        spearmanUpgradeCost = EconomyConfig.GetUpgradeCost(140, spearmanLevel);
        cavalryUpgradeCost = EconomyConfig.GetUpgradeCost(180, cavalryLevel); 
        wallArcherUpgradeCost = EconomyConfig.GetUpgradeCost(200, wallArcherLevel); 

        maxUnits = SaveSystem.GetInt("SavedMaxUnits", 5);
        barracksLevel = SaveSystem.GetInt("SavedBarracksLevel", 0);
        if (barracksLevel > 0) isBarracksBuilt = true;

        isSpearmanUnlocked = SaveSystem.GetInt("SavedSpearmanUnlocked", 0) == 1;
        isCavalryUnlocked = SaveSystem.GetInt("SavedCavalryUnlocked", 0) == 1; 
        
        if (barracksLevel == 0) 
        {
            isSpearmanUnlocked = false;
            isCavalryUnlocked = false;
        }

        isMineBuilt = SaveSystem.GetInt("SavedMineBuilt", 0) == 1;
        mineLevel = SaveSystem.GetInt("SavedMineLevel", 0);
        if (mineLevel > 0) isMineBuilt = true;

        towerLevel = SaveSystem.GetInt("SavedTowerLevel", 1);
        towerWoodCost = GetTowerWoodCost(towerLevel);
        towerStoneCost = GetTowerStoneCost(towerLevel);

        isCrossbowBuilt = SaveSystem.GetInt("SavedCrossbowBuilt", 0) == 1;
        crossbowDamageLevel = SaveSystem.GetInt("SavedCrossbowDmgLevel", 1);
        crossbowReloadLevel = SaveSystem.GetInt("SavedCrossbowReloadLevel", 1);
        
        crossbowDamageCostGold = EconomyConfig.GetUpgradeCost(150, crossbowDamageLevel);
        crossbowReloadCostGold = 50 + (crossbowReloadLevel * 20) + Mathf.RoundToInt(Mathf.Pow(crossbowReloadLevel, 2) * 0.5f);

        gems = SaveSystem.GetInt("SavedGems", 0);
        currentKills = SaveSystem.GetInt("SavedCurrentKills", 0);
        killsToNextGem = SaveSystem.GetInt("SavedKillsToNextGem", 25);
        if (killsToNextGem < 25) killsToNextGem = 25; 
        
        metaFortifiedWalls = SaveSystem.GetInt("MetaFortifiedWalls", 0);
        metaPrecisionBows = SaveSystem.GetInt("MetaPrecisionBows", 0);
        metaVolleyBarrage = SaveSystem.GetInt("MetaVolleyBarrage", 0);
        metaTrophyBounty = SaveSystem.GetInt("MetaTrophyBounty", 0);
        metaEfficientCarts = SaveSystem.GetInt("MetaEfficientCarts", 0);
        metaMendingMasonry = SaveSystem.GetInt("MetaMendingMasonry", 0);

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
                int savedCastleLvl = SaveSystem.GetInt("SavedCastleLevel", 1);
                castle.LoadState(savedCastleLvl);
            }

            // --- НОВИЙ КОД: ЗБЕРЕЖЕННЯ АРМІЇ У "ПУЛ", А НЕ НА БАЗУ ---
            if (CrossSceneData.isReturningFromBattle)
            {
                // Додаємо врятованих юнітів до твого резерву (Pool)
                SaveSystem.SetInt("PoolKnights", SaveSystem.GetInt("PoolKnights", 0) + CrossSceneData.knightsCount);
                SaveSystem.SetInt("PoolArchers", SaveSystem.GetInt("PoolArchers", 0) + CrossSceneData.archersCount);
                SaveSystem.SetInt("PoolSpearmen", SaveSystem.GetInt("PoolSpearmen", 0) + CrossSceneData.spearmenCount);
                SaveSystem.SetInt("PoolCavalry", SaveSystem.GetInt("PoolCavalry", 0) + CrossSceneData.cavalryCount);
                SaveSystem.Save();

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
        if (SaveSystem.HasKey("SavedArmyData"))
        {
            string json = SaveSystem.GetString("SavedArmyData");
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
    [ContextMenu("Delete Save File")]
    public void DeleteSave()
    {
        SaveSystem.DeleteAll();
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
}
