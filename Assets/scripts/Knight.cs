using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Knight : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Характеристики")]
    public float speed = 3.0f;
    public float attackRange = 0.2f; 
    public float attackRate = 1f;
    public int maxHealth = 120;

    [Header("Запобігання стеку")]
    public LayerMask allyLayer; 
    public float stopDistance = 0.6f;

    [Header("Навігація (Обхід)")]
    public LayerMask obstacleLayer; 
    public float avoidanceForce = 2.0f; 
    public float checkDistance = 1.5f;

    [Header("Компоненти")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    public int currentHealth; 
    
    private int myDamage;
    private float nextAttackTime = 0f;
    private Vector3 originalScale;

    private Guard targetGuard;
    private EnemySpearman targetSpearman;
    private EnemyHorse targetHorse;
    private EnemyArcher targetArcher; 
    private Boss targetBoss; 

    private Vector3 startPoint;
    private Rigidbody2D rb; 
    private bool isDead = false;

    private float retargetTimer = 0f;
    private Vector3 formationPos;
    private UnitStats myStats;

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
            GameManager.Instance.UpdateUI();
            myDamage = GameManager.Instance.GetKnightDamage();
            if (GameManager.Instance.knightLevel > 1 && spriteRenderer != null) 
            {
                spriteRenderer.color = new Color(1f, 0.9f, 0.9f);
            }
        }
        else 
        {
            myDamage = 10;
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
            myDamage = GameManager.Instance.GetKnightDamage();
        }

        if (targetBoss != null && (targetBoss.CompareTag("Untagged") || !targetBoss.gameObject.activeInHierarchy)) targetBoss = null;
        if (targetHorse != null && (targetHorse.CompareTag("Untagged") || !targetHorse.gameObject.activeInHierarchy)) targetHorse = null;
        if (targetGuard != null && (targetGuard.CompareTag("Untagged") || !targetGuard.gameObject.activeInHierarchy)) targetGuard = null;
        if (targetSpearman != null && (targetSpearman.CompareTag("Untagged") || !targetSpearman.gameObject.activeInHierarchy)) targetSpearman = null;
        if (targetArcher != null && (targetArcher.CompareTag("Untagged") || !targetArcher.gameObject.activeInHierarchy)) targetArcher = null;

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f)
        {
            FindNearestTarget();
            retargetTimer = 0.25f;
        }

        Transform currentTarget = null;
        if (targetBoss != null) currentTarget = targetBoss.transform; 
        else if (targetHorse != null) currentTarget = targetHorse.transform;
        else if (targetGuard != null) currentTarget = targetGuard.transform;
        else if (targetSpearman != null) currentTarget = targetSpearman.transform;
        else if (targetArcher != null) currentTarget = targetArcher.transform;

        if (currentTarget != null)
        {
            EngageEnemy(currentTarget);
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

    void EngageEnemy(Transform target)
    {
        FlipSprite(target.position.x);
        float distanceToTarget = GetDistanceToTarget(target);

        if (distanceToTarget <= attackRange)
        {
            StopMoving();
            if (Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
        else
        {
            MoveTo(target.position);
        }
    }

    void MoveTo(Vector3 targetPosition)
    {
        if (Vector2.Distance(transform.position, targetPosition) < 0.05f)
        {
            StopMoving();
            return;
        }

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.rightBoundary != null && targetPosition.x > GameManager.Instance.rightBoundary.position.x)
            {
                targetPosition.x = GameManager.Instance.rightBoundary.position.x;
            }
            if (GameManager.Instance.leftBoundary != null && targetPosition.x < GameManager.Instance.leftBoundary.position.x)
            {
                targetPosition.x = GameManager.Instance.leftBoundary.position.x;
            }
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
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z); 
        }
        else if (targetX < transform.position.x) 
        {
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z); 
        }
    }

    void FindNearestTarget()
    {
        targetBoss = null; 
        targetHorse = null; 
        targetGuard = null; 
        targetSpearman = null; 
        targetArcher = null;

        float minX = -1000f; 
        float maxX = 1000f;
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.leftBoundary) minX = GameManager.Instance.leftBoundary.position.x - 2f;
            if (GameManager.Instance.rightBoundary) maxX = GameManager.Instance.rightBoundary.position.x + 2f;
        }

        float shortestDist = Mathf.Infinity;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        foreach (GameObject go in enemies)
        {
            if (go == gameObject) continue;
            if (go.CompareTag("Untagged")) continue;
            
            if (GameManager.Instance != null && GameManager.Instance.engagementLine != null)
            {
                if (go.transform.position.x > GameManager.Instance.engagementLine.position.x) continue;
            }
            
            if (go.transform.position.x > maxX || go.transform.position.x < minX) continue;

            float dist = Vector2.Distance(transform.position, go.transform.position);

            if (go.GetComponent<Boss>()) 
            { 
                if (dist < shortestDist) { shortestDist = dist; targetBoss = go.GetComponent<Boss>(); } 
                continue; 
            }
            if (go.GetComponent<EnemyHorse>()) 
            { 
                if (dist < shortestDist) { shortestDist = dist; targetHorse = go.GetComponent<EnemyHorse>(); } 
                continue; 
            }
            if (go.GetComponent<Guard>()) 
            { 
                if (dist < shortestDist) { shortestDist = dist; targetGuard = go.GetComponent<Guard>(); } 
                continue; 
            }
            if (go.GetComponent<EnemySpearman>()) 
            { 
                if (dist < shortestDist) { shortestDist = dist; targetSpearman = go.GetComponent<EnemySpearman>(); } 
                continue; 
            }
            if (go.GetComponent<EnemyArcher>()) 
            { 
                if (dist < shortestDist) { shortestDist = dist; targetArcher = go.GetComponent<EnemyArcher>(); } 
                continue; 
            }
        }
    }

    void Attack()
    {
        if (animator) 
        {
            animator.SetTrigger("Attack");
        }
        
        if (SoundManager.Instance != null) 
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit);
        }
        
        int finalDamage = myDamage;
        UnitStats targetStats = null;

        if (targetBoss != null) targetStats = targetBoss.GetComponent<UnitStats>();
        else if (targetHorse != null) targetStats = targetHorse.GetComponent<UnitStats>();
        else if (targetGuard != null) targetStats = targetGuard.GetComponent<UnitStats>();
        else if (targetSpearman != null) targetStats = targetSpearman.GetComponent<UnitStats>();
        else if (targetArcher != null) targetStats = targetArcher.GetComponent<UnitStats>();

        if (myStats != null && targetStats != null)
        {
            float multiplier = GameManager.GetDamageMultiplier(myStats.category, targetStats.category);
            finalDamage = Mathf.RoundToInt(myDamage * multiplier);
        }

        if (targetBoss != null) targetBoss.TakeDamage(finalDamage);
        else if (targetHorse != null) targetHorse.TakeDamage(finalDamage);
        else if (targetGuard != null) targetGuard.TakeDamage(finalDamage);
        else if (targetSpearman != null) targetSpearman.TakeDamage(finalDamage);
        else if (targetArcher != null) targetArcher.TakeDamage(finalDamage);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        
        if (healthBar != null) 
        {
            healthBar.SetHealth(currentHealth, maxHealth);
        }
        
        if (SoundManager.Instance != null) 
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.knightHit);
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
            GameManager.Instance.OnUnitDeath(gameObject, "Knight"); 
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

        if (animator) 
        {
            animator.Rebind();
            animator.Update(0f);
            animator.enabled = false;
        }
        
        transform.Rotate(0, 0, -90);
        
        if (spriteRenderer != null) 
        {
            spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f);
            spriteRenderer.sortingOrder = 0; 
        }

        Destroy(this);
        Destroy(gameObject, 10f);
    }
}