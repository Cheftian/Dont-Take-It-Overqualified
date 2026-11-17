using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class NarrationCsvImporter : EditorWindow
{
    private TextAsset csvFile;
    private string outputFolder = "Assets/Data/Narrations";

    [MenuItem("HRD Game/Import Narration CSV")]
    public static void ShowWindow()
    {
        GetWindow<NarrationCsvImporter>("CSV to NarrationData");
    }

    private void OnGUI()
    {
        GUILayout.Label("Generator NarrationData dari CSV", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        csvFile = (TextAsset)EditorGUILayout.ObjectField("File CSV", csvFile, typeof(TextAsset), false);
        outputFolder = EditorGUILayout.TextField("Folder Tujuan", outputFolder);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate NarrationData Assets", GUILayout.Height(35)))
        {
            if (csvFile == null)
            {
                EditorUtility.DisplayDialog("Error", "Silakan masukkan file .csv terlebih dahulu!", "OK");
                return;
            }

            ImportCSV();
        }
    }

    private void ImportCSV()
    {
        // Pastikan folder tujuan tersedia
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }

        // Dictionary untuk mengelompokkan data berdasarkan Prefix ID (misal: "BIGBOS", "ADRIAN")
        Dictionary<string, NarrationData> groupedData = new Dictionary<string, NarrationData>();

        // Pisahkan per baris (mendukung CRLF dan LF)
        string[] lines = csvFile.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

        // Mulai dari index 1 untuk melewati baris header (ID,Type,Character,Expression,DialogueED)
        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = SplitCsvLine(lines[i]);
            if (cols.Length < 5) continue;

            string rawId = cols[0].Trim();        // Contoh: "ADRIAN-001"
            string type = cols[1].Trim();         // Contoh: "Intro"
            string character = cols[2].Trim();    // Contoh: "Adrian" atau "HRD"
            string expression = cols[3].Trim();   // Contoh: "Normal" atau "-"
            string dialogue = cols[4].Trim();     // Kalimat dialog

            // Ambil prefix sebelum tanda '-' sebagai kunci pengelompokan file (misal: "ADRIAN-001" -> "ADRIAN")
            string groupKey = rawId.Contains("-") ? rawId.Split('-')[0] : rawId;

            if (!groupedData.ContainsKey(groupKey))
            {
                NarrationData newData = ScriptableObject.CreateInstance<NarrationData>();
                groupedData[groupKey] = newData;
            }

            NarrationData currentAsset = groupedData[groupKey];

            // Cek apakah pembicara adalah HRD
            bool isHrdSpeaker = character.Equals("HRD", System.StringComparison.OrdinalIgnoreCase);

            // Jika bukan HRD, set targetCharacterId pada aset sesuai nama karakter tersebut
            if (!isHrdSpeaker && string.IsNullOrEmpty(currentAsset.targetCharacterId))
            {
                currentAsset.targetCharacterId = character;
            }

            // Buat baris dialog baru
            DialogueLine newLine = new DialogueLine
            {
                isHRD = isHrdSpeaker,
                expressionId = (isHrdSpeaker || expression == "-") ? "" : expression,
                sentence = dialogue
            };

            // Masukkan ke list yang sesuai berdasarkan kolom Type
            switch (type.ToLower())
            {
                case "intro":
                    currentAsset.introLines.Add(newLine);
                    break;
                case "skill":
                    currentAsset.skillLines.Add(newLine);
                    break;
                case "salary":
                    currentAsset.salaryLines.Add(newLine);
                    break;
                case "outro":
                    currentAsset.outroLines.Add(newLine);
                    break;
                default:
                    Debug.LogWarning($"[CSV Importer] Type '{type}' tidak dikenali pada baris {i + 1}");
                    break;
            }
        }

        // Simpan semua ScriptableObject ke dalam folder tujuan
        int createdCount = 0;
        foreach (var kvp in groupedData)
        {
            string fileName = $"{kvp.Key}.asset"; // Menghasilkan BIGBOS.asset, ADRIAN.asset, dst.
            string fullPath = Path.Combine(outputFolder, fileName);

            // Hapus file lama jika sudah ada agar data ter-update bersih
            if (AssetDatabase.LoadAssetAtPath<NarrationData>(fullPath) != null)
            {
                AssetDatabase.DeleteAsset(fullPath);
            }

            AssetDatabase.CreateAsset(kvp.Value, fullPath);
            createdCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Selesai!", $"Berhasil membuat {createdCount} aset NarrationData di:\n{outputFolder}", "Mantap");
    }

    /// <summary>
    /// Memecah baris CSV dengan aman meskipun ada tanda koma di dalam tanda kutip ("...")
    /// </summary>
    private string[] SplitCsvLine(string line)
    {
        string pattern = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";
        string[] fields = Regex.Split(line, pattern);

        for (int i = 0; i < fields.Length; i++)
        {
            fields[i] = fields[i].Trim();
            // Hapus tanda kutip pembungkus di awal dan akhir kalimat jika ada
            if (fields[i].StartsWith("\"") && fields[i].EndsWith("\"") && fields[i].Length >= 2)
            {
                fields[i] = fields[i].Substring(1, fields[i].Length - 2);
            }
            // Ubah double quote "" menjadi single "
            fields[i] = fields[i].Replace("\"\"", "\"");
        }

        return fields;
    }
}
