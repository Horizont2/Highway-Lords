using UnityEngine;
using UnityEngine.UI;

public class CartManager : MonoBehaviour
{
    [Header("Налаштування Спавну")]
    public GameObject cartPrefab;
    public Transform cartSpawnPoint;

    [Header("UI Кнопки")]
    public Button callCartButton;
    public Image cooldownOverlay;
    public GameObject cartShineEffect; // <--- НОВА ЗМІННА ДЛЯ БЛІКУ

    [Header("Баланс")]
    public float cooldownTime = 45f; 
    private float currentCooldown = 0f;

    [Header("Звуки")]
    public AudioClip cartCallSound; 
    
    [Range(0f, 1f)]
    public float cartCallVolume = 0.3f; // Гучність (0.6 = мінус 40% від максимуму)

    void Start()
    {
        if (callCartButton != null)
        {
            callCartButton.onClick.RemoveAllListeners();
            callCartButton.onClick.AddListener(CallCart);
        }

        currentCooldown = 0f;
        UpdateUI();
    }

    void Update()
    {
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;

            // Якщо час вийшов під час цього кадру
            if (currentCooldown <= 0)
            {
                currentCooldown = 0; // Фіксуємо рівно нуль
            }
            
            // Викликаємо оновлення UI (тепер воно точно спрацює при 0)
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        if (currentCooldown > 0)
        {
            callCartButton.interactable = false;
            
            // Вимикаємо ефект, поки йде перезарядка
            if (cartShineEffect != null) cartShineEffect.SetActive(false); 

            if (cooldownOverlay != null) 
            {
                cooldownOverlay.fillAmount = 1f - (currentCooldown / cooldownTime);
            }
        }
        else
        {
            callCartButton.interactable = true;
            
            // Вмикаємо ефект, коли можна клікати!
            if (cartShineEffect != null) cartShineEffect.SetActive(true);

            if (cooldownOverlay != null) 
            {
                cooldownOverlay.fillAmount = 1f;
            }
        }
    }

    public void CallCart()
    {
        Debug.Log("КНОПКУ ВОЗУ НАТИСНУТО!"); // <--- Додайте це

        if (currentCooldown > 0) 
        {
             Debug.Log("Але кулдаун ще не пройшов: " + currentCooldown);
             return;
        }

        if (cartPrefab != null && cartSpawnPoint != null)
        {
            Instantiate(cartPrefab, cartSpawnPoint.position, Quaternion.identity);
            
            if (cartCallSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(cartCallSound, cartCallVolume);
            }

            currentCooldown = cooldownTime;
            UpdateUI();
        }
        else
        {
            Debug.LogWarning("CartManager: Не призначено префаб воза або точку спавну!");
        }
    }
}