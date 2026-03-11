using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Knight : MonoBehaviour
{
    [Header("Ефекти Grow Empire")]
    public Image healthBarFill;
    public GameObject hitParticlePrefab;
    private Color defaultColor = Color.white;

    [Header("Характеристики")]
    public float speed = 3.0f;
    public float attackRange = 0.8f;
    public float attackRate = 1f;
    public int maxHealth = 120;

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

    private Boss targetBoss; 
    private BatteringRam targetRam; 
    private EnemyHorse targetHorse;
    private Guard targetGuard;
    private EnemySpearman targetSpearman;
    private EnemyArcher targetArcher; 

    private Vector3 startPoint;
    public float patrolRadius = 3f;
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
        UpdateHealthBar();
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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateUI();
            myDamage = GameManager.Instance.GetKnightDamage();
            maxHealth = EconomyConfig.GetUnitMaxHealth(120, GameManager.Instance.knightLevel);
            attackRate = EconomyConfig.GetUnitAttackRate(1.0f, GameManager.Instance.knightLevel);

            if (GameManager.Instance.knightLevel > 1 && spriteRenderer != null) 
                spriteRenderer.color = new Color(1f, 0.9f, 0.9f);
        }
        else 
        {
            myDamage = 12;
            maxHealth = 120;
            attackRate = 1.0f;
        }

        if (currentHealth <= 0) currentHealth = maxHealth;

        if (spriteRenderer != null) defaultColor = spriteRenderer.color;
        UpdateHealthBar();
    }

    void Update()
    {
        if (isDead) return;

        if (GameManager.Instance != null)
        {
            myDamage = GameManager.Instance.GetKnightDamage();
        }

        if (targetBoss != null && (targetBoss.CompareTag("Untagged") || !targetBoss.gameObject.activeInHierarchy)) targetBoss = null;
        if (targetRam != null && (targetRam.CompareTag("Untagged") || !targetRam.gameObject.activeInHierarchy)) targetRam = null;
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
        else if (targetRam != null) currentTarget = targetRam.transform; 
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

    void EngageEnemy(Transform target)
    {
        Vector3 moveDestination = target.position;
        FlipSprite(target.position.x);
        
        Collider2D targetCol = target.GetComponent<Collider2D>();
        float distance = targetCol != null ? 
            Vector2.Distance(transform.position, targetCol.ClosestPoint(transform.position)) : 
            Vector2.Distance(transform.position, target.position);

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
            MoveTo(moveDestination);
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
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z); 
        else if (targetX < transform.position.x)
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z); 
    }

    void FindNearestTarget()
    {
        targetBoss = null; targetRam = null; targetHorse = null; 
        targetGuard = null; targetSpearman = null; targetArcher = null;

        float minX = -1000f; float maxX = 1000f;
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.leftBoundary) minX = GameManager.Instance.leftBoundary.position.x - 2f;
            if (GameManager.Instance.rightBoundary) maxX = GameManager.Instance.rightBoundary.position.x + 2f;
        }

        float shortestDist = Mathf.Infinity;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject closestEnemy = null;
        
        foreach (GameObject go in enemies)
        {
            if (go == gameObject || go.CompareTag("Untagged")) continue;
            
            if (GameManager.Instance != null && GameManager.Instance.engagementLine != null)
                if (go.transform.position.x > GameManager.Instance.engagementLine.position.x) continue;
            
            if (go.transform.position.x > maxX || go.transform.position.x < minX) continue;

            float dist = Vector2.Distance(transform.position, go.transform.position);

            float perceivedDist = go.GetComponent<BatteringRam>() ? dist - 1.5f : dist;

            if (perceivedDist < shortestDist) 
            {
                shortestDist = perceivedDist;
                closestEnemy = go;
            }
        }

        if (closestEnemy != null)
        {
            targetBoss = closestEnemy.GetComponent<Boss>();
            targetRam = closestEnemy.GetComponent<BatteringRam>();
            targetHorse = closestEnemy.GetComponent<EnemyHorse>();
            targetGuard = closestEnemy.GetComponent<Guard>();
            targetSpearman = closestEnemy.GetComponent<EnemySpearman>();
            targetArcher = closestEnemy.GetComponent<EnemyArcher>();
        }
    }

    void Attack()
    {
        if (animator) animator.SetTrigger("Attack");
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit);
        
        int finalDamage = myDamage;
        UnitStats targetStats = null;

        if (targetBoss != null) targetStats = targetBoss.GetComponent<UnitStats>();
        else if (targetRam != null) targetStats = targetRam.GetComponent<UnitStats>();
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
        else if (targetRam != null) targetRam.TakeDamage(finalDamage);
        else if (targetHorse != null) targetHorse.TakeDamage(finalDamage);
        else if (targetGuard != null) targetGuard.TakeDamage(finalDamage);
        else if (targetSpearman != null) targetSpearman.TakeDamage(finalDamage);
        else if (targetArcher != null) targetArcher.TakeDamage(finalDamage);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        UpdateHealthBar();

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.knightHit);
        
        Vector3 popupPos = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.5f, 1.2f), 0);
        GameManager.CreateDamagePopup(popupPos, damage);

        if (hitParticlePrefab != null)
        {
            GameObject particles = Instantiate(hitParticlePrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(particles, 1f);
        }

        if (spriteRenderer != null) StartCoroutine(FlashColor());

        if (currentHealth <= 0) Die();
    }

    void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Clamp01((float)currentHealth / maxHealth);
        }
    }

    private IEnumerator FlashColor()
    {
        spriteRenderer.color = new Color(1f, 0.4f, 0.4f); 
        yield return new WaitForSeconds(0.1f);
        if (!isDead) spriteRenderer.color = defaultColor; 
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        gameObject.tag = "Untagged";

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false; 

        if (GameManager.Instance != null && !GameManager.Instance.isResettingUnits)
        {
            GameManager.Instance.OnUnitDeath(gameObject, "Knight"); 
        }

        if (healthBarFill != null && healthBarFill.transform.parent != null) 
            healthBarFill.transform.parent.gameObject.SetActive(false);

        Transform shadow = transform.Find("Shadow");
        if (shadow != null) shadow.gameObject.SetActive(false);

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