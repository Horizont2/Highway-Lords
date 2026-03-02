using UnityEngine;
using System.Collections;

public class Cart : MonoBehaviour
{
    [Header("Характеристики")]
    public float speed = 2.5f;
    [Tooltip("Відстань від стіни зліва, де віз зупиниться (наприклад, -2.5)")]
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

        // Визначаємо точку зупинки (відносно стіни)
        if (GameManager.Instance != null && GameManager.Instance.castle != null)
        {
            // Беремо позицію X стіни і віднімаємо від неї наш відступ
            targetX = GameManager.Instance.castle.transform.position.x + stopOffsetFromWall;
        }
        else
        {
            targetX = transform.position.x + 5f; // Запасний варіант, якщо стіни немає
        }
    }

    void CalculateRewards()
    {
        woodReward = Random.Range(minWood, maxWood + 1);
        stoneReward = Random.Range(minStone, maxStone + 1);

        // Легке скалювання від хвилі
        if (GameManager.Instance != null)
        {
            int wave = GameManager.Instance.currentWave;
            woodReward += wave;
            stoneReward += Mathf.RoundToInt(wave * 0.5f);
        }
    }

    void Update()
    {
        // Якщо розвантажується - стоїмо на місці
        if (isUnloading) return;

        // Якщо розвантажився і їде назад
        if (isLeaving)
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime);
            return;
        }

        // Їдемо вправо до точки зупинки
        if (transform.position.x < targetX)
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime);
        }
        else
        {
            // Доїхали до стіни! Починаємо розвантаження
            StartCoroutine(UnloadRoutine());
        }
    }

    IEnumerator UnloadRoutine()
    {
        isUnloading = true;

        // Вмикаємо анімацію розвантаження (якщо вона є)
        if (animator != null)
        {
            animator.SetTrigger("Unload");
        }

        // Чекаємо 1.5 секунди (час на розвантаження)
        yield return new WaitForSeconds(1.5f); 

        DeliverResources();

        // Після розвантаження віз їде назад за екран
        isUnloading = false;
        isLeaving = true;
        
        // Розвертаємо спрайт воза, щоб він їхав задом/розвернувся
        transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

        // Видаляємо віз через 5 секунд, коли він точно виїде за межі екрана
        Destroy(gameObject, 5f);
    }

    void DeliverResources()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddResource(ResourceType.Wood, woodReward);
            GameManager.Instance.AddResource(ResourceType.Stone, stoneReward);

            // Показуємо попапи ресурсів над возом
            Vector3 popupPos = transform.position + Vector3.up * 1.5f;
            GameManager.Instance.ShowResourcePopup(ResourceType.Wood, woodReward, popupPos);
            
            Vector3 popupPos2 = transform.position + new Vector3(0.5f, 2f, 0f);
            GameManager.Instance.ShowResourcePopup(ResourceType.Stone, stoneReward, popupPos2);
        }

        if (SoundManager.Instance != null && SoundManager.Instance.coinPickup != null) 
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.coinPickup);
        }
    }
}