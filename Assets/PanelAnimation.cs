using System.Collections;
using UnityEngine;

public class PanelAnimation : MonoBehaviour
{
    [Header("Objek Karakter & Sekuriti (2D)")]
    [Tooltip("Transform dari GameObject karakter yang memiliki SpriteRenderer")]
    public Transform characterTransform;

    [Tooltip("Transform dari GameObject sprite tangan/karakter sekuriti")]
    public Transform securityHandTransform;

    [Header("Pengaturan Posisi (World Space)")]
    [Tooltip("Posisi X saat bersembunyi di luar layar kanan")]
    public float offscreenRightX = 10f;

    [Tooltip("Posisi X karakter saat berdiri di depan meja wawancara")]
    public float centerInterviewX = 0f;

    [Tooltip("Offset X posisi tangan sekuriti saat mencengkeram karakter (misal: sedikit di kanan karakter)")]
    public float securityGrabOffsetX = 1.2f;

    [Header("Durasi Animasi")]
    public float slideDuration = 0.6f;
    public float securityReachDuration = 0.35f; // Kecepatan tangan sekuriti masuk
    public float securityPullDuration = 0.5f;   // Kecepatan sekuriti menarik karakter keluar

    private void Start()
    {
        // Pastikan tangan sekuriti berada di luar layar kanan saat game dimulai
        if (securityHandTransform != null)
        {
            securityHandTransform.position = new Vector3(
                offscreenRightX,
                securityHandTransform.position.y,
                securityHandTransform.position.z
            );
        }
    }

    /// <summary>
    /// Animasi karakter masuk dari luar layar kanan menuju tengah (slide ke kiri).
    /// </summary>
    public IEnumerator SlideCharacterIn()
    {
        if (characterTransform == null) yield break;

        Vector3 startPos = new Vector3(offscreenRightX, characterTransform.position.y, characterTransform.position.z);
        Vector3 targetPos = new Vector3(centerInterviewX, characterTransform.position.y, characterTransform.position.z);

        characterTransform.position = startPos;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            characterTransform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            yield return null;
        }

        characterTransform.position = targetPos;
    }

    /// <summary>
    /// Animasi karakter keluar normal menuju luar layar kanan (slide ke kanan).
    /// </summary>
    public IEnumerator SlideCharacterOut()
    {
        if (characterTransform == null) yield break;

        Vector3 startPos = characterTransform.position;
        Vector3 targetPos = new Vector3(offscreenRightX, characterTransform.position.y, characterTransform.position.z);

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            characterTransform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            yield return null;
        }

        characterTransform.position = targetPos;
    }

    /// <summary>
    /// Animasi tangan sekuriti masuk dari kanan ke arah karakter, lalu menarik karakter keluar ke kanan.
    /// </summary>
    public IEnumerator SlideSecurityPullOut()
    {
        if (characterTransform == null || securityHandTransform == null)
        {
            // Fallback jika sprite tangan sekuriti belum dipasang
            yield return StartCoroutine(SlideCharacterOut());
            yield break;
        }

        // --- FASE 1: Tangan sekuriti bergerak dari kanan menuju karakter ---
        Vector3 handStartPos = new Vector3(offscreenRightX, securityHandTransform.position.y, securityHandTransform.position.z);
        float grabX = characterTransform.position.x + securityGrabOffsetX;
        Vector3 handGrabPos = new Vector3(grabX, securityHandTransform.position.y, securityHandTransform.position.z);

        securityHandTransform.position = handStartPos;

        float elapsed = 0f;
        while (elapsed < securityReachDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / securityReachDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            securityHandTransform.position = Vector3.Lerp(handStartPos, handGrabPos, smoothT);
            yield return null;
        }
        securityHandTransform.position = handGrabPos;

        // Jeda singkat sepersekian detik saat tangan mencengkeram karakter
        yield return new WaitForSeconds(0.15f);

        // --- FASE 2: Tangan sekuriti dan karakter bergerak bersamaan ke kanan ---
        Vector3 charPullStart = characterTransform.position;
        Vector3 charPullTarget = new Vector3(offscreenRightX, characterTransform.position.y, characterTransform.position.z);

        Vector3 handPullStart = securityHandTransform.position;
        Vector3 handPullTarget = new Vector3(offscreenRightX + securityGrabOffsetX, securityHandTransform.position.y, securityHandTransform.position.z);

        elapsed = 0f;
        while (elapsed < securityPullDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / securityPullDuration);
            // Menggunakan t * t (ease-in) agar terasa seperti tarikan bertenaga
            float pullT = t * t;

            characterTransform.position = Vector3.Lerp(charPullStart, charPullTarget, pullT);
            securityHandTransform.position = Vector3.Lerp(handPullStart, handPullTarget, pullT);
            yield return null;
        }

        characterTransform.position = charPullTarget;
        securityHandTransform.position = handPullTarget;
    }
}
