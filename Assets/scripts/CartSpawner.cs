using UnityEngine;

public class CartSpawner : MonoBehaviour
{
    public GameObject cartPrefab; // Шаблон воза
    public float spawnRate = 3f;  // Раз на скільки секунд
    float nextSpawn = 0f;

    void Update()
    {
        if (Time.time > nextSpawn)
        {
            nextSpawn = Time.time + spawnRate;
            SpawnCart();
        }
    }

    void SpawnCart()
    {
        // Створюємо віз у позиції спавнера
        Instantiate(cartPrefab, transform.position, Quaternion.identity);
    }
}