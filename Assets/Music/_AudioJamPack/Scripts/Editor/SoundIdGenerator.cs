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
    private const string OutputPath = OutputFolder + "/SoundId.cs";

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
        {
            sb.AppendLine($"    {names[i]} = {i},");
        }

        sb.AppendLine("}");

        File.WriteAllText(OutputPath, sb.ToString(), Encoding.UTF8);

        AssetDatabase.Refresh();
        Debug.Log($"[SoundIdGenerator] Generated {names.Count} ids at {OutputPath}");
    }

    private static SoundLibrary FindSoundLibrary()
    {
        var guids = AssetDatabase.FindAssets("t:SoundLibrary");
        if (guids == null || guids.Length == 0) return null;

        // If you have more than one, just picks the first.
        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<SoundLibrary>(path);
    }

    private static string MakeSafeEnumName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";

        // Remove common prefixes people use in asset names
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
                    sb.Append('_'); // enum can't start with digit

                sb.Append(c);
                first = false;
            }
            else
            {
                // turn spaces/dashes/etc into underscore
                if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                    sb.Append('_');
                first = false;
            }
        }

        // Trim trailing underscores
        while (sb.Length > 0 && sb[sb.Length - 1] == '_')
            sb.Length--;

        return sb.ToString();
    }
}
#endif
