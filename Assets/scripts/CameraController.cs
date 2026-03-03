using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Налаштування руху (Клавіатура)")]
    public float panSpeed = 15f; 

    [Header("Обмеження (Межі мапи)")]
    public float minX = -10f; 
    public float maxX = 20f;  

    [Header("Перетягування мишкою (Drag)")]
    public bool useDragPanning = true;
    [Tooltip("0 - Ліва кнопка, 1 - Права, 2 - Коліщатко миші")]
    public int dragMouseButton = 0; 

    private Vector3 dragOrigin;
    private Camera cam;

    void Start()
    {
        // Отримуємо компонент камери, щоб правильно рахувати координати миші
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        // Не рухаємо камеру, якщо гравець програв
        if (GameManager.Instance != null && GameManager.Instance.isDefeated)
            return;

        Vector3 pos = transform.position;

        // 1. Рух клавіатурою (A/D або Стрілки) залишаємо для зручності
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        pos.x += horizontalInput * panSpeed * Time.deltaTime;

        // 2. Перетягування мишкою
        if (useDragPanning)
        {
            // Коли тільки натиснули кнопку — запам'ятовуємо точку в світі
            if (Input.GetMouseButtonDown(dragMouseButton))
            {
                dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            }

            // Коли тримаємо кнопку натиснутою — рухаємо камеру
            if (Input.GetMouseButton(dragMouseButton))
            {
                Vector3 currentMousePos = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector3 difference = dragOrigin - currentMousePos;
                
                // Додаємо різницю тільки по осі X (щоб камера не їздила вгору-вниз)
                pos.x += difference.x;
            }
        }

        // Обмежуємо рух камери межами мапи
        pos.x = Mathf.Clamp(pos.x, minX, maxX);

        // Застосовуємо нову позицію
        transform.position = pos;
    }
}