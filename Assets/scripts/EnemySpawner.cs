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
    public bool isElephant = false; 
    public bool isBatteringRam = false; 
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
    public float fastSpawnDelay = 0.3f; 

    [Header("UI")]
    public WaveInfoPanel waveInfoPanel;  

    private List<EnemyConfig> currentWavePool = new List<EnemyConfig>(); 
    private EnemyConfig bossConfig;
    private EnemyConfig cartConfig; 
    private EnemyConfig elephantConfig; 
    private EnemyConfig ramConfig;      
    
    private int currentWaveNumber;
    private bool isBossWave = false;
    private bool isCartWave = false;
    private bool isElephantWave = false; 
    private bool isRamWave = false;     
    
    private bool hasSpawnedCartThisWave = false; 
    private bool hasSpawnedRamThisWave = false; 
    private bool hasSpawnedElephantThisWave = false; 

    public void PrepareForWave(int waveNumber)
    {
        currentWaveNumber = waveNumber;
        
        isBossWave = (waveNumber % 10 == 0);
        isRamWave = (waveNumber % 10 == 0);      
        isElephantWave = (waveNumber % 25 == 0); 
        
        isCartWave = (!isBossWave && !isElephantWave && waveNumber % 3 == 0);
        
        hasSpawnedCartThisWave = false; 
        hasSpawnedRamThisWave = false; 
        hasSpawnedElephantThisWave = false; 
        
        PrepareEnemyPool(waveNumber);
        UpdateWaveUI();
    }

    public void SpawnSquad(bool isBossMoment = false)
    {
        if (isElephantWave && !hasSpawnedElephantThisWave)
        {
            isBossMoment = true;
        }
        else if (!isBossMoment && (isElephantWave || isRamWave || isBossWave))
        {
            if (GameObject.FindGameObjectsWithTag("Enemy").Length <= 2) isBossMoment = true;
        }

        StartCoroutine(SpawnSquadRoutine(isBossMoment));
    }

    private IEnumerator SpawnSquadRoutine(bool isBossMoment)
    {
        List<EnemyConfig> squadToSpawn = new List<EnemyConfig>();

        if (isBossMoment)
        {
            if (isBossWave && bossConfig != null) squadToSpawn.Add(bossConfig);
            
            if (isElephantWave && elephantConfig != null && !hasSpawnedElephantThisWave) 
            {
                if (!squadToSpawn.Contains(elephantConfig)) squadToSpawn.Add(elephantConfig);
                hasSpawnedElephantThisWave = true;
            }
            
            if (isRamWave && ramConfig != null && !hasSpawnedRamThisWave) 
            {
                if (!squadToSpawn.Contains(ramConfig)) squadToSpawn.Add(ramConfig);
                hasSpawnedRamThisWave = true;
            }
        }
        else if (isCartWave && !hasSpawnedCartThisWave && cartConfig != null)
        {
            squadToSpawn.Add(cartConfig);
            hasSpawnedCartThisWave = true;
        }

        int escortCount = 0;
        
        if (isBossMoment && (isBossWave || isElephantWave || isRamWave)) 
            escortCount = Random.Range(bossEscortMin, bossEscortMax + 1);
        else if (squadToSpawn.Count == 0) 
            escortCount = Random.Range(3, 6);

        for (int i = 0; i < escortCount; i++)
        {
            EnemyConfig randomUnit = GetWeightedRandomEnemy(false);
            if (randomUnit != null) squadToSpawn.Add(randomUnit);
        }

        Transform sp = (spawnPoints != null && spawnPoints.Length > 0) 
            ? spawnPoints[Random.Range(0, spawnPoints.Length)] 
            : transform;

        CameraController cam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;

        foreach (var unitConfig in squadToSpawn)
        {
            // Стій! Чекаємо, доки закінчиться катсцена
            while (cam != null && cam.isCinematicPlaying)
            {
                yield return null;
            }

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
    }

    void PrepareEnemyPool(int wave)
    {
        currentWavePool.Clear();
        
        bossConfig = isBossWave ? allEnemies.Find(e => e.isBoss) : null;
        cartConfig = isCartWave ? allEnemies.Find(e => e.isCart && e.unlockWave <= wave) : null;
        elephantConfig = isElephantWave ? allEnemies.Find(e => e.isElephant) : null; 
        ramConfig = isRamWave ? allEnemies.Find(e => e.isBatteringRam) : null; 

        var available = allEnemies
            .Where(e => e.unlockWave <= wave && !e.isBoss && !e.isCart && !e.isElephant && !e.isBatteringRam) 
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
        if (ramConfig != null) currentWavePool.Add(ramConfig); 
    }

    EnemyConfig GetWeightedRandomEnemy(bool allowCarts)
    {
        if (currentWavePool.Count == 0) return null;

        var pool = allowCarts 
            ? currentWavePool 
            : currentWavePool.Where(e => !e.isCart && !e.isElephant && !e.isBoss && !e.isBatteringRam).ToList();
            
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