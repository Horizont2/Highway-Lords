using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TavernManager : MonoBehaviour
{
    public static TavernManager Instance { get; private set; }

    [Header("Головна Панель")]
    public GameObject tavernPanel;         
    public Transform tavernContentPanel;   
    public TMP_Text playerGloryText;       

    [System.Serializable]
    public class MercenarySlot
    {
        [Header("Налаштування Загону")]
        public string mercName;       
        public string unitClass;      
        [Tooltip("Більше не використовується. Сила розраховується динамічно!")]
        public int unitPower;         
        public int costGlory;         
        public int maxBattles;        
        
        [Header("UI Елементи")]
        public TMP_Text powerText;    
        public TMP_Text battlesText;  
        public TMP_Text costText;     
        public Button hireButton;     
        public CanvasGroup btnCanvasGroup; 
    }

    [Header("Слоти Найманців")]
    public MercenarySlot[] mercSlots = new MercenarySlot[4]; 

    private int currentGlory;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (!PlayerPrefs.HasKey("PlayerGlory")) 
            PlayerPrefs.SetInt("PlayerGlory", 2000); 

        LoadData();

        for (int i = 0; i < mercSlots.Length; i++)
        {
            int index = i; 
            if (mercSlots[i].hireButton != null)
            {
                mercSlots[i].hireButton.onClick.RemoveAllListeners();
                mercSlots[i].hireButton.onClick.AddListener(() => HireMercenary(index));
            }
        }

        // АВТО-ЗАПУСК: Відкриваємо таверну безпечно
        OpenTavern();
    }

    public void OpenTavern()
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        LoadData();
        UpdateUI();
        
        if (tavernPanel) 
        {
            tavernPanel.SetActive(true); 
            Transform targetPanel = tavernContentPanel != null ? tavernContentPanel : tavernPanel.transform;
            
            if (gameObject.activeInHierarchy) {
                StartCoroutine(AnimatePanel(targetPanel, true)); 
            } else {
                targetPanel.localScale = Vector3.one; 
            }
        }
    }

    public void CloseTavern()
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        
        if (tavernPanel) 
        {
            Transform targetPanel = tavernContentPanel != null ? tavernContentPanel : tavernPanel.transform;
            
            if (gameObject.activeInHierarchy) {
                StartCoroutine(AnimatePanel(targetPanel, false)); 
            } else {
                tavernPanel.SetActive(false);
            }
        }
    }

    private IEnumerator AnimatePanel(Transform panel, bool show)
    {
        float duration = 0.25f;
        float time = 0f;

        Vector3 startScale = show ? new Vector3(0.8f, 0.8f, 1f) : Vector3.one;
        Vector3 endScale = show ? Vector3.one : new Vector3(0.8f, 0.8f, 1f);

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.gameObject.AddComponent<CanvasGroup>();

        float startAlpha = show ? 0f : 1f;
        float endAlpha = show ? 1f : 0f;

        if (show) panel.localScale = startScale;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime; 
            float t = time / duration;
            float ease = show ? (t * t * (3f - 2f * t)) : (t * t); 

            panel.localScale = Vector3.Lerp(startScale, endScale, ease);
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            
            yield return null;
        }

        panel.localScale = endScale;
        cg.alpha = endAlpha;

        if (!show && tavernPanel != null) tavernPanel.SetActive(false); 
    }

    void LoadData()
    {
        currentGlory = PlayerPrefs.GetInt("PlayerGlory", 0);
    }

    public void UpdateUI()
    {
        if (playerGloryText) playerGloryText.text = currentGlory.ToString();

        // ФІКС: Отримуємо рівень міста (замку) гравця
        int cityLevel = PlayerPrefs.GetInt("SavedCastleLevel", 1);
        // ФІКС: Розраховуємо силу за "розумною" формулою
        int dynamicPower = 120 + (cityLevel * 15);

        for (int i = 0; i < mercSlots.Length; i++)
        {
            MercenarySlot slot = mercSlots[i];
            
            // Встановлюємо правильну динамічну силу (замість 500)
            if (slot.powerText) slot.powerText.text = $"Power: {dynamicPower}";

            int battlesLeft = PlayerPrefs.GetInt("Merc_" + slot.unitClass + "_Battles", 0);

            if (battlesLeft > 0)
            {
                if (slot.battlesText) slot.battlesText.text = $"Battles: {battlesLeft}/{slot.maxBattles}";
                if (slot.costText) slot.costText.text = "HIRED!";
                
                slot.hireButton.interactable = false; 
                if (slot.btnCanvasGroup) slot.btnCanvasGroup.alpha = 0.4f; 
            }
            else
            {
                if (slot.battlesText) slot.battlesText.text = $"Battles: 0/{slot.maxBattles}";
                if (slot.costText) slot.costText.text = $"Cost: {slot.costGlory}";

                bool canAfford = currentGlory >= slot.costGlory;
                slot.hireButton.interactable = canAfford;
                if (slot.btnCanvasGroup) slot.btnCanvasGroup.alpha = canAfford ? 1f : 0.4f;
            }
        }
    }

    public void HireMercenary(int index)
    {
        MercenarySlot slot = mercSlots[index];

        if (currentGlory >= slot.costGlory)
        {
            currentGlory -= slot.costGlory;
            PlayerPrefs.SetInt("PlayerGlory", currentGlory);

            PlayerPrefs.SetInt("Merc_" + slot.unitClass + "_Battles", slot.maxBattles);
            PlayerPrefs.Save();

            if (SoundManager.Instance) 
            {
                if (SoundManager.Instance.unitUpgradeSound != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);
                else SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
            }

            if (GameManager.Instance != null) GameManager.Instance.UpdateResourcesUI();

            UpdateUI();
        }
    }
}