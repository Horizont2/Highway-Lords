using UnityEngine;
using UnityEngine.InputSystem; // Додаємо цей простір імен
using UnityEngine.EventSystems;

public class EnemySelector : MonoBehaviour
{
    void Update()
    {
        // Перевіряємо, чи підключена мишка
        if (Mouse.current == null) return;

        // "wasPressedThisFrame" - це аналог Input.GetMouseButtonDown(0)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 1. Перевірка на UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 2. Отримуємо позицію мишки через нову систему
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log($"🎯 ВЛУЧИВ У: {hit.collider.gameObject.name}");

                if (hit.collider.gameObject == gameObject)
                {
                    SelectEnemy();
                }
            }
        }
    }

    void SelectEnemy()
    {
        if (GameManager.Instance != null)
        {
            Debug.Log("✅ Ціль обрано!");
            GameManager.Instance.SetManualTarget(transform);
            
            if (SoundManager.Instance != null) 
                SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        }
    }
}