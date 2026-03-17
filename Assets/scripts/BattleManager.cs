using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("UI Битви (HUD)")]
    public TMP_Text locationNameText;
    public TMP_Text enemyCountText;
    public TMP_Text armyCountText;

    [Header("Управління Битвой")]
    public Button attackButton;
    public Button retreatButton;

    [Header("Пауза та Налаштування")]
    public GameObject settingsPanel;
    public Button openSettingsButton;
    public Button closeSettingsButton;
    public Button surrenderButton; 

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

    [Header("Точки Спавну")]
    public Transform playerSpawnPoint; 
    public Transform enemySpawnPoint;  

    [Header("Звуки Катсцени")]
    public AudioClip warHornSound;
    public AudioClip marchingSound;
    private AudioSource audioSrc;

    private bool battleIsActive = false;
    private List<GameObject> spawnedAllies = new List<GameObject>();
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (GameManager.Instance == null)
        {
            GameObject gmObj = new GameObject("Temp_GameManager");
            gmObj.AddComponent<GameManager>(); 
        }

        if (GameManager.Instance.engagementLine == null)
        {
            GameObject dummyLine = new GameObject("DummyEngagementLine");
            dummyLine.transform.position = Vector3.zero; 
            GameManager.Instance.engagementLine = dummyLine.transform;
        }
        if (GameManager.Instance.castle == null)
        {
            GameObject dummyCastle = new GameObject("DummyCastle");
            dummyCastle.transform.position = new Vector3(-30f, 0, 0); 
            Wall fakeWall = dummyCastle.AddComponent<Wall>();
            fakeWall.maxHealth = 999999;
            fakeWall.currentHealth = 999999;
            GameManager.Instance.castle = fakeWall;
        }
        
        GameObject dummyL = new GameObject("DummyL"); dummyL.transform.position = new Vector3(-20f, 0, 0);
        GameManager.Instance.leftBoundary = dummyL.transform;
        GameObject dummyR = new GameObject("DummyR"); dummyR.transform.position = new Vector3(20f, 0, 0);
        GameManager.Instance.rightBoundary = dummyR.transform;
    }

    void Start()
    {
        if (locationNameText) locationNameText.text = "BATTLE: " + CrossSceneData.campName.ToUpper();
        if (enemyCountText) enemyCountText.text = "<color=#FF4444>Enemies: Waiting</color>";
        if (armyCountText) armyCountText.text = "<color=#44FF44>Your Army: Ready</color>";

        attackButton.onClick.AddListener(StartClash);
        retreatButton.onClick.AddListener(Retreat);
        
        if (openSettingsButton) openSettingsButton.onClick.AddListener(ToggleSettings);
        if (closeSettingsButton) closeSettingsButton.onClick.AddListener(ToggleSettings);
        if (surrenderButton) surrenderButton.onClick.AddListener(SurrenderBattle);

        if (settingsPanel) settingsPanel.SetActive(false);

        audioSrc = gameObject.AddComponent<AudioSource>();

        if (SoundManager.Instance != null) SoundManager.Instance.PlayBattleMusic();

        SpawnArmies();
        StartCoroutine(IntroCutscene());
    }

    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            bool isOpening = !settingsPanel.activeSelf;
            settingsPanel.SetActive(isOpening);
            Time.timeScale = isOpening ? 0f : 1f;
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        }
    }

    public void SurrenderBattle()
    {
        Time.timeScale = 1f; 
        EndBattle(false); 
    }

    IEnumerator IntroCutscene()
    {
        attackButton.gameObject.SetActive(false);
        retreatButton.gameObject.SetActive(false);
        if (openSettingsButton) openSettingsButton.gameObject.SetActive(false);

        float startOffset = -18f; 
        float marchDuration = 6.0f; 

        Dictionary<GameObject, Vector3> finalPositions = new Dictionary<GameObject, Vector3>();
        foreach (var ally in spawnedAllies)
        {
            if (ally != null)
            {
                finalPositions[ally] = ally.transform.position;
                ally.transform.position += new Vector3(startOffset, 0, 0); 
                
                Animator anim = ally.GetComponent<Animator>();
                if (anim) anim.SetBool("IsMoving", true);
            }
        }

        Camera mainCam = Camera.main;
        Vector3 camEndPos = mainCam.transform.position; 
        Vector3 camStartPos = camEndPos + new Vector3(startOffset + 6f, 0, 0); 
        mainCam.transform.position = camStartPos;

        if (warHornSound) audioSrc.PlayOneShot(warHornSound, 1f);
        yield return new WaitForSeconds(1.5f);

        if (marchingSound)
        {
            audioSrc.clip = marchingSound;
            audioSrc.loop = true;
            audioSrc.Play();
        }

        float elapsed = 0f;
        while (elapsed < marchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / marchDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            foreach (var kvp in finalPositions)
            {
                if (kvp.Key != null) kvp.Key.transform.position = Vector3.Lerp(kvp.Value + new Vector3(startOffset, 0, 0), kvp.Value, smoothT);
            }

            float camT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.15f));
            mainCam.transform.position = Vector3.Lerp(camStartPos, camEndPos, camT);
            yield return null; 
        }

        foreach (var kvp in finalPositions)
        {
            if (kvp.Key != null)
            {
                kvp.Key.transform.position = kvp.Value;
                Animator anim = kvp.Key.GetComponent<Animator>();
                if (anim) anim.SetBool("IsMoving", false);
            }
        }
        mainCam.transform.position = camEndPos; 

        if (marchingSound) audioSrc.Stop();
        
        attackButton.gameObject.SetActive(true);
        retreatButton.gameObject.SetActive(true);
        if (openSettingsButton) openSettingsButton.gameObject.SetActive(true);
    }

    void SpawnArmies()
    {
        SpawnGroup(playerKnightPrefab, CrossSceneData.knightsCount, playerSpawnPoint.position, false);
        SpawnGroup(playerSpearmanPrefab, CrossSceneData.spearmenCount, playerSpawnPoint.position + new Vector3(-1.5f, 0, 0), false);
        SpawnGroup(playerArcherPrefab, CrossSceneData.archersCount, playerSpawnPoint.position + new Vector3(-3.0f, 0, 0), false);
        SpawnGroup(playerCavalryPrefab, CrossSceneData.cavalryCount, playerSpawnPoint.position + new Vector3(-4.5f, 0, 0), false);

        SpawnGroup(enemyGuardPrefab, CrossSceneData.enemyGuards, enemySpawnPoint.position, true);
        SpawnGroup(enemySpearmanPrefab, CrossSceneData.enemySpearmen, enemySpawnPoint.position + new Vector3(1.5f, 0, 0), true);
        SpawnGroup(enemyArcherPrefab, CrossSceneData.enemyArchers, enemySpawnPoint.position + new Vector3(3.0f, 0, 0), true);
        SpawnGroup(enemyCavalryPrefab, CrossSceneData.enemyCavalry, enemySpawnPoint.position + new Vector3(4.5f, 0, 0), true);
    }

    void SpawnGroup(GameObject prefab, int count, Vector3 startPos, bool isEnemy)
    {
        if (prefab == null || count <= 0) return;

        int rows = 5; 
        float spacingX = 0.9f; 
        float spacingY = 0.8f; 

        for (int i = 0; i < count; i++)
        {
            int col = i / rows;
            int row = i % rows;

            float xOffset = isEnemy ? (col * spacingX) : -(col * spacingX);
            float yOffset = (row * spacingY) - ((Mathf.Min(count, rows) - 1) * spacingY / 2f);

            Vector3 spawnPos = startPos + new Vector3(xOffset, yOffset, 0);
            spawnPos.x += UnityEngine.Random.Range(-0.15f, 0.15f);
            spawnPos.y += UnityEngine.Random.Range(-0.15f, 0.15f);

            GameObject unitGO = Instantiate(prefab, spawnPos, Quaternion.identity);
            
            // ФІКС УРОНУ ЛУЧНИКІВ: Тег вішається ТІЛЬКИ на головний об'єкт
            unitGO.tag = isEnemy ? "Enemy" : "PlayerUnit"; 

            if (isEnemy)
            {
                Vector3 scale = unitGO.transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                unitGO.transform.localScale = scale;
            }
            else
            {
                Vector3 scale = unitGO.transform.localScale;
                scale.x = Mathf.Abs(scale.x);
                unitGO.transform.localScale = scale;
            }

            MonoBehaviour[] scripts = unitGO.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                if (script == null) continue;
                string sName = script.GetType().Name;
                if (sName == "Animator" || sName == "SpriteRenderer" || sName == "Canvas" || sName == "Image" || sName == "AudioSource" || sName == "MovementVFX") continue; 
                
                script.enabled = false;
            }

            if (isEnemy) spawnedEnemies.Add(unitGO);
            else spawnedAllies.Add(unitGO);
        }
    }

    void StartClash()
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        StartCoroutine(AnimateButtonsAndStartBattle());
    }

    IEnumerator AnimateButtonsAndStartBattle()
    {
        RectTransform attackRect = attackButton.GetComponent<RectTransform>();
        RectTransform retreatRect = retreatButton.GetComponent<RectTransform>();

        Vector2 startPosA = attackRect.anchoredPosition;
        Vector2 startPosR = retreatRect.anchoredPosition;

        float t = 0;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            attackRect.anchoredPosition = startPosA + new Vector2(0, t * 200f);
            retreatRect.anchoredPosition = startPosR + new Vector2(0, t * 200f);
            yield return null;
        }

        t = 0;
        Vector2 topPosA = attackRect.anchoredPosition;
        Vector2 topPosR = retreatRect.anchoredPosition;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            attackRect.anchoredPosition = topPosA - new Vector2(0, t * 3000f);
            retreatRect.anchoredPosition = topPosR - new Vector2(0, t * 3000f);
            yield return null;
        }

        attackButton.gameObject.SetActive(false);
        retreatButton.gameObject.SetActive(false);
        
        battleIsActive = true;

        foreach (var ally in spawnedAllies) if (ally != null) EnableUnitLogic(ally);
        foreach (var enemy in spawnedEnemies) if (enemy != null) EnableUnitLogic(enemy);
    }

    void EnableUnitLogic(GameObject unit)
    {
        // 1. Вмикаємо всі скрипти на об'єкті
        MonoBehaviour[] scripts = unit.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script == null) continue;
            script.enabled = true;
        }

        // 2. ЖОРСТКИЙ ЗАПУСК ЛОГІКИ
        // Оскільки юніти могли "заснути" під час катсцени, ми примусово 
        // викликаємо їхні методи пошуку цілей.
        
        // Для твоїх лицарів/піхоти:
        unit.SendMessage("SetStateMovingToEngagementLine", SendMessageOptions.DontRequireReceiver);
        
        // Для всіх (і наших, і ворогів):
        unit.SendMessage("FindNextTarget", SendMessageOptions.DontRequireReceiver);
        
        // Якщо у юніта є логіка вибору цілі через GameManager:
        if (unit.CompareTag("PlayerUnit"))
        {
            unit.SendMessage("ScanForEnemies", SendMessageOptions.DontRequireReceiver);
        }
    }

    void Update()
    {
        if (!battleIsActive) return;
        
        UpdateHUDText();
        if (Time.frameCount % 30 == 0) CheckBattleVictoryCondition();
    }

    void UpdateHUDText()
    {
        int playerAlive = 0;
        int enemyAlive = 0;

        foreach (var ally in spawnedAllies)
            if (ally != null && !ally.CompareTag("Untagged")) playerAlive++;

        foreach (var enemy in spawnedEnemies)
            if (enemy != null && !enemy.CompareTag("Untagged")) enemyAlive++;

        if (enemyCountText) enemyCountText.text = "<color=#FF4444>Enemies: " + enemyAlive + "</color>";
        if (armyCountText) armyCountText.text = "<color=#44FF44>Your Army: " + playerAlive + "</color>";
    }

    void CheckBattleVictoryCondition()
    {
        int playerAlive = 0;
        int enemyAlive = 0;

        foreach (var ally in spawnedAllies)
            if (ally != null && !ally.CompareTag("Untagged")) playerAlive++;

        foreach (var enemy in spawnedEnemies)
            if (enemy != null && !enemy.CompareTag("Untagged")) enemyAlive++;

        if (enemyAlive == 0 && battleIsActive && spawnedEnemies.Count > 0) 
        {
            StartCoroutine(VictorySlowMotion());
        }
        else if (playerAlive == 0 && battleIsActive && spawnedAllies.Count > 0) 
        {
            EndBattle(false);
        }
    }

    IEnumerator VictorySlowMotion()
    {
        battleIsActive = false; 
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
        EndBattle(false);
    }

    void EndBattle(bool isVictory)
    {
        battleIsActive = false;
        Time.timeScale = 1f; 

        SaveSurvivorsToData(); 

        CrossSceneData.isReturningFromBattle = true;
        CrossSceneData.lastBattleWon = isVictory;

        if (isVictory)
        {
            PlayerPrefs.SetInt("Camp_" + CrossSceneData.campId + "_Conquered", 1);
            PlayerPrefs.Save();
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayIdleMusic();

        SceneManager.LoadScene("Main"); 
    }

    void SaveSurvivorsToData()
    {
        int k_left = 0, a_left = 0, s_left = 0, c_left = 0;

        foreach (var ally in spawnedAllies)
        {
            if (ally != null && !ally.CompareTag("Untagged"))
            {
                if (ally.name.Contains(playerKnightPrefab.name)) k_left++;
                else if (ally.name.Contains(playerArcherPrefab.name)) a_left++;
                else if (ally.name.Contains(playerSpearmanPrefab.name)) s_left++;
                else if (ally.name.Contains(playerCavalryPrefab.name)) c_left++;
            }
        }

        CrossSceneData.knightsCount = k_left;
        CrossSceneData.archersCount = a_left;
        CrossSceneData.spearmenCount = s_left;
        CrossSceneData.cavalryCount = c_left;
    }
}