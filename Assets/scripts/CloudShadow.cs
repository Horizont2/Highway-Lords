using UnityEngine;

public class CloudShadow : MonoBehaviour
{
    public float speed = 1.5f; // Швидкість руху хмари
    public float lifetime = 40f; // Через скільки секунд видалити

    void Start()
    {
        // Знищуємо хмару через заданий час, коли вона вилетить за екран
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Рухаємо вправо
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }
}