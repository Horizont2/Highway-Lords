using UnityEngine;

public class MovementVFX : MonoBehaviour
{
    [Header("Налаштування")]
    public ParticleSystem dustEffect; // Сюди перетягнемо наш префаб
    public float velocityThreshold = 0.1f; // Поріг швидкості для появи пилу

    private Rigidbody2D rb;
    private ParticleSystem.EmissionModule emission;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (dustEffect != null)
        {
            emission = dustEffect.emission;
            // Спочатку вимикаємо емісію
            emission.enabled = false;
        }
    }

    void Update()
    {
        if (dustEffect == null || rb == null) return;

        // Перевіряємо, чи рухається юніт (величина вектора швидкості)
        bool isMoving = rb.linearVelocity.magnitude > velocityThreshold;

        // Вмикаємо/вимикаємо потік частинок
        if (isMoving && !emission.enabled)
        {
            emission.enabled = true;
        }
        else if (!isMoving && emission.enabled)
        {
            emission.enabled = false;
        }
    }
}