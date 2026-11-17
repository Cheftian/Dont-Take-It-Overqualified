using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ExpressionData
{
    public string expressionId; // Contoh: "neutral", "smile", "nervous", "angry"
    public Sprite expressionSprite;
}

[System.Serializable]
public class CharacterProfile
{
    [Header("Identitas & Status")]
    public string characterId;      // ID unik yang direferensikan oleh NarrationData
    public string displayName;      // Nama yang tampil di kotak dialog
    public bool isOrangDalam;       // Penentu ending A atau B

    [Header("Data Narasi (Intro, Skill, Salary, Outro)")]
    public NarrationData narrationData;

    [Header("Visual & Ekspresi")]
    public SpriteRenderer characterSpriteRenderer;
    public Sprite defaultSprite;    // Sprite default saat karakter pertama kali muncul
    public List<ExpressionData> expressions = new List<ExpressionData>();
}

public class CharacterManager : MonoBehaviour
{
    [Header("Komponen Visual 2D Utama")]
    [Tooltip("Fallback untuk profil yang belum memiliki SpriteRenderer sendiri")]
    public SpriteRenderer characterSpriteRenderer;

    [Header("Database Karakter")]
    public List<CharacterProfile> characterDatabase = new List<CharacterProfile>();

    [Header("State Kandidat Aktif")]
    private CharacterProfile currentCandidate;

    public CharacterProfile CurrentCandidate => currentCandidate;
    public SpriteRenderer CurrentCharacterSpriteRenderer =>
        currentCandidate != null ? GetCharacterSpriteRenderer(currentCandidate) : null;
    public bool IsCurrentCandidateOrangDalam => currentCandidate != null && currentCandidate.isOrangDalam;

    /// <summary>
    /// Memuat data dan visual kandidat berdasarkan characterId.
    /// </summary>
    public CharacterProfile SetupCandidate(string characterId)
    {
        CharacterProfile profile = GetCharacterProfile(characterId);
        if (profile != null)
        {
            currentCandidate = profile;
            SpriteRenderer profileRenderer = GetCharacterSpriteRenderer(profile);

            foreach (CharacterProfile character in characterDatabase)
            {
                if (character.characterSpriteRenderer != null && character.characterSpriteRenderer != profileRenderer)
                {
                    character.characterSpriteRenderer.enabled = false;
                }
            }

            if (characterSpriteRenderer != null && characterSpriteRenderer != profileRenderer)
            {
                characterSpriteRenderer.enabled = false;
            }

            if (profileRenderer != null)
            {
                profileRenderer.sprite = profile.defaultSprite;
                profileRenderer.enabled = true;
            }
        }
        else
        {
            Debug.LogWarning($"[CharacterManager] Profil untuk characterId '{characterId}' tidak ditemukan!");
        }

        return currentCandidate;
    }

    /// <summary>
    /// Mengubah ekspresi karakter berdasarkan characterId dari NarrationData dan expressionId.
    /// </summary>
    public void SetExpression(string characterId, string expressionId)
    {
        CharacterProfile profile = GetCharacterProfile(characterId);
        if (profile == null) return;

        SpriteRenderer profileRenderer = GetCharacterSpriteRenderer(profile);
        if (profileRenderer == null) return;

        if (string.IsNullOrEmpty(expressionId))
        {
            profileRenderer.sprite = profile.defaultSprite;
            return;
        }

        ExpressionData foundExpression = profile.expressions.Find(e => e.expressionId == expressionId);
        if (foundExpression != null && foundExpression.expressionSprite != null)
        {
            profileRenderer.sprite = foundExpression.expressionSprite;
        }
        else
        {
            Debug.LogWarning($"[CharacterManager] Ekspresi '{expressionId}' tidak ditemukan pada '{characterId}'. Menggunakan default.");
            profileRenderer.sprite = profile.defaultSprite;
        }
    }

    private SpriteRenderer GetCharacterSpriteRenderer(CharacterProfile profile)
    {
        return profile.characterSpriteRenderer != null ? profile.characterSpriteRenderer : characterSpriteRenderer;
    }

    public CharacterProfile GetCharacterProfile(string characterId)
    {
        return characterDatabase.Find(c => c.characterId == characterId);
    }
}
