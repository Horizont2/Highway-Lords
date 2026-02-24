using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

public class SkillManager : MonoBehaviour
{
    [Header("Налаштування")]
    public GameObject rainArrowPrefab; 
    public GameObject aimingReticle;   
    public Button skillButton;         
    public Image cooldownOverlay;      
    public GameObject lockIcon;        

    [Header("Візуал Кнопки")]
    public Color normalColor = Color.white;      // Звичайний колір
    public Color selectedColor = new Color(0.6f, 0.6f, 0.6f, 1f); // Затемнений (коли цілимось)

    [Header("Баланс")]
    public int unlockWave = 30;        
    public float cooldownTime = 15f;   
    public int arrowsCount = 20;       
    public float radius = 3.0f;        

    private bool isUnlocked = false;
    private bool isAiming = false;
    private bool isCooldown = false;

    private Plane groundPlane = new Plane(Vector3.forward, Vector3.zero);

    void Start()
    {
        if (aimingReticle) aimingReticle.SetActive(false);
        
        // Налаштовуємо кнопку
        if (skillButton) 
        {
            skillButton.onClick.AddListener(OnSkillButtonClick);
            skillButton.image.color = normalColor; // Старт з нормальним кольором
        }
        
        CheckUnlock();
    }

    void Update()
    {
        CheckUnlock();

        if (isAiming)
        {
            if (Mouse.current == null) return;
            if (Camera.main == null) return;

            // Логіка променя (Raycast)
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            float enterDist;
            Vector3 worldPos = Vector3.zero;

            if (groundPlane.Raycast(ray, out enterDist))
            {
                worldPos = ray.GetPoint(enterDist);
            }

            // Рухаємо приціл
            if (aimingReticle) 
            {
                SpriteRenderer sr = aimingReticle.GetComponent<SpriteRenderer>();
                if(sr) sr.sortingOrder = 2000; 

                aimingReticle.transform.position = new Vector3(worldPos.x, worldPos.y, -2f);
                if (!aimingReticle.activeSelf) aimingReticle.SetActive(true);
            }

            // ЛКМ - Стріляємо
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    StartCoroutine(CastArrowRain(worldPos));
                    StopAiming(); // Стрільнули - вимикаємо приціл
                }
            }
            // ПКМ - Скасування
            else if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                StopAiming();
            }
        }
    }

    // === ЛОГІКА КНОПКИ (УВІМКНУТИ / ВИМКНУТИ) ===
    public void OnSkillButtonClick()
    {
        if (!isUnlocked || isCooldown) return;

        // ЯКЩО МИ ВЖЕ ЦІЛИМОСЬ -> СКАСУВАТИ
        if (isAiming)
        {
            StopAiming();
        }
        // ЯКЩО НЕ ЦІЛИМОСЬ -> ПОЧАТИ
        else
        {
            StartAiming();
        }
    }

    void StartAiming()
    {
        isAiming = true;
        if (aimingReticle) aimingReticle.SetActive(true);
        
        // Змінюємо колір кнопки на темніший
        if (skillButton) skillButton.image.color = selectedColor;
    }

    void StopAiming()
    {
        isAiming = false;
        if (aimingReticle) aimingReticle.SetActive(false);

        // Повертаємо колір кнопки назад
        if (skillButton) skillButton.image.color = normalColor;
    }

    // ... (CheckUnlock без змін) ...
    void CheckUnlock()
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.currentWave >= unlockWave)
            {
                if (!isUnlocked)
                {
                    isUnlocked = true;
                    if (skillButton) skillButton.interactable = !isCooldown;
                    if (lockIcon != null) lockIcon.SetActive(false);
                }
            }
            else
            {
                if (isUnlocked)
                {
                    isUnlocked = false;
                    if (skillButton) skillButton.interactable = false;
                    if (lockIcon != null) lockIcon.SetActive(true);
                }
            }
        }
    }

    IEnumerator CastArrowRain(Vector3 targetPos)
    {
        StartCoroutine(CooldownRoutine());
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot); 

        for (int i = 0; i < arrowsCount; i++)
        {
            float offsetX = Random.Range(-radius, radius);
            Vector3 spawnPos = new Vector3(targetPos.x + offsetX - 3f, targetPos.y + 12f, -2f);
            Vector3 hitPos = new Vector3(targetPos.x + offsetX, targetPos.y, -2f);

            GameObject arrow = Instantiate(rainArrowPrefab, spawnPos, Quaternion.identity);
            
            SpriteRenderer sr = arrow.GetComponent<SpriteRenderer>();
            if(sr != null) sr.sortingOrder = 2001;

            Vector3 direction = (hitPos - spawnPos).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            arrow.transform.rotation = Quaternion.Euler(0, 0, angle);

            yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));
        }
    }

    IEnumerator CooldownRoutine()
    {
        isCooldown = true;
        skillButton.interactable = false;
        
        // Повертаємо колір на білий, бо почався кулдаун
        if (skillButton) skillButton.image.color = normalColor; 

        float timer = cooldownTime;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            if (cooldownOverlay) cooldownOverlay.fillAmount = timer / cooldownTime;
            yield return null;
        }
        isCooldown = false;
        if (cooldownOverlay) cooldownOverlay.fillAmount = 0;
        skillButton.interactable = true;
    }
}