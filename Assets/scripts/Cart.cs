using UnityEngine;
using System.Collections;

public class Cart : MonoBehaviour
{
    [Header("Характеристики")]
    public float speed = 2.5f;
    public float stopOffsetFromWall = -2.5f; 

    [Header("Базові ресурси (Рандом)")]
    public int minWood = 15;
    public int maxWood = 30;
    public int minStone = 5;
    public int maxStone = 15;

    private int woodReward;
    private int stoneReward;
    
    private bool isUnloading = false;
    private bool isLeaving = false;
    private float targetX;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        CalculateRewards();

        if (GameManager.Instance != null && GameManager.Instance.castle != null)
        {
            targetX = GameManager.Instance.castle.transform.position.x + stopOffsetFromWall;
        }
        else
        {
            targetX = transform.position.x + 5f; 
        }
    }

    void CalculateRewards()
    {
        woodReward = Random.Range(minWood, maxWood + 1);
        stoneReward = Random.Range(minStone, maxStone + 1);

        if (GameManager.Instance != null)
        {
            int wave = GameManager.Instance.currentWave;
            woodReward += wave;
            stoneReward += Mathf.RoundToInt(wave * 0.5f);

            float bonusMultiplier = 1f + (GameManager.Instance.metaEfficientCarts * 0.10f);
            woodReward = Mathf.RoundToInt(woodReward * bonusMultiplier);
            stoneReward = Mathf.RoundToInt(stoneReward * bonusMultiplier);

            // === НОВЕ: Сюжетний віз на 1-й хвилі ===
            // Гарантуємо 100 дерева для казарми!
            if (wave <= 1 && GameManager.Instance.barracksLevel == 0)
            {
                woodReward = 100;
                stoneReward = 0; // Камінь поки не потрібен
            }
        }
    }

    void Update()
    {
        if (isUnloading) return;

        if (isLeaving)
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime);
            return;
        }

        if (transform.position.x < targetX)
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime);
        }
        else
        {
            StartCoroutine(UnloadRoutine());
        }
    }

    IEnumerator UnloadRoutine()
    {
        isUnloading = true;
        if (animator != null) animator.SetTrigger("Unload");

        yield return new WaitForSeconds(1.5f); 

        DeliverResources();

        isUnloading = false;
        isLeaving = true;
        transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        Destroy(gameObject, 5f);
    }

    void DeliverResources()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddResource(ResourceType.Wood, woodReward);
            GameManager.Instance.AddResource(ResourceType.Stone, stoneReward);

            // === НОВЕ: Докидаємо золото, якщо гравцю не вистачає на казарму (200G) ===
            if (GameManager.Instance.currentWave <= 1 && GameManager.Instance.barracksLevel == 0)
            {
                if (GameManager.Instance.gold < 200)
                {
                    int goldNeeded = 200 - GameManager.Instance.gold;
                    GameManager.Instance.AddResource(ResourceType.Gold, goldNeeded);
                    
                    // Показуємо поп-ап золота
                    Vector3 goldPos = transform.position + new Vector3(-0.5f, 2.5f, 0f);
                    GameManager.Instance.ShowResourcePopup(ResourceType.Gold, goldNeeded, goldPos);
                }
            }

            Vector3 popupPos = transform.position + Vector3.up * 1.5f;
            if (woodReward > 0) GameManager.Instance.ShowResourcePopup(ResourceType.Wood, woodReward, popupPos);
            
            Vector3 popupPos2 = transform.position + new Vector3(0.5f, 2f, 0f);
            if (stoneReward > 0) GameManager.Instance.ShowResourcePopup(ResourceType.Stone, stoneReward, popupPos2);
        }

        if (SoundManager.Instance != null && SoundManager.Instance.coinPickup != null) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.coinPickup);
    }
}