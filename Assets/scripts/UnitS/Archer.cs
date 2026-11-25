using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Archer : MonoBehaviour
{
    [Header("Ефекти Grow Empire")]
    public Image healthBarFill;
    public GameObject hitParticlePrefab;
    private Color defaultColor = Color.white;

    [Header("Характеристики")]
    public float speed = 2.5f;
    public float attackRange = 5f; 
    public float attackRate = 0.8f;
    public int maxHealth = 60;

    [Header("Навігація")]
    public LayerMask obstacleLayer; 
    public float checkDistance = 1.5f; 
    public float avoidanceForce = 2.0f; 
    
    // --- ДИСЦИПЛІНА МАРШУ ---
    public float aggroRange = 6.0f; // Лучники ламають стрій раніше, щоб зайняти позиції
    private float startY;

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

    public void SetFormationPosition(Vector3 pos) { formationPos = pos; }
    public void LoadState(int savedHealth) { currentHealth = savedHealth; UpdateHealthBar(); }

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
        
        startY = transform.position.y; // Запам'ятовуємо лінію маршу

        if (GameManager.Instance != null)
        {
            myDamage = GameManager.Instance.GetArcherDamage();
            maxHealth = EconomyConfig.GetUnitMaxHealth(60, GameManager.Instance.archerLevel);
            attackRate = EconomyConfig.GetUnitAttackRate(1.2f, GameManager.Instance.archerLevel);

            if (GameManager.Instance.archerLevel > 1 && spriteRenderer != null) 
                spriteRenderer.color = new Color(0.9f, 1f, 0.9f);
        }
        else 
        {
            myDamage = 8;
            maxHealth = 60;
            attackRate = 1.2f;
        }

        if (currentHealth <= 0) currentHealth = maxHealth;
        
        if (spriteRenderer != null) defaultColor = spriteRenderer.color;
        UpdateHealthBar();
    }

    void Update()
    {
        if (isDead) return;
        
        if (GameManager.Instance != null) myDamage = GameManager.Instance.GetArcherDamage();
        
        if (target != null && (target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy)) 
            target = null;

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f)
        {
            FindNearestTarget();
            retargetTimer = 0.25f;
        }

        if (target != null) EngageEnemy(target);
        else
        {
            MoveTo(formationPos); 
            if (Vector2.Distance(transform.position, formationPos) < 0.1f) FlipSprite(transform.position.x + 1f);
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
                nextAttackTime = Time.time + attackRate;
            }
        }
        else MoveTo(targetTransform.position);
    }

    void StartAttack() { if (animator != null) animator.SetTrigger("Attack"); }

    public void ShootArrow() 
    {
        if (isDead) return;
        if (target == null || target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy) return;

        float dist = Vector2.Distance(transform.position, target.position);
        if (dist > attackRange + 1.5f) return; 

        if (arrowPrefab != null && firePoint != null)
        {
            if (SoundManager.Instance != null) 
                SoundManager.Instance.PlaySFXRandomPitch(SoundManager.Instance.arrowShoot);

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

        // --- 1. ФАЗА МАРШУ ---
        float distToTarget = Vector2.Distance(transform.position, targetPosition);
        if (distToTarget > aggroRange)
        {
            float dirX = Mathf.Sign(targetPosition.x - transform.position.x);
            float newY = Mathf.MoveTowards(transform.position.y, startY, speed * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            rb.linearVelocity = new Vector2(dirX * speed, 0);
            return; 
        }

        // --- 2. ФАЗА БОЮ (Ухилення) ---
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
        if (targetX > transform.position.x) transform.localScale = new Vector3(absX, originalScale.y, originalScale.z); 
        else if (targetX < transform.position.x) transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z); 
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
                if (go.transform.position.x > GameManager.Instance.engagementLine.position.x) continue; 

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
        UpdateHealthBar();

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

    void UpdateHealthBar() { if (healthBarFill != null) healthBarFill.fillAmount = Mathf.Clamp01((float)currentHealth / maxHealth); }

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
        
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; }
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (GameManager.Instance != null && !GameManager.Instance.isResettingUnits) 
            GameManager.Instance.OnUnitDeath(gameObject, "Archer"); 
        
        if (healthBarFill != null && healthBarFill.transform.parent != null) 
            healthBarFill.transform.parent.gameObject.SetActive(false);

        if (animator) { animator.Rebind(); animator.Update(0f); animator.enabled = false; }

        transform.Rotate(0, 0, -90);
        if (spriteRenderer != null) { spriteRenderer.color = Color.gray; spriteRenderer.sortingOrder = 0; }
        
        Destroy(this);
        Destroy(gameObject, 10f);
    }
}