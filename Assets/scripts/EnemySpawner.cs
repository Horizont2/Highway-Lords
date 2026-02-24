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

    [Header("Бос-хвилі")]
    public int bossEscortMin = 3;
    public int bossEscortMax = 5;

    private EnemyConfig bossConfig;
    private bool isBossWave = false;

    [Header("Налаштування Хвилі")]
    public Transform[] spawnPoints;      
    public float timeBetweenSpawns = 2.5f; 

    [Header("Налаштування Загонів")]
    [Range(0f, 1f)] public float squadChance = 0.3f; 
    public int minSquadSize = 3; 
    public int maxSquadSize = 5; 
    public float fastSpawnDelay = 0.4f; 
    
    [Header("UI")]
    public WaveInfoPanel waveInfoPanel;  

    // Внутрішні змінні
    private List<EnemyConfig> currentWavePool = new List<EnemyConfig>(); 
    private int enemiesToSpawn;
    private int enemiesSpawned;
    private bool spawning = false;
    private int currentWaveNumber; 

    public void StartWave(int waveNumber)
    {
        StopAllCoroutines();
        currentWaveNumber = waveNumber;

        PrepareEnemyPool(waveNumber);
        
        // Оновлюємо UI строго після формування пулу
        UpdateWaveUI(); 

        isBossWave = (waveNumber % 10 == 0);
        enemiesToSpawn = Mathf.RoundToInt(5 + (waveNumber * 1.5f));

        if (isBossWave)
        {
            int escorts = Random.Range(bossEscortMin, bossEscortMax + 1);
            enemiesToSpawn = 1 + escorts; // 1 бос + ескорт
        }

        enemiesSpawned = 0;
        spawning = true;

        if (isBossWave && bossConfig != null)
        {
            SpawnEnemy(bossConfig); // спавнимо боса одразу
        }

        if (GameManager.Instance != null) GameManager.Instance.InitWaveProgress(enemiesToSpawn);

        StartCoroutine(SpawnRoutine());
    }

    void PrepareEnemyPool(int currentWave)
    {
        currentWavePool.Clear();
        bossConfig = null;
        isBossWave = (currentWave % 10 == 0);

        // 1. БОС (спавнимо окремо, щоб не дублювався)
        if (isBossWave)
        {
            bossConfig = allEnemies.Find(e => e.isBoss);
        }

        // 2. Доступні вороги (без босів і возів)
        List<EnemyConfig> available = allEnemies
            .Where(e => e.unlockWave <= currentWave && !e.isBoss && !e.isCart)
            .ToList();

        if (available.Count == 0 && allEnemies.Count > 0)
            available.Add(allEnemies[0]); // Fallback

        // 3. ГАРАНТІЯ НОВОГО ЮНІТА
        // Сортуємо: спочатку нові (високий UnlockWave)
        available.Sort((a, b) => b.unlockWave.CompareTo(a.unlockWave));

        if (available.Count > 0)
        {
            // Додаємо найсильнішого/нового ворога першим
            currentWavePool.Add(available[0]); 
            available.RemoveAt(0); // Прибираємо зі списку доступних, щоб не дублювати
        }

        // 4. ВИПАДКОВИЙ РОЗМІР ПУЛУ (2 або 3 типи)
        // Random.Range(2, 4) поверне 2 або 3
        int targetTypeCount = Random.Range(2, 4); 

        // Перемішуємо залишок доступних ворогів
        available = available.OrderBy(x => Random.value).ToList();

        // Добираємо ворогів до цільової кількості
        foreach (var enemy in available)
        {
            if (currentWavePool.Count >= targetTypeCount) break;
            currentWavePool.Add(enemy);
        }

        // 5. ВІЗОК (Бонус) - додається понад ліміт (не на босс-хвилі)
        if (!isBossWave && currentWave % 3 == 0) 
        {
            EnemyConfig cart = allEnemies.Find(e => e.isCart && e.unlockWave <= currentWave);
            if (cart != null) currentWavePool.Add(cart);
        }
    }

    void UpdateWaveUI()
    {
        if (waveInfoPanel != null)
        {
            List<Sprite> icons = new List<Sprite>();
            // Беремо іконки ТІЛЬКИ з поточного пулу
            foreach (var enemy in currentWavePool)
            {
                if (enemy.icon != null && !icons.Contains(enemy.icon))
                {
                    icons.Add(enemy.icon);
                }
            }
            waveInfoPanel.ShowWaveEnemies(icons);
        }
    }

    public int GetEstimatedGoldFromEnemies()
    {
        if (currentWavePool.Count == 0) return 0;
        
        float totalWeight = 0f;
        float weightedGoldSum = 0f;
        float waveMultiplier = 1.0f + (currentWaveNumber * 0.1f);

        foreach (var enemy in currentWavePool)
        {
            float reward = enemy.baseGoldReward * waveMultiplier;
            weightedGoldSum += reward * enemy.spawnWeight;
            totalWeight += enemy.spawnWeight;
        }

        if (totalWeight == 0) return 0;
        return Mathf.RoundToInt((weightedGoldSum / totalWeight) * enemiesToSpawn);
    }

    IEnumerator SpawnRoutine()
    {
        while (enemiesSpawned < enemiesToSpawn && spawning)
        {
            int remaining = enemiesToSpawn - enemiesSpawned;
            bool spawnSquad = (Random.value < squadChance) && (remaining >= minSquadSize) && (enemiesToSpawn > 5);

            if (spawnSquad)
            {
                // Загін: вибираємо один тип ворога для всієї групи
                EnemyConfig squadType = GetWeightedRandomEnemy(false); 
                if (squadType != null && squadType.isCart)
                {
                    // Вози не можуть спавнитись "загоном"
                    SpawnEnemy(squadType);
                    continue;
                }

                int currentSquadSize = Random.Range(minSquadSize, maxSquadSize + 1);
                if (currentSquadSize > remaining) currentSquadSize = remaining;

                for (int i = 0; i < currentSquadSize; i++)
                {
                    SpawnEnemy(squadType); 
                    yield return new WaitForSeconds(fastSpawnDelay);
                }
            }
            else
            {
                // Одинак
                SpawnEnemy(GetWeightedRandomEnemy(true));
            }
            
            float delay = Random.Range(timeBetweenSpawns * 0.8f, timeBetweenSpawns * 1.2f);
            yield return new WaitForSeconds(delay);
        }
        spawning = false;
    }

    void SpawnEnemy(EnemyConfig config)
    {
        if (config == null || config.prefab == null) return;

        Vector3 pos = transform.position; 
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            pos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
        }
        
        pos += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
        Instantiate(config.prefab, pos, Quaternion.identity);

        enemiesSpawned++;
        if (GameManager.Instance != null) GameManager.Instance.RegisterEnemy();
    }

    EnemyConfig GetWeightedRandomEnemy(bool allowCarts)
    {
        if (currentWavePool.Count == 0) return null;

        var pool = allowCarts ? currentWavePool : currentWavePool.Where(e => !e.isCart).ToList();
        if (pool.Count == 0) pool = currentWavePool;

        int totalWeight = 0;
        foreach (var e in pool) totalWeight += e.spawnWeight;

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var e in pool)
        {
            currentWeight += e.spawnWeight;
            if (randomValue < currentWeight)
                return e;
        }
        return pool[0];
    }

    public void StopSpawning()
    {
        spawning = false;
        StopAllCoroutines();
        if (waveInfoPanel != null) waveInfoPanel.Hide();
    }

    public void ClearEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies) Destroy(enemy);
    }
}