using UnityEngine;
using UnityEngine.UI;

public class PhoneInteruption : MonoBehaviour
{
    [Header("Pengaturan Ponsel")]
    [Tooltip("Opsional: Isi jika objek ponsel adalah UI Button")]
    public Button phoneButton;

    [Tooltip("Panel telepon yang ditutup sebelum animasi sekuriti dimulai")]
    public GameObject phonePanel;

    private void Start()
    {
        // Jika menggunakan UI Button, otomatis hubungkan event klik
        if (phoneButton != null)
        {
            phoneButton.onClick.AddListener(CallSecurity);
        }
    }

    private void OnDestroy()
    {
        if (phoneButton != null)
        {
            phoneButton.onClick.RemoveListener(CallSecurity);
        }
    }

    // Jika ponsel menggunakan objek 2D Sprite + BoxCollider2D di Scene (bukan UI)
    private void OnMouseDown()
    {
        CallSecurity();
    }

    /// <summary>
    /// Memanggil sekuriti untuk menyeret kandidat keluar.
    /// Bisa juga dihubungkan langsung lewat OnClick() di Inspector UI Button.
    /// </summary>
    public void CallSecurity()
    {
        if (phonePanel != null)
        {
            phonePanel.SetActive(false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerSecurityRemoval();
        }
        else
        {
            Debug.LogWarning("[PhoneInteruption] GameManager tidak ditemukan di Scene!");
        }
    }
}
