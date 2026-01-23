using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    // === НОВЕ: Посилання на іконку ===
    public SpriteRenderer iconSprite;
    // ================================
    
    private float disappearTimer = 1f;
    private Color textColor;
    private Color iconColor; // Колір іконки для зникання
    private Vector3 moveVector;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        // Якщо іконку не призначили в інспекторі, пробуємо знайти її в дочірніх
        if (iconSprite == null) iconSprite = GetComponentInChildren<SpriteRenderer>();
    }

    // Стара функція для урону (залишається без змін)
    public void Setup(int damageAmount)
    {
        if (iconSprite != null) iconSprite.enabled = false; // Ховаємо іконку для урону
        textMesh.text = damageAmount.ToString();
        textColor = textMesh.color;
        textMesh.fontSize = 5; // Стандартний розмір для урону
        moveVector = new Vector3(Random.Range(-0.5f, 0.5f), 2f) * 3f;
    }

    // === НОВА ФУНКЦІЯ: Для ресурсів ===
    public void SetupResource(Sprite resourceIcon, int amount, Color specificColor)
    {
        // Налаштовуємо текст
        textMesh.text = "+" + amount.ToString();
        textMesh.color = specificColor;
        textColor = textMesh.color;
        textMesh.fontSize = 4; // Трохи менший шрифт для ресурсів

        // Налаштовуємо іконку
        if (iconSprite != null)
        {
            iconSprite.enabled = true;
            iconSprite.sprite = resourceIcon;
            iconColor = iconSprite.color;
        }

        // Вектор руху (трохи повільніший, ніж урон)
        moveVector = new Vector3(Random.Range(-0.3f, 0.3f), 1.5f) * 2f;
    }
    // ==================================

    void Update()
    {
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 8f * Time.deltaTime;

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            float disappearSpeed = 3f;
            
            // Зникання тексту
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;

            // === Зникання іконки ===
            if (iconSprite != null)
            {
                iconColor.a -= disappearSpeed * Time.deltaTime;
                iconSprite.color = iconColor;
            }
            // =======================

            if (textColor.a < 0) Destroy(gameObject);
        }
    }
}