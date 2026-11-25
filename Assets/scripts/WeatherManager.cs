using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// Погода: дощ, блискавки, хмарні тіні.
///
/// Затемнення під дощ робиться справжнім 2D-освітленням і пост-обробкою,
/// а не чорною UI-картинкою поверх екрана. Світло реагує на все в сцені,
/// тож замок, юніти й земля темнішають разом, а не ховаються під плівкою.
///
/// Якщо globalLight / globalVolume не призначені — працює старий шлях
/// через weatherDarkness / lightningFlash, тому сцена не ламається.
/// </summary>
public class WeatherManager : MonoBehaviour
{
    [Header("Освітлення (основний шлях)")]
    [Tooltip("Глобальне 2D-світло сцени — Rendering/Global Light 2D")]
    public Light2D globalLight;
    [Tooltip("Глобальний Volume з профілем пост-обробки — Rendering/Global Volume")]
    public Volume globalVolume;

    [Header("Налаштування дощу")]
    public ParticleSystem rainParticles;

    [Header("Ясна погода")]
    public Color clearLightColor = Color.white;
    [Range(0f, 2f)] public float clearLightIntensity = 1f;

    [Header("Погода під дощ")]
    [Tooltip("Холодний відтінок — дощ читається навіть без затемнення")]
    public Color rainLightColor = new Color(0.72f, 0.80f, 1f, 1f);
    [Range(0f, 2f)] public float rainLightIntensity = 0.58f;
    [Range(-100f, 100f)] public float rainSaturation = -20f;
    [Range(-100f, 100f)] public float rainContrast = 8f;
    [Range(0f, 1f)] public float rainVignette = 0.34f;

    [Tooltip("Скільки секунд триває перехід між ясно і дощ")]
    public float weatherTransitionDuration = 6f;

    [Header("Налаштування блискавки")]
    [Range(0f, 4f)] public float lightningIntensity = 2.4f;
    public Color lightningColor = new Color(0.86f, 0.92f, 1f, 1f);
    public float minThunderTime = 10f;
    public float maxThunderTime = 15f;

    [Header("Налаштування хмар (Тіней)")]
    public GameObject cloudShadowPrefab;
    public float minCloudInterval = 40f;
    public float maxCloudInterval = 80f;
    public float cloudSpawnOffsetX = 70f;

    [Header("Таймери погоди (у секундах)")]
    [Tooltip("Час до найпершого дощу після старту гри")]
    public float firstRainDelayMin = 180f;
    public float firstRainDelayMax = 300f;

    public float minClearTime = 120f;
    public float maxClearTime = 240f;
    public float minRainTime = 40f;
    public float maxRainTime = 90f;

    [Header("Застарілий шлях (лише якщо немає globalLight)")]
    [Tooltip("Чорна UI-картинка поверх екрана. Використовується тільки як запасний варіант.")]
    public Image weatherDarkness;
    public float darknessAlpha = 0.3f;
    public Image lightningFlash;
    public float transitionSpeed = 0.5f;

    private bool isRaining = false;
    private Coroutine fadeCoroutine;
    private Coroutine thunderCoroutine;

    // Кеш оверайдів пост-обробки
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private float baseSaturation, baseContrast, baseVignette;

    private bool UseLighting => globalLight != null;

    void Start()
    {
        if (rainParticles != null)
        {
            rainParticles.Stop();
            rainParticles.gameObject.SetActive(false);
        }

        CachePostProcessing();

        if (UseLighting)
        {
            ApplyLighting(0f);

            // Запасний оверлей більше не потрібен — тримаємо його прозорим.
            if (weatherDarkness != null) SetImageAlpha(weatherDarkness, 0f);
        }
        else
        {
            if (weatherDarkness != null)
            {
                weatherDarkness.gameObject.SetActive(true);
                SetImageAlpha(weatherDarkness, 0f);
            }
        }

        if (lightningFlash != null)
        {
            lightningFlash.gameObject.SetActive(true);
            SetImageAlpha(lightningFlash, 0f);
        }

        StartCoroutine(WeatherRoutine());
        StartCoroutine(CloudRoutine());
    }

    private void CachePostProcessing()
    {
        if (globalVolume == null) return;

        // .profile (а не .sharedProfile) дає рантайм-копію,
        // тож ми не змінюємо asset на диску під час гри.
        VolumeProfile profile = globalVolume.profile;
        if (profile == null) return;

        if (profile.TryGet(out colorAdjustments))
        {
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.contrast.overrideState = true;
            baseSaturation = colorAdjustments.saturation.value;
            baseContrast = colorAdjustments.contrast.value;
        }

        if (profile.TryGet(out vignette))
        {
            vignette.intensity.overrideState = true;
            baseVignette = vignette.intensity.value;
        }
    }

    /// <summary>t: 0 — ясно, 1 — дощ.</summary>
    private void ApplyLighting(float t)
    {
        if (globalLight != null)
        {
            globalLight.color = Color.Lerp(clearLightColor, rainLightColor, t);
            globalLight.intensity = Mathf.Lerp(clearLightIntensity, rainLightIntensity, t);
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = Mathf.Lerp(baseSaturation, rainSaturation, t);
            colorAdjustments.contrast.value = Mathf.Lerp(baseContrast, rainContrast, t);
        }

        if (vignette != null)
            vignette.intensity.value = Mathf.Lerp(baseVignette, rainVignette, t);
    }

    private static void SetImageAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    IEnumerator WeatherRoutine()
    {
        yield return new WaitForSeconds(Random.Range(firstRainDelayMin, firstRainDelayMax));

        while (true)
        {
            StartRain();
            yield return new WaitForSeconds(Random.Range(minRainTime, maxRainTime));

            StopRain();
            yield return new WaitForSeconds(Random.Range(minClearTime, maxClearTime));
        }
    }

    IEnumerator CloudRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minCloudInterval, maxCloudInterval));

            if (!isRaining && cloudShadowPrefab != null && Camera.main != null)
            {
                Vector3 spawnPos = Camera.main.transform.position;
                spawnPos.x -= cloudSpawnOffsetX;
                spawnPos.y += Random.Range(-5f, 5f);
                spawnPos.z = 0;

                Instantiate(cloudShadowPrefab, spawnPos, Quaternion.identity);
            }
        }
    }

    IEnumerator ThunderRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minThunderTime, maxThunderTime));
            if (isRaining) yield return LightningFlashEffect();
        }
    }

    IEnumerator LightningFlashEffect()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayThunder();

        // Подвійний спалах: яскравий короткий, потім слабший.
        yield return FlashOnce(1f, 0.05f);
        yield return new WaitForSeconds(0.06f);
        yield return FlashOnce(0.5f, 0.05f);

        // Повертаємо поточний стан погоди (ми під дощем).
        if (UseLighting) ApplyLighting(isRaining ? 1f : 0f);
    }

    IEnumerator FlashOnce(float strength, float duration)
    {
        if (UseLighting)
        {
            globalLight.color = Color.Lerp(rainLightColor, lightningColor, strength);
            globalLight.intensity = Mathf.Lerp(rainLightIntensity, lightningIntensity, strength);
            yield return new WaitForSeconds(duration);
            globalLight.intensity = rainLightIntensity;
            globalLight.color = rainLightColor;
        }
        else
        {
            SetImageAlpha(lightningFlash, 0.8f * strength);
            yield return new WaitForSeconds(duration);
            SetImageAlpha(lightningFlash, 0f);
        }
    }

    [ContextMenu("Force Start Rain")]
    public void StartRain()
    {
        if (isRaining) return;
        isRaining = true;

        if (rainParticles != null)
        {
            rainParticles.gameObject.SetActive(true);
            rainParticles.Play();
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayRain();

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(TransitionWeather(1f));

        if (thunderCoroutine != null) StopCoroutine(thunderCoroutine);
        thunderCoroutine = StartCoroutine(ThunderRoutine());
    }

    [ContextMenu("Force Stop Rain")]
    public void StopRain()
    {
        if (!isRaining) return;
        isRaining = false;

        if (rainParticles != null)
        {
            rainParticles.Stop();
            StartCoroutine(DisableRainDelay());
        }

        if (SoundManager.Instance != null) SoundManager.Instance.StopRain();

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(TransitionWeather(0f));

        if (thunderCoroutine != null) StopCoroutine(thunderCoroutine);
    }

    IEnumerator DisableRainDelay()
    {
        yield return new WaitForSeconds(3f);
        if (!isRaining && rainParticles != null) rainParticles.gameObject.SetActive(false);
    }

    IEnumerator TransitionWeather(float target)
    {
        if (!UseLighting)
        {
            yield return FadeDarknessOverlay(target > 0.5f ? darknessAlpha : 0f);
            yield break;
        }

        float from = Mathf.Approximately(target, 1f) ? 0f : 1f;
        float duration = Mathf.Max(0.01f, weatherTransitionDuration);
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            ApplyLighting(Mathf.SmoothStep(from, target, time / duration));
            yield return null;
        }

        ApplyLighting(target);
    }

    IEnumerator FadeDarknessOverlay(float targetAlpha)
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
