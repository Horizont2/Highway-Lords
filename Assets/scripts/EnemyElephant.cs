using UnityEngine;

public class EnemyElephant : MonoBehaviour
{
    [Header("Характеристики")]
    public float maxHealth = 1500f;
    public float currentHealth;
    public float moveSpeed = 0.5f;
    public int damage = 40;
    
    [Header("Атака")]
    public float attackRange = 2.5f; // Більший радіус атаки через розмір
    public float attackCooldown = 3.0f;
    private float lastAttackTime = 0f;

    [Header("Посилання")]
    public int goldReward = 150;
    private Animator anim;
    private Transform targetCastle;
    private bool isDead = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;

        // Пошук замку
        if (GameManager.Instance != null && GameManager.Instance.castle != null)
        {
            targetCastle = GameManager.Instance.castle.transform;
        }
    }

    void Update()
    {
        if (isDead || targetCastle == null) return;

        float distanceToCastle = Vector3.Distance(transform.position, targetCastle.position);

        if (distanceToCastle > attackRange)
        {
            // Йдемо до замку
            transform.position = Vector3.MoveTowards(transform.position, targetCastle.position, moveSpeed * Time.deltaTime);
            anim.SetBool("isWalking", true);
        }
        else
        {
            // Стоїмо і б'ємо
            anim.SetBool("isWalking", false);

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
            }
        }
    }

    void Attack()
    {
        lastAttackTime = Time.time;
        anim.SetTrigger("Attack");

        // Урон по замку (викликається через Animation Event або прямо тут)
        if (GameManager.Instance != null && GameManager.Instance.castle != null)
        {
            GameManager.Instance.castle.TakeDamage(damage);
            GameManager.Instance.ShowDamage(damage, targetCastle.position);
            
            // Звук атаки слона
            if (SoundManager.Instance != null) 
                SoundManager.Instance.PlaySFX(SoundManager.Instance.heavyHitSound);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        GameManager.CreateDamagePopup(transform.position + Vector3.up, Mathf.RoundToInt(amount));

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        anim.SetTrigger("Die");
        GetComponent<Collider2D>().enabled = false;
        
        // Вимикаємо скрипт, щоб він не рухався під час анімації смерті
        this.enabled = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddResource(ResourceType.Gold, goldReward);
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, goldReward, transform.position);
            GameManager.Instance.UnregisterEnemy();
        }

        Destroy(gameObject, 3f); // Зникає через 3 секунди після смерті
    }
}