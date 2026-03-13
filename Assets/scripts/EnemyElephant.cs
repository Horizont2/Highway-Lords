using UnityEngine;
using UnityEngine.UI;

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
    public GameObject healthBarCanvas;      
    public Image healthBarFill;             
    public Transform segmentsContainer;     
    public GameObject segmentDividerPrefab; 
    public float healthPerSegment = 500f;   

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
        // ВАЖЛИВО: Дозволяє слону програвати анімацію під час паузи (timeScale = 0)
        if (anim != null) anim.updateMode = AnimatorUpdateMode.UnscaledTime;

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

        GenerateSegments();
        UpdateHealthBar();

        // Запускаємо катсцену
        if (Camera.main != null)
        {
            CameraController cam = Camera.main.GetComponent<CameraController>();
            if (cam != null) cam.PlayBossCutscene(this.transform);
        }
    }

    void Update()
    {
        if (isDead || targetCastle == null) return;

        float distanceToCastle = Vector3.Distance(transform.position, targetCastle.position);

        if (distanceToCastle > attackRange)
        {
            // МАГІЯ: Якщо йде катсцена, слон рухається використовуючи UnscaledTime!
            bool isCinematic = (GameManager.Instance != null && GameManager.Instance.isCinematicActive);
            float dt = isCinematic ? Time.unscaledDeltaTime : Time.deltaTime;
            
            transform.position = Vector3.MoveTowards(transform.position, targetCastle.position, moveSpeed * dt);
            anim.SetBool("isWalking", true);
        }
        else
        {
            anim.SetBool("isWalking", false);
            
            // Забороняємо атакувати під час катсцени
            if (GameManager.Instance != null && GameManager.Instance.isCinematicActive) return;

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
        if (healthBarFill != null) healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
    }

    void GenerateSegments()
    {
        if (segmentsContainer == null || segmentDividerPrefab == null) return;

        foreach (Transform child in segmentsContainer) Destroy(child.gameObject);

        int segmentCount = Mathf.CeilToInt(maxHealth / healthPerSegment);
        if (segmentCount <= 1) return;

        for (int i = 1; i < segmentCount; i++)
        {
            GameObject divider = Instantiate(segmentDividerPrefab, segmentsContainer);
            RectTransform rt = divider.GetComponent<RectTransform>();
            float percent = (float)i / segmentCount;
            rt.anchorMin = new Vector2(percent, 0);
            rt.anchorMax = new Vector2(percent, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 0); 
        }
    }

    void Die()
    {
        isDead = true;
        anim.SetTrigger("Die");
        GetComponent<Collider2D>().enabled = false;
        this.enabled = false; 

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