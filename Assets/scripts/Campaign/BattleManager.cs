using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;

[System.Serializable]
public class LegionData
{
    public int knights = 0;
    public int archers = 0;
    public int spearmen = 0;
    public int cavalry = 0;

    public int TotalUnits => knights + archers + spearmen + cavalry;
}

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    public enum BattleState { Intro, March, TacticalPause, Battle, Retreating, GameOver }
    public BattleState currentState = BattleState.Intro;

    [Header("Параметри Підкріплень")]
    public int maxLegions = 3; 
    public int currentLegion = 0;

    [Header("UI Битви (HUD)")]
    public TMP_Text locationNameText;
    public TMP_Text enemyCountText;
    public TMP_Text armyCountText;
    public Image campDefenseProgressBarFill;

    [Header("Тактична Пауза (UI)")]
    public GameObject tacticalPanel; 
    public Button attackButton;
    public Button retreatButton;
    
    public Button[] tacticalUnitButtons; 
    public Button[] rowButtons; 

    [Header("Навички Полководця")]
    public GameObject skillsPanel;
    public Button volleyButton;
    public Image volleyCooldownFill;
    public GameObject volleyLockIcon;
    public GameObject volleyEffectPrefab; 
    public Button warCryButton;
    public Image warCryCooldownFill;
    public GameObject warCryLockIcon;

    [Header("Префаби Гравця")]
    public GameObject playerKnightPrefab; 
    public GameObject playerArcherPrefab; 
    public GameObject playerSpearmanPrefab; 
    public GameObject playerCavalryPrefab; 

    // === ДОДАНО: Поля для префабів Найманців ===
    [Header("Префаби Найманців (Mercenaries)")]
    public GameObject mercKnightPrefab;
    public GameObject mercArcherPrefab;
    public GameObject mercSpearmanPrefab;
    public GameObject mercCavalryPrefab;

    [Header("Префаби Ворога")]
    public GameObject enemyGuardPrefab; 
    public GameObject enemyArcherPrefab; 
    public GameObject enemySpearmanPrefab; 
    public GameObject enemyCavalryPrefab; 

    [Header("Обертання Спрайтів")]
    public bool flipPlayerKnight = false;
    public bool flipPlayerArcher = false;
    public bool flipPlayerSpearman = false;
    public bool flipPlayerCavalry = false;
    public bool flipEnemyKnight = false;
    public bool flipEnemyArcher = false;
    public bool flipEnemySpearman = false;
    public bool flipEnemyCavalry = false;

    [Header("Ефекти та Звуки")]
    public GameObject damagePopupPrefab; 
    public GameObject bloodHitPrefab;
    public AudioClip warHornSound;
    public AudioClip marchingSound;
    private AudioSource audioSrc;

    [Header("Точки Спавну")]
    public Transform playerSpawnPoint; 
    public Transform enemySpawnPoint;  

    private List<CampaignUnit> allUnitsOnField = new List<CampaignUnit>();
    
    private int totalEnemies;
    private int enemiesSpawned;
    private int enemiesKilled;

    // Гарнізон табору — рівно той склад, який показала розвідка на мапі.
    private int garrisonGuards, garrisonArchers, garrisonSpearmen, garrisonCavalry;

    private int currentWave = 0;
    private int maxWaves = 1;

    private string highlightedClass = "";
    private List<string> activeClasses = new List<string>();
    private string[] rowAssignments = new string[4];
    private int activeLineCount = 0;

    private bool isVolleyUnlocked = false;
    private bool isWarCryUnlocked = false;
    private float volleyCooldown = 12f, currentVolleyTimer = 0f;
    private float warCryCooldown = 20f, currentWarCryTimer = 0f;
    private bool isTargetingVolley = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // GameManager у цій сцені не потрібен: BattleManager читає рівні юнітів
        // напряму з PlayerPrefs. Раніше тут створювався "Temp_GameManager" —
        // об'єкт без жодного налаштованого посилання, який ще й запускав
        // паралельний спавн армії в GameManager.Start().
    }

    /// <summary>
    /// Прибирає юніта зі списку живих. Викликається з CampaignUnit.Die().
    /// Без цього список накопичує знищені об'єкти, і звернення до них
    /// кидає MissingReferenceException у кожного юніта щокадру.
    /// </summary>
    public void UnregisterUnit(CampaignUnit unit)
    {
        if (unit == null) return;
        allUnitsOnField.Remove(unit);
    }

    void Start()
    {
        if (locationNameText) locationNameText.text = "BATTLE: " + CrossSceneData.campName.ToUpper();
        
        attackButton.onClick.AddListener(StartBattle);
        retreatButton.onClick.AddListener(Retreat);

        if (tacticalPanel) tacticalPanel.SetActive(false);
        attackButton.gameObject.SetActive(false);
        if (skillsPanel) skillsPanel.SetActive(false); 
        if (retreatButton) retreatButton.gameObject.SetActive(true);

        audioSrc = gameObject.AddComponent<AudioSource>();
        if (SoundManager.Instance != null) SoundManager.Instance.PlayBattleMusic();

        isVolleyUnlocked = PlayerPrefs.GetInt("MetaVolleyBarrage", 0) > 0;
        isWarCryUnlocked = PlayerPrefs.GetInt("SavedKnightLevel", 1) >= 2 || PlayerPrefs.GetInt("MetaFortifiedWalls", 0) > 0; 

        currentLegion = 0;

        activeClasses.Clear();
        for (int i = 0; i < 3; i++) 
        {
            int slotType = CrossSceneData.squadSlots[i];
            if (slotType != -1) 
            {
                int baseType = slotType % 4; 
                string uClass = "";
                if (baseType == 0) uClass = "Knight";
                else if (baseType == 1) uClass = "Archer";
                else if (baseType == 2) uClass = "Spearman";
                else if (baseType == 3) uClass = "Cavalry";

                if (!activeClasses.Contains(uClass)) activeClasses.Add(uClass);
            }
        }

        activeLineCount = activeClasses.Count;
        for(int i = 0; i < 4; i++) rowAssignments[i] = (i < activeLineCount) ? activeClasses[i] : "";

        SetupSkillUI();
        DisableEmptyTacticalButtons();

        Vector3 offScreenStart = playerSpawnPoint.position + new Vector3(-18f, 0, 0);
        Camera.main.transform.position = new Vector3(offScreenStart.x + 5f, Camera.main.transform.position.y, -10f);

        SpawnLegion(currentLegion, offScreenStart);
        UpdateFormationPositions(offScreenStart.x + 4f, true); 
        UpdateRowUI(); 

        CalculateEnemyPool();
        SpawnEnemyWave(); 
        
        if (campDefenseProgressBarFill) campDefenseProgressBarFill.fillAmount = 1f;

        StartCoroutine(MarchCutscene(offScreenStart));
    }

    private void SetupSkillUI()
    {
        if (volleyButton)
        {
            volleyButton.onClick.RemoveAllListeners();
            volleyButton.onClick.AddListener(OnVolleyClicked);
            if (volleyLockIcon) volleyLockIcon.SetActive(!isVolleyUnlocked);
        }

        if (warCryButton)
        {
            warCryButton.onClick.RemoveAllListeners();
            warCryButton.onClick.AddListener(OnWarCryClicked);
            if (warCryLockIcon) warCryLockIcon.SetActive(!isWarCryUnlocked);
        }
    }

    void SpawnLegion(int legionIndex, Vector3 startPos)
    {
        float squadSpacingY = 2.0f; 
        float squadSpacingX = 2.5f; 

        int startIndex = legionIndex * 3;
        
        for (int i = startIndex; i < startIndex + 3; i++)
        {
            if (i >= 9) break;

            int unitType = CrossSceneData.squadSlots[i];
            if (unitType == -1) continue;

            int slotInLegion = i % 3;

            float yPos = 0;
            float xPos = 0;
            
            if (slotInLegion == 0) { yPos = 0f; xPos = 0f; }                    
            else if (slotInLegion == 1) { yPos = squadSpacingY; xPos = -squadSpacingX; } 
            else if (slotInLegion == 2) { yPos = -squadSpacingY; xPos = -squadSpacingX; } 

            Vector3 squadCenter = startPos + new Vector3(xPos, yPos, 0);
            SpawnSquadInFormation(unitType, squadCenter);
        }
    }

    void SpawnSquadInFormation(int type, Vector3 centerPos)
    {
        GameObject prefab = null;
        string uClass = "";
        bool isMerc = type >= 4;
        bool flip = false;
        int baseType = type % 4; 

        // === ФІКС: Підв'язка префабів найманців (або дефолтних, якщо поля пусті) ===
        if (baseType == 0) { prefab = (isMerc && mercKnightPrefab != null) ? mercKnightPrefab : playerKnightPrefab; flip = flipPlayerKnight; uClass = "Knight"; }
        else if (baseType == 1) { prefab = (isMerc && mercArcherPrefab != null) ? mercArcherPrefab : playerArcherPrefab; flip = flipPlayerArcher; uClass = "Archer"; }
        else if (baseType == 2) { prefab = (isMerc && mercSpearmanPrefab != null) ? mercSpearmanPrefab : playerSpearmanPrefab; flip = flipPlayerSpearman; uClass = "Spearman"; }
        else if (baseType == 3) { prefab = (isMerc && mercCavalryPrefab != null) ? mercCavalryPrefab : playerCavalryPrefab; flip = flipPlayerCavalry; uClass = "Cavalry"; }

        if (prefab == null) return;

        int hp, dmg; float speed; bool isRanged;

        if (isMerc)
        {
            CampaignBalance.GetMercenaryStats(uClass, out hp, out dmg, out speed, out isRanged);
        }
        else
        {
            string prefsKey = uClass == "Archer" ? "SavedArcherLevel"
                            : uClass == "Spearman" ? "SavedSpearmanLevel"
                            : uClass == "Cavalry" ? "SavedCavalryLevel"
                            : "SavedKnightLevel";

            CampaignBalance.GetPlayerStats(uClass, PlayerPrefs.GetInt(prefsKey, 1),
                out hp, out dmg, out speed, out isRanged);
        }

        Vector3[] formationOffsets = new Vector3[5] {
            new Vector3( 0.0f,  0.8f, 0), 
            new Vector3( 0.0f, -0.8f, 0), 
            new Vector3(-1.2f,  1.2f, 0), 
            new Vector3(-1.2f,  0.0f, 0), 
            new Vector3(-1.2f, -1.2f, 0)  
        };

        for (int i = 0; i < CampaignBalance.UnitsPerSquad && i < formationOffsets.Length; i++)
        {
            Vector3 spawnPos = centerPos + formationOffsets[i];
            GameObject unitGO = Instantiate(prefab, spawnPos, Quaternion.identity);
            unitGO.tag = "PlayerUnit"; 

            CampaignUnit newAI = PrepareUnit(unitGO, prefab);
            newAI.Setup(false, uClass, hp, dmg, speed, isRanged, bloodHitPrefab, flip);
            
            if (isMerc) newAI.transform.localScale *= 1f; 
            
            newAI.tacticalTargetPos = spawnPos; 
            allUnitsOnField.Add(newAI);
        }
    }

    public void SpawnNextLegion()
    {
        if (currentLegion >= maxLegions - 1) return; 
        
        currentLegion++;
        if (SoundManager.Instance && warHornSound) audioSrc.PlayOneShot(warHornSound, 1f);
        
        Vector3 spawnPos = new Vector3(Camera.main.transform.position.x - 14f, playerSpawnPoint.position.y, 0f);
        
        SpawnLegion(currentLegion, spawnPos);
        UpdateFormationPositions(spawnPos.x, true);
        
        UpdateHUDText(); 
    }

    public void SpawnMercenaries()
    {
        if (SoundManager.Instance && warHornSound) audioSrc.PlayOneShot(warHornSound, 1f);

        Vector3 spawnPos = new Vector3(Camera.main.transform.position.x - 15f, playerSpawnPoint.position.y, 0f);

        if (CrossSceneData.useMercKnights) SpawnSquadInFormation(4, spawnPos + new Vector3(0, 2.5f, 0));
        if (CrossSceneData.useMercArchers) SpawnSquadInFormation(5, spawnPos);
        if (CrossSceneData.useMercSpearmen) SpawnSquadInFormation(6, spawnPos + new Vector3(0, -2.5f, 0));
        if (CrossSceneData.useMercCavalry) SpawnSquadInFormation(7, spawnPos + new Vector3(-3.0f, 0, 0));

        UpdateFormationPositions(spawnPos.x, true);
        UpdateHUDText();
    }

    void DisableEmptyTacticalButtons()
    {
        if (tacticalUnitButtons == null || tacticalUnitButtons.Length < 4) return;
        
        tacticalUnitButtons[0].interactable = activeClasses.Contains("Knight");
        tacticalUnitButtons[1].interactable = activeClasses.Contains("Archer");
        tacticalUnitButtons[2].interactable = activeClasses.Contains("Spearman");
        tacticalUnitButtons[3].interactable = activeClasses.Contains("Cavalry");
    }

    void UpdateRowUI()
    {
        if (rowButtons == null || rowButtons.Length == 0) return;
        
        for (int i = 0; i < 4; i++)
        {
            if (i >= rowButtons.Length || rowButtons[i] == null) continue;

            if (i >= activeLineCount) 
            {
                rowButtons[i].gameObject.SetActive(false); 
            }
            else
            {
                rowButtons[i].gameObject.SetActive(true);
                
                TMP_Text btnText = rowButtons[i].GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    btnText.text = $"Line {i + 1}\n{GetClassName(rowAssignments[i])}";
                }

                Image btnImg = rowButtons[i].GetComponent<Image>();
                if (btnImg != null)
                {
                    if (string.IsNullOrEmpty(highlightedClass)) 
                    {
                        rowButtons[i].interactable = false; 
                        btnImg.color = Color.white; 
                    }
                    else 
                    {
                        if (rowAssignments[i] == highlightedClass)
                        {
                            rowButtons[i].interactable = false; 
                            btnImg.color = new Color(0.3f, 0.3f, 0.3f, 1f); 
                        }
                        else
                        {
                            rowButtons[i].interactable = true; 
                            btnImg.color = new Color(0.4f, 1f, 0.4f, 1f); 
                        }
                    }
                }
            }
        }
    }

    public bool IsBattleActive() { return currentState == BattleState.Battle; }
    public List<CampaignUnit> GetAllUnits() { return allUnitsOnField; }

    IEnumerator MarchCutscene(Vector3 startOffset)
    {
        currentState = BattleState.March;

        if (warHornSound && audioSrc) audioSrc.PlayOneShot(warHornSound, 1f);
        yield return new WaitForSeconds(1.0f);

        if (marchingSound && audioSrc) { audioSrc.clip = marchingSound; audioSrc.loop = true; audioSrc.Play(); }

        Camera mainCam = Camera.main;
        Vector3 camStart = new Vector3(startOffset.x + 5f, mainCam.transform.position.y, -10f);
        Vector3 camMid = new Vector3(playerSpawnPoint.position.x, mainCam.transform.position.y, -10f); 
        Vector3 camEnemy = new Vector3(enemySpawnPoint.position.x + 6f, mainCam.transform.position.y, -10f); 

        float elapsed = 0f;
        while (elapsed < 3.0f) { elapsed += Time.deltaTime; mainCam.transform.position = Vector3.Lerp(camStart, camMid, Mathf.SmoothStep(0f, 1f, elapsed / 3.0f)); yield return null; }
        
        elapsed = 0f;
        while (elapsed < 1.5f) { elapsed += Time.deltaTime; mainCam.transform.position = Vector3.Lerp(camMid, camEnemy, Mathf.SmoothStep(0f, 1f, elapsed / 1.5f)); yield return null; }
        
        yield return new WaitForSeconds(0.8f); 

        elapsed = 0f;
        while (elapsed < 1.5f) { elapsed += Time.deltaTime; mainCam.transform.position = Vector3.Lerp(camEnemy, camMid, Mathf.SmoothStep(0f, 1f, elapsed / 1.5f)); yield return null; }

        if (marchingSound && audioSrc) audioSrc.Stop();
        
        currentState = BattleState.TacticalPause;
        if (tacticalPanel) tacticalPanel.SetActive(true);
        attackButton.gameObject.SetActive(true);
    }

    public void HighlightKnights() { SetHighlight("Knight"); }
    public void HighlightArchers() { SetHighlight("Archer"); }
    public void HighlightSpearmen() { SetHighlight("Spearman"); }
    public void HighlightCavalry() { SetHighlight("Cavalry"); }

    void SetHighlight(string uClass)
    {
        highlightedClass = uClass;
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        foreach (var u in allUnitsOnField) if (u != null && !u.isEnemy) u.SetHighlight(u.unitClass == highlightedClass);
        UpdateRowUI(); 
    }

    public void AssignHighlightedToRow(int targetRow)
    {
        if (string.IsNullOrEmpty(highlightedClass)) return;
        if (targetRow < 0 || targetRow >= activeLineCount) return;

        int currRow = -1;
        for (int i = 0; i < activeLineCount; i++) if (rowAssignments[i] == highlightedClass) { currRow = i; break; }

        if (currRow == -1 || currRow == targetRow) return;

        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);

        string classA = rowAssignments[currRow];
        string classB = rowAssignments[targetRow];
        rowAssignments[currRow] = classB;
        rowAssignments[targetRow] = classA;

        highlightedClass = ""; 
        foreach (var u in allUnitsOnField) if (u != null && !u.isEnemy) u.SetHighlight(false);

        UpdateRowUI(); 
        UpdateFormationPositions(playerSpawnPoint.position.x, false);
    }

    void UpdateFormationPositions(float baseX, bool snapInstantly)
    {
        float currentXOffset = 0f; 
        int unitsPerCol = 4; 
        float spacingX = 1.0f; 
        float spacingY = 0.8f; 

        for (int i = 0; i < activeLineCount; i++)
        {
            string uClass = rowAssignments[i];
            List<CampaignUnit> classUnits = allUnitsOnField.Where(u => u != null && !u.isEnemy && !u.isDead && u.unitClass == uClass).ToList();
            if (classUnits.Count == 0) continue;

            for (int j = 0; j < classUnits.Count; j++)
            {
                int col = j / unitsPerCol; int row = j % unitsPerCol;
                float targetX = baseX - currentXOffset - (col * spacingX);
                int currentRows = Mathf.Min(classUnits.Count - (col * unitsPerCol), unitsPerCol);
                
                float targetY = playerSpawnPoint.position.y + (row * spacingY) - ((currentRows - 1) * spacingY / 2f);
                classUnits[j].tacticalTargetPos = new Vector3(targetX, targetY, 0);
                if (snapInstantly) classUnits[j].transform.position = classUnits[j].tacticalTargetPos;
            }
            int colsNeeded = Mathf.CeilToInt((float)classUnits.Count / unitsPerCol);
            currentXOffset += (colsNeeded * spacingX) + 1.2f; 
        }
    }

    string GetClassName(string rawClass)
    {
        if (rawClass == "Knight") return "Knights";
        if (rawClass == "Spearman") return "Spearmen";
        if (rawClass == "Archer") return "Archers";
        if (rawClass == "Cavalry") return "Cavalry";
        return "Empty";
    }

    void StartBattle()
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        if (SoundManager.Instance && SoundManager.Instance.victoryCries) SoundManager.Instance.PlaySFX(SoundManager.Instance.victoryCries, 0.6f);
        
        foreach (var u in allUnitsOnField) if (u != null && !u.isEnemy) u.SetHighlight(false);
        highlightedClass = "";

        if (tacticalPanel) tacticalPanel.SetActive(false);
        attackButton.gameObject.SetActive(false);
        if (skillsPanel) skillsPanel.SetActive(true); 

        currentState = BattleState.Battle; 
    }

    void Update()
    {
        if (currentState != BattleState.Battle) return;
        UpdateHUDText();
        CheckEnemyWaves();
        UpdateSkillsCooldowns();
        HandleVolleyTargeting();
        if (Time.frameCount % 30 == 0 && !isTargetingVolley) CheckBattleVictoryCondition();
    }

    void CheckEnemyWaves()
    {
        int aliveEnemies = 0;
        foreach (var u in allUnitsOnField) if (u != null && !u.isDead && u.isEnemy) aliveEnemies++;
        
        enemiesKilled = enemiesSpawned - aliveEnemies;
        
        if (campDefenseProgressBarFill && totalEnemies > 0)
            campDefenseProgressBarFill.fillAmount = 1f - ((float)enemiesKilled / totalEnemies);

        if (aliveEnemies <= 0 && currentWave < maxWaves)
        {
            if (SoundManager.Instance && warHornSound) audioSrc.PlayOneShot(warHornSound, 2f);
            SpawnEnemyWave();
        }
    }

    void UpdateSkillsCooldowns()
    {
        if (currentVolleyTimer > 0)
        {
            currentVolleyTimer -= Time.deltaTime;
            if (volleyCooldownFill) volleyCooldownFill.fillAmount = currentVolleyTimer / volleyCooldown;
            if (volleyButton) volleyButton.interactable = false;
        }
        else if (isVolleyUnlocked && volleyButton) volleyButton.interactable = true;

        if (currentWarCryTimer > 0)
        {
            currentWarCryTimer -= Time.deltaTime;
            if (warCryCooldownFill) warCryCooldownFill.fillAmount = currentWarCryTimer / warCryCooldown;
            if (warCryButton) warCryButton.interactable = false;
        }
        else if (isWarCryUnlocked && warCryButton) warCryButton.interactable = true;
    }

    void OnVolleyClicked()
    {
        if (currentVolleyTimer > 0 || !isVolleyUnlocked) return;
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        isTargetingVolley = true; Time.timeScale = 0.2f; 
    }

    void HandleVolleyTargeting()
    {
        if (!isTargetingVolley) return;
        if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Vector3 targetPos = Camera.main.ScreenToWorldPoint(Input.mousePosition); targetPos.z = 0;
            if (volleyEffectPrefab)
            {
                // Ефект не знищував себе сам — лишався в сцені після кожного залпу.
                GameObject volley = PoolRegistry.Spawn(volleyEffectPrefab, targetPos, Quaternion.identity);
                PoolRegistry.DespawnAfter(volley, 3f);
            }
            StartCoroutine(VolleyDamageRoutine(targetPos));
            Time.timeScale = 1f; isTargetingVolley = false; currentVolleyTimer = volleyCooldown; 
        }
        else if (Input.GetMouseButtonDown(1)) { Time.timeScale = 1f; isTargetingVolley = false; }
    }

    IEnumerator VolleyDamageRoutine(Vector3 targetPos)
    {
        yield return new WaitForSeconds(0.6f);
        int damageToDeal = 150 + (PlayerPrefs.GetInt("MetaVolleyBarrage", 0) * 50);
        var snapshot = allUnitsOnField.ToArray();
        foreach (var u in snapshot)
        {
            if (u != null && !u.isDead && u.isEnemy && Vector2.Distance(u.transform.position, targetPos) <= 3.5f) u.TakeDamage(damageToDeal);
        }
    }

    void OnWarCryClicked()
    {
        if (currentWarCryTimer > 0 || !isWarCryUnlocked) return;
        if (SoundManager.Instance && warHornSound) audioSrc.PlayOneShot(warHornSound, 1f);
        foreach (var u in allUnitsOnField) if (u != null && !u.isDead && !u.isEnemy) u.ApplyWarCry(5f);
        currentWarCryTimer = warCryCooldown; 
    }

    void CalculateEnemyPool()
    {
        int campLvl = CrossSceneData.campLevel > 0 ? CrossSceneData.campLevel : 1;

        garrisonGuards = Mathf.Max(0, CrossSceneData.enemyGuards);
        garrisonArchers = Mathf.Max(0, CrossSceneData.enemyArchers);
        garrisonSpearmen = Mathf.Max(0, CrossSceneData.enemySpearmen);
        garrisonCavalry = Mathf.Max(0, CrossSceneData.enemyCavalry);

        totalEnemies = garrisonGuards + garrisonArchers + garrisonSpearmen + garrisonCavalry;

        // Сцену відкрили напряму з редактора, без заходу через мапу —
        // даємо мінімальний гарнізон, щоб було що тестувати.
        if (totalEnemies == 0)
        {
            Debug.LogWarning("[BattleManager] Гарнізон порожній (сцену запущено не з мапи). Беру запасний склад.");
            CampaignBalance.GenerateGarrison(1, campLvl, 35 * campLvl,
                out garrisonGuards, out garrisonArchers, out garrisonSpearmen, out garrisonCavalry);
            totalEnemies = garrisonGuards + garrisonArchers + garrisonSpearmen + garrisonCavalry;
        }

        maxWaves = CampaignBalance.WaveCount(totalEnemies);
        currentWave = 0;
        enemiesSpawned = 0;
        enemiesKilled = 0;
    }

    void SpawnEnemyWave()
    {
        if (currentWave >= maxWaves) return; 

        // Ділимо гарнізон між хвилями так, щоб сума точно дорівнювала розвідці.
        int waveIndex = currentWave;
        currentWave++;

        int eGuards = CampaignBalance.UnitsInWave(garrisonGuards, waveIndex, maxWaves);
        int eSpearmen = CampaignBalance.UnitsInWave(garrisonSpearmen, waveIndex, maxWaves);
        int eArchers = CampaignBalance.UnitsInWave(garrisonArchers, waveIndex, maxWaves);
        int eCavalry = CampaignBalance.UnitsInWave(garrisonCavalry, waveIndex, maxWaves);

        Vector3 spawnCenter = (currentWave == 1) ? enemySpawnPoint.position : enemySpawnPoint.position + new Vector3(15f, 0, 0);
        float currentXOffset = 0f;

        currentXOffset = SpawnEnemyGroupDynamic(enemyGuardPrefab, eGuards, spawnCenter, currentXOffset, "Knight", flipEnemyKnight);
        currentXOffset = SpawnEnemyGroupDynamic(enemySpearmanPrefab, eSpearmen, spawnCenter, currentXOffset, "Spearman", flipEnemySpearman);
        currentXOffset = SpawnEnemyGroupDynamic(enemyArcherPrefab, eArchers, spawnCenter, currentXOffset, "Archer", flipEnemyArcher);
        currentXOffset = SpawnEnemyGroupDynamic(enemyCavalryPrefab, eCavalry, spawnCenter, currentXOffset, "Cavalry", flipEnemyCavalry);

        enemiesSpawned += (eGuards + eSpearmen + eArchers + eCavalry);
    }

    float SpawnEnemyGroupDynamic(GameObject prefab, int count, Vector3 basePos, float startXOffset, string unitClass, bool applyFlip)
    {
        if (prefab == null || count <= 0) return startXOffset;

        int campLvl = CrossSceneData.campLevel > 0 ? CrossSceneData.campLevel : 1;
        CampaignBalance.GetEnemyStats(unitClass, campLvl, out int hp, out int dmg, out float speed, out bool isRanged);

        int rows = 3; 
        float spacingX = 1.2f; float spacingY = 0.9f; int colsUsed = Mathf.CeilToInt((float)count / rows);

        for (int i = 0; i < count; i++)
        {
            int col = i / rows; int row = i % rows;
            float targetX = basePos.x + startXOffset + (col * spacingX);
            int currentRows = Mathf.Min(count - (col * rows), rows);
            if (count > rows) currentRows = rows; 
            
            float targetY = basePos.y + (row * spacingY) - ((currentRows - 1) * spacingY / 2f);
            Vector3 spawnPos = new Vector3(targetX, targetY, 0);
            
            GameObject unitGO = Instantiate(prefab, spawnPos, Quaternion.identity);
            unitGO.tag = "Enemy"; 

            CampaignUnit newAI = PrepareUnit(unitGO, prefab);
            newAI.Setup(true, unitClass, hp, dmg, speed, isRanged, bloodHitPrefab, applyFlip);
            newAI.tacticalTargetPos = spawnPos; newAI.transform.position = spawnPos;
            allUnitsOnField.Add(newAI);
        }
        return startXOffset + (colsUsed * spacingX) + 1.5f;
    }

    /// <summary>
    /// Віддає CampaignUnit інстансу. Якщо його немає на префабі — додає
    /// й попереджає: правильно, щоб компонент лежав на префабі заздалегідь.
    /// Скрипти AI оборони CampaignUnit вимикає сам у Setup().
    /// </summary>
    private CampaignUnit PrepareUnit(GameObject unitGO, GameObject sourcePrefab)
    {
        var ai = unitGO.GetComponent<CampaignUnit>();
        if (ai != null) return ai;

        if (!warnedAboutMissingCampaignUnit)
        {
            warnedAboutMissingCampaignUnit = true;
            Debug.LogWarning(
                $"[BattleManager] На префабі '{sourcePrefab.name}' немає CampaignUnit, довелося додати в рантаймі. " +
                "Запусти Tools > Highway Lords > Campaign > Підготувати префаби бою.");
        }

        return unitGO.AddComponent<CampaignUnit>();
    }

    private bool warnedAboutMissingCampaignUnit = false;

    public void ShowDamagePopup(Vector3 pos, int dmg)
    {
        if (damagePopupPrefab != null)
        {
            Vector3 spawnPos = new Vector3(pos.x, pos.y, -5f);
            GameObject popup = PoolRegistry.Spawn(damagePopupPrefab, spawnPos, Quaternion.identity);
            popup.GetComponent<DamagePopup>()?.Setup(dmg, false);
            MeshRenderer mr = popup.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 100;
        }
    }

    void UpdateHUDText()
    {
        int playerAlive = 0; int enemyAlive = 0;
        foreach (var u in allUnitsOnField) { if (u != null && !u.isDead) { if (u.isEnemy) enemyAlive++; else playerAlive++; } }
        if (enemyCountText) enemyCountText.text = $"Enemies: {enemyAlive} (Wave)";
        if (armyCountText) armyCountText.text = $"Your Army: {playerAlive}";
    }

    void CheckBattleVictoryCondition()
    {
        int playerAlive = 0; int enemyAlive = 0;
        foreach (var u in allUnitsOnField) { if (u != null && !u.isDead) { if (u.isEnemy) enemyAlive++; else playerAlive++; } }

        if (enemyAlive == 0 && enemiesSpawned >= totalEnemies) StartCoroutine(VictorySlowMotion());
        else if (playerAlive == 0 && currentState == BattleState.Battle) StartCoroutine(DefeatSequence());
    }

    IEnumerator VictorySlowMotion()
    {
        currentState = BattleState.GameOver;
        if (retreatButton) retreatButton.gameObject.SetActive(false); 

        Time.timeScale = 0.3f;
        if (SoundManager.Instance != null && SoundManager.Instance.victoryMusicStinger != null) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.victoryMusicStinger);
        
        yield return new WaitForSecondsRealtime(1.8f);
        
        EndBattle(true);
    }

    IEnumerator DefeatSequence()
    {
        currentState = BattleState.GameOver;
        if (retreatButton) retreatButton.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.5f);
        EndBattle(false);
    }

    void Retreat()
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        if (retreatButton) retreatButton.gameObject.SetActive(false); 
        StartCoroutine(RetreatRoutine());
    }

    IEnumerator RetreatRoutine()
    {
        currentState = BattleState.Retreating;
        if (SoundManager.Instance && warHornSound) audioSrc.PlayOneShot(warHornSound, 0.8f);
        yield return new WaitForSeconds(5.0f);
        EndBattle(false);
    }

    void EndBattle(bool isVictory)
    {
        if (currentState == BattleState.GameOver && Time.timeScale == 0) return; 
        currentState = BattleState.GameOver;

        if (CrossSceneData.useMercKnights) DecreaseMercenaryContracts("Knight");
        if (CrossSceneData.useMercArchers) DecreaseMercenaryContracts("Archer");
        if (CrossSceneData.useMercSpearmen) DecreaseMercenaryContracts("Spearman");
        if (CrossSceneData.useMercCavalry) DecreaseMercenaryContracts("Cavalry");

        CrossSceneData.useMercKnights = false;
        CrossSceneData.useMercArchers = false;
        CrossSceneData.useMercSpearmen = false;
        CrossSceneData.useMercCavalry = false;

        int k_left = 0, a_left = 0, s_left = 0, c_left = 0;
        
        foreach (var u in allUnitsOnField)
        {
            if (u != null && !u.isDead && !u.isEnemy)
            {
                if (u.unitClass == "Knight") k_left++;
                else if (u.unitClass == "Archer") a_left++;
                else if (u.unitClass == "Spearman") s_left++;
                else if (u.unitClass == "Cavalry") c_left++;
            }
        }

        int nextUncalledSlot = (currentLegion + 1) * 3;
        for (int i = nextUncalledSlot; i < 9 && CrossSceneData.squadSlots != null && i < CrossSceneData.squadSlots.Length; i++)
        {
            int uType = CrossSceneData.squadSlots[i];
            if (uType == 0) k_left += 5;
            else if (uType == 1) a_left += 5;
            else if (uType == 2) s_left += 5;
            else if (uType == 3) c_left += 5;
        }
        
        PlayerPrefs.SetInt("FreeKnights", PlayerPrefs.GetInt("FreeKnights", 0) + k_left);
        PlayerPrefs.SetInt("FreeArchers", PlayerPrefs.GetInt("FreeArchers", 0) + a_left);
        PlayerPrefs.SetInt("FreeSpearmen", PlayerPrefs.GetInt("FreeSpearmen", 0) + s_left);
        PlayerPrefs.SetInt("FreeCavalry", PlayerPrefs.GetInt("FreeCavalry", 0) + c_left);
        
        if (isVictory)
        {
            PlayerPrefs.SetInt("SavedGold", PlayerPrefs.GetInt("SavedGold", 100) + CrossSceneData.rewardGold);
            PlayerPrefs.SetInt("SavedWood", PlayerPrefs.GetInt("SavedWood", 0) + CrossSceneData.rewardWood);
            PlayerPrefs.SetInt("SavedStone", PlayerPrefs.GetInt("SavedStone", 0) + CrossSceneData.rewardStone);
            PlayerPrefs.SetInt("Camp_" + CrossSceneData.campId + "_Conquered", 1);
            PlayerPrefs.SetInt("PlayerGlory", PlayerPrefs.GetInt("PlayerGlory", 0) + (CrossSceneData.campLevel * 50));
            PlayerPrefs.SetInt("CitiesConquered", 1);
        }
        PlayerPrefs.Save(); 
        PlayerPrefs.Save();

        CrossSceneData.knightsCount = k_left;
        CrossSceneData.archersCount = a_left;
        CrossSceneData.spearmenCount = s_left;
        CrossSceneData.cavalryCount = c_left;
        CrossSceneData.isReturningFromBattle = true;
        CrossSceneData.lastBattleWon = isVictory;

        StartCoroutine(ShowResultAndLeave(isVictory));
    }

    void DecreaseMercenaryContracts(string mClass)
    {
        int battlesLeft = PlayerPrefs.GetInt("Merc_" + mClass + "_Battles", 0);
        if (battlesLeft > 0)
        {
            PlayerPrefs.SetInt("Merc_" + mClass + "_Battles", battlesLeft - 1);
        }
    }

    IEnumerator ShowResultAndLeave(bool isVictory)
    {
        AnimatedBattleResult resultPanel = AnimatedBattleResult.Instance;
        if (resultPanel == null)
        {
            AnimatedBattleResult[] allPanels = Resources.FindObjectsOfTypeAll<AnimatedBattleResult>();
            foreach (var p in allPanels) { if (p.gameObject.scene.name != null) { resultPanel = p; break; } }
        }

        if (resultPanel != null)
        {
            resultPanel.gameObject.SetActive(true);
            resultPanel.ShowResult(isVictory, CrossSceneData.rewardGold, CrossSceneData.rewardWood, CrossSceneData.rewardStone);
        }
        else
        {
            yield return new WaitForSecondsRealtime(2f);
            LoadMainScene();
            yield break;
        }

        if (!isVictory)
        {
            yield return new WaitForSecondsRealtime(3.5f);
            if (resultPanel != null) resultPanel.gameObject.SetActive(false); 
            LoadMainScene();
        }
    }

    public void LoadMainScene()
    {
        Time.timeScale = 1f; 
        if (SoundManager.Instance != null) SoundManager.Instance.PlayIdleMusic();
        
        LoadingManager lm = Object.FindFirstObjectByType<LoadingManager>(FindObjectsInactive.Include);
        if (lm != null) { lm.gameObject.SetActive(true); lm.LoadScene("Main"); }
        else SceneManager.LoadScene("Main");
    }

    public void OnContinueButtonClicked()
    {
        if (SoundManager.Instance != null && SoundManager.Instance.clickSound != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound, 1.5f);

        if (AnimatedBattleResult.Instance != null) 
            AnimatedBattleResult.Instance.gameObject.SetActive(false);

        LoadMainScene();
    }
}