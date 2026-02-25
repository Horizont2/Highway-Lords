using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Archer : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Характеристики")]
    public float speed = 2.5f;
    public float attackRange = 5f; 
    public float attackRate = 0.8f;
    public int maxHealth = 80;

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

    // МЕТОД ДЛЯ ЗАВАНТАЖЕННЯ ЗБЕРЕЖЕННЯ
    public void LoadState(int savedHealth)
    {
        currentHealth = savedHealth;
        if (healthBar != null) healthBar.SetHealth(currentHealth, maxHealth);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic; 
        rb.gravityScale = 0; 
        rb.freezeRotation = true; 

        if(spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if(animator == null) animator = GetComponent<Animator>(); 
        myStats = GetComponent<UnitStats>();

        startPoint = transform.position;
        if (formationPos == Vector3.zero) formationPos = startPoint;
        originalScale = transform.localScale;

        if (currentHealth <= 0) currentHealth = maxHealth;
        
        if (healthBar != null)
        {
            healthBar.targetTransform = transform;
            healthBar.SetHealth(currentHealth, maxHealth);
        }

        if (GameManager.Instance != null)
        {
            myDamage = GameManager.Instance.GetArcherDamage();
            if (GameManager.Instance.archerLevel > 1 && spriteRenderer != null) 
                spriteRenderer.color = new Color(0.9f, 1f, 0.9f);
        }
        else myDamage = 8;
    }

    void Update()
    {
        if (isDead) return;
        
        if (GameManager.Instance != null)
        {
            myDamage = GameManager.Instance.GetArcherDamage();
        }
        
        if (target != null && (target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy)) 
            target = null;

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
            MoveTo(formationPos); // Повернення в ширенгу
        }
    }

    void EngageEnemy(Transform targetTransform)
    {
        float distance = Vector2.Distance(transform.position, targetTransform.position);
        FlipSprite(targetTransform.position.x);

        if (distance <= attackRange)
        {
            rb.linearVelocity = Vector2.zero; 
            if (animator) animator.SetBool("IsMoving", false);

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
        if (animator != null) animator.SetTrigger("Attack");
    }

    public void ShootArrow() // Викликається анімацією
    {
        if (isDead) return;
        if (target == null || target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy) return;

        float dist = Vector2.Distance(transform.position, target.position);
        if (dist > attackRange + 1.5f) return; 

        if (arrowPrefab != null && firePoint != null)
        {
            if (SoundManager.Instance != null) 
                SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot);

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
        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator) animator.SetBool("IsMoving", false);
            return;
        }

        if (animator) animator.SetBool("IsMoving", true);
        FlipSprite(targetPosition.x);

        Vector2 direction = (targetPosition - transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, checkDistance, obstacleLayer);
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
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z); 
        else if (targetX < transform.position.x) 
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z); 
    }

    void FindNearestTarget()
    {
        float shortestDist = Mathf.Infinity;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;

        foreach (GameObject go in enemies)
        {
            if (go.CompareTag("Untagged")) continue;
            float dist = Vector2.Distance(transform.position, go.transform.position);
            
            if (dist < shortestDist && dist <= attackRange * 2.0f) 
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
        if (healthBar != null) healthBar.SetHealth(currentHealth, maxHealth);
        GameManager.CreateDamagePopup(transform.position, damage);
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

        if (GameManager.Instance != null && !GameManager.Instance.isResettingUnits) 
        { 
            GameManager.Instance.OnUnitDeath(gameObject, "Archer"); 
        }
        
        if (healthBar != null) healthBar.gameObject.SetActive(false);

        if (animator)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.enabled = false;
        }

        transform.Rotate(0, 0, -90);
        if (spriteRenderer != null) { spriteRenderer.color = Color.gray; spriteRenderer.sortingOrder = 0; }
        
        Destroy(this);
        Destroy(gameObject, 10f);
    }
}