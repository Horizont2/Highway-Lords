using UnityEngine;
using System.Collections;

public class Arrow : MonoBehaviour
{
    [Header("Характеристики")]
    public float speed = 15f;
    public float stickTime = 5f; 
    public bool stickToTarget = true; 

    private Transform target;
    private int damage;
    private bool hasHit = false;
    
    private Vector3 lastKnownPosition;

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
        if (hasHit) return; 

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
            if (isTargetAlive)
            {
                HitTarget();
            }
            else
            {
                // Якщо ворога вже нема, просто падаємо там, де він був
                FallToGround(); 
            }
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
            transform.SetParent(target); 
        }

        DisablePhysics();
        StartCoroutine(FadeAndDestroy());
    }

    void FallToGround()
    {
        hasHit = true;
        transform.position += new Vector3(0, -0.2f, 0); // Трохи в землю
        DisablePhysics();
        StartCoroutine(FadeAndDestroy());
    }

    void DisablePhysics()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false; // Вимикаємо колайдер

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = -10; // Ховаємо за юнітами
    }

    IEnumerator FadeAndDestroy()
    {
        yield return new WaitForSeconds(stickTime - 1f); 
        
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
        Destroy(gameObject);
    }
}