using System.Collections;
using UnityEngine;

public class UIPopupAnimator : MonoBehaviour
{
    public enum PopupDirection
    {
        Kiri,
        Tengah,
        Kanan
    }

    [Header("Pengaturan Animasi")]
    [Tooltip("Pilih arah munculnya animasi pop up")]
    public PopupDirection direksi = PopupDirection.Tengah;

    [Tooltip("Durasi animasi dalam detik")]
    public float durasi = 0.3f;

    [Tooltip("Kurva animasi agar gerakan lebih halus (Ease Out/In)")]
    public AnimationCurve kurvaAnimasi = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Nonaktifkan GameObject otomatis setelah animasi close selesai")]
    public bool nonaktifkanSaatSelesaiClose = true;

    private RectTransform rectTransform;
    private Vector3 skalaAwal;
    private Coroutine animasiCoroutine;
    private bool sudahInisialisasi = false;

    private void Awake()
    {
        Inisialisasi();
    }

    private void Inisialisasi()
    {
        if (sudahInisialisasi) return;

        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Simpan skala asli dari UI Image
            skalaAwal = rectTransform.localScale;
            // Jika skala awal di inspector kebetulan 0, jadikan (1,1,1) sebagai default
            if (skalaAwal == Vector3.zero) skalaAwal = Vector3.one;
        }
        sudahInisialisasi = true;
    }

    // Otomatis berjalan setiap kali GameObject UI Image diaktifkan (SetActive(true))
    private void OnEnable()
    {
        Inisialisasi();
        AturPivotTanpaMengubahPosisi();
        MulaiAnimasi(Vector3.zero, skalaAwal, false);
    }

    /// <summary>
    /// Panggil fungsi ini pada event OnClick() di Button Close
    /// </summary>
    public void ClosePopup()
    {
        if (!gameObject.activeInHierarchy) return;

        AturPivotTanpaMengubahPosisi();
        MulaiAnimasi(rectTransform.localScale, Vector3.zero, nonaktifkanSaatSelesaiClose);
    }

    private void MulaiAnimasi(Vector3 dariSkala, Vector3 keSkala, bool matikanObjekSetelahnya)
    {
        if (animasiCoroutine != null)
        {
            StopCoroutine(animasiCoroutine);
        }
        animasiCoroutine = StartCoroutine(ProsesAnimasi(dariSkala, keSkala, matikanObjekSetelahnya));
    }

    private IEnumerator ProsesAnimasi(Vector3 dariSkala, Vector3 keSkala, bool matikanObjekSetelahnya)
    {
        float waktuBerjalan = 0f;
        rectTransform.localScale = dariSkala;

        while (waktuBerjalan < durasi)
        {
            // Menggunakan unscaledDeltaTime agar animasi tetap jalan meski game di-pause (Time.timeScale = 0)
            waktuBerjalan += Time.unscaledDeltaTime;
            float persentase = Mathf.Clamp01(waktuBerjalan / durasi);
            float nilaiKurva = kurvaAnimasi.Evaluate(persentase);

            rectTransform.localScale = Vector3.LerpUnclamped(dariSkala, keSkala, nilaiKurva);
            yield return null;
        }

        rectTransform.localScale = keSkala;
        animasiCoroutine = null;

        if (matikanObjekSetelahnya)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Mengubah nilai Pivot (X) sesuai direksi sambil menjaga posisi UI tetap di tempatnya
    /// </summary>
    private void AturPivotTanpaMengubahPosisi()
    {
        if (rectTransform == null) return;

        float targetPivotX = 0.5f; // Default Tengah

        switch (direksi)
        {
            case PopupDirection.Kiri:
                targetPivotX = 0f;
                break;
            case PopupDirection.Tengah:
                targetPivotX = 0.5f;
                break;
            case PopupDirection.Kanan:
                targetPivotX = 1f;
                break;
        }

        Vector2 pivotBaru = new Vector2(targetPivotX, 0.5f);
        Vector2 selisihPivot = pivotBaru - rectTransform.pivot;

        // Hitung kompensasi posisi agar objek tidak bergeser saat pivot berubah
        Vector3 deltaPosisi = new Vector3(
            selisihPivot.x * rectTransform.rect.width * skalaAwal.x,
            selisihPivot.y * rectTransform.rect.height * skalaAwal.y,
            0f
        );

        rectTransform.pivot = pivotBaru;
        rectTransform.localPosition += deltaPosisi;
    }
}
