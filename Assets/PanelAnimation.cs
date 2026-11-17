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

    [Tooltip("Offset X dari posisi awal setiap karakter saat berdiri di depan meja wawancara")]
    public float centerInterviewX = 0f;

    [Tooltip("Offset X posisi tangan sekuriti saat mencengkeram karakter (misal: sedikit di kanan karakter)")]
    public float securityGrabOffsetX = 1.2f;

    [Header("Durasi Animasi")]
    public float slideDuration = 0.6f;
    public float sitDuration = 0.18f;
    public float standDuration = 0.18f;
    public float securityReachDuration = 0.35f; // Kecepatan tangan sekuriti masuk
    public float securityPullDuration = 0.5f;   // Kecepatan sekuriti menarik karakter keluar

    [Header("Gerakan Karakter")]
    public float sitDropDistance = 0.12f;
    public float walkingWiggleAmount = 0.12f;
    public float walkingWiggleFrequency = 4f;
    public float talkingWiggleAmount = 0.06f;
    public float talkingWiggleFrequency = 5f;

    private Vector3 standingPosition;
    private Vector3 characterBaseScale;
    private Coroutine talkingWiggleCoroutine;

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

        Vector3 targetPos = characterTransform.position;
        targetPos.x += centerInterviewX;
        Vector3 startPos = new Vector3(offscreenRightX, targetPos.y, targetPos.z);
        standingPosition = targetPos;
        characterBaseScale = characterTransform.localScale;

        characterTransform.position = startPos;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            characterTransform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            ApplyVerticalCompression(Mathf.Abs(Mathf.Sin(elapsed * walkingWiggleFrequency * Mathf.PI * 2f)) * walkingWiggleAmount);
            yield return null;
        }

        characterTransform.position = targetPos;
        characterTransform.localScale = characterBaseScale;

        Vector3 seatedPosition = targetPos + Vector3.down * sitDropDistance;
        yield return StartCoroutine(AnimatePosition(targetPos, seatedPosition, sitDuration));
    }

    /// <summary>
    /// Karakter berdiri, lalu berjalan menuju luar layar kanan.
    /// </summary>
    public IEnumerator SlideCharacterOut()
    {
        if (characterTransform == null) yield break;

        StopTalkingAnimation();
        characterBaseScale = characterTransform.localScale;

        Vector3 seatedPosition = characterTransform.position;
        yield return StartCoroutine(AnimatePosition(seatedPosition, standingPosition, standDuration));

        Vector3 targetPos = new Vector3(offscreenRightX, standingPosition.y, standingPosition.z);
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            characterTransform.position = Vector3.Lerp(standingPosition, targetPos, smoothT);
            ApplyVerticalCompression(Mathf.Abs(Mathf.Sin(elapsed * walkingWiggleFrequency * Mathf.PI * 2f)) * walkingWiggleAmount);
            yield return null;
        }

        characterTransform.position = targetPos;
        characterTransform.localScale = characterBaseScale;
    }

    public void StartTalkingAnimation()
    {
        if (characterTransform == null || talkingWiggleCoroutine != null) return;

        characterBaseScale = characterTransform.localScale;
        talkingWiggleCoroutine = StartCoroutine(TalkingWiggle());
    }

    public void StopTalkingAnimation()
    {
        if (talkingWiggleCoroutine == null) return;

        StopCoroutine(talkingWiggleCoroutine);
        talkingWiggleCoroutine = null;

        if (characterTransform != null)
        {
            characterTransform.localScale = characterBaseScale;
        }
    }

    private IEnumerator TalkingWiggle()
    {
        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.deltaTime;
            ApplyVerticalCompression(Mathf.Abs(Mathf.Sin(elapsed * talkingWiggleFrequency * Mathf.PI * 2f)) * talkingWiggleAmount);
            yield return null;
        }
    }

    private IEnumerator AnimatePosition(Vector3 startPosition, Vector3 targetPosition, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            characterTransform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);
            yield return null;
        }

        characterTransform.position = targetPosition;
    }

    private void ApplyVerticalCompression(float amount)
    {
        Vector3 scale = characterBaseScale;
        scale.y *= 1f - amount;
        characterTransform.localScale = scale;
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

        StopTalkingAnimation();

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
