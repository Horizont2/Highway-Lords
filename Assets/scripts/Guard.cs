using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))] // Автоматично додасть скрипт статистики
public class Guard : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar; 

    [Header("Характеристики")]
    public float speed = 1.5f;
    public float attackRange = 1.2f; 
    public int damage = 15;
    public int health = 60; // Початкове здоров'я
    // public int goldReward = 15; // ВИДАЛЕНО: Тепер це в EnemyStats

    [Header("Навігація (Обхід)")]
    public LayerMask obstacleLayer; 
    public float avoidanceForce = 2.0f;

    [Header("Атака")]
    public float attackCooldown = 1.5f;
    private float nextAttackTime = 0f;

    private Animator animator;
    private Rigidbody2D rb; 
    private SpriteRenderer spriteRenderer;
    private int _maxHealth; 
    private bool isDead = false;
    private Vector3 originalScale;

    private Transform target; 
    private Transform myCart;
    private float laneOffset; 
    private float cartSafetyRadius = 2.5f; 

    // Кешування статів
    private UnitStats myStats;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        myStats = GetComponent<UnitStats>();

        originalScale = transform.localScale;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        float randomDir = Random.value > 0.5f ? 1f : -1f;
        laneOffset = Random.Range(0.8f, 1.5f) * randomDir;

        if (GameManager.Instance != null)
        {
            health = GameManager.Instance.GetDifficultyHealth();
            GameManager.Instance.RegisterEnemy();
        }

        _maxHealth = health;

        if (healthBar != null)
        {
            healthBar.targetTransform = transform; 
            healthBar.SetHealth(health, _maxHealth); 
        }

        Cart cartScript = FindFirstObjectByType<Cart>();
        if (cartScript != null) myCart = cartScript.transform;
    }

    void Update()
    {
        if (isDead) return;

        if (target == null || target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy) 
        {
            target = null;
            FindTarget();
        }

        // 1. АТАКА
        if (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.position);

            if (distance <= attackRange)
            {
                StopMoving();
                if (Time.time >= nextAttackTime)
                {
                    StartAttack(); 
                    nextAttackTime = Time.time + attackCooldown;
                }
                return; 
            }
        }

        // 2. УХИЛЕННЯ ВІД ВОЗА
        if (IsCartTooClose())
        {
            DodgeCart();
            return; 
        }

        // 3. РУХ
        if (target != null)
        {
            FaceTarget(target.position);
            
            Vector3 dest = target.position;
            float distance = Vector2.Distance(transform.position, target.position);

            if (distance > 3.5f) 
            {
                dest.y += laneOffset;
            }
            
            MoveTowards(dest);
        }
        else
        {
            float baseY = (myCart != null) ? myCart.position.y : 0;
            Vector3 destination = transform.position + Vector3.left;
            destination.y = baseY + laneOffset;
            
            FaceTarget(destination); 
            MoveTowards(destination);
        }
    }

    void FindTarget()
    {
        float minDistance = Mathf.Infinity;
        Transform closestTarget = null;

        void CheckDistance(Transform t)
        {
            if (t == null || !t.gameObject.activeInHierarchy || t.CompareTag("Untagged")) return;
            float dist = Vector2.Distance(transform.position, t.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestTarget = t;
            }
        }

        Knight[] knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        foreach (Knight k in knights) CheckDistance(k.transform);

        Archer[] archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        foreach (Archer a in archers) CheckDistance(a.transform);
        
        Spearman[] spearmen = FindObjectsByType<Spearman>(FindObjectsSortMode.None); 
        foreach (Spearman s in spearmen) CheckDistance(s.transform);

        if (GameManager.Instance != null && GameManager.Instance.currentSpikes != null)
            CheckDistance(GameManager.Instance.currentSpikes.transform);

        if (closestTarget == null && GameManager.Instance != null && GameManager.Instance.castle != null)
        {
            closestTarget = GameManager.Instance.castle.transform;
        }
        
        target = closestTarget;
    }

    void MoveTowards(Vector3 destination)
    {
        if (animator) animator.SetBool("IsRunning", true);
        
        Vector2 direction = (destination - transform.position).normalized;

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

    bool IsCartTooClose()
    {
        if (myCart == null) return false;
        float dist = Vector2.Distance(transform.position, myCart.position);
        if (dist < cartSafetyRadius)
        {
            if (Mathf.Abs(transform.position.y - myCart.position.y) < 1.0f) return true;
        }
        return false;
    }

    void DodgeCart()
    {
        if (myCart == null) return;
        float dirY = (transform.position.y > myCart.position.y) ? 1f : -1f;
        Vector2 dodgeVector = new Vector2(-0.5f, dirY).normalized; 
        rb.linearVelocity = dodgeVector * (speed * 1.5f);
        if (animator) animator.SetBool("IsRunning", true);
    }

    void FaceTarget(Vector3 targetPos)
    {
        float absX = Mathf.Abs(originalScale.x);
        if (targetPos.x > transform.position.x)
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z); 
        else
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z); 
    }

    void StartAttack()
    {
        hasHitThisAttack = false;
        if (animator) animator.SetTrigger("Attack");
    }

    private bool hasHitThisAttack = false;

    public void Hit()
    {
        if (isDead) return;
        if (target == null) return; 
        if (hasHitThisAttack) return;

        hasHitThisAttack = true;

        if (Vector2.Distance(transform.position, target.position) > attackRange + 0.5f) return;

        if (SoundManager.Instance != null) 
             SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit);

        // === РОЗРАХУНОК УРОНУ ===
        int finalDamage = damage;
        
        if (myStats != null)
        {
            UnitStats targetStats = target.GetComponent<UnitStats>();
            if (targetStats != null)
            {
                float multiplier = GameManager.GetDamageMultiplier(myStats.category, targetStats.category);
                finalDamage = Mathf.RoundToInt(damage * multiplier);
            }
        }

        if (target.TryGetComponent<Knight>(out Knight k)) k.TakeDamage(finalDamage);
        else if (target.TryGetComponent<Archer>(out Archer a)) a.TakeDamage(finalDamage);
        else if (target.TryGetComponent<Spearman>(out Spearman s)) s.TakeDamage(finalDamage);
        else if (target.TryGetComponent<Castle>(out Castle c))
        {
            c.TakeDamage(finalDamage);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.1f, 0.2f); 
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.castleDamage);
        }
        else if (target.TryGetComponent<Spikes>(out Spikes spikes))
        {
            spikes.TakeDamage(finalDamage);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.05f, 0.1f);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;
        health -= damageAmount;
        if (healthBar != null) healthBar.SetHealth(health, _maxHealth);
        
        GameManager.CreateDamagePopup(transform.position, damageAmount);
        
        if (health <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        
        gameObject.tag = "Untagged"; 

        if (healthBar != null) healthBar.gameObject.SetActive(false);
        Transform shadow = transform.Find("Shadow");
        if (shadow != null) shadow.gameObject.SetActive(false);

        // === ОНОВЛЕНО: ВИКОРИСТАННЯ EnemyStats ===
        // Викликаємо метод GiveGold(), який сам нарахує гроші, покаже Popup і зніме ворога з обліку
        if (TryGetComponent<EnemyStats>(out EnemyStats stats))
        {
            stats.GiveGold();
        }
        else
        {
            // Резервний варіант, якщо забули додати скрипт EnemyStats
            if (GameManager.Instance != null) GameManager.Instance.UnregisterEnemy();
        }
        // ==========================================
        
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);

        if (animator)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.enabled = false;
        }

        if (rb) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; }
        
        Collider2D col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        transform.Rotate(0, 0, -90);
        if (spriteRenderer != null) { spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f); spriteRenderer.sortingOrder = 0; }

        this.enabled = false;
        Destroy(gameObject, 5f);
    }
}