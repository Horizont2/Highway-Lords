using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Spearman : MonoBehaviour
{
    [Header("Ефекти Grow Empire")]
    public Image healthBarFill;
    public GameObject hitParticlePrefab;
    private Color defaultColor = Color.white;

    [Header("Характеристики")]
    public float speed = 2.2f;          
    public float attackRange = 2.2f;    
    public float attackRate = 0.8f;     
    public int maxHealth = 100;          

    [Header("Навігація")]
    public LayerMask obstacleLayer; 
    public float avoidanceForce = 2.0f; 
    public float checkDistance = 1.5f;
    
    // --- ДИСЦИПЛІНА МАРШУ ---
    public float aggroRange = 4.5f; 
    private float startY;

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
    private EnemySpearman targetEnemySpearman;
    private EnemyArcher targetArcher; 

    private Vector3 startPoint;
    private Rigidbody2D rb; 
    private bool isDead = false;
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
        myStats = GetComponent<UnitStats>();

        startPoint = transform.position;
        if (formationPos == Vector3.zero) formationPos = startPoint;
        originalScale = transform.localScale;
        
        startY = transform.position.y; // Запам'ятовуємо лінію

        if (GameManager.Instance != null)
        {
            myDamage = GameManager.Instance.GetSpearmanDamage();
            maxHealth = EconomyConfig.GetUnitMaxHealth(100, GameManager.Instance.spearmanLevel);
            attackRate = EconomyConfig.GetUnitAttackRate(0.8f, GameManager.Instance.spearmanLevel);

            if (GameManager.Instance.spearmanLevel > 1 && spriteRenderer != null) 
                spriteRenderer.color = new Color(0.8f, 0.9f, 1f);
        }
        else { myDamage = 15; maxHealth = 100; attackRate = 0.8f; }

        if (currentHealth <= 0) currentHealth = maxHealth;
        if (spriteRenderer != null) defaultColor = spriteRenderer.color;
        UpdateHealthBar();
    }

    void Update()
    {
        if (isDead) return;

        if (GameManager.Instance != null) myDamage = GameManager.Instance.GetSpearmanDamage();

        if (targetBoss != null && (targetBoss.CompareTag("Untagged") || !targetBoss.gameObject.activeInHierarchy)) targetBoss = null;
        if (targetRam != null && (targetRam.CompareTag("Untagged") || !targetRam.gameObject.activeInHierarchy)) targetRam = null;
        if (targetHorse != null && (targetHorse.CompareTag("Untagged") || !targetHorse.gameObject.activeInHierarchy)) targetHorse = null;
        if (targetGuard != null && (targetGuard.CompareTag("Untagged") || !targetGuard.gameObject.activeInHierarchy)) targetGuard = null;
        if (targetEnemySpearman != null && (targetEnemySpearman.CompareTag("Untagged") || !targetEnemySpearman.gameObject.activeInHierarchy)) targetEnemySpearman = null;
        if (targetArcher != null && (targetArcher.CompareTag("Untagged") || !targetArcher.gameObject.activeInHierarchy)) targetArcher = null;

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f) { FindNearestTarget(); retargetTimer = 0.25f; }

        Transform currentTarget = null;
        if (targetBoss != null) currentTarget = targetBoss.transform; 
        else if (targetRam != null) currentTarget = targetRam.transform;
        else if (targetHorse != null) currentTarget = targetHorse.transform;
        else if (targetGuard != null) currentTarget = targetGuard.transform;
        else if (targetEnemySpearman != null) currentTarget = targetEnemySpearman.transform;
        else if (targetArcher != null) currentTarget = targetArcher.transform;

        if (currentTarget != null) EngageEnemy(currentTarget);
        else
        {
            MoveTo(formationPos, false); 
            if (Vector2.Distance(transform.position, formationPos) < 0.2f) FlipSprite(transform.position.x + 1f);
        }
    }

    void EngageEnemy(Transform targetTransform)
    {
        Vector3 targetPos = GetDynamicEngagementPosition(targetTransform);
        FlipSprite(targetTransform.position.x);
        
        Collider2D targetCol = targetTransform.GetComponent<Collider2D>();
        float distanceToHit = targetCol != null ? 
            Vector2.Distance(transform.position, targetCol.ClosestPoint(transform.position)) : 
            Vector2.Distance(transform.position, targetTransform.position);

        if (distanceToHit <= attackRange)
        {
            Vector2 sep = GetSeparationVector();
            if (sep.magnitude > 0.2f) rb.linearVelocity = sep * (speed * 0.3f);
            else rb.linearVelocity = Vector2.zero;

            if (animator) animator.SetBool("IsMoving", false);
            
            if (Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + attackRate;
            }
        }
        else MoveTo(targetPos, true);
    }

    Vector3 GetDynamicEngagementPosition(Transform targetTransform)
    {
        Vector3 basePos = targetTransform.position;
        float dirX = (transform.position.x > basePos.x) ? 1f : -1f;
        Vector3 frontSlot = basePos + new Vector3(dirX * (attackRange * 0.8f), 0, 0);

        Collider2D[] allies = Physics2D.OverlapCircleAll(frontSlot, 0.4f);
        int crowdCount = 0;
        foreach(var col in allies) {
            if (col.gameObject != gameObject && col.CompareTag(gameObject.tag)) crowdCount++;
        }

        if (crowdCount == 0) return frontSlot;
        if (crowdCount == 1) return basePos + new Vector3(dirX * attackRange * 0.5f, attackRange, 0); 
        if (crowdCount == 2) return basePos + new Vector3(dirX * attackRange * 0.5f, -attackRange, 0);
        if (crowdCount == 3) return basePos + new Vector3(-dirX * attackRange, 0, 0); 

        return basePos + new Vector3(dirX * (attackRange + crowdCount * 0.6f), 0, 0); 
    }

    Vector2 GetSeparationVector()
    {
        Vector2 separation = Vector2.zero;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 0.6f);
        foreach (var col in nearby)
        {
            if (col.gameObject != gameObject && col.CompareTag(gameObject.tag) && !col.isTrigger)
            {
                Vector2 diff = transform.position - col.transform.position;
                if (diff.magnitude > 0.01f) separation += diff.normalized * (1f - diff.magnitude / 0.6f);
            }
        }
        return separation;
    }

    void MoveTo(Vector3 targetPosition, bool useSeparation)
    {
        if (Vector2.Distance(transform.position, targetPosition) < 0.15f)
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

        // --- 2. ФАЗА БОЮ ---
        Vector2 direction = (targetPosition - transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, checkDistance, obstacleLayer);
        if (hit.collider != null && hit.collider.gameObject != gameObject)
        {
            float dodgeDirY = (transform.position.y >= hit.collider.bounds.center.y) ? 1f : -1f;
            direction += new Vector2(0, dodgeDirY) * avoidanceForce;
        }

        if (useSeparation) direction += GetSeparationVector() * 1.5f;
        rb.linearVelocity = direction.normalized * speed;
    }

    void FlipSprite(float targetX)
    {
        // ВАЖЛИВО: Для Лицаря/Списоносця/Кінноти логіка має бути ТАКОЮ:
        
        float absX = Mathf.Abs(originalScale.x);
        
        // Якщо йдемо ВПРАВО (targetX > currentX) -> ставимо МІНУСОВИЙ scale (щоб відзеркалити спрайт вправо)
        if (targetX > transform.position.x) 
        {
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z);
        }
        // Якщо йдемо ВЛІВО (targetX < currentX) -> ставимо ПОЗИТИВНИЙ scale (оригінальний вигляд спрайту)
        else if (targetX < transform.position.x)
        {
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z);
        }
    }

    void FindNearestTarget()
    {
        targetBoss = null; targetRam = null; targetHorse = null; 
        targetGuard = null; targetEnemySpearman = null; targetArcher = null;

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

            if (perceivedDist < shortestDist) { shortestDist = perceivedDist; closestEnemy = go; }
        }

        if (closestEnemy != null)
        {
            targetBoss = closestEnemy.GetComponent<Boss>();
            targetRam = closestEnemy.GetComponent<BatteringRam>();
            targetHorse = closestEnemy.GetComponent<EnemyHorse>();
            targetGuard = closestEnemy.GetComponent<Guard>();
            targetEnemySpearman = closestEnemy.GetComponent<EnemySpearman>();
            targetArcher = closestEnemy.GetComponent<EnemyArcher>();
        }
    }

    void Attack() { if (animator) animator.SetTrigger("Attack"); }

    public void Hit() 
    {
        if (isDead) return;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFXRandomPitch(SoundManager.Instance.swordHit); 

        int finalDamage = myDamage;
        UnitStats targetStats = null;

        if (targetBoss != null) targetStats = targetBoss.GetComponent<UnitStats>();
        else if (targetRam != null) targetStats = targetRam.GetComponent<UnitStats>();
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
        else if (targetRam != null) targetRam.TakeDamage(finalDamage);
        else if (targetHorse != null) targetHorse.TakeDamage(finalDamage);
        else if (targetGuard != null) targetGuard.TakeDamage(finalDamage);
        else if (targetEnemySpearman != null) targetEnemySpearman.TakeDamage(finalDamage);
        else if (targetArcher != null) targetArcher.TakeDamage(finalDamage);
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
        
        if (GameManager.Instance != null && !GameManager.Instance.isResettingUnits) GameManager.Instance.OnUnitDeath(gameObject, "Spearman"); 

        if (healthBarFill != null && healthBarFill.transform.parent != null) healthBarFill.transform.parent.gameObject.SetActive(false);
        if (animator) { animator.Rebind(); animator.Update(0f); animator.enabled = false; }
        transform.Rotate(0, 0, -90);
        if (spriteRenderer != null) { spriteRenderer.color = Color.gray; spriteRenderer.sortingOrder = 0; }
        Destroy(this);
        Destroy(gameObject, 10f);
    }
}