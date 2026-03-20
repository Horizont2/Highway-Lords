using UnityEngine;

public class GameStarter : MonoBehaviour
{
    void Start()
    {
        // Шукаємо твій екран завантаження на цій сцені
        LoadingManager lm = Object.FindFirstObjectByType<LoadingManager>();
        
        if (lm != null)
        {
            // Кажемо йому по-справжньому завантажити важку сцену гри
            lm.LoadScene("Main"); 
        }
        else
        {
            Debug.LogError("Не знайдено LoadingManager на Boot-сцені!");
        }
    }
}