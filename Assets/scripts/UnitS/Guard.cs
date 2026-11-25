using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))] 
public class Guard : UnitBase
{
    [Header("Характеристики")]
    public int damage = 15;

    [Header("Навігація та Агро")]
    public float aggroRadius = 3.5f; 
    
    // --- ДИСЦИПЛІНА МАРШУ ---

    private float retargetTimer = 0f;

    [Header("Атака")]
    public float attackCooldown = 1.5f;
    private float nextAttackTime = 0f;

    private Transform target; 
    private Transform myCart;
    private float laneOffset; 
    private float cartSafetyRadius = 2.5f; 

    private bool hasHitThisAttack = false;

    private int _baseHealth;
    private int _baseDamage;

    // Ворожі лучники щокадру питають, чи є попереду танк. Раніше кожен із них
    // робив повне сканування сцени — при 8 лучниках це 480 сканувань на
    // секунду. Реєстр підтримується самими вартовими, тож коштує нічого.
    private static readonly List<Guard> active = new List<Guard>();
    public static IReadOnlyList<Guard> Active => active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => active.Clear();

    void OnEnable() { if (!active.Contains(this)) active.Add(this); }

    // Die() вимикає компонент, тож мертвий вартовий виходить звідси сам.
    void OnDisable() { active.Remove(this); }

    void Awake()
    {
        _baseHealth = maxHealth;
        _baseDamage = damage;
    }

    protected override void Start()
    {
        base.Start();

        float randomDir = Random.value > 0.5f ? 1f : -1f;
        laneOffset = Random.Range(0.8f, 1.5f) * randomDir;

        if (GameManager.Instance != null)
        {
            effectiveMaxHealth = GameManager.Instance.GetScaledEnemyHealth(_baseHealth);
            damage = Mathf.RoundToInt(_baseDamage * GameManager.Instance.GetEnemyDamageMultiplier());
        }
        else { effectiveMaxHealth = _baseHealth; }

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

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f) { FindTarget(); retargetTimer = 0.25f; }

        if (target != null && (target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy)) 
            target = null;

        if (IsCartTooClose()) { DodgeCart(); return; }

        if (target != null)
        {
            FaceTarget(target.position);
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

                if (Time.time >= nextAttackTime)
                {
                    StartAttack(); 
                    nextAttackTime = Time.time + attackCooldown;
                }
            }
            else
            {
                Vector3 dest = isStructure ? target.position : GetDynamicEngagementPosition(target);
                MoveTowards(dest, isStructure, true);
            }
        }
        else
        {
            float baseY = (myCart != null) ? myCart.position.y : 0;
            Vector3 destination = transform.position + Vector3.left * 5f; // Далеко вперед
            destination.y = baseY + laneOffset;
            FaceTarget(destination); 
            MoveTowards(destination, false, false);
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

        // --- ФАЗА БОЮ ---
        Vector3 targetPosFixed = new Vector3(destination.x, destination.y, transform.position.z);
        if (isStructureTarget) targetPosFixed = new Vector3(destination.x, transform.position.y, transform.position.z);

        Vector2 direction = (targetPosFixed - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 1.5f, obstacleLayer);
        
        if (hit.collider != null && hit.collider.gameObject != gameObject)
        {
            bool hitMyTarget = false;
            if (target != null && (hit.collider.transform == target || hit.collider.transform.IsChildOf(target))) hitMyTarget = true;
            if (!hitMyTarget) direction += hit.normal * avoidanceForce; 
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

        void CheckDistance(Transform t)
        {
            if (t == null || !t.gameObject.activeInHierarchy || t.CompareTag("Untagged")) return;
            float dist = Vector2.Distance(transform.position, t.position);
            if (dist < minDistance) { minDistance = dist; closestTarget = t; }
        }

        if (GameManager.Instance != null)
        {
            var playerUnits = GameManager.Instance.GetPlayerUnits();
            for (int i = 0; i < playerUnits.Count; i++) CheckDistance(playerUnits[i]);
        }

        if (closestTarget != null && minDistance <= aggroRadius) target = closestTarget;
        else if (GameManager.Instance != null && GameManager.Instance.castle != null) target = GameManager.Instance.castle.transform;
    }

    bool IsCartTooClose()
    {
        if (myCart == null) return false;
        if (Vector2.Distance(transform.position, myCart.position) < cartSafetyRadius)
            if (Mathf.Abs(transform.position.y - myCart.position.y) < 1.0f) return true;
        return false;
    }

    void DodgeCart()
    {
        if (myCart == null) return;
        float dirY = (transform.position.y > myCart.position.y) ? 1f : -1f;
        Vector2 dodgeVector = new Vector2(-0.5f, dirY).normalized; 
        rb.linearVelocity = dodgeVector * (speed * 1.5f);
        if (animator) animator.SetBool("IsMoving", true);
    }

    void FaceTarget(Vector3 targetPos) => FaceTowards(targetPos.x);

    void StartAttack() { hasHitThisAttack = false; if (animator) animator.SetTrigger("Attack"); }

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
        else if (target.TryGetComponent<Wall>(out Wall c)) { c.TakeDamage(finalDamage); if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.1f, 0.2f); if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.castleDamage); }
        else if (target.TryGetComponent<Spikes>(out Spikes spikes)) { spikes.TakeDamage(finalDamage); if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.05f, 0.1f); }
    }

    protected override void OnDeath()
    {
        if (TryGetComponent<EnemyStats>(out EnemyStats stats)) stats.GiveGold();
        else if (GameManager.Instance != null) GameManager.Instance.UnregisterEnemy();

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);
    }
}
