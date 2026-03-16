using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))] 
public class EnemyArcher : MonoBehaviour
{
    [Header("Ефекти Grow Empire")]
    public Image healthBarFill;
    public GameObject hitParticlePrefab;
    private Color defaultColor = Color.white;

    [Header("Характеристики")]
    public float speed = 2.5f;
    public float attackRange = 6.5f;
    public float timeBetweenShots = 2.0f;
    public int damage = 10;
    public int maxHealth = 40; 
    
    [Header("Навігація та Агро")]
    public LayerMask obstacleLayer; 
    public float avoidanceForce = 2.0f;
    public float aggroRadius = 8.0f; 
    private float retargetTimer = 0f;

    [Header("Поведінка")]
    private float safeDistanceBehindTank = 2.0f;
    private float maxTankWaitDistance = 15.0f; 

    [Header("Стрільба")]
    public GameObject arrowPrefab;
    public Transform firePoint;

    private int currentHealth;
    private Animator animator;
    private Transform target;
    private float nextShotTime;
    
    private Vector3 originalScale;
    private int _maxHealth;
    private Rigidbody2D rb;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;

    private Transform myCart;
    private UnitStats myStats;

    private int _baseMaxHealth;
    private int _baseDamage;

    void Awake()
    {
        _baseMaxHealth = maxHealth;
        _baseDamage = damage;
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        myStats = GetComponent<UnitStats>();
        originalScale = transform.localScale;

        if (GameManager.Instance != null)
        {
            _maxHealth = GameManager.Instance.GetScaledEnemyHealth(_baseMaxHealth);
            damage = Mathf.RoundToInt(_baseDamage * GameManager.Instance.GetEnemyDamageMultiplier());
        }
        else { _maxHealth = _baseMaxHealth; }

        currentHealth = _maxHealth;
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
            if (animator) animator.SetBool("IsRunning", true);
            if (rb != null) rb.linearVelocity = new Vector2(-speed, 0f);
            return;
        }

        if (target != null && (target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy)) target = null;

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f) { FindTarget(); retargetTimer = 0.25f; }

        Vector2 finalVelocity = Vector2.zero;
        bool shouldMove = false;

        if (target != null)
        {
            bool isStructure = target.TryGetComponent<Spikes>(out _) || target.TryGetComponent<Wall>(out _);
            float distanceToTarget;

            if (isStructure)
            {
                Collider2D targetCol = target.GetComponent<Collider2D>();
                if (targetCol != null) distanceToTarget = Vector2.Distance(transform.position, targetCol.ClosestPoint(transform.position));
                else distanceToTarget = Mathf.Abs(transform.position.x - target.position.x);
            }
            else distanceToTarget = Vector2.Distance(transform.position, target.position);

            FaceTarget(target.position);

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
                    Vector3 targetPosFixed = target.position;
                    if (isStructure) targetPosFixed.y = transform.position.y;

                    Vector2 direction = (targetPosFixed - transform.position).normalized;
                    if (IsBlockingCart()) direction = ApplyCartAvoidance(direction);
                    direction = ApplyWallAvoidance(direction);

                    finalVelocity = direction * speed;
                    shouldMove = true;
                    if (firePoint) firePoint.localRotation = Quaternion.identity;
                }
            }
            else
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
        }
        else
        {
            if (!ShouldWaitForTank())
            {
                Vector3 destination = transform.position + Vector3.left;
                FaceTarget(destination);
                Vector2 direction = (destination - transform.position).normalized;
                if (IsBlockingCart()) direction = ApplyCartAvoidance(direction);
                direction = ApplyWallAvoidance(direction);

                finalVelocity = direction * speed;
                shouldMove = true;
            }
        }

        rb.linearVelocity = finalVelocity; 
        if (animator) animator.SetBool("IsRunning", shouldMove);
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

        Knight[] knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        foreach (Knight k in knights) CheckDistance(k.transform);
        Archer[] archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        foreach (Archer a in archers) CheckDistance(a.transform);
        Spearman[] spearmen = FindObjectsByType<Spearman>(FindObjectsSortMode.None); 
        foreach (Spearman s in spearmen) CheckDistance(s.transform);
        Cavalry[] cavs = FindObjectsByType<Cavalry>(FindObjectsSortMode.None); 
        foreach (Cavalry c in cavs) CheckDistance(c.transform);

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
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot);
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

    void FaceTarget(Vector3 targetPos) { float absX = Mathf.Abs(originalScale.x); transform.localScale = new Vector3(targetPos.x > transform.position.x ? absX : -absX, originalScale.y, originalScale.z); }

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

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;
        currentHealth -= damageAmount;
        UpdateHealthBar();

        Vector3 popupPos = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.5f, 1.2f), 0);
        GameManager.CreateDamagePopup(popupPos, damageAmount);

        if (hitParticlePrefab != null)
        {
            GameObject particles = Instantiate(hitParticlePrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(particles, 1f);
        }

        if (spriteRenderer != null) StartCoroutine(FlashColor());

        if (currentHealth <= 0) Die();
    }

    void UpdateHealthBar() { if (healthBarFill != null) healthBarFill.fillAmount = Mathf.Clamp01((float)currentHealth / _maxHealth); }

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
        
        if (healthBarFill != null && healthBarFill.transform.parent != null) healthBarFill.transform.parent.gameObject.SetActive(false);
        Transform shadow = transform.Find("Shadow");
        if (shadow != null) shadow.gameObject.SetActive(false);
        
        if (TryGetComponent<EnemyStats>(out EnemyStats stats)) stats.GiveGold();
        else if (GameManager.Instance != null) GameManager.Instance.UnregisterEnemy();
        
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);
        
        if (animator) { animator.Rebind(); animator.Update(0f); animator.enabled = false; }
        if (rb) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; }
        Collider2D col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        
        transform.Rotate(0, 0, -90);
        if (spriteRenderer != null) { spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f); spriteRenderer.sortingOrder = 0; }
        this.enabled = false;
        Destroy(gameObject, 5f);
    }
}