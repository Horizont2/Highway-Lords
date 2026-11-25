using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))] 
public class EnemyArcher : UnitBase
{
    [Header("Характеристики")]
    public float timeBetweenShots = 2.0f;
    public int damage = 10;
    
    [Header("Навігація та Агро")]
    public float aggroRadius = 8.0f; 
    
    // --- ДИСЦИПЛІНА МАРШУ ---

    private float retargetTimer = 0f;

    private float safeDistanceBehindTank = 2.0f;
    private float maxTankWaitDistance = 15.0f; 

    [Header("Стрільба")]
    public GameObject arrowPrefab;
    public Transform firePoint;

    private Transform target;
    private float nextShotTime;

    private Transform myCart;

    private int _baseMaxHealth;
    private int _baseDamage;

    void Awake()
    {
        _baseMaxHealth = maxHealth;
        _baseDamage = damage;
    }

    protected override void Start()
    {
        base.Start();

        if (GameManager.Instance != null)
        {
            effectiveMaxHealth = GameManager.Instance.GetScaledEnemyHealth(_baseMaxHealth);
            damage = Mathf.RoundToInt(_baseDamage * GameManager.Instance.GetEnemyDamageMultiplier());
        }
        else { effectiveMaxHealth = _baseMaxHealth; }

        currentHealth = effectiveMaxHealth;
        if (spriteRenderer != null) defaultColor = spriteRenderer.color;
        UpdateHealthBar();

        Cart cartScript = FindFirstObjectByType<Cart>();
        if (cartScript != null) myCart = cartScript.transform;
    }

    void Update()
    {
        if (isDead) return;

        if (GameManager.Instance != null && GameManager.Instance.isDefeated)
        {
            target = null;
            if (animator) animator.SetBool("IsMoving", true);
            if (rb != null) rb.linearVelocity = new Vector2(-speed, 0f);
            return;
        }

        if (target != null && (target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy)) target = null;

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f) { FindTarget(); retargetTimer = 0.25f; }

        Vector2 finalVelocity = Vector2.zero;
        bool shouldMove = false;

        float distanceToTarget = target != null ? Vector2.Distance(transform.position, target.position) : Mathf.Infinity;

        if (target != null)
        {
            bool isStructure = target.TryGetComponent<Spikes>(out _) || target.TryGetComponent<Wall>(out _);
            
            if (isStructure)
            {
                Collider2D targetCol = target.GetComponent<Collider2D>();
                if (targetCol != null) distanceToTarget = Vector2.Distance(transform.position, targetCol.ClosestPoint(transform.position));
                else distanceToTarget = Mathf.Abs(transform.position.x - target.position.x);
            }
            FaceTarget(target.position);
        }
        
        // --- ФАЗА МАРШУ ---
        if (distanceToTarget > attackRange)
        {
            if (ShouldWaitForTank())
            {
                finalVelocity = Vector2.zero;
                shouldMove = false;
                if (IsBlockingCart()) { finalVelocity = GetDodgeVector(); shouldMove = true; }
            }
            else
            {
                if (distanceToTarget > aggroRange)
                {
                    // Рівний крок
                    float dirX = target != null ? Mathf.Sign(target.position.x - transform.position.x) : -1f;
                    float newY = Mathf.MoveTowards(transform.position.y, startY, speed * Time.deltaTime);
                    transform.position = new Vector3(transform.position.x, newY, transform.position.z);
                    
                    finalVelocity = new Vector2(dirX * speed, 0);
                    shouldMove = true;
                    if (firePoint) firePoint.localRotation = Quaternion.identity;
                }
                else
                {
                    // --- ФАЗА БОЮ (Ухиляння і вибір позиції) ---
                    Vector3 targetPosFixed = target.position;
                    bool isStructure = target.TryGetComponent<Spikes>(out _) || target.TryGetComponent<Wall>(out _);
                    if (isStructure) targetPosFixed.y = transform.position.y;

                    Vector2 direction = (targetPosFixed - transform.position).normalized;
                    if (IsBlockingCart()) direction = ApplyCartAvoidance(direction);
                    direction = ApplyWallAvoidance(direction);

                    finalVelocity = direction * speed;
                    shouldMove = true;
                    if (firePoint) firePoint.localRotation = Quaternion.identity;
                }
            }
        }
        else if (target != null)
        {
            if (IsBlockingCart())
            {
                finalVelocity = GetDodgeVector();
                shouldMove = true;
            }
            else
            {
                AimAtTarget();
                if (Time.time > nextShotTime) { if (animator != null) animator.SetTrigger("Attack"); nextShotTime = Time.time + timeBetweenShots; }
            }
        }
        
        // Коли немає цілі - суворо маршируємо вліво
        if (target == null && !ShouldWaitForTank())
        {
            FaceTarget(transform.position + Vector3.left * 5f);
            float newY = Mathf.MoveTowards(transform.position.y, startY, speed * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            finalVelocity = new Vector2(-speed, 0);
            shouldMove = true;
        }

        rb.linearVelocity = finalVelocity; 
        if (animator) animator.SetBool("IsMoving", shouldMove);
    }

    void FindTarget()
    {
        float minDistance = Mathf.Infinity;
        Transform closestUnit = null;

        void CheckDistance(Transform t)
        {
            if (t == null || !t.gameObject.activeInHierarchy || t.CompareTag("Untagged")) return;
            float dist = Vector2.Distance(transform.position, t.position);
            if (dist < minDistance) { minDistance = dist; closestUnit = t; }
        }

        if (GameManager.Instance != null)
        {
            var playerUnits = GameManager.Instance.GetPlayerUnits();
            for (int i = 0; i < playerUnits.Count; i++) CheckDistance(playerUnits[i]);
        }

        if (GameManager.Instance != null && GameManager.Instance.currentSpikes != null) CheckDistance(GameManager.Instance.currentSpikes.transform);

        if (closestUnit != null && minDistance <= aggroRadius) target = closestUnit;
        else if (GameManager.Instance != null && GameManager.Instance.castle != null) target = GameManager.Instance.castle.transform;
        else target = null;
    }

    public void ShootArrow()
    {
        if (isDead || target == null || target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy) return;

        bool isStructure = target.TryGetComponent<Spikes>(out _) || target.TryGetComponent<Wall>(out _);
        float dist;

        if (isStructure)
        {
            Collider2D targetCol = target.GetComponent<Collider2D>();
            if (targetCol != null) dist = Vector2.Distance(transform.position, targetCol.ClosestPoint(transform.position));
            else dist = Mathf.Abs(transform.position.x - target.position.x);
        }
        else dist = Vector2.Distance(transform.position, target.position);

        if (dist > attackRange + 1.5f) return;

        if (arrowPrefab != null && firePoint != null)
        {
            AimAtTarget(); 
            GameObject arrowObj = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);
            EnemyProjectile p = arrowObj.GetComponent<EnemyProjectile>();
            
            if (p != null)
            {
                int finalDamage = damage;
                if (myStats != null)
                {
                    UnitStats targetStats = target.GetComponent<UnitStats>();
                    if (targetStats != null) finalDamage = Mathf.RoundToInt(damage * GameManager.GetDamageMultiplier(myStats.category, targetStats.category));
                }
                p.Initialize(target.position, finalDamage);
            }
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFXRandomPitch(SoundManager.Instance.arrowShoot);
        }
    }

    Vector2 GetDodgeVector() { return ((transform.position.y > myCart.position.y) ? Vector2.up : Vector2.down) * speed; }
    Vector2 ApplyCartAvoidance(Vector2 dir) { float yDiff = transform.position.y - myCart.position.y; dir.y += (yDiff > 0 ? 1f : -1f) * 2.5f; return dir.normalized; }
    Vector2 ApplyWallAvoidance(Vector2 dir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, 1.5f, obstacleLayer);
        if (hit.collider != null)
        {
            bool hitMyTarget = false;
            if (target != null && (hit.collider.transform == target || hit.collider.transform.IsChildOf(target))) hitMyTarget = true;
            if (!hitMyTarget) { dir += hit.normal * avoidanceForce; return dir.normalized; }
        }
        return dir;
    }

    bool ShouldWaitForTank()
    {
        Guard[] guards = FindObjectsByType<Guard>(FindObjectsSortMode.None);
        float myX = transform.position.x;
        float forwardMostX = 9999f; 
        bool hasRelevantTank = false;

        foreach (Guard g in guards) 
        {
            if (g.CompareTag("Untagged")) continue;
            float guardX = g.transform.position.x;
            if (guardX < myX && Mathf.Abs(myX - guardX) < maxTankWaitDistance) { if (guardX < forwardMostX) forwardMostX = guardX; hasRelevantTank = true; }
        }
        return hasRelevantTank && myX < (forwardMostX + safeDistanceBehindTank);
    }

    bool IsBlockingCart()
    {
        if (myCart == null) return false;
        if (transform.position.x < myCart.position.x && Vector2.Distance(transform.position, myCart.position) < 3.5f)
            if (Mathf.Abs(transform.position.y - myCart.position.y) < 1.2f) return true;
        return false;
    }

    void FaceTarget(Vector3 targetPos) => FaceTowards(targetPos.x);

    void AimAtTarget()
    {
        if (firePoint != null && target != null)
        {
            Vector3 direction = target.position - firePoint.position;
            if (transform.localScale.x < 0) { direction.x = -direction.x; direction.y = -direction.y; }
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            firePoint.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    protected override void OnDeath()
    {
        if (TryGetComponent<EnemyStats>(out EnemyStats stats)) stats.GiveGold();
        else if (GameManager.Instance != null) GameManager.Instance.UnregisterEnemy();

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);
    }
}
