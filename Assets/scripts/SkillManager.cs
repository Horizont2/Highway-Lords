using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class SkillManager : MonoBehaviour
{
    [Header("Налаштування")]
    public GameObject rainArrowPrefab; 
    public GameObject aimingReticle;   
    public Button skillButton;         
    public Image cooldownOverlay;      
    public GameObject lockIcon;        

    [Header("Візуал Кнопки")]
    public Color normalColor = Color.white;      
    public Color selectedColor = new Color(0.6f, 0.6f, 0.6f, 1f); 

    [Header("Баланс")]
    public int unlockWave = 30;        
    public float cooldownTime = 15f;   
    public int baseArrowsCount = 20;       
    public float radius = 3.0f;        

    private bool isUnlocked = false;
    private bool isAiming = false;
    private bool isCooldown = false;
    private Camera mainCam;
    private GameObject currentReticleInstance; 

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null) mainCam = FindFirstObjectByType<Camera>();

        if (aimingReticle != null) 
        {
            currentReticleInstance = Instantiate(aimingReticle);
            currentReticleInstance.transform.SetParent(null);
            
            float s = radius * 2f;
            currentReticleInstance.transform.localScale = new Vector3(s, s, 1f);
            currentReticleInstance.SetActive(false);
        }
        
        if (skillButton) 
        {
            skillButton.onClick.AddListener(OnSkillButtonClick);
            skillButton.image.color = normalColor; 
        }
        
        CheckUnlock();
    }

    void Update()
    {
        CheckUnlock();

        if (isAiming)
        {
            if (mainCam == null) mainCam = FindFirstObjectByType<Camera>();
            if (mainCam == null) return;

            Vector3 rawMousePos = Input.mousePosition;
            Vector2 mouseWorld2D = mainCam.ScreenToWorldPoint(rawMousePos);
            Vector3 finalReticlePos = new Vector3(mouseWorld2D.x, mouseWorld2D.y, -5f);

            if (currentReticleInstance != null)
            {
                currentReticleInstance.transform.position = finalReticlePos;
                if (!currentReticleInstance.activeSelf) currentReticleInstance.SetActive(true);
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && !EventSystem.current.IsPointerOverGameObject())
                {
                    StartCoroutine(CastArrowRain(finalReticlePos));
                    StopAiming();
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                StopAiming();
            }
        }
    }

    public void OnSkillButtonClick()
    {
        if (!isUnlocked || isCooldown) return;
        if (isAiming) StopAiming();
        else StartAiming();
    }

    void StartAiming()
    {
        isAiming = true;
        if (currentReticleInstance != null) currentReticleInstance.SetActive(true);
        if (skillButton) skillButton.image.color = selectedColor;
    }

    void StopAiming()
    {
        isAiming = false;
        if (currentReticleInstance != null) currentReticleInstance.SetActive(false);
        if (skillButton && !isCooldown) skillButton.image.color = normalColor;
    }

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
        else { isUnlocked = true; } 
    }

    IEnumerator CastArrowRain(Vector3 targetPos)
    {
        StartCoroutine(CooldownRoutine());
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot); 

        int totalArrows = baseArrowsCount;
        
        if (GameManager.Instance != null)
        {
            // БОНУС ДО КІЛЬКОСТІ СТРІЛ УВІМКНЕНО!
            totalArrows += GameManager.Instance.metaVolleyBarrage;
        }

        for (int i = 0; i < totalArrows; i++)
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

            yield return new WaitForSeconds(Random.Range(0.04f, 0.08f));
        }
    }

    IEnumerator CooldownRoutine()
    {
        isCooldown = true;
        if (skillButton) skillButton.interactable = false;
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
        
        if (isUnlocked && skillButton) skillButton.interactable = true;
    }
}