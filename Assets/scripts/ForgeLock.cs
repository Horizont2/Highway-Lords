using UnityEngine;
using UnityEngine.UI; // Обов'язково для роботи з UI

[RequireComponent(typeof(ScrollRect))] // Гарантує, що скрипт не впаде без ScrollRect
public class ForgeLock : MonoBehaviour
{
    private ScrollRect scrollRect;

    void Start()
    {
        // Отримуємо доступ до компонента прокрутки на цьому ж об'єкті
        scrollRect = GetComponent<ScrollRect>();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // ЛОГІКА:
        // Якщо Списоносець відкритий (isSpearmanUnlocked == true) -> Скрол УВІМКНЕНО.
        // Якщо закритий (false) -> Скрол ВИМКНЕНО.
        
        if (scrollRect.vertical != GameManager.Instance.isSpearmanUnlocked)
        {
            scrollRect.vertical = GameManager.Instance.isSpearmanUnlocked;
        }
    }
}