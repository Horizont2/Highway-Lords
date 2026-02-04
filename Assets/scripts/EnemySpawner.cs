using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Перелік типів ворогів для зручності
public enum EnemyType { Guard, Archer, Cart, Boss }

// Клас, що описує один загін (групу ворогів)
[System.Serializable]
public class Squad
{
    public string squadName;        // Назва (напр. "Охорона Воза")
    public List<EnemyType> units;   // Послідовність виходу юнітів
    public float delayBetweenUnits = 0.8f; // Пауза між юнітами в загоні
    public int costInBudget = 10;   // Скільки "балів" коштує цей загін
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Префаби")]
    public GameObject guardPrefab;
    public GameObject enemyArcherPrefab;
    public GameObject cartPrefab;
    public GameObject bossPrefab;

    [Header("Налаштування")]
    public float timeBetweenSquads = 4f; // Час відпочинку між групами ворогів
    public Transform spawnPoint;

    // Список можливих шаблонів загонів
    private List<Squad> squadTemplates;

    void Awake()
    {
        InitializeSquadTemplates();
    }

    // Тут ми прописуємо "рецепти" загонів
    void InitializeSquadTemplates()
    {
        squadTemplates = new List<Squad>();

        // 1. Звичайний патруль (Дешевий)
        Squad infantry = new Squad();
        infantry.squadName = "Infantry Patrol";
        infantry.units = new List<EnemyType> { EnemyType.Guard, EnemyType.Guard };
        infantry.costInBudget = 10;
        squadTemplates.Add(infantry);

        // 2. Підтримка лучників (Середній)
        Squad ranged = new Squad();
        ranged.squadName = "Archer Support";
        ranged.units = new List<EnemyType> { EnemyType.Guard, EnemyType.Archer };
        ranged.costInBudget = 15;
        squadTemplates.Add(ranged);

        // 3. КАРАВАН З ВОЗОМ (Дорогий і захищений)
        // Порядок: Гвардієць -> Віз -> Лучник
        Squad cartEscort = new Squad();
        cartEscort.squadName = "Cart Caravan";
        cartEscort.units = new List<EnemyType> { EnemyType.Guard, EnemyType.Guard, EnemyType.Cart, EnemyType.Archer };
        cartEscort.delayBetweenUnits = 1.2f; 
        cartEscort.costInBudget = 40; // Коштує багато бюджету
        squadTemplates.Add(cartEscort);
    }

    // Викликається з GameManager
    public void StartWave(int waveNumber)
    {
        StopAllCoroutines();
        StartCoroutine(SpawnWaveRoutine(waveNumber));
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
    }

    IEnumerator SpawnWaveRoutine(int waveNumber)
    {
        // 1. Розрахунок бюджету (Економіка складності)
        // Хвиля 1 = 35 балів, Хвиля 10 = 170 балів і т.д.
        int waveBudget = 20 + (waveNumber * 15); 
        
        List<Squad> squadsToSpawn = new List<Squad>();

        // 2. ЛОГІКА БОСА (Кожні 10 хвиль)
        if (waveNumber % 10 == 0)
        {
            SpawnUnit(EnemyType.Boss);
            yield return new WaitForSeconds(2f);
            
            // На хвилях боса бюджет подвоюється для м'яса
            waveBudget *= 2; 
        }

        // 3. ЛОГІКА ВОЗА (Гарантований віз кожні 3 хвилі)
        // Якщо це 3, 6, 9 хвиля... то обов'язково додаємо Караван першим
        if (waveNumber % 3 == 0)
        {
            Squad cartSquad = squadTemplates.Find(x => x.squadName == "Cart Caravan");
            if (cartSquad != null)
            {
                squadsToSpawn.Add(cartSquad);
                waveBudget -= cartSquad.costInBudget;
            }
        }

        // 4. ЗАПОВНЕННЯ ЗАЛИШКУ БЮДЖЕТУ
        // Купуємо випадкові загони, поки є гроші
        int safetyCounter = 0;
        while (waveBudget > 5 && safetyCounter < 100)
        {
            // Знаходимо всі загони, які можемо дозволити
            List<Squad> affordable = squadTemplates.FindAll(x => x.costInBudget <= waveBudget);
            
            if (affordable.Count == 0) break; 

            // Вибираємо випадковий
            Squad picked = affordable[Random.Range(0, affordable.Count)];
            squadsToSpawn.Add(picked);
            waveBudget -= picked.costInBudget;
            safetyCounter++;
        }

        // 5. ПРОЦЕС СПАВНУ
        foreach (Squad squad in squadsToSpawn)
        {
            // Спавнимо кожного юніта в загоні по черзі
            foreach (EnemyType type in squad.units)
            {
                SpawnUnit(type);
                // Чекаємо перед наступним членом загону
                yield return new WaitForSeconds(squad.delayBetweenUnits);
            }

            // Загін вийшов повністю. Чекаємо перед наступною групою.
            yield return new WaitForSeconds(timeBetweenSquads);
        }
    }

    void SpawnUnit(EnemyType type)
    {
        GameObject prefab = null;
        Vector3 pos = spawnPoint.position;

        switch (type)
        {
            case EnemyType.Guard: 
                prefab = guardPrefab; 
                // Невеликий розкид по Y, щоб не злипались
                pos += new Vector3(0, Random.Range(-0.3f, 0.3f), 0);
                break;
            case EnemyType.Archer: 
                prefab = enemyArcherPrefab; 
                // Лучники трохи позаду
                pos += new Vector3(0.5f, Random.Range(-0.3f, 0.3f), 0);
                break;
            case EnemyType.Cart: 
                prefab = cartPrefab; 
                // Віз їде по центру
                pos += Vector3.zero; 
                break;
            case EnemyType.Boss: 
                prefab = bossPrefab; 
                break;
        }

        if (prefab != null)
        {
            Instantiate(prefab, pos, Quaternion.identity);
        }
    }

    public void ClearEnemies()
    {
        // Знаходимо всіх ворогів за їх компонентами і видаляємо
        // Це надійніше, ніж за тегами при рестарті
        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var script in allScripts)
        {
            if (script is Guard || script is Cart || script is EnemyArcher || script is Boss || script is EnemyProjectile)
            {
                Destroy(script.gameObject);
            }
        }
    }
}