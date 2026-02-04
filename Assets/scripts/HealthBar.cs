using UnityEngine;
using UnityEngine.UI; // Обов'язково для роботи з Image

public class HealthBar : MonoBehaviour
{
    [Header("Налаштування")]
    public Image fillImage; // Сюди перетягни зелену картинку (Fill)
    public Transform targetTransform; // Сюди автоматично підтягнеться Лицар/Лучник

    private Vector3 originalScale;

    void Start()
    {
        // Запам'ятовуємо масштаб, щоб не деформуватися
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Логіка, щоб хелсбар не "дзеркалився", коли юніт повертається
        if (targetTransform != null)
        {
            if (targetTransform.localScale.x < 0)
            {
                // Якщо юніт дивиться вліво (-1), ми теж розвертаємось (-1), 
                // мінус на мінус дає плюс, і текст/бар виглядає нормально.
                transform.localScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);
            }
            else
            {
                transform.localScale = originalScale;
            }
        }
    }

    // Головний метод оновлення здоров'я
    public void SetHealth(int currentHealth, int maxHealth)
    {
        if (fillImage != null)
        {
            // Рахуємо відсоток (наприклад, 50 / 100 = 0.5)
            float fillValue = (float)currentHealth / maxHealth;
            fillImage.fillAmount = fillValue;
        }
    }

    // Метод для сумісності (якщо десь викликається старий код)
    public void SetMaxHealth(int maxHealth)
    {
        // Вже не потрібен для Image, але залишаємо пустим, щоб не було помилок
    }
}