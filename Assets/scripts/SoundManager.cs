using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Налаштування Джерел")]
    public AudioSource musicSource; 
    public AudioSource sfxSource;   

    [Header("Музика")]
    public AudioClip backgroundMusic;

    [Header("Атака та Бій")]
    public AudioClip arrowShoot;    
    public AudioClip arrowHit;      
    public AudioClip swordHit;      
    public AudioClip enemyDeath;    
    public AudioClip knightHit;     
    // === ДОДАНО ДЛЯ СЛОНА ===
    public AudioClip heavyHitSound; 

    [Header("Руйнування")]
    public AudioClip cartBreak;     
    public AudioClip castleDamage;  
    public AudioClip woodBreak; 

    [Header("Економіка та UI")]
    public AudioClip coinPickup;    
    public AudioClip buyItem;       
    public AudioClip error;
    public AudioClip clickSound;
    public AudioClip unitUpgradeSound; 

    [Header("Будівництво")]
    public AudioClip constructionSound; 
    public AudioClip constructionComplete; 

    [Header("Події гри")]
    public AudioClip waveStart;     
    public AudioClip victory;       
    public AudioClip defeat;        

    [HideInInspector] public float musicVolume = 0.5f;
    [HideInInspector] public float sfxVolume = 1f;

    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
            return;
        }

        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    void Start()
    {
        if (musicSource != null) musicSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        
        PlayMusic();
    }

    public void PlayMusic()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale * sfxVolume);
        }
    }

    public void PlaySFXRandomPitch(AudioClip clip, float volumeScale = 1.0f)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.pitch = Random.Range(0.85f, 1.15f); 
            sfxSource.PlayOneShot(clip, volumeScale * sfxVolume);
            sfxSource.pitch = 1.0f; 
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        if (musicSource != null) musicSource.volume = musicVolume;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }
}