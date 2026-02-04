using UnityEngine;
using System.Collections;

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

    [Header("Стрільба")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float arrowSpawnDelay = 0.3f; // Затримка для синхронізації з анімацією

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

    // === ПАТРУЛЬ ===
    private Vector3 startPoint;
    public float patrolRadius = 2.5f;
    private Vector3 currentPatrolTarget;
    private float patrolWaitTimer = 0f;
    private bool isWaiting = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic; 
        rb.gravityScale = 0; 
        rb.freezeRotation = true; 

        // === АВТОМАТИЧНЕ ПІДКЛЮЧЕННЯ КОМПОНЕНТІВ ===
        if(spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if(animator == null) animator = GetComponent<Animator>(); 
        // ============================================

        startPoint = transform.position;
        originalScale = transform.localScale;

        if (currentHealth <= 0) currentHealth = maxHealth;
        
        SetNewPatrolTarget();

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

    public void LoadState(int savedHealth)
    {
        currentHealth = savedHealth;
        if (healthBar != null) healthBar.SetHealth(currentHealth, maxHealth);
    }

    void Update()
    {
        if (isDead) return;
        
        if (target != null && (target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy)) 
            target = null;

        FindNearestTarget();

        if (target != null)
        {
            EngageEnemy(target);
            isWaiting = false;
        }
        else
        {
            Patrol();
        }
    }

    void EngageEnemy(Transform targetTransform)
    {
        float distance = Vector2.Distance(transform.position, targetTransform.position);
        FlipSprite(targetTransform.position.x);

        if (distance <= attackRange)
        {
            // Повна зупинка
            rb.linearVelocity = Vector2.zero; 
            if (animator) animator.SetBool("IsMoving", false);

            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
        else
        {
            MoveTo(targetTransform.position);
        }
    }

    IEnumerator AttackRoutine()
    {
        if (target == null) yield break;

        // === ДІАГНОСТИКА ===
        if (animator != null)
        {
            // Debug.Log("⚔️ Спроба анімації Attack!"); // Розкоментуй, якщо хочеш бачити в консолі
            animator.SetTrigger("Attack");
        }
        // ===================

        // Чекаємо поки лучник натягне тятиву
        yield return new WaitForSeconds(arrowSpawnDelay);

        if (target != null && arrowPrefab != null && firePoint != null)
        {
            if (SoundManager.Instance != null) 
                SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot);

            GameObject arrowGO = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
            Arrow arrowScript = arrowGO.GetComponent<Arrow>();
            
            if (arrowScript != null)
            {
                arrowScript.Initialize(target, myDamage);
            }
        }
    }

    void Patrol()
    {
        if (isWaiting)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator) animator.SetBool("IsMoving", false);
            
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0)
            {
                isWaiting = false;
                SetNewPatrolTarget();
            }
            return;
        }

        if (Vector2.Distance(transform.position, currentPatrolTarget) < 0.2f)
        {
            isWaiting = true;
            patrolWaitTimer = Random.Range(1.0f, 3.0f);
        }
        else
        {
            MoveTo(currentPatrolTarget);
        }
    }

    void SetNewPatrolTarget()
    {
        Vector2 randomPoint = Random.insideUnitCircle * patrolRadius;
        currentPatrolTarget = startPoint + (Vector3)randomPoint;
    }

    void MoveTo(Vector3 targetPosition)
    {
        if (animator) animator.SetBool("IsMoving", true);
        FlipSprite(targetPosition.x);
        Vector2 direction = (targetPosition - transform.position).normalized;
        rb.linearVelocity = direction * speed;
    }

    void FlipSprite(float targetX)
    {
        float absX = Mathf.Abs(originalScale.x);
        if (targetX > transform.position.x) 
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z); 
        else 
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z); 
    }

    void FindNearestTarget()
    {
        if (target != null && Vector2.Distance(transform.position, target.position) <= attackRange 
            && target.gameObject.activeInHierarchy && !target.CompareTag("Untagged")) return;

        float shortestDist = Mathf.Infinity;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;

        foreach (GameObject go in enemies)
        {
            if (go.CompareTag("Untagged")) continue;
            float dist = Vector2.Distance(transform.position, go.transform.position);
            
            if (dist < shortestDist && dist <= attackRange * 1.5f) 
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
        if (GameManager.Instance != null) GameManager.Instance.ShowDamage(damage, transform.position);
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

        if (GameManager.Instance != null) { GameManager.Instance.currentUnits--; GameManager.Instance.UpdateUI(); }
        if (healthBar != null) healthBar.gameObject.SetActive(false);
        if (animator) animator.enabled = false;

        transform.Rotate(0, 0, -90);
        if (spriteRenderer != null) { spriteRenderer.color = Color.gray; spriteRenderer.sortingOrder = 0; }
        
        Destroy(this);
        Destroy(gameObject, 10f);
    }
}