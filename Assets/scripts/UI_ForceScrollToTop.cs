using UnityEngine;
using UnityEngine.UI;

public class UI_ForceScrollToTop : MonoBehaviour
{
    // Сюди перетягнеш свій ScrollRect (об'єкт ConstructionsScrollArea)
    public ScrollRect scrollRect; 

    void Start()
    {
        // Чекаємо кадр, щоб Unity встигла все розрахувати, а потім ставимо наверх
        StartCoroutine(ScrollToTop());
    }

    System.Collections.IEnumerator ScrollToTop()
    {
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f; // 1 = Верх, 0 = Низ
        }
    }
}