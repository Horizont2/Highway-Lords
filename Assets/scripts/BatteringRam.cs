using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class BatteringRam : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Характеристики")]
    public float speed = 0.8f;           // Повільний
    public float attackRange = 2.5f;     
    public float attackCooldown = 3.0f;  // Повільна, але потужна атака
    
    [Header("Базові Стати")]
    public int baseDamage = 100;         
    public int baseHealth = 500;         

    [Header("Еволюція (Аніматори)")]
    [Tooltip("0 - базовий (хвилі 1-49), 1 - (хвилі 50-99), 2 - (хвилі 100-149) і т.д.")]
    public RuntimeAnimatorController[] tierAnimators; // Масив для різних скінів

    private Animator animator;
    private Rigidbody2D rb;
    private int currentHealth;
    private int _maxHealth;
    private bool isDead = false;

    private Transform targetStructure;
    private float nextAttackTime = 0f;

    // ЗБЕРЕЖЕННЯ БАЗОВИХ ЗНАЧЕНЬ ІНСПЕКТОРА
    private int _baseHealth;
    private int _baseDamage;

    void Awake()
    {
        _baseHealth = baseHealth;
        _baseDamage = baseDamage;
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        if (GameManager.Instance != null)
        {
            int wave = GameManager.Instance.currentWave;
            
            // Застосовуємо формулу
            _maxHealth = EconomyConfig.GetEnemyHealth(_baseHealth, wave);
            baseDamage = EconomyConfig.GetEnemyDamage(_baseDamage, wave) * 5; 

            // === ЛОГІКА ЗМІНИ СКІНА (АНІМАТОРА) ===
            if (tierAnimators != null && tierAnimators.Length > 0 && animator != null)
            {
                // Визначаємо індекс: хвиля 1-49 = 0, 50-99 = 1, 100-149 = 2
                int skinIndex = (wave - 1) / 50; 
                
                // Захист від виходу за межі (якщо хвиля дуже велика, ставимо останній скін)
                if (skinIndex >= tierAnimators.Length)
                {
                    skinIndex = tierAnimators.Length - 1; 
                }
                
                animator.runtimeAnimatorController = tierAnimators[skinIndex];
            }

            GameManager.Instance.RegisterEnemy();
        }
        else
        {
            _maxHealth = _baseHealth;
        }

        currentHealth = _maxHealth;

        if (healthBar != null)
        {
            healthBar.targetTransform = transform;
            healthBar.SetHealth(currentHealth, _maxHealth);
        }
    }

    void Update()
    {
        if (isDead) return;

        if (GameManager.Instance != null && GameManager.Instance.isDefeated)
        {
            if (animator) animator.SetBool("IsMoving", true);
            rb.linearVelocity = new Vector2(-speed, 0f);
            return;
        }

        FindTarget();

        if (targetStructure != null)
        {
            float distanceToTarget;
            Collider2D targetCol = targetStructure.GetComponent<Collider2D>();
            
            if (targetCol != null) 
                distanceToTarget = Vector2.Distance(transform.position, targetCol.ClosestPoint(transform.position));
            else 
                distanceToTarget = Mathf.Abs(transform.position.x - targetStructure.position.x);

            if (distanceToTarget <= attackRange)
            {
                StopMoving();
                if (Time.time >= nextAttackTime)
                {
                    Attack();
                    nextAttackTime = Time.time + attackCooldown;
                }
            }
            else
            {
                MoveTowards(targetStructure.position);
            }
        }
        else
        {
            MoveTowards(transform.position + Vector3.left * 5f);
        }
    }

    void FindTarget()
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.currentSpikes != null)
            {
                targetStructure = GameManager.Instance.currentSpikes.transform;
            }
            else if (GameManager.Instance.castle != null)
            {
                targetStructure = GameManager.Instance.castle.transform;
            }
        }
    }

    void MoveTowards(Vector3 destination)
    {
        if (animator) animator.SetBool("IsMoving", true);
        
        Vector3 targetPosFixed = new Vector3(destination.x, transform.position.y, transform.position.z);
        Vector2 direction = (targetPosFixed - transform.position).normalized;
        
        rb.linearVelocity = direction * speed;
    }

    void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        if (animator) animator.SetBool("IsMoving", false);
    }

    void Attack()
    {
        if (animator) animator.SetTrigger("Attack");

        if (targetStructure != null)
        {
            if (targetStructure.TryGetComponent<Wall>(out Wall wall))
            {
                wall.TakeDamage(baseDamage);
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 0.3f);
                if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.heavyHitSound);
            }
            else if (targetStructure.TryGetComponent<Spikes>(out Spikes spikes))
            {
                spikes.TakeDamage(baseDamage);
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.1f, 0.2f);
                if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.heavyHitSound);
            }
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;
        
        currentHealth -= damageAmount;
        
        if (healthBar != null) healthBar.SetHealth(currentHealth, _maxHealth);
        GameManager.CreateDamagePopup(transform.position, damageAmount);
        
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        gameObject.tag = "Untagged";

        if (healthBar != null) healthBar.gameObject.SetActive(false);
        if (rb) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; }
        
        Collider2D col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        if (TryGetComponent<EnemyStats>(out EnemyStats stats)) stats.GiveGold();
        else if (GameManager.Instance != null) GameManager.Instance.UnregisterEnemy();

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);

        if (animator) { animator.enabled = false; }
        
        transform.Rotate(0, 0, -90);
        
        this.enabled = false;
        Destroy(gameObject, 5f);
    }
}