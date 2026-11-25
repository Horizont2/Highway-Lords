using UnityEngine;
using UnityEngine.UI;

public class HealthBarSegments : MonoBehaviour
{
    [Header("Налаштування")]
    public GameObject tickPrefab;      
    public Transform tickContainer;    
    
    [Tooltip("Скільки ХП означає один квадратик?")]
    public int healthPerSegment = 25; // Поставив 25 за замовчуванням, щоб точно були лінії

    public void UpdateSegments(int maxHealth)
    {
        if (tickPrefab == null || tickContainer == null) return;

        // 1. Очищаємо старі рисочки
        foreach (Transform child in tickContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Рахуємо кількість ліній
        int numberOfLines = (maxHealth / healthPerSegment) - 1;
        if (numberOfLines <= 0) return;

        // 3. Створюємо нові
        for (int i = 1; i <= numberOfLines; i++)
        {
            float positionPercent = (float)i / (numberOfLines + 1);

            GameObject tick = Instantiate(tickPrefab, tickContainer);
            RectTransform rt = tick.GetComponent<RectTransform>();
            
            // ФІКС БАГІВ UNITY UI: примусово ставимо нормальний масштаб
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);

            // Налаштовуємо якорі: фіксований X (відсоток), розтягнення по Y (від 0 до 1)
            rt.anchorMin = new Vector2(positionPercent, 0f);
            rt.anchorMax = new Vector2(positionPercent, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            
            // Задаємо ширину рівно 2 пікселі (від -1 до 1 відносно центру), висота розтягнута
            rt.offsetMin = new Vector2(-1f, 0f);
            rt.offsetMax = new Vector2(1f, 0f); 
        }
    }
}