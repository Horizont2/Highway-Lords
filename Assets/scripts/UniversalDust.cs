using UnityEngine;

public class UniversalDust : MonoBehaviour
{
    public ParticleSystem dustSystem;
    private Vector3 lastPos;

    void Start()
    {
        lastPos = transform.position;
        if (dustSystem == null) dustSystem = GetComponentInChildren<ParticleSystem>();

        if (dustSystem != null)
        {
            var em = dustSystem.emission;
            em.enabled = false; 
            if (!dustSystem.isPlaying) dustSystem.Play(); 
        }
        else
        {
            Debug.LogError($"❌ ПОМИЛКА: На юніті {gameObject.name} ВЗАГАЛІ НЕМАЄ Particle System!");
        }
    }

    void Update()
    {
        if (dustSystem == null) return;

        float speed = Vector3.Distance(transform.position, lastPos) / Time.deltaTime;
        bool isMoving = speed > 0.1f;

        var em = dustSystem.emission;
        em.enabled = isMoving;

        // ДЕТЕКТИВ: Якщо юніт рухається, але ми не бачимо пил — пишемо в консоль!
        if (isMoving && gameObject.name.Contains("Archer"))
        {
            Debug.Log($"🏹 Лучник рухається зі швидкістю {speed}! Пил МАЄ йти. Якщо його не видно — він або прозорий, або під землею (Order in Layer)!");
        }

        lastPos = transform.position;
    }
}