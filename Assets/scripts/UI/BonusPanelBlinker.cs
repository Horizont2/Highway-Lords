using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BonusPanelBlinker : MonoBehaviour
{
    [Header("Налаштування мигання")]
    [Tooltip("Колір, яким буде підсвічуватись кнопка")]
    public Color blinkColor = new Color(1f, 0.9f, 0.4f, 1f); // Золотисто-жовтий
    public float blinkTime = 3.5f; // Скільки секунд триватиме мигання
    public float blinkSpeed = 8f;  // Швидкість (частота) пульсації
    
    private Image btnImage;
    private Color normalColor;
    private Vector3 normalScale;
    private Vector3 blinkScale;
    
    private int lastGems = -1;
    private Coroutine blinkCoroutine;

    void Start()
    {
        btnImage = GetComponent<Image>();
        
        // Автоматично запам'ятовуємо стандартний колір та розмір кнопки
        if (btnImage != null) normalColor = btnImage.color;
        
        normalScale = transform.localScale;
        blinkScale = normalScale * 1.1f; // Кнопка буде збільшуватись на 10%

        // Якщо гравець клікне по кнопці, мигання одразу зупиниться
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(StopBlink);
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        int currentGems = GameManager.Instance.gems;

        // Ініціалізація при першому кадрі
        if (lastGems == -1)
        {
            lastGems = currentGems;
            return;
        }

        // Якщо кількість кристалів змінилася
        if (currentGems != lastGems)
        {
            // Якщо гравець щойно отримав новий кристал
            if (currentGems > lastGems)
            {
                int affordableNow = CountAffordableUpgrades(currentGems);
                int affordableBefore = CountAffordableUpgrades(lastGems);

                // Якщо тепер ми можемо дозволити собі більше унікальних покращень, ніж секунду тому
                if (affordableNow > affordableBefore)
                {
                    StartBlink();
                }
            }
            
            lastGems = currentGems;
        }
    }

    // Рахує, скільки з 6 покращень гравець може купити ПРЯМО ЗАРАЗ
    int CountAffordableUpgrades(int gemsAmount)
    {
        int count = 0;
        
        // Формули цін повністю відповідають тим, що у вашому GameManager
        if (gemsAmount >= 10 + (GameManager.Instance.metaFortifiedWalls * 5)) count++;
        if (gemsAmount >= 15 + (GameManager.Instance.metaPrecisionBows * 10)) count++;
        if (gemsAmount >= 25 + (GameManager.Instance.metaVolleyBarrage * 15)) count++;
        if (gemsAmount >= 10 + (GameManager.Instance.metaTrophyBounty * 5)) count++;
        if (gemsAmount >= 15 + (GameManager.Instance.metaEfficientCarts * 10)) count++;
        if (gemsAmount >= 20 + (GameManager.Instance.metaMendingMasonry * 15)) count++;

        return count;
    }

    public void StartBlink()
    {
        // Якщо панель і так вже відкрита перед очима - не мигаємо
        if (GameManager.Instance.metaShopPanel != null && GameManager.Instance.metaShopPanel.activeSelf) 
            return;

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        if (btnImage == null) yield break;

        float timer = 0f;
        while (timer < blinkTime)
        {
            timer += Time.deltaTime;
            
            // Плавна пульсація (від 0 до 1) за допомогою математичної синусоїди
            float lerp = (Mathf.Sin(timer * blinkSpeed) + 1f) / 2f; 
            
            btnImage.color = Color.Lerp(normalColor, blinkColor, lerp);
            transform.localScale = Vector3.Lerp(normalScale, blinkScale, lerp);
            
            yield return null;
        }

        StopBlink(); // Вимикаємо по закінченню часу
    }

    public void StopBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        
        // Гарантовано повертаємо кнопку до нормального стану
        if (btnImage != null) btnImage.color = normalColor;
        transform.localScale = normalScale;
    }
}