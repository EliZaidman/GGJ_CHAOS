#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SoundIdGenerator
{
    private const string OutputFolder = "Assets/JamAudio/Generated";
    private const string OutputFileName = "SoundId.cs";            // keep your current name
    private static readonly string OutputPath = $"{OutputFolder}/{OutputFileName}";

    // If you previously used different names, include them here so we clean them too.
    private static readonly string[] LegacyFileNames =
    {
        "SoundId.cs",
        "SoundId.generated.cs",
        "SoundId.Generated.cs"
    };

    [MenuItem("Tools/JamAudio/Regenerate SoundId Enum")]
    public static void Regenerate()
    {
        var library = FindSoundLibrary();
        if (library == null)
        {
            Debug.LogError("[SoundIdGenerator] No SoundLibrary found. Create one and add cues.");
            return;
        }

        if (library.cues == null)
        {
            Debug.LogError("[SoundIdGenerator] SoundLibrary.cues is null.");
            return;
        }

        Directory.CreateDirectory(OutputFolder);

        // ✅ Ensure there is exactly ONE generated SoundId file:
        DeleteAllSoundIdGeneratedFilesExcept(OutputPath);

        var used = new HashSet<string>(StringComparer.Ordinal);
        var names = new List<string>();

        for (int i = 0; i < library.cues.Count; i++)
        {
            var cue = library.cues[i];
            if (cue == null)
            {
                names.Add($"Missing_{i}");
                continue;
            }

            var raw = cue.GetEffectiveIdName();
            var safe = MakeSafeEnumName(raw);

            if (string.IsNullOrWhiteSpace(safe))
                safe = $"Sound_{i}";

            // Ensure unique
            var finalName = safe;
            int suffix = 1;
            while (!used.Add(finalName))
            {
                suffix++;
                finalName = $"{safe}_{suffix}";
            }

            names.Add(finalName);
        }

        var sb = new StringBuilder();
        sb.AppendLine("// AUTO-GENERATED. Do not edit by hand.");
        sb.AppendLine("// Source of truth: the order of cues in your SoundLibrary.");
        sb.AppendLine();
        sb.AppendLine("public enum SoundId");
        sb.AppendLine("{");
        for (int i = 0; i < names.Count; i++)
            sb.AppendLine($"    {names[i]} = {i},");
        sb.AppendLine("}");

        // Write (overwrite). If it existed, it is already deleted via AssetDatabase,
        // but writing is fine either way.
        File.WriteAllText(OutputPath, sb.ToString(), Encoding.UTF8);

        AssetDatabase.ImportAsset(OutputPath);
        AssetDatabase.Refresh();

        Debug.Log($"[SoundIdGenerator] Generated {names.Count} ids at {OutputPath}");
    }

    /// <summary>
    /// Deletes any SoundId enum files we consider "generated" duplicates,
    /// so only the one at keepPath remains.
    /// </summary>
    private static void DeleteAllSoundIdGeneratedFilesExcept(string keepPath)
    {
        // Normalize for comparisons
        keepPath = keepPath.Replace("\\", "/");

        // 1) Delete any matching filenames anywhere under Assets/
        // We do a broad search and then filter by filename.
        var csGuids = AssetDatabase.FindAssets("SoundId t:TextAsset");
        for (int i = 0; i < csGuids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(csGuids[i]).Replace("\\", "/");
            if (string.IsNullOrEmpty(path)) continue;

            var fileName = Path.GetFileName(path);
            if (!IsLegacyName(fileName)) continue;

            // Don't delete the keep path (we’ll overwrite it anyway, but keep meta stable)
            if (string.Equals(path, keepPath, StringComparison.OrdinalIgnoreCase))
            {
                // If you want to strictly delete then recreate, uncomment:
                // AssetDatabase.DeleteAsset(path);
                continue;
            }

            // Extra safety: only delete if it's in a Generated folder or inside JamAudio.
            // If you want FULL enforcement (delete any duplicates anywhere), remove this guard.
            bool looksGenerated =
                path.Contains("/Generated/", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("/JamAudio/", StringComparison.OrdinalIgnoreCase);

            if (!looksGenerated)
                continue;

            AssetDatabase.DeleteAsset(path);
        }

        // 2) If keepPath exists but you want "delete before create", do it here:
        // This guarantees a clean rewrite every time.
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(keepPath) != null)
        {
            AssetDatabase.DeleteAsset(keepPath);
        }

        // Make sure folder still exists after deletions
        Directory.CreateDirectory(OutputFolder);
    }

    private static bool IsLegacyName(string fileName)
    {
        for (int i = 0; i < LegacyFileNames.Length; i++)
        {
            if (string.Equals(fileName, LegacyFileNames[i], StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static SoundLibrary FindSoundLibrary()
    {
        var guids = AssetDatabase.FindAssets("t:SoundLibrary");
        if (guids == null || guids.Length == 0) return null;

        // If you have more than one, picks the first.
        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<SoundLibrary>(path);
    }

    private static string MakeSafeEnumName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";

        raw = raw.Trim();
        raw = raw.Replace("SC_", "", StringComparison.OrdinalIgnoreCase);
        raw = raw.Replace("SL_", "", StringComparison.OrdinalIgnoreCase);

        var sb = new StringBuilder(raw.Length);
        bool first = true;

        foreach (char c in raw)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                if (first && char.IsDigit(c))
                    sb.Append('_');

                sb.Append(c);
                first = false;
            }
            else
            {
                if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                    sb.Append('_');
                first = false;
            }
        }

        while (sb.Length > 0 && sb[sb.Length - 1] == '_')
            sb.Length--;

        return sb.ToString();
    }
}
#endif
