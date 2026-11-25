using UnityEngine;
using System.Collections;

public class SimpleShine : MonoBehaviour
{
    public RectTransform shineRect; // Перетягніть сюди сам об'єкт бліку
    public float speed = 1500f;     // Швидкість прольоту
    public float delay = 2f;        // Затримка між прольотами

    private float startX = -150f;   // Звідки починає (зліва)
    private float endX = 150f;      // Де закінчує (справа)

    void OnEnable()
    {
        if (shineRect != null) StartCoroutine(ShineRoutine());
    }

    IEnumerator ShineRoutine()
    {
        while (true)
        {
            // Ставимо блік зліва
            shineRect.anchoredPosition = new Vector2(startX, shineRect.anchoredPosition.y);
            
            // Рухаємо вправо
            while (shineRect.anchoredPosition.x < endX)
            {
                shineRect.anchoredPosition += new Vector2(speed * Time.deltaTime, 0);
                yield return null;
            }

            // Чекаємо перед наступним прольотом
            yield return new WaitForSeconds(delay);
        }
    }
}