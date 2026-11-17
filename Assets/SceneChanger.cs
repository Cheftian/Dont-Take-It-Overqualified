using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Header("Pengaturan Scene")]
    [Tooltip("Isi nama scene tujuan secara tepat (sesuai yang ada di Build Settings)")]
    public string targetSceneName;

    /// <summary>
    /// Memuat scene berdasarkan variabel targetSceneName yang diisi di Inspector.
    /// </summary>
    public void LoadTargetScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("[SceneChanger] Nama targetSceneName masih kosong!");
        }
    }

    /// <summary>
    /// Memuat scene berdasarkan parameter teks yang dimasukkan langsung pada event Button OnClick().
    /// </summary>
    public void LoadSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("[SceneChanger] Nama scene parameter kosong!");
        }
    }

    /// <summary>
    /// Mengulang kembali scene yang sedang aktif saat ini (cocok untuk tombol Restart/Retry).
    /// </summary>
    public void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}
