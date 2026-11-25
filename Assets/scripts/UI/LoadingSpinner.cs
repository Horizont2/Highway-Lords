using UnityEngine;

public class LoadingSpinner : MonoBehaviour
{
    [Header("Швидкість обертання")]
    public float spinSpeed = 200f; // Чим більше, тим швидше

    void Update()
    {
        // Крутимо об'єкт по осі Z (проти годинникової стрілки, якщо мінус)
        // Використовуємо unscaledDeltaTime, щоб він крутився навіть якщо Time.timeScale = 0
        transform.Rotate(0f, 0f, -spinSpeed * Time.unscaledDeltaTime);
    }
}