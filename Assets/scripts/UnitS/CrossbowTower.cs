using UnityEngine;
using System.Collections;

public class CrossbowTower : MonoBehaviour
{
    [Header("Параметри стрільби")]
    public float range = 10f;
    public float fallbackReloadTime = 2.5f; 
    
    [Tooltip("Час від початку анімації атаки до моменту вильоту стріли")]
    public float shootDelay = 0.2f; 
    private float fireCountdown = 0f;

    [Header("Налаштування Unity")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public Animator animator;

    private Transform target;

    void Update()
    {
        // 1. ВИБІР ЦІЛІ
        if (GameManager.Instance != null && GameManager.Instance.manualTarget != null)
        {
            Transform manual = GameManager.Instance.manualTarget;
            if (IsValidTarget(manual)) target = manual;
            else UpdateTarget();
        }
        else
        {
            if (!IsValidTarget(target)) UpdateTarget();
        }

        // 2. СТРІЛЬБА ТА ПЕРЕЗАРЯДКА
        if (target != null)
        {
            if (fireCountdown <= 0f)
            {
                Shoot();
                
                if (GameManager.Instance != null)
                    fireCountdown = GameManager.Instance.GetCrossbowReloadTime();
                else 
                    fireCountdown = fallbackReloadTime;
            }
        }

        if (fireCountdown > 0f)
        {
            fireCountdown -= Time.deltaTime;
        }
    }

    bool IsValidTarget(Transform t)
    {
        if (t == null) return false;
        if (!t.gameObject.activeInHierarchy) return false;
        if (t.CompareTag("Untagged")) return false;
        
        float dist = Vector2.Distance(transform.position, t.position);
        return dist <= range;
    }

    void Shoot()
    {
        if (animator != null) animator.SetTrigger("Shoot");
        
        // Запускаємо постріл із затримкою
        StartCoroutine(FireRoutine(target));
    }

    IEnumerator FireRoutine(Transform initialTarget)
    {
        // Чекаємо, поки аніматор дійде до кадру, де арбалет стріляє
        yield return new WaitForSeconds(shootDelay);

        // Якщо за час замаху орк вже помер - шукаємо нового
        if (!IsValidTarget(initialTarget))
        {
            UpdateTarget();
            initialTarget = target;
        }

        // Спавнимо стрілу
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject projGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            
            int dmg = 25; 
            if (GameManager.Instance != null)
            {
                dmg = GameManager.Instance.GetCrossbowDamage();
            }

            if (projGO.TryGetComponent<Projectile>(out Projectile proj))
            {
                if (initialTarget != null)
                {
                    // Стріляємо з самонаведенням у ворога
                    proj.Initialize(initialTarget, dmg); 
                }
                else
                {
                    // Якщо ворогів взагалі немає, пускаємо просто вперед "в молоко"
                    Vector3 forwardPos = firePoint.position + Vector3.right * range;
                    proj.Initialize(forwardPos, dmg);
                }
            }
            else if (projGO.TryGetComponent<Arrow>(out Arrow arrowScript))
            {
                if (initialTarget == null)
                {
                    GameObject dummy = new GameObject("DummyTarget");
                    dummy.transform.position = firePoint.position + Vector3.right * range;
                    Destroy(dummy, 3f);
                    initialTarget = dummy.transform;
                }
                arrowScript.Initialize(initialTarget, dmg);
            }
        }
        else
        {
            Debug.LogWarning("У вежі не призначено Projectile Prefab або Fire Point!");
        }

        if (SoundManager.Instance != null) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot); 
    }

    void UpdateTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            if (enemy.CompareTag("Untagged")) continue;

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            
            if (dist < shortestDistance) 
            { 
                shortestDistance = dist; 
                nearestEnemy = enemy.transform; 
            }
        }

        if (nearestEnemy != null && shortestDistance <= range) target = nearestEnemy;
        else target = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}