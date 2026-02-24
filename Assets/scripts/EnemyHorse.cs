using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))] // Автоматично додасть скрипт статистики
public class EnemyHorse : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Характеристики")]
    public float speed = 3.4f;          // Трохи повільніше за піхоту
    public float attackRange = 1.8f;    // Трохи більша дальність через спис
    public float attackCooldown = 2.0f; // Довша перезарядка між атаками
    public int damage = 25;             // Високий урон (Charge)
    public int maxHealth = 90;          // Середнє здоров'я
    // public int goldReward = 25;      // ВИДАЛЕНО: Тепер це в EnemyStats

    [Header("Навігація")]
    public LayerMask obstacleLayer; 
    public float avoidanceForce = 2.0f;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private int currentHealth;
    private int _maxHealth;
    private bool isDead = false;
    private Vector3 originalScale;

    private Transform target;
    private float nextAttackTime = 0f;

    // Кешування статів для системи контр-піків
    private UnitStats myStats;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        myStats = GetComponent<UnitStats>(); // Отримуємо категорію "Cavalry"

        originalScale = transform.localScale;

        // Налаштування фізики
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        // === БАЛАНС ===
        if (GameManager.Instance != null)
        {
            // Кавалерія отримує трохи менше HP від складності, ніж боси, але більше ніж гвардійці
            int difficultyHealth = GameManager.Instance.GetDifficultyHealth(); 
            maxHealth = Mathf.RoundToInt(difficultyHealth * 1.2f); 
            
            GameManager.Instance.RegisterEnemy();
        }

        currentHealth = maxHealth;
        _maxHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.targetTransform = transform;
            healthBar.SetHealth(currentHealth, _maxHealth);
        }
    }

    void Update()
    {
        if (isDead) return;

        // Очищення цілі, якщо вона зникла
        if (target != null && (target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy)) 
        {
            target = null;
        }

        if (target == null) FindTarget();

        // Логіка бою
        if (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.position);

            if (distance <= attackRange)
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
                MoveTowards(target.position);
            }
        }
        else
        {
            // Якщо цілей немає, просто біжимо вліво (до замку гравця)
            Vector3 forwardPos = transform.position + Vector3.left * 5f;
            MoveTowards(forwardPos);
        }
    }

    void FindTarget()
    {
        // Шукаємо найближчого ворога (Лицарі, Лучники, Списоносці, Замок)
        float minDistance = Mathf.Infinity;
        Transform closestTarget = null;

        void Check(Transform t)
        {
            if (t == null || !t.gameObject.activeInHierarchy || t.CompareTag("Untagged")) return;
            float dist = Vector2.Distance(transform.position, t.position);
            if (dist < minDistance) { minDistance = dist; closestTarget = t; }
        }

        // Пріоритет: спочатку юніти, потім будівлі
        Knight[] knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        foreach (var k in knights) Check(k.transform);

        Spearman[] spearmen = FindObjectsByType<Spearman>(FindObjectsSortMode.None);
        foreach (var s in spearmen) Check(s.transform);

        Archer[] archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        foreach (var a in archers) Check(a.transform);

        // Якщо нікого немає, атакуємо шипи або замок
        if (closestTarget == null && GameManager.Instance != null)
        {
            if (GameManager.Instance.currentSpikes != null) Check(GameManager.Instance.currentSpikes.transform);
            if (GameManager.Instance.castle != null) Check(GameManager.Instance.castle.transform);
        }

        target = closestTarget;
    }

    void MoveTowards(Vector3 destination)
    {
        if (animator) animator.SetBool("IsRunning", true);

        FaceDirection(destination);
        
        Vector2 direction = (destination - transform.position).normalized;

        // Простий обхід перешкод
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 1.5f, obstacleLayer);
        if (hit.collider != null)
        {
            direction += hit.normal * avoidanceForce;
            direction.Normalize();
        }

        rb.linearVelocity = direction * speed;
    }

    void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        if (animator) animator.SetBool("IsRunning", false);
    }

    void FaceDirection(Vector3 targetPos)
    {
        float absX = Mathf.Abs(originalScale.x);
        if (targetPos.x < transform.position.x)
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z);
    }

    void Attack()
    {
        // Запускаємо анімацію. Урон наноситься через Event "Hit" в анімації
        if (animator) animator.SetTrigger("Attack");
    }

    // === ЦЕЙ МЕТОД МАЄ БУТИ ВИКЛИКАНИЙ ЧЕРЕЗ ANIMATION EVENT ===
    public void Hit()
    {
        if (isDead || target == null) return;

        if (SoundManager.Instance != null) 
             SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit); 

        // === РОЗРАХУНОК УРОНУ (Cavalry vs ...) ===
        int finalDamage = damage;
        
        if (myStats != null)
        {
            UnitStats targetStats = target.GetComponent<UnitStats>();
            if (targetStats != null)
            {
                // Отримуємо бонус (наприклад x1.5 проти піхоти)
                float multiplier = GameManager.GetDamageMultiplier(myStats.category, targetStats.category);
                finalDamage = Mathf.RoundToInt(damage * multiplier);
            }
        }
        // ==========================================

        // Наносимо урон
        if (target.TryGetComponent<Knight>(out Knight k)) k.TakeDamage(finalDamage);
        else if (target.TryGetComponent<Archer>(out Archer a)) a.TakeDamage(finalDamage);
        else if (target.TryGetComponent<Spearman>(out Spearman s)) s.TakeDamage(finalDamage);
        else if (target.TryGetComponent<Castle>(out Castle c))
        {
             c.TakeDamage(finalDamage);
             if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.castleDamage);
        }
        else if (target.TryGetComponent<Spikes>(out Spikes sp)) sp.TakeDamage(finalDamage);
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;
        
        currentHealth -= damageAmount;
        
        if (healthBar != null) healthBar.SetHealth(currentHealth, _maxHealth);
        
        // Візуалізація
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

        // === ОНОВЛЕНО: ВИКОРИСТАННЯ EnemyStats ===
        if (TryGetComponent<EnemyStats>(out EnemyStats stats))
        {
            stats.GiveGold();
        }
        else
        {
            if (GameManager.Instance != null) GameManager.Instance.UnregisterEnemy();
        }
        // ==========================================

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);

        // Анімація смерті (поворот)
        if (animator) { animator.enabled = false; }
        transform.Rotate(0, 0, -90);
        if (spriteRenderer) { spriteRenderer.color = Color.gray; spriteRenderer.sortingOrder = 0; }

        Destroy(gameObject, 5f);
    }
}