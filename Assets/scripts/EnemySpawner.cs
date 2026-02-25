using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; 

[System.Serializable]
public class EnemyConfig
{
    public string name;           
    public GameObject prefab;     
    public Sprite icon;           
    public int unlockWave = 1;    
    public bool isBoss = false;   
    public bool isCart = false;   
    [Range(1, 100)] public int spawnWeight = 50; 
    public int baseGoldReward = 15; 
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Налаштування Всіх Ворогів")]
    public List<EnemyConfig> allEnemies = new List<EnemyConfig>();

    [Header("Бос-хвилі (Ескорт)")]
    public int bossEscortMin = 3;
    public int bossEscortMax = 5;

    [Header("Налаштування Хвилі")]
    public Transform[] spawnPoints;      
    public float fastSpawnDelay = 0.3f; // Затримка між юнітами всередині одного загону

    [Header("UI")]
    public WaveInfoPanel waveInfoPanel;  

    // Внутрішні змінні
    private List<EnemyConfig> currentWavePool = new List<EnemyConfig>(); 
    private EnemyConfig bossConfig;
    private EnemyConfig cartConfig; // Окремо зберігаємо конфг воза
    
    private int currentWaveNumber;
    private bool isBossWave = false;
    private bool isCartWave = false;
    private bool hasSpawnedCartThisWave = false; // Запобіжник для воза

    /// <summary>
    /// Готує пул ворогів для хвилі. Викликається з GameManager.NextWave()
    /// </summary>
    public void PrepareForWave(int waveNumber)
    {
        currentWaveNumber = waveNumber;
        isBossWave = (waveNumber % 10 == 0);
        
        // Віз виходить кожну 3-тю хвилю, окрім хвиль босів
        isCartWave = (!isBossWave && waveNumber % 3 == 0);
        hasSpawnedCartThisWave = false; // Скидаємо перед новою хвилею
        
        PrepareEnemyPool(waveNumber);
        UpdateWaveUI();
    }

    /// <summary>
    /// Головний метод для виклику з GameManager, коли прогрес-бар досягає мітки.
    /// </summary>
    public void SpawnSquad(bool forceBoss = false)
    {
        StartCoroutine(SpawnSquadRoutine(forceBoss));
    }

    private IEnumerator SpawnSquadRoutine(bool forceBoss)
    {
        EnemyConfig squadType = null;
        int count = 0;

        if (forceBoss && isBossWave && bossConfig != null)
        {
            // 1. Спочатку спавнимо самого боса
            SpawnEnemy(bossConfig);
            
            // 2. Потім готуємо його охорону (випадкові вороги з пулу, не вози)
            squadType = GetWeightedRandomEnemy(false); 
            count = Random.Range(bossEscortMin, bossEscortMax + 1);
        }
        else if (isCartWave && !hasSpawnedCartThisWave && cartConfig != null)
        {
            // ГАРАНТОВАНИЙ СПАВН ВОЗА (Тільки 1 раз за хвилю)
            squadType = cartConfig;
            count = 1;
            hasSpawnedCartThisWave = true;
        }
        else
        {
            // Звичайний загін: вибираємо тип ворога
            squadType = GetWeightedRandomEnemy(true);
            
            if (squadType != null)
            {
                // Якщо рандом таки вибрав віз з пулу - він виходить один, піхота - загоном
                count = squadType.isCart ? 1 : Random.Range(3, 6); 
            }
        }

        // Вибираємо випадкову точку спавну
        Transform sp = (spawnPoints != null && spawnPoints.Length > 0) 
            ? spawnPoints[Random.Range(0, spawnPoints.Length)] 
            : transform;

        for (int i = 0; i < count; i++)
        {
            SpawnEnemy(squadType, sp.position);
            yield return new WaitForSeconds(fastSpawnDelay);
        }
    }

    void SpawnEnemy(EnemyConfig config, Vector3? basePos = null)
    {
        if (config == null || config.prefab == null) return;

        // Визначаємо позицію
        Vector3 pos = basePos ?? transform.position;

        // Розкид, щоб вороги в загоні не злипалися в одну точку
        pos += new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.8f, 0.8f), 0);
        
        Instantiate(config.prefab, pos, Quaternion.identity);

        if (GameManager.Instance != null) GameManager.Instance.RegisterEnemy();
    }

    void PrepareEnemyPool(int wave)
    {
        currentWavePool.Clear();
        bossConfig = isBossWave ? allEnemies.Find(e => e.isBoss) : null;
        cartConfig = isCartWave ? allEnemies.Find(e => e.isCart && e.unlockWave <= wave) : null;

        // Фільтруємо доступних ворогів за рівнем хвилі
        var available = allEnemies
            .Where(e => e.unlockWave <= wave && !e.isBoss && !e.isCart)
            .OrderByDescending(e => e.unlockWave)
            .ToList();

        if (available.Count == 0 && allEnemies.Count > 0) 
            available.Add(allEnemies[0]);

        // ГАРАНТІЯ: додаємо найновішого/найсильнішого ворога
        currentWavePool.Add(available[0]);
        available.RemoveAt(0);

        // Набираємо випадково ще типи для різноманіття (разом 2 або 3 типи)
        int targetTypeCount = Random.Range(2, 4);
        var extraEnemies = available.OrderBy(x => Random.value).ToList();

        foreach (var enemy in extraEnemies)
        {
            if (currentWavePool.Count >= targetTypeCount) break;
            currentWavePool.Add(enemy);
        }

        // Додаємо воза в пул для відображення в UI
        if (cartConfig != null) currentWavePool.Add(cartConfig);
    }

    EnemyConfig GetWeightedRandomEnemy(bool allowCarts)
    {
        if (currentWavePool.Count == 0) return null;

        var pool = allowCarts ? currentWavePool : currentWavePool.Where(e => !e.isCart).ToList();
        if (pool.Count == 0) pool = currentWavePool;

        int totalWeight = pool.Sum(e => e.spawnWeight);
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var e in pool)
        {
            currentWeight += e.spawnWeight;
            if (randomValue < currentWeight) return e;
        }
        return pool[0];
    }

    void UpdateWaveUI()
    {
        if (waveInfoPanel != null)
        {
            List<Sprite> icons = currentWavePool
                .Where(e => e.icon != null)
                .Select(e => e.icon)
                .Distinct()
                .ToList();
            waveInfoPanel.ShowWaveEnemies(icons);
        }
    }

    public int GetEstimatedGoldFromEnemies()
    {
        if (currentWavePool.Count == 0) return 0;
        
        float waveMultiplier = 1.0f + (currentWaveNumber * 0.1f);
        float avgGold = (float)currentWavePool.Average(e => e.baseGoldReward) * waveMultiplier;
        
        // Приблизний розрахунок (приблизно 15 ворогів за хвилю в новій системі загонів)
        return Mathf.RoundToInt(avgGold * 15);
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
    }

    public void ClearEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies) Destroy(enemy);
    }
}