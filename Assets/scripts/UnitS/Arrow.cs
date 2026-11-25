using UnityEngine;
using System.Collections;

/// <summary>
/// Стріла лучника гравця.
///
/// Чому не SetParent: раніше при влучанні стріла ставала дочірньою цілі, щоб
/// триматися за неї. Але тоді вона гинула разом із ціллю, і повертати її в
/// пул було нічим. Тепер вона запам'ятовує ціль і зсув, і щокадру тримається
/// на місці — вигляд той самий, але екземпляр належить пулу, а не трупу.
/// </summary>
public class Arrow : MonoBehaviour, IPoolable
{
    [Header("Характеристики")]
    public float speed = 15f;
    public float stickTime = 5f;
    public bool stickToTarget = true;

    private Transform target;
    private int damage;
    private bool hasHit = false;

    private Vector3 lastKnownPosition;

    // Куди встрягли
    private Transform stuckInto;
    private Vector3 stuckOffset;

    private Collider2D myCollider;
    private SpriteRenderer sprite;
    private int baseSortingOrder;
    private Color baseColor = Color.white;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();

        if (sprite != null)
        {
            baseSortingOrder = sprite.sortingOrder;
            baseColor = sprite.color;
        }
    }

    /// <summary>
    /// Пул віддає той самий екземпляр, тож усе, що змінилося за попередній
    /// політ — вимкнений колайдер, прозорість, порядок малювання — треба
    /// повернути до вихідного.
    /// </summary>
    public void OnSpawnFromPool()
    {
        hasHit = false;
        target = null;
        stuckInto = null;

        StopAllCoroutines();

        if (myCollider != null) myCollider.enabled = true;

        if (sprite != null)
        {
            sprite.sortingOrder = baseSortingOrder;
            sprite.color = baseColor;
        }
    }

    public void Initialize(Transform targetTransform, int damageAmount)
    {
        target = targetTransform;
        damage = damageAmount;

        if (target != null)
        {
            lastKnownPosition = target.position;
            RotateTowards(lastKnownPosition);
        }
        else
        {
            // Якщо цілі немає при спавні, просто падаємо поруч
            lastKnownPosition = transform.position + new Vector3(1f, -1f, 0);
        }
    }

    void Update()
    {
        if (hasHit)
        {
            // Тримаємося за ціль, поки вона існує.
            if (stuckInto != null) transform.position = stuckInto.position + stuckOffset;
            return;
        }

        bool isTargetAlive = target != null && target.gameObject.activeInHierarchy && !target.CompareTag("Untagged");

        if (isTargetAlive)
        {
            lastKnownPosition = target.position;
        }

        // Рухаємось до останньої точки
        transform.position = Vector3.MoveTowards(transform.position, lastKnownPosition, speed * Time.deltaTime);
        RotateTowards(lastKnownPosition);

        // Якщо долетіли до точки
        if (Vector3.Distance(transform.position, lastKnownPosition) < 0.2f)
        {
            if (isTargetAlive) HitTarget();
            else FallToGround();
        }
    }

    void RotateTowards(Vector3 targetPos)
    {
        Vector2 direction = targetPos - transform.position;
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void HitTarget()
    {
        hasHit = true;

        // Викликаємо універсальний метод отримання шкоди для БУДЬ-ЯКОГО ворога
        if (target != null && target.CompareTag("Enemy"))
        {
            target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }

        if (stickToTarget && target != null)
        {
            stuckInto = target;
            stuckOffset = transform.position - target.position;
        }

        DisablePhysics();
        StartCoroutine(FadeAndRelease());
    }

    void FallToGround()
    {
        hasHit = true;
        stuckInto = null;
        transform.position += new Vector3(0, -0.2f, 0); // Трохи в землю
        DisablePhysics();
        StartCoroutine(FadeAndRelease());
    }

    void DisablePhysics()
    {
        if (myCollider != null) myCollider.enabled = false;
        if (sprite != null) sprite.sortingOrder = -10; // Ховаємо за юнітами
    }

    IEnumerator FadeAndRelease()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, stickTime - 1f));

        if (sprite != null)
        {
            float fadeDuration = 1f;
            float t = 0f;
            Color startColor = sprite.color;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
                sprite.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }

        PoolRegistry.Despawn(gameObject);
    }
}
