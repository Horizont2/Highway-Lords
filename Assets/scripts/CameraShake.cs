using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private float shakeTimer;
    private float shakeAmount;
    private Vector3 currentShakeOffset = Vector3.zero;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Використовуємо LateUpdate, щоб трясти камеру ТІЛЬКИ ПІСЛЯ того, як CameraController її пересунув
    void LateUpdate()
    {
        // 1. Спочатку віднімаємо зсув від попереднього кадру, щоб повернути камеру на справжню позицію
        transform.position -= currentShakeOffset;
        currentShakeOffset = Vector3.zero;

        // 2. Якщо йде тряска - генеруємо новий зсув
        if (shakeTimer > 0)
        {
            currentShakeOffset = Random.insideUnitSphere * shakeAmount;
            currentShakeOffset.z = 0f; // Для 2D ігор вісь Z краще не трясти, щоб не глючила графіка!

            // Додаємо зсув до позиції камери
            transform.position += currentShakeOffset;

            // Зменшуємо таймер
            shakeTimer -= Time.deltaTime;
        }
    }

    // Цей метод ми викликаємо з інших скриптів
    public void Shake(float time, float amount)
    {
        shakeTimer = time;
        shakeAmount = amount;
    }
}