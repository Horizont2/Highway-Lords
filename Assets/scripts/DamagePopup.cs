using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    
    [Header("Налаштування")]
    // === Посилання на іконку ===
    public SpriteRenderer iconSprite;
    
    // === Зміщення іконки ===
    public Vector3 resourceIconOffset = new Vector3(-0.8f, 0, 0); 
    // ============================

    private float disappearTimer = 1f;
    private Color textColor;
    private Color iconColor;
    private Vector3 moveVector;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (iconSprite == null) iconSprite = GetComponentInChildren<SpriteRenderer>();
    }

    // === ВИПРАВЛЕНО: Додано аргумент isCriticalHit ===
    public void Setup(int damageAmount, bool isCriticalHit = false)
    {
        if (iconSprite != null) iconSprite.enabled = false; // Ховаємо іконку для урону
        
        textMesh.text = damageAmount.ToString();

        if (isCriticalHit)
        {
            // Критичний удар: Великий, червоний
            textMesh.fontSize = 7;
            textMesh.color = Color.red;
        }
        else
        {
            // Звичайний удар: Менший, білий (або жовтий)
            textMesh.fontSize = 5;
            textMesh.color = Color.white; 
        }

        textColor = textMesh.color; // Запам'ятовуємо колір для зникання
        moveVector = new Vector3(Random.Range(-0.5f, 0.5f), 2f) * 3f;
    }
    // ================================================

    public void SetupResource(Sprite resourceIcon, int amount, Color specificColor)
    {
        // 1. Налаштовуємо текст
        textMesh.text = "+" + amount.ToString();
        textMesh.color = specificColor;
        textColor = textMesh.color;
        textMesh.fontSize = 4;

        // 2. Налаштовуємо іконку
        if (iconSprite != null)
        {
            iconSprite.enabled = true;
            iconSprite.sprite = resourceIcon;
            iconSprite.color = Color.white; // Скидаємо колір іконки
            iconColor = iconSprite.color;

            // === ВАЖЛИВО: Застосовуємо зміщення ===
            iconSprite.transform.localPosition = resourceIconOffset; 
        }

        moveVector = new Vector3(Random.Range(-0.3f, 0.3f), 1.5f) * 2f;
    }

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

            // Зникання іконки
            if (iconSprite != null && iconSprite.enabled)
            {
                iconColor.a -= disappearSpeed * Time.deltaTime;
                iconSprite.color = iconColor;
            }

            if (textColor.a < 0) Destroy(gameObject);
        }
    }
}