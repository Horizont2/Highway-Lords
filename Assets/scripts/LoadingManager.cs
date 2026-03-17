using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    [Header("UI Елементи")]
    public CanvasGroup mainCanvasGroup;   // Для плавного згасання всього екрану
    public RectTransform backgroundRect;  // Твоя картинка з лицарем
    public Image progressBarFill;         // Смужка завантаження
    public TMP_Text progressText;         // Текст "100%"
    public TMP_Text tipText;              // Текст підказки

    [Header("Налаштування Анімації")]
    public float zoomSpeed = 0.02f;       // Швидкість наближення
    public float panSpeed = 15f;          // Швидкість руху камери вгору
    public float minLoadingTime = 2.5f;   // Мінімальний час екрану (щоб не блимав, якщо сцена важить мало)

    [Header("Підказки для гравців")]
    [TextArea(2, 3)]
    public string[] tips = new string[]
    {
        "TIP: Spearmen deal double damage to Cavalry units.",
        "TIP: Upgrade your Barracks to increase your maximum army limit.",
        "TIP: Retreating from a lost battle will save your surviving units for the next attack.",
        "TIP: Knights are heavily armored and perfect for breaking through enemy Archer lines.",
        "TIP: Use the Volley skill to wipe out large groups of enemies instantly."
    };

    private bool isLoading = false;

    void Awake()
    {
        // Робимо цей об'єкт безсмертним (він переходитиме з нами між усіма сценами)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ховаємо екран на старті
        mainCanvasGroup.alpha = 0f;
        mainCanvasGroup.blocksRaycasts = false;
        mainCanvasGroup.interactable = false;
    }

    // ГОЛОВНИЙ МЕТОД ДЛЯ ВИКЛИКУ
    public void LoadScene(string sceneName)
    {
        if (!isLoading) StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        // 1. Готуємо UI (Скидаємо анімацію, вибираємо рандомну підказку)
        backgroundRect.localScale = Vector3.one * 1.05f; // Трохи збільшуємо, щоб було куди рухати
        backgroundRect.anchoredPosition = Vector2.zero;
        
        if (tips.Length > 0 && tipText != null)
        {
            tipText.text = tips[Random.Range(0, tips.Length)];
        }

        progressBarFill.fillAmount = 0f;
        if (progressText) progressText.text = "0%";

        // 2. Плавно показуємо екран завантаження
        mainCanvasGroup.blocksRaycasts = true;
        float fadeTime = 0.4f;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            mainCanvasGroup.alpha = t / fadeTime;
            yield return null;
        }
        mainCanvasGroup.alpha = 1f;

        // 3. Починаємо асинхронне завантаження сцени!
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Забороняємо Unity миттєво перемикати сцену, коли вона завантажиться
        asyncLoad.allowSceneActivation = false; 

        float loadTimer = 0f;

        // 4. Цикл самого завантаження і анімації
        while (!asyncLoad.isDone)
        {
            loadTimer += Time.unscaledDeltaTime;

            // АНІМАЦІЯ ФОНУ (Камера летить вгору і зумиться)
            backgroundRect.localScale += Vector3.one * zoomSpeed * Time.unscaledDeltaTime;
            backgroundRect.anchoredPosition += Vector2.down * panSpeed * Time.unscaledDeltaTime; // Картинка їде вниз = камера летить вгору

            // ОНОВЛЕННЯ ПРОГРЕСУ
            // Unity завантажує до 0.9, а потім чекає allowSceneActivation
            float targetProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            // Якщо завантажилось швидше, ніж minLoadingTime, ми штучно "дотягуємо" смужку
            float displayProgress = Mathf.Clamp01(loadTimer / minLoadingTime);
            float finalProgress = Mathf.Min(targetProgress, displayProgress);

            progressBarFill.fillAmount = finalProgress;
            if (progressText) progressText.text = Mathf.RoundToInt(finalProgress * 100f) + "%";

            // Якщо сцена завантажена І мінімальний час пройшов - дозволяємо перехід
            if (asyncLoad.progress >= 0.9f && loadTimer >= minLoadingTime)
            {
                progressBarFill.fillAmount = 1f;
                if (progressText) progressText.text = "100%";
                asyncLoad.allowSceneActivation = true; // БАМ! Сцена перемкнулась на фоні
            }

            yield return null;
        }

        // 5. Сцена завантажилась, плавно ховаємо екран
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            // Анімація продовжує йти під час згасання екрану
            backgroundRect.localScale += Vector3.one * zoomSpeed * Time.unscaledDeltaTime;
            backgroundRect.anchoredPosition += Vector2.down * panSpeed * Time.unscaledDeltaTime;

            mainCanvasGroup.alpha = 1f - (t / fadeTime);
            yield return null;
        }

        mainCanvasGroup.alpha = 0f;
        mainCanvasGroup.blocksRaycasts = false;
        isLoading = false;
    }
}