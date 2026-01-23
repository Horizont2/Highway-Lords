using UnityEngine;
using System.Collections;

public class Knight : MonoBehaviour
{
    [Header("Характеристики")]
    public float speed = 2.0f;
    public float attackRange = 0.8f;
    public float stopDistance = 0.5f;
    public float attackRate = 1f;
    
    [Header("Бойові параметри")]
    public int baseDamage = 10;
    public int myDamage;
    public int maxHealth = 120;
    private int currentHealth;

    [Header("Компоненти")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    // Патрулювання
    public float patrolRadius = 3f;
    private Vector3 startPoint;
    private bool movingRight = true;
    private float nextAttackTime = 0f;

    // ЦІЛІ (Тільки Охоронці та Вози)
    private Cart targetCart;
    private Guard targetGuard;

    void Start()
    {
        startPoint = transform.position;
        currentHealth = maxHealth;

        if (GameManager.Instance != null)
        {        
            GameManager.Instance.UpdateUI();
            myDamage = baseDamage + (GameManager.Instance.damageLevel * 5);
        }
        else
        {
            myDamage = baseDamage;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentUnits--;
            GameManager.Instance.UpdateUI();
        }
    }

    void Update()
    {
        FindNearestTarget();

        // Визначаємо пріоритетну ціль: Спочатку Охоронець, потім Віз
        Transform currentTargetTransform = null;
        if (targetGuard != null) currentTargetTransform = targetGuard.transform;
        else if (targetCart != null) currentTargetTransform = targetCart.transform;

        if (currentTargetTransform != null)
        {
            EngageEnemy(currentTargetTransform);
        }
        else
        {
            Patrol();
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.knightHit);

        if (GameManager.Instance != null) 
            GameManager.Instance.ShowDamage(damage, transform.position);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

    void EngageEnemy(Transform target)
    {
        FlipSprite(target.position.x);
        float distance = Vector2.Distance(transform.position, target.position);

        if (distance <= attackRange)
        {
            if (animator) animator.SetBool("IsMoving", false);
            if (Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
        else if (distance > stopDistance)
        {
            MoveTo(target.position);
        }
    }

    void Patrol()
    {
        float targetX = movingRight ? startPoint.x + patrolRadius : startPoint.x - patrolRadius;
        Vector3 targetPos = new Vector3(targetX, startPoint.y, 0);
        MoveTo(targetPos);
        if (Vector2.Distance(transform.position, targetPos) < 0.1f) movingRight = !movingRight;
    }

    void MoveTo(Vector3 targetPosition)
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.rightBoundary != null)
            {
                float maxX = GameManager.Instance.rightBoundary.position.x;
                if (targetPosition.x > maxX) targetPosition.x = maxX;
            }
            if (GameManager.Instance.leftBoundary != null)
            {
                float minX = GameManager.Instance.leftBoundary.position.x;
                if (targetPosition.x < minX) targetPosition.x = minX;
            }
        }
        
        if (animator) animator.SetBool("IsMoving", true);
        FlipSprite(targetPosition.x);
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
    }

    void FlipSprite(float targetX)
    {
        if (spriteRenderer == null) return;
        if (targetX < transform.position.x) spriteRenderer.flipX = true;
        else spriteRenderer.flipX = false;
    }

    // Оновлена логіка пошуку (Тільки Guard і Cart)
    void FindNearestTarget()
    {
        float minX = -1000f; float maxX = 1000f;
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.leftBoundary != null) minX = GameManager.Instance.leftBoundary.position.x - 2f;
            if (GameManager.Instance.rightBoundary != null) maxX = GameManager.Instance.rightBoundary.position.x + 2f;
        }

        targetGuard = null;
        targetCart = null;
        
        float shortestDist = Mathf.Infinity;
        // Шукаємо об'єкти з тегом "Enemy" (це мають бути Вози та Охоронці)
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        foreach (GameObject go in enemies)
        {
            if (go == gameObject) continue;
            if (go.transform.position.x > maxX || go.transform.position.x < minX) continue;

            float dist = Vector2.Distance(transform.position, go.transform.position);
            if (dist > 10f) continue; // Ігноруємо занадто далекі цілі

            // 1. ОХОРОНЕЦЬ
            Guard g = go.GetComponent<Guard>();
            if (g != null)
            {
                if (dist < shortestDist)
                {
                    shortestDist = dist;
                    targetGuard = g;
                    targetCart = null; // Охоронець важливіший
                }
                continue; 
            }

            // 2. ВІЗ
            Cart c = go.GetComponent<Cart>();
            if (c != null)
            {
                if (targetGuard == null && dist < shortestDist)
                {
                    shortestDist = dist;
                    targetCart = c;
                }
            }
        }
    }

    void Attack()
    {
        if (animator) animator.SetTrigger("Attack");
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit);
        
        if (targetGuard != null) targetGuard.TakeDamage(myDamage);
        else if (targetCart != null) targetCart.TakeDamage(myDamage);
    }
}