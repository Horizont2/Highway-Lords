using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Живе мерехтіння для 2D-світла: факели, вогнища, горно кузні, світло у вікнах.
///
/// Вішається на той самий об'єкт, де вже є Light2D (компонент нічого не створює).
/// Мерехтіння йде через Perlin-шум, а не Random — тому воно плавне й не схоже
/// на стробоскоп. Кожен екземпляр отримує власне зміщення по шуму, тож два
/// факели поруч не блимають синхронно.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class FlickeringLight2D : MonoBehaviour
{
    public enum Preset { Torch, Campfire, Forge, CalmGlow }

    [Header("Пресет")]
    [Tooltip("Застосовується в Reset та через контекстне меню 'Apply Preset'")]
    public Preset preset = Preset.Torch;

    [Header("Яскравість")]
    [Tooltip("0 — взяти поточну яскравість Light2D на старті")]
    public float baseIntensity = 0f;
    [Range(0f, 1f)] public float intensityAmplitude = 0.22f;
    [Tooltip("Швидкість мерехтіння")]
    public float speed = 2.6f;

    [Header("Радіус (необов'язково)")]
    public bool animateRadius = false;
    [Range(0f, 1f)] public float radiusAmplitude = 0.06f;

    [Header("Колір (необов'язково)")]
    [Tooltip("Легке гуляння між двома відтінками полум'я")]
    public bool animateColor = false;
    public Color warmColor = new Color(1f, 0.72f, 0.38f, 1f);
    public Color hotColor = new Color(1f, 0.88f, 0.62f, 1f);

    private Light2D light2D;
    private float noiseOffset;
    private float startIntensity;
    private float startOuterRadius;
    private float startInnerRadius;

    void Awake()
    {
        light2D = GetComponent<Light2D>();

        startIntensity = baseIntensity > 0f ? baseIntensity : light2D.intensity;
        startOuterRadius = light2D.pointLightOuterRadius;
        startInnerRadius = light2D.pointLightInnerRadius;

        // Власна фаза — інакше всі факели в сцені блимають в унісон.
        noiseOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float t = Time.time * speed + noiseOffset;

        // Perlin дає [0..1]; зсуваємо в [-1..1] і трохи загострюємо.
        float n = Mathf.PerlinNoise(t, 0f) * 2f - 1f;

        light2D.intensity = Mathf.Max(0f, startIntensity * (1f + n * intensityAmplitude));

        if (animateRadius)
        {
            float r = 1f + n * radiusAmplitude;
            light2D.pointLightOuterRadius = startOuterRadius * r;
            light2D.pointLightInnerRadius = startInnerRadius * r;
        }

        if (animateColor)
        {
            // Другий канал шуму, щоб колір не повторював яскравість один-в-один.
            float cn = Mathf.PerlinNoise(0f, t * 0.6f);
            light2D.color = Color.Lerp(warmColor, hotColor, cn);
        }
    }

    void Reset()
    {
        ApplyPreset();
    }

    [ContextMenu("Apply Preset")]
    public void ApplyPreset()
    {
        switch (preset)
        {
            case Preset.Torch:
                intensityAmplitude = 0.22f; speed = 2.6f;
                animateRadius = true; radiusAmplitude = 0.06f;
                animateColor = true;
                warmColor = new Color(1f, 0.72f, 0.38f, 1f);
                hotColor = new Color(1f, 0.88f, 0.62f, 1f);
                break;

            case Preset.Campfire:
                intensityAmplitude = 0.3f; speed = 1.8f;
                animateRadius = true; radiusAmplitude = 0.1f;
                animateColor = true;
                warmColor = new Color(1f, 0.6f, 0.26f, 1f);
                hotColor = new Color(1f, 0.84f, 0.5f, 1f);
                break;

            case Preset.Forge:
                intensityAmplitude = 0.16f; speed = 4.2f;
                animateRadius = false;
                animateColor = true;
                warmColor = new Color(1f, 0.46f, 0.2f, 1f);
                hotColor = new Color(1f, 0.74f, 0.42f, 1f);
                break;

            case Preset.CalmGlow:
                intensityAmplitude = 0.08f; speed = 0.9f;
                animateRadius = false;
                animateColor = false;
                break;
        }
    }
}
