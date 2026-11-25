using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI Елементи")]
    public GameObject settingsPanel;
    public Slider musicSlider;
    public Slider sfxSlider;

    void Start()
    {
        // === 1. Ініціалізація гучності ===
        if (SoundManager.Instance != null)
        {
            musicSlider.value = SoundManager.Instance.musicVolume;
            sfxSlider.value = SoundManager.Instance.sfxVolume;
        }

        // Ховаємо меню на старті
        settingsPanel.SetActive(false);
    }

    // Прив'язуємо до Slider (Music)
    public void SetMusicVolume(float volume)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMusicVolume(volume);
        }
    }

    // Прив'язуємо до Slider (SFX)
    public void SetSFXVolume(float volume)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXVolume(volume);
        }
    }

    // Кнопка "Відкрити налаштування"
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        Time.timeScale = 0f; // Пауза гри
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
    }

    // Кнопка "Закрити/Назад"
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        Time.timeScale = 1f; // Відновлення гри
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);
    }

    // === НОВЕ: ВИХІД З ГРИ ===
    public void QuitGame()
    {
        // Звук кліку
        if (SoundManager.Instance != null) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.clickSound);

        Debug.Log("Вихід з гри..."); // Щоб бачити в консолі

        // Логіка виходу
        Application.Quit();

        // Цей блок дозволяє кнопці працювати навіть коли ти тестуєш гру в Unity Editor
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}