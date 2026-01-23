using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Налаштування Джерел")]
    public AudioSource musicSource; // Сюди AudioSource з галочкою Loop
    public AudioSource sfxSource;   // Сюди AudioSource БЕЗ галочки Loop

    [Header("Музика")]
    public AudioClip backgroundMusic;

    [Header("Атака та Бій")]
    public AudioClip arrowShoot;    // Постріл вежі
    public AudioClip arrowHit;      // Влучання стріли
    public AudioClip swordHit;      // Удар меча
    public AudioClip enemyDeath;    // Смерть ворога
    public AudioClip knightHit;     // Лицар отримав урон (Критично!)

    [Header("Руйнування")]
    public AudioClip cartBreak;     // Віз зламався
    public AudioClip castleDamage;  // Урон по замку (Критично!)

    [Header("Економіка та UI")]
    public AudioClip coinPickup;    // Зібрав золото
    public AudioClip buyItem;       // Успішна покупка
    public AudioClip error;         // Немає грошей (Помилка)

    [Header("Події гри")]
    public AudioClip waveStart;     // Початок хвилі
    public AudioClip victory;       // Перемога
    public AudioClip defeat;        // Програш

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        PlayMusic();
    }

    public void PlayMusic()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = 0.3f; // Музика тихіше (30%)
            musicSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}