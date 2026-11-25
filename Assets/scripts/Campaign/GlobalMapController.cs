using UnityEngine;
using UnityEngine.UI;

public class GlobalMapController : MonoBehaviour
{
    [Header("Панель Карти")]
    public GameObject globalMapPanel; // Перетягни сюди свій GlobalMapPanel

    [Header("Кнопки")]
    public Button openMapButton;      // Кнопка на головному екрані бази
    public Button closeMapButton;     // Кнопка-хрестик на самій карті

    void Start()
    {
        // Ховаємо карту при старті гри, щоб гравець бачив базу
        if (globalMapPanel != null) 
            globalMapPanel.SetActive(false);

        // Підключаємо натискання кнопок
        if (openMapButton != null) 
            openMapButton.onClick.AddListener(OpenMap);
            
        if (closeMapButton != null) 
            closeMapButton.onClick.AddListener(CloseMap);
    }

    void OpenMap()
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        
        // Вмикаємо глобальну карту
        if (globalMapPanel != null) globalMapPanel.SetActive(true);
    }

    void CloseMap()
    {
        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
        
        // Вимикаємо глобальну карту
        if (globalMapPanel != null) globalMapPanel.SetActive(false);
    }
}