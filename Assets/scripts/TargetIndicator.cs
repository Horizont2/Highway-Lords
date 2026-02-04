using UnityEngine;

public class TargetIndicator : MonoBehaviour
{
    private Transform target;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Hide(); // Ховаємо на старті
    }

    void Update()
    {
        // Якщо ціль існує і жива
        if (target != null && target.gameObject.activeInHierarchy && !target.CompareTag("Untagged"))
        {
            transform.position = target.position; 
            // Можна додати обертання для краси
            transform.Rotate(Vector3.forward * 100 * Time.deltaTime);
        }
        else
        {
            Hide(); // Якщо ціль померла - ховаємо курсор
        }
    }

    public void Show(Transform newTarget)
    {
        target = newTarget;
        if (spriteRenderer) spriteRenderer.enabled = true;
    }

    public void Hide()
    {
        target = null;
        if (spriteRenderer) spriteRenderer.enabled = false;
    }
}