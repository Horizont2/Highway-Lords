using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class AnimatedBattleResult : MonoBehaviour
{
    public static AnimatedBattleResult Instance { get; private set; }

    [Header("Main Panels")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;

    [Header("Victory Text Elements")]
    public TMP_Text goldText;
    public TMP_Text woodText;
    public TMP_Text stoneText;

    [Header("Settings")]
    public float panelFadeInDuration = 0.5f; 
    public float resourceCountingDuration = 2.0f; 
    public float defeatAutoCloseDelay = 3.0f; // Скільки секунд висить панель поразки

    private CanvasGroup canvasGroup;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        canvasGroup = GetComponent<CanvasGroup>();

        gameObject.SetActive(false); 
        if (victoryPanel) victoryPanel.SetActive(false);
        if (defeatPanel) defeatPanel.SetActive(false);
    }

    public void ShowResult(bool isVictory, int gold = 0, int wood = 0, int stone = 0)
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 0; 
        transform.localScale = Vector3.one * 0.8f; 

        // Зупиняємо всі попередні анімації, якщо вони ще йшли
        StopAllCoroutines(); 

        if (isVictory)
        {
            if (defeatPanel) defeatPanel.SetActive(false);
            if (victoryPanel) victoryPanel.SetActive(true);
            
            if (goldText) goldText.text = "0";
            if (woodText) woodText.text = "0";
            if (stoneText) stoneText.text = "0";
        }
        else
        {
            if (victoryPanel) victoryPanel.SetActive(false);
            if (defeatPanel) defeatPanel.SetActive(true);
        }

        StartCoroutine(AnimatePanelIn());

        if (isVictory)
        {
            // Нараховуємо ресурси для перемоги
            StartCoroutine(CountResourceCoroutine(gold, goldText));
            StartCoroutine(CountResourceCoroutine(wood, woodText));
            StartCoroutine(CountResourceCoroutine(stone, stoneText));
        }
        else
        {
            // Якщо це поразка - запускаємо таймер автозакриття
            StartCoroutine(AutoCloseDefeatCoroutine());
        }
    }

    IEnumerator AnimatePanelIn()
    {
        float time = 0;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = Vector3.one;

        while (time < panelFadeInDuration)
        {
            time += Time.deltaTime;
            float t = time / panelFadeInDuration;
            
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            
            float bounceT = 1f - Mathf.Pow(1f - t, 3f); 
            transform.localScale = Vector3.Lerp(startScale, targetScale, bounceT);
            
            yield return null;
        }

        transform.localScale = targetScale;
        canvasGroup.alpha = 1f;
    }

    // --- НОВА КОРУТИНА ДЛЯ АВТОМАТИЧНОГО ЗАКРИТТЯ ---
    IEnumerator AutoCloseDefeatCoroutine()
    {
        // 1. Чекаємо заданий час (наприклад, 3 секунди)
        yield return new WaitForSeconds(defeatAutoCloseDelay);

        // 2. Плавно розчиняємо панель (Fade Out)
        float time = 0;
        while (time < panelFadeInDuration)
        {
            time += Time.deltaTime;
            float t = time / panelFadeInDuration;
            
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        // 3. Вимикаємо об'єкт повністю
        gameObject.SetActive(false);
    }

    IEnumerator CountResourceCoroutine(int targetValue, TMP_Text textElement)
    {
        if (textElement == null || targetValue <= 0) yield break;

        float time = 0;
        int currentValue = 0;

        while (time < resourceCountingDuration)
        {
            time += Time.deltaTime;
            float t = time / resourceCountingDuration;

            float t_slow = 1f - Mathf.Pow(1f - t, 2.5f); 

            currentValue = Mathf.RoundToInt(Mathf.Lerp(0, targetValue, t_slow));
            textElement.text = currentValue.ToString();

            yield return null;
        }

        textElement.text = targetValue.ToString();
    }
    // --- ТИМЧАСОВІ МЕТОДИ ДЛЯ ТЕСТОВИХ КНОПОК ---
    public void TestVictory()
    {
        // Викликаємо нашу головну функцію і передаємо тестові ресурси
        ShowResult(true, 1000, 500, 250); 
    }

    public void TestDefeat()
    {
        // Викликаємо поразку (ресурси по нулях)
        ShowResult(false); 
    }
}