using UnityEngine;

public class Guard : MonoBehaviour
{
    [Header("Характеристики")]
    public float speed = 1.5f;
    public float attackRange = 0.8f;
    public int damage = 15;
    public int health = 60;
    public int goldReward = 15;

    private Animator animator;
    private Transform target; 
    private float nextAttackTime = 0f;
    private float attackCooldown = 1.5f;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (GameManager.Instance != null)
        {
            health += (GameManager.Instance.currentWave - 1) * 10;
        }
    }

    void Update()
    {
        FindTarget();

        if (target != null)
        {
            // ВАЖЛИВО: Завжди повертаємося обличчям до цілі, 
            // незалежно від того, йдемо ми чи стоїмо.
            FaceTarget(target.position);

            float distance = Vector2.Distance(transform.position, target.position);

            if (distance <= attackRange)
            {
                // Стоїмо і атакуємо
                StopMoving();
                if (Time.time >= nextAttackTime)
                {
                    Attack();
                    nextAttackTime = Time.time + attackCooldown;
                }
            }
            else
            {
                // Йдемо до цілі
                MoveTowards(target.position);
            }
        }
        else
        {
            // Якщо немає цілей - йдемо вліво (до замку гравця)
            Vector3 destination = transform.position + Vector3.left;
            FaceTarget(destination); // Повертаємося вліво
            MoveTowards(destination);
        }
    }

    // --- НОВИЙ МЕТОД ДЛЯ ПОВОРОТУ ---
    void FaceTarget(Vector3 targetPos)
    {
        // Оскільки твій спрайт по дефолту дивиться ВПРАВО:
        
        if (targetPos.x > transform.position.x)
        {
            // Ціль справа -> Дивимося вправо (Scale X = 1)
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (targetPos.x < transform.position.x)
        {
            // Ціль зліва -> Дивимося вліво (інвертуємо Scale X = -1)
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    void MoveTowards(Vector3 destination)
    {
        if (animator) animator.SetBool("IsRunning", true);
        // Тільки рухаємося. Поворот тепер обробляється у FaceTarget()
        transform.position = Vector2.MoveTowards(transform.position, destination, speed * Time.deltaTime);
    }

    void StopMoving()
    {
        if (animator) animator.SetBool("IsRunning", false);
    }

    void FindTarget()
    {
        float minDistance = Mathf.Infinity;
        Transform closestTarget = null;

        // 1. Шукаємо ЛИЦАРІВ
        Knight[] knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        foreach (Knight k in knights)
        {
            float dist = Vector2.Distance(transform.position, k.transform.position);
            if (dist < minDistance) { minDistance = dist; closestTarget = k.transform; }
        }

        // 2. Шукаємо ЛУЧНИКІВ
        Archer[] archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        foreach (Archer a in archers)
        {
            float dist = Vector2.Distance(transform.position, a.transform.position);
            if (dist < minDistance) { minDistance = dist; closestTarget = a.transform; }
        }

        // 3. Якщо героїв немає - йдемо ламати Замок
        if (closestTarget == null && GameManager.Instance != null && GameManager.Instance.castle != null)
        {
            closestTarget = GameManager.Instance.castle.transform;
        }
        target = closestTarget;
    }

    void Attack()
    {
        if (animator) animator.SetTrigger("Attack");

        if (target != null)
        {
            Knight knight = target.GetComponent<Knight>();
            Archer archer = target.GetComponent<Archer>();
            Castle castle = target.GetComponent<Castle>();

            if (knight != null) knight.TakeDamage(damage);
            else if (archer != null) archer.TakeDamage(damage);
            else if (castle != null) castle.TakeDamage(damage);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        if (GameManager.Instance != null)
            GameManager.Instance.ShowDamage(damageAmount, transform.position);

        if (health <= 0) Die();
    }

    void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddResource(ResourceType.Gold, goldReward);
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, goldReward, transform.position);
        }
        Destroy(gameObject);
    }
}