using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Тактильна відповідь кнопки на дотик.
///
/// У проєкті не було жодної реалізації IPointerDown — кнопки відповідали лише
/// стандартним підфарбуванням Unity. Це найчастіша взаємодія в грі, і саме
/// вона має відчуватися фізично: палець тисне — кнопка присідає, відпускає —
/// пружно вистрілює назад.
///
/// Вішається на той самий об'єкт, де Button. Якщо поруч є LivingButton,
/// який щокадру пише свій масштаб, натискання передається йому множником —
/// інакше вони затирали б одне одного.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Натискання")]
    [Tooltip("До якого масштабу присідає кнопка")]
    [Range(0.5f, 1f)] public float pressedScale = 0.91f;

    [Tooltip("Як швидко присідає")]
    public float pressDuration = 0.06f;

    [Tooltip("Як повертається. Довше за присідання — так читається пружність")]
    public float releaseDuration = 0.22f;

    [Tooltip("Наскільки проскакує 100% при поверненні")]
    [Range(0f, 4f)] public float releaseOvershoot = 2.6f;

    private Button button;
    private LivingButton livingButton;
    private Vector3 baseScale = Vector3.one;

    private Coroutine routine;
    private float current = 1f;

    void Awake()
    {
        button = GetComponent<Button>();
        livingButton = GetComponent<LivingButton>();

        // LivingButton сам володіє масштабом цього об'єкта, тож свій
        // базовий знімок беремо лише тоді, коли його немає.
        if (livingButton == null) baseScale = transform.localScale;
    }

    void OnDisable()
    {
        // Кнопку могли сховати натиснутою — інакше вона такою й лишиться.
        routine = null;
        current = 1f;
        Apply();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        Animate(pressedScale, pressDuration, overshoot: 0f);
    }

    public void OnPointerUp(PointerEventData eventData) => Release();

    public void OnPointerExit(PointerEventData eventData) => Release();

    private void Release()
    {
        // Повертаємо навіть для неактивної кнопки: вона могла стати
        // неактивною саме в момент натискання.
        Animate(1f, releaseDuration, releaseOvershoot);
    }

    private void Animate(float target, float duration, float overshoot)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ScaleRoutine(target, duration, overshoot));
    }

    private IEnumerator ScaleRoutine(float target, float duration, float overshoot)
    {
        float from = current;
        float time = 0f;

        while (time < duration)
        {
            // unscaled: кнопки мають працювати і в паузі, і в уповільненні
            // при прицілюванні залпом.
            time += Time.unscaledDeltaTime;
            float t = time / duration;

            float eased = overshoot > 0f ? Easing.OutBack(t, overshoot) : Easing.OutCubic(t);
            current = Mathf.LerpUnclamped(from, target, eased);
            Apply();

            yield return null;
        }

        current = target;
        Apply();
        routine = null;
    }

    private void Apply()
    {
        if (livingButton != null) livingButton.PressScale = current;
        else transform.localScale = baseScale * current;
    }
}
