using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DepthSorter : MonoBehaviour
{
    // Точність сортування (чим більше число, тим плавніше перемикання)
    private const int SORTING_PRECISION = 100;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Змінюємо Order in Layer залежно від Y позиції.
        // Чим нижче об'єкт (менше Y), тим більший Order (малюється поверх інших).
        spriteRenderer.sortingOrder = -(int)(transform.position.y * SORTING_PRECISION);
    }
}