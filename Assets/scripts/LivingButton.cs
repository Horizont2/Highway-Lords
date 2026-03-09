using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LivingButton : MonoBehaviour
{
    [Header("Налаштування пульсації (Дихання)")]
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.05f;

    [Header("Налаштування Бліку (Shine)")]
    public RectTransform shineTransform; // Дочірній об'єкт бліку
    public float shineInterval = 3f;     // Раз на скільки секунд блищить
    public float shineSpeed = 1.5f;      // Швидкість руху смужки

    private Vector3 originalScale;
    private float shineTimer;
    private float shineWidth;

    void Start()
    {
        originalScale = transform.localScale;
        shineTimer = shineInterval;

        if (shineTransform != null)
        {
            // Запам'ятовуємо ширину, щоб знати, куди летіти
            shineWidth = GetComponent<RectTransform>().rect.width * 2f;
            ResetShinePosition();
        }
    }

    void Update()
    {
        // 1. Ефект дихання (Пульсація)
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = originalScale * pulse;

        // 2. Логіка бліку
        if (shineTransform != null)
        {
            shineTimer -= Time.deltaTime;
            if (shineTimer <= 0)
            {
                StartCoroutine(PlayShineEffect());
                shineTimer = shineInterval;
            }
        }
    }

    IEnumerator PlayShineEffect()
    {
        float elapsed = 0f;
        float duration = 1f / shineSpeed;
        
        Vector2 startPos = new Vector2(-shineWidth, 0);
        Vector2 endPos = new Vector2(shineWidth, 0);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Рухаємо блік зліва направо
            shineTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        ResetShinePosition();
    }

    void ResetShinePosition()
    {
        if (shineTransform != null)
            shineTransform.anchoredPosition = new Vector2(-shineWidth, 0);
    }
}