using UnityEngine;
using TMPro;

/// <summary>
/// Цифри шкоди й здобичі.
///
/// Раніше попап просто їхав угору по прямій і згасав. Тепер він
/// вистрілює дугою, підскакує масштабом при появі й падає з гальмуванням —
/// найчастіший елемент бою має читатися з першого кадру.
///
/// Об'єкт живе в пулі, тож увесь стан скидається в OnSpawnFromPool:
/// поля з ініціалізацією виконуються лише при першому створенні.
/// </summary>
public class DamagePopup : MonoBehaviour, IPoolable
{
    private TMP_Text textMesh;

    [Header("Налаштування")]
    public SpriteRenderer iconSprite;
    public Vector3 resourceIconOffset = new Vector3(-0.8f, 0, 0);

    [Header("Рух")]
    [Tooltip("Скільки попап живе до початку згасання")]
    public float lifeTime = 0.75f;
    public float fadeDuration = 0.45f;

    [Tooltip("Початкова швидкість угору")]
    public float launchSpeed = 3.2f;
    [Tooltip("Розкид по горизонталі, щоб кілька попапів не злипалися")]
    public float horizontalSpread = 1.1f;
    public float gravity = 7f;

    [Header("Поява")]
    [Tooltip("З якого масштабу підскакує")]
    public float popFromScale = 0.4f;
    public float popDuration = 0.18f;

    [Header("Критичний удар")]
    public float criticalScale = 1.45f;
    public Color criticalColor = new Color(1f, 0.35f, 0.25f);

    private Color textColor;
    private Color iconColor;
    private Vector3 velocity;

    private float age;
    private bool isCritical;
    private Vector3 baseScale = Vector3.one;
    private bool hasBaseScale;

    void Awake()
    {
        textMesh = GetComponent<TMP_Text>();
        if (iconSprite == null) iconSprite = GetComponentInChildren<SpriteRenderer>();

        if (!hasBaseScale)
        {
            baseScale = transform.localScale;
            hasBaseScale = true;
        }
    }

    public void OnSpawnFromPool()
    {
        age = 0f;
        isCritical = false;
        velocity = Vector3.zero;

        if (!hasBaseScale)
        {
            baseScale = transform.localScale;
            hasBaseScale = true;
        }

        transform.localScale = baseScale * popFromScale;

        if (textMesh != null)
        {
            Color c = textMesh.color;
            c.a = 1f;
            textMesh.color = c;
            textColor = c;
        }

        if (iconSprite != null)
        {
            Color ic = iconSprite.color;
            ic.a = 1f;
            iconSprite.color = ic;
            iconColor = ic;
            iconSprite.enabled = false;
        }
    }

    public void Setup(int damageAmount, bool isCriticalHit = false)
    {
        if (textMesh == null) return;

        age = 0f;
        isCritical = isCriticalHit;

        if (iconSprite != null) iconSprite.enabled = false;
        textMesh.text = damageAmount.ToString();

        if (isCriticalHit)
        {
            textMesh.fontSize = 7;
            textMesh.color = criticalColor;
        }
        else
        {
            textMesh.fontSize = 5;
            textMesh.color = Color.white;
        }

        textColor = textMesh.color;
        Launch(launchSpeed, horizontalSpread);
    }

    public void SetupResource(Sprite resourceIcon, int amount, Color specificColor)
    {
        if (textMesh == null) return;

        age = 0f;
        isCritical = false;

        textMesh.text = "+" + amount.ToString();
        textMesh.color = specificColor;
        textColor = textMesh.color;
        textMesh.fontSize = 4;

        if (iconSprite != null)
        {
            iconSprite.enabled = true;
            iconSprite.sprite = resourceIcon;
            iconSprite.color = Color.white;
            iconColor = iconSprite.color;
            iconSprite.transform.localPosition = resourceIconOffset;
        }

        // Здобич підлітає спокійніше за шкоду — вона не має перебивати бій.
        Launch(launchSpeed * 0.65f, horizontalSpread * 0.5f);
    }

    private void Launch(float upSpeed, float spread)
    {
        velocity = new Vector3(Random.Range(-spread, spread), upSpeed, 0f);
        transform.localScale = baseScale * popFromScale;
    }

    void Update()
    {
        if (textMesh == null) return;

        age += Time.deltaTime;

        // Політ по дузі: початковий поштовх плюс гальмування.
        velocity.y -= gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        UpdateScale();
        UpdateFade();
    }

    private void UpdateScale()
    {
        float target = isCritical ? criticalScale : 1f;

        if (age < popDuration)
        {
            // Підскок із перельотом — попап "вистрілює", а не з'являється.
            float t = age / popDuration;
            float k = Mathf.LerpUnclamped(popFromScale, target, Easing.OutBack(t, 2.2f));
            transform.localScale = baseScale * k;
        }
        else
        {
            transform.localScale = baseScale * target;
        }
    }

    private void UpdateFade()
    {
        if (age < lifeTime) return;

        float t = Mathf.Clamp01((age - lifeTime) / Mathf.Max(0.01f, fadeDuration));
        float alpha = 1f - Easing.InCubic(t);

        textColor.a = alpha;
        textMesh.color = textColor;

        if (iconSprite != null && iconSprite.enabled)
        {
            iconColor.a = alpha;
            iconSprite.color = iconColor;
        }

        if (t >= 1f) PoolRegistry.Despawn(gameObject);
    }
}
