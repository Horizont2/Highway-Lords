using UnityEngine;
using UnityEngine.UI;

public class HornController : MonoBehaviour
{
    [Header("UI Об'єкти")]
    public Image fillRing;      // Золоте кільце (Image Type: Filled!)
    public Button hornBtn;      // Кнопка
    public GameObject lockIcon; // Замок

    [Header("Налаштування")]
    public int unlockWave = 2;       // Коли відкривати
    public float cooldown = 15.0f;   // Час перезарядки
    public float buffDuration = 10f; // Час дії
    public bool debugUnlock = false; // Галочка для тесту

    [Header("Звук Горну")]
    public AudioClip hornSound;      // <--- ПОВЕРНУЛИ ЗВУК

    // Внутрішні стани
    private bool isUnlocked = false;
    private bool isBuffActive = false;
    private float timer = 0f;

    void Start()
    {
        // Налаштовуємо кнопку
        if (hornBtn)
        {
            hornBtn.onClick.RemoveAllListeners();
            hornBtn.onClick.AddListener(ActivateHorn);
        }

        // Перевіряємо старт
        CheckUnlockCondition();
        
        // Якщо відкрили через Debug - робимо готовим одразу
        if (isUnlocked)
        {
            timer = cooldown;
            UpdateRingVisual(1f);
        }
    }

    void Update()
    {
        // 1. ПЕРЕВІРКА ВІДКРИТТЯ
        if (!isUnlocked)
        {
            CheckUnlockCondition();
            
            if (!isUnlocked)
            {
                if (lockIcon) lockIcon.SetActive(true);
                if (fillRing) fillRing.fillAmount = 0;
                if (hornBtn) hornBtn.interactable = false;
                return;
            }
        }

        // 2. ЯКЩО ВІДКРИТО
        if (lockIcon) lockIcon.SetActive(false);

        // СТАН 1: БАФ АКТИВНИЙ (Кільце зникає)
        if (isBuffActive)
        {
            timer -= Time.deltaTime;
            UpdateRingVisual(timer / buffDuration);

            if (timer <= 0)
            {
                EndBuff();
            }
        }
        // СТАН 2: ПЕРЕЗАРЯДКА (Кільце росте)
        else if (timer < cooldown)
        {
            timer += Time.deltaTime;
            UpdateRingVisual(timer / cooldown);
            if (hornBtn) hornBtn.interactable = false;
        }
        // СТАН 3: ГОТОВО (Кільце повне)
        else
        {
            UpdateRingVisual(1f);
            if (hornBtn) hornBtn.interactable = true;
        }
    }

    void ActivateHorn()
    {
        if (timer < cooldown || isBuffActive) return;

        Debug.Log("Horn Activated!");
        isBuffActive = true;
        timer = buffDuration;

        // ВІДТВОРЕННЯ ЗВУКУ ГОРНУ
        if (SoundManager.Instance != null && hornSound != null)
        {
            SoundManager.Instance.PlaySFX(hornSound);
        }
        // Запасний звук кліку, якщо горн не призначений
        else if (SoundManager.Instance != null && SoundManager.Instance.clickSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        }

        // Логіка бафу
        if (GameManager.Instance) 
        {
            GameManager.Instance.globalDamageMultiplier = 1.2f;
            GameManager.Instance.UpdateUI();
        }
    }

    void EndBuff()
    {
        Debug.Log("Horn Cooldown Started");
        isBuffActive = false;
        timer = 0;

        if (GameManager.Instance) 
        {
            GameManager.Instance.globalDamageMultiplier = 1.0f;
            GameManager.Instance.UpdateUI();
        }
    }

    void UpdateRingVisual(float amount)
    {
        if (fillRing) fillRing.fillAmount = Mathf.Clamp01(amount);
    }

    void CheckUnlockCondition()
    {
        if (debugUnlock || (GameManager.Instance && GameManager.Instance.currentWave >= unlockWave))
        {
            isUnlocked = true;
        }
    }
}