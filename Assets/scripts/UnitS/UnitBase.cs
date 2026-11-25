using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Спільна основа для всіх бойових юнітів режиму оборони.
///
/// Вісім скриптів юнітів майже дослівно повторювали отримання шкоди, смугу
/// здоров'я, спалах при влучанні, розворот спрайта і смерть. Тут лежить саме
/// ця механіка; прицілювання, рух і атака лишаються в кожному класі своїми —
/// вони справді різні.
///
/// Імена серіалізованих полів збережені навмисно: Unity зіставляє значення
/// на префабах за іменем поля, тож перенесення в базовий клас нічого не
/// втрачає. Поле Guard називалося health — воно підхоплюється через
/// FormerlySerializedAs.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class UnitBase : MonoBehaviour
{
    [Header("Ефекти")]
    public Image healthBarFill;
    public GameObject hitParticlePrefab;

    [Header("Характеристики")]
    public float speed = 2f;
    public float attackRange = 1f;

    [FormerlySerializedAs("health")]
    public int maxHealth = 100;

    [Header("Навігація")]
    public LayerMask obstacleLayer;
    public float avoidanceForce = 2f;

    [Tooltip("Дистанція, на якій юніт ламає стрій і кидається в бій")]
    public float aggroRange = 4.5f;

    [Header("Компоненти")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    public int currentHealth;

    protected Rigidbody2D rb;
    protected UnitStats myStats;
    protected Vector3 originalScale;
    protected Color defaultColor = Color.white;
    protected bool isDead = false;

    /// <summary>Висота, на якій юніт заспавнився — лінія маршу.</summary>
    protected float startY;

    /// <summary>
    /// Фактичний максимум здоров'я. У ворогів відрізняється від maxHealth:
    /// GameManager масштабує його під складність хвилі.
    /// </summary>
    protected int effectiveMaxHealth;

    public bool IsDead => isDead;

    // ------------------------------------------------------------------
    // Параметри, якими класи відрізняються
    // ------------------------------------------------------------------

    /// <summary>
    /// У юнітів гравця спрайт намальований ліворуч, тому "вправо" — це
    /// від'ємний scale. У ворогів навпаки.
    /// </summary>
    protected virtual bool FlipInverted => false;

    /// <summary>Скільки лежить труп, перш ніж зникнути.</summary>
    protected virtual float CorpseLifeTime => 5f;

    /// <summary>Колір трупа.</summary>
    protected virtual Color CorpseColor => new Color(0.6f, 0.6f, 0.6f);

    // ------------------------------------------------------------------
    // Життєвий цикл
    // ------------------------------------------------------------------

    protected virtual void Start()
    {
        CacheComponents();
    }

    /// <summary>
    /// Кешує компоненти й виставляє фізику. Викликається зі Start кожного
    /// класу; якщо посилання вже проставлене в інспекторі — не чіпається.
    /// </summary>
    protected void CacheComponents()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (myStats == null) myStats = GetComponent<UnitStats>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        originalScale = transform.localScale;
        startY = transform.position.y;

        // currentHealth і effectiveMaxHealth тут НЕ виставляємо навмисно:
        // юніти гравця рахують maxHealth з EconomyConfig уже після base.Start(),
        // а вороги масштабують його під складність хвилі. Якби база щось
        // присвоїла тут, вона б перебила ще не пораховані значення.
    }

    /// <summary>Відновлення здоров'я із збереження.</summary>
    public void LoadState(int savedHealth)
    {
        currentHealth = savedHealth;
        UpdateHealthBar();
    }

    // ------------------------------------------------------------------
    // Шкода
    // ------------------------------------------------------------------

    public virtual void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        UpdateHealthBar();

        Vector3 popupPos = transform.position + new Vector3(
            Random.Range(-0.3f, 0.3f), Random.Range(0.5f, 1.2f), 0f);
        GameManager.CreateDamagePopup(popupPos, damageAmount);

        SpawnHitParticles();

        if (spriteRenderer != null) StartCoroutine(FlashColor());

        if (currentHealth <= 0) Die();
    }

    protected virtual void UpdateHealthBar()
    {
        if (healthBarFill == null) return;
        int max = effectiveMaxHealth > 0 ? effectiveMaxHealth : maxHealth;
        healthBarFill.fillAmount = Mathf.Clamp01((float)currentHealth / max);
    }

    protected void SpawnHitParticles()
    {
        if (hitParticlePrefab == null) return;

        GameObject particles = PoolRegistry.Spawn(
            hitParticlePrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        PoolRegistry.DespawnAfter(particles, 1f);
    }

    protected IEnumerator FlashColor()
    {
        spriteRenderer.color = new Color(1f, 0.4f, 0.4f);
        yield return new WaitForSeconds(0.1f);
        if (!isDead && spriteRenderer != null) spriteRenderer.color = defaultColor;
    }

    // ------------------------------------------------------------------
    // Розворот
    // ------------------------------------------------------------------

    protected void FaceTowards(float targetX)
    {
        if (Mathf.Approximately(targetX, transform.position.x)) return;

        float absX = Mathf.Abs(originalScale.x);
        bool goingRight = targetX > transform.position.x;
        bool positive = FlipInverted ? !goingRight : goingRight;

        transform.localScale = new Vector3(
            positive ? absX : -absX, originalScale.y, originalScale.z);
    }

    // ------------------------------------------------------------------
    // Смерть
    // ------------------------------------------------------------------

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        gameObject.tag = "Untagged";

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Нагорода, зняття з обліку, звук — у кожного класу своє.
        OnDeath();

        HideHealthBar();

        Transform shadow = transform.Find("Shadow");
        if (shadow != null) shadow.gameObject.SetActive(false);

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.enabled = false;
        }

        transform.Rotate(0f, 0f, -90f);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = CorpseColor;
            spriteRenderer.sortingOrder = 0;
        }

        // Зупиняємо логіку, але компонент лишаємо: посилання на нього
        // можуть ще існувати, а відсіюються мертві юніти за тегом.
        enabled = false;
        Destroy(gameObject, CorpseLifeTime);
    }

    /// <summary>Що зробити в момент смерті: золото, облік, звук.</summary>
    protected abstract void OnDeath();

    protected virtual void HideHealthBar()
    {
        if (healthBarFill != null && healthBarFill.transform.parent != null)
            healthBarFill.transform.parent.gameObject.SetActive(false);
    }
}
