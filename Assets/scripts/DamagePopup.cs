using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour, IPoolable
{
    // Замінюємо TextMeshPro на універсальний TMP_Text
    private TMP_Text textMesh; 
    
    [Header("Налаштування")]
    public SpriteRenderer iconSprite;
    public Vector3 resourceIconOffset = new Vector3(-0.8f, 0, 0); 

    private const float LifeTime = 1f;

    private float disappearTimer = LifeTime;
    private Color textColor;
    private Color iconColor;
    private Vector3 moveVector;

    void Awake()
    {
        // Шукаємо будь-який текстовий компонент TMP
        textMesh = GetComponent<TMP_Text>(); 
        if (iconSprite == null) iconSprite = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// Пул віддає цей самий об'єкт повторно, а disappearTimer і прозорість
    /// лишаються від минулого разу — без скидання попап зникав би миттєво.
    /// </summary>
    public void OnSpawnFromPool()
    {
        disappearTimer = LifeTime;

        if (textMesh != null)
        {
            Color c = textMesh.color;
            c.a = 1f;
            textMesh.color = c;
            textColor = c;
        }

        if (iconSprite != null)
        {
            Color ic = iconSprite.color;
            ic.a = 1f;
            iconSprite.color = ic;
            iconColor = ic;
            iconSprite.enabled = false;
        }
    }

    public void Setup(int damageAmount, bool isCriticalHit = false)
    {
        disappearTimer = LifeTime;

        if (textMesh == null) return; // Захист: якщо тексту немає, нічого не робимо
        
        if (iconSprite != null) iconSprite.enabled = false;
        textMesh.text = damageAmount.ToString();

        if (isCriticalHit)
        {
            textMesh.fontSize = 7;
            textMesh.color = Color.red;
        }
        else
        {
            textMesh.fontSize = 5;
            textMesh.color = Color.white; 
        }

        textColor = textMesh.color;
        moveVector = new Vector3(Random.Range(-0.5f, 0.5f), 2f) * 3f;
    }

    public void SetupResource(Sprite resourceIcon, int amount, Color specificColor)
    {
        if (textMesh == null) return;
        disappearTimer = LifeTime;

        textMesh.text = "+" + amount.ToString();
        textMesh.color = specificColor;
        textColor = textMesh.color;
        textMesh.fontSize = 4;

        if (iconSprite != null)
        {
            iconSprite.enabled = true;
            iconSprite.sprite = resourceIcon;
            iconSprite.color = Color.white;
            iconColor = iconSprite.color;
            iconSprite.transform.localPosition = resourceIconOffset; 
        }

        moveVector = new Vector3(Random.Range(-0.3f, 0.3f), 1.5f) * 2f;
    }

    void Update()
    {
        if (textMesh == null) return; // Головний захист від NullReference

        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 8f * Time.deltaTime;

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            float disappearSpeed = 3f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;

            if (iconSprite != null && iconSprite.enabled)
            {
                iconColor.a -= disappearSpeed * Time.deltaTime;
                iconSprite.color = iconColor;
            }

            if (textColor.a < 0) PoolRegistry.Despawn(gameObject);
        }
    }
}