using UnityEngine;
using UnityEngine.UI;

public class Castle : MonoBehaviour
{
    [Header("Характеристики")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI")]
    public Image healthBarFill;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // ЗВУК ТРИВОГИ (Урон по замку)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.castleDamage);
        }

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