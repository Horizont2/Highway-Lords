using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private Vector3 originalPos; // Щоб повернути камеру на місце
    private float shakeTimer;
    private float shakeAmount;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Запам'ятовуємо, де камера стояла на початку
        originalPos = transform.localPosition;
    }

    void Update()
    {
        if (shakeTimer > 0)
        {
            // Рухаємо камеру випадково в межах кола радіусом shakeAmount
            transform.localPosition = originalPos + Random.insideUnitSphere * shakeAmount;
            
            // Зменшуємо час тряски
            shakeTimer -= Time.deltaTime;
        }
        else
        {
            // Повертаємо камеру точно на місце, коли час вийшов
            shakeTimer = 0f;
            transform.localPosition = originalPos;
        }
    }

    // Цей метод ми будемо викликати з інших скриптів
    public void Shake(float time, float amount)
    {
        shakeTimer = time;
        shakeAmount = amount;
    }
}