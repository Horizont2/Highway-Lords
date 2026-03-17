using UnityEngine;

public class MovementVFX : MonoBehaviour
{
    public ParticleSystem dustParticles;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInParent<Animator>(); // Шукаємо аніматор
    }

    void Update()
    {
        if (anim != null && dustParticles != null)
        {
            bool isMoving = anim.GetBool("IsMoving");
            
            // ЄДИНИЙ ПРАВИЛЬНИЙ СПОСІБ вмикати частинки в Unity:
            var emission = dustParticles.emission;
            emission.enabled = isMoving;
        }
    }
}