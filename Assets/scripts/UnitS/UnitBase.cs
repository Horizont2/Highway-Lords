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

        if (spawnPopDuration > 0f) StartCoroutine(SpawnPopRoutine());

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
        PlayHitReaction();

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

    // Поворот і стиск обидва пишуть у localScale, тож вони мають
    // проходити через одне місце — інакше по черзі затирали б одне одного.
    private bool facingPositive = true;
    private float squashX = 1f;
    private float squashY = 1f;
    private float spawnScale = 1f;

    protected void FaceTowards(float targetX)
    {
        if (Mathf.Approximately(targetX, transform.position.x)) return;

        bool goingRight = targetX > transform.position.x;
        facingPositive = FlipInverted ? !goingRight : goingRight;

        ApplyScale();
    }

    private void ApplyScale()
    {
        float absX = Mathf.Abs(originalScale.x);
        transform.localScale = new Vector3(
            (facingPositive ? absX : -absX) * squashX * spawnScale,
            originalScale.y * squashY * spawnScale,
            originalScale.z);
    }

    // ------------------------------------------------------------------
    // Поява
    // ------------------------------------------------------------------

    [Header("Поява")]
    [Tooltip("З якого масштабу юніт виростає при появі. 0 вимикає ефект")]
    public float spawnPopDuration = 0.26f;
    [Range(0f, 1f)] public float spawnFromScale = 0.55f;

    /// <summary>
    /// Юніт виростає з перельотом, а не виникає в одному кадрі. Разом із
    /// пилом під ногами це єдине, що відрізняє "з'явився" від "вискочив".
    /// </summary>
    private IEnumerator SpawnPopRoutine()
    {
        float duration = spawnPopDuration;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            spawnScale = Mathf.LerpUnclamped(spawnFromScale, 1f, Easing.OutBack(t / duration, 2f));
            if (!isDead) ApplyScale();
            yield return null;
        }

        spawnScale = 1f;
        if (!isDead) ApplyScale();
    }

    // ------------------------------------------------------------------
    // Реакція на влучання
    // ------------------------------------------------------------------

    [Header("Відчуття удару")]
    [Tooltip("Наскільки юніт сплющується від влучання")]
    [Range(0f, 0.5f)] public float hitSquash = 0.22f;
    [Tooltip("Скільки триває стиск і повернення")]
    public float hitSquashDuration = 0.18f;

    private Coroutine squashRoutine;

    /// <summary>
    /// Коротке "ой" від удару: юніт присідає впоперек і пружно повертається.
    /// Читається навіть краще за спалах кольором, бо впадає в око рухом.
    /// </summary>
    protected void PlayHitReaction()
    {
        if (isDead || hitSquash <= 0f) return;

        if (squashRoutine != null) StopCoroutine(squashRoutine);
        squashRoutine = StartCoroutine(SquashRoutine());
    }

    private IEnumerator SquashRoutine()
    {
        float duration = Mathf.Max(0.01f, hitSquashDuration);
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            // Punch: різко в стиск і пружно назад, без ривка в кінці.
            float k = Easing.Punch(time / duration);
            squashX = 1f + hitSquash * k;
            squashY = 1f - hitSquash * k;

            if (!isDead) ApplyScale();
            yield return null;
        }

        squashX = 1f;
        squashY = 1f;
        if (!isDead) ApplyScale();

        squashRoutine = null;
    }

    // ------------------------------------------------------------------
    // Смерть
    // ------------------------------------------------------------------

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        gameObject.tag = "Untagged";

        // Інакше труп застигне сплющеним на тому кадрі, де його добили.
        if (squashRoutine != null) { StopCoroutine(squashRoutine); squashRoutine = null; }
        squashX = 1f;
        squashY = 1f;
        ApplyScale();

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

        if (spriteRenderer != null) spriteRenderer.sortingOrder = 0;

        // Чим важчий юніт, тим відчутніший удар об землю.
        if (CameraShake.Instance != null)
        {
            float weight = Mathf.Clamp01(maxHealth / 200f);
            CameraShake.Instance.Shake(0.08f + weight * 0.12f, 0.04f + weight * 0.1f);
        }

        // Компонент лишаємо увімкненим, інакше корутина падіння не доживе
        // до кінця. Update кожного класу й так починається з if (isDead) return.
        StartCoroutine(DeathRoutine());
        Destroy(gameObject, CorpseLifeTime);
    }

    /// <summary>
    /// Падіння замість миттєвого повороту на 90°: юніт завалюється назад від
    /// того боку, куди дивився, темніє, лежить — і розчиняється наприкінці,
    /// а не зникає в одному кадрі.
    /// </summary>
    private IEnumerator DeathRoutine()
    {
        const float fallDuration = 0.32f;
        float fadeDuration = Mathf.Min(0.8f, CorpseLifeTime * 0.4f);
        float holdDuration = Mathf.Max(0f, CorpseLifeTime - fallDuration - fadeDuration);

        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, -90f);

        // Валиться назад: у бік, протилежний тому, куди дивився.
        float back = facingPositive ? -1f : 1f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + new Vector3(back * 0.35f, -0.12f, 0f);

        Color liveColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        Color deadColor = CorpseColor;

        float t = 0f;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fallDuration);

            // Прискорення: падіння важчає до кінця, а не йде рівно.
            float eased = k * k;

            transform.rotation = Quaternion.Slerp(startRot, endRot, eased);
            transform.position = Vector3.Lerp(startPos, endPos, Easing.OutCubic(k));

            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(liveColor, deadColor, k);

            yield return null;
        }

        transform.rotation = endRot;
        transform.position = endPos;
        if (spriteRenderer != null) spriteRenderer.color = deadColor;

        yield return new WaitForSeconds(holdDuration);

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            if (spriteRenderer != null)
            {
                Color c = deadColor;
                c.a = 1f - Easing.InCubic(t / fadeDuration);
                spriteRenderer.color = c;
            }
            yield return null;
        }
    }

    /// <summary>Що зробити в момент смерті: золото, облік, звук.</summary>
    protected abstract void OnDeath();

    protected virtual void HideHealthBar()
    {
        if (healthBarFill != null && healthBarFill.transform.parent != null)
            healthBarFill.transform.parent.gameObject.SetActive(false);
    }
}
