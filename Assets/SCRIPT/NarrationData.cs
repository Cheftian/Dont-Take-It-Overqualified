using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public bool isHRD;           // Centang jika pembicara adalah HRD (hanya teks, tanpa ekspresi)
    public string expressionId;  // ID ekspresi milik targetCharacterId (diabaikan jika isHRD = true)
    [TextArea(2, 5)]
    public string sentence;
}

[CreateAssetMenu(fileName = "NewNarrationData", menuName = "HRD Game/Narration Data")]
public class NarrationData : ScriptableObject
{
    [Header("Referensi Karakter")]
    [Tooltip("Harus sama dengan characterId yang ada di CharacterManager")]
    public string targetCharacterId;

    [Header("Alur Dialog 2 Arah")]
    public List<DialogueLine> introLines = new List<DialogueLine>();
    public List<DialogueLine> skillLines = new List<DialogueLine>();
    public List<DialogueLine> salaryLines = new List<DialogueLine>();
    public List<DialogueLine> outroLines = new List<DialogueLine>();
}
