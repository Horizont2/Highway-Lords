using UnityEngine;

public class EnemyElephant : MonoBehaviour
{
    [Header("Характеристики")]
    public float maxHealth = 1500f;
    public float currentHealth;
    public float moveSpeed = 0.5f;
    public int damage = 40;
    
    [Header("Атака")]
    public float attackRange = 2.5f; 
    public float attackCooldown = 3.0f;
    private float lastAttackTime = 0f;

    [Header("Посилання")]
    public int goldReward = 150;
    private Animator anim;
    private Transform targetCastle;
    private bool isDead = false;

    private float _baseMaxHealth;
    private int _baseDamage;
    private int _baseGoldReward;

    void Awake()
    {
        _baseMaxHealth = maxHealth;
        _baseDamage = damage;
        _baseGoldReward = goldReward;
    }

    void Start()
    {
        anim = GetComponent<Animator>();

        if (GameManager.Instance != null)
        {
            int wave = GameManager.Instance.currentWave;
            maxHealth = EconomyConfig.GetEnemyHealth(Mathf.RoundToInt(_baseMaxHealth), wave);
            damage = EconomyConfig.GetEnemyDamage(_baseDamage, wave);
            goldReward = EconomyConfig.GetEnemyGoldDrop(_baseGoldReward, wave);
        }

        currentHealth = maxHealth;

        if (GameManager.Instance != null && GameManager.Instance.castle != null)
            targetCastle = GameManager.Instance.castle.transform;
    }

    void Update()
    {
        if (isDead || targetCastle == null) return;

        float distanceToCastle = Vector3.Distance(transform.position, targetCastle.position);

        if (distanceToCastle > attackRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetCastle.position, moveSpeed * Time.deltaTime);
            anim.SetBool("isWalking", true);
        }
        else
        {
            anim.SetBool("isWalking", false);
            if (Time.time - lastAttackTime >= attackCooldown) Attack();
        }
    }

    void Attack()
    {
        lastAttackTime = Time.time;
        anim.SetTrigger("Attack");

        if (GameManager.Instance != null && GameManager.Instance.castle != null)
        {
            GameManager.Instance.castle.TakeDamage(damage);
            GameManager.Instance.ShowDamage(damage, targetCastle.position);
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.heavyHitSound);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        GameManager.CreateDamagePopup(transform.position + Vector3.up, Mathf.RoundToInt(amount));

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        anim.SetTrigger("Die");
        GetComponent<Collider2D>().enabled = false;
        this.enabled = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddResource(ResourceType.Gold, goldReward);
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, goldReward, transform.position);
            GameManager.Instance.UnregisterEnemy();
        }

        Destroy(gameObject, 3f); 
    }
}