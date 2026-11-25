using UnityEngine;
using UnityEngine.UI;

public class UIPulseEffect : MonoBehaviour
{
    [Header("Налаштування пульсації")]
    public float pulseSpeed = 4f; // Швидкість дихання
    public float maxScale = 1.1f; // Наскільки сильно збільшується (1.1 = на 10%)
    
    [Header("Опціонально: Зміна кольору")]
    public bool pulseColor = false;
    public Color highlightColor = new Color(1f, 0.9f, 0.5f); // Золотистий відтінок
    
    private Vector3 originalScale;
    private Color originalColor;
    private Image buttonImage;
    private bool isPulsing = false;

    void Awake()
    {
        originalScale = transform.localScale;
        buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            originalColor = buttonImage.color;
        }
    }

    void Update()
    {
        if (isPulsing)
        {
            // Плавне обчислення від 0 до 1 за допомогою синусоїди
            float lerp = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) / 2f;

            // Збільшуємо розмір
            float currentScale = Mathf.Lerp(1f, maxScale, lerp);
            transform.localScale = originalScale * currentScale;

            // Змінюємо колір (якщо увімкнено)
            if (pulseColor && buttonImage != null)
            {
                buttonImage.color = Color.Lerp(originalColor, highlightColor, lerp);
            }
        }
    }

    public void SetPulse(bool active)
    {
        if (isPulsing == active) return; // Нічого не робимо, якщо стан не змінився

        isPulsing = active;

        // Якщо вимикаємо пульсацію — повертаємо кнопку до нормального стану
        if (!isPulsing)
        {
            transform.localScale = originalScale;
            if (pulseColor && buttonImage != null)
            {
                buttonImage.color = originalColor;
            }
        }
    }
}