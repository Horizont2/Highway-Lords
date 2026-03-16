using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class EnemySpearman : MonoBehaviour
{
    [Header("Ефекти Grow Empire")]
    public Image healthBarFill;
    public GameObject hitParticlePrefab;
    private Color defaultColor = Color.white;

    [Header("Характеристики")]
    public float speed = 1.8f;
    public float attackRange = 2.5f;
    public float attackCooldown = 1.5f;
    public int damage = 15;
    public int maxHealth = 70;

    [Header("Навігація та Агро")]
    public LayerMask obstacleLayer; 
    public float avoidanceForce = 2.0f;
    public float aggroRadius = 4.5f;
    
    // --- ДИСЦИПЛІНА МАРШУ ---
    public float aggroRange = 4.5f; 
    private float startY;

    private float retargetTimer = 0f;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private int currentHealth;
    private int _maxHealth;
    private bool isDead = false;
    private Vector3 originalScale;

    private Transform target;
    private float nextAttackTime = 0f;
    private bool hasHitThisAttack = false;
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

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0; 
        rb.freezeRotation = true; 

        startY = transform.position.y; // Запам'ятовуємо позицію

        if (GameManager.Instance != null)
        {
            _maxHealth = GameManager.Instance.GetScaledEnemyHealth(_baseMaxHealth);
            damage = Mathf.RoundToInt(_baseDamage * GameManager.Instance.GetEnemyDamageMultiplier());
        }
        else { _maxHealth = _baseMaxHealth; }
        
        currentHealth = _maxHealth;
        if (spriteRenderer != null) defaultColor = spriteRenderer.color;
        UpdateHealthBar();
    }

    void Update()
    {
        if (isDead) return;

        if (GameManager.Instance != null && GameManager.Instance.isDefeated)
        {
            target = null;
            if (animator) animator.SetBool("IsMoving", true);
            rb.linearVelocity = new Vector2(-speed, 0f);
            return;
        }

        if (target != null && (!target.gameObject.activeInHierarchy || target.CompareTag("Untagged"))) target = null;

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f) { FindTarget(); retargetTimer = 0.25f; }

        if (target != null)
        {
            FaceDirection(target.position);
            bool isStructure = target.TryGetComponent<Spikes>(out _) || target.TryGetComponent<Wall>(out _);
            float distanceToTarget;

            if (isStructure)
            {
                Collider2D targetCol = target.GetComponent<Collider2D>();
                if (targetCol != null) distanceToTarget = Vector2.Distance(transform.position, targetCol.ClosestPoint(transform.position));
                else distanceToTarget = Mathf.Abs(transform.position.x - target.position.x);
            }
            else distanceToTarget = Vector2.Distance(transform.position, target.position);

            if (distanceToTarget <= attackRange)
            {
                Vector2 sep = GetSeparationVector();
                if (sep.magnitude > 0.2f) rb.linearVelocity = sep * (speed * 0.3f);
                else rb.linearVelocity = Vector2.zero;

                if (animator) animator.SetBool("IsMoving", false);

                if (Time.time >= nextAttackTime) { hasHitThisAttack = false; if (animator) animator.SetTrigger("Attack"); nextAttackTime = Time.time + attackCooldown; }
            }
            else
            {
                Vector3 dest = isStructure ? target.position : GetDynamicEngagementPosition(target);
                MoveTo(dest, isStructure, true);
            }
        }
        else
        {
            Vector3 leftDirection = transform.position + Vector3.left * 5f; 
            FaceDirection(leftDirection);
            MoveTo(leftDirection, false, false);
        }
    }

    Vector3 GetDynamicEngagementPosition(Transform targetTransform)
    {
        Vector3 basePos = targetTransform.position;
        float dirX = (transform.position.x > basePos.x) ? 1f : -1f;
        Vector3 frontSlot = basePos + new Vector3(dirX * (attackRange * 0.8f), 0, 0);

        Collider2D[] friends = Physics2D.OverlapCircleAll(frontSlot, 0.4f);
        int crowdCount = 0;
        foreach(var col in friends) {
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

    void MoveTo(Vector3 destination, bool isStructureTarget, bool useSeparation)
    {
        if (animator) animator.SetBool("IsMoving", true);

        // --- ФАЗА МАРШУ ---
        float distToTarget = Vector2.Distance(transform.position, destination);
        if (distToTarget > aggroRange)
        {
            float dirX = Mathf.Sign(destination.x - transform.position.x);
            float newY = Mathf.MoveTowards(transform.position.y, startY, speed * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            rb.linearVelocity = new Vector2(dirX * speed, 0);
            return; 
        }

        // --- ФАЗА БОЮ ---
        Vector3 targetPosFixed = new Vector3(destination.x, destination.y, transform.position.z);
        if (isStructureTarget) targetPosFixed = new Vector3(destination.x, transform.position.y, transform.position.z);

        Vector2 direction = (targetPosFixed - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 1.5f, obstacleLayer);
        
        if (hit.collider != null && hit.collider.gameObject != gameObject)
        {
            bool hitMyTarget = false;
            if (target != null && (hit.collider.transform == target || hit.collider.transform.IsChildOf(target))) hitMyTarget = true;
            if (!hitMyTarget) { float dodgeDirY = (transform.position.y >= hit.collider.bounds.center.y) ? 1f : -1f; direction += new Vector2(0, dodgeDirY) * avoidanceForce; direction.Normalize(); }
        }

        if (useSeparation) direction += GetSeparationVector() * 1.5f;
        rb.linearVelocity = direction.normalized * speed;
    }

    void FindTarget()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentSpikes != null)
        {
            Transform spikes = GameManager.Instance.currentSpikes.transform;
            if (transform.position.x > spikes.position.x - 2.0f) { target = spikes; return; }
        }

        float minDistance = Mathf.Infinity;
        Transform closestTarget = null;

        void Check(Transform t)
        {
            if (t == null || !t.gameObject.activeInHierarchy || t.CompareTag("Untagged")) return;
            float dist = Vector2.Distance(transform.position, t.position);
            if (dist < minDistance) { minDistance = dist; closestTarget = t; }
        }

        Knight[] knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        foreach (var k in knights) Check(k.transform);
        Spearman[] spearmen = FindObjectsByType<Spearman>(FindObjectsSortMode.None);
        foreach (var s in spearmen) Check(s.transform);
        Cavalry[] cavalries = FindObjectsByType<Cavalry>(FindObjectsSortMode.None);
        foreach (var c in cavalries) Check(c.transform);
        Archer[] archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        foreach (var a in archers) Check(a.transform);

        if (closestTarget != null && minDistance <= aggroRadius) target = closestTarget;
        else if (GameManager.Instance != null && GameManager.Instance.castle != null) target = GameManager.Instance.castle.transform;
    }

    void FaceDirection(Vector3 targetPos) { float absX = Mathf.Abs(originalScale.x); transform.localScale = new Vector3(targetPos.x > transform.position.x ? absX : -absX, originalScale.y, originalScale.z); }

    void Attack() { hasHitThisAttack = false; if (animator) animator.SetTrigger("Attack"); }

    public void Hit()
    {
        if (isDead || target == null || hasHitThisAttack) return;
        
        bool isStructure = target.TryGetComponent<Spikes>(out _) || target.TryGetComponent<Wall>(out _);
        float distanceToTarget;

        if (isStructure)
        {
            Collider2D targetCol = target.GetComponent<Collider2D>();
            distanceToTarget = targetCol != null ? Vector2.Distance(transform.position, targetCol.ClosestPoint(transform.position)) : Mathf.Abs(transform.position.x - target.position.x);
        }
        else distanceToTarget = Vector2.Distance(transform.position, target.position);

        if (distanceToTarget > attackRange + 1.5f) return;

        hasHitThisAttack = true;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit); 

        int finalDamage = damage;
        if (myStats != null)
        {
            UnitStats targetStats = target.GetComponent<UnitStats>();
            if (targetStats != null) finalDamage = Mathf.RoundToInt(damage * GameManager.GetDamageMultiplier(myStats.category, targetStats.category));
        }

        if (target.TryGetComponent<Knight>(out Knight k)) k.TakeDamage(finalDamage);
        else if (target.TryGetComponent<Archer>(out Archer a)) a.TakeDamage(finalDamage);
        else if (target.TryGetComponent<Spearman>(out Spearman s)) s.TakeDamage(finalDamage);
        else if (target.TryGetComponent<Cavalry>(out Cavalry cV)) cV.TakeDamage(finalDamage);
        else if (target.TryGetComponent<Wall>(out Wall c)) { c.TakeDamage(finalDamage); if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.castleDamage); }
        else if (target.TryGetComponent<Spikes>(out Spikes sp)) sp.TakeDamage(finalDamage);
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

    void UpdateHealthBar()
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = Mathf.Clamp01((float)currentHealth / _maxHealth);
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

        if (healthBarFill != null && healthBarFill.transform.parent != null) 
            healthBarFill.transform.parent.gameObject.SetActive(false);

        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; }
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        if (TryGetComponent<EnemyStats>(out EnemyStats stats)) stats.GiveGold();
        else if (GameManager.Instance != null) GameManager.Instance.UnregisterEnemy();
        
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);
        
        if (animator) { animator.Rebind(); animator.Update(0f); animator.enabled = false; }
        transform.Rotate(0, 0, -90);
        if (spriteRenderer != null) { spriteRenderer.color = Color.gray; spriteRenderer.sortingOrder = 0; }
        
        Destroy(this);
        Destroy(gameObject, 10f);
    }
}