using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NarrationManager : MonoBehaviour
{
    [Header("Referensi Manager")]
    public CharacterManager characterManager;
    public PanelAnimation panelAnimation;

    [Header("Komponen UI Dialog")]
    public GameObject dialogueBox;          // Panel utama yang mencakup name box & text box
    public TextMeshProUGUI nameText;        // Teks nama pembicara ("HRD" atau nama kandidat)
    public TextMeshProUGUI dialogueText;    // Teks isi kalimat dialog
    public float typingSpeed = 0.03f;       // Kecepatan efek ketik huruf demi huruf

    [Header("Tutorial Big Boss")]
    public string tutorialCharacterId = "Big Boss";
    public Button pdfButton;
    public Button pdfCloseButton;
    public Button wordButton;
    public Button wordCloseButton;
    public Button excelButton;
    public ExcelPanelManager excelPanelManager;

    [Header("Tombol Multiple Choices (3 Pilihan)")]
    public GameObject choicesContainer;     // Parent panel yang membungkus ketiga tombol
    public Button skillButton;              // Tombol tanya Pengalaman / Skill
    public Button salaryButton;             // Tombol tanya Ekspektasi Gaji
    public Button endInterviewButton;       // Tombol Sudahi Interview (muncul setelah isi Excel)

    [Header("Pemblokir Input Dialog")]
    [Tooltip("Jika salah satu GameObject di sini aktif, klik layar & tombol Spasi tidak akan melanjutkan dialog.")]
    public GameObject[] blockingObjects;

    [Header("Animasi Dialog")]
    public float dialoguePopupDuration = 0.3f;
    public float dialoguePopupOffsetX = 140f;
    public float dialogueShakeDuration = 0.18f;
    public float dialogueShakeStrength = 6f;

    [Header("Animasi Pilihan")]
    public float choiceSlideDuration = 0.3f;
    public float choiceSlideOffsetX = 180f;
    public Vector2 choiceRandomDelayRange = new Vector2(0f, 0.25f);


    // State internal dialog
    private NarrationData currentNarration;
    private Queue<DialogueLine> lineQueue = new Queue<DialogueLine>();
    private DialogueLine currentLine;
    private Coroutine typingCoroutine;

    private bool isTyping = false;
    private bool isDialogueActive = false;
    private bool hasFilledExcel = false;
    private bool dialogueShouldBeVisible;
    private bool choicesShouldBeVisible;
    private bool blockingObjectsWereActive;
    private bool hasDisplayedLineInSequence;
    private Action onSequenceComplete;

    private RectTransform dialogueBoxRect;
    private Vector2 dialogueBoxOriginalPosition;
    private Vector3 dialogueBoxOriginalScale;
    private Coroutine dialogueBoxAnimationCoroutine;
    private Coroutine choicesStaggerCoroutine;
    private readonly List<Coroutine> choiceBubbleAnimationCoroutines = new List<Coroutine>();
    private readonly Dictionary<RectTransform, Vector2> choiceBubbleOriginalPositions = new Dictionary<RectTransform, Vector2>();
    private GridLayoutGroup choicesGridLayout;
    private bool choicesGridLayoutWasEnabled;
    private bool choicesGridLayoutTemporarilyDisabled;

    private enum TutorialStep
    {
        None,
        WaitingForPdfOpen,
        WaitingForPdfClose,
        WaitingForWordOpen,
        WaitingForWordClose,
        WaitingForExcelOpen,
        WaitingForExcelClose,
        ShowingChoices
    }

    private TutorialStep tutorialStep;
    private bool isTutorial;
    private bool isTutorialChoiceMode;
    private Button tutorialExcelCloseButton;
    private TextMeshProUGUI skillButtonLabel;
    private TextMeshProUGUI salaryButtonLabel;
    private string skillButtonOriginalLabel;
    private string salaryButtonOriginalLabel;

    // Event untuk memberi tahu GameManager saat sesi wawancara (termasuk Outro) selesai
    public event Action OnInterviewFinished;

    private void Awake()
    {
        if (skillButton != null)
            skillButton.onClick.AddListener(OnSelectSkill);

        if (salaryButton != null)
            salaryButton.onClick.AddListener(OnSelectSalary);

        if (endInterviewButton != null)
            endInterviewButton.onClick.AddListener(OnSelectEndInterview);

        if (pdfButton != null)
            pdfButton.onClick.AddListener(OnTutorialPdfOpened);
        if (pdfCloseButton != null)
            pdfCloseButton.onClick.AddListener(OnTutorialPdfClosed);
        if (wordButton != null)
            wordButton.onClick.AddListener(OnTutorialWordOpened);
        if (wordCloseButton != null)
            wordCloseButton.onClick.AddListener(OnTutorialWordClosed);
        if (excelButton != null)
            excelButton.onClick.AddListener(OnTutorialExcelOpened);

        skillButtonLabel = GetButtonLabel(skillButton);
        salaryButtonLabel = GetButtonLabel(salaryButton);
        skillButtonOriginalLabel = skillButtonLabel != null ? skillButtonLabel.text : string.Empty;
        salaryButtonOriginalLabel = salaryButtonLabel != null ? salaryButtonLabel.text : string.Empty;

        CacheDialogueBoxTransform();
        HideAllUI();
        blockingObjectsWereActive = IsAnyBlockingObjectActive();
    }

    private void OnDestroy()
    {
        if (skillButton != null)
            skillButton.onClick.RemoveListener(OnSelectSkill);
        if (salaryButton != null)
            salaryButton.onClick.RemoveListener(OnSelectSalary);
        if (endInterviewButton != null)
            endInterviewButton.onClick.RemoveListener(OnSelectEndInterview);

        if (pdfButton != null)
            pdfButton.onClick.RemoveListener(OnTutorialPdfOpened);
        if (pdfCloseButton != null)
            pdfCloseButton.onClick.RemoveListener(OnTutorialPdfClosed);
        if (wordButton != null)
            wordButton.onClick.RemoveListener(OnTutorialWordOpened);
        if (wordCloseButton != null)
            wordCloseButton.onClick.RemoveListener(OnTutorialWordClosed);
        if (excelButton != null)
            excelButton.onClick.RemoveListener(OnTutorialExcelOpened);
        if (tutorialExcelCloseButton != null)
            tutorialExcelCloseButton.onClick.RemoveListener(OnTutorialExcelClosed);
    }

    private void Update()
    {
        RefreshBlockingObjectState();

        if (isDialogueActive && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            // Cek jika panel pilihan aktif
            if (choicesContainer != null && choicesContainer.activeSelf) return;

            // Cek jika ada GameObject di dalam array blockingObjects yang sedang aktif
            if (IsAnyBlockingObjectActive()) return;

            if (isTyping)
            {
                CompleteCurrentSentence();
            }
            else
            {
                DisplayNextLine();
            }
        }
    }

    private void RefreshBlockingObjectState()
        {
            bool blockingObjectsActive = IsAnyBlockingObjectActive();
            if (blockingObjectsActive == blockingObjectsWereActive) return;

            blockingObjectsWereActive = blockingObjectsActive;
            if (blockingObjectsActive)
            {
                HideDialogueBoxForBlockingObject();
                HideChoicesForBlockingObject();
                return;
            }

            if (dialogueShouldBeVisible)
                SetDialogueBoxVisible(true, true);
            if (choicesShouldBeVisible)
                SetChoicesContainerVisible(true, true);
        }

    private void CacheDialogueBoxTransform()
        {
            if (dialogueBox == null) return;

            dialogueBoxRect = dialogueBox.GetComponent<RectTransform>();
            if (dialogueBoxRect == null) return;

            dialogueBoxOriginalPosition = dialogueBoxRect.anchoredPosition;
            dialogueBoxOriginalScale = dialogueBoxRect.localScale;
        }

    private void SetDialogueBoxVisible(bool visible, bool animate)
        {
            dialogueShouldBeVisible = visible;
            if (dialogueBox == null) return;

            if (!visible || IsAnyBlockingObjectActive())
            {
                StopDialogueBoxAnimation();
                dialogueBox.SetActive(false);
                return;
            }

            dialogueBox.SetActive(true);
            if (animate)
                StartDialogueBoxPopup();
            else
                ResetDialogueBoxTransform();
        }

    private void HideDialogueBoxForBlockingObject()
        {
            if (dialogueBox == null) return;

            StopDialogueBoxAnimation();
            dialogueBox.SetActive(false);
        }

    private void StartDialogueBoxPopup()
        {
            if (dialogueBoxRect == null) return;

            StopDialogueBoxAnimation();
            dialogueBoxRect.anchoredPosition = dialogueBoxOriginalPosition + Vector2.left * dialoguePopupOffsetX;
            dialogueBoxRect.localScale = Vector3.zero;
            dialogueBoxAnimationCoroutine = StartCoroutine(AnimateDialogueBoxPopup());
        }

    private IEnumerator AnimateDialogueBoxPopup()
        {
            float elapsed = 0f;
            while (elapsed < dialoguePopupDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dialoguePopupDuration);
                float easedT = Mathf.SmoothStep(0f, 1f, t);

                dialogueBoxRect.anchoredPosition = Vector2.Lerp(
                    dialogueBoxOriginalPosition + Vector2.left * dialoguePopupOffsetX,
                    dialogueBoxOriginalPosition,
                    easedT
                );
                dialogueBoxRect.localScale = Vector3.Lerp(Vector3.zero, dialogueBoxOriginalScale, easedT);
                yield return null;
            }

            ResetDialogueBoxTransform();
            dialogueBoxAnimationCoroutine = null;
        }

    private void StartDialogueBoxShake()
        {
            if (dialogueBoxRect == null || dialogueBox == null ||
                !dialogueBox.activeInHierarchy || IsAnyBlockingObjectActive())
            {
                return;
            }

            StopDialogueBoxAnimation();
            dialogueBoxAnimationCoroutine = StartCoroutine(ShakeDialogueBox());
        }

    private IEnumerator ShakeDialogueBox()
        {
            float elapsed = 0f;
            while (elapsed < dialogueShakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dialogueShakeDuration);
                float strength = dialogueShakeStrength * (1f - t);
                float offset = Mathf.Sin(elapsed * Mathf.PI * 2f * 24f) * strength;
                dialogueBoxRect.anchoredPosition = dialogueBoxOriginalPosition + Vector2.right * offset;
                yield return null;
            }

            ResetDialogueBoxTransform();
            dialogueBoxAnimationCoroutine = null;
        }

    private void StopDialogueBoxAnimation()
        {
            if (dialogueBoxAnimationCoroutine != null)
            {
                StopCoroutine(dialogueBoxAnimationCoroutine);
                dialogueBoxAnimationCoroutine = null;
            }

            ResetDialogueBoxTransform();
        }

    private void ResetDialogueBoxTransform()
        {
            if (dialogueBoxRect == null) return;

            dialogueBoxRect.anchoredPosition = dialogueBoxOriginalPosition;
            dialogueBoxRect.localScale = dialogueBoxOriginalScale;
        }

    private void SetChoicesContainerVisible(bool visible, bool animate)
        {
            choicesShouldBeVisible = visible;
            if (choicesContainer == null) return;

            if (!visible || IsAnyBlockingObjectActive())
            {
                StopChoicesAnimation();
                choicesContainer.SetActive(false);
                return;
            }

            choicesContainer.SetActive(true);
            if (animate)
                StartChoicesAnimation();
            else
                StopChoicesAnimation();
        }

    private void HideChoicesForBlockingObject()
        {
            if (choicesContainer == null) return;

            StopChoicesAnimation();
            choicesContainer.SetActive(false);
        }

    private void StartChoicesAnimation()
        {
            StopChoicesAnimation();

            RectTransform containerRect = choicesContainer.transform as RectTransform;
            if (containerRect != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            }

            choicesGridLayout = choicesContainer.GetComponent<GridLayoutGroup>();
            if (choicesGridLayout != null)
            {
                choicesGridLayoutWasEnabled = choicesGridLayout.enabled;
                choicesGridLayoutTemporarilyDisabled = choicesGridLayout.enabled;
                choicesGridLayout.enabled = false;
            }

            List<RectTransform> bubbles = GetVisibleChoiceBubbles();
            foreach (RectTransform bubble in bubbles)
            {
                choiceBubbleOriginalPositions[bubble] = bubble.anchoredPosition;
                bubble.anchoredPosition += Vector2.left * choiceSlideOffsetX;
            }

            if (bubbles.Count == 0)
            {
                choiceBubbleOriginalPositions.Clear();
                RestoreChoicesGridLayout();
                return;
            }
            choicesStaggerCoroutine = StartCoroutine(AnimateChoiceBubbles(bubbles));
        }

    private List<RectTransform> GetVisibleChoiceBubbles()
        {
            List<RectTransform> bubbles = new List<RectTransform>();
            Button[] buttons = { skillButton, salaryButton, endInterviewButton };

            foreach (Button button in buttons)
            {
                if (button == null || !button.gameObject.activeSelf) continue;

                RectTransform rect = button.transform as RectTransform;
                if (rect != null && !bubbles.Contains(rect))
                    bubbles.Add(rect);
            }

            return bubbles;
        }

    private IEnumerator AnimateChoiceBubbles(List<RectTransform> bubbles)
        {
            float maximumDelay = Mathf.Max(choiceRandomDelayRange.x, choiceRandomDelayRange.y);
            foreach (RectTransform bubble in bubbles)
            {
                float delay = UnityEngine.Random.Range(
                    Mathf.Min(choiceRandomDelayRange.x, choiceRandomDelayRange.y),
                    maximumDelay
                );
                choiceBubbleAnimationCoroutines.Add(StartCoroutine(
                    SlideChoiceBubble(bubble, choiceBubbleOriginalPositions[bubble], delay)
                ));
            }

            yield return new WaitForSecondsRealtime(maximumDelay + choiceSlideDuration);
            choicesStaggerCoroutine = null;
            choiceBubbleAnimationCoroutines.Clear();
            RestoreChoicesGridLayout();
        }

    private IEnumerator SlideChoiceBubble(RectTransform bubble, Vector2 targetPosition, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            Vector2 startPosition = targetPosition + Vector2.left * choiceSlideOffsetX;
            float elapsed = 0f;
            while (elapsed < choiceSlideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / choiceSlideDuration);
                bubble.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            bubble.anchoredPosition = targetPosition;
        }

    private void StopChoicesAnimation()
        {
            if (choicesStaggerCoroutine != null)
            {
                StopCoroutine(choicesStaggerCoroutine);
                choicesStaggerCoroutine = null;
            }

            foreach (Coroutine animation in choiceBubbleAnimationCoroutines)
            {
                if (animation != null)
                    StopCoroutine(animation);
            }
            choiceBubbleAnimationCoroutines.Clear();

            foreach (KeyValuePair<RectTransform, Vector2> bubble in choiceBubbleOriginalPositions)
            {
                if (bubble.Key != null)
                    bubble.Key.anchoredPosition = bubble.Value;
            }
            choiceBubbleOriginalPositions.Clear();
            RestoreChoicesGridLayout();
        }

    private void RestoreChoicesGridLayout()
        {
            if (choicesGridLayout != null && choicesGridLayoutTemporarilyDisabled)
                choicesGridLayout.enabled = choicesGridLayoutWasEnabled;

            choicesGridLayout = null;
            choicesGridLayoutTemporarilyDisabled = false;
        }
    /// <summary>
    /// Mengecek apakah ada minimal satu GameObject di dalam array yang sedang aktif di scene.
    /// </summary>
    private bool IsAnyBlockingObjectActive()
    {
        if (blockingObjects == null || blockingObjects.Length == 0) return false;

        foreach (GameObject obj in blockingObjects)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Memulai sesi wawancara baru: membuka teks box untuk Intro, lalu menutupnya saat pilihan muncul.
    /// </summary>
    public void StartInterview(NarrationData data)
    {
        currentNarration = data;
        hasFilledExcel = false;
        isTutorial = !string.IsNullOrEmpty(tutorialCharacterId) &&
                     data.targetCharacterId == tutorialCharacterId;
        isTutorialChoiceMode = false;
        tutorialStep = TutorialStep.None;
        RestoreTutorialChoiceLabels();

        if (endInterviewButton != null)
            endInterviewButton.gameObject.SetActive(false);

        SetChoicesContainerVisible(false, false);

        if (isTutorial)
        {
            if (data.introLines == null || data.introLines.Count != 15)
            {
                Debug.LogError($"[NarrationManager] Tutorial '{tutorialCharacterId}' harus memiliki tepat 15 intro lines.");
            }

            if (TutorialButtonsAreConfigured() && BindTutorialExcelCloseButton())
            {
                StartTutorialFromBeginning();
                return;
            }

            isTutorial = false;
        }

        StartDialogueSequence(currentNarration.introLines, ShowChoicesAndCloseDialogue);
    }

    public void UnlockEndInterviewButton()
    {
        hasFilledExcel = true;

        if (choicesShouldBeVisible && endInterviewButton != null)
        {
            endInterviewButton.gameObject.SetActive(true);
            if (choicesContainer != null && choicesContainer.activeInHierarchy && !IsAnyBlockingObjectActive())
                StartChoicesAnimation();
        }
    }

    public void ForceStopDialogue()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (panelAnimation != null)
            panelAnimation.StopTalkingAnimation();

        lineQueue.Clear();
        isTyping = false;
        isDialogueActive = false;
        isTutorial = false;
        isTutorialChoiceMode = false;
        tutorialStep = TutorialStep.None;
        RestoreTutorialChoiceLabels();
        HideAllUI();
    }

    #region Logika Pemutaran Dialog

    private void StartDialogueSequence(List<DialogueLine> lines, Action onCompleteCallback)
    {
        StartDialogueSequence(lines, 0, lines != null ? lines.Count : 0, onCompleteCallback);
    }

    private void StartDialogueSequence(List<DialogueLine> lines, int startIndex, int lineCount, Action onCompleteCallback)
    {
        if (lines == null || startIndex >= lines.Count || lineCount <= 0)
        {
            onCompleteCallback?.Invoke();
            return;
        }

        SetChoicesContainerVisible(false, false);
        SetDialogueBoxVisible(true, true);

        onSequenceComplete = onCompleteCallback;
        lineQueue.Clear();
        hasDisplayedLineInSequence = false;

        int endIndex = Mathf.Min(startIndex + lineCount, lines.Count);
        for (int i = startIndex; i < endIndex; i++)
        {
            lineQueue.Enqueue(lines[i]);
        }

        isDialogueActive = true;
        DisplayNextLine();
    }

    private void StartTutorialFromBeginning()
    {
        isTutorialChoiceMode = false;
        tutorialStep = TutorialStep.None;

        SetChoicesContainerVisible(false, false);

        if (endInterviewButton != null)
            endInterviewButton.gameObject.SetActive(false);

        StartDialogueSequence(currentNarration.introLines, 0, 6, () =>
        {
            tutorialStep = TutorialStep.WaitingForPdfOpen;
        });
    }

    private void StartTutorialSegment(int startIndex, int lineCount, Action onComplete)
    {
        StartDialogueSequence(currentNarration.introLines, startIndex, lineCount, onComplete);
    }

    private bool BindTutorialExcelCloseButton()
    {
        if (excelPanelManager == null || excelPanelManager.ExcelMainGameObject == null)
        {
            Debug.LogError("[NarrationManager] ExcelPanelManager atau GameObject utama Excel belum tersedia untuk tutorial.");
            return false;
        }

        Button[] buttons = excelPanelManager.ExcelMainGameObject.GetComponentsInChildren<Button>(true);
        Button closeButton = null;
        foreach (Button button in buttons)
        {
            if (button.gameObject.name == "Close")
            {
                closeButton = button;
                break;
            }
        }

        if (closeButton == null)
        {
            Debug.LogError("[NarrationManager] Tombol Close tidak ditemukan pada panel Excel tutorial.");
            return false;
        }

        if (tutorialExcelCloseButton != null)
            tutorialExcelCloseButton.onClick.RemoveListener(OnTutorialExcelClosed);

        tutorialExcelCloseButton = closeButton;
        tutorialExcelCloseButton.onClick.AddListener(OnTutorialExcelClosed);
        return true;
    }

    private bool TutorialButtonsAreConfigured()
    {
        if (pdfButton != null && pdfCloseButton != null &&
            wordButton != null && wordCloseButton != null &&
            excelButton != null && skillButton != null &&
            salaryButton != null && choicesContainer != null &&
            skillButtonLabel != null && salaryButtonLabel != null)
        {
            return true;
        }

        Debug.LogError("[NarrationManager] Tombol dan label untuk tutorial Big Boss belum lengkap di Inspector.");
        return false;
    }

    private TextMeshProUGUI GetButtonLabel(Button button)
    {
        return button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
    }

    private void ShowTutorialChoices()
    {
        tutorialStep = TutorialStep.ShowingChoices;
        isTutorialChoiceMode = true;

        if (skillButtonLabel != null)
            skillButtonLabel.text = "Tanya lagi";
        if (salaryButtonLabel != null)
            salaryButtonLabel.text = "Selesai";

        if (skillButton != null)
            skillButton.gameObject.SetActive(true);
        if (salaryButton != null)
            salaryButton.gameObject.SetActive(true);
        if (endInterviewButton != null)
            endInterviewButton.gameObject.SetActive(false);

        SetDialogueBoxVisible(false, false);
        SetChoicesContainerVisible(true, true);
    }

    private void RestoreTutorialChoiceLabels()
    {
        if (skillButtonLabel != null)
            skillButtonLabel.text = skillButtonOriginalLabel;
        if (salaryButtonLabel != null)
            salaryButtonLabel.text = salaryButtonOriginalLabel;
    }

    private void FinishTutorial()
    {
        isTutorial = false;
        isTutorialChoiceMode = false;
        tutorialStep = TutorialStep.None;
        RestoreTutorialChoiceLabels();
        HideAllUI();
        OnInterviewFinished?.Invoke();
    }

    private void OnTutorialPdfOpened()
    {
        if (isTutorial && tutorialStep == TutorialStep.WaitingForPdfOpen)
            tutorialStep = TutorialStep.WaitingForPdfClose;
    }

    private void OnTutorialPdfClosed()
    {
        if (!isTutorial || tutorialStep != TutorialStep.WaitingForPdfClose) return;

        StartTutorialSegment(6, 1, () => tutorialStep = TutorialStep.WaitingForWordOpen);
    }

    private void OnTutorialWordOpened()
    {
        if (isTutorial && tutorialStep == TutorialStep.WaitingForWordOpen)
            tutorialStep = TutorialStep.WaitingForWordClose;
    }

    private void OnTutorialWordClosed()
    {
        if (!isTutorial || tutorialStep != TutorialStep.WaitingForWordClose) return;

        StartTutorialSegment(7, 2, () => tutorialStep = TutorialStep.WaitingForExcelOpen);
    }

    private void OnTutorialExcelOpened()
    {
        if (excelPanelManager != null)
            excelPanelManager.ShowExcelPanel();

        if (isTutorial && tutorialStep == TutorialStep.WaitingForExcelOpen)
            tutorialStep = TutorialStep.WaitingForExcelClose;
    }

    private void OnTutorialExcelClosed()
    {
        if (!isTutorial || tutorialStep != TutorialStep.WaitingForExcelClose) return;

        int remainingLines = currentNarration.introLines != null
            ? Mathf.Max(0, currentNarration.introLines.Count - 9)
            : 0;
        StartTutorialSegment(9, remainingLines, ShowTutorialChoices);
    }

    private void DisplayNextLine()
    {
        if (lineQueue.Count == 0)
        {
            // Semua baris teks dalam fase ini sudah selesai dibacakan
            isDialogueActive = false;
            onSequenceComplete?.Invoke();
            return;
        }

        if (hasDisplayedLineInSequence)
            StartDialogueBoxShake();
        hasDisplayedLineInSequence = true;

        currentLine = lineQueue.Dequeue();

        if (currentLine.isHRD)
        {
            if (nameText != null) nameText.text = "HRD";
        }
        else
        {
            string charId = currentNarration.targetCharacterId;
            CharacterProfile profile = characterManager.GetCharacterProfile(charId);

            if (nameText != null)
                nameText.text = (profile != null && !string.IsNullOrEmpty(profile.displayName)) ? profile.displayName : charId;

            characterManager.SetExpression(charId, currentLine.expressionId);
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeSentence(currentLine.sentence));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        if (panelAnimation != null)
            panelAnimation.StartTalkingAnimation();

        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        if (panelAnimation != null)
            panelAnimation.StopTalkingAnimation();
    }

    private void CompleteCurrentSentence()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueText.text = currentLine.sentence;
        isTyping = false;
        if (panelAnimation != null)
            panelAnimation.StopTalkingAnimation();
    }

    #endregion

    #region Logika 3 Tombol Pilihan (Multiple Choices)

    /// <summary>
    /// Menutup teks box dan memunculkan tombol pilihan setelah dialog selesai.
    /// </summary>
    private void ShowChoicesAndCloseDialogue()
    {
        if (endInterviewButton != null)
            endInterviewButton.gameObject.SetActive(hasFilledExcel);

        SetDialogueBoxVisible(false, false);
        SetChoicesContainerVisible(true, true);
    }

    private void OnSelectSkill()
    {
        if (isTutorialChoiceMode)
        {
            StartTutorialFromBeginning();
            return;
        }

        // Membuka kembali teks box dan memutar dialog Skill
        StartDialogueSequence(currentNarration.skillLines, ShowChoicesAndCloseDialogue);
    }

    private void OnSelectSalary()
    {
        if (isTutorialChoiceMode)
        {
            FinishTutorial();
            return;
        }

        // Membuka kembali teks box dan memutar dialog Salary
        StartDialogueSequence(currentNarration.salaryLines, ShowChoicesAndCloseDialogue);
    }

    private void OnSelectEndInterview()
    {
        if (isTutorialChoiceMode) return;

        // Membuka kembali teks box untuk Outro, lalu menutup semuanya saat Outro selesai
        StartDialogueSequence(currentNarration.outroLines, () =>
        {
            HideAllUI();
            OnInterviewFinished?.Invoke();
        });
    }

    private void HideAllUI()
    {
        SetDialogueBoxVisible(false, false);
        SetChoicesContainerVisible(false, false);
    }

    #endregion
}
