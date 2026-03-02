using UnityEngine;
using TMPro; // Потрібно для TextMeshPro
using UnityEngine.UI;

public class CastleUpgradeUI : MonoBehaviour
{
    [Header("Посилання")]
    public Wall castle;          // Перетягни сюди об'єкт Wall зі сцени (змінна називається castle для сумісності)
    public Button upgradeButton;   // Сама кнопка
    public TextMeshProUGUI costText; // Текст ціни на кнопці (або під нею)
    public TextMeshProUGUI levelText; // Текст "Level 1" (опціонально)
    public TextMeshProUGUI statsText; // Текст HP: current → next

    void Start()
    {
        // ЗМІНЕНО: Тепер шукаємо Wall замість Castle
        if (castle == null) castle = FindFirstObjectByType<Wall>();

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(TryUpgrade);
        }

        UpdateUI();
    }

    void Update()
    {
        // Оновлюємо колір кнопки (активна/неактивна), якщо вистачає грошей
        if (castle != null && GameManager.Instance != null && upgradeButton != null)
        {
            int cost = castle.GetUpgradeCost();
            bool canAfford = GameManager.Instance.gold >= cost;
            upgradeButton.interactable = canAfford;
        }
    }

    void TryUpgrade()
    {
        if (castle == null || GameManager.Instance == null) return;

        int cost = castle.GetUpgradeCost();

        if (cost <= 0) return;

        if (GameManager.Instance.gold >= cost)
        {
            // Списуємо гроші
            GameManager.Instance.gold -= cost;
            GameManager.Instance.UpdateUI();
            
            // Покращуємо стіну
            castle.UpgradeCastle();

            // Оновлюємо текст
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        if (castle == null) return;

        int cost = castle.GetUpgradeCost();

        if (costText != null) 
            costText.text = $"{cost} Gold";

        if (levelText != null)
            // ЗМІНЕНО: Текст і змінна тепер вказують на wallLevel
            levelText.text = $"Wall Lvl {castle.wallLevel}";

        if (statsText != null)
        {
            int currentHp = castle.maxHealth;
            int nextHp    = castle.maxHealth + castle.hpBonusPerUpgrade;
            statsText.text = $"HP: {currentHp} → {nextHp}";
        }
    }
}