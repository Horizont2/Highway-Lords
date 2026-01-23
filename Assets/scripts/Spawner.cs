using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Налаштування")]
    public GameObject normalCartPrefab; // Звичайний віз
    public GameObject bossCartPrefab;   // Префаб Боса
    public float spawnInterval = 3f;    // Час між спавном (сек)

    [Header("Статистика")]
    public int cartCounter = 0;         // Скільки всього виїхало

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            Spawn();
            timer = 0f;
        }
    }

    void Spawn()
    {
        cartCounter++; // +1 віз

        GameObject prefabToSpawn;

        // Перевірка: чи ділиться число на 10 без залишку? (10, 20, 30...)
        if (cartCounter % 10 == 0)
        {
            prefabToSpawn = bossCartPrefab;
            Debug.Log("УВАГА! БОСС!");
        }
        else
        {
            prefabToSpawn = normalCartPrefab;
        }

        // Створюємо віз прямо там, де стоїть цей Спавнер
        Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
    }
}