using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Налаштування")]
    public float speed = 15f;
    public int damage = 20;
    public float rotationOffset = -90f; // Коригування кута спрайта

    private Transform targetTransform; // Для самонаведення
    private Vector3 targetPosition;    // Для прямого пострілу
    private bool homing = false;       // Режим самонаведення?
    private bool isInitialized = false;

    // === ВАРІАНТ 1: Самонаведення (Для Вежі) ===
    public void Initialize(Transform _target, int _damage)
    {
        targetTransform = _target;
        damage = _damage;
        homing = true;
        isInitialized = true;
    }

    // === ВАРІАНТ 2: Прямий постріл (Для Лучників) ===
    // Цей метод потрібен, щоб Archer.cs міг використовувати цей скрипт
    public void Initialize(Vector3 _targetPos, int _damage)
    {
        targetPosition = _targetPos;
        damage = _damage;
        homing = false;
        isInitialized = true;
        
        // Одразу повертаємо в бік цілі
        Vector3 dir = (targetPosition - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
        
        // Видаляємо через 3 секунди, якщо нікуди не влучив
        Destroy(gameObject, 3f);
    }

    void Update()
    {
        if (!isInitialized) return;

        Vector3 moveDir;

        if (homing)
        {
            // === Логіка самонаведення ===
            if (targetTransform != null)
            {
                targetPosition = targetTransform.position;
            }
            // Якщо ціль зникла, летимо в останню відому точку
            
            moveDir = (targetPosition - transform.position).normalized;
            
            // Поворот
            float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);

            if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
            {
                HitTarget();
                return;
            }
        }
        else
        {
            // === Логіка прямого польоту ===
            // Летимо просто прямо туди, куди дивимось (з урахуванням офсету)
            // Оскільки ми повернули об'єкт при старті, можна використовувати transform.right або up
            // Але простіше перерахувати вектор, якщо він не змінюється
            // Тут ми просто рухаємось до точки, яку запам'ятали
             moveDir = (targetPosition - transform.position).normalized;
        }

        transform.Translate(moveDir * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ігноруємо стріли та свої юніти (якщо це стріла гравця)
        if (other.CompareTag("Projectile") || other.CompareTag("Player") || other.CompareTag("PlayerUnit")) return;
        
        // Знищення об границі екрану (Boundary)
        if (other.CompareTag("Boundary"))
        {
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Enemy"))
        {
            // Перевіряємо, чи це не труп
            if (other.GetComponent<Guard>() && other.GetComponent<Guard>().enabled == false) return;

            HitTarget(other.gameObject);
        }
        
        // Влучання в землю
        if (other.CompareTag("Ground")) Destroy(gameObject);
    }

    void HitTarget(GameObject specificHit = null)
    {
        GameObject hitObj = specificHit;
        
        // Якщо самонаведення і ми просто долетіли до цілі
        if (hitObj == null && homing && targetTransform != null) 
        {
            hitObj = targetTransform.gameObject;
        }

        if (hitObj != null)
        {
            if (hitObj.TryGetComponent<Guard>(out Guard g)) g.TakeDamage(damage);
            else if (hitObj.TryGetComponent<Cart>(out Cart c)) c.TakeDamage(damage);
            else if (hitObj.TryGetComponent<EnemyArcher>(out EnemyArcher ea)) ea.TakeDamage(damage);
            else if (hitObj.TryGetComponent<EnemySpearman>(out EnemySpearman es)) es.TakeDamage(damage);
            else if (hitObj.TryGetComponent<EnemyHorse>(out EnemyHorse eh)) eh.TakeDamage(damage);
            else if (hitObj.TryGetComponent<Boss>(out Boss b)) b.TakeDamage(damage);
        }
        
        Destroy(gameObject);
    }
}