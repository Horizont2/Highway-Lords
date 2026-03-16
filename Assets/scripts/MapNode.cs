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

    void Start()
    {
        btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(OnNodeClicked);

        GenerateEnemyArmy(); // Автоматично створюємо армію для розвідки
    }

    void GenerateEnemyArmy()
    {
        int powerLeft = enemyPowerScore;
        e_guards = 0; e_archers = 0; e_spearmen = 0; e_cavalry = 0;

        // Зменшив вартість сили, щоб генерувалося більше юнітів
        int p_guard = 15, p_archer = 20, p_spear = 30, p_cav = 50;

        while (powerLeft >= p_guard)
        {
            if (campLevel >= 8 && powerLeft >= p_cav && Random.value > 0.7f) { e_cavalry++; powerLeft -= p_cav; }
            else if (campLevel >= 4 && powerLeft >= p_spear && Random.value > 0.6f) { e_spearmen++; powerLeft -= p_spear; }
            else if (campLevel >= 2 && powerLeft >= p_archer && Random.value > 0.5f) { e_archers++; powerLeft -= p_archer; }
            else { e_guards++; powerLeft -= p_guard; }
        }
        
        // Гарантований мінімум для вигляду (навіть якщо сила дуже мала)
        if (e_guards + e_archers + e_spearmen + e_cavalry == 0) e_guards = 3;
    }

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