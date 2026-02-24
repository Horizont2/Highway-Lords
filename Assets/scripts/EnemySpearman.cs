using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class EnemySpearman : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Характеристики")]
    public float speed = 1.8f;
    public float attackRange = 2.5f;
    public float attackCooldown = 1.5f;
    public int damage = 15;
    public int maxHealth = 70;

    [Header("Навігація")]
    public LayerMask obstacleLayer; // Постав 'Nothing' для початку!
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
    private UnitStats myStats;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        myStats = GetComponent<UnitStats>();

        originalScale = transform.localScale;

        // Налаштування фізики
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0; 
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; 

        // Баланс та реєстрація
        if (GameManager.Instance != null)
        {
            maxHealth = Mathf.RoundToInt(GameManager.Instance.GetDifficultyHealth() * 1.1f);
            damage = Mathf.RoundToInt(damage * Mathf.Pow(1.08f, GameManager.Instance.currentWave - 1));
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

        // 1. ПЕРЕВІРКА ЦІЛІ
        // Якщо ціль зникла або вимкнена - забуваємо її
        if (target != null && (!target.gameObject.activeInHierarchy))
        {
            target = null;
        }

        // 2. ПОШУК ЦІЛІ (Якщо її немає)
        if (target == null)
        {
            FindTarget();
        }

        // 3. ЛОГІКА РУХУ
        if (target != null)
        {
            // Якщо є кого бити (Лицар або Замок)
            float distance = Vector2.Distance(transform.position, target.position);
            
            // Повертаємось обличчям до цілі
            FaceDirection(target.position - transform.position);

            if (distance <= attackRange)
            {
                StopMoving();
                if (Time.time >= nextAttackTime)
                {
                    if (animator) animator.SetTrigger("Attack");
                    nextAttackTime = Time.time + attackCooldown;
                }
            }
            else
            {
                MoveTo(target.position);
            }
        }
        else
        {
            // 4. ЗАПАСНИЙ ПЛАН: Якщо цілей немає - йди просто ВЛІВО
            // Це гарантує, що він не буде стояти на спавні
            Vector3 leftDirection = Vector3.left; 
            FaceDirection(leftDirection);
            
            // Рухаємось в точку, яка зліва від нас
            MoveTo(transform.position + leftDirection * 5f);
        }
    }

    void FindTarget()
    {
        float minDistance = Mathf.Infinity;
        Transform closestTarget = null;

        void Check(Transform t)
        {
            if (t == null || !t.gameObject.activeInHierarchy) return;
            // Ігноруємо тег "Untagged", щоб випадково не скинути ціль
            
            float dist = Vector2.Distance(transform.position, t.position);
            if (dist < minDistance) { minDistance = dist; closestTarget = t; }
        }

        // Шукаємо захисників
        Knight[] knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        foreach (var k in knights) Check(k.transform);
        
        Spearman[] spearmen = FindObjectsByType<Spearman>(FindObjectsSortMode.None);
        foreach (var s in spearmen) Check(s.transform);

        Archer[] archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        foreach (var a in archers) Check(a.transform);

        // Якщо захисників немає - шукаємо ЗАМОК
        if (closestTarget == null)
        {
            // Спроба 1: Через GameManager
            if (GameManager.Instance != null && GameManager.Instance.castle != null)
            {
                Check(GameManager.Instance.castle.transform);
            }
            // Спроба 2: Пошук за тегом (Надійніше!)
            if (closestTarget == null)
            {
                GameObject castleObj = GameObject.FindGameObjectWithTag("Castle");
                if (castleObj == null) castleObj = GameObject.FindGameObjectWithTag("Player"); // Спробуй цей тег
                
                if (castleObj != null) Check(castleObj.transform);
            }
        }

        target = closestTarget;
    }

    void MoveTo(Vector3 destination)
    {
        if (animator) animator.SetBool("IsRunning", true);

        // Ігноруємо Z, щоб не йти "в глибину"
        Vector3 targetPosFixed = new Vector3(destination.x, destination.y, transform.position.z);
        Vector2 direction = (targetPosFixed - transform.position).normalized;
        
        // Raycast (тільки якщо шар обраний)
        if (obstacleLayer.value != 0) 
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 1.0f, obstacleLayer);
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                Vector2 avoidance = Vector2.Perpendicular(hit.normal) * avoidanceForce;
                direction += avoidance;
                direction.Normalize();
            }
        }

        // Рух через velocity
        rb.linearVelocity = direction * speed;
    }

    void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        if (animator) animator.SetBool("IsRunning", false);
    }

    void FaceDirection(Vector3 direction)
    {
        float absX = Mathf.Abs(originalScale.x);
        
        // Якщо рухаємось ВЛІВО (x < 0) -> Дивимось ВЛІВО
        // Якщо спрайт намальований вправо, то Scale X має бути мінусовим
        if (direction.x < -0.1f) 
        {
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z);
        }
        // Якщо рухаємось ВПРАВО (x > 0) -> Дивимось ВПРАВО
        else if (direction.x > 0.1f) 
        {
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z);
        }
    }

    public void Hit()
    {
        if (isDead || target == null) return;
        if (Vector2.Distance(transform.position, target.position) > attackRange + 1.0f) return;
        
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit);

        int finalDamage = damage;
        // Counter-Pick логіка тут...
        if (myStats != null)
        {
            UnitStats targetStats = target.GetComponent<UnitStats>();
            if (targetStats != null)
            {
                float multiplier = GameManager.GetDamageMultiplier(myStats.category, targetStats.category);
                finalDamage = Mathf.RoundToInt(damage * multiplier);
            }
        }

        // Нанесення урону
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
        
        if (animator) animator.enabled = false; 
        transform.Rotate(0, 0, -90);
        if (spriteRenderer) { spriteRenderer.color = Color.gray; spriteRenderer.sortingOrder = 0; }
        Destroy(gameObject, 5f);
    }
}