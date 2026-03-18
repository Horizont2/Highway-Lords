using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CallLegionCooldownManager : MonoBehaviour
{
    [Header("Кнопка та Затемнення")]
    public Button callSkillButton;
    public Image cooldownOverlay; 
    public TMP_Text waveCounterText;

    [Header("Налаштування")]
    public float cooldownTime = 15f; 
    private float currentCooldownTimer;
    private bool isSkillOnCooldown = false;

    private int totalLegions = 1; 
    private int legionsUsed = 1; 

    [Header("Анімація підстрибування")]
    public float bounceSpeed = 4f;     
    public float bounceAmount = 0.05f; 
    private Vector3 baseScale;

    private void Start()
    {
        baseScale = callSkillButton.transform.localScale;
        isSkillOnCooldown = false;
        if(cooldownOverlay) cooldownOverlay.fillAmount = 0f;
        callSkillButton.gameObject.SetActive(false); // Ховаємо на старті
    }

    private void Update()
    {
        if (BattleManager.Instance == null) return;

        // КНОПКА З'ЯВЛЯЄТЬСЯ ТІЛЬКИ КОЛИ ПОЧАВСЯ БІЙ І Є РЕЗЕРВ
        totalLegions = BattleManager.Instance.maxLegions;
        if (BattleManager.Instance.currentState != BattleManager.BattleState.Battle || totalLegions <= 1)
        {
            if (callSkillButton.gameObject.activeSelf) callSkillButton.gameObject.SetActive(false);
            return;
        }

        // Вмикаємо кнопку, якщо йде бій
        if (!callSkillButton.gameObject.activeSelf) 
        {
            callSkillButton.gameObject.SetActive(true);
            UpdateWaveText();
        }

        if (isSkillOnCooldown)
        {
            currentCooldownTimer -= Time.deltaTime;
            if(cooldownOverlay) cooldownOverlay.fillAmount = currentCooldownTimer / cooldownTime;

            if (currentCooldownTimer <= 0) EndCooldown();
        }
        else if (legionsUsed < totalLegions)
        {
            float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceAmount;
            callSkillButton.transform.localScale = baseScale + new Vector3(bounce, bounce, 0f);
        }
        else
        {
            callSkillButton.transform.localScale = baseScale;
        }
    }

    public void CallLegion()
    {
        if (!isSkillOnCooldown && legionsUsed < totalLegions)
        {
            legionsUsed++;
            if (BattleManager.Instance != null) BattleManager.Instance.SpawnNextLegion();
            UpdateWaveText();
            
            if (legionsUsed < totalLegions) StartCooldown();
            else
            {
                callSkillButton.interactable = false;
                if(cooldownOverlay) cooldownOverlay.fillAmount = 1f; 
                callSkillButton.transform.localScale = baseScale;
            }
        }
    }

    private void StartCooldown()
    {
        isSkillOnCooldown = true;
        currentCooldownTimer = cooldownTime;
        if(cooldownOverlay) cooldownOverlay.fillAmount = 1f; 
        callSkillButton.interactable = false; 
        if (waveCounterText != null) waveCounterText.color = new Color(0.7f, 0.7f, 0.7f, 1f); 
        callSkillButton.transform.localScale = baseScale; 
    }

    private void EndCooldown()
    {
        isSkillOnCooldown = false;
        if(cooldownOverlay) cooldownOverlay.fillAmount = 0f; 
        callSkillButton.interactable = true; 
        if (waveCounterText != null) waveCounterText.color = Color.white;
    }

    private void UpdateWaveText()
    {
        if (waveCounterText != null)
        {
            int remainingLegions = totalLegions - legionsUsed;
            waveCounterText.text = remainingLegions.ToString(); 
        }
    }
}