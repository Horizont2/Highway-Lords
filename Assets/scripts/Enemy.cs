using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Налаштування")]
    public int baseHealth = 30;
    public int goldReward = 15; 
    
    private int currentHealth;
    private bool isRunningAway = false;

    private int _baseHealth;
    private int _baseGoldReward;

    void Awake()
    {
        _baseHealth = baseHealth;
        _baseGoldReward = goldReward;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            int wave = GameManager.Instance.currentWave;
            currentHealth = EconomyConfig.GetEnemyHealth(_baseHealth, wave);
            goldReward = EconomyConfig.GetEnemyGoldDrop(_baseGoldReward, wave);
            GameManager.Instance.RegisterEnemy();
        }
        else
        {
            currentHealth = _baseHealth;
        }
    }

    void Update()
    {
        if (!isRunningAway && GameManager.Instance != null && GameManager.Instance.isDefeated)
            isRunningAway = true;

        if (isRunningAway)
        {
            transform.position += Vector3.left * 2.5f * Time.deltaTime;
            return;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (GameManager.Instance != null) GameManager.Instance.ShowDamage(damage, transform.position);
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit);
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy();
            GameManager.Instance.AddResource(ResourceType.Gold, goldReward);
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, goldReward, transform.position);
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);
        Destroy(gameObject); 
    }
}