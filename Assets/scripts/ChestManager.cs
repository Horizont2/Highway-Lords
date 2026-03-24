using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ChestManager : MonoBehaviour
{
    public static ChestManager Instance;

    private enum ChestState { Hidden, Ready, Opening, WaitingForTap, Flying }
    private ChestState currentState = ChestState.Hidden;

    [Header("UI Панелі")]
    public GameObject chestPanel;         
    public Image chestImage;              
    public Button tapScreenButton;        
    public GameObject tapTextObj;         

    [Header("Спрайти")]
    public Sprite closedChestSprite;
    public Sprite openChestSprite;

    [Header("Налаштування Луту (Префаби)")]
    public GameObject flyingCoinPrefab;   
    public GameObject flyingGemPrefab;    
    
    [Header("Куди летить лут? (Твої лічильники)")]
    public RectTransform goldCounterUI;   
    public RectTransform gemCounterUI;    

    private int generatedGold = 0;
    private int generatedGems = 0;
    private List<GameObject> spawnedVisualLoot = new List<GameObject>();
    private Coroutine idleBounceCoroutine; // Корутина для підстрибування

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (chestPanel) chestPanel.SetActive(false);
        if (tapScreenButton) tapScreenButton.onClick.AddListener(OnScreenTapped);
    }

    // ТИМЧАСОВИЙ КОД ДЛЯ ТЕСТУ
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            GiveChest();
        }
    }

    [ContextMenu("Test Give Chest")]
    public void GiveChest()
    {
        GameManager.Instance.CloseAllPanels();
        
        currentState = ChestState.Ready;
        chestImage.sprite = closedChestSprite;
        chestImage.transform.localScale = Vector3.one;
        chestImage.rectTransform.anchoredPosition = Vector2.zero;
        
        if (tapTextObj) tapTextObj.SetActive(true);
        if (tapTextObj) tapTextObj.GetComponent<TMP_Text>().text = "TAP TO OPEN";
        
        tapScreenButton.interactable = true;
        chestPanel.SetActive(true);

        // Запускаємо анімацію підстрибування!
        if (idleBounceCoroutine != null) StopCoroutine(idleBounceCoroutine);
        idleBounceCoroutine = StartCoroutine(IdleBounceAnimation());
    }

    private void OnScreenTapped()
    {
        if (currentState == ChestState.Ready) StartCoroutine(ChestOpenAnimation());
        else if (currentState == ChestState.WaitingForTap) StartCoroutine(FlyLootToCounters());
    }

    // === НОВА АНІМАЦІЯ ПІДСТРИБУВАННЯ ===
    private IEnumerator IdleBounceAnimation()
    {
        Vector2 basePos = Vector2.zero; 
        
        while (currentState == ChestState.Ready)
        {
            // Математика стрибка: беремо модуль від Синуса (щоб стрибало тільки вгору)
            float hop = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 25f;
            
            // Легкий Squish & Stretch для мультяшності
            float stretch = 1f + (hop * 0.004f); 
            float squish = 1f - (hop * 0.002f);

            chestImage.rectTransform.anchoredPosition = basePos + new Vector2(0, hop);
            chestImage.transform.localScale = new Vector3(squish, stretch, 1f);
            
            yield return null;
        }
        
        // Повертаємо на місце, якщо анімація закінчилась
        chestImage.rectTransform.anchoredPosition = basePos;
        chestImage.transform.localScale = Vector3.one;
    }

    private IEnumerator ChestOpenAnimation()
    {
        // Зупиняємо підстрибування перед відкриттям
        if (idleBounceCoroutine != null) StopCoroutine(idleBounceCoroutine);

        currentState = ChestState.Opening;
        if (tapTextObj) tapTextObj.SetActive(false);

        // 1. Трясемо скриню
        float shakeDuration = 1.0f;
        float elapsed = 0f;
        Vector2 originalPos = chestImage.rectTransform.anchoredPosition;

        // Новий звук тряски (або запасний, якщо його немає)
        if (SoundManager.Instance && SoundManager.Instance.chestShakeSound) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.chestShakeSound); 
        else if (SoundManager.Instance) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.constructionSound); 

        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float strength = Mathf.Lerp(5f, 25f, elapsed / shakeDuration); 
            float offsetX = UnityEngine.Random.Range(-strength, strength);
            float offsetY = UnityEngine.Random.Range(-strength, strength);
            
            chestImage.rectTransform.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);
            yield return null;
        }

        chestImage.rectTransform.anchoredPosition = originalPos;

        // 2. Відкриваємо (Зміна картинки + Віддзеркалення)
        chestImage.sprite = openChestSprite;
        
        // Новий звук відкриття
        if (SoundManager.Instance && SoundManager.Instance.chestOpenSound) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.chestOpenSound); 
        else if (SoundManager.Instance) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.unitUpgradeSound);

        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1f, 1.3f, Mathf.PingPong(elapsed * 5f, 1f));
            chestImage.transform.localScale = new Vector3(-scale, scale, 1f);
            yield return null;
        }
        chestImage.transform.localScale = new Vector3(-1f, 1f, 1f); 

        // 3. Розкидаємо лут
        GenerateAndScatterLoot();
    }

    private void GenerateAndScatterLoot()
    {
        // Звук вильоту луту (фонтан)
        if (SoundManager.Instance && SoundManager.Instance.chestLootBurstSound) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.chestLootBurstSound); 

        int waveMultiplier = GameManager.Instance.currentWave < 1 ? 1 : GameManager.Instance.currentWave;
        generatedGold = UnityEngine.Random.Range(300, 600) + (waveMultiplier * 50);
        generatedGems = UnityEngine.Random.Range(5, 15);

        int visualCoins = Mathf.Clamp(generatedGold / 30, 8, 20);
        int visualGems = Mathf.Clamp(generatedGems, 3, 10);

        SpawnScatterGroup(flyingCoinPrefab, visualCoins);
        SpawnScatterGroup(flyingGemPrefab, visualGems);

        currentState = ChestState.WaitingForTap;
        if (tapTextObj) 
        {
            tapTextObj.SetActive(true);
            tapTextObj.GetComponent<TMP_Text>().text = "TAP TO COLLECT";
        }
    }

    private void SpawnScatterGroup(GameObject prefab, int count)
    {
        if (prefab == null) return;

        for (int i = 0; i < count; i++)
        {
            GameObject item = Instantiate(prefab, chestPanel.transform);
            item.transform.position = chestImage.transform.position; 
            spawnedVisualLoot.Add(item);

            Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
            float randomDist = UnityEngine.Random.Range(150f, 350f);
            Vector2 targetPos = chestImage.rectTransform.anchoredPosition + (randomDir * randomDist);

            StartCoroutine(AnimateScatterItem(item.GetComponent<RectTransform>(), targetPos));
        }
    }

    private IEnumerator AnimateScatterItem(RectTransform rt, Vector2 targetPos)
    {
        float duration = UnityEngine.Random.Range(0.4f, 0.7f);
        float elapsed = 0f;
        Vector2 startPos = rt.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float ease = 1f - Mathf.Pow(1f - t, 3f); 
            
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
            yield return null;
        }
    }

    private IEnumerator FlyLootToCounters()
    {
        currentState = ChestState.Flying;
        if (tapTextObj) tapTextObj.SetActive(false);

        float flyDuration = 0.6f;

        foreach (var item in spawnedVisualLoot)
        {
            RectTransform rt = item.GetComponent<RectTransform>();
            bool isCoin = item.name.Contains(flyingCoinPrefab.name);
            RectTransform targetUI = isCoin ? goldCounterUI : gemCounterUI;

            if (targetUI != null) StartCoroutine(FlySingleItem(rt, targetUI, flyDuration));
        }

        yield return new WaitForSecondsRealtime(flyDuration + 0.1f);

        if (SoundManager.Instance) SoundManager.Instance.PlaySFX(SoundManager.Instance.coinPickup);
        GameManager.Instance.AddResource(ResourceType.Gold, generatedGold);
        GameManager.Instance.gems += generatedGems;
        GameManager.Instance.UpdateMetaUI();

        foreach (var item in spawnedVisualLoot) Destroy(item);
        spawnedVisualLoot.Clear();

        chestPanel.SetActive(false);
        currentState = ChestState.Hidden;
    }

    private IEnumerator FlySingleItem(RectTransform rt, RectTransform targetUI, float duration)
    {
        yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0f, 0.2f));

        float elapsed = 0f;
        Vector3 startPos = rt.position; 

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float ease = t * t * t; 

            rt.position = Vector3.Lerp(startPos, targetUI.position, ease);
            
            float scale = Mathf.Lerp(1f, 0.5f, t);
            rt.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }
        
        rt.gameObject.SetActive(false); 
    }
}