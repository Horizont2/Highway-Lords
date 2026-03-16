using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapMarkerVisuals : MonoBehaviour
{
    private MapNode nodeData; 
    private Button townButton; 

    [Header("Візуальні елементи")]
    public Image iconImage;         
    public TMP_Text levelText;      

    [Header("Твої Спрайти")]
    public Sprite swordsSprite;     
    public Sprite flagSprite;       
    public Sprite skullSprite;      

    void Start()
    {
        nodeData = GetComponent<MapNode>();
        townButton = GetComponent<Button>();

        UpdateMarkerState();
    }

    public void UpdateMarkerState()
    {
        if (nodeData == null || iconImage == null) return;

        int myId = nodeData.campId;

        // Чи захоплене саме ЦЕ місто?
        bool isConquered = PlayerPrefs.GetInt("Camp_" + myId + "_Conquered", 0) == 1;
        
        // Чи відкрите воно для атаки?
        bool isUnlocked = false;
        if (myId == 1) 
        {
            isUnlocked = true; 
        }
        else 
        {
            isUnlocked = PlayerPrefs.GetInt("Camp_" + (myId - 1) + "_Conquered", 0) == 1;
        }

        // 1. БЛОКУВАННЯ КНОПКИ
        if (townButton != null) 
        {
            // Кнопка активна ТІЛЬКИ якщо місто відкрите І ЩЕ НЕ захоплене
            townButton.interactable = isUnlocked && !isConquered;
        }

        // Шукаємо скрипт анімації польоту на нашій іконці
        FloatingIcon floater = iconImage.GetComponent<FloatingIcon>();

        // 2. ВІЗУАЛ ТА АНІМАЦІЇ
        if (isConquered)
        {
            // МІСТО ПРОЙДЕНО
            iconImage.sprite = flagSprite; 
            iconImage.color = Color.white;
            
            if (floater != null) floater.enabled = false; // Прапор стоїть рівно
            SetLevelText();
        }
        else if (isUnlocked)
        {
            // МІСТО ВІДКРИТЕ ДЛЯ АТАКИ
            iconImage.sprite = (nodeData.campLevel >= 10) ? skullSprite : swordsSprite;
            iconImage.color = Color.white;
            
            if (floater != null) floater.enabled = true; // Активна ціль стрибає!
            SetLevelText();
        }
        else
        {
            // МІСТО ЗАБЛОКОВАНЕ
            iconImage.sprite = swordsSprite; 
            iconImage.color = new Color(0.3f, 0.3f, 0.3f, 1f); 
            
            if (floater != null) floater.enabled = false; // Заблоковані іконки не рухаються
            
            if (levelText != null) 
            {
                levelText.text = "???";
                levelText.color = Color.gray;
            }
        }
    }

    void SetLevelText()
    {
        if (levelText != null && nodeData != null)
        {
            levelText.text = "LVL " + nodeData.campLevel;

            float currentLvl = (float)nodeData.campLevel;
            float maxRedLevel = 10f; 

            float difficultyPercent = Mathf.Clamp01((currentLvl - 1f) / (maxRedLevel - 1f));

            float hue = Mathf.Lerp(0.27f, 0f, difficultyPercent);
            Color finalColor = Color.HSVToRGB(hue, 1f, 0.9f);
            finalColor.a = 1f; 

            levelText.color = finalColor;
        }
    }
}