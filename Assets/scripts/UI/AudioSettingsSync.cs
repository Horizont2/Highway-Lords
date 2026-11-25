using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsSync : MonoBehaviour
{
    public Slider musicSlider;
    public Slider sfxSlider;

    void OnEnable()
    {
        if (musicSlider != null) 
        {
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
            musicSlider.onValueChanged.AddListener(val => { if (SoundManager.Instance) SoundManager.Instance.SetMusicVolume(val); });
        }
        if (sfxSlider != null) 
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxSlider.onValueChanged.AddListener(val => { if (SoundManager.Instance) SoundManager.Instance.SetSFXVolume(val); });
        }
    }

    void OnDisable()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveAllListeners();
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveAllListeners();
    }
}