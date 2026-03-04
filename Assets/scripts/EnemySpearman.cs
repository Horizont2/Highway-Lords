using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class EnemySpearman : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Характеристики")]
    public float speed = 1.8f;
    public float attackRange = 0.7f;
    public float attackCooldown = 1.5f;
    public int damage = 15;
    public int maxHealth = 70;

    [Header("Запобігання стеку")]
    public LayerMask allyLayer; 
    public float stopDistance = 0.5f;

    [Header("Навігація та Агро")]
    public float aggroRadius = 4.5f;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private int currentHealth;
    private int _maxHealth;
    private bool isDead = false;
    private Vector3 originalScale;

    private Transform target;
    private Transform myCart;
    private float myLaneY;
    private float retargetTimer = 0f;
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

        Cart cartScript = FindFirstObjectByType<Cart>();
        if (cartScript != null) 
        {
            myCart = cartScript.transform;
        }
        
        float baseY = (myCart != null) ? myCart.position.y : transform.position.y;
        float[] laneOffsets = { 0f, -0.8f, -1.6f, -2.4f };
        myLaneY = baseY + laneOffsets[Random.Range(0, laneOffsets.Length)];

        if (animator != null) 
        {
            animator.speed = Random.Range(0.9f, 1.1f);
        }
        nextAttackTime = Time.time + Random.Range(0f, 0.3f);
    }

    void Update()
    {
        if (isDead) return;

        if (GameManager.Instance != null && GameManager.Instance.isDefeated)
        {
            target = null; 
            if (animator) 
            {
                animator.SetBool("IsRunning", true);
            }
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(-speed, 0f); 
            }
            return;
        }

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
            bool isStructure = target.TryGetComponent<Spikes>(out _) || target.TryGetComponent<Wall>(out _);
            bool inRange = false;

            if (isStructure) 
            {
                inRange = GetDistanceToStructure(target) <= attackRange;
            }
            else 
            {
                inRange = Mathf.Abs(transform.position.x - target.position.x) <= attackRange;
            }

            if (inRange)
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
                Vector3 dest = new Vector3(target.position.x, myLaneY, transform.position.z);
                MoveTo(dest);
            }
        }
        else
        {
            Vector3 leftDirection = new Vector3(transform.position.x - 5f, myLaneY, transform.position.z);
            FaceDirection(leftDirection); 
            MoveTo(leftDirection);
        }
    }

    void StopMoving() 
    { 
        rb.linearVelocity = Vector2.zero; 
        if (animator) 
        {
            animator.SetBool("IsRunning", false); 
        }
    }

    float GetDistanceToStructure(Transform t)
    {
        Collider2D targetCol = t.GetComponent<Collider2D>();
        Collider2D myCol = GetComponent<Collider2D>();
        if (targetCol != null && myCol != null) 
        { 
            ColliderDistance2D dist = Physics2D.Distance(myCol, targetCol); 
            if (dist.isValid) 
            {
                return dist.distance; 
            }
        }
        return Vector2.Distance(transform.position, t.position);
    }

    void FindTarget()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentSpikes != null)
        {
            Transform spikes = GameManager.Instance.currentSpikes.transform;
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
            if (dist < minDistance) 
            { 
                minDistance = dist; 
                closestTarget = t; 
            } 
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
        else if (GameManager.Instance != null && GameManager.Instance.castle != null) 
        {
            target = GameManager.Instance.castle.transform;
        }
    }

    void MoveTo(Vector3 destination)
    {
        if (Vector2.Distance(transform.position, destination) < 0.05f) 
        { 
            StopMoving(); 
            return; 
        }

        Vector2 direction = (destination - transform.position).normalized;
        Vector2 rayOrigin = transform.position + Vector3.up * 0.3f;
        Vector2 checkDir = new Vector2(direction.x, 0).normalized; 
        RaycastHit2D allyHit = Physics2D.CircleCast(rayOrigin, 0.3f, checkDir, stopDistance, allyLayer);
        
        if (allyHit.collider != null && allyHit.collider.gameObject != gameObject)
        {
            if (Mathf.Abs(allyHit.collider.transform.position.y - transform.position.y) < 0.4f) 
            { 
                StopMoving(); 
                return; 
            }
        }

        if (animator) 
        {
            animator.SetBool("IsRunning", true);
        }
        
        rb.linearVelocity = direction * speed;
    }

    void FaceDirection(Vector3 targetPos)
    {
        float absX = Mathf.Abs(originalScale.x);
        if (targetPos.x > transform.position.x) 
        {
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z);
        }
        else 
        {
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z);
        }
    }

    public void Hit() 
    {
        if (isDead || target == null || hasHitThisAttack) return;
        
        hasHitThisAttack = true;
        if (SoundManager.Instance != null) 
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit); 
        }

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
        else if (target.TryGetComponent<Wall>(out Wall c)) 
        { 
            c.TakeDamage(finalDamage); 
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.castleDamage); 
        }
        else if (target.TryGetComponent<Spikes>(out Spikes sp)) 
        {
            sp.TakeDamage(finalDamage);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;
        currentHealth -= damageAmount;
        
        if (healthBar != null) 
        {
            healthBar.SetHealth(currentHealth, _maxHealth);
        }
        
        GameManager.CreateDamagePopup(transform.position, damageAmount);
        
        if (currentHealth <= 0) 
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true; 
        gameObject.tag = "Untagged";
        
        StopMoving(); 
        rb.bodyType = RigidbodyType2D.Static;
        
        Collider2D col = GetComponent<Collider2D>(); 
        if (col != null) 
        {
            col.enabled = false;
        }
        
        if (TryGetComponent<EnemyStats>(out EnemyStats stats)) 
        {
            stats.GiveGold();
        }
        else if (GameManager.Instance != null) 
        {
            GameManager.Instance.UnregisterEnemy();
        }
        
        if (SoundManager.Instance != null) 
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);
        }
        
        if (animator) 
        { 
            animator.Rebind(); 
            animator.Update(0f); 
            animator.enabled = false; 
        }
        
        transform.Rotate(0, 0, -90);
        
        if (spriteRenderer != null) 
        { 
            spriteRenderer.color = Color.gray; 
            spriteRenderer.sortingOrder = 0; 
        }
        
        Destroy(this); 
        Destroy(gameObject, 10f);
    }
}