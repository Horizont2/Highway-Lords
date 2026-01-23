using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Кого спавнити")]
    public GameObject cartPrefab;
    public GameObject bossPrefab;
    public GameObject guardPrefab; 

    [Header("Налаштування")]
    public float timeBetweenWaves = 3f;
    public float spawnInterval = 1.5f;

    private int enemiesPerWave = 3;

    void Start()
    {
        StartCoroutine(WaveLogic());
    }

    public void RestartWave()
    {
        StopAllCoroutines();
        StartCoroutine(WaveLogic());
    }

    public void ClearEnemies()
    {
        Cart[] carts = FindObjectsByType<Cart>(FindObjectsSortMode.None);
        foreach (var c in carts) Destroy(c.gameObject);
        
        Guard[] guards = FindObjectsByType<Guard>(FindObjectsSortMode.None);
        foreach (var g in guards) Destroy(g.gameObject);
    }

    IEnumerator WaveLogic()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeBetweenWaves);

            int currentWave = 1;
            if (GameManager.Instance != null)
                currentWave = GameManager.Instance.currentWave;

            // === ХВИЛЯ БОСА ===
            if (currentWave % 10 == 0)
            {
                int bossCount = currentWave / 10;
                
                for (int i = 0; i < bossCount; i++)
                {
                    SpawnBoss();
                    // Спавнимо охоронців гарантовано: одного зверху, одного знизу
                    if (guardPrefab != null)
                    {
                        // Збільшив відступ по Y до 0.8f, щоб не торкалися боса
                        Instantiate(guardPrefab, transform.position + new Vector3(1f, 0.8f, 0), Quaternion.identity);
                        Instantiate(guardPrefab, transform.position + new Vector3(1f, -0.8f, 0), Quaternion.identity);
                    }
                    yield return new WaitForSeconds(spawnInterval * 3f);
                }
            }
            else
            {
                // === ЗВИЧАЙНА ХВИЛЯ ===
                for (int i = 0; i < enemiesPerWave; i++)
                {
                    SpawnEnemy(); // Тут тепер рандомна позиція
                    yield return new WaitForSeconds(spawnInterval);
                }
                enemiesPerWave += 1; 
            }

            yield return new WaitForSeconds(8f);

            if (GameManager.Instance != null && GameManager.Instance.castle.maxHealth > 0)
            {
                GameManager.Instance.NextWave();
            }
        }
    }

    void SpawnEnemy()
    {
        // 1. Спавнимо ВІЗ (по центру, Y=0)
        if (cartPrefab != null) 
            Instantiate(cartPrefab, transform.position, Quaternion.identity);

        // 2. Спавнимо ОХОРОНЦЯ (Шанс 40%)
        if (guardPrefab != null && Random.value < 0.4f)
        {
            // === РАНДОМНА ПОЗИЦІЯ ===
            // Random.Range(0, 2) повертає 0 або 1.
            // Якщо 0 -> ставимо Y = 0.8 (Зверху)
            // Якщо 1 -> ставимо Y = -0.8 (Знизу)
            float randomY = (Random.Range(0, 2) == 0) ? 0.8f : -0.8f;

            // X = 0.5f (трохи позаду носа воза, але не надто далеко)
            Vector3 spawnPos = transform.position + new Vector3(0.5f, randomY, 0);

            Instantiate(guardPrefab, spawnPos, Quaternion.identity);
        }
    }

    void SpawnBoss()
    {
        if (bossPrefab != null) Instantiate(bossPrefab, transform.position, Quaternion.identity);
    }
}