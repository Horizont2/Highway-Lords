using UnityEngine;
using UnityEngine.Rendering; // Потрібно для SortingGroup

public class YSorter : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private SortingGroup sortingGroup;

    [Tooltip("Базове значення для сортування (щоб не перекрити UI)")]
    public int sortingOrderBase = 5000;

    [Tooltip("Увімкни для будівель (Шахта/Стіна), щоб скрипт спрацював 1 раз і вимкнувся для економії ресурсів")]
    public bool isStatic = false; 

    void Awake()
    {
        // Шукаємо компоненти рендеру на цьому об'єкті
        spriteRenderer = GetComponent<SpriteRenderer>();
        sortingGroup = GetComponent<SortingGroup>();
    }

    void LateUpdate()
    {
        // Чим нижче юніт на екрані (менший Y), тим БІЛЬШИМ має бути його Order (ближче до камери).
        // Множимо на 100, щоб навіть міліметри мали значення при обгоні.
        int sortOrder = (int)(sortingOrderBase - transform.position.y * 100);

        // Якщо юніт складається з кількох спрайтів (SortingGroup), сортуємо всю групу
        if (sortingGroup != null) 
            sortingGroup.sortingOrder = sortOrder;
        // Інакше сортуємо єдиний спрайт
        else if (spriteRenderer != null) 
            spriteRenderer.sortingOrder = sortOrder;

        // Будівлі не ходять, тому їм достатньо відсортуватися лише при появі
        if (isStatic) Destroy(this);
    }
}