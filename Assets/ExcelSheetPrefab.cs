using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ExcelEvaluationRow
{
    public string criteriaName;         // Label opsional agar mudah dibaca di Inspector (misal: "Skill", "Salary", "Attitude", "Keputusan Akhir")
    public Toggle qualifiedToggle;      // Toggle Qualified pada baris ini
    public Toggle notQualifiedToggle;   // Toggle Not Qualified pada baris ini
}

public class ExcelSheetPrefab : MonoBehaviour
{
    [Header("Daftar 4 Baris Toggling Excel")]
    public List<ExcelEvaluationRow> evaluationRows = new List<ExcelEvaluationRow>();

    [Header("Pengaturan Baris Penilaian Utama")]
    [Tooltip("Indeks baris (mulai dari 0) yang menentukan status Qualified/Not Qualified kandidat di GameManager. Contoh: 3 untuk baris ke-4.")]
    public int finalDecisionRowIndex = 3;

    public void ResetEvaluation()
    {
        foreach (ExcelEvaluationRow row in evaluationRows)
        {
            if (row.qualifiedToggle != null && row.qualifiedToggle.isOn)
            {
                row.qualifiedToggle.isOn = false;
            }

            if (row.notQualifiedToggle != null && row.notQualifiedToggle.isOn)
            {
                row.notQualifiedToggle.isOn = false;
            }
        }
    }

    private void Start()
    {
        // Daftarkan listener untuk masing-masing baris (total 4 baris)
        for (int i = 0; i < evaluationRows.Count; i++)
        {
            int rowIndex = i; // Simpan salinan lokal index untuk closure
            ExcelEvaluationRow row = evaluationRows[rowIndex];

            if (row.qualifiedToggle != null)
            {
                row.qualifiedToggle.onValueChanged.AddListener((isOn) => OnRowToggleChanged(rowIndex, true, isOn));
            }

            if (row.notQualifiedToggle != null)
            {
                row.notQualifiedToggle.onValueChanged.AddListener((isOn) => OnRowToggleChanged(rowIndex, false, isOn));
            }
        }
    }

    /// <summary>
    /// Dipanggil setiap kali salah satu toggle di baris manapun berubah.
    /// </summary>
    private void OnRowToggleChanged(int rowIndex, bool isQualifiedToggle, bool isOn)
    {
        ExcelEvaluationRow row = evaluationRows[rowIndex];

        if (isOn)
        {
            // Jika toggle ini dicentang (ON), matikan pasangannya di baris yang sama tanpa memicu loop event
            if (isQualifiedToggle && row.notQualifiedToggle != null)
            {
                row.notQualifiedToggle.SetIsOnWithoutNotify(false);
            }
            else if (!isQualifiedToggle && row.qualifiedToggle != null)
            {
                row.qualifiedToggle.SetIsOnWithoutNotify(false);
            }
        }

        // Cek apakah keempat baris sudah terisi semua
        EvaluateAllRows();
    }

    /// <summary>
    /// Mengecek apakah semua baris sudah memiliki 1 pilihan, lalu mengirim hasil baris penilaian utama ke GameManager.
    /// </summary>
    private void EvaluateAllRows()
    {
        // 1. Pastikan keempat baris sudah terisi (salah satu dari Qualified atau Not Qualified harus ON)
        for (int i = 0; i < evaluationRows.Count; i++)
        {
            bool qOn = evaluationRows[i].qualifiedToggle != null && evaluationRows[i].qualifiedToggle.isOn;
            bool nqOn = evaluationRows[i].notQualifiedToggle != null && evaluationRows[i].notQualifiedToggle.isOn;

            // Jika ada satu saja baris yang masih kosong, hentikan proses (tombol Sudahi Interview tetap terkunci)
            if (!qOn && !nqOn)
            {
                return;
            }
        }

        // 2. Jika keempat baris sudah terisi semua, ambil nilai dari 1 baris penilaian utama
        if (finalDecisionRowIndex >= 0 && finalDecisionRowIndex < evaluationRows.Count)
        {
            ExcelEvaluationRow finalRow = evaluationRows[finalDecisionRowIndex];
            bool isFinalQualified = finalRow.qualifiedToggle != null && finalRow.qualifiedToggle.isOn;

            // Kirim status akhir ke GameManager sekaligus membuka tombol "Sudahi Interview"
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCurrentCandidateQualification(isFinalQualified);
            }
        }
        else
        {
            Debug.LogWarning("[ExcelSheetPrefab] Final Decision Row Index di luar batas jumlah baris!");
        }
    }
}
