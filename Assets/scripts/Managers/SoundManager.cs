using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Налаштування Джерел")]
    public AudioSource musicSource; 
    public AudioSource sfxSource;   
    [Tooltip("Джерело для зациклених фонових звуків (дощ, вітер)")]
    public AudioSource ambientSource; 

    [Header("Музика (Плейлисти)")]
    [Tooltip("Список музики під час забудови та між хвилями")]
    public AudioClip[] idleMusicTracks;
    
    [Tooltip("Список епічної музики під час хвилі ворогів")]
    public AudioClip[] battleMusicTracks;

    private AudioClip lastIdleTrack;
    private AudioClip lastBattleTrack;

    [Header("Амбієнт (Погода)")]
    public AudioClip rainSound; 
    public AudioClip thunderSound; 

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

    [Header("Скрині (Lootbox)")]
    public AudioClip chestShakeSound;     // Звук тряски скрині
    public AudioClip chestOpenSound;      // Звук відкриття (скрип/магічний хлопок)
    public AudioClip chestLootBurstSound; // Звук вильоту луту фонтаном

    [Header("Події гри")]
    public AudioClip waveStart;     
    public AudioClip victory;  
    [Tooltip("Короткі фанфари після відбиття хвилі")]
    public AudioClip victoryMusicStinger; 
    [Tooltip("Галас натовпу, що радіє перемозі")]
    public AudioClip victoryCries;        
    public AudioClip defeat;        

    [HideInInspector] public float musicVolume = 0.5f;
    [HideInInspector] public float sfxVolume = 1f;

    private Coroutine ambientFadeCoroutine; 
    private Coroutine musicTransitionCoroutine; 

    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            // Робимо об'єкт незнищуваним між сценами
            DontDestroyOnLoad(gameObject); 
        }
        else 
        {
            Destroy(gameObject);
            return;
        }

        // Завантажуємо збережені налаштування гучності
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    void Start()
    {
        if (musicSource != null) musicSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        if (ambientSource != null) ambientSource.volume = 0f; 
        
        PlayIdleMusic();
    }

    public void PlayIdleMusic()
    {
        if (idleMusicTracks == null || idleMusicTracks.Length == 0) return;
        AudioClip clipToPlay = GetRandomTrack(idleMusicTracks, ref lastIdleTrack);
        SwitchMusic(clipToPlay);
    }

    public void PlayBattleMusic()
    {
        if (battleMusicTracks == null || battleMusicTracks.Length == 0) return;
        AudioClip clipToPlay = GetRandomTrack(battleMusicTracks, ref lastBattleTrack);
        SwitchMusic(clipToPlay);
    }

    private AudioClip GetRandomTrack(AudioClip[] tracks, ref AudioClip lastPlayed)
    {
        if (tracks.Length == 1) return tracks[0];
        
        AudioClip randomTrack = tracks[Random.Range(0, tracks.Length)];
        int attempts = 0;
        while (randomTrack == lastPlayed && attempts < 10)
        {
            randomTrack = tracks[Random.Range(0, tracks.Length)];
            attempts++;
        }
        lastPlayed = randomTrack;
        return randomTrack;
    }

    private void SwitchMusic(AudioClip newClip)
    {
        if (musicSource == null || newClip == null) return;
        if (musicSource.clip == newClip && musicSource.isPlaying) return;

        if (musicTransitionCoroutine != null) StopCoroutine(musicTransitionCoroutine);
        musicTransitionCoroutine = StartCoroutine(CrossfadeMusic(newClip, 1.5f));
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip, float duration)
    {
        float halfDuration = duration / 2f;
        float startVolume = musicSource.volume;
        float t = 0f;

        if (musicSource.isPlaying)
        {
            while (t < halfDuration)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t / halfDuration);
                yield return null;
            }
        }

        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();
        t = 0f;
        
        while (t < halfDuration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, t / halfDuration);
            yield return null;
        }

        musicSource.volume = musicVolume;
    }

    public void PlayRain()
    {
        if (ambientSource != null && rainSound != null && !ambientSource.isPlaying)
        {
            ambientSource.clip = rainSound;
            ambientSource.loop = true;
            ambientSource.volume = 0f; 
            ambientSource.Play();

            if (ambientFadeCoroutine != null) StopCoroutine(ambientFadeCoroutine);
            ambientFadeCoroutine = StartCoroutine(FadeAmbientVolume(sfxVolume * 0.4f, 3f)); 
        }
    }

    public void StopRain()
    {
        if (ambientSource != null && ambientSource.isPlaying)
        {
            if (ambientFadeCoroutine != null) StopCoroutine(ambientFadeCoroutine);
            ambientFadeCoroutine = StartCoroutine(FadeAmbientVolume(0f, 2f, true));
        }
    }

    public void PlayThunder()
    {
        if (sfxSource != null && thunderSound != null)
        {
            sfxSource.PlayOneShot(thunderSound, sfxVolume * 1.5f); 
        }
    }

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
        if (stopAudioAtEnd) ambientSource.Stop();
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
    {
        if (sfxSource != null && clip != null)
        {
            // ФІКС: Якщо гравець викликає звук горну, штучно підвищуємо йому гучність!
            if (clip == waveStart) 
            {
                volumeScale *= 2.5f; // Робить горн у 2.5 рази гучнішим (можеш змінити на 2f або 3f)
            }

            sfxSource.PlayOneShot(clip, volumeScale * sfxVolume);
        }
    }

    // Для надійності додамо те саме і сюди:
    public void PlaySFXRandomPitch(AudioClip clip, float volumeScale = 1.0f)
    {
        if (sfxSource != null && clip != null)
        {
            if (clip == waveStart) volumeScale *= 2.5f; // Також підвищуємо гучність

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
        if (ambientSource != null && ambientSource.isPlaying) ambientSource.volume = sfxVolume * 0.4f; 
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    // Викликай це зі слайдера в налаштуваннях
    public void UpdateMusicVolume(float volume)
    {
        musicVolume = volume;
        if (musicSource != null) musicSource.volume = musicVolume;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
    }

    public void UpdateSFXVolume(float volume)
    {
        sfxVolume = volume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }
}