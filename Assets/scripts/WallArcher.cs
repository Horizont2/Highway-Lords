using UnityEngine;

public class WallArcher : MonoBehaviour
{
    [Header("Налаштування")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    
    [Header("Базові Стати")]
    public float attackRange = 15f; // Трохи збільшив для надійності
    public float timeBetweenShots = 1.5f;
    public int baseDamage = 10;
    public int damagePerLevel = 4;

    private int currentDamage;
    private float nextShotTime;
    private Animator animator;
    private Transform target;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Initialize(int level)
    {
        currentDamage = baseDamage + ((level - 1) * damagePerLevel);
    }

    void Update()
    {
        // Спрощена перевірка: якщо GameManager каже, що ми програли — не стріляємо
        if (GameManager.Instance != null && GameManager.Instance.isDefeated) return;

        FindTarget();

        if (target != null)
        {
            AimAtTarget();
            if (Time.time >= nextShotTime)
            {
                if (animator) animator.SetTrigger("Attack");
                nextShotTime = Time.time + timeBetweenShots;
            }
        }
    }

    void FindTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float minDistance = attackRange;
        Transform closest = null;

        foreach (GameObject enemy in enemies)
        {
            // Перевірка, чи ворог живий (якщо у ворогів є скрипти з хелсбаром)
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = enemy.transform;
            }
        }
        target = closest;
    }

    void AimAtTarget()
    {
        if (firePoint != null && target != null)
        {
            Vector3 direction = target.position - firePoint.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            firePoint.rotation = Quaternion.Euler(0, 0, angle);
            
            float flip = target.position.x > transform.position.x ? 1f : -1f;
            transform.localScale = new Vector3(flip * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1f);
        }
    }

    public void ShootArrow() 
    {
        if (target == null) return;

        if (arrowPrefab != null && firePoint != null)
        {
            GameObject arrowObj = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);
            
            Arrow arrowScript = arrowObj.GetComponent<Arrow>();
            if (arrowScript != null) 
            {
                arrowScript.Initialize(target, currentDamage);
            }
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot);
        }
    }
}