using UnityEngine;
using System.Collections;

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

    [Header("Авто-повернення після хвилі")]
    public float defaultX = 0f; // Позиція біля замку (налаштуйте в Інспекторі)
    public float returnSpeed = 1.5f; // Швидкість польоту камери

    private Vector3 dragOrigin;
    private Camera cam;
    private bool isAutoPanning = false;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isDefeated)
            return;

        // Якщо камера зараз автоматично летить до бази
        if (isAutoPanning)
        {
            // Якщо гравець вирішив сам посунути екран під час польоту - скасовуємо політ
            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetMouseButtonDown(dragMouseButton))
            {
                isAutoPanning = false;
                StopAllCoroutines();
            }
            else
            {
                return; // Інакше блокуємо звичайний рух і летимо далі
            }
        }

        Vector3 pos = transform.position;

        // 1. Рух клавіатурою
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        pos.x += horizontalInput * panSpeed * Time.deltaTime;

        // 2. Перетягування мишкою
        if (useDragPanning)
        {
            if (Input.GetMouseButtonDown(dragMouseButton))
            {
                dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            }

            if (Input.GetMouseButton(dragMouseButton))
            {
                Vector3 currentMousePos = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector3 difference = dragOrigin - currentMousePos;
                pos.x += difference.x;
            }
        }

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        transform.position = pos;
    }

    // Викликається з GameManager, коли хвиля завершена
    public void ReturnToBase()
    {
        StopAllCoroutines();
        StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        isAutoPanning = true;
        float startX = transform.position.x;
        float time = 0f;

        while (time < 1f)
        {
            // Використовуємо Time.unscaledDeltaTime на випадок пауз
            time += Time.unscaledDeltaTime * returnSpeed;
            
            // Математична функція для плавного старту та гальмування
            float smoothStep = Mathf.SmoothStep(0f, 1f, time); 
            
            Vector3 pos = transform.position;
            pos.x = Mathf.Lerp(startX, defaultX, smoothStep);
            transform.position = pos;
            
            yield return null;
        }
        
        isAutoPanning = false;
    }
}