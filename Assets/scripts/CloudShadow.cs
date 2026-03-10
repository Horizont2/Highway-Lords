using UnityEngine;

public class CloudShadow : MonoBehaviour
{
    [Header("Базові налаштування")]
    public float baseSpeed = 1.5f; 
    public float lifetime = 60f; 

    private float actualSpeed;
    private float driftY;
    private Vector3 initialScale;
    private float randomMorphSpeed;

    void Start()
    {
        // Знищуємо через заданий час
        Destroy(gameObject, lifetime);

        // 1. Кожна хмара летить зі своєю швидкістю (щоб не було "конвеєра")
        actualSpeed = baseSpeed * Random.Range(0.7f, 1.3f);

        // 2. Легкий дрейф по вертикалі (вітер рідко дме ідеально рівно)
        driftY = Random.Range(-0.3f, 0.3f);

        // 3. Випадковий розмір (одні величезні, інші менші)
        float randomScale = Random.Range(0.8f, 1.5f);
        transform.localScale *= randomScale;

        initialScale = transform.localScale;
        
        // Швидкість зміни форми хмари
        randomMorphSpeed = Random.Range(0.2f, 0.5f);

        // 4. Випадкова густота (прозорість)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Random.Range(0.15f, 0.35f); // Від 15% до 35% прозорості
            sr.color = c;
        }
    }

    void Update()
    {
        // Рухаємо хмару з урахуванням дрейфу
        transform.Translate(new Vector3(actualSpeed, driftY, 0) * Time.deltaTime);

        // Імітація зміни форми хмари (плавне "дихання" розміру)
        float morphX = Mathf.Sin(Time.time * randomMorphSpeed) * 0.1f;
        float morphY = Mathf.Cos(Time.time * randomMorphSpeed) * 0.1f;
        
        transform.localScale = initialScale + new Vector3(morphX, morphY, 0);
    }
}