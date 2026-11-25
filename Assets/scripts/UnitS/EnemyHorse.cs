using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))] 
public class EnemyHorse : UnitBase
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Характеристики")]
    public float attackCooldown = 2.0f; 
    public int damage = 25;             

    [Header("Навігація та Агро")]
    public float aggroRadius = 6.0f; 
    
    // --- ДИСЦИПЛІНА МАРШУ ---

    private float retargetTimer = 0f;

    private Transform target;
    private float nextAttackTime = 0f;
    private bool hasHitThisAttack = false;

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

        if (healthBar != null)
        {
            healthBar.targetTransform = transform;
            healthBar.SetHealth(currentHealth, effectiveMaxHealth);
        }
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

        if (target != null && (target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy)) target = null;

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
                if (sep.magnitude > 0.2f) rb.linearVelocity = sep * (speed * 0.4f);
                else rb.linearVelocity = Vector2.zero;

                if (animator) animator.SetBool("IsMoving", false);

                if (Time.time >= nextAttackTime) { Attack(); nextAttackTime = Time.time + attackCooldown; }
            }
            else
            {
                Vector3 dest = isStructure ? target.position : GetDynamicEngagementPosition(target);
                MoveTowards(dest, isStructure, true);
            }
        }
        else
        {
            Vector3 forwardPos = transform.position + Vector3.left * 5f;
            FaceDirection(forwardPos);
            MoveTowards(forwardPos, false, false);
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

    // --- ПРОСУНУТИЙ АЛГОРИТМ ОБГОНУ ДЛЯ ВОРОЖИХ КОНЕЙ ---
    Vector2 GetSeparationVector()
    {
        Vector2 separation = Vector2.zero;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 1.2f);
        foreach (var col in nearby)
        {
            if (col.gameObject != gameObject && col.CompareTag(gameObject.tag) && !col.isTrigger)
            {
                Vector2 diff = transform.position - col.transform.position;
                if (diff.magnitude > 0.01f)
                {
                    float pushForce = 1f - (diff.magnitude / 1.2f);
                    
                    if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y)) 
                    {
                        float dodgeY = (diff.y >= 0) ? 1.5f : -1.5f; 
                        diff = new Vector2(diff.x * 0.3f, dodgeY); 
                    }
                    
                    separation += diff.normalized * pushForce;
                }
            }
        }
        return separation;
    }

    void MoveTowards(Vector3 destination, bool isStructureTarget, bool useSeparation)
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

        // --- ФАЗА БОЮ ТА ОБГОНУ ---
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

        // Кіннота використовує сильну сепарацію (2.5) щоб протиснутись крізь піхоту
        if (useSeparation) direction += GetSeparationVector() * 2.5f;
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

        if (GameManager.Instance != null)
        {
            var playerUnits = GameManager.Instance.GetPlayerUnits();
            for (int i = 0; i < playerUnits.Count; i++) Check(playerUnits[i]);
        }

        if (closestTarget != null && minDistance <= aggroRadius) target = closestTarget;
        else if (GameManager.Instance != null && GameManager.Instance.castle != null) target = GameManager.Instance.castle.transform;
    }

    void FaceDirection(Vector3 targetPos) => FaceTowards(targetPos.x);

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

    protected override void OnDeath()
    {
        if (TryGetComponent<EnemyStats>(out EnemyStats stats)) stats.GiveGold();
        else if (GameManager.Instance != null) GameManager.Instance.UnregisterEnemy();

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);
    }

    // У цього юніта смуга здоров'я — окремий компонент, а не Image з fillAmount.
    protected override void UpdateHealthBar()
    {
        if (healthBar != null) healthBar.SetHealth(currentHealth, effectiveMaxHealth);
    }

    protected override void HideHealthBar()
    {
        if (healthBar != null) healthBar.gameObject.SetActive(false);
    }
}
