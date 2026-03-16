using UnityEngine;

public class BattleCameraScroll : MonoBehaviour
{
    public float panSpeed = 20f;
    public float minX = -10f; // Ліва межа карти
    public float maxX = 30f;  // Права межа карти (де вороги)

    private Vector3 dragOrigin;
    private Camera cam;

    void Start() { cam = GetComponent<Camera>(); }

    void Update()
    {
        PanCamera();
    }

    void PanCamera()
    {
        // Зберігаємо точку, де ми натиснули мишку
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        // Рухаємо камеру, поки мишка затиснута
        if (Input.GetMouseButton(0))
        {
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            
            // Рухаємо тільки по осі X (вліво-вправо)
            Vector3 newPos = transform.position + new Vector3(difference.x, 0, 0);
            newPos.x = Mathf.Clamp(newPos.x, minX, maxX); // Обмежуємо вихід за краї

            transform.position = newPos;
        }
    }
}