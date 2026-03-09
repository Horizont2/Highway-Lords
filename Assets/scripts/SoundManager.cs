using UnityEngine;
using System.Collections; // Важливо для роботи корутин (Fade)

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Налаштування Джерел")]
    public AudioSource musicSource; 
    public AudioSource sfxSource;   
    [Tooltip("Джерело для зациклених фонових звуків (дощ, вітер)")]
    public AudioSource ambientSource; 

    [Header("Музика")]
    public AudioClip backgroundMusic;

    [Header("Амбієнт (Погода)")]
    public AudioClip rainSound; 
    public AudioClip thunderSound; // <--- ЗВУК ГРОМУ

    [Header("Атака та Бій")]
    public AudioClip arrowShoot;    
    public AudioClip arrowHit;      
    public AudioClip swordHit;      
    public AudioClip enemyDeath;    
    public AudioClip knightHit;     
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

    private Coroutine ambientFadeCoroutine; 

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

        // Завантажуємо налаштування гучності
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    void Start()
    {
        if (musicSource != null) musicSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        if (ambientSource != null) ambientSource.volume = 0f; // Зі старту дощу немає
        
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

    // === МЕТОДИ ДЛЯ ПОГОДИ (З ПЛАВНИМ ПЕРЕХОДОМ) ===
    
    public void PlayRain()
    {
        if (ambientSource != null && rainSound != null && !ambientSource.isPlaying)
        {
            ambientSource.clip = rainSound;
            ambientSource.loop = true;
            ambientSource.volume = 0f; // Починаємо з абсолютної тиші
            ambientSource.Play();

            // Запускаємо плавне наростання до цільової гучності за 3 секунди
            if (ambientFadeCoroutine != null) StopCoroutine(ambientFadeCoroutine);
            ambientFadeCoroutine = StartCoroutine(FadeAmbientVolume(sfxVolume * 0.4f, 3f)); // Дощ грає на 40% від SFX
        }
    }

    public void StopRain()
    {
        if (ambientSource != null && ambientSource.isPlaying)
        {
            // Запускаємо плавне затихання до нуля за 2 секунди, після чого звук вимкнеться
            if (ambientFadeCoroutine != null) StopCoroutine(ambientFadeCoroutine);
            ambientFadeCoroutine = StartCoroutine(FadeAmbientVolume(0f, 2f, true));
        }
    }

    public void PlayThunder()
    {
        if (sfxSource != null && thunderSound != null)
        {
            // Гучність грому робимо вищою за звичайні звуки (коефіцієнт 1.5f), щоб він був епічним
            sfxSource.PlayOneShot(thunderSound, sfxVolume * 1.5f); 
        }
    }

    // Корутина, яка робить кінематографічний Fade In / Fade Out для амбієнту
    private IEnumerator FadeAmbientVolume(float targetVolume, float duration, bool stopAudioAtEnd = false)
    {
        float startVolume = ambientSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            ambientSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            yield return null;
        }

        ambientSource.volume = targetVolume;

        if (stopAudioAtEnd)
        {
            ambientSource.Stop();
        }
    }
    // ===============================================

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
        
        // Якщо дощ зараз грає, оновлюємо і його гучність (з коефіцієнтом 0.4)
        if (ambientSource != null && ambientSource.isPlaying) 
        {
            ambientSource.volume = sfxVolume * 0.4f; 
        }
        
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }
}