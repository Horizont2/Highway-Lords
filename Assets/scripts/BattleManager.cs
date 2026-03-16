using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    [Header("UI")]
    public Button attackButton;
    public Button retreatButton;

    [Header("Префаби Гравця (ТВОЇ)")]
    public GameObject playerKnightPrefab; // Сюди кидай префаб зі скриптом Knight
    public GameObject playerArcherPrefab; // Зі скриптом Archer
    public GameObject playerSpearmanPrefab; // Зі скриптом Spearman
    public GameObject playerCavalryPrefab; // Зі скриптом Cavalry

    [Header("Префаби Ворога (ТВОЇ)")]
    public GameObject enemyGuardPrefab; // Зі скриптом Guard
    public GameObject enemyArcherPrefab; // Зі скриптом EnemyArcher
    public GameObject enemySpearmanPrefab; // Зі скриптом EnemySpearman
    public GameObject enemyCavalryPrefab; // Зі скриптом EnemyHorse

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

    void Start()
    {
        attackButton.onClick.AddListener(StartClash);
        retreatButton.onClick.AddListener(Retreat);
        
        audioSrc = gameObject.AddComponent<AudioSource>();

        // Перевіряємо, чи є SoundManager і вмикаємо бойову музику
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBattleMusic();
        }

        SpawnArmies();

        // ЗАПУСКАЄМО КІНЕМАТОГРАФІЧНУ КАТСЦЕНУ З КАМЕРОЮ
        StartCoroutine(IntroCutscene());
    }

    IEnumerator IntroCutscene()
    {
        attackButton.gameObject.SetActive(false);
        retreatButton.gameObject.SetActive(false);

        float startOffset = -18f; 
        float marchDuration = 8.0f; 

        // Відкидаємо тільки НАШИХ союзників назад
        Dictionary<GameObject, Vector3> finalPositions = new Dictionary<GameObject, Vector3>();
        foreach (var ally in spawnedAllies)
        {
            if (ally != null)
            {
                finalPositions[ally] = ally.transform.position;
                ally.transform.position += new Vector3(startOffset, 0, 0); 
                
                // Вмикаємо анімацію бігу (в твоїх скриптах це "IsMoving")
                Animator anim = ally.GetComponent<Animator>();
                if (anim) anim.SetBool("IsMoving", true);
            }
        }

        Camera mainCam = Camera.main;
        Vector3 camEndPos = mainCam.transform.position; 
        Vector3 camStartPos = camEndPos + new Vector3(startOffset + 6f, 0, 0); 
        mainCam.transform.position = camStartPos;

        if (warHornSound) audioSrc.PlayOneShot(warHornSound, 1f);
        yield return new WaitForSeconds(2.0f);

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
                if (kvp.Key != null)
                {
                    kvp.Key.transform.position = Vector3.Lerp(kvp.Value + new Vector3(startOffset, 0, 0), kvp.Value, smoothT);
                }
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
    }

    void SpawnArmies()
    {
        // СПАВН НАШИХ
        SpawnGroup(playerKnightPrefab, CrossSceneData.knightsCount, playerSpawnPoint.position, false);
        SpawnGroup(playerSpearmanPrefab, CrossSceneData.spearmenCount, playerSpawnPoint.position + new Vector3(-1.5f, 0, 0), false);
        SpawnGroup(playerArcherPrefab, CrossSceneData.archersCount, playerSpawnPoint.position + new Vector3(-3.0f, 0, 0), false);
        SpawnGroup(playerCavalryPrefab, CrossSceneData.cavalryCount, playerSpawnPoint.position + new Vector3(-4.5f, 0, 0), false);

        // СПАВН ВОРОГІВ
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
            
            // Задаємо правильний тег для пошуку ворогів у ТВОЇХ скриптах
            unitGO.tag = isEnemy ? "Enemy" : "PlayerUnit"; 

            // Повертаємо ворогів обличчям вліво
            if (isEnemy)
            {
                Vector3 scale = unitGO.transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                unitGO.transform.localScale = scale;
            }

            // Вимикаємо скрипти логіки, поки не натиснули Attack
            // Вимикаємо скрипти логіки, щоб вони не почали битися під час маршу
            MonoBehaviour[] scripts = unitGO.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                if (script == null) continue;

                // Отримуємо ім'я типу скрипта
                string sName = script.GetType().Name;
                
                // Якщо це скрипт анімації, рендерер або сам BattleManager - НЕ вимикаємо їх
                if (sName == "Animator" || sName == "SpriteRenderer" || sName == "Canvas" || sName == "Image") 
                {
                    continue; 
                }

                script.enabled = false;
            }

            if (isEnemy) spawnedEnemies.Add(unitGO);
            else spawnedAllies.Add(unitGO);
        }
    }

    void StartClash()
    {
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

        // Вмикаємо всі бойові скрипти!
        foreach (var ally in spawnedAllies) if (ally != null) EnableUnitLogic(ally);
        foreach (var enemy in spawnedEnemies) if (enemy != null) EnableUnitLogic(enemy);
    }

    void EnableUnitLogic(GameObject unit)
    {
        MonoBehaviour[] scripts = unit.GetComponents<MonoBehaviour>();
        foreach (var script in scripts) script.enabled = true;
    }

    void Update()
    {
        if (!battleIsActive) return;
        
        // Перевіряємо кожну секунду, а не кожен кадр (для оптимізації)
        if (Time.frameCount % 60 == 0) CheckBattleStatus();
    }

    void CheckBattleStatus()
    {
        int playerAlive = 0;
        int enemyAlive = 0;

        foreach (var ally in spawnedAllies)
        {
            // Перевіряємо, чи юніт ще живий (тег Untagged ти ставиш при смерті)
            if (ally != null && !ally.CompareTag("Untagged")) playerAlive++;
        }

        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null && !enemy.CompareTag("Untagged")) enemyAlive++;
        }

        if (enemyAlive == 0) EndBattle(true);
        else if (playerAlive == 0) EndBattle(false);
    }

    void Retreat()
    {
        EndBattle(false);
    }

    void EndBattle(bool isVictory)
    {
        battleIsActive = false;
        SaveSurvivorsToData(); 

        CrossSceneData.isReturningFromBattle = true;
        CrossSceneData.lastBattleWon = isVictory;

        if (isVictory)
        {
            PlayerPrefs.SetInt("Camp_" + CrossSceneData.campId + "_Conquered", 1);
            PlayerPrefs.Save();
        }

        // Повертаємо спокійну музику перед виходом
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