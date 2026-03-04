using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))] 
public class EnemyArcher : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Характеристики")]
    public float speed = 2.5f;
    public float attackRange = 6.0f;
    public float timeBetweenShots = 2.0f;
    public int damage = 10;
    public int maxHealth = 40; 
    
    [Header("Запобігання стеку")]
    public LayerMask allyLayer; 
    public float stopDistance = 0.5f;

    [Header("Навігація та Агро")]
    public LayerMask obstacleLayer; 
    public float avoidanceForce = 2.0f;
    public float aggroRadius = 8.0f; 
    private float retargetTimer = 0f;

    [Header("Стрільба")]
    public GameObject arrowPrefab;
    public Transform firePoint;

    private int currentHealth;
    private Animator animator;
    private Transform target;
    private float nextShotTime;
    
    private Vector3 originalScale;
    private int _maxHealth;
    private Rigidbody2D rb;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;

    private Transform myCart;
    private float myLaneY;
    private UnitStats myStats;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        myStats = GetComponent<UnitStats>();
        originalScale = transform.localScale;

        if (GameManager.Instance != null)
        {
            maxHealth = GameManager.Instance.GetDifficultyHealth();
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
        nextShotTime = Time.time + Random.Range(0f, 0.5f);
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

        if (target != null && (target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy)) 
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
            FaceTarget(target.position);
            float distX = Mathf.Abs(transform.position.x - target.position.x);

            if (distX <= attackRange)
            {
                if (Mathf.Abs(transform.position.y - myLaneY) > 0.1f)
                {
                    Vector3 dest = new Vector3(transform.position.x, myLaneY, transform.position.z);
                    MoveTowards(dest);
                }
                else
                {
                    StopMoving();
                    AimAtTarget();
                    if (Time.time > nextShotTime) 
                    { 
                        if (animator != null) animator.SetTrigger("Attack"); 
                        nextShotTime = Time.time + timeBetweenShots; 
                    }
                }
            }
            else
            {
                Vector3 dest = new Vector3(target.position.x, myLaneY, transform.position.z);
                MoveTowards(dest);
            }
        }
        else
        {
            Vector3 destination = new Vector3(transform.position.x - 5f, myLaneY, transform.position.z);
            FaceTarget(destination); 
            MoveTowards(destination);
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

    void FindTarget()
    {
        float minDistance = Mathf.Infinity; 
        Transform closestUnit = null;

        void CheckDistance(Transform t)
        {
            if (t == null || !t.gameObject.activeInHierarchy || t.CompareTag("Untagged")) return;
            float dist = Vector2.Distance(transform.position, t.position);
            if (dist < minDistance) { minDistance = dist; closestUnit = t; }
        }

        Knight[] knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        foreach (Knight k in knights) CheckDistance(k.transform);

        Archer[] archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        foreach (Archer a in archers) CheckDistance(a.transform);
        
        Spearman[] spearmen = FindObjectsByType<Spearman>(FindObjectsSortMode.None); 
        foreach (Spearman s in spearmen) CheckDistance(s.transform);

        if (GameManager.Instance != null && GameManager.Instance.currentSpikes != null) 
        {
            CheckDistance(GameManager.Instance.currentSpikes.transform);
        }

        if (closestUnit != null && minDistance <= aggroRadius) 
        {
            target = closestUnit;
        }
        else if (GameManager.Instance != null && GameManager.Instance.castle != null) 
        {
            target = GameManager.Instance.castle.transform;
        }
        else 
        {
            target = null;
        }
    }

    public void ShootArrow()
    {
        if (isDead || target == null || target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy) return;

        if (arrowPrefab != null && firePoint != null)
        {
            AimAtTarget(); 
            GameObject arrowObj = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);
            EnemyProjectile p = arrowObj.GetComponent<EnemyProjectile>();
            
            if (p != null)
            {
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
                p.Initialize(target.position, finalDamage);
            }
            
            if (SoundManager.Instance != null) 
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot);
            }
        }
    }

    void MoveTowards(Vector3 destination)
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

    void FaceTarget(Vector3 targetPos)
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

    void AimAtTarget()
    {
        if (firePoint != null && target != null)
        {
            Vector3 direction = target.position - firePoint.position;
            if (transform.localScale.x < 0) 
            { 
                direction.x = -direction.x; 
                direction.y = -direction.y; 
            }
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            firePoint.localRotation = Quaternion.Euler(0, 0, angle);
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
        
        if (healthBar != null) 
        {
            healthBar.gameObject.SetActive(false);
        }
        
        Transform shadow = transform.Find("Shadow"); 
        if (shadow != null) 
        {
            shadow.gameObject.SetActive(false);
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
        
        this.enabled = false; 
        Destroy(gameObject, 5f);
    }
}