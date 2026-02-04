using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Guard : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar; 

    [Header("Характеристики")]
    public float speed = 1.5f;
    public float attackRange = 1.2f; 
    public int damage = 15;
    public int health = 60;
    public int goldReward = 15;

    [Header("Атака")]
    public float attackCooldown = 1.5f;
    private float nextAttackTime = 0f;

    // Компоненти
    private Animator animator;
    private Rigidbody2D rb; 
    private SpriteRenderer spriteRenderer;
    private int _maxHealth; 
    private bool isDead = false;
    private Vector3 originalScale;

    // Логіка руху
    private Transform target; 
    private Transform myCart;
    private float laneOffset; 
    private float cartSafetyRadius = 2.5f; 

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        float randomDir = Random.value > 0.5f ? 1f : -1f;
        laneOffset = Random.Range(0.8f, 1.5f) * randomDir;

        if (GameManager.Instance != null)
        {
            health += (GameManager.Instance.currentWave - 1) * 10;
            GameManager.Instance.RegisterEnemy();
        }

        _maxHealth = health;

        if (healthBar != null)
        {
            healthBar.targetTransform = transform; 
            healthBar.SetHealth(health, _maxHealth); 
        }

        Cart cartScript = FindFirstObjectByType<Cart>();
        if (cartScript != null) myCart = cartScript.transform;
    }

    void Update()
    {
        if (isDead) return;

        // Перевірка на "зникнення" цілі
        if (target == null || target.CompareTag("Untagged")) 
        {
            target = null;
            FindTarget(); // Шукаємо нову
        }

        // === 1. АТАКА ===
        if (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.position);

            if (distance <= attackRange)
            {
                StopMoving();
                if (Time.time >= nextAttackTime)
                {
                    Attack();
                    nextAttackTime = Time.time + attackCooldown;
                }
                return; 
            }
        }

        // === 2. УХИЛЕННЯ ВІД ВОЗА ===
        if (IsCartTooClose())
        {
            DodgeCart();
            return; 
        }

        // === 3. РУХ ДО ЦІЛІ ===
        if (target != null)
        {
            FaceTarget(target.position);
            
            Vector3 dest = target.position;
            float distance = Vector2.Distance(transform.position, target.position);

            // Якщо далеко - тримаємо смугу. Якщо близько - йдемо напролом
            if (distance > 3.5f) 
            {
                dest.y += laneOffset;
            }
            
            MoveTowards(dest);
        }
        else
        {
            float baseY = (myCart != null) ? myCart.position.y : 0;
            Vector3 destination = transform.position + Vector3.left;
            destination.y = baseY + laneOffset;
            
            FaceTarget(destination); 
            MoveTowards(destination);
        }
    }

    void FindTarget()
    {
        float minDistance = Mathf.Infinity;
        Transform closestTarget = null;

        // 1. Шукаємо Лицарів
        Knight[] knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        foreach (Knight k in knights)
        {
            if (k.CompareTag("Untagged")) continue; 
            float dist = Vector2.Distance(transform.position, k.transform.position);
            if (dist < minDistance) { minDistance = dist; closestTarget = k.transform; }
        }

        // 2. Шукаємо Лучників
        Archer[] archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        foreach (Archer a in archers)
        {
            if (a.CompareTag("Untagged")) continue;
            float dist = Vector2.Distance(transform.position, a.transform.position);
            if (dist < minDistance) { minDistance = dist; closestTarget = a.transform; }
        }

        // 3. === НОВЕ: Пріоритет атаки на Колючки ===
        if (GameManager.Instance != null && GameManager.Instance.currentSpikes != null)
        {
            float distToSpikes = Vector2.Distance(transform.position, GameManager.Instance.currentSpikes.transform.position);
            // Якщо колючки ближче, ніж будь-який лицар (або якщо лицарів немає)
            if (distToSpikes < minDistance)
            {
                closestTarget = GameManager.Instance.currentSpikes.transform;
                minDistance = distToSpikes;
            }
        }

        // 4. Шукаємо Замок (якщо нікого іншого)
        if (closestTarget == null && GameManager.Instance != null && GameManager.Instance.castle != null)
        {
            closestTarget = GameManager.Instance.castle.transform;
        }
        
        target = closestTarget;
    }

    void MoveTowards(Vector3 destination)
    {
        if (animator) animator.SetBool("IsRunning", true);
        Vector2 direction = (destination - transform.position).normalized;
        rb.linearVelocity = direction * speed; 
    }

    void StopMoving()
    {
        rb.linearVelocity = Vector2.zero; 
        if (animator) animator.SetBool("IsRunning", false);
    }

    bool IsCartTooClose()
    {
        if (myCart == null) return false;
        float dist = Vector2.Distance(transform.position, myCart.position);
        if (dist < cartSafetyRadius)
        {
            if (Mathf.Abs(transform.position.y - myCart.position.y) < 1.0f) return true;
        }
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
        if (targetPos.x > transform.position.x)
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z); 
        else
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z); 
    }

    void Attack()
    {
        if (animator) animator.SetTrigger("Attack");

        if (SoundManager.Instance != null) 
             SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit);

        if (target != null)
        {
            if (target.TryGetComponent<Knight>(out Knight k)) k.TakeDamage(damage);
            else if (target.TryGetComponent<Archer>(out Archer a)) a.TakeDamage(damage);
            else if (target.TryGetComponent<Castle>(out Castle c))
            {
                c.TakeDamage(damage);
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.1f, 0.2f); 
                if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.castleDamage);
            }
            // === НОВЕ: Атака колючок ===
            else if (target.TryGetComponent<Spikes>(out Spikes spikes))
            {
                spikes.TakeDamage(damage);
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.05f, 0.1f);
            }
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;
        health -= damageAmount;
        if (healthBar != null) healthBar.SetHealth(health, _maxHealth);
        if (GameManager.Instance != null) GameManager.Instance.ShowDamage(damageAmount, transform.position);
        if (health <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        gameObject.tag = "Untagged"; 

        if (healthBar != null) healthBar.gameObject.SetActive(false);
        Transform shadow = transform.Find("Shadow");
        if (shadow != null) shadow.gameObject.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy();
            GameManager.Instance.AddResource(ResourceType.Gold, goldReward);
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, goldReward, transform.position);
        }
        
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);

        if (animator) animator.enabled = false;
        
        if (rb) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; }
        
        Collider2D col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        transform.Rotate(0, 0, -90);
        if (spriteRenderer != null) { spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f); spriteRenderer.sortingOrder = 0; }

        this.enabled = false;
        Destroy(gameObject, 10f);
    }
}