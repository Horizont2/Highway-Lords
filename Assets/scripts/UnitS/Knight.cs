using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Knight : UnitBase
{
    [Header("Характеристики")]
    public float attackRate = 1f;

    [Header("Навігація (Обхід)")]
    public float checkDistance = 1.5f;

    private int myDamage;
    private float nextAttackTime = 0f;

    private Boss targetBoss; 
    private BatteringRam targetRam; 
    private EnemyHorse targetHorse;
    private Guard targetGuard;
    private EnemySpearman targetSpearman;
    private EnemyArcher targetArcher; 

    private Vector3 startPoint;
    public float patrolRadius = 3f;

    private float retargetTimer = 0f;
    private Vector3 formationPos;

    public void SetFormationPosition(Vector3 pos) { formationPos = pos; }

    protected override void Start()
    {
        base.Start();

        startPoint = transform.position;
        if (formationPos == Vector3.zero) formationPos = startPoint;
        
        // Запам'ятовуємо висоту (Y), на якій заспавнився юніт

        if (GameManager.Instance != null)
        {
            myDamage = GameManager.Instance.GetKnightDamage();
            maxHealth = EconomyConfig.GetUnitMaxHealth(120, GameManager.Instance.knightLevel);
            attackRate = EconomyConfig.GetUnitAttackRate(1.0f, GameManager.Instance.knightLevel);
        }
        else { myDamage = 12; maxHealth = 120; attackRate = 1.0f; }

        if (currentHealth <= 0) currentHealth = maxHealth;
        if (spriteRenderer != null) defaultColor = spriteRenderer.color;
        UpdateHealthBar();
    }

    void Update()
    {
        if (isDead) return;

        if (GameManager.Instance != null) myDamage = GameManager.Instance.GetKnightDamage();

        if (targetBoss != null && (targetBoss.CompareTag("Untagged") || !targetBoss.gameObject.activeInHierarchy)) targetBoss = null;
        if (targetRam != null && (targetRam.CompareTag("Untagged") || !targetRam.gameObject.activeInHierarchy)) targetRam = null;
        if (targetHorse != null && (targetHorse.CompareTag("Untagged") || !targetHorse.gameObject.activeInHierarchy)) targetHorse = null;
        if (targetGuard != null && (targetGuard.CompareTag("Untagged") || !targetGuard.gameObject.activeInHierarchy)) targetGuard = null;
        if (targetSpearman != null && (targetSpearman.CompareTag("Untagged") || !targetSpearman.gameObject.activeInHierarchy)) targetSpearman = null;
        if (targetArcher != null && (targetArcher.CompareTag("Untagged") || !targetArcher.gameObject.activeInHierarchy)) targetArcher = null;

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f) { FindNearestTarget(); retargetTimer = 0.25f; }

        Transform currentTarget = null;
        if (targetBoss != null) currentTarget = targetBoss.transform; 
        else if (targetRam != null) currentTarget = targetRam.transform; 
        else if (targetHorse != null) currentTarget = targetHorse.transform;
        else if (targetGuard != null) currentTarget = targetGuard.transform;
        else if (targetSpearman != null) currentTarget = targetSpearman.transform;
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
        else
        {
            MoveTo(targetPos, true);
        }
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

    // --- ОНОВЛЕНИЙ МЕТОД МАРШУ ---
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
        
        float distToTarget = Vector2.Distance(transform.position, targetPosition);

        // --- 1. ФАЗА МАРШУ (Ворог далеко) ---
        if (distToTarget > aggroRange)
        {
            // Рухаємося строго по горизонталі (до цілі)
            float dirX = Mathf.Sign(targetPosition.x - transform.position.x);
            
            // Якщо колізія трохи змістила юніта, він плавно повертається на свою ідеальну лінію Y
            float newY = Mathf.MoveTowards(transform.position.y, startY, speed * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // Штовхаємо строго вперед, БЕЗ РОЗДІЛЕННЯ (не обходимо союзників)
            rb.linearVelocity = new Vector2(dirX * speed, 0);
            return; 
        }

        // --- 2. ФАЗА БОЮ (Хаос і маневри) ---
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

            if (perceivedDist < shortestDist) { shortestDist = perceivedDist; closestEnemy = go; }
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
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFXRandomPitch(SoundManager.Instance.swordHit);
        
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

    void FlipSprite(float targetX) => FaceTowards(targetX);

    // Спрайт намальований ліворуч, тож "вправо" — це від'ємний scale.
    protected override bool FlipInverted => true;

    // Труп юніта гравця лежить довше за ворожий.
    protected override float CorpseLifeTime => 10f;

    protected override void OnDeath()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isResettingUnits)
            GameManager.Instance.OnUnitDeath(gameObject, "Knight");
    }
}
