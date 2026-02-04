using UnityEngine;
using UnityEngine.UI;

public class Castle : MonoBehaviour
{
    [Header("Характеристики")]
    public int maxHealth = 1000; // Збільшено для балансу (було 100)
    public int currentHealth;

    [Header("UI")]
    public Image healthBarFill; // Сюди перетягни картинку з типом Filled

    [Header("Точка виходу військ")]
    public Transform spawnPoint; // Створи пустий об'єкт перед дверима і перетягни сюди

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();

        // === РЕЄСТРАЦІЯ В МЕНЕДЖЕРІ ===
        if (GameManager.Instance != null)
        {
            // Кажемо менеджеру: "Я - головна база"
            GameManager.Instance.castle = this;

            // Кажемо менеджеру, де створювати лицарів
            if (spawnPoint != null)
            {
                GameManager.Instance.unitSpawnPoint = spawnPoint;
            }
            else
            {
                Debug.LogWarning("У Castle не задана точка SpawnPoint! Юніти з'являться всередині замку.");
                GameManager.Instance.unitSpawnPoint = transform; 
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // 1. ТРЯСКА ЕКРАНУ
        if (CameraShake.Instance != null)
        {
            // (Час тряски, Сила тряски)
            CameraShake.Instance.Shake(0.2f, 0.1f);
        }

        // 2. ЦИФРИ УРОНУ
        if (GameManager.Instance != null)
        {
            // Показуємо цифру трохи вище центру замку
            GameManager.Instance.ShowDamage(damage, transform.position + Vector3.up * 2f);
        }

        // 3. ЗВУК
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.castleDamage);
        }

        // 4. ПЕРЕВІРКА НА ПОРАЗКУ
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            if (GameManager.Instance != null) GameManager.Instance.Defeat();
        }
        
        UpdateUI();
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateUI();
    }

    public void HealMax()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)currentHealth / maxHealth;
        }
    }
}