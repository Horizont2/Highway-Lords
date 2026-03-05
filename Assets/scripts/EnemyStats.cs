using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Економіка")]
    public int baseGoldReward = 15; // Базова ціна
    
    [HideInInspector] public int currentGoldReward; 

    void Start()
    {
        // === НОВА ЕКОНОМІКА ===
        if (GameManager.Instance != null)
        {
            int wave = GameManager.Instance.currentWave;
            currentGoldReward = EconomyConfig.GetEnemyGoldDrop(baseGoldReward, wave);
        }
        else
        {
            currentGoldReward = baseGoldReward;
        }
    }

    public void GiveGold()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddResource(ResourceType.Gold, currentGoldReward);
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, currentGoldReward, transform.position);
            GameManager.Instance.UnregisterEnemy();
        }
    }
}