using UnityEngine;

/// <summary>
/// Криві згладжування.
///
/// Навіщо: майже вся анімація в проєкті зроблена через Lerp із лінійним t —
/// панелі, попапи, рух UI. Лінійний рух читається як дешевий, бо в природі
/// нічого так не рухається: усе розганяється, проскакує ціль і сідає назад.
/// Саме цей перельот ("overshoot") дає відчуття ваги й пружності.
///
/// Усі функції приймають t у [0..1] і повертають множник, який може виходити
/// за межі [0..1] — це навмисно.
/// </summary>
public static class Easing
{
    /// <summary>Плавний старт і зупинка. Базовий вибір, коли нічого не має підстрибувати.</summary>
    public static float SmoothStep(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    /// <summary>Різкий старт, повільна зупинка. Для появи.</summary>
    public static float OutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    /// <summary>Повільний старт, різкий кінець. Для зникнення.</summary>
    public static float InCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    /// <summary>
    /// Проскакує ціль і повертається. Те, що робить появу панелі "живою".
    /// </summary>
    public static float OutBack(float t, float overshoot = 1.70158f)
    {
        t = Mathf.Clamp01(t) - 1f;
        return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
    }

    /// <summary>Дзеркальний OutBack: спершу трохи присідає, потім іде.</summary>
    public static float InBack(float t, float overshoot = 1.70158f)
    {
        t = Mathf.Clamp01(t);
        return t * t * ((overshoot + 1f) * t - overshoot);
    }

    /// <summary>Пружина з кількома згасаючими коливаннями.</summary>
    public static float OutElastic(float t, float amplitude = 1f, float period = 0.3f)
    {
        t = Mathf.Clamp01(t);
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;

        float s = period / (2f * Mathf.PI) * Mathf.Asin(Mathf.Clamp(1f / Mathf.Max(amplitude, 1f), -1f, 1f));
        return amplitude * Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - s) * (2f * Mathf.PI) / period) + 1f;
    }

    /// <summary>Падіння з відскоками.</summary>
    public static float OutBounce(float t)
    {
        t = Mathf.Clamp01(t);
        const float n = 7.5625f, d = 2.75f;

        if (t < 1f / d) return n * t * t;
        if (t < 2f / d) { t -= 1.5f / d; return n * t * t + 0.75f; }
        if (t < 2.5f / d) { t -= 2.25f / d; return n * t * t + 0.9375f; }
        t -= 2.625f / d;
        return n * t * t + 0.984375f;
    }

    /// <summary>
    /// Удар: різко підскакує до 1 і згасає до 0. На відміну від решти,
    /// повертає не прогрес, а силу — для короткого поштовху.
    /// </summary>
    public static float Punch(float t)
    {
        t = Mathf.Clamp01(t);
        return Mathf.Sin(t * Mathf.PI) * (1f - t);
    }
}
