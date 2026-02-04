using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
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

    [Header("Логіка зупинки (Spikes)")]
    public float stopDistance = 3.5f; 
    
    // Компоненти
    private Rigidbody2D rb;
    private Animator animator; // === НОВЕ: Аніматор ===

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); // Отримуємо аніматор

        // === ВАЖЛИВО: Фізика щоб не падав ===
        rb.gravityScale = 0; 
        rb.freezeRotation = true; 
        
        int difficultyHealth = 50;
        if (GameManager.Instance != null)
        {
            difficultyHealth = GameManager.Instance.GetDifficultyHealth();
            goldAmount = GameManager.Instance.GetGoldReward();
        }

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
        // === ПЕРЕВІРКА НА КОЛЮЧКИ ===
        if (GameManager.Instance != null && GameManager.Instance.currentSpikes != null)
        {
            float dist = Vector2.Distance(transform.position, GameManager.Instance.currentSpikes.transform.position);
            
            // Перевіряємо, чи колючки ПОПЕРЕДУ нас (лівіше по X)
            bool isSpikesAhead = (transform.position.x > GameManager.Instance.currentSpikes.transform.position.x);

            if (dist < stopDistance && isSpikesAhead)
            {
                // === ГАЛЬМУЄМО ===
                if (rb != null) rb.linearVelocity = Vector2.zero;
                
                // Зупиняємо анімацію
                if (animator != null) animator.SetBool("IsMoving", false); 
                
                return; // Виходимо з Update, щоб не виконувався код руху нижче
            }
        }

        // === РУХ ===
        // Вмикаємо анімацію
        if (animator != null) animator.SetBool("IsMoving", true);
        
        transform.Translate(Vector2.left * speed * Time.deltaTime);
        
        if (transform.position.x < -15f) Destroy(gameObject);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
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
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.cartBreak);
            SoundManager.Instance.PlaySFX(SoundManager.Instance.coinPickup);
        }

        if (GameManager.Instance != null)
        {
            int finalGold = isBoss ? goldAmount * 3 : goldAmount;
            GameManager.Instance.AddResource(ResourceType.Gold, finalGold);
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, finalGold, transform.position + Vector3.up * 0.5f);

            if (isBoss || Random.Range(0, 100) < woodChance)
            {
                GameManager.Instance.AddResource(ResourceType.Wood, woodAmount);
                Vector3 woodPos = transform.position + new Vector3(1.2f, 1f, 0); 
                GameManager.Instance.ShowResourcePopup(ResourceType.Wood, woodAmount, woodPos);
            }

            if (isBoss || Random.Range(0, 100) < stoneChance)
            {
                GameManager.Instance.AddResource(ResourceType.Stone, stoneAmount);
                Vector3 stonePos = transform.position + new Vector3(-1.2f, 1f, 0);
                GameManager.Instance.ShowResourcePopup(ResourceType.Stone, stoneAmount, stonePos);
            }
        }
        
        Destroy(gameObject);
    }
}