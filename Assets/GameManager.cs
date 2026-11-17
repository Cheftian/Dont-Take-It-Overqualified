using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum QualificationStatus
{
    None,
    Qualified,
    NotQualified,
    Security
}

[System.Serializable]
public class CandidateRecord
{
    public string characterId;
    public bool isOrangDalam;
    public QualificationStatus status;
}

public class GameManager : MonoBehaviour
{
    [Header("Referensi Manager")]

    public static GameManager Instance { get; private set; }
    public CharacterManager characterManager;
    public NarrationManager narrationManager;
    public ExcelPanelManager excelPanelManager;
    public PanelAnimation panelAnimation;

    [Header("Pengaturan Antrean Kandidat")]
    [Tooltip("CharacterId yang wajib muncul paling pertama (contoh: Big Boss)")]
    public string firstCharacterId = "Big Boss";
    [Tooltip("Centang jika karakter pertama ikut dinilai dalam logika ending, hilangkan centang jika hanya intro/tutorial")]
    public bool includeFirstCharacterInEvaluation = false;

    [Header("Pengaturan Scene Ending")]
    public string endingASceneName = "EndingA_OrangDalamLolos";
    public string endingBSceneName = "EndingB_OrangDalamGagal";
    public string endingCSceneName = "EndingC_SemuaDiusir";

    [Header("Data Rekap Hasil Interview (Read-Only di Inspector)")]
    [SerializeField] private List<CandidateRecord> interviewQueue = new List<CandidateRecord>();
    [SerializeField] private int currentIndex = -1;

    private bool isTransitioning = false;

    public CandidateRecord CurrentCandidateRecord =>
        (currentIndex >= 0 && currentIndex < interviewQueue.Count) ? interviewQueue[currentIndex] : null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (narrationManager != null)
        {
            narrationManager.OnInterviewFinished += HandleInterviewFinished;
        }

        BuildRandomizedQueue();
        StartNextCandidate();
    }

    private void OnDestroy()
    {
        if (narrationManager != null)
        {
            narrationManager.OnInterviewFinished -= HandleInterviewFinished;
        }
    }

    /// <summary>
    /// Menyusun antrean: karakter pertama tetap di posisi 0, sisanya diacak.
    /// </summary>
    private void BuildRandomizedQueue()
    {
        interviewQueue.Clear();
        List<CharacterProfile> allProfiles = new List<CharacterProfile>(characterManager.characterDatabase);
        List<CharacterProfile> randomPool = new List<CharacterProfile>();

        // 1. Cari karakter wajib pertama (misal: Big Boss)
        CharacterProfile firstChar = allProfiles.Find(c => c.characterId == firstCharacterId);
        if (firstChar != null)
        {
            interviewQueue.Add(new CandidateRecord
            {
                characterId = firstChar.characterId,
                isOrangDalam = firstChar.isOrangDalam,
                status = QualificationStatus.None
            });
            allProfiles.Remove(firstChar);
        }
        else if (!string.IsNullOrEmpty(firstCharacterId))
        {
            Debug.LogWarning($"[GameManager] FirstCharacterId '{firstCharacterId}' tidak ditemukan di CharacterDatabase!");
        }

        // 2. Masukkan sisa karakter ke pool acak
        randomPool.AddRange(allProfiles);

        // 3. Acak urutan menggunakan algoritma Fisher-Yates Shuffle
        for (int i = randomPool.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            CharacterProfile temp = randomPool[i];
            randomPool[i] = randomPool[randomIndex];
            randomPool[randomIndex] = temp;
        }

        // 4. Masukkan hasil acakan ke dalam antrean utama
        foreach (CharacterProfile profile in randomPool)
        {
            interviewQueue.Add(new CandidateRecord
            {
                characterId = profile.characterId,
                isOrangDalam = profile.isOrangDalam,
                status = QualificationStatus.None
            });
        }
    }

    /// <summary>
    /// Memanggil kandidat berikutnya atau memicu ending jika antrean habis.
    /// </summary>
    public void StartNextCandidate()
    {
        currentIndex++;

        if (currentIndex >= interviewQueue.Count)
        {
            EvaluateAndLoadEnding();
            return;
        }

        StartCoroutine(EnterCandidateRoutine());
    }

     private IEnumerator EnterCandidateRoutine()
    {
        isTransitioning = true;

        if (excelPanelManager != null)
        {
            excelPanelManager.ResetExcelEvaluation();
        }

        CandidateRecord record = interviewQueue[currentIndex];
        CharacterProfile profile = characterManager.SetupCandidate(record.characterId);

        if (profile != null && panelAnimation != null)
        {
            SpriteRenderer characterRenderer = characterManager.CurrentCharacterSpriteRenderer;
            if (characterRenderer != null)
            {
                panelAnimation.characterTransform = characterRenderer.transform;
            }
            else
            {
                Debug.LogError($"[GameManager] SpriteRenderer tidak tersedia untuk karakter '{record.characterId}'.");
            }
        }

        // Jalankan animasi slide dari kanan ke kiri
        if (panelAnimation != null)
        {
            yield return StartCoroutine(panelAnimation.SlideCharacterIn());
        }

        isTransitioning = false;

        if (profile != null && profile.narrationData != null)
        {
            narrationManager.StartInterview(profile.narrationData);
        }
        else
        {
            Debug.LogError($"[GameManager] NarrationData kosong untuk karakter '{record.characterId}'!");
        }
    }


    /// <summary>
    /// Dipanggil oleh UI Excel saat pemain mencentang Qualified (true) atau Not Qualified (false).
    /// </summary>
    public void SetCurrentCandidateQualification(bool isQualified)
    {
        CandidateRecord current = CurrentCandidateRecord;
        if (current == null || isTransitioning) return;

        current.status = isQualified ? QualificationStatus.Qualified : QualificationStatus.NotQualified;

        // Buka kunci tombol "Sudahi Interview" di NarrationManager
        if (narrationManager != null)
        {
            narrationManager.UnlockEndInterviewButton();
        }
    }

    /// <summary>
    /// Dipanggil oleh PhoneInteruption saat pemain menekan ponsel untuk memanggil sekuriti.
    /// </summary>
    public void TriggerSecurityRemoval()
    {
        CandidateRecord current = CurrentCandidateRecord;
        if (current == null || isTransitioning) return;

        current.status = QualificationStatus.Security;

        // Hentikan dialog paksa
        if (narrationManager != null)
        {
            narrationManager.ForceStopDialogue();
        }

        StartCoroutine(ExitCandidateRoutine(isDraggedBySecurity: true));
    }

    /// <summary>
    /// Dipanggil otomatis saat pemain menekan "Sudahi Interview" dan dialog Outro selesai.
    /// </summary>
    private void HandleInterviewFinished()
    {
        if (isTransitioning) return;
        StartCoroutine(ExitCandidateRoutine(isDraggedBySecurity: false));
    }

    private IEnumerator ExitCandidateRoutine(bool isDraggedBySecurity)
    {
        isTransitioning = true;

        if (isDraggedBySecurity)
        {
            // Jalankan animasi tangan sekuriti masuk lalu menarik karakter ke kanan
            if (panelAnimation != null)
            {
                yield return StartCoroutine(panelAnimation.SlideSecurityPullOut());
            }
        }
        else
        {
            // Jalankan animasi karakter slide keluar normal ke kanan
            if (panelAnimation != null)
            {
                yield return StartCoroutine(panelAnimation.SlideCharacterOut());
            }
        }

        isTransitioning = false;
        StartNextCandidate();
    }


    /// <summary>
    /// Mengevaluasi seluruh hasil interview untuk menentukan salah satu dari 3 Ending.
    /// </summary>
    private void EvaluateAndLoadEnding()
    {
        // Tentukan indeks awal evaluasi (lewati index 0 jika karakter pertama hanya pengantar/Big Boss)
        int startIndex = (!includeFirstCharacterInEvaluation && !string.IsNullOrEmpty(firstCharacterId)) ? 1 : 0;

        int evaluatedCount = 0;
        int securityCount = 0;
        bool orangDalamQualified = false;

        for (int i = startIndex; i < interviewQueue.Count; i++)
        {
            CandidateRecord rec = interviewQueue[i];
            evaluatedCount++;

            if (rec.status == QualificationStatus.Security)
            {
                securityCount++;
            }

            if (rec.isOrangDalam && rec.status == QualificationStatus.Qualified)
            {
                orangDalamQualified = true;
            }
        }

        // Prioritas 1: Ending C - Mengeluarkan semua calon karyawan dengan security
        if (evaluatedCount > 0 && securityCount == evaluatedCount)
        {
            Debug.Log("[Ending Triggered] Ending C: Semua kandidat diusir sekuriti!");
            SceneManager.LoadScene(endingCSceneName);
            return;
        }

        // Prioritas 2: Ending A - Berhasil memasukkan karakter dengan bool Orang Dalam
        if (orangDalamQualified)
        {
            Debug.Log("[Ending Triggered] Ending A: Orang Dalam berhasil lolos!");
            SceneManager.LoadScene(endingASceneName);
            return;
        }

        // Prioritas 3: Ending B - Tidak memasukkan karakter dengan bool Orang Dalam
        Debug.Log("[Ending Triggered] Ending B: Orang Dalam tidak lolos!");
        SceneManager.LoadScene(endingBSceneName);
    }

    
}
