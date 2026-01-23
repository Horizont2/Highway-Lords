using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public float lifeTime = 3f;

    void Start()
    {
        GetComponent<Rigidbody2D>().linearVelocity = transform.right * speed;
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Цей рядок покаже в консолі, у що саме влучила стріла
        // Debug.Log($"Стріла влучила в: {hitInfo.name}");

        // 1. Спроба знайти Охоронця (шукаємо і на об'єкті, і на його батьках)
        Guard guard = hitInfo.GetComponentInParent<Guard>();
        if (guard != null)
        {
            guard.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // 2. Спроба знайти Віз
        Cart cart = hitInfo.GetComponentInParent<Cart>();
        if (cart != null)
        {
            cart.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }
        
        // 3. Знищення об землю (переконайся, що у землі шар "Ground")
        if (hitInfo.gameObject.layer == LayerMask.NameToLayer("Ground")) 
        {
            Destroy(gameObject);
        }
    }
}