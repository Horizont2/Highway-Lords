using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 20; // Це значення перезапишеться вежею
    public float rotationOffset = -90f; // Поворот картинки стріли

    private Transform target;

    public void Seek(Transform _target)
    {
        target = _target;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 direction = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        // Поворот до цілі
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);

        if (direction.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        transform.Translate(direction.normalized * distanceThisFrame, Space.World);
    }

    void HitTarget()
    {
        // 1. Спробуємо знайти Віз
        Cart cart = target.GetComponent<Cart>();
        if (cart != null)
        {
            cart.TakeDamage(damage);
        }
        else
        {
            // 2. Якщо це не віз, спробуємо знайти Охоронця
            Guard guard = target.GetComponent<Guard>();
            if (guard != null)
            {
                guard.TakeDamage(damage);
            }
        }
        
        Destroy(gameObject); // Знищуємо стрілу
    }
}