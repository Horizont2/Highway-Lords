using UnityEngine;
using UnityEngine.EventSystems;

public class ManualMapScroll : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    private RectTransform mapRect;
    private Canvas canvas;

    [Header("Налаштування обмежень")]
    public float minX = -1000f; // Налаштуй ці цифри під розмір своєї карти
    public float maxX = 1000f;
    public float minY = -800f;
    public float maxY = 800f;

    void Awake()
    {
        mapRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Потрібно для ініціалізації драгу
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Розраховуємо зміщення мишки відносно масштабу канвасу
        Vector2 delta = eventData.delta / canvas.scaleFactor;
        
        // Отримуємо нову позицію
        Vector2 newPos = mapRect.anchoredPosition + delta;

        // Обмежуємо рух, щоб карта не вилітала за межі екрана
        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

        mapRect.anchoredPosition = newPos;
    }
}