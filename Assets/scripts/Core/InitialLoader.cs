using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class InitialLoader : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    
    [Tooltip("Скільки секунд екран буде висіти повністю чорним")]
    public float waitTime = 1.0f; 
    
    [Tooltip("Скільки секунд він буде плавно розчинятися")]
    public float fadeDuration = 0.5f;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        // Робимо екран абсолютно непрозорим і таким, що блокує кліки мишки
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    void Start()
    {
        // Запускаємо процес зникнення
        StartCoroutine(FadeOutRoutine());
    }

    IEnumerator FadeOutRoutine()
    {
        // 1. Чекаємо, поки всі інші скрипти в грі виконають свої Awake() та Start()
        yield return new WaitForSeconds(waitTime);

        // 2. Плавно робимо екран прозорим
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        // 3. Вимикаємо екран, щоб він не заважав грати
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }
}