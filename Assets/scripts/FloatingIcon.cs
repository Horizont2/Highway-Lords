using UnityEngine;

public class FloatingIcon : MonoBehaviour
{
    [Header("Налаштування анімації")]
    public float amplitude = 10f; // Як високо підлітає іконка (в пікселях)
    public float speed = 2f;      // Швидкість польоту

    private Vector3 startPos;

    void Start()
    {
        // Запам'ятовуємо початкову позицію
        startPos = transform.localPosition;
    }

    void Update()
    {
        // Математика плавного руху вгору-вниз (Синусоїда)
        float newY = startPos.y + Mathf.Sin(Time.time * speed) * amplitude;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}