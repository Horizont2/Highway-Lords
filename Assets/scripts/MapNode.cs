using UnityEngine;
using UnityEngine.UI;

public class MapNode : MonoBehaviour
{
    [Header("Налаштування Поселення")]
    public int campId; 
    public string campName = "Barbarian Camp";
    public int campLevel = 1;
    public int enemyPowerScore = 1500; 

    [Header("Нагороди за захоплення")]
    public int rewardGold = 1000;
    public int rewardWood = 200;
    public int rewardStone = 50;

    [Header("Статус")]
    public bool isCaptured = false;
    public GameObject capturedIcon; // Прапорець
    
    // Склад ворожої армії (генерується автоматично)
    [HideInInspector] public int e_guards, e_archers, e_spearmen, e_cavalry;

    private Button btn;

    void Awake()
    {
        // Гарнізон потрібен уже на старті: MapMarkerVisuals і панель розвідки
        // читають його у своїх Start().
        GenerateEnemyArmy();
    }

    void Start()
    {
        btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(OnNodeClicked);
    }

    /// <summary>
    /// Склад гарнізону. Детермінований — той самий табір завжди має ту саму
    /// армію. Раніше тут був Random у Start(), тож розвідка показувала новий
    /// склад при кожному заході на мапу.
    /// </summary>
    void GenerateEnemyArmy()
    {
        CampaignBalance.GenerateGarrison(campId, campLevel, enemyPowerScore,
            out e_guards, out e_archers, out e_spearmen, out e_cavalry);
    }

    /// <summary>Сумарна кількість захисників — рівно стільки й буде в бою.</summary>
    public int TotalGarrison => e_guards + e_archers + e_spearmen + e_cavalry;

    /// <summary>Сила гарнізону за тією самою формулою, що й сила армії гравця.</summary>
    public int GarrisonPower =>
        CampaignBalance.GarrisonPower(e_guards, e_archers, e_spearmen, e_cavalry, campLevel);

    public void LoadState(bool captured)
    {
        isCaptured = captured;
        if (capturedIcon != null) capturedIcon.SetActive(isCaptured);
        if (btn != null) btn.interactable = !isCaptured; 
    }

    void OnNodeClicked()
    {
        if (isCaptured) return;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        
        // Відкриваємо панель і передаємо дані цього міста
        if (CampaignManager.Instance != null) CampaignManager.Instance.OpenPanel(this);
    }
}