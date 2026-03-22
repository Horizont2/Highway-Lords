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
    public float defeatAutoCloseDelay = 3.0f; 

    private CanvasGroup canvasGroup;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        canvasGroup = GetComponent<CanvasGroup>();

        if (victoryPanel) victoryPanel.SetActive(false);
        if (defeatPanel) defeatPanel.SetActive(false);
    }

    public void ShowResult(bool isVictory, int gold = 0, int wood = 0, int stone = 0)
    {
        gameObject.SetActive(true); 
        
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0; 
        transform.localScale = Vector3.one * 0.8f; 

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
            // === ФІКС: Красиве нарахування доходу (Gold/Hour) ===
            // Передаємо префікс "+" та суфікс " Gold/Hour" із золотим кольором
            StartCoroutine(CountResourceCoroutine(gold, goldText, "+", " <color=#FFD700>Gold/Hour</color>"));
            
            // Для дерева і каменю можна залишити просто "+цифра"
            StartCoroutine(CountResourceCoroutine(wood, woodText, "+", ""));
            StartCoroutine(CountResourceCoroutine(stone, stoneText, "+", ""));
        }
        else
        {
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
            time += Time.unscaledDeltaTime; // Використовуємо реальний час, незалежно від паузи
            float t = time / panelFadeInDuration;
            
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            
            float bounceT = 1f - Mathf.Pow(1f - t, 3f); 
            transform.localScale = Vector3.Lerp(startScale, targetScale, bounceT);
            
            yield return null;
        }

        transform.localScale = targetScale;
        canvasGroup.alpha = 1f;
    }

    IEnumerator AutoCloseDefeatCoroutine()
    {
        yield return new WaitForSecondsRealtime(defeatAutoCloseDelay);

        float time = 0;
        while (time < panelFadeInDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / panelFadeInDuration;
            
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    // === ФІКС: Додано підтримку тексту до і після цифри ===
    IEnumerator CountResourceCoroutine(int targetValue, TMP_Text textElement, string prefix = "", string suffix = "")
    {
        if (textElement == null || targetValue <= 0) yield break;

        float time = 0;
        int currentValue = 0;
        
        // Опціонально: Можеш додати звук старту нарахування монет
        if (SoundManager.Instance != null && suffix.Contains("Gold")) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.coinPickup);

        while (time < resourceCountingDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / resourceCountingDuration;

            // Ease-out ефект: спочатку швидко, під кінець повільно
            float t_slow = 1f - Mathf.Pow(1f - t, 2.5f); 

            currentValue = Mathf.RoundToInt(Mathf.Lerp(0, targetValue, t_slow));
            textElement.text = $"{prefix}{currentValue}{suffix}";

            yield return null;
        }

        textElement.text = $"{prefix}{targetValue}{suffix}";
        
        // Опціонально: Звук завершення нарахування
        if (SoundManager.Instance != null && suffix.Contains("Gold")) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.coinPickup, 1.5f);
    }

    public void TestVictory() { ShowResult(true, 250, 50, 20); }
    public void TestDefeat() { ShowResult(false); }
}