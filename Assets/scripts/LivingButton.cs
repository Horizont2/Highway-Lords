using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LivingButton : MonoBehaviour
{
    [Header("Налаштування пульсації (Дихання)")]
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.05f;

    [Header("Налаштування Бліку (Shine)")]
    public RectTransform shineTransform;
    public float shineInterval = 3f;
    public float shineSpeed = 1.5f;

    private Vector3 originalScale;
    private float shineTimer;
    private float shineWidth;
    
    private Button myButton;
    private CanvasGroup myCanvasGroup;
    private Image[] childImages;

    void Awake()
    {
        // Записуємо базовий масштаб найпершим
        originalScale = transform.localScale;
        
        myButton = GetComponentInParent<Button>();
        myCanvasGroup = GetComponentInParent<CanvasGroup>();
        // Збираємо всі картинки, щоб потім знайти серед них кільце кулдауну
        childImages = GetComponentsInChildren<Image>(true);
    }

    void Start()
    {
        shineTimer = shineInterval;
        if (shineTransform != null)
        {
            shineWidth = GetComponent<RectTransform>().rect.width * 2f;
            ResetShinePosition();
        }
    }

    // Супер-перевірка: чи дійсно кнопка готова?
    bool CanPulse()
    {
        if (myButton != null && !myButton.interactable) return false;
        
        if (myCanvasGroup != null)
        {
            if (!myCanvasGroup.blocksRaycasts || myCanvasGroup.alpha < 0.1f) return false;
        }

        // ХАК ДЛЯ КУЛДАУНУ: Перевіряємо, чи є кільце перезарядки
        if (childImages != null)
        {
            foreach (var img in childImages)
            {
                // Якщо картинка "Filled" і вона не заповнена на 100% та не порожня на 0%
                if (img != null && img.type == Image.Type.Filled)
                {
                    if (img.fillAmount > 0.01f && img.fillAmount < 0.99f)
                        return false; // Кнопка ще на кулдауні!
                }
            }
        }
        
        return true;
    }

    void Update()
    {
        if (!CanPulse())
        {
            // Якщо кнопка "мертва" - жорстко скидаємо розмір і зупиняємось
            transform.localScale = originalScale;
            return; 
        }

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
        transform.localScale = originalScale * pulse;

        if (shineTransform != null)
        {
            shineTimer -= Time.unscaledDeltaTime;
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
            if (!CanPulse())
            {
                ResetShinePosition();
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            
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