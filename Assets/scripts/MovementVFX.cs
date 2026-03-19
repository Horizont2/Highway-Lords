using UnityEngine;

public class MovementVFX : MonoBehaviour
{
    public ParticleSystem dustParticles;
    public Animator anim; 

    void Start()
    {
        if (anim == null) anim = GetComponentInParent<Animator>(); 

        // БРОНЕБІЙНИЙ ФІКС: Примусово запускаємо "насос" частинок, 
        // інакше модуль Emission не зможе випускати пил!
        if (dustParticles != null && !dustParticles.isPlaying)
        {
            dustParticles.Play();
        }
    }

    void Update()
    {
        if (anim != null && dustParticles != null)
        {
            bool isMoving = anim.GetBool("IsMoving");
            
            var emission = dustParticles.emission;
            emission.enabled = isMoving;
        }
    }
}