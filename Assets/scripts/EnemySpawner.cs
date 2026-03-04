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
    public bool isElephant = false; // === НОВЕ: Галочка для слона ===
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
    private EnemyConfig cartConfig; 
    private EnemyConfig elephantConfig; // === НОВЕ: Конфіг слона ===
    
    private int currentWaveNumber;
    private bool isBossWave = false;
    private bool isCartWave = false;
    private bool isElephantWave = false; // === НОВЕ ===
    private bool hasSpawnedCartThisWave = false; 

    public void PrepareForWave(int waveNumber)
    {
        currentWaveNumber = waveNumber;
        isBossWave = (waveNumber % 10 == 0);
        isElephantWave = (waveNumber % 25 == 0); // Слон кожні 25 хвиль
        
        isCartWave = (!isBossWave && waveNumber % 3 == 0);
        hasSpawnedCartThisWave = false; 
        
        PrepareEnemyPool(waveNumber);
        UpdateWaveUI();
    }

    public void SpawnSquad(bool isBossMoment = false)
    {
        StartCoroutine(SpawnSquadRoutine(isBossMoment));
    }

    private IEnumerator SpawnSquadRoutine(bool isBossMoment)
    {
        List<EnemyConfig> squadToSpawn = new List<EnemyConfig>();

        // 1. ПЕРЕВІРКА НА СПЕЦІАЛЬНИХ ЮНІТІВ (Бос, Слон, Віз)
        if (isBossMoment)
        {
            if (isBossWave && bossConfig != null) squadToSpawn.Add(bossConfig);
            if (isElephantWave && elephantConfig != null) squadToSpawn.Add(elephantConfig);
        }
        else if (isCartWave && !hasSpawnedCartThisWave && cartConfig != null)
        {
            squadToSpawn.Add(cartConfig);
            hasSpawnedCartThisWave = true;
        }

        // 2. ДОДАЄМО ЗВИЧАЙНУ ПІХОТУ (Змішаний загін)
        int escortCount = 0;
        
        if (isBossMoment && (isBossWave || isElephantWave)) 
            escortCount = Random.Range(bossEscortMin, bossEscortMax + 1);
        else if (squadToSpawn.Count == 0) // Якщо це звичайний тік без воза і босів
            escortCount = Random.Range(3, 6);

        // === НОВЕ: Змішування юнітів ===
        // Вибираємо випадкового ворога для КОЖНОГО місця в загоні окремо
        for (int i = 0; i < escortCount; i++)
        {
            EnemyConfig randomUnit = GetWeightedRandomEnemy(false);
            if (randomUnit != null) squadToSpawn.Add(randomUnit);
        }

        // 3. СПАВНИМО ЗАГІН З ЗАТРИМКОЮ
        Transform sp = (spawnPoints != null && spawnPoints.Length > 0) 
            ? spawnPoints[Random.Range(0, spawnPoints.Length)] 
            : transform;

        foreach (var unitConfig in squadToSpawn)
        {
            SpawnEnemy(unitConfig, sp.position);
            yield return new WaitForSeconds(fastSpawnDelay);
        }
    }

    void SpawnEnemy(EnemyConfig config, Vector3? basePos = null)
    {
        if (config == null || config.prefab == null) return;

        Vector3 pos = basePos ?? transform.position;
        pos += new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.8f, 0.8f), 0);
        
        Instantiate(config.prefab, pos, Quaternion.identity);

        if (GameManager.Instance != null) GameManager.Instance.RegisterEnemy();
    }

    void PrepareEnemyPool(int wave)
    {
        currentWavePool.Clear();
        bossConfig = isBossWave ? allEnemies.Find(e => e.isBoss) : null;
        cartConfig = isCartWave ? allEnemies.Find(e => e.isCart && e.unlockWave <= wave) : null;
        elephantConfig = isElephantWave ? allEnemies.Find(e => e.isElephant) : null; // Шукаємо слона

        var available = allEnemies
            .Where(e => e.unlockWave <= wave && !e.isBoss && !e.isCart && !e.isElephant) // Відсікаємо всіх спеціальних
            .OrderByDescending(e => e.unlockWave)
            .ToList();

        if (available.Count == 0 && allEnemies.Count > 0) 
            available.Add(allEnemies[0]);

        currentWavePool.Add(available[0]);
        available.RemoveAt(0);

        int targetTypeCount = Random.Range(2, 4);
        var extraEnemies = available.OrderBy(x => Random.value).ToList();

        foreach (var enemy in extraEnemies)
        {
            if (currentWavePool.Count >= targetTypeCount) break;
            currentWavePool.Add(enemy);
        }

        if (cartConfig != null) currentWavePool.Add(cartConfig);
        if (elephantConfig != null) currentWavePool.Add(elephantConfig);
    }

    EnemyConfig GetWeightedRandomEnemy(bool allowCarts)
    {
        if (currentWavePool.Count == 0) return null;

        var pool = allowCarts ? currentWavePool : currentWavePool.Where(e => !e.isCart && !e.isElephant && !e.isBoss).ToList();
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
        return Mathf.RoundToInt(avgGold * 15);
    }

    public void StopSpawning() { StopAllCoroutines(); }
    public void ClearEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies) Destroy(enemy);
    }
}