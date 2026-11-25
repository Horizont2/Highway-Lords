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

    /// <summary>Скільки живе ефект крові, перш ніж повернутися в пул.</summary>
    private const float BloodLifeTime = 2f;


    /// <summary>Поки Setup не викликали, юніт не діє: полів ще немає.</summary>
    private bool isReady = false;

    /// <summary>
    /// Скрипти AI з режиму оборони. У сцені бою вони не потрібні — тут
    /// юнітом керує CampaignUnit. Раніше BattleManager зривав їх через
    /// DestroyImmediate у рантаймі; тепер просто вимикаємо.
    /// </summary>
    private static readonly string[] DefenceAiScripts =
    {
        "Knight", "Archer", "Spearman", "Cavalry",
        "Guard", "EnemyArcher", "EnemySpearman", "EnemyHorse",
        "EnemyElephant", "Boss", "BatteringRam",
        "EnemySelector", "UnitStats", "EnemyStats",
        "WallArcher", "CrossbowTower",
    };

    private void DisableDefenceAi()
    {
        foreach (var script in GetComponents<MonoBehaviour>())
        {
            if (script == null || script == this) continue;

            string name = script.GetType().Name;
            foreach (var banned in DefenceAiScripts)
            {
                if (name != banned) continue;
                script.enabled = false;
                break;
            }
        }
    }

    public void Setup(bool enemy, string uClass, int hp, int dmg, float spd, bool ranged, GameObject blood, bool facesLeft)
    {
        DisableDefenceAi();

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

        // Розносимо юнітів по кадрах, щоб навантаження не збиралося в піки.
        separationPhase = Random.Range(0, SeparationInterval);

        isReady = true;
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
        if (currentTarget != null) currentTarget.targetedByCount--;
        currentTarget = newTarget;
        if (currentTarget != null) currentTarget.targetedByCount++;
    }

    void Update()
    {
        if (!isReady || isDead || BattleManager.Instance == null) return;

        var state = BattleManager.Instance.currentState;

        if (state == BattleManager.BattleState.Retreating)
        {
            MoveUnit(Vector3.left * 1.8f);
            return;
        }

        if (state == BattleManager.BattleState.March)
        {
            MoveUnit(Vector3.right * 2.5f);
            tacticalTargetPos = transform.position; 
            return;
        }

        if (state == BattleManager.BattleState.TacticalPause)
        {
            if (!isEnemy)
            {
                if (Vector3.Distance(transform.position, tacticalTargetPos) > 0.05f)
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

                // --- НОВА ЛОГІКА: КАЙТИНГ ДЛЯ ЛУЧНИКІВ ---
                if (isRanged && dist < attackRange * 0.4f)
                {
                    // Ворог занадто близько! Відходимо назад
                    Vector3 retreatDir = (transform.position - currentTarget.transform.position).normalized;
                    retreatDir.y = 0; // Відходимо тільки по горизонталі
                    transform.position += retreatDir * (moveSpeed * 0.8f) * Time.deltaTime;
                    
                    if (anim) anim.SetBool("IsMoving", true);
                    FlipTowards(currentTarget.transform.position); // Все ще дивимось на ворога
                }
                // --- ЗВИЧАЙНИЙ РУХ ДО ЦІЛІ ---
                else if (dist > attackRange)
                {
                    ApplySeparation(); 
                    // Додаємо мікро-зміщення по осі Y, щоб вони не йшли "гуськом" один за одним
                    Vector3 targetPos = currentTarget.transform.position;
                    targetPos.y += Random.Range(-0.1f, 0.1f); 

                    transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                    
                    if (anim) anim.SetBool("IsMoving", true);
                    FlipTowards(currentTarget.transform.position);
                }
                // --- АТАКА ---
                else
                {
                    if (anim) anim.SetBool("IsMoving", false);
                    FlipTowards(currentTarget.transform.position);

                    if (Time.time >= lastAttackTime + attackCooldown)
                    {
                        if (anim) anim.SetTrigger("Attack");
                        lastAttackTime = Time.time;
                        StartCoroutine(DelayedDamageRoutine());
                    }
                }
            }
            else
            {
                if (anim) anim.SetBool("IsMoving", false);
                ApplySeparation(); 
            }
        }
    }

    private void MoveUnit(Vector3 directionMultiplier)
    {
        if (isEnemy)
        {
            if (anim) anim.SetBool("IsMoving", false);
            return;
        }
        transform.position += directionMultiplier * (originalMoveSpeed * Time.deltaTime);
        if (anim) anim.SetBool("IsMoving", true);
        FlipTowards(transform.position + directionMultiplier);
    }

    // ВИНЕСЛИ КОРУТИНУ З UPDATE (Це виправить купу мікро-багів)
    private IEnumerator DelayedDamageRoutine()
    {
        yield return new WaitForSeconds(0.4f); 
        DealDamage();
    }

    // Розштовхування — найдорожче місце бою: кожен юніт обходить усіх інших.
    // При 85 юнітах це ~7000 перевірок на кадр. Рахуємо вектор раз на кілька
    // кадрів і застосовуємо збережений — рух лишається плавним.
    private const int SeparationInterval = 4;
    private int separationPhase;
    private Vector3 cachedSeparation;

    void ApplySeparation()
    {
        if ((Time.frameCount + separationPhase) % SeparationInterval == 0)
            cachedSeparation = ComputeSeparation();

        if (cachedSeparation != Vector3.zero)
            transform.position += cachedSeparation * Time.deltaTime * 8f;
    }

    private Vector3 ComputeSeparation()
    {
        Vector3 separation = Vector3.zero;
        int count = 0;

        float sepRadius = isRanged ? 1.0f : 1.2f;
        float sqrRadius = sepRadius * sepRadius;

        var units = BattleManager.Instance.GetAllUnits();
        for (int i = 0; i < units.Count; i++)
        {
            var ally = units[i];
            if (ally == null || ally == this || ally.isDead || ally.isEnemy != isEnemy) continue;

            // Без Vector2.Distance: корінь рахуємо лише для тих, хто справді поруч.
            Vector3 delta = transform.position - ally.transform.position;
            float sqr = delta.x * delta.x + delta.y * delta.y;
            if (sqr <= 0.000001f || sqr >= sqrRadius) continue;

            float dist = Mathf.Sqrt(sqr);
            separation += (delta / dist) * (sepRadius - dist);
            count++;
        }

        return count > 0 ? separation / count : Vector3.zero;
    }

    void FindTarget()
    {
        if (currentTarget != null && !currentTarget.isDead) return;

        float closestDist = Mathf.Infinity;
        CampaignUnit bestTarget = null;

        foreach (var u in BattleManager.Instance.GetAllUnits())
        {
            if (u != null && u.isEnemy != this.isEnemy && !u.isDead)
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
                if (u != null && u.isEnemy != this.isEnemy && !u.isDead)
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
    // Старі назви
    public void Hit() { DealDamage(); }
    public void ShootArrow() { DealDamage(); }
    public void Shoot() { DealDamage(); }

    // === ФІКС: НОВІ ДУБЛЕРИ ДЛЯ ТВОЇХ ЛИЦАРІВ І СПИСОНОСЦІВ ===
    public void Attack() { DealDamage(); }
    public void MeleeAttack() { DealDamage(); }
    public void Strike() { DealDamage(); }
    public void AttackAction() { DealDamage(); }

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
        if (bloodPrefab != null)
        {
            // Префаб крові не має ні скрипта, ні stopAction: Destroy — раніше
            // кожне влучання лишало мертвий об'єкт частинок у сцені назавжди.
            GameObject blood = PoolRegistry.Spawn(bloodPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            PoolRegistry.DespawnAfter(blood, BloodLifeTime);
        }

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

        // Виходимо зі списку живих до того, як об'єкт буде знищено.
        if (BattleManager.Instance != null) BattleManager.Instance.UnregisterUnit(this);
        
        if (anim) anim.enabled = false; 
        if (hpFillImage != null && hpFillImage.transform.parent != null) hpFillImage.transform.parent.gameObject.SetActive(false); 

        // Труп іде під живих одразу, щоб не перекривав бій.
        if (spriteRenderers != null)
            foreach (var sr in spriteRenderers) sr.sortingOrder = -10;

        StartCoroutine(DeathRoutine());
        Destroy(gameObject, 3.5f);
    }

    /// <summary>
    /// Падіння замість миттєвого повороту на 90°: юніт плавно завалюється,
    /// присідає до землі й темнішає, а в кінці життя трупа згасає.
    /// </summary>
    IEnumerator DeathRoutine()
    {
        const float fallDuration = 0.35f;
        const float holdDuration = 2.4f;
        const float fadeDuration = 0.7f;

        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(0, 0, nativelyFacesLeft ? 90f : -90f);
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + new Vector3(0f, -0.3f, 0f);

        Color liveTint = Color.white;
        Color deadTint = new Color(0.34f, 0.34f, 0.38f, 1f);

        float t = 0f;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fallDuration);

            // Падіння з легким прискоренням — вага відчувається краще за лінійне.
            float eased = k * k;
            transform.rotation = Quaternion.Slerp(startRot, endRot, eased);
            transform.position = Vector3.Lerp(startPos, endPos, eased);
            SetTint(Color.Lerp(liveTint, deadTint, k));

            yield return null;
        }

        transform.rotation = endRot;
        transform.position = endPos;
        SetTint(deadTint);

        yield return new WaitForSeconds(holdDuration);

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            Color c = deadTint;
            c.a = 1f - Mathf.Clamp01(t / fadeDuration);
            SetTint(c);
            yield return null;
        }
    }

    private void SetTint(Color c)
    {
        if (spriteRenderers == null) return;
        foreach (var sr in spriteRenderers) if (sr != null) sr.color = c;
    }
}