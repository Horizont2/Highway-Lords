using UnityEngine;
using UnityEngine.UI; // Обов'язково для роботи з UI

public class EnemyElephant : MonoBehaviour
{
    [Header("Характеристики")]
    public float maxHealth = 1500f;
    public float currentHealth;
    public float moveSpeed = 0.5f;
    public int damage = 40;
    
    [Header("Атака")]
    public float attackRange = 2.5f; 
    public float attackCooldown = 3.0f;
    private float lastAttackTime = 0f;

    [Header("UI Хелсбар (Сегментований)")]
    public GameObject healthBarCanvas;      // Загальний об'єкт Canvas хелсбару, щоб сховати при смерті
    public Image healthBarFill;             // Картинка самого заповнення (червона/зелена смужка)
    public Transform segmentsContainer;     // Порожній об'єкт поверх хелсбару для ліній
    public GameObject segmentDividerPrefab; // Префаб чорної вертикальної лінії
    public float healthPerSegment = 500f;   // Скільки ХП містить один сегмент (кубік)

    [Header("Посилання")]
    public int goldReward = 150;
    private Animator anim;
    private Transform targetCastle;
    private bool isDead = false;

    private float _baseMaxHealth;
    private int _baseDamage;
    private int _baseGoldReward;

    void Awake()
    {
        _baseMaxHealth = maxHealth;
        _baseDamage = damage;
        _baseGoldReward = goldReward;
    }

    void Start()
    {
        anim = GetComponent<Animator>();

        if (GameManager.Instance != null)
        {
            int wave = GameManager.Instance.currentWave;
            maxHealth = EconomyConfig.GetEnemyHealth(Mathf.RoundToInt(_baseMaxHealth), wave);
            damage = EconomyConfig.GetEnemyDamage(_baseDamage, wave);
            goldReward = EconomyConfig.GetEnemyGoldDrop(_baseGoldReward, wave);
        }

        currentHealth = maxHealth;

        if (GameManager.Instance != null && GameManager.Instance.castle != null)
            targetCastle = GameManager.Instance.castle.transform;

        // Генеруємо смужки та оновлюємо ХП при появі
        GenerateSegments();
        UpdateHealthBar();
    }

    void Update()
    {
        if (isDead || targetCastle == null) return;

        float distanceToCastle = Vector3.Distance(transform.position, targetCastle.position);

        if (distanceToCastle > attackRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetCastle.position, moveSpeed * Time.deltaTime);
            anim.SetBool("isWalking", true);
        }
        else
        {
            anim.SetBool("isWalking", false);
            if (Time.time - lastAttackTime >= attackCooldown) Attack();
        }
    }

    void Attack()
    {
        lastAttackTime = Time.time;
        anim.SetTrigger("Attack");

        if (GameManager.Instance != null && GameManager.Instance.castle != null)
        {
            GameManager.Instance.castle.TakeDamage(damage);
            GameManager.Instance.ShowDamage(damage, targetCastle.position);
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.heavyHitSound);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        GameManager.CreateDamagePopup(transform.position + Vector3.up, Mathf.RoundToInt(amount));

        UpdateHealthBar();

        if (currentHealth <= 0) Die();
    }

    void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            // Оновлюємо заповнення від 0 до 1
            healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        }
    }

    void GenerateSegments()
    {
        if (segmentsContainer == null || segmentDividerPrefab == null) return;

        // Спочатку очищаємо старі лінії (якщо вони були)
        foreach (Transform child in segmentsContainer)
        {
            Destroy(child.gameObject);
        }

        // Рахуємо, скільки всього шматочків має бути
        int segmentCount = Mathf.CeilToInt(maxHealth / healthPerSegment);

        // Якщо менше 2 сегментів (тобто <500 ХП), розділювачі не потрібні
        if (segmentCount <= 1) return;

        // Створюємо розділювачі (на 1 менше, ніж загальна кількість сегментів)
        for (int i = 1; i < segmentCount; i++)
        {
            GameObject divider = Instantiate(segmentDividerPrefab, segmentsContainer);
            RectTransform rt = divider.GetComponent<RectTransform>();
            
            // Вираховуємо відсоток позиції (наприклад, 33%, 66% тощо)
            float percent = (float)i / segmentCount;
            
            // Розставляємо якорі так, щоб лінія стояла рівно на своєму відсотку
            rt.anchorMin = new Vector2(percent, 0);
            rt.anchorMax = new Vector2(percent, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 0); // Висота підлаштовується під контейнер
        }
    }

    void Die()
    {
        isDead = true;
        anim.SetTrigger("Die");
        GetComponent<Collider2D>().enabled = false;
        this.enabled = false; // Вимикаємо скрипт

        // Ховаємо хелсбар при смерті
        if (healthBarCanvas != null) healthBarCanvas.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddResource(ResourceType.Gold, goldReward);
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, goldReward, transform.position);
            GameManager.Instance.UnregisterEnemy();
        }

        Destroy(gameObject, 3f); 
    }
}