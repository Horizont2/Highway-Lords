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

    [Header("Руйнування")]
    public AudioClip cartBreak;     
    public AudioClip castleDamage;  
    public AudioClip woodBreak; 

    [Header("Економіка та UI")]
    public AudioClip coinPickup;    
    public AudioClip buyItem;       
    public AudioClip error;
    public AudioClip clickSound;    

    [Header("Будівництво")]
    public AudioClip constructionSound;

    [Header("Події гри")]
    public AudioClip waveStart;     
    public AudioClip victory;       
    public AudioClip defeat;        

    // === НОВЕ: Змінні гучності ===
    [HideInInspector] public float musicVolume = 0.5f;
    [HideInInspector] public float sfxVolume = 1f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Завантажуємо збережену гучність
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    void Start()
    {
        // Застосовуємо гучність до джерел
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
        
        PlayMusic();
    }

    public void PlayMusic()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = musicVolume; // Використовуємо змінну
            musicSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (sfxSource != null && clip != null)
        {
            // Множимо гучність ефекту на загальну гучність SFX
            sfxSource.PlayOneShot(clip, volumeScale * sfxVolume);
        }
    }

    // === НОВІ МЕТОДИ ДЛЯ НАЛАШТУВАНЬ ===
    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        musicSource.volume = musicVolume;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        sfxSource.volume = sfxVolume; // Це вплине тільки на наступні PlayOneShot, або якщо sfxSource щось грає
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }
}