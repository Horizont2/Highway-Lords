using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    [Header("Налаштування дощу")]
    public ParticleSystem rainParticles;
    public Image weatherDarkness; 
    public float darknessAlpha = 0.3f; 
    public float transitionSpeed = 0.5f;

    [Header("Налаштування блискавки")]
    public Image lightningFlash; // Біла картинка для спалаху
    public float minThunderTime = 10f; // Мінімальний час між громом
    public float maxThunderTime = 15f; // Максимальний час між громом

    [Header("Налаштування хмар (Тіней)")]
    public GameObject cloudShadowPrefab; // Префаб хмари
    public float minCloudInterval = 40f; // Як часто з'являються хмари
    public float maxCloudInterval = 80f;

    [Header("Таймери (у секундах)")]
    public float minClearTime = 60f;  
    public float maxClearTime = 180f; 
    public float minRainTime = 60f;   
    public float maxRainTime = 120f;  

    private bool isRaining = false;
    private Coroutine fadeCoroutine;
    private Coroutine thunderCoroutine;

    void Start()
    {
        // Зі старту вимикаємо дощ
        if (rainParticles != null) rainParticles.Stop();
        
        // Зі старту робимо екран світлим
        if (weatherDarkness != null) 
        {
            Color c = weatherDarkness.color; c.a = 0f; weatherDarkness.color = c;
        }
        
        // Зі старту вимикаємо спалах
        if (lightningFlash != null) 
        {
            Color c = lightningFlash.color; c.a = 0f; lightningFlash.color = c;
        }

        // Запускаємо цикли погоди та хмар
        StartCoroutine(WeatherRoutine());
        StartCoroutine(CloudRoutine()); 
    }

    IEnumerator WeatherRoutine()
    {
        // Невеличка випадкова затримка перед ПЕРШИМ дощем (від 10 до 30 сек)
        yield return new WaitForSeconds(Random.Range(10f, 30f));

        while (true)
        {
            // Починається дощ
            StartRain();
            yield return new WaitForSeconds(Random.Range(minRainTime, maxRainTime));

            // Дощ закінчується
            StopRain();
            yield return new WaitForSeconds(Random.Range(minClearTime, maxClearTime));
        }
    }

    // === ФАБРИКА ХМАР ===
    IEnumerator CloudRoutine()
    {
        while (true)
        {
            // Чекаємо випадковий час перед спробою створити хмару
            yield return new WaitForSeconds(Random.Range(minCloudInterval, maxCloudInterval));
            
            // Спавнимо хмару ТІЛЬКИ якщо зараз НЕ йде дощ (!isRaining)
            if (!isRaining && cloudShadowPrefab != null && Camera.main != null)
            {
                // Спавнимо значно лівіше за межами екрана, щоб вона плавно вповзала
                Vector3 spawnPos = Camera.main.transform.position;
                spawnPos.x -= 40f; 
                spawnPos.y += Random.Range(-5f, 5f); // Випадкова висота
                spawnPos.z = 0;
                
                Instantiate(cloudShadowPrefab, spawnPos, Quaternion.identity);
            }
        }
    }

    // === БЛИСКАВКА І ГРІМ ===
    IEnumerator ThunderRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minThunderTime, maxThunderTime));
            
            if (isRaining)
            {
                StartCoroutine(LightningFlashEffect());
            }
        }
    }

    IEnumerator LightningFlashEffect()
    {
        if (lightningFlash == null) yield break;

        // Вмикаємо звук грому
        if (SoundManager.Instance != null) SoundManager.Instance.PlayThunder();

        // Подвійний спалах (як справжня блискавка)
        Color c = lightningFlash.color;
        
        c.a = 0.8f; lightningFlash.color = c; // Перший яскравий спалах
        yield return new WaitForSeconds(0.05f);
        
        c.a = 0f; lightningFlash.color = c; // Темно
        yield return new WaitForSeconds(0.05f);
        
        c.a = 0.4f; lightningFlash.color = c; // Другий слабший спалах
        yield return new WaitForSeconds(0.05f);
        
        c.a = 0f; lightningFlash.color = c; // Вимикаємо повністю
    }

    [ContextMenu("Force Start Rain")] 
    public void StartRain()
    {
        if (isRaining) return;
        isRaining = true;

        // Вмикаємо частинки та звук
        if (rainParticles != null) rainParticles.Play();
        if (SoundManager.Instance != null) SoundManager.Instance.PlayRain();

        // Затемнюємо екран
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeDarkness(darknessAlpha));

        // Запускаємо грозу
        if (thunderCoroutine != null) StopCoroutine(thunderCoroutine);
        thunderCoroutine = StartCoroutine(ThunderRoutine());
    }

    [ContextMenu("Force Stop Rain")]
    public void StopRain()
    {
        if (!isRaining) return;
        isRaining = false;

        // Вимикаємо частинки та звук
        if (rainParticles != null) rainParticles.Stop(); 
        if (SoundManager.Instance != null) SoundManager.Instance.StopRain();

        // Повертаємо світло
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeDarkness(0f));

        // Зупиняємо грозу
        if (thunderCoroutine != null) StopCoroutine(thunderCoroutine);
    }

    IEnumerator FadeDarkness(float targetAlpha)
    {
        if (weatherDarkness == null) yield break;

        Color c = weatherDarkness.color;
        while (Mathf.Abs(c.a - targetAlpha) > 0.01f)
        {
            c.a = Mathf.MoveTowards(c.a, targetAlpha, transitionSpeed * Time.deltaTime);
            weatherDarkness.color = c;
            yield return null;
        }
    }
}