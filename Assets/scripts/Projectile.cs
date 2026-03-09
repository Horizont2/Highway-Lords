using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Налаштування")]
    public float speed = 15f;
    public int damage = 20;
    public float rotationOffset = -90f; // Коригування кута спрайта
    [Tooltip("Висота, куди летить стріла (щоб влучати в тіло, а не в п'яти)")]
    public float targetHeightOffset = 0.5f; 

    private Transform targetTransform; // Для самонаведення
    private Vector3 targetPosition;    // Для прямого пострілу / остання відома позиція
    private bool homing = false;       // Режим самонаведення?
    private bool isInitialized = false;

    private Vector3 moveDir; 
    private bool hasHit = false; // Захист від подвійного урону

    // === ВАРІАНТ 1: Самонаведення (Для Вежі та Лучників) ===
    public void Initialize(Transform _target, int _damage)
    {
        targetTransform = _target;
        damage = _damage;
        homing = true;
        isInitialized = true;

        if (targetTransform != null) 
        {
            targetPosition = targetTransform.position + Vector3.up * targetHeightOffset;
        }
        else
        {
            targetPosition = transform.position + transform.right * 10f;
        }
        
        UpdateRotation();
    }

    // === ВАРІАНТ 2: Прямий постріл (по координатах) ===
    public void Initialize(Vector3 _targetPos, int _damage)
    {
        targetPosition = _targetPos + Vector3.up * targetHeightOffset;
        damage = _damage;
        homing = false;
        isInitialized = true;
        
        UpdateRotation();
        Destroy(gameObject, 3f); // Знищення через 3 секунди, якщо нікуди не влучили
    }

    void UpdateRotation()
    {
        moveDir = (targetPosition - transform.position).normalized;
        if (moveDir != Vector3.zero)
        {
            float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
        }
    }

    void Update()
    {
        if (!isInitialized || hasHit) return;

        float distanceThisFrame = speed * Time.deltaTime;

        if (homing)
        {
            // Якщо ціль жива, оновлюємо її позицію (слідкуємо за нею)
            if (targetTransform != null && targetTransform.gameObject.activeInHierarchy && !targetTransform.CompareTag("Untagged"))
            {
                targetPosition = targetTransform.position + Vector3.up * targetHeightOffset;
            }
            // Якщо ціль померла - targetPosition просто залишиться останньою записаною точкою (стріла полетить в труп)

            UpdateRotation();

            // Перевірка влучання по дистанції (ігноруємо Z для 2D)
            Vector2 currentPos2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 targetPos2D = new Vector2(targetPosition.x, targetPosition.y);

            if (Vector2.Distance(currentPos2D, targetPos2D) <= distanceThisFrame)
            {
                // Якщо долетіли і ціль ще жива - б'ємо
                if (targetTransform != null && !targetTransform.CompareTag("Untagged"))
                {
                    HitTarget(targetTransform.gameObject);
                }
                else
                {
                    // Ціль вже мертва, просто знищуємо стрілу (ніби встрягла в землю)
                    Destroy(gameObject);
                }
                return;
            }
        }

        // Захист Raycast: щоб швидка стріла не пролетіла крізь тонкого ворога за 1 кадр
        RaycastHit2D hit = Physics2D.Raycast(transform.position, moveDir, distanceThisFrame);
        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                if (!hit.collider.CompareTag("Untagged"))
                {
                    HitTarget(hit.collider.gameObject);
                }
                return;
            }
            else if (hit.collider.CompareTag("Ground") || hit.collider.CompareTag("Boundary"))
            {
                Destroy(gameObject);
                return;
            }
        }

        // Фізично рухаємо стрілу
        transform.position += moveDir * distanceThisFrame;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        
        if (other.CompareTag("Projectile") || other.CompareTag("Player") || other.CompareTag("PlayerUnit")) return;
        
        if (other.CompareTag("Boundary") || other.CompareTag("Ground"))
        {
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Enemy"))
        {
            if (other.CompareTag("Untagged")) return;
            
            // Якщо це Guard, перевіряємо, чи він не мертвий (enabled == false)
            if (other.TryGetComponent<Guard>(out Guard g) && !g.enabled) return;

            HitTarget(other.gameObject);
        }
    }

    void HitTarget(GameObject specificHit)
    {
        if (hasHit) return;
        hasHit = true; 

        // Відправляємо урон будь-якому об'єкту, у якого є метод TakeDamage
        if (specificHit != null)
        {
            specificHit.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
        
        Destroy(gameObject);
    }
}