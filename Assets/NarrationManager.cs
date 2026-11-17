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

    [Header("Komponen UI Dialog")]
    public GameObject dialogueBox;          // Panel utama yang mencakup name box & text box
    public TextMeshProUGUI nameText;        // Teks nama pembicara ("HRD" atau nama kandidat)
    public TextMeshProUGUI dialogueText;    // Teks isi kalimat dialog
    public float typingSpeed = 0.03f;       // Kecepatan efek ketik huruf demi huruf

    [Header("Tombol Multiple Choices (3 Pilihan)")]
    public GameObject choicesContainer;     // Parent panel yang membungkus ketiga tombol
    public Button skillButton;              // Tombol tanya Pengalaman / Skill
    public Button salaryButton;             // Tombol tanya Ekspektasi Gaji
    public Button endInterviewButton;       // Tombol Sudahi Interview (muncul setelah isi Excel)

    // State internal dialog
    private NarrationData currentNarration;
    private Queue<DialogueLine> lineQueue = new Queue<DialogueLine>();
    private DialogueLine currentLine;
    private Coroutine typingCoroutine;

    private bool isTyping = false;
    private bool isDialogueActive = false;
    private bool hasFilledExcel = false;
    private Action onSequenceComplete;

    // Event untuk memberi tahu GameManager saat sesi wawancara (termasuk Outro) selesai
    public event Action OnInterviewFinished;

    private void Start()
    {
        if (skillButton != null)
            skillButton.onClick.AddListener(OnSelectSkill);

        if (salaryButton != null)
            salaryButton.onClick.AddListener(OnSelectSalary);

        if (endInterviewButton != null)
            endInterviewButton.onClick.AddListener(OnSelectEndInterview);

        HideAllUI();
    }

    private void Update()
    {
        if (isDialogueActive && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            if (choicesContainer != null && choicesContainer.activeSelf) return;

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

    /// <summary>
    /// Memulai sesi wawancara baru: membuka teks box untuk Intro, lalu menutupnya saat pilihan muncul.
    /// </summary>
    public void StartInterview(NarrationData data)
    {
        currentNarration = data;
        hasFilledExcel = false;

        if (endInterviewButton != null)
            endInterviewButton.gameObject.SetActive(false);

        if (choicesContainer != null)
            choicesContainer.SetActive(false);

        StartDialogueSequence(currentNarration.introLines, ShowChoicesAndCloseDialogue);
    }

    public void UnlockEndInterviewButton()
    {
        hasFilledExcel = true;

        if (choicesContainer != null && choicesContainer.activeSelf && endInterviewButton != null)
        {
            endInterviewButton.gameObject.SetActive(true);
        }
    }

    public void ForceStopDialogue()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        lineQueue.Clear();
        isTyping = false;
        isDialogueActive = false;
        HideAllUI();
    }

    #region Logika Pemutaran Dialog

    private void StartDialogueSequence(List<DialogueLine> lines, Action onCompleteCallback)
    {
        if (lines == null || lines.Count == 0)
        {
            onCompleteCallback?.Invoke();
            return;
        }

        // Tutup panel pilihan dan buka kembali teks box
        if (choicesContainer != null)
            choicesContainer.SetActive(false);

        if (dialogueBox != null)
            dialogueBox.SetActive(true);

        onSequenceComplete = onCompleteCallback;
        lineQueue.Clear();

        foreach (DialogueLine line in lines)
        {
            lineQueue.Enqueue(line);
        }

        isDialogueActive = true;
        DisplayNextLine();
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
        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void CompleteCurrentSentence()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueText.text = currentLine.sentence;
        isTyping = false;
    }

    #endregion

    #region Logika 3 Tombol Pilihan (Multiple Choices)

    /// <summary>
    /// Menutup teks box dan memunculkan tombol pilihan setelah dialog selesai.
    /// </summary>
    private void ShowChoicesAndCloseDialogue()
    {
        if (dialogueBox != null)
            dialogueBox.SetActive(false);

        if (choicesContainer != null)
            choicesContainer.SetActive(true);

        if (endInterviewButton != null)
            endInterviewButton.gameObject.SetActive(hasFilledExcel);
    }

    private void OnSelectSkill()
    {
        // Membuka kembali teks box dan memutar dialog Skill
        StartDialogueSequence(currentNarration.skillLines, ShowChoicesAndCloseDialogue);
    }

    private void OnSelectSalary()
    {
        // Membuka kembali teks box dan memutar dialog Salary
        StartDialogueSequence(currentNarration.salaryLines, ShowChoicesAndCloseDialogue);
    }

    private void OnSelectEndInterview()
    {
        // Membuka kembali teks box untuk Outro, lalu menutup semuanya saat Outro selesai
        StartDialogueSequence(currentNarration.outroLines, () =>
        {
            HideAllUI();
            OnInterviewFinished?.Invoke();
        });
    }

    private void HideAllUI()
    {
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (choicesContainer != null) choicesContainer.SetActive(false);
    }

    #endregion
}
