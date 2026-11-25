using UnityEngine;

public class CartSpawner : MonoBehaviour
{
    public GameObject cartPrefab; 
    public float spawnRate = 3f;  
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
        Instantiate(cartPrefab, transform.position, Quaternion.identity);
    }
}