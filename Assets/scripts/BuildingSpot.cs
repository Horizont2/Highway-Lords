using UnityEngine;

public class BuildingSpot : MonoBehaviour
{
    [Header("Що будуємо")]
    public GameObject barracksPrefab; // Префаб готової казарми
    public int buildCostGold = 200;
    public int buildCostWood = 100;

    [Header("UI Будівництва")]
    public GameObject buildButtonUI; // Кнопка "Побудувати" над фундаментом

    private GameObject currentBuilding; // Посилання на збудовану будівлю

    void Start()
    {
        if (buildButtonUI) buildButtonUI.SetActive(false);
    }

    // Клік по фундаменту
    void OnMouseDown()
    {
        if (currentBuilding == null)
        {
            // Якщо пусто - показуємо кнопку "Будувати"
            if (buildButtonUI) buildButtonUI.SetActive(!buildButtonUI.activeSelf);
        }
    }

    // Цей метод вішаємо на кнопку "Build Barracks" в UI цього слота
    public void BuildBarracks()
    {
        if (GameManager.Instance.gold >= buildCostGold && GameManager.Instance.wood >= buildCostWood)
        {
            // Списання ресурсів
            GameManager.Instance.gold -= buildCostGold;
            GameManager.Instance.wood -= buildCostWood;

            // Будівництво
            currentBuilding = Instantiate(barracksPrefab, transform.position, Quaternion.identity);
            
            // Ховаємо меню будівництва
            if (buildButtonUI) buildButtonUI.SetActive(false);
            
            // Ховаємо спрайт самого фундаменту (опціонально)
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<BoxCollider2D>().enabled = false; // Щоб більше не клікати на фундамент

            GameManager.Instance.UpdateUI();
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.buyItem);
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.error);
            Debug.Log("Не вистачає ресурсів!");
        }
    }
}