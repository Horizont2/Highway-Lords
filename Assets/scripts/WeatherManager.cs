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
    public Image lightningFlash; 
    public float minThunderTime = 10f; 
    public float maxThunderTime = 15f; 

    [Header("Налаштування хмар (Тіней)")]
    public GameObject cloudShadowPrefab; 
    public float minCloudInterval = 40f; 
    public float maxCloudInterval = 80f;
    public float cloudSpawnOffsetX = 70f; 

    [Header("Таймери погоди (у секундах)")]
    [Tooltip("Час до найпершого дощу після старту гри")]
    public float firstRainDelayMin = 180f; // Мінімум 3 хвилини до першого дощу
    public float firstRainDelayMax = 300f; // Максимум 5 хвилин до першого дощу
    
    public float minClearTime = 120f;  // Мінімум 2 хв ясної погоди між дощами
    public float maxClearTime = 240f;  // Максимум 4 хв ясної погоди між дощами
    public float minRainTime = 40f;    // Мінімум 40 сек йде дощ
    public float maxRainTime = 90f;    // Максимум 1.5 хв йде дощ

    private bool isRaining = false;
    private Coroutine fadeCoroutine;
    private Coroutine thunderCoroutine;

    void Start()
    {
        if (rainParticles != null) 
        {
            rainParticles.Stop();
            rainParticles.gameObject.SetActive(false);
        }
        
        if (weatherDarkness != null) 
        {
            weatherDarkness.gameObject.SetActive(true); 
            Color c = weatherDarkness.color; c.a = 0f; weatherDarkness.color = c;
        }
        
        if (lightningFlash != null) 
        {
            lightningFlash.gameObject.SetActive(true); 
            Color c = lightningFlash.color; c.a = 0f; lightningFlash.color = c;
        }

        StartCoroutine(WeatherRoutine());
        StartCoroutine(CloudRoutine()); 
    }

    IEnumerator WeatherRoutine()
    {
        // === ВЕЛИКА ЗАТРИМКА ПЕРЕД ПЕРШИМ ДОЩЕМ ===
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
            
            if (isRaining)
            {
                StartCoroutine(LightningFlashEffect());
            }
        }
    }

    IEnumerator LightningFlashEffect()
    {
        if (lightningFlash == null) yield break;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayThunder();

        Color c = lightningFlash.color;
        
        c.a = 0.8f; lightningFlash.color = c; 
        yield return new WaitForSeconds(0.05f);
        
        c.a = 0f; lightningFlash.color = c; 
        yield return new WaitForSeconds(0.05f);
        
        c.a = 0.4f; lightningFlash.color = c; 
        yield return new WaitForSeconds(0.05f);
        
        c.a = 0f; lightningFlash.color = c; 
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
        fadeCoroutine = StartCoroutine(FadeDarkness(darknessAlpha));

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
        fadeCoroutine = StartCoroutine(FadeDarkness(0f));

        if (thunderCoroutine != null) StopCoroutine(thunderCoroutine);
    }

    IEnumerator DisableRainDelay()
    {
        yield return new WaitForSeconds(3f); 
        if (!isRaining && rainParticles != null)
        {
            rainParticles.gameObject.SetActive(false); 
        }
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