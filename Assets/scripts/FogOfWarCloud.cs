using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class FogOfWarCloud : MonoBehaviour
{
    [Header("Налаштування")]
    [Tooltip("Яке місто ховає ця хмара? (Наприклад, 2)")]
    public int coversCampId = 2; 
    public float animationDuration = 1.5f; // Скільки секунд розсіюється туман

    private CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        CheckFogStatus();
    }

    public void CheckFogStatus()
    {
        // Перевіряємо, чи пройдено ПОПЕРЕДНЄ місто (щоб відкрити це)
        bool isPreviousConquered = PlayerPrefs.GetInt("Camp_" + (coversCampId - 1) + "_Conquered", 0) == 1;
        
        // Перевіряємо, чи ми ВЖЕ програвали анімацію розсіювання для цієї хмари
        bool isFogDispelled = PlayerPrefs.GetInt("Camp_" + coversCampId + "_FogDispelled", 0) == 1;

        if (isPreviousConquered)
        {
            if (!isFogDispelled)
            {
                // МІСТО ЩОЙНО ВІДКРИЛОСЯ! Граємо красиву анімацію
                StartCoroutine(DispelAnimation());
            }
            else
            {
                // Місто відкрите давно, просто вимикаємо хмару
                gameObject.SetActive(false);
            }
        }
        else
        {
            // Місто ще заблоковане, хмара висить щільно
            canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
        }
    }

    IEnumerator DispelAnimation()
    {
        // Одразу записуємо, що туман розсіяно, щоб не грати це знову при наступному запуску
        PlayerPrefs.SetInt("Camp_" + coversCampId + "_FogDispelled", 1);
        PlayerPrefs.Save();

        float time = 0;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * 1.5f; // Хмара трохи розширюється при зникненні

        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;
            
            // Плавно зменшуємо прозорість від 1 до 0
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            
            // Плавно збільшуємо розмір
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            yield return null; // Чекаємо наступного кадру
        }

        gameObject.SetActive(false); // Вимикаємо повністю, коли анімація завершена
    }
}