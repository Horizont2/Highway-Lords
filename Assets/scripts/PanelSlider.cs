using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PanelSlider : MonoBehaviour
{
    [Header("UI Елементи")]
    public RectTransform panelRect;       
    public Button toggleButton;           
    public Transform arrowIcon;           

    [Header("Налаштування")]
    public float slideDuration = 0.3f;    
    [Tooltip("На скільки пікселів панель від'їде вправо (наприклад 160-180)")]
    public float slideOffset = 180f;       

    private Vector2 openPos;
    private Vector2 closedPos;
    private bool isOpen = true; 
    private Coroutine slideCoroutine;
    private CanvasGroup canvasGroup;

    void Start()
    {
        if (panelRect == null) panelRect = GetComponent<RectTransform>();

        canvasGroup = panelRect.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = panelRect.gameObject.AddComponent<CanvasGroup>();

        // ЗАПАМ'ЯТОВУЄМО ПОЗИЦІЮ. В редакторі панель має стояти відкрита!
        openPos = panelRect.anchoredPosition;
        // Закрита позиція - це відкрита + зміщення вправо
        closedPos = new Vector2(openPos.x + slideOffset, openPos.y);

        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(TogglePanel);
        }

        // На старті гри вона відкрита і не прозора
        isOpen = true;
        panelRect.anchoredPosition = openPos;
        canvasGroup.alpha = 1f;
    }

    public void TogglePanel()
    {
        SetPanelState(!isOpen);
    }

    // Цей метод буде викликати GameManager
    public void SetPanelState(bool open)
    {
        if (isOpen == open) return; // Вже в цьому стані
        
        isOpen = open;
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        
        Vector2 targetPos = isOpen ? openPos : closedPos;
        float targetRotation = isOpen ? 180f : 0f; 

        slideCoroutine = StartCoroutine(SlideRoutine(targetPos, targetRotation));
    }

    IEnumerator SlideRoutine(Vector2 targetPos, float targetRotation)
    {
        float time = 0f;
        Vector2 startPos = panelRect.anchoredPosition;
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = isOpen ? 1f : 0.4f; // 0.4f - напівпрозора коли схована
        
        Quaternion startRot = arrowIcon != null ? arrowIcon.localRotation : Quaternion.identity;
        Quaternion targetQuat = Quaternion.Euler(0, 0, targetRotation);

        if (SoundManager.Instance != null && SoundManager.Instance.clickSound != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);

        while (time < slideDuration)
        {
            time += Time.unscaledDeltaTime; 
            float t = time / slideDuration;
            float smoothStep = t * (2f - t); 

            panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothStep);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothStep);
            
            if (arrowIcon != null)
                arrowIcon.localRotation = Quaternion.Lerp(startRot, targetQuat, smoothStep);

            yield return null;
        }

        panelRect.anchoredPosition = targetPos;
        canvasGroup.alpha = targetAlpha;
        if (arrowIcon != null) arrowIcon.localRotation = targetQuat;
    }
}