using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))] 
public class Guard : MonoBehaviour
{
    [Header("Ефекти Grow Empire")]
    public Image healthBarFill;
    public GameObject hitParticlePrefab;
    private Color defaultColor = Color.white;

    [Header("Характеристики")]
    public float speed = 1.5f;
    public float attackRange = 1.2f; 
    public int damage = 15;
    public int health = 60;

    [Header("Навігація та Агро")]
    public LayerMask obstacleLayer; 
    public float avoidanceForce = 2.0f;
    public float aggroRadius = 3.5f; 
    private float retargetTimer = 0f;

    [Header("Атака")]
    public float attackCooldown = 1.5f;
    private float nextAttackTime = 0f;

    private Animator animator;
    private Rigidbody2D rb; 
    private SpriteRenderer spriteRenderer;
    private int _maxHealth; 
    private bool isDead = false;
    private Vector3 originalScale;

    private Transform target; 
    private Transform myCart;
    private float laneOffset; 
    private float cartSafetyRadius = 2.5f; 

    private UnitStats myStats;
    private bool hasHitThisAttack = false;

    private int _baseHealth;
    private int _baseDamage;

    void Awake()
    {
        _baseHealth = health;
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

        float randomDir = Random.value > 0.5f ? 1f : -1f;
        laneOffset = Random.Range(0.8f, 1.5f) * randomDir;

        if (GameManager.Instance != null)
        {
            int wave = GameManager.Instance.currentWave;
            health = EconomyConfig.GetEnemyHealth(_baseHealth, wave);
            damage = EconomyConfig.GetEnemyDamage(_baseDamage, wave);
            GameManager.Instance.RegisterEnemy();
        }

        _maxHealth = health;

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

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f)
        {
            FindTarget();
            retargetTimer = 0.25f;
        }

        if (target != null && (target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy)) 
            target = null;

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

            if (distanceToTarget <= attackRange)
            {
                StopMoving();
                if (Time.time >= nextAttackTime)
                {
                    StartAttack(); 
                    nextAttackTime = Time.time + attackCooldown;
                }
                return; 
            }
        }

        if (IsCartTooClose())
        {
            DodgeCart();
            return; 
        }

        if (target != null)
        {
            FaceTarget(target.position);
            bool isStructure = target.TryGetComponent<Spikes>(out _) || target.TryGetComponent<Wall>(out _);
            Vector3 dest = target.position;
            float distance = Vector2.Distance(transform.position, target.position);

            if (!isStructure && distance > 3.5f) dest.y += laneOffset;
            MoveTowards(dest, isStructure);
        }
        else
        {
            float baseY = (myCart != null) ? myCart.position.y : 0;
            Vector3 destination = transform.position + Vector3.left;
            destination.y = baseY + laneOffset;
            FaceTarget(destination); 
            MoveTowards(destination, false);
        }
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

        Knight[] knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        foreach (Knight k in knights) CheckDistance(k.transform);

        Archer[] archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        foreach (Archer a in archers) CheckDistance(a.transform);
        
        Spearman[] spearmen = FindObjectsByType<Spearman>(FindObjectsSortMode.None); 
        foreach (Spearman s in spearmen) CheckDistance(s.transform);

        if (closestTarget != null && minDistance <= aggroRadius) target = closestTarget;
        else if (GameManager.Instance != null && GameManager.Instance.castle != null) target = GameManager.Instance.castle.transform;
    }

    void MoveTowards(Vector3 destination, bool isStructureTarget)
    {
        if (animator) animator.SetBool("IsRunning", true);
        Vector3 targetPosFixed = new Vector3(destination.x, destination.y, transform.position.z);
        if (isStructureTarget) targetPosFixed = new Vector3(destination.x, transform.position.y, transform.position.z);

        Vector2 direction = (targetPosFixed - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 1.5f, obstacleLayer);
        
        if (hit.collider != null && hit.collider.gameObject != gameObject)
        {
            bool hitMyTarget = false;
            if (target != null && (hit.collider.transform == target || hit.collider.transform.IsChildOf(target))) hitMyTarget = true;
            if (!hitMyTarget) { direction += hit.normal * avoidanceForce; direction.Normalize(); }
        }
        rb.linearVelocity = direction * speed; 
    }

    void StopMoving() { rb.linearVelocity = Vector2.zero; if (animator) animator.SetBool("IsRunning", false); }

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
        if (animator) animator.SetBool("IsRunning", true);
    }

    void FaceTarget(Vector3 targetPos)
    {
        float absX = Mathf.Abs(originalScale.x);
        transform.localScale = new Vector3(targetPos.x > transform.position.x ? absX : -absX, originalScale.y, originalScale.z); 
    }

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
        else if (target.TryGetComponent<Wall>(out Wall c)) { c.TakeDamage(finalDamage); if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.1f, 0.2f); if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.castleDamage); }
        else if (target.TryGetComponent<Spikes>(out Spikes spikes)) { spikes.TakeDamage(finalDamage); if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.05f, 0.1f); }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;
        health -= damageAmount;
        UpdateHealthBar();

        Vector3 popupPos = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.5f, 1.2f), 0);
        GameManager.CreateDamagePopup(popupPos, damageAmount);

        if (hitParticlePrefab != null)
        {
            GameObject particles = Instantiate(hitParticlePrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(particles, 1f);
        }

        if (spriteRenderer != null) StartCoroutine(FlashColor());

        if (health <= 0) Die();
    }

    void UpdateHealthBar()
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = Mathf.Clamp01((float)health / _maxHealth);
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