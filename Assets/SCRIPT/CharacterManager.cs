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
    public Sprite defaultSprite;    // Sprite default saat karakter pertama kali muncul
    public List<ExpressionData> expressions = new List<ExpressionData>();
}

public class CharacterManager : MonoBehaviour
{
    [Header("Komponen Visual 2D Utama")]
    [Tooltip("Komponen SpriteRenderer pada GameObject karakter di Scene")]
    public SpriteRenderer characterSpriteRenderer;

    [Header("Database Karakter")]
    public List<CharacterProfile> characterDatabase = new List<CharacterProfile>();

    [Header("State Kandidat Aktif")]
    private CharacterProfile currentCandidate;

    public CharacterProfile CurrentCandidate => currentCandidate;
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

            if (characterSpriteRenderer != null)
            {
                characterSpriteRenderer.sprite = profile.defaultSprite;
                characterSpriteRenderer.enabled = true;
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
        if (profile == null || characterSpriteRenderer == null) return;

        if (string.IsNullOrEmpty(expressionId))
        {
            characterSpriteRenderer.sprite = profile.defaultSprite;
            return;
        }

        ExpressionData foundExpression = profile.expressions.Find(e => e.expressionId == expressionId);
        if (foundExpression != null && foundExpression.expressionSprite != null)
        {
            characterSpriteRenderer.sprite = foundExpression.expressionSprite;
        }
        else
        {
            Debug.LogWarning($"[CharacterManager] Ekspresi '{expressionId}' tidak ditemukan pada '{characterId}'. Menggunakan default.");
            characterSpriteRenderer.sprite = profile.defaultSprite;
        }
    }

    public CharacterProfile GetCharacterProfile(string characterId)
    {
        return characterDatabase.Find(c => c.characterId == characterId);
    }
}
