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
    public AudioClip constructionSound; // Початок будівництва
    public AudioClip constructionComplete; // Завершення

    [Header("Події гри")]
    public AudioClip waveStart;     
    public AudioClip victory;       
    public AudioClip defeat;        

    // === Змінні гучності ===
    [HideInInspector] public float musicVolume = 0.5f;
    [HideInInspector] public float sfxVolume = 1f;

    void Awake()
    {
        // Сінлтон: гарантує, що існує лише один SoundManager
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
            return;
        }

        // Завантажуємо збережену гучність
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    void Start()
    {
        // Застосовуємо гучність до джерел
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

    // === ГОЛОВНИЙ МЕТОД ДЛЯ ЗВУКІВ ===
    // volumeScale = 1.0f (стандартна гучність). 
    // Якщо передати 0.5f, звук буде грати на 50% від загальної гучності.
    public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
    {
        if (sfxSource != null && clip != null)
        {
            // Формула: (Гучність конкретного звуку) * (Загальна гучність ефектів)
            sfxSource.PlayOneShot(clip, volumeScale * sfxVolume);
        }
    }

    // === БОНУС: МЕТОД ДЛЯ ВИПАДКОВОЇ ВИСОТИ ТОНУ (PITCH) ===
    // Це робить стрільбу та удари менш монотонними (звук трохи різний щоразу)
    public void PlaySFXRandomPitch(AudioClip clip, float volumeScale = 1.0f)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.pitch = Random.Range(0.85f, 1.15f); // Змінюємо висоту звуку
            sfxSource.PlayOneShot(clip, volumeScale * sfxVolume);
            sfxSource.pitch = 1.0f; // Повертаємо назад
        }
    }

    // === МЕТОДИ ДЛЯ НАЛАШТУВАНЬ ===
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