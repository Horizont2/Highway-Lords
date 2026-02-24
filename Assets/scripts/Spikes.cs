using UnityEngine;

public class Spikes : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar; // Не забудь перетягнути сюди HealthBarCanvas з префабу

    [Header("Характеристики")]
    public int health = 300; 
    private int maxHealth;

    [Header("Ефекти")]
    public GameObject destroyEffect; // Опціонально: ефект вибуху/трісок

    void Start()
    {
        maxHealth = health;

        // 1. Реєструємось у GameManager
        if (GameManager.Instance != null)
        {
            // Якщо раптом стара барикада ще існує - видаляємо її
            if (GameManager.Instance.currentSpikes != null && GameManager.Instance.currentSpikes != this)
            {
                Destroy(GameManager.Instance.currentSpikes.gameObject);
            }
            GameManager.Instance.currentSpikes = this;
        }

        // 2. Налаштовуємо Health Bar
        if (healthBar != null)
        {
            healthBar.targetTransform = transform; // Прив'язуємо бар до барикади
            healthBar.SetHealth(health, maxHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        // Оновлюємо смужку життя
        if (healthBar != null)
        {
            healthBar.SetHealth(health, maxHealth);
        }

        // Показуємо цифри шкоди
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowDamage(damage, transform.position);
        }
        
        // Легка тряска камери
        if (CameraShake.Instance != null) 
        {
            CameraShake.Instance.Shake(0.05f, 0.1f);
        }

        if (health <= 0)
        {
            BreakSpikes();
        }
    }

    void BreakSpikes()
    {
        // Повідомляємо менеджеру, що барикади більше немає
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentSpikes = null;
            GameManager.Instance.UpdateUI(); // Оновлюємо кнопку, щоб можна було будувати знову
        }

        // Звук руйнування (використовуємо castleDamage, щоб не було помилок)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.castleDamage);
        }

        // Ефект руйнування
        if (destroyEffect != null)
        {
            Instantiate(destroyEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}