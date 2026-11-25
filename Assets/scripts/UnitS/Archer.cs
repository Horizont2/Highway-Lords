using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Archer : UnitBase
{
    [Header("Характеристики")]
    public float attackRate = 0.8f;

    [Header("Навігація")]
    public float checkDistance = 1.5f; 

    [Header("Стрільба")]
    public GameObject arrowPrefab;
    public Transform firePoint;

    private int myDamage;
    private float nextAttackTime = 0f;
    private Transform target; 

    private Vector3 startPoint;
    private float retargetTimer = 0f;
    private Vector3 formationPos;

    public void SetFormationPosition(Vector3 pos) { formationPos = pos; }

    protected override void Start()
    {
        base.Start();

        startPoint = transform.position;
        if (formationPos == Vector3.zero) formationPos = startPoint;

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

            GameObject arrowGO = PoolRegistry.Spawn(arrowPrefab, firePoint.position, Quaternion.identity);
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

    void FlipSprite(float targetX) => FaceTowards(targetX);

    // Лучник — єдиний юніт гравця, у якого спрайт намальований праворуч.
    // До зведення в UnitBase його FlipSprite відрізнявся від решти трьох,
    // і при уніфікації ця відмінність загубилася — лучник ходив спиною вперед.
    protected override bool FlipInverted => false;

    // Труп юніта гравця лежить довше за ворожий.
    protected override float CorpseLifeTime => 10f;

    protected override Color CorpseColor => Color.gray;

    protected override void OnDeath()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isResettingUnits)
            GameManager.Instance.OnUnitDeath(gameObject, "Archer");
    }
}
