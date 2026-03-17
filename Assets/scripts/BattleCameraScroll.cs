using UnityEngine;
using UnityEngine.EventSystems;

public class BattleCameraScroll : MonoBehaviour
{
    public float panSpeed = 15f;
    public float minX = -40f; 
    public float maxX = 30f;  

    private Camera cam;
    private Vector3 dragOrigin;
    private bool isDragging;

    void Start() { cam = GetComponent<Camera>(); }

    void Update()
    {
        if (BattleManager.Instance != null && 
           (BattleManager.Instance.currentState == BattleManager.BattleState.Intro || 
            BattleManager.Instance.currentState == BattleManager.BattleState.March))
        {
            isDragging = false;
            return;
        }

        // Рух на WASD / Стрілочки
        float moveX = Input.GetAxis("Horizontal");
        
        // РУХ НА ЛІВУ КНОПКУ МИШІ (0)
        if (Input.GetMouseButtonDown(0))
        {
            // Якщо клікнули по кнопці UI - ігноруємо скрол
            if (EventSystem.current.IsPointerOverGameObject()) return;

            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            isDragging = true;
        }
        
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 diff = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            moveX = diff.x * 2f; 
        }
        
        if (Input.GetMouseButtonUp(0)) isDragging = false;

        if (Mathf.Abs(moveX) > 0.01f)
        {
            Vector3 newPos = transform.position + new Vector3(moveX * panSpeed * Time.deltaTime, 0, 0);
            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
            transform.position = newPos;
        }
    }
}