using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Archer : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Характеристики")]
    public float speed = 2.5f;
    public float attackRange = 6.0f; 
    public float attackRate = 0.8f;
    public int maxHealth = 80;

    [Header("Запобігання стеку")]
    public LayerMask allyLayer; 
    public float stopDistance = 0.6f;

    [Header("Навігація")]
    public LayerMask obstacleLayer; 
    public float checkDistance = 1.5f; 
    public float avoidanceForce = 2.0f; 

    [Header("Стрільба")]
    public GameObject arrowPrefab;
    public Transform firePoint;

    [Header("Компоненти")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    public int currentHealth;
    private int myDamage;
    private float nextAttackTime = 0f;
    private Vector3 originalScale;
    private Transform target; 
    private Rigidbody2D rb; 
    private bool isDead = false;

    private Vector3 startPoint; 
    private UnitStats myStats;
    private float retargetTimer = 0f;
    private Vector3 formationPos;

    public void SetFormationPosition(Vector3 pos) 
    { 
        formationPos = pos; 
    }

    public void LoadState(int savedHealth)
    {
        currentHealth = savedHealth;
        if (healthBar != null) 
        {
            healthBar.SetHealth(currentHealth, maxHealth);
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic; 
        rb.gravityScale = 0; 
        rb.freezeRotation = true; 

        if(spriteRenderer == null) 
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        if(animator == null) 
        {
            animator = GetComponent<Animator>(); 
        }
        
        myStats = GetComponent<UnitStats>();

        startPoint = transform.position; 
        if (formationPos == Vector3.zero) 
        {
            formationPos = startPoint;
        }
        
        originalScale = transform.localScale;

        if (currentHealth <= 0) 
        {
            currentHealth = maxHealth;
        }
        
        if (healthBar != null) 
        { 
            healthBar.targetTransform = transform; 
            healthBar.SetHealth(currentHealth, maxHealth); 
        }

        if (GameManager.Instance != null)
        {
            myDamage = GameManager.Instance.GetArcherDamage();
            if (GameManager.Instance.archerLevel > 1 && spriteRenderer != null) 
            {
                spriteRenderer.color = new Color(0.9f, 1f, 0.9f);
            }
        }
        else 
        {
            myDamage = 8;
        }

        if (animator != null) 
        {
            animator.speed = Random.Range(0.9f, 1.1f);
        }
        
        nextAttackTime = Time.time + Random.Range(0f, 0.3f);
    }

    void Update()
    {
        if (isDead) return;
        
        if (GameManager.Instance != null) 
        {
            myDamage = GameManager.Instance.GetArcherDamage();
        }
        
        if (target != null && (target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy)) 
        {
            target = null;
        }

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f) 
        { 
            FindNearestTarget(); 
            retargetTimer = 0.25f; 
        }

        if (target != null) 
        {
            EngageEnemy(target);
        }
        else
        {
            MoveTo(formationPos); 
            if (Vector2.Distance(transform.position, formationPos) < 0.1f) 
            {
                FlipSprite(transform.position.x + 1f);
            }
        }
    }

    void StopMoving() 
    { 
        rb.linearVelocity = Vector2.zero; 
        if (animator) 
        {
            animator.SetBool("IsMoving", false); 
        }
    }

    float GetDistanceToTarget(Transform t)
    {
        Collider2D targetCol = t.GetComponent<Collider2D>();
        Collider2D myCol = GetComponent<Collider2D>();
        if (targetCol != null && myCol != null)
        {
            ColliderDistance2D dist = Physics2D.Distance(myCol, targetCol);
            if (dist.isValid) return dist.distance;
        }
        return Vector2.Distance(transform.position, t.position);
    }

    void EngageEnemy(Transform targetTransform)
    {
        FlipSprite(targetTransform.position.x);
        float distanceToTarget = GetDistanceToTarget(targetTransform);

        if (distanceToTarget <= attackRange)
        {
            StopMoving();
            if (Time.time >= nextAttackTime) 
            { 
                StartAttack(); 
                nextAttackTime = Time.time + 1f / attackRate; 
            }
        }
        else
        {
            MoveTo(targetTransform.position);
        }
    }

    void StartAttack() 
    { 
        if (animator != null) 
        {
            animator.SetTrigger("Attack"); 
        }
    }

    public void ShootArrow() 
    {
        if (isDead || target == null || target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy) return;

        float dist = GetDistanceToTarget(target);
        if (dist > attackRange + 1.5f) return; 

        if (arrowPrefab != null && firePoint != null)
        {
            if (SoundManager.Instance != null) 
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot);
            }

            GameObject arrowGO = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
            Arrow arrowScript = arrowGO.GetComponent<Arrow>();
            
            if (arrowScript != null)
            {
                int finalDamage = myDamage;
                if (myStats != null)
                {
                    UnitStats targetStats = target.GetComponent<UnitStats>();
                    if (targetStats != null) 
                    {
                        float multiplier = GameManager.GetDamageMultiplier(myStats.category, targetStats.category);
                        finalDamage = Mathf.RoundToInt(myDamage * multiplier);
                    }
                }
                arrowScript.Initialize(target, finalDamage);
            }
        }
    }

    void MoveTo(Vector3 targetPosition)
    {
        if (Vector2.Distance(transform.position, targetPosition) < 0.05f) 
        { 
            StopMoving(); 
            return; 
        }

        FlipSprite(targetPosition.x);
        Vector2 direction = (targetPosition - transform.position).normalized;

        if (targetPosition != formationPos)
        {
            Vector2 rayOrigin = transform.position + Vector3.up * 0.3f;
            RaycastHit2D allyHit = Physics2D.CircleCast(rayOrigin, 0.3f, direction, stopDistance, allyLayer);
            
            if (allyHit.collider != null && allyHit.collider.gameObject != gameObject)
            {
                StopMoving();
                return;
            }
        }

        if (animator) 
        {
            animator.SetBool("IsMoving", true);
        }
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position + Vector3.up * 0.3f, direction, checkDistance, obstacleLayer);
        if (hit.collider != null && hit.collider.gameObject != gameObject)
        {
            float dodgeDirY = (transform.position.y >= hit.collider.bounds.center.y) ? 1f : -1f;
            Vector2 avoidance = new Vector2(0, dodgeDirY); 
            direction += avoidance * avoidanceForce;
            direction.Normalize(); 
        }

        rb.linearVelocity = direction * speed;
    }

    void FlipSprite(float targetX)
    {
        float absX = Mathf.Abs(originalScale.x);
        if (targetX > transform.position.x) 
        {
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z); 
        }
        else if (targetX < transform.position.x) 
        {
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z); 
        }
    }

    void FindNearestTarget()
    {
        float shortestDist = Mathf.Infinity;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;

        foreach (GameObject go in enemies)
        {
            if (go.CompareTag("Untagged")) continue;
            
            if (GameManager.Instance != null && GameManager.Instance.engagementLine != null) 
            {
                if (go.transform.position.x > GameManager.Instance.engagementLine.position.x) continue;
            }

            float dist = Vector2.Distance(transform.position, go.transform.position);
            
            if (dist < shortestDist) 
            { 
                shortestDist = dist; 
                nearest = go.transform; 
            }
        }
        target = nearest;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        
        if (healthBar != null) 
        {
            healthBar.SetHealth(currentHealth, maxHealth);
        }
        
        GameManager.CreateDamagePopup(transform.position, damage);
        
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
        
        if (GameManager.Instance != null && !GameManager.Instance.isResettingUnits) 
        {
            GameManager.Instance.OnUnitDeath(gameObject, "Archer"); 
        }
        
        if (healthBar != null) 
        {
            healthBar.gameObject.SetActive(false);
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