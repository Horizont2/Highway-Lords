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
            // Один раз при старті, а не помилка: частина юнітів свідомо без пилу.
            Debug.LogWarning($"[UniversalDust] На '{gameObject.name}' немає ParticleSystem — пилу не буде.", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (dustSystem == null) return;

        float speed = Vector3.Distance(transform.position, lastPos) / Time.deltaTime;
        bool isMoving = speed > 0.1f;

        var em = dustSystem.emission;
        em.enabled = isMoving;

        lastPos = transform.position;
    }
}