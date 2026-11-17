using UnityEngine;

public class ExcelPanelManager : MonoBehaviour
{
    [Header("Referensi Prefab & Tempat Spawn")]
    [Tooltip("Masukkan Prefab Panel Excel dari folder Project")]
    public GameObject excelPanelPrefab;

    [Tooltip("Parent di dalam Canvas tempat Prefab Excel akan di-spawn")]
    public Transform excelParentContainer;

    private GameObject currentExcelInstance;

    /// <summary>
    /// Mengganti panel Excel kandidat lama dengan instance baru yang semua togglenya kosong.
    /// Dipanggil oleh GameManager setiap kali kandidat baru masuk.
    /// </summary>
    public void SpawnFreshExcelPanel()
    {
        bool hadPreviousInstance = currentExcelInstance != null;
        bool wasVisible = hadPreviousInstance && currentExcelInstance.activeSelf;

        if (hadPreviousInstance)
        {
            ExcelSheetPrefab previousExcelSheet = currentExcelInstance.GetComponent<ExcelSheetPrefab>();
            if (previousExcelSheet != null)
            {
                previousExcelSheet.ResetEvaluation();
            }

            Destroy(currentExcelInstance);
        }

        if (excelPanelPrefab != null)
        {
            Transform parent = excelParentContainer != null ? excelParentContainer : transform;
            currentExcelInstance = Instantiate(excelPanelPrefab, parent);
            if (hadPreviousInstance)
            {
                currentExcelInstance.SetActive(wasVisible);
            }

            ExcelSheetPrefab excelSheet = currentExcelInstance.GetComponent<ExcelSheetPrefab>();
            if (excelSheet != null)
            {
                excelSheet.ResetEvaluation();
            }
            else
            {
                Debug.LogWarning("[ExcelPanelManager] Excel Panel Prefab tidak memiliki ExcelSheetPrefab!");
            }
        }
        else
        {
            Debug.LogWarning("[ExcelPanelManager] Excel Panel Prefab belum diisi di Inspector!");
        }
    }

    /// <summary>
    /// Opsional: Jika kamu ingin menampilkan/menyembunyikan panel Excel lewat tombol ikon Excel.
    /// </summary>
    public void ToggleExcelVisibility()
    {
        if (currentExcelInstance != null)
        {
            currentExcelInstance.SetActive(!currentExcelInstance.activeSelf);
        }
    }
}
