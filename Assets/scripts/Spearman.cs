using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Spearman : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Характеристики")]
    public float speed = 2.2f;          
    public float attackRange = 2.2f;    
    public float attackRate = 0.8f;     
    public int maxHealth = 100;          

    [Header("Навігація")]
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
    private EnemyArcher targetArcher; 
    private EnemySpearman targetEnemySpearman;
    private Boss targetBoss; 
    private EnemyHorse targetHorse; 

    private Vector3 startPoint;
    private Rigidbody2D rb; 
    private bool isDead = false;
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
        if (healthBar != null) healthBar.SetHealth(currentHealth, maxHealth);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic; 
        rb.gravityScale = 0; 
        rb.freezeRotation = true; 

        if(spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        myStats = GetComponent<UnitStats>();

        startPoint = transform.position;
        if (formationPos == Vector3.zero) formationPos = startPoint;
        originalScale = transform.localScale;

        // === НОВА ЕКОНОМІКА ===
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateUI();
            myDamage = GameManager.Instance.GetSpearmanDamage();
            maxHealth = EconomyConfig.GetUnitMaxHealth(100, GameManager.Instance.spearmanLevel);
            attackRate = EconomyConfig.GetUnitAttackRate(0.8f, GameManager.Instance.spearmanLevel);

            if (GameManager.Instance.spearmanLevel > 1 && spriteRenderer != null) 
                spriteRenderer.color = new Color(0.8f, 0.9f, 1f);
        }
        else
        {
            myDamage = 15;
            maxHealth = 100;
            attackRate = 0.8f;
        }

        if (currentHealth <= 0) currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.targetTransform = transform;
            healthBar.SetHealth(currentHealth, maxHealth);
        }
    }

    void Update()
    {
        if (isDead) return;

        if (GameManager.Instance != null)
        {
            myDamage = GameManager.Instance.GetSpearmanDamage();
        }

        if (targetBoss != null && (targetBoss.CompareTag("Untagged") || !targetBoss.gameObject.activeInHierarchy)) targetBoss = null;
        if (targetGuard != null && (targetGuard.CompareTag("Untagged") || !targetGuard.gameObject.activeInHierarchy)) targetGuard = null;
        if (targetArcher != null && (targetArcher.CompareTag("Untagged") || !targetArcher.gameObject.activeInHierarchy)) targetArcher = null;
        if (targetEnemySpearman != null && (targetEnemySpearman.CompareTag("Untagged") || !targetEnemySpearman.gameObject.activeInHierarchy)) targetEnemySpearman = null;
        if (targetHorse != null && (targetHorse.CompareTag("Untagged") || !targetHorse.gameObject.activeInHierarchy)) targetHorse = null;

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
        else if (targetEnemySpearman != null) currentTarget = targetEnemySpearman.transform;
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

    void EngageEnemy(Transform target)
    {
        float distance = Vector2.Distance(transform.position, target.position);
        FlipSprite(target.position.x);

        if (distance <= attackRange)
        {
            rb.linearVelocity = Vector2.zero; 
            if (animator) animator.SetBool("IsMoving", false);
            
            if (Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + attackRate;
            }
        }
        else
        {
            MoveTo(target.position);
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

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.rightBoundary != null && targetPosition.x > GameManager.Instance.rightBoundary.position.x)
                targetPosition.x = GameManager.Instance.rightBoundary.position.x;
            if (GameManager.Instance.leftBoundary != null && targetPosition.x < GameManager.Instance.leftBoundary.position.x)
                targetPosition.x = GameManager.Instance.leftBoundary.position.x;
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
        targetBoss = null; targetHorse = null; targetGuard = null; 
        targetEnemySpearman = null; targetArcher = null;

        float minX = -1000f; float maxX = 1000f;
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.leftBoundary) minX = GameManager.Instance.leftBoundary.position.x - 2f;
            if (GameManager.Instance.rightBoundary) maxX = GameManager.Instance.rightBoundary.position.x + 2f;
        }

        float shortestDist = Mathf.Infinity;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        foreach (GameObject go in enemies)
        {
            if (go == gameObject || go.CompareTag("Untagged")) continue;
            if (GameManager.Instance != null && GameManager.Instance.engagementLine != null)
                if (go.transform.position.x > GameManager.Instance.engagementLine.position.x) continue;
            if (go.transform.position.x > maxX || go.transform.position.x < minX) continue;

            float dist = Vector2.Distance(transform.position, go.transform.position);

            if (go.GetComponent<Boss>()) { if (dist < shortestDist) { shortestDist = dist; targetBoss = go.GetComponent<Boss>(); } continue; }
            if (go.GetComponent<EnemyHorse>()) { if (dist < shortestDist) { shortestDist = dist; targetHorse = go.GetComponent<EnemyHorse>(); } continue; }
            if (go.GetComponent<Guard>() && !targetBoss && !targetHorse) { if (dist < shortestDist) { shortestDist = dist; targetGuard = go.GetComponent<Guard>(); } continue; }
            if (go.GetComponent<EnemySpearman>() && !targetBoss && !targetHorse && !targetGuard) { if (dist < shortestDist) { shortestDist = dist; targetEnemySpearman = go.GetComponent<EnemySpearman>(); } continue; }
            if (go.GetComponent<EnemyArcher>() && !targetBoss && !targetHorse && !targetGuard && !targetEnemySpearman) { if (dist < shortestDist) { shortestDist = dist; targetArcher = go.GetComponent<EnemyArcher>(); } continue; }
        }
    }

    void Attack()
    {
        if (animator) animator.SetTrigger("Attack");
    }

    public void Hit() 
    {
        if (isDead) return;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit); 

        int finalDamage = myDamage;
        UnitStats targetStats = null;

        if (targetBoss != null) targetStats = targetBoss.GetComponent<UnitStats>();
        else if (targetHorse != null) targetStats = targetHorse.GetComponent<UnitStats>();
        else if (targetGuard != null) targetStats = targetGuard.GetComponent<UnitStats>();
        else if (targetEnemySpearman != null) targetStats = targetEnemySpearman.GetComponent<UnitStats>();
        else if (targetArcher != null) targetStats = targetArcher.GetComponent<UnitStats>();

        if (myStats != null && targetStats != null)
        {
            float multiplier = GameManager.GetDamageMultiplier(myStats.category, targetStats.category);
            finalDamage = Mathf.RoundToInt(myDamage * multiplier);
        }
        
        if (targetBoss != null) targetBoss.TakeDamage(finalDamage);
        else if (targetHorse != null) targetHorse.TakeDamage(finalDamage);
        else if (targetGuard != null) targetGuard.TakeDamage(finalDamage);
        else if (targetEnemySpearman != null) targetEnemySpearman.TakeDamage(finalDamage);
        else if (targetArcher != null) targetArcher.TakeDamage(finalDamage);
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
            GameManager.Instance.OnUnitDeath(gameObject, "Spearman"); 
        }

        if (healthBar != null) healthBar.gameObject.SetActive(false);
        if (animator) { animator.Rebind(); animator.Update(0f); animator.enabled = false; }
        transform.Rotate(0, 0, -90);
        if (spriteRenderer != null) { spriteRenderer.color = Color.gray; spriteRenderer.sortingOrder = 0; }
        Destroy(this);
        Destroy(gameObject, 10f);
    }
}