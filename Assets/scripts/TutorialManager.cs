using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public enum TutorialTrigger { ClickButton, WaitForWaveEnd, WaitTime, WaitForCart, WaitForSpearman, InfoOnly }

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
    public GameObject darkOverlay; // Raycast Target = true
    public TMP_Text dialogText;
    public RectTransform pointerHand;
    public GameObject dialogPanel;
    
    [Header("Налаштування вказівника")]
    public Vector2 pointerOffset = new Vector2(0, 0);
    [Tooltip("Напрямок стрибка: (0,1) - вверх/вниз, (1,0) - вправо/вліво")]
    public Vector2 bounceDirection = new Vector2(0, 1); 

    [Header("Кроки")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Дебаг")]
    public int currentStepIndex = 0;
    private bool isTutorialActive = false;
    private RectTransform currentTarget;
    private Button currentButton;

    private Canvas addedCanvas;
    private GraphicRaycaster addedRaycaster;
    private int originalSortingOrder;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if (tutorialCanvas != null) tutorialCanvas.SetActive(true);
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

        if (dialogText != null) dialogText.text = step.dialogText;
        if (dialogPanel != null) dialogPanel.SetActive(!string.IsNullOrEmpty(step.dialogText));

        RestorePreviousTarget();

        Time.timeScale = step.pauseGame ? 0f : 1f;

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
            case TutorialTrigger.InfoOnly:
                if (pointerHand != null) pointerHand.gameObject.SetActive(false);
                SetupClickToContinue();
                break;
        }
    }

    void Update()
    {
        if (!isTutorialActive) return;

        if (currentButton != null && !currentButton.interactable)
        {
            currentButton.interactable = true;
            CanvasGroup cg = currentButton.GetComponent<CanvasGroup>();
            if (cg != null && cg.alpha < 1f) cg.alpha = 1f;
        }

        if (pointerHand != null && pointerHand.gameObject.activeSelf)
        {
            float bounce = Mathf.Sin(Time.unscaledTime * 6f) * 15f;
            // Використовуємо bounceDirection, щоб стрибати у потрібному напрямку
            pointerHand.anchoredPosition = (Vector2.zero + pointerOffset) + (bounceDirection * bounce);
        }
    }

    void HighlightTarget(RectTransform target)
    {
        currentTarget = target;
        currentButton = target.GetComponent<Button>();

        if (pointerHand != null)
        {
            pointerHand.gameObject.SetActive(true);
            pointerHand.SetParent(target, false);
            pointerHand.anchoredPosition = Vector2.zero + pointerOffset;
            
            // ВИДАЛЕНО: pointerHand.localRotation = Quaternion.identity; 
            // Тепер рукавиця зберігає той поворот (Rotation), який ви задали їй в сцені!
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
        addedCanvas.sortingOrder = 2000;

        addedRaycaster = target.gameObject.GetComponent<GraphicRaycaster>();
        if (addedRaycaster == null)
        {
            addedRaycaster = target.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (currentButton != null)
        {
            currentButton.interactable = true;
            currentButton.onClick.RemoveListener(OnTargetButtonClicked);
            currentButton.onClick.AddListener(OnTargetButtonClicked);
        }
    }

    void RestorePreviousTarget()
    {
        if (currentButton != null)
        {
            currentButton.onClick.RemoveListener(OnTargetButtonClicked);
        }

        if (addedCanvas != null)
        {
            if (pointerHand != null)
            {
                pointerHand.SetParent(tutorialCanvas.transform, false);
                pointerHand.gameObject.SetActive(false);
            }

            if (addedRaycaster != null) Destroy(addedRaycaster);

            if (originalSortingOrder == 0)
            {
                Destroy(addedCanvas);
            }
            else
            {
                addedCanvas.overrideSorting = false;
                addedCanvas.sortingOrder = originalSortingOrder;
            }

            addedCanvas = null;
        }

        currentButton = null;
        currentTarget = null;
    }

    void OnTargetButtonClicked()
    {
        if (steps[currentStepIndex].trigger != TutorialTrigger.ClickButton) return;

        if (currentButton != null)
        {
            currentButton.onClick.RemoveListener(OnTargetButtonClicked);
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

        if (darkOverlay != null && darkOverlay.GetComponent<Button>() != null)
            darkOverlay.GetComponent<Button>().onClick.RemoveAllListeners();

        currentStepIndex++;
        PlayerPrefs.SetInt("TutorialStepIndex", currentStepIndex);
        PlayerPrefs.Save();

        ShowStep(currentStepIndex);
    }

    public void EndTutorial()
    {
        isTutorialActive = false;
        RestorePreviousTarget();
        if (tutorialCanvas != null) tutorialCanvas.SetActive(false);
        PlayerPrefs.SetInt("TutorialStepIndex", 999);
    }

    IEnumerator WaitForWaveRoutine()
    {
        yield return new WaitForSeconds(2f);
        while (GameManager.Instance.enemiesAlive > 0 || GameManager.Instance.waveTimerBar.value > 0)
            yield return null;
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
        CameraController cam = Camera.main.GetComponent<CameraController>();

        if (cart != null && cam != null) cam.StartFollowing(cart.transform);

        float timer = 0f;
        while (cart != null && timer < backupTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (cam != null) { cam.StopFollowing(); cam.ReturnToBase(); }
        yield return new WaitForSeconds(1.5f);
        NextStep();
    }

    IEnumerator WaitForSpearmanRoutine()
    {
        EnemySpearman spearman = null;
        while (spearman == null)
        {
            spearman = FindFirstObjectByType<EnemySpearman>();
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(1.5f);
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