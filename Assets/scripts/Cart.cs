using UnityEngine;
using UnityEngine.UI;

public class Cart : MonoBehaviour
{
    [Header("Статус")]
    public bool isBoss = false; 

    [Header("Характеристики")]
    public float speed = 2f;
    public int maxHealth = 50; 
    private int currentHealth;

    [Header("UI")]
    public Image healthBarFill; 

    [Header("Нагорода (Золото)")]
    public int goldAmount = 50; 
    
    [Header("Ресурси (Дерево)")]
    [Range(0, 100)] public int woodChance = 50; 
    public int woodAmount = 20;

    [Header("Ресурси (Камінь)")]
    [Range(0, 100)] public int stoneChance = 30;
    public int stoneAmount = 10;

    void Start()
    {
        int difficultyHealth = 50;
        // Отримуємо складність з GameManager, якщо він є
        if (GameManager.Instance != null)
        {
            difficultyHealth = GameManager.Instance.GetDifficultyHealth();
            goldAmount = GameManager.Instance.GetGoldReward();
        }

        // Налаштування Боса
        if (isBoss)
        {
            maxHealth = difficultyHealth * 5; 
            transform.localScale = transform.localScale * 1.5f; 
            speed = speed * 0.7f; 
        }
        else
        {
            maxHealth = difficultyHealth;
        }

        currentHealth = maxHealth;
        UpdateHealthBar(); 
    }

    void Update()
    {
        transform.Translate(Vector2.left * speed * Time.deltaTime);
        
        // Знищення, якщо віз виїхав за межі екрану
        if (transform.position.x < -15f) Destroy(gameObject);
    }

    // Цей метод викликає Стріла (Arrow.cs)
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        // Показуємо цифру нанесеної шкоди (червону)
        if (GameManager.Instance != null)
            GameManager.Instance.ShowDamage(damage, transform.position);
            
        UpdateHealthBar(); 
        
        if (currentHealth <= 0) Die();
    }

    void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float fillValue = (float)currentHealth / maxHealth;
            healthBarFill.fillAmount = fillValue;
        }
    }

    void Die()
    {
        // Звуки
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.cartBreak);
            SoundManager.Instance.PlaySFX(SoundManager.Instance.coinPickup);
        }

        if (GameManager.Instance != null)
        {
            // === ЗОЛОТО ===
            int finalGold = isBoss ? goldAmount * 3 : goldAmount;
            GameManager.Instance.AddResource(ResourceType.Gold, finalGold);
            
            // Показуємо іконку золота з текстом кількості
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, finalGold, transform.position + Vector3.up * 0.5f);

            // === ДЕРЕВО ===
            if (isBoss || Random.Range(0, 100) < woodChance)
            {
                GameManager.Instance.AddResource(ResourceType.Wood, woodAmount);
                
                // Зсуваємо трохи вправо, щоб не перекривало золото
                Vector3 woodPos = transform.position + new Vector3(1.2f, 1f, 0); 
                // Показуємо іконку дерева з текстом
                GameManager.Instance.ShowResourcePopup(ResourceType.Wood, woodAmount, woodPos);
            }

            // === КАМІНЬ ===
            if (isBoss || Random.Range(0, 100) < stoneChance)
            {
                GameManager.Instance.AddResource(ResourceType.Stone, stoneAmount);
                
                // Зсуваємо трохи вліво
                Vector3 stonePos = transform.position + new Vector3(-1.2f, 1f, 0);
                // Показуємо іконку каменю з текстом
                GameManager.Instance.ShowResourcePopup(ResourceType.Stone, stoneAmount, stonePos);
            }
        }
        
        Destroy(gameObject);
    }
}