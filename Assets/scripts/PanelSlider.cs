using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PanelSlider : MonoBehaviour
{
    [Header("UI Елементи")]
    [Tooltip("Сама панель, яка буде їздити")]
    public RectTransform panelRect;       
    [Tooltip("Кнопка-хвостик")]
    public Button toggleButton;           
    [Tooltip("Об'єкт зі стрілочкою (щоб її крутити)")]
    public Transform arrowIcon;           

    [Header("Налаштування")]
    public float slideDuration = 0.3f;    // Час анімації в секундах
    public float offScreenX = 400f;       // Наскільки далеко панель ховається (Ширина панелі)
    public float onScreenX = 0f;          // Позиція, коли панель відкрита

    private bool isOpen = false;
    private Coroutine slideCoroutine;

    void Start()
    {
        // 1. При старті гри автоматично ховаємо панель
        if (panelRect != null)
        {
            Vector2 pos = panelRect.anchoredPosition;
            pos.x = offScreenX;
            panelRect.anchoredPosition = pos;
        }

        // 2. Прив'язуємо кнопку
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePanel);
        }
    }

    public void TogglePanel()
    {
        // Якщо панель вже їде, зупиняємо поточну анімацію
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        
        isOpen = !isOpen;
        
        // Визначаємо цільову позицію та кут повороту стрілки (0 = вліво, 180 = вправо)
        float targetX = isOpen ? onScreenX : offScreenX;
        float targetRotation = isOpen ? 180f : 0f; 

        slideCoroutine = StartCoroutine(SlideRoutine(targetX, targetRotation));
    }

    IEnumerator SlideRoutine(float targetX, float targetRotation)
    {
        float time = 0f;
        Vector2 startPos = panelRect.anchoredPosition;
        Vector2 targetPos = new Vector2(targetX, startPos.y);
        
        Quaternion startRot = arrowIcon != null ? arrowIcon.localRotation : Quaternion.identity;
        Quaternion targetQuat = Quaternion.Euler(0, 0, targetRotation);

        // Звук виїзду (якщо є)
        if (SoundManager.Instance != null && SoundManager.Instance.clickSound != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);

        while (time < slideDuration)
        {
            time += Time.deltaTime;
            float t = time / slideDuration;
            
            // Математична функція (Ease Out) для красивого плавного гальмування в кінці
            float smoothStep = t * (2f - t); 

            panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothStep);
            
            if (arrowIcon != null)
            {
                arrowIcon.localRotation = Quaternion.Lerp(startRot, targetQuat, smoothStep);
            }

            yield return null;
        }

        // Гарантуємо точну фінальну позицію
        panelRect.anchoredPosition = targetPos;
        if (arrowIcon != null) arrowIcon.localRotation = targetQuat;
    }
}