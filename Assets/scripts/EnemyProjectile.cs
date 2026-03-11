using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Характеристики")]
    public float speed = 10f;
    public float stickTime = 5f; // Скільки секунд стріла стирчить у землі/тілі
    public bool stickToTarget = true;

    private Vector3 startPosition;
    private float maxTravelDistance;
    private int damage;
    private bool hasHit = false;
    private Rigidbody2D rb;
    
    // Зберігаємо ціль, щоб перевіряти, чи вона ще жива
    private Transform trackedTarget; 

    // Додано необов'язковий параметр targetUnit (можете передавати його з ворожого лучника)
    public void Initialize(Vector3 targetPos, int dmg, Transform targetUnit = null)
    {
        damage = dmg;
        startPosition = transform.position;
        trackedTarget = targetUnit;
        
        // Вираховуємо максимальну дистанцію. 
        // Додаємо +1.5f юніта, щоб стріла могла трохи пролетіти за спину ворога (якщо промаже), але не летіла аж у стіну!
        maxTravelDistance = Vector3.Distance(startPosition, targetPos) + 1.5f;

        Vector3 direction = (targetPos - transform.position).normalized;
        
        // Поворот носом до цілі
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * speed; 
        }
    }

    void Update()
    {
        if (hasHit) return;

        // ПЕРЕВІРКА 1: Якщо ми передали юніта і він помер під час польоту стріли
        if (trackedTarget != null && (!trackedTarget.gameObject.activeInHierarchy || trackedTarget.CompareTag("Untagged")))
        {
            FallToGround();
            return;
        }

        // ПЕРЕВІРКА 2: Якщо стріла пролетіла свою дистанцію, а нікого не зачепила 
        // (наприклад, юніт встиг померти, а ми не передали Transform)
        if (Vector3.Distance(startPosition, transform.position) >= maxTravelDistance)
        {
            FallToGround();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;
        
        // Ігноруємо ворогів (своїх) та інші стріли
        if (collision.CompareTag("Enemy") || collision.CompareTag("Projectile")) return;

        bool hitSomething = false;
        Transform hitTransform = collision.transform;

        // 1. Влучили в Барикаду (Spikes)
        if (collision.TryGetComponent<Spikes>(out Spikes spikes))
        {
            spikes.TakeDamage(damage);
            hitSomething = true; 
        }
        // 2. Влучили в Стіну
        else if (collision.TryGetComponent<Wall>(out Wall w))
        {
            w.TakeDamage(damage);
            hitSomething = true;
        }
        // 3. Влучили в Юнітів гравця
        else if (collision.TryGetComponent<Knight>(out Knight k))
        {
            k.TakeDamage(damage);
            hitSomething = true;
        }
        else if (collision.TryGetComponent<Archer>(out Archer a))
        {
            a.TakeDamage(damage);
            hitSomething = true;
        }
        else if (collision.TryGetComponent<Spearman>(out Spearman s))
        {
            s.TakeDamage(damage);
            hitSomething = true;
        }
        // 4. Влучили в землю або абстрактні межі карти
        else if (collision.CompareTag("Ground") || collision.CompareTag("Boundary"))
        {
            hitSomething = true;
            hitTransform = null; // Не прилипаємо до невидимих меж
        }

        if (hitSomething)
        {
            HitTarget(hitTransform);
        }
    }

    void HitTarget(Transform targetObj)
    {
        hasHit = true;

        // Якщо влучили в юніта - робимо стрілу його дочірнім об'єктом, щоб вона "встрягла"
        if (stickToTarget && targetObj != null && !targetObj.CompareTag("Ground"))
        {
            transform.SetParent(targetObj);
        }

        DisablePhysics();
        StartCoroutine(FadeAndDestroy());
    }

    void FallToGround()
    {
        hasHit = true;
        
        // Трохи опускаємо її вниз, ніби вона встромилась у землю
        transform.position += new Vector3(0, -0.2f, 0); 
        
        DisablePhysics();
        StartCoroutine(FadeAndDestroy());
    }

    void DisablePhysics()
    {
        // Зупиняємо рух
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Вимикаємо колайдер, щоб вона не пошкодила когось випадково
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Відправляємо на задній план (за юнітів)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = -10; 
    }

    IEnumerator FadeAndDestroy()
    {
        // Чекаємо основний час
        yield return new WaitForSeconds(stickTime - 1f); 
        
        // Останню секунду плавно розчиняємо стрілу (робимо прозорою)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float fadeDuration = 1f;
            float t = 0;
            Color startColor = sr.color;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }
        
        // Остаточно знищуємо
        Destroy(gameObject);
    }
}