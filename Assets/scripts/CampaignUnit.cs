using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CampaignUnit : MonoBehaviour
{
    public bool isEnemy;
    public string unitClass; 
    public int health;
    public int maxHealth;
    public int damage;
    public float moveSpeed;
    public float attackRange;
    public bool isRanged;
    public bool nativelyFacesLeft;

    private Animator anim;
    private CampaignUnit currentTarget;
    private float attackCooldown = 1.2f;
    private float lastAttackTime;
    public bool isDead = false;
    
    private Image hpFillImage;
    private GameObject bloodPrefab;
    private SpriteRenderer[] spriteRenderers;

    public Vector3 tacticalTargetPos;
    private float originalMoveSpeed;
    private float originalAttackCooldown;

    public int targetedByCount = 0; 

    public void Setup(bool enemy, string uClass, int hp, int dmg, float spd, bool ranged, GameObject blood, bool facesLeft)
    {
        isEnemy = enemy;
        unitClass = uClass;
        maxHealth = hp;
        health = hp;
        damage = dmg;
        moveSpeed = spd; 
        isRanged = ranged;
        bloodPrefab = blood;
        nativelyFacesLeft = facesLeft;
        
        attackRange = isRanged ? 4.5f : 1.3f; 
        
        originalMoveSpeed = moveSpeed;
        originalAttackCooldown = attackCooldown;

        anim = GetComponent<Animator>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
        
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img.gameObject.name.ToLower().Contains("fill") || img.type == Image.Type.Filled)
            {
                hpFillImage = img;
                break;
            }
        }
        if (hpFillImage != null) hpFillImage.fillAmount = 1f;

        tacticalTargetPos = transform.position; 
        FlipTowards(isEnemy ? transform.position + Vector3.left : transform.position + Vector3.right);
    }

    public void SetHighlight(bool isOn)
    {
        if (isDead || spriteRenderers == null) return;
        foreach (var sr in spriteRenderers)
        {
            sr.color = isOn ? new Color(1f, 0.9f, 0.4f, 1f) : Color.white; 
        }
    }

    public void ApplyWarCry(float duration)
    {
        if (isDead || isEnemy) return;
        StartCoroutine(WarCryRoutine(duration));
    }

    IEnumerator WarCryRoutine(float duration)
    {
        moveSpeed = originalMoveSpeed * 1.5f;
        attackCooldown = originalAttackCooldown * 0.5f;
        if (spriteRenderers != null) foreach (var sr in spriteRenderers) sr.color = new Color(1f, 0.5f, 0.5f, 1f);
        yield return new WaitForSeconds(duration);
        moveSpeed = originalMoveSpeed;
        attackCooldown = originalAttackCooldown;
        if (!isDead && spriteRenderers != null) foreach (var sr in spriteRenderers) sr.color = Color.white;
    }

    void SetTarget(CampaignUnit newTarget)
    {
        if (currentTarget == newTarget) return;
        if (currentTarget != null && !currentTarget.isDead) currentTarget.targetedByCount--;
        currentTarget = newTarget;
        if (currentTarget != null) currentTarget.targetedByCount++;
    }

    void Update()
    {
        if (isDead || BattleManager.Instance == null) return;

        var state = BattleManager.Instance.currentState;

        if (state == BattleManager.BattleState.Retreating)
        {
            if (!isEnemy)
            {
                // ШВИДКІСТЬ ВІДСТУПУ (x1.8 від звичайної швидкості - це швидкий біг, але не телепортація)
                transform.position += Vector3.left * (originalMoveSpeed * 1.8f) * Time.deltaTime;
                if (anim) anim.SetBool("IsMoving", true);
                FlipTowards(transform.position + Vector3.left);
            }
            else
            {
                if (anim) anim.SetBool("IsMoving", false);
            }
            return;
        }

        if (state == BattleManager.BattleState.March)
        {
            if (!isEnemy)
            {
                // МАРШ ТЕПЕР ШВИДШИЙ (2.5)
                transform.position += Vector3.right * 2.5f * Time.deltaTime;
                tacticalTargetPos = transform.position; 
                if (anim) anim.SetBool("IsMoving", true);
                FlipTowards(transform.position + Vector3.right);
            }
            else
            {
                if (anim) anim.SetBool("IsMoving", false);
            }
            return;
        }

        if (state == BattleManager.BattleState.TacticalPause)
        {
            if (!isEnemy)
            {
                float dist = Vector3.Distance(transform.position, tacticalTargetPos);
                if (dist > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, tacticalTargetPos, originalMoveSpeed * 2.5f * Time.deltaTime);
                    if (anim) anim.SetBool("IsMoving", true);
                    FlipTowards(tacticalTargetPos);
                }
                else
                {
                    if (anim) anim.SetBool("IsMoving", false);
                    FlipTowards(transform.position + Vector3.right); 
                }
            }
            return;
        }

        if (state == BattleManager.BattleState.Battle)
        {
            if (currentTarget != null && currentTarget.isDead) SetTarget(null);

            FindTarget();

            if (currentTarget != null)
            {
                float dist = Vector2.Distance(transform.position, currentTarget.transform.position);

                // Якщо ми ДАЛЕКО - йдемо і відштовхуємось від своїх (щоб не злипатися)
                if (dist > attackRange)
                {
                    ApplySeparation(); 
                    transform.position = Vector3.MoveTowards(transform.position, currentTarget.transform.position, moveSpeed * Time.deltaTime);
                    
                    if (anim) anim.SetBool("IsMoving", true);
                    FlipTowards(currentTarget.transform.position);
                }
                // Якщо ми БЛИЗЬКО - ЖОРСТКО СТОЇМО і б'ємо (ніякого сковзання)
                else
                {
                    if (anim) anim.SetBool("IsMoving", false);
                    FlipTowards(currentTarget.transform.position);

                    if (Time.time >= lastAttackTime + attackCooldown)
                    {
                        if (anim) anim.SetTrigger("Attack");
                        lastAttackTime = Time.time;
                    }
                }
            }
            else
            {
                if (anim) anim.SetBool("IsMoving", false);
                ApplySeparation(); // Трохи розходимось, якщо немає ворогів
            }
        }
    }

    void ApplySeparation()
    {
        Vector3 separation = Vector3.zero;
        int count = 0;
        float sepRadius = isRanged ? 1.0f : 1.2f; 

        foreach (var ally in BattleManager.Instance.GetAllUnits())
        {
            if (ally != this && !ally.isDead && ally.isEnemy == this.isEnemy)
            {
                float dist = Vector2.Distance(transform.position, ally.transform.position);
                if (dist > 0 && dist < sepRadius) 
                {
                    separation += (transform.position - ally.transform.position).normalized * (sepRadius - dist);
                    count++;
                }
            }
        }
        if (count > 0) transform.position += (separation / count) * Time.deltaTime * 8f;
    }

    void FindTarget()
    {
        if (currentTarget != null && !currentTarget.isDead) return;

        float closestDist = Mathf.Infinity;
        CampaignUnit bestTarget = null;

        foreach (var u in BattleManager.Instance.GetAllUnits())
        {
            if (u.isEnemy != this.isEnemy && !u.isDead)
            {
                float d = Vector2.Distance(transform.position, u.transform.position);
                if (!isRanged && u.targetedByCount >= (isEnemy ? 2 : 1)) continue; 

                if (d < closestDist)
                {
                    closestDist = d;
                    bestTarget = u;
                }
            }
        }
        
        if (bestTarget == null)
        {
            closestDist = Mathf.Infinity;
            foreach (var u in BattleManager.Instance.GetAllUnits())
            {
                if (u.isEnemy != this.isEnemy && !u.isDead)
                {
                    float d = Vector2.Distance(transform.position, u.transform.position);
                    if (d < closestDist) { closestDist = d; bestTarget = u; }
                }
            }
        }

        SetTarget(bestTarget);
    }

    void FlipTowards(Vector3 targetPos)
    {
        Vector3 scale = transform.localScale;
        float absX = Mathf.Abs(scale.x);

        if (targetPos.x > transform.position.x) scale.x = nativelyFacesLeft ? -absX : absX;
        else scale.x = nativelyFacesLeft ? absX : -absX;
        
        transform.localScale = scale;
    }

    public void Hit() { DealDamage(); }
    public void ShootArrow() { DealDamage(); }
    public void Shoot() { DealDamage(); }

    void DealDamage()
    {
        if (currentTarget != null && !currentTarget.isDead && !isDead)
        {
            currentTarget.TakeDamage(damage);
            
            if (SoundManager.Instance != null)
            {
                if (isRanged && SoundManager.Instance.arrowHit != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowHit);
                else if (!isRanged && SoundManager.Instance.swordHit != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit);
            }
        }
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        
        health -= dmg;
        if (hpFillImage != null) hpFillImage.fillAmount = (float)health / maxHealth; 
        
        if (BattleManager.Instance != null) BattleManager.Instance.ShowDamagePopup(transform.position + Vector3.up * 1.5f, dmg);
        if (bloodPrefab != null) Instantiate(bloodPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        StartCoroutine(HitFlash());
        if (health <= 0) Die();
    }

    IEnumerator HitFlash()
    {
        if (spriteRenderers != null)
        {
            foreach (var sr in spriteRenderers) sr.color = new Color(0.8f, 0.4f, 0.4f, 1f); 
            yield return new WaitForSeconds(0.15f);
            if (!isDead) foreach (var sr in spriteRenderers) sr.color = Color.white; 
        }
    }

    void Die()
    {
        isDead = true;
        gameObject.tag = "Untagged";
        SetTarget(null);
        
        if (anim) anim.enabled = false; 
        if (hpFillImage != null && hpFillImage.transform.parent != null) hpFillImage.transform.parent.gameObject.SetActive(false); 

        if (spriteRenderers != null)
        {
            foreach (var sr in spriteRenderers) 
            {
                sr.color = new Color(0.3f, 0.3f, 0.3f, 1f); 
                sr.sortingOrder = -10; 
            }
        }

        transform.rotation = Quaternion.Euler(0, 0, nativelyFacesLeft ? 90f : -90f);
        transform.position += new Vector3(0, -0.3f, 0); 
        
        Destroy(gameObject, 3.5f);
    }
}