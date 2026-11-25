using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Налаштування руху (Клавіатура)")]
    public float panSpeed = 15f; 

    [Header("Обмеження (Межі мапи)")]
    public float minX = -10f; 
    public float maxX = 20f;  

    [Header("Перетягування мишкою (Drag)")]
    public bool useDragPanning = true;
    [Tooltip("0 - Ліва кнопка, 1 - Права, 2 - Коліщатко миші")]
    public int dragMouseButton = 0; 

    [Header("Авто-повернення після хвилі")]
    public float defaultX = 0f; 
    public float returnSpeed = 1.5f; 

    [Header("Налаштування Катсцен")]
    public AudioClip elephantRoarSound;     
    public AudioClip cinematicImpactSound;  
    public bool isCinematicPlaying = false; 

    private Vector3 dragOrigin;
    private Camera cam;
    private bool isAutoPanning = false;
    
    // НОВЕ: Об'єкт, за яким камера має слідкувати
    private Transform targetToFollow;

    private GameObject cinematicCanvas;
    private RectTransform topBar;
    private RectTransform bottomBar;

    void Start()
    {
        cam = GetComponent<Camera>();
        SetupCinematicBars();
    }

    void Update()
    {
        if (isCinematicPlaying) return; 
        if (GameManager.Instance != null && GameManager.Instance.isDefeated) return;

        // === НОВЕ: Слідкування за об'єктом (наприклад, возом) ===
        if (targetToFollow != null)
        {
            Vector3 followPos = transform.position;
            // Плавне слідкування (Lerp)
            followPos.x = Mathf.Lerp(followPos.x, targetToFollow.position.x, Time.deltaTime * 5f);
            followPos.x = Mathf.Clamp(followPos.x, minX, maxX);
            transform.position = followPos;
            
            return; // Блокуємо ручне керування, поки камера слідкує!
        }

        if (isAutoPanning)
        {
            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetMouseButtonDown(dragMouseButton))
            {
                isAutoPanning = false;
                StopAllCoroutines();
            }
            else return; 
        }

        Vector3 pos = transform.position;
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        pos.x += horizontalInput * panSpeed * Time.deltaTime;

        if (useDragPanning)
        {
            if (Input.GetMouseButtonDown(dragMouseButton)) dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            if (Input.GetMouseButton(dragMouseButton))
            {
                Vector3 currentMousePos = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector3 difference = dragOrigin - currentMousePos;
                pos.x += difference.x;
            }
        }

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        transform.position = pos;
    }

    // === НОВІ МЕТОДИ ДЛЯ ТУТОРІАЛУ ===
    public void StartFollowing(Transform target)
    {
        targetToFollow = target;
        isAutoPanning = false; // Вимикаємо авто-повернення, якщо воно йшло
    }

    public void StopFollowing()
    {
        targetToFollow = null;
    }

    public void ReturnToBase()
    {
        if (isCinematicPlaying) return; 
        StopAllCoroutines();
        StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        isAutoPanning = true;
        float startX = transform.position.x;
        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * returnSpeed;
            float smoothStep = Mathf.SmoothStep(0f, 1f, time); 
            
            Vector3 pos = transform.position;
            pos.x = Mathf.Lerp(startX, defaultX, smoothStep);
            transform.position = pos;
            
            yield return null;
        }
        
        isAutoPanning = false;
    }

    private void SetupCinematicBars()
    {
        if (cinematicCanvas != null) return;
        
        cinematicCanvas = new GameObject("CinematicCanvas");
        Canvas c = cinematicCanvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 999; 
        
        CanvasScaler cs = cinematicCanvas.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        
        GameObject tBar = new GameObject("TopBar");
        tBar.transform.SetParent(cinematicCanvas.transform, false); 
        Image tImg = tBar.AddComponent<Image>();
        tImg.color = Color.black;
        topBar = tBar.GetComponent<RectTransform>();
        topBar.anchorMin = new Vector2(0, 1);
        topBar.anchorMax = new Vector2(1, 1);
        topBar.pivot = new Vector2(0.5f, 1);
        topBar.anchoredPosition = Vector2.zero;
        topBar.sizeDelta = new Vector2(0, 0); 
        
        GameObject bBar = new GameObject("BottomBar");
        bBar.transform.SetParent(cinematicCanvas.transform, false); 
        Image bImg = bBar.AddComponent<Image>();
        bImg.color = Color.black;
        bottomBar = bBar.GetComponent<RectTransform>();
        bottomBar.anchorMin = new Vector2(0, 0);
        bottomBar.anchorMax = new Vector2(1, 0);
        bottomBar.pivot = new Vector2(0.5f, 0);
        bottomBar.anchoredPosition = Vector2.zero;
        bottomBar.sizeDelta = new Vector2(0, 0);
    }

    public void PlayBossCutscene(Transform bossTransform)
    {
        StartCoroutine(BossCutsceneRoutine(bossTransform));
    }

    private IEnumerator BossCutsceneRoutine(Transform bossTransform)
    {
        isCinematicPlaying = true;
        isAutoPanning = false; 
        
        if (GameManager.Instance != null) GameManager.Instance.SetCinematicUI(true);

        float t = 0;
        float targetBarHeight = Screen.height * 0.15f; 
        while(t < 0.5f) {
            t += Time.deltaTime;
            float h = Mathf.Lerp(0, targetBarHeight, t / 0.5f);
            topBar.sizeDelta = new Vector2(0, h);
            bottomBar.sizeDelta = new Vector2(0, h);
            yield return null;
        }

        Vector3 startPos = transform.position;
        float targetX = Mathf.Clamp(bossTransform.position.x - 3f, minX, maxX); 
        Vector3 targetPos = new Vector3(targetX, transform.position.y, transform.position.z);
        
        t = 0f;
        while (t < 3.5f)
        {
            t += Time.deltaTime;
            if (bossTransform != null) {
                targetX = Mathf.Clamp(bossTransform.position.x - 3f, minX, maxX);
                targetPos = new Vector3(targetX, transform.position.y, transform.position.z);
            }
            float smooth = Mathf.SmoothStep(0f, 1f, t / 3.5f);
            transform.position = Vector3.Lerp(startPos, targetPos, smooth);
            yield return null;
        }

        if (SoundManager.Instance != null)
        {
            if (elephantRoarSound != null) SoundManager.Instance.PlaySFX(elephantRoarSound, 1.5f);
            if (cinematicImpactSound != null) SoundManager.Instance.PlaySFX(cinematicImpactSound, 1.5f);
        }

        float shakeDuration = 1.5f; 
        float shakeMagnitude = 0.3f; 
        float shakeTime = 0f;
        Vector3 shakeBasePos = transform.position;

        while (shakeTime < shakeDuration)
        {
            shakeTime += Time.deltaTime;
            float offsetX = Random.Range(-1f, 1f) * shakeMagnitude;
            float offsetY = Random.Range(-1f, 1f) * shakeMagnitude;
            transform.position = new Vector3(shakeBasePos.x + offsetX, shakeBasePos.y + offsetY, shakeBasePos.z);
            yield return null;
        }
        transform.position = shakeBasePos; 

        yield return new WaitForSeconds(1.5f);

        t = 0f;
        startPos = transform.position;
        Vector3 baseTarget = new Vector3(defaultX, transform.position.y, transform.position.z);

        while (t < 1.5f)
        {
            t += Time.deltaTime;
            float smooth = Mathf.SmoothStep(0f, 1f, t / 1.5f);
            transform.position = Vector3.Lerp(startPos, baseTarget, smooth);
            yield return null;
        }
        transform.position = baseTarget;

        t = 0;
        while(t < 0.5f) {
            t += Time.deltaTime;
            float h = Mathf.Lerp(targetBarHeight, 0, t / 0.5f);
            topBar.sizeDelta = new Vector2(0, h);
            bottomBar.sizeDelta = new Vector2(0, h);
            yield return null;
        }

        if (GameManager.Instance != null) GameManager.Instance.SetCinematicUI(false);

        isCinematicPlaying = false;
    }
}