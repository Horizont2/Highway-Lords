using UnityEngine;

public class WallArcher : MonoBehaviour
{
    [Header("Налаштування")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    
    [Header("Базові Стати")]
    public float attackRange = 15f; 
    public float timeBetweenShots = 1.5f;

    [Header("Еволюція (Аніматори)")]
    [Tooltip("0 - базовий (1-5 лвл), 1 - (6-10 лвл), 2 - (11-15 лвл) і т.д.")]
    public RuntimeAnimatorController[] levelAnimators;

    private int currentDamage;
    private float nextShotTime;
    private Animator animator;
    private Transform target;

    // Ціль перебиралася щокадру. Коли два вороги майже рівновіддалені,
    // найближчий міняється по колу, і спрайт дзеркалився туди-сюди кожен
    // кадр — саме це читалося як блимання лучника на стіні.
    private float retargetTimer;
    private const float RetargetInterval = 0.2f;

    // Мертва зона розвороту: поки ворог майже строго під лучником,
    // напрямок не міняємо.
    private const float FlipDeadZone = 0.35f;
    private float facing = 1f;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        UpdateStats();
    }

    // Метод викликається на старті і КОЖНОГО разу, коли гравець купує поліпшення
    public void UpdateStats()
    {
        if (GameManager.Instance != null)
        {
            // Беремо актуальний урон
            currentDamage = GameManager.Instance.GetWallArcherDamage();
            
            // Оновлюємо Аніматор, якщо є масив
            if (levelAnimators != null && levelAnimators.Length > 0 && animator != null)
            {
                int skinIndex = GameManager.Instance.GetWallArcherSkinIndex();
                
                // Захист: якщо рівень більший, ніж у нас є аніматорів, ставимо останній доступний
                if (skinIndex >= levelAnimators.Length) 
                {
                    skinIndex = levelAnimators.Length - 1;
                }

                // Змінюємо сам Аніматор (усі анімації зміняться автоматично!)
                if (animator.runtimeAnimatorController != levelAnimators[skinIndex])
                {
                    animator.runtimeAnimatorController = levelAnimators[skinIndex];
                }
            }
        }
        else
        {
            currentDamage = 10; // Якщо GameManager не знайдено (для тестів)
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isDefeated) return;

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f || target == null || target.CompareTag("Untagged"))
        {
            FindTarget();
            retargetTimer = RetargetInterval;
        }

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
            if (enemy.CompareTag("Untagged")) continue;

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
            
            float dx = target.position.x - transform.position.x;
            if (Mathf.Abs(dx) > FlipDeadZone) facing = dx > 0f ? 1f : -1f;

            Vector3 scale = transform.localScale;
            transform.localScale = new Vector3(facing * Mathf.Abs(scale.x), scale.y, scale.z);
        }
    }

    public void ShootArrow() 
    {
        if (target == null) return;

        if (arrowPrefab != null && firePoint != null)
        {
            GameObject arrowObj = PoolRegistry.Spawn(arrowPrefab, firePoint.position, firePoint.rotation);
            
            Arrow arrowScript = arrowObj.GetComponent<Arrow>();
            // Використовуємо наш Projectile (стрілу)
            if (arrowObj.TryGetComponent<Projectile>(out Projectile proj))
            {
                proj.Initialize(target.position, currentDamage);
            }
            else if (arrowScript != null) 
            {
                arrowScript.Initialize(target, currentDamage);
            }
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot);
        }
    }
}