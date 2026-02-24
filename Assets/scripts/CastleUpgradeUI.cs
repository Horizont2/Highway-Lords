using UnityEngine;
using TMPro; // Потрібно для TextMeshPro
using UnityEngine.UI;

public class CastleUpgradeUI : MonoBehaviour
{
    [Header("Посилання")]
    public Castle castle;          // Перетягни сюди об'єкт Castle зі сцени
    public Button upgradeButton;   // Сама кнопка
    public TextMeshProUGUI costText; // Текст ціни на кнопці (або під нею)
    public TextMeshProUGUI levelText; // Текст "Level 1" (опціонально)

    void Start()
    {
        // Знаходимо замок автоматично, якщо забув перетягнути
        if (castle == null) castle = FindFirstObjectByType<Castle>();

        if (upgradeButton != null)
        {
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

        if (GameManager.Instance.gold >= cost)
        {
            // Списуємо гроші
            GameManager.Instance.AddResource(ResourceType.Gold, -cost); // Мінус золото
            
            // Покращуємо замок
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
            levelText.text = $"Castle Lvl {castle.castleLevel}";
    }
}