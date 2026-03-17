using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    public enum BattleState { Intro, March, TacticalPause, Battle, Retreating, GameOver }
    public BattleState currentState = BattleState.Intro;

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
    public TMP_Text[] rowAssignmentTexts; 

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

        if (GameManager.Instance == null)
        {
            GameObject gmObj = new GameObject("Temp_GameManager");
            gmObj.AddComponent<GameManager>(); 
        }
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

        if (GameManager.Instance != null)
        {
            isVolleyUnlocked = GameManager.Instance.metaVolleyBarrage > 0;
            isWarCryUnlocked = GameManager.Instance.knightLevel >= 2 || GameManager.Instance.metaFortifiedWalls > 0; 
        }

        if (CrossSceneData.knightsCount > 0) activeClasses.Add("Knight");
        if (CrossSceneData.spearmenCount > 0) activeClasses.Add("Spearman");
        if (CrossSceneData.archersCount > 0) activeClasses.Add("Archer");
        if (CrossSceneData.cavalryCount > 0) activeClasses.Add("Cavalry");

        activeLineCount = activeClasses.Count;
        for(int i = 0; i < 4; i++) rowAssignments[i] = (i < activeLineCount) ? activeClasses[i] : "";

        SetupSkillUI();
        DisableEmptyTacticalButtons();
        UpdateRowUI(); 

        Vector3 offScreenStart = playerSpawnPoint.position + new Vector3(-15f, 0, 0);
        Camera.main.transform.position = new Vector3(offScreenStart.x + 5f, Camera.main.transform.position.y, Camera.main.transform.position.z);

        SpawnPlayerArmy(offScreenStart);
        CalculateEnemyPool();
        SpawnEnemyWave(true); 
        
        if (campDefenseProgressBarFill) campDefenseProgressBarFill.fillAmount = 1f;

        StartCoroutine(MarchCutscene(offScreenStart));
    }

    void DisableEmptyTacticalButtons()
    {
        if (tacticalUnitButtons == null || tacticalUnitButtons.Length < 4) return;
        tacticalUnitButtons[0].interactable = CrossSceneData.knightsCount > 0;
        tacticalUnitButtons[1].interactable = CrossSceneData.archersCount > 0;
        tacticalUnitButtons[2].interactable = CrossSceneData.spearmenCount > 0;
        tacticalUnitButtons[3].interactable = CrossSceneData.cavalryCount > 0;
    }

    void UpdateRowUI()
    {
        if (rowAssignmentTexts == null) return;
        for (int i = 0; i < 4; i++)
        {
            if (rowAssignmentTexts[i] == null) continue;
            Button rowBtn = rowAssignmentTexts[i].transform.parent.GetComponent<Button>();
            if (rowBtn == null) continue;

            if (i >= activeLineCount) rowBtn.gameObject.SetActive(false); 
            else
            {
                rowBtn.gameObject.SetActive(true);
                rowAssignmentTexts[i].text = $"Line {i + 1}: {GetClassName(rowAssignments[i])}";

                if (string.IsNullOrEmpty(highlightedClass)) rowBtn.interactable = false; 
                else rowBtn.interactable = (rowAssignments[i] != highlightedClass);
            }
        }
    }

    void SetupSkillUI()
    {
        if (volleyLockIcon) volleyLockIcon.SetActive(!isVolleyUnlocked);
        if (warCryLockIcon) warCryLockIcon.SetActive(!isWarCryUnlocked);

        if (volleyButton) { volleyButton.interactable = isVolleyUnlocked; volleyButton.onClick.AddListener(OnVolleyClicked); }
        if (warCryButton) { warCryButton.interactable = isWarCryUnlocked; warCryButton.onClick.AddListener(OnWarCryClicked); }

        if (volleyCooldownFill) volleyCooldownFill.fillAmount = 0f;
        if (warCryCooldownFill) warCryCooldownFill.fillAmount = 0f;
    }

    public bool IsBattleActive() { return currentState == BattleState.Battle; }
    public List<CampaignUnit> GetAllUnits() { return allUnitsOnField; }

    IEnumerator MarchCutscene(Vector3 startOffset)
    {
        currentState = BattleState.March;

        if (warHornSound && audioSrc) audioSrc.PlayOneShot(warHornSound, 1f);
        yield return new WaitForSeconds(1.0f);

        if (marchingSound && audioSrc)
        {
            audioSrc.clip = marchingSound;
            audioSrc.loop = true;
            audioSrc.Play();
        }

        Camera mainCam = Camera.main;
        Vector3 camStart = new Vector3(startOffset.x + 5f, mainCam.transform.position.y, mainCam.transform.position.z);
        Vector3 camMid = new Vector3(playerSpawnPoint.position.x, mainCam.transform.position.y, mainCam.transform.position.z); 
        Vector3 camEnemy = new Vector3(enemySpawnPoint.position.x - 2f, mainCam.transform.position.y, mainCam.transform.position.z); 
        
        mainCam.transform.position = camStart;

        float elapsed = 0f;
        while (elapsed < 3.0f)
        {
            elapsed += Time.deltaTime;
            mainCam.transform.position = Vector3.Lerp(camStart, camMid, Mathf.SmoothStep(0f, 1f, elapsed / 3.0f));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 1.5f)
        {
            elapsed += Time.deltaTime;
            mainCam.transform.position = Vector3.Lerp(camMid, camEnemy, Mathf.SmoothStep(0f, 1f, elapsed / 1.5f));
            yield return null;
        }
        
        yield return new WaitForSeconds(0.5f); 

        elapsed = 0f;
        while (elapsed < 1.5f)
        {
            elapsed += Time.deltaTime;
            mainCam.transform.position = Vector3.Lerp(camEnemy, camMid, Mathf.SmoothStep(0f, 1f, elapsed / 1.5f));
            yield return null;
        }

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

        foreach (var u in allUnitsOnField)
        {
            if (!u.isEnemy) u.SetHighlight(u.unitClass == highlightedClass);
        }
        
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

        UpdateRowUI(); 
        UpdateFormationPositions(playerSpawnPoint.position.x, false);
    }

    void UpdateFormationPositions(float baseX, bool snapInstantly)
    {
        float currentXOffset = 0f;
        int unitsPerCol = 5; 
        float spacingX = 1.0f;
        float spacingY = 0.8f;

        for (int i = 0; i < activeLineCount; i++)
        {
            string uClass = rowAssignments[i];
            
            List<CampaignUnit> classUnits = allUnitsOnField.Where(u => !u.isEnemy && !u.isDead && u.unitClass == uClass).ToList();
            if (classUnits.Count == 0) continue;

            for (int j = 0; j < classUnits.Count; j++)
            {
                int col = j / unitsPerCol;
                int row = j % unitsPerCol;

                float targetX = baseX - currentXOffset - (col * spacingX);
                int currentRows = Mathf.Min(classUnits.Count - (col * unitsPerCol), unitsPerCol);
                if (classUnits.Count > unitsPerCol) currentRows = unitsPerCol; 
                
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
        
        foreach (var u in allUnitsOnField) if (!u.isEnemy) u.SetHighlight(false);
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
        isTargetingVolley = true;
        Time.timeScale = 0.2f; 
    }

    void HandleVolleyTargeting()
    {
        if (!isTargetingVolley) return;

        if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Vector3 targetPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            targetPos.z = 0;

            if (volleyEffectPrefab) Instantiate(volleyEffectPrefab, targetPos, Quaternion.identity);
            StartCoroutine(VolleyDamageRoutine(targetPos));

            Time.timeScale = 1f;
            isTargetingVolley = false;
            currentVolleyTimer = volleyCooldown; 
        }
        else if (Input.GetMouseButtonDown(1))
        {
            Time.timeScale = 1f;
            isTargetingVolley = false;
        }
    }

    IEnumerator VolleyDamageRoutine(Vector3 targetPos)
    {
        yield return new WaitForSeconds(0.6f);
        int damageToDeal = 150 + (GameManager.Instance != null ? GameManager.Instance.metaVolleyBarrage * 50 : 0);

        foreach (var u in allUnitsOnField)
        {
            if (u != null && !u.isDead && u.isEnemy)
            {
                if (Vector2.Distance(u.transform.position, targetPos) <= 3.5f) u.TakeDamage(damageToDeal);
            }
        }
    }

    void OnWarCryClicked()
    {
        if (currentWarCryTimer > 0 || !isWarCryUnlocked) return;
        if (SoundManager.Instance && warHornSound) audioSrc.PlayOneShot(warHornSound, 1f);
        
        foreach (var u in allUnitsOnField)
        {
            if (u != null && !u.isDead && !u.isEnemy) u.ApplyWarCry(5f);
        }
        currentWarCryTimer = warCryCooldown; 
    }

    void CalculateEnemyPool()
    {
        int campLvl = CrossSceneData.campLevel > 0 ? CrossSceneData.campLevel : 1;
        totalEnemies = (8 + (campLvl * 5)) + (campLvl * 3) + (campLvl * 2); 
        enemiesSpawned = 0;
        enemiesKilled = 0;
    }

    // --- ФІКС: ІДЕАЛЬНА ВОРОЖА СІТКА ---
    void SpawnEnemyWave(bool isVanguard)
    {
        int campLvl = CrossSceneData.campLevel > 0 ? CrossSceneData.campLevel : 1;
        
        int totalGuards = 4 + (campLvl * 3);
        int totalSpears = campLvl >= 2 ? 2 + (campLvl * 2) : 0;
        int totalArchers = campLvl >= 3 ? 1 + (campLvl * 2) : 0;
        int totalCavs = campLvl >= 4 ? (campLvl * 1) : 0;

        // Авангард це 60% армії (щоб вони виглядали епічно), решта - підкріплення
        int eGuards = isVanguard ? Mathf.CeilToInt(totalGuards * 0.6f) : totalGuards - Mathf.CeilToInt(totalGuards * 0.6f);
        int eSpearmen = isVanguard ? Mathf.CeilToInt(totalSpears * 0.6f) : totalSpears - Mathf.CeilToInt(totalSpears * 0.6f);
        int eArchers = isVanguard ? Mathf.CeilToInt(totalArchers * 0.6f) : totalArchers - Mathf.CeilToInt(totalArchers * 0.6f);
        int eCavalry = isVanguard ? Mathf.CeilToInt(totalCavs * 0.6f) : totalCavs - Mathf.CeilToInt(totalCavs * 0.6f);

        Vector3 spawnCenter = isVanguard ? enemySpawnPoint.position : enemySpawnPoint.position + new Vector3(15f, 0, 0);
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

        int hp = 100; int dmg = 10;
        bool isRanged = (unitClass == "Archer");
        float speed = 1.6f;
        if (unitClass == "Spearman") speed = 1.4f; 
        else if (unitClass == "Archer") speed = 1.1f;
        else if (unitClass == "Cavalry") speed = 2.5f;

        int campLvl = CrossSceneData.campLevel > 0 ? CrossSceneData.campLevel : 1;
        if (unitClass == "Knight") { hp = 100 + (campLvl * 40); dmg = 15 + (campLvl * 8); }
        else if (unitClass == "Archer") { hp = 70 + (campLvl * 25); dmg = 10 + (campLvl * 6); }
        else if (unitClass == "Spearman") { hp = 90 + (campLvl * 35); dmg = 12 + (campLvl * 7); }
        else if (unitClass == "Cavalry") { hp = 150 + (campLvl * 50); dmg = 25 + (campLvl * 10); }

        int rows = 5; 
        float spacingX = 1.0f; 
        float spacingY = 0.8f; 
        int colsUsed = Mathf.CeilToInt((float)count / rows);

        for (int i = 0; i < count; i++)
        {
            int col = i / rows;
            int row = i % rows;

            // Вороги будуються вправо від базової точки
            float targetX = basePos.x + startXOffset + (col * spacingX);
            int currentRows = Mathf.Min(count - (col * rows), rows);
            if (count > rows) currentRows = rows; 
            
            float targetY = basePos.y + (row * spacingY) - ((currentRows - 1) * spacingY / 2f);

            Vector3 spawnPos = new Vector3(targetX, targetY, 0);
            
            GameObject unitGO = Instantiate(prefab, spawnPos, Quaternion.identity);
            unitGO.tag = "Enemy"; 
            
            MonoBehaviour[] scripts = unitGO.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                if (script == null) continue;
                string sName = script.GetType().Name;
                if (sName.Contains("Enemy") || sName.Contains("UnitStats") || sName.Contains("Selector") || sName == "CampaignUnit") 
                {
                    DestroyImmediate(script); 
                }
            }

            CampaignUnit newAI = unitGO.AddComponent<CampaignUnit>();
            newAI.Setup(true, unitClass, hp, dmg, speed, isRanged, bloodHitPrefab, applyFlip);
            
            newAI.tacticalTargetPos = spawnPos;
            newAI.transform.position = spawnPos;
            allUnitsOnField.Add(newAI);
        }

        return startXOffset + (colsUsed * spacingX) + 1.2f;
    }

    void CheckEnemyWaves()
    {
        int aliveEnemies = allUnitsOnField.Count(u => u != null && !u.isDead && u.isEnemy);
        
        int totalKilledOrDead = totalEnemies - (enemiesSpawned - aliveEnemies) - (totalEnemies - enemiesSpawned);
        if (campDefenseProgressBarFill) campDefenseProgressBarFill.fillAmount = (float)(totalEnemies - enemiesKilled) / totalEnemies;

        if (aliveEnemies <= 3 && enemiesSpawned < totalEnemies)
        {
            if (SoundManager.Instance && warHornSound) audioSrc.PlayOneShot(warHornSound, 0.7f);
            SpawnEnemyWave(false);
        }
    }

    void SpawnPlayerArmy(Vector3 startPos)
    {
        for (int i = 0; i < activeLineCount; i++)
        {
            string uClass = rowAssignments[i];
            int count = 0; bool flip = false; GameObject prefab = null;

            if (uClass == "Knight") { count = CrossSceneData.knightsCount; prefab = playerKnightPrefab; flip = flipPlayerKnight; }
            else if (uClass == "Spearman") { count = CrossSceneData.spearmenCount; prefab = playerSpearmanPrefab; flip = flipPlayerSpearman; }
            else if (uClass == "Archer") { count = CrossSceneData.archersCount; prefab = playerArcherPrefab; flip = flipPlayerArcher; }
            else if (uClass == "Cavalry") { count = CrossSceneData.cavalryCount; prefab = playerCavalryPrefab; flip = flipPlayerCavalry; }

            SpawnGroup(prefab, count, startPos, uClass, flip, false);
        }
        
        UpdateFormationPositions(startPos.x, true); 
    }

    void SpawnGroup(GameObject prefab, int count, Vector3 startPos, string unitClass, bool applyFlip, bool isEnemy)
    {
        if (prefab == null || count <= 0) return;

        int hp = 100; int dmg = 10;
        bool isRanged = (unitClass == "Archer");
        float speed = 1.6f;
        if (unitClass == "Spearman") speed = 1.4f; 
        else if (unitClass == "Archer") speed = 1.1f;
        else if (unitClass == "Cavalry") speed = 2.5f;

        if (!isEnemy && GameManager.Instance != null)
        {
            if (unitClass == "Knight") { hp = 120 + (GameManager.Instance.knightLevel * 20); dmg = GameManager.Instance.GetKnightDamage(); }
            else if (unitClass == "Archer") { hp = 60 + (GameManager.Instance.archerLevel * 10); dmg = GameManager.Instance.GetArcherDamage(); }
            else if (unitClass == "Spearman") { hp = 90 + (GameManager.Instance.spearmanLevel * 15); dmg = GameManager.Instance.GetSpearmanDamage(); }
            else if (unitClass == "Cavalry") { hp = 150 + (GameManager.Instance.cavalryLevel * 25); dmg = GameManager.Instance.GetCavalryDamage(); }
        }

        for (int i = 0; i < count; i++)
        {
            GameObject unitGO = Instantiate(prefab, startPos, Quaternion.identity);
            unitGO.tag = "PlayerUnit"; 
            
            MonoBehaviour[] scripts = unitGO.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                if (script == null) continue;
                string sName = script.GetType().Name;
                if (sName.Contains("Enemy") || sName.Contains("UnitStats") || sName.Contains("Selector") || sName.Contains("Knight") || sName.Contains("Archer") || sName.Contains("Spearman") || sName.Contains("Cavalry") || sName == "CampaignUnit") 
                {
                    DestroyImmediate(script); 
                }
            }

            CampaignUnit newAI = unitGO.AddComponent<CampaignUnit>();
            newAI.Setup(false, unitClass, hp, dmg, speed, isRanged, bloodHitPrefab, applyFlip);
            
            allUnitsOnField.Add(newAI);
        }
    }

    public void ShowDamagePopup(Vector3 pos, int dmg)
    {
        if (damagePopupPrefab != null)
        {
            Vector3 spawnPos = new Vector3(pos.x, pos.y, -5f);
            GameObject popup = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
            popup.GetComponent<DamagePopup>()?.Setup(dmg, false);
            MeshRenderer mr = popup.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 100;
        }
    }

    void UpdateHUDText()
    {
        int playerAlive = 0;
        int enemyAlive = 0;
        foreach (var u in allUnitsOnField)
        {
            if (u != null && !u.isDead)
            {
                if (u.isEnemy) enemyAlive++;
                else playerAlive++;
            }
        }
        if (enemyCountText) enemyCountText.text = $"Enemies: {enemyAlive} (Wave)";
        if (armyCountText) armyCountText.text = $"Your Army: {playerAlive}";
    }

    void CheckBattleVictoryCondition()
    {
        int playerAlive = 0;
        int enemyAlive = 0;
        foreach (var u in allUnitsOnField)
        {
            if (u != null && !u.isDead)
            {
                if (u.isEnemy) enemyAlive++;
                else playerAlive++;
            }
        }

        // ПЕРЕМОГА
        if (enemyAlive == 0 && enemiesSpawned >= totalEnemies) 
        {
            StartCoroutine(VictorySlowMotion());
        }
        // ПОРАЗКА
        else if (playerAlive == 0 && currentState == BattleState.Battle) 
        {
            StartCoroutine(DefeatSequence()); // Викликаємо нову послідовність поразки
        }
    }

    IEnumerator DefeatSequence()
    {
        currentState = BattleState.GameOver;
        if (retreatButton) retreatButton.gameObject.SetActive(false);
        
        yield return new WaitForSeconds(1.5f); // Пауза, щоб усвідомити поразку
        EndBattle(false);
    }

    IEnumerator VictorySlowMotion()
    {
        currentState = BattleState.GameOver;
        if (retreatButton) retreatButton.gameObject.SetActive(false); 
        Time.timeScale = 0.3f;
        if (SoundManager.Instance != null && SoundManager.Instance.victoryMusicStinger != null) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.victoryMusicStinger);
        yield return new WaitForSecondsRealtime(1.8f);
        Time.timeScale = 1f; 
        EndBattle(true);
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
        
        // Даємо солдатам час втекти
        yield return new WaitForSeconds(5.0f);
        
        // Відступ — це завжди НЕ перемога (false)
        EndBattle(false); 
    }

    void EndBattle(bool isVictory)
    {
        // 1. Рахуємо тих, хто вижив для резерву
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

        // 2. Записуємо результат у крос-сценні дані
        CrossSceneData.knightsCount = k_left;
        CrossSceneData.archersCount = a_left;
        CrossSceneData.spearmenCount = s_left;
        CrossSceneData.cavalryCount = c_left;
        CrossSceneData.isReturningFromBattle = true;
        CrossSceneData.lastBattleWon = isVictory;

        // Якщо це поразка або відступ, нагорода = 0
        if (!isVictory)
        {
            CrossSceneData.rewardGold = 0;
            CrossSceneData.rewardWood = 0;
            CrossSceneData.rewardStone = 0;
        }

        // 3. Якщо перемога — нараховуємо ресурси в PlayerPrefs
        if (isVictory)
        {
            int newGold = PlayerPrefs.GetInt("SavedGold", 100) + CrossSceneData.rewardGold;
            int newWood = PlayerPrefs.GetInt("SavedWood", 0) + CrossSceneData.rewardWood;
            int newStone = PlayerPrefs.GetInt("SavedStone", 0) + CrossSceneData.rewardStone;
            PlayerPrefs.SetInt("SavedGold", newGold);
            PlayerPrefs.SetInt("SavedWood", newWood);
            PlayerPrefs.SetInt("SavedStone", newStone);
            PlayerPrefs.SetInt("Camp_" + CrossSceneData.campId + "_Conquered", 1);
            PlayerPrefs.Save();
        }

        // 4. Миттєво йдемо в завантаження (результат покажемо вже ТАМ)
        if (SoundManager.Instance != null) SoundManager.Instance.PlayIdleMusic();
        
        LoadingManager lm = LoadingManager.Instance;
        if (lm != null) lm.LoadScene("Main");
        else SceneManager.LoadScene("Main");
    }

    IEnumerator ShowResultAndLeave(bool isVictory)
    {
        // 1. Викликаємо твою анімовану панель
        // Переконайся, що скрипт AnimatedBattleResult є на сцені!
        if (AnimatedBattleResult.Instance != null)
        {
            AnimatedBattleResult.Instance.ShowResult(
                isVictory, 
                CrossSceneData.rewardGold, 
                CrossSceneData.rewardWood, 
                CrossSceneData.rewardStone
            );
        }

        // 2. Чекаємо, поки гравець подивиться на результат (наприклад, 4 секунди)
        yield return new WaitForSecondsRealtime(4.0f);

        // 3. Тільки тепер запускаємо екран завантаження
        if (SoundManager.Instance != null) SoundManager.Instance.PlayIdleMusic();
        
        LoadingManager lm = LoadingManager.Instance;
        if (lm != null) lm.LoadScene("Main");
        else SceneManager.LoadScene("Main");
    }
}