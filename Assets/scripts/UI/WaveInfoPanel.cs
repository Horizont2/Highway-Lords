using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WaveInfoPanel : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject iconPrefab;     // Префаб картинки (має бути Image)
    public Transform iconsContainer;  // Об'єкт з Horizontal Layout Group
    
    private void Awake()
    {
        // Ховаємо панель на старті
        Hide();
    }

    public void ShowWaveEnemies(List<Sprite> enemyIcons)
    {
        // 1. Очищаємо старі іконки перед створенням нових
        ClearIcons();

        // Перевірка на всяк випадок
        if (enemyIcons == null || enemyIcons.Count == 0)
        {
            // Якщо ворогів немає, просто ховаємо панель
            Hide();
            return; 
        }

        // 2. Створюємо нові іконки
        foreach (Sprite icon in enemyIcons)
        {
            if (icon == null) continue;

            // Створюємо об'єкт всередині контейнера
            GameObject newIcon = Instantiate(iconPrefab, iconsContainer);
            
            // === ВАЖЛИВИЙ ФІКС: Скидаємо масштаб ===
            // Unity UI часто створює об'єкти з Scale (0,0,0) або дивними координатами при Instantiate
            newIcon.transform.localScale = Vector3.one; 
            newIcon.transform.localPosition = Vector3.zero;
            
            // Призначаємо спрайт
            Image img = newIcon.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = icon;
                
                // (Опціонально) Зберігаємо пропорції картинки, щоб її не розтягувало
                // img.preserveAspect = true; 
            }
            
            // Вмикаємо об'єкт (на всяк випадок)
            newIcon.SetActive(true);
        }

        // 3. Показуємо саму панель
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        ClearIcons(); // Очищаємо при хованні
        gameObject.SetActive(false);
    }

    private void ClearIcons()
    {
        if (iconsContainer == null) return;

        foreach (Transform child in iconsContainer)
        {
            Destroy(child.gameObject);
        }
    }
}