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

    [Header("Навігація та Агро")]
    public LayerMask obstacleLayer; 
    public float avoidanceForce = 2.0f;
    public float aggroRadius = 4.5f;
    private float retargetTimer = 0f;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private int currentHealth;
    private int _maxHealth;
    private bool isDead = false;
    private Vector3 originalScale;

    private Transform target;
    private float nextAttackTime = 0f;
    private bool hasHitThisAttack = false;
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

        if (target != null && (!target.gameObject.activeInHierarchy || target.CompareTag("Untagged")))
        {
            target = null;
        }

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f)
        {
            FindTarget();
            retargetTimer = 0.25f;
        }

        if (target != null)
        {
            FaceDirection(target.position);

            bool isStructure = target.TryGetComponent<Spikes>(out _) || target.TryGetComponent<Castle>(out _);
            float distanceToTarget;

            // ФІКС 1: Міряємо відстань до КРАЮ колайдера, а не до центру
            if (isStructure)
            {
                Collider2D targetCol = target.GetComponent<Collider2D>();
                if (targetCol != null) 
                {
                    distanceToTarget = Vector2.Distance(transform.position, targetCol.ClosestPoint(transform.position));
                }
                else 
                {
                    distanceToTarget = Mathf.Abs(transform.position.x - target.position.x);
                }
            }
            else
            {
                distanceToTarget = Vector2.Distance(transform.position, target.position);
            }

            if (distanceToTarget <= attackRange)
            {
                StopMoving();
                if (Time.time >= nextAttackTime)
                {
                    hasHitThisAttack = false;
                    if (animator) animator.SetTrigger("Attack");
                    nextAttackTime = Time.time + attackCooldown;
                }
            }
            else
            {
                MoveTo(target.position, isStructure);
            }
        }
        else
        {
            Vector3 leftDirection = transform.position + Vector3.left * 5f; 
            FaceDirection(leftDirection);
            MoveTo(leftDirection, false);
        }
    }

    void FindTarget()
    {
        // Барикада - пріоритет №1
        if (GameManager.Instance != null && GameManager.Instance.currentSpikes != null)
        {
            Transform spikes = GameManager.Instance.currentSpikes.transform;
            // Атакуємо її, поки не пройдемо її повністю
            if (transform.position.x > spikes.position.x - 2.0f)
            {
                target = spikes;
                return;
            }
        }

        float minDistance = Mathf.Infinity;
        Transform closestTarget = null;

        void Check(Transform t)
        {
            if (t == null || !t.gameObject.activeInHierarchy || t.CompareTag("Untagged")) return;
            float dist = Vector2.Distance(transform.position, t.position);
            if (dist < minDistance) { minDistance = dist; closestTarget = t; }
        }

        Knight[] knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        foreach (var k in knights) Check(k.transform);
        
        Spearman[] spearmen = FindObjectsByType<Spearman>(FindObjectsSortMode.None);
        foreach (var s in spearmen) Check(s.transform);

        Archer[] archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        foreach (var a in archers) Check(a.transform);

        if (closestTarget != null && minDistance <= aggroRadius)
        {
            target = closestTarget;
        }
        else
        {
            if (GameManager.Instance != null && GameManager.Instance.castle != null)
                target = GameManager.Instance.castle.transform;
        }
    }

    void MoveTo(Vector3 destination, bool isStructureTarget)
    {
        if (animator) animator.SetBool("IsRunning", true);

        Vector3 targetPosFixed = new Vector3(destination.x, destination.y, transform.position.z);
        
        // ФІКС 2: Не ковзаємо по осі Y, якщо ціль - барикада
        if (isStructureTarget)
        {
            targetPosFixed = new Vector3(destination.x, transform.position.y, transform.position.z);
        }

        Vector2 direction = (targetPosFixed - transform.position).normalized;
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 1.5f, obstacleLayer);
        if (hit.collider != null && hit.collider.gameObject != gameObject)
        {
            bool hitMyTarget = false;
            if (target != null)
            {
                if (hit.collider.transform == target || hit.collider.transform.IsChildOf(target))
                {
                    hitMyTarget = true;
                }
            }

            // Відштовхуємось ТІЛЬКИ якщо це не наша ціль
            if (!hitMyTarget)
            {
                float dodgeDirY = (transform.position.y >= hit.collider.bounds.center.y) ? 1f : -1f;
                Vector2 avoidance = new Vector2(0, dodgeDirY); 
                direction += avoidance * avoidanceForce;
                direction.Normalize(); 
            }
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
        if (targetPos.x > transform.position.x) transform.localScale = new Vector3(absX, originalScale.y, originalScale.z);
        else transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z);
    }

    public void Hit() 
    {
        if (isDead || target == null) return;
        if (hasHitThisAttack) return;
        
        bool isStructure = target.TryGetComponent<Spikes>(out _) || target.TryGetComponent<Castle>(out _);
        float distanceToTarget;

        if (isStructure)
        {
            Collider2D targetCol = target.GetComponent<Collider2D>();
            if (targetCol != null) 
            {
                distanceToTarget = Vector2.Distance(transform.position, targetCol.ClosestPoint(transform.position));
            }
            else 
            {
                distanceToTarget = Mathf.Abs(transform.position.x - target.position.x);
            }
        }
        else
        {
            distanceToTarget = Vector2.Distance(transform.position, target.position);
        }

        // ФІКС 3: Даємо допуск +1.5f. Якщо інший ворог штовхнув цього ззаду під час анімації, удар все одно пройде!
        if (distanceToTarget > attackRange + 1.5f) return;

        hasHitThisAttack = true;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit); 

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
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; }
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        if (TryGetComponent<EnemyStats>(out EnemyStats stats)) stats.GiveGold();
        else if (GameManager.Instance != null) GameManager.Instance.UnregisterEnemy();
        
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);
        
        if (animator) { animator.Rebind(); animator.Update(0f); animator.enabled = false; }
        transform.Rotate(0, 0, -90);
        if (spriteRenderer != null) { spriteRenderer.color = Color.gray; spriteRenderer.sortingOrder = 0; }
        
        Destroy(this);
        Destroy(gameObject, 10f);
    }
}