using UnityEngine;

public class Spikes : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar; 

    [Header("Характеристики")]
    public int baseHealth = 300; 
    private int currentHealth;
    private int maxHealth;

    [Header("Ефекти")]
    public GameObject destroyEffect; 

    void Start()
    {
        // === НОВА ЕКОНОМІКА: Здоров'я барикади росте разом із хвилями ===
        if (GameManager.Instance != null)
        {
            int wave = GameManager.Instance.currentWave;
            // Використовуємо криву ворогів, щоб барикада завжди тримала удар пропорційно етапу гри
            maxHealth = EconomyConfig.GetEnemyHealth(baseHealth, wave);
            
            if (GameManager.Instance.currentSpikes != null && GameManager.Instance.currentSpikes != this)
            {
                Destroy(GameManager.Instance.currentSpikes.gameObject);
            }
            GameManager.Instance.currentSpikes = this;
        }
        else
        {
            maxHealth = baseHealth;
        }

        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.targetTransform = transform; 
            healthBar.SetHealth(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (healthBar != null) healthBar.SetHealth(currentHealth, maxHealth);
        if (GameManager.Instance != null) GameManager.Instance.ShowDamage(damage, transform.position);
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.05f, 0.1f);

        if (currentHealth <= 0) BreakSpikes();
    }

    void BreakSpikes()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentSpikes = null;
            GameManager.Instance.UpdateUI(); 
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.castleDamage);
        if (destroyEffect != null) Instantiate(destroyEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}