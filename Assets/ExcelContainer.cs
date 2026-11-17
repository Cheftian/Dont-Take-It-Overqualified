using UnityEngine;

public class ExcelPanelManager : MonoBehaviour
{
    [Header("Referensi GameObject Excel")]
    [Tooltip("GameObject utama Excel yang sudah ada di dalam scene")]
    public GameObject excelMainGameObject;

    public GameObject ExcelMainGameObject => excelMainGameObject;

    public void ResetExcelEvaluation()
    {
        if (excelMainGameObject == null)
        {
            Debug.LogError("[ExcelPanelManager] GameObject utama Excel belum diisi di Inspector!");
            return;
        }

        ExcelSheetPrefab excelSheet = excelMainGameObject.GetComponent<ExcelSheetPrefab>();
        if (excelSheet != null)
        {
            excelSheet.ResetEvaluation();
        }
        else
        {
            Debug.LogError("[ExcelPanelManager] GameObject utama Excel tidak memiliki ExcelSheetPrefab!");
        }
    }

    public void ToggleExcelVisibility()
    {
        if (excelMainGameObject != null)
        {
            excelMainGameObject.SetActive(!excelMainGameObject.activeSelf);
        }
    }

    public void ShowExcelPanel()
    {
        if (excelMainGameObject != null)
        {
            excelMainGameObject.SetActive(true);
        }
    }
}
