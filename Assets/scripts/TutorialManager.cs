using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Додано нові тригери: WaitForGold, WaitForPanel
public enum TutorialTrigger { ClickButton, WaitForWaveEnd, WaitTime, WaitForCart, WaitForSpearman, InfoOnly, WaitForGold, WaitForPanel }

[System.Serializable]
public class TutorialStep
{
    [TextArea(3, 5)] public string dialogText;
    public RectTransform targetUI;
    public TutorialTrigger trigger;
    public float waitDuration = 3f;
    public bool pauseGame = false;
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI Гайду")]
    public GameObject tutorialCanvas;
    public GameObject darkOverlay; 
    public TMP_Text dialogText;
    public RectTransform pointerHand;
    public GameObject dialogPanel;
    
    [Header("Налаштування вказівника")]
    public Vector2 pointerOffset = new Vector2(0, 0);
    public Vector2 bounceDirection = new Vector2(0, 1); 

    [Header("Кроки")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Дебаг")]
    public int currentStepIndex = 0;
    private bool isTutorialActive = false;
    private RectTransform currentTarget;
    
    private Button[] activeButtons;
    private Canvas addedCanvas;
    private GraphicRaycaster addedRaycaster;
    private int originalSortingOrder;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        // Видалено примусове увімкнення на старті, щоб уникнути "блимання"
    }

    void Start()
    {
        currentStepIndex = PlayerPrefs.GetInt("TutorialStepIndex", 0);
        if (currentStepIndex >= steps.Count)
        {
            if (tutorialCanvas != null) tutorialCanvas.SetActive(false);
            return;
        }
        StartCoroutine(StartTutorialWithDelay());
    }

    IEnumerator StartTutorialWithDelay()
    {
        yield return new WaitForSeconds(0.6f);
        isTutorialActive = true;
        ShowStep(currentStepIndex);
    }

    void ShowStep(int index)
    {
        if (index >= steps.Count) { EndTutorial(); return; }

        currentStepIndex = index;
        TutorialStep step = steps[index];

        // Вмикаємо канвас тільки тоді, коли реально показуємо крок
        if (tutorialCanvas != null) tutorialCanvas.SetActive(true);

        if (dialogText != null) dialogText.text = step.dialogText;
        if (dialogPanel != null) dialogPanel.SetActive(!string.IsNullOrEmpty(step.dialogText));

        RestorePreviousTarget();

        Time.timeScale = step.pauseGame ? 0f : 1f;

        if (tutorialCanvas != null)
        {
            Canvas tutCanvas = tutorialCanvas.GetComponent<Canvas>();
            if (tutCanvas != null)
            {
                tutCanvas.overrideSorting = true;
                tutCanvas.sortingOrder = 30000;
            }
        }

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(true);
            Image overlayImg = darkOverlay.GetComponent<Image>();
            if (overlayImg != null)
            {
                overlayImg.raycastTarget = true;
                overlayImg.color = step.pauseGame ? new Color(0, 0, 0, 0.5f) : new Color(0, 0, 0, 0.85f);
            }
        }

        switch (step.trigger)
        {
            case TutorialTrigger.ClickButton:
                if (step.targetUI != null) HighlightTarget(step.targetUI);
                else { Debug.LogWarning("Tutorial: TargetUI is null for ClickButton step!"); NextStep(); }
                break;
            case TutorialTrigger.WaitForWaveEnd:
                HideTutorialVisuals();
                StartCoroutine(WaitForWaveRoutine());
                break;
            case TutorialTrigger.WaitTime:
                if (pointerHand != null) pointerHand.gameObject.SetActive(false);
                StartCoroutine(WaitTimeRoutine(step.waitDuration));
                break;
            case TutorialTrigger.WaitForCart:
                StartCoroutine(WaitForCartRoutine(step.waitDuration));
                break;
            case TutorialTrigger.WaitForSpearman:
                HideTutorialVisuals();
                StartCoroutine(WaitForSpearmanRoutine());
                break;
            case TutorialTrigger.WaitForGold:
                HideTutorialVisuals();
                StartCoroutine(WaitForGoldRoutine((int)step.waitDuration)); 
                break;
            case TutorialTrigger.WaitForPanel:
                HideTutorialVisuals();
                StartCoroutine(WaitForPanelRoutine(step.targetUI));
                break;
            case TutorialTrigger.InfoOnly:
                if (pointerHand != null) pointerHand.gameObject.SetActive(false);
                SetupClickToContinue();
                break;
        }
    }

    void Update()
    {
        if (!isTutorialActive) return;

        if (activeButtons != null)
        {
            foreach (var b in activeButtons)
            {
                if (b != null) 
                {
                    b.interactable = true;
                    
                    CanvasGroup cg = b.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.alpha = 1f;
                        cg.blocksRaycasts = true;
                    }

                    ColorBlock cb = b.colors;
                    cb.normalColor = Color.white;
                    cb.highlightedColor = Color.white;
                    cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                    cb.disabledColor = Color.white;
                    cb.colorMultiplier = 1f;
                    b.colors = cb;
                }
            }
        }

        if (pointerHand != null && pointerHand.gameObject.activeSelf)
        {
            float bounce = Mathf.Sin(Time.unscaledTime * 6f) * 15f;
            pointerHand.anchoredPosition = (Vector2.zero + pointerOffset) + (bounceDirection * bounce);
        }
    }

    void HighlightTarget(RectTransform target)
    {
        if (target == null) return; 

        Button actualButton = target.GetComponentInChildren<Button>();
        if (actualButton != null && actualButton.transform != target)
        {
            target = actualButton.GetComponent<RectTransform>();
        }

        currentTarget = target;
        activeButtons = target.GetComponentsInChildren<Button>(true);

        if (pointerHand != null)
        {
            pointerHand.gameObject.SetActive(true);
            pointerHand.SetParent(target, false);
            pointerHand.anchoredPosition = Vector2.zero + pointerOffset;
        }

        addedCanvas = target.gameObject.GetComponent<Canvas>();
        if (addedCanvas == null)
        {
            addedCanvas = target.gameObject.AddComponent<Canvas>();
            originalSortingOrder = 0;
        }
        else
        {
            originalSortingOrder = addedCanvas.sortingOrder;
        }

        addedCanvas.overrideSorting = true;
        addedCanvas.sortingOrder = 30010;

        CanvasGroup targetCg = target.GetComponent<CanvasGroup>();
        if (targetCg == null) targetCg = target.gameObject.AddComponent<CanvasGroup>();
        targetCg.ignoreParentGroups = true;
        targetCg.blocksRaycasts = true;
        targetCg.interactable = true;

        addedRaycaster = target.gameObject.GetComponent<GraphicRaycaster>();
        if (addedRaycaster == null) addedRaycaster = target.gameObject.AddComponent<GraphicRaycaster>();

        if (activeButtons != null)
        {
            foreach (var b in activeButtons)
            {
                if (b == null) continue;
                b.interactable = true;
                b.onClick.RemoveListener(OnTargetButtonClicked);
                b.onClick.AddListener(OnTargetButtonClicked);

                CanvasGroup bcg = b.GetComponent<CanvasGroup>();
                if (bcg != null)
                {
                    bcg.ignoreParentGroups = true;
                    bcg.blocksRaycasts = true;
                    bcg.interactable = true;
                }
            }
        }
    }

    void RestorePreviousTarget()
    {
        if (activeButtons != null)
        {
            foreach (var b in activeButtons)
            {
                if (b != null)
                {
                    b.onClick.RemoveListener(OnTargetButtonClicked);
                    CanvasGroup bcg = b.GetComponent<CanvasGroup>();
                    if (bcg != null) bcg.ignoreParentGroups = false;
                }
            }
            activeButtons = null;
        }

        if (currentTarget != null)
        {
            CanvasGroup targetCg = currentTarget.GetComponent<CanvasGroup>();
            if (targetCg != null) targetCg.ignoreParentGroups = false;
        }

        if (addedCanvas != null)
        {
            if (pointerHand != null)
            {
                pointerHand.SetParent(tutorialCanvas.transform, false);
                pointerHand.gameObject.SetActive(false);
            }

            if (addedRaycaster != null) Destroy(addedRaycaster);

            if (originalSortingOrder == 0) Destroy(addedCanvas);
            else
            {
                addedCanvas.overrideSorting = false;
                addedCanvas.sortingOrder = originalSortingOrder;
            }

            addedCanvas = null;
        }

        currentTarget = null;
        
        if (GameManager.Instance != null) GameManager.Instance.UpdateUI();
    }

    void OnTargetButtonClicked()
    {
        if (steps[currentStepIndex].trigger != TutorialTrigger.ClickButton) return;

        if (activeButtons != null)
        {
            foreach (var b in activeButtons)
            {
                if (b != null) b.onClick.RemoveListener(OnTargetButtonClicked);
            }
        }

        NextStep();
    }

    void SetupClickToContinue()
    {
        if (darkOverlay == null) return;
        Button b = darkOverlay.GetComponent<Button>();
        if (b == null) b = darkOverlay.AddComponent<Button>();
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(NextStep);
    }

    void HideTutorialVisuals()
    {
        if (darkOverlay != null) darkOverlay.SetActive(false);
        if (dialogPanel != null) dialogPanel.SetActive(false);
        if (pointerHand != null) pointerHand.gameObject.SetActive(false);
    }

    public void NextStep()
    {
        if (!isTutorialActive) return;

        Time.timeScale = 1f; 
        RestorePreviousTarget();

        if (darkOverlay != null)
        {
            Button b = darkOverlay.GetComponent<Button>();
            if (b != null) b.onClick.RemoveAllListeners();
        }

        currentStepIndex++;
        PlayerPrefs.SetInt("TutorialStepIndex", currentStepIndex);
        PlayerPrefs.Save();

        ShowStep(currentStepIndex);
    }

    public void EndTutorial()
    {
        isTutorialActive = false;
        Time.timeScale = 1f; 
        RestorePreviousTarget();
        HideTutorialVisuals();
        if (tutorialCanvas != null) tutorialCanvas.SetActive(false);
        PlayerPrefs.SetInt("TutorialStepIndex", 999);
    }

    IEnumerator WaitForWaveRoutine()
    {
        yield return new WaitForSeconds(2f);
        if (GameManager.Instance != null)
        {
            while (GameManager.Instance.enemiesAlive > 0 || (GameManager.Instance.waveTimerBar != null && GameManager.Instance.waveTimerBar.value > 0))
                yield return null;
        }
        yield return new WaitForSeconds(1.5f);
        NextStep();
    }

    IEnumerator WaitTimeRoutine(float time)
    {
        yield return new WaitForSecondsRealtime(time);
        NextStep();
    }

    IEnumerator WaitForCartRoutine(float backupTime)
    {
        if (dialogPanel != null) dialogPanel.SetActive(false);
        if (darkOverlay != null) darkOverlay.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        yield return new WaitForSeconds(0.5f);
        GameObject cart = GameObject.FindGameObjectWithTag("Cart");
        CameraController cam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;

        if (cart != null && cam != null) cam.StartFollowing(cart.transform);

        float timer = 0f;
        while (cart != null && timer < backupTime)
        {
            timer += Time.unscaledDeltaTime; 
            yield return null;
        }

        if (cam != null) { cam.StopFollowing(); cam.ReturnToBase(); }
        yield return new WaitForSecondsRealtime(1.5f);
        NextStep();
    }

    IEnumerator WaitForSpearmanRoutine()
    {
        EnemySpearman spearman = null;
        while (spearman == null && isTutorialActive)
        {
            spearman = FindFirstObjectByType<EnemySpearman>();
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(1.5f);
        NextStep();
    }

    // НОВЕ: Очікування золота
    IEnumerator WaitForGoldRoutine(int targetGold)
    {
        while (GameManager.Instance == null || GameManager.Instance.gold < targetGold)
        {
            yield return new WaitForSeconds(1f);
        }
        yield return new WaitForSeconds(0.5f);
        NextStep();
    }

    // НОВЕ: Очікування відкриття вікна
    IEnumerator WaitForPanelRoutine(RectTransform panel)
    {
        while (panel == null || !panel.gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(0.5f);
        NextStep();
    }

    [ContextMenu("Reset Tutorial Progress")]
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey("TutorialStepIndex");
        currentStepIndex = 0;
        Debug.Log("ПРОГРЕС ГАЙДУ СКИНУТО! ПЕРЕЗАПУСТІТЬ ГРУ.");
    }
}