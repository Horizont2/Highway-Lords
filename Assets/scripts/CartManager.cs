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
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        if (currentCooldown > 0)
        {
            callCartButton.interactable = false;
            if (cooldownOverlay != null) 
            {
                cooldownOverlay.fillAmount = 1f - (currentCooldown / cooldownTime);
            }
        }
        else
        {
            callCartButton.interactable = true;
            if (cooldownOverlay != null) 
            {
                cooldownOverlay.fillAmount = 1f;
            }
        }
    }

    public void CallCart()
    {
        if (currentCooldown > 0) return;

        if (cartPrefab != null && cartSpawnPoint != null)
        {
            Instantiate(cartPrefab, cartSpawnPoint.position, Quaternion.identity);
            
            // === НОВА ЛОГІКА ЗВУКУ З ГУЧНІСТЮ ===
            if (cartCallSound != null && SoundManager.Instance != null)
            {
                // Програємо звук із заданою гучністю
                SoundManager.Instance.PlaySFX(cartCallSound, cartCallVolume);
            }
            // =========================

            currentCooldown = cooldownTime;
            UpdateUI();
        }
        else
        {
            Debug.LogWarning("CartManager: Не призначено префаб воза або точку спавну!");
        }
    }
}