#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SoundIdGenerator
{
    // Where the generated enum lives (single source of truth)
    private const string GeneratedFolder = "Assets/JamAudio/Generated";
    private const string GeneratedEnumFile = "SoundId.generated.cs";
    private static readonly string GeneratedEnumPath = $"{GeneratedFolder}/{GeneratedEnumFile}";

    // Where we keep the one SoundLibrary asset (created automatically if missing)
    private const string LibraryFolder = "Assets/JamAudio";
    private const string LibraryAssetName = "SoundLibrary.asset";
    private static readonly string LibraryAssetPath = $"{LibraryFolder}/{LibraryAssetName}";

    // Any legacy names we want to delete so there is exactly one enum definition
    private static readonly string[] EnumFileNamesToDelete =
    {
        "SoundId.cs",
        "SoundId.generated.cs",
        "SoundId.Generated.cs"
    };

    [MenuItem("Tools/JamAudio/Update SoundLibrary + Regenerate SoundId Enum")]
    public static void UpdateLibraryThenRegenerateEnum()
    {
        // 0) Ensure folders exist
        Directory.CreateDirectory(LibraryFolder);
        Directory.CreateDirectory(GeneratedFolder);

        // 1) Get (or create) the SoundLibrary we will maintain
        var library = FindOrCreateSingleSoundLibrary();
        if (library == null)
        {
            Debug.LogError("[SoundIdGenerator] Failed to find or create SoundLibrary.");
            return;
        }

        // 2) Scan the entire project for all SoundCue assets (deterministic order)
        var allCues = FindAllSoundCuesSortedByPath();

        // 3) Update library first (no duplicates, deterministic)
        bool libraryChanged = UpdateLibraryContents(library, allCues);

        // 4) Ensure exactly ONE generated enum file exists
        DeleteAllEnumDuplicatesExcept(GeneratedEnumPath);

        // 5) Generate enum based on updated library order
        GenerateEnumFromLibrary(library, GeneratedEnumPath);

        // 6) Force refresh + save (Unity 6 friendly)
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(GeneratedEnumPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        Debug.Log(
            $"[SoundIdGenerator] ✅ Done.\n" +
            $"- Found SoundCues: {allCues.Count}\n" +
            $"- Library updated: {(libraryChanged ? "YES" : "NO")}\n" +
            $"- Library path: {AssetDatabase.GetAssetPath(library)}\n" +
            $"- Enum path: {GeneratedEnumPath}"
        );
    }

    // ─────────────────────────────────────────────────────────────
    // Step 1: Library (find/create one)
    // ─────────────────────────────────────────────────────────────

    private static SoundLibrary FindOrCreateSingleSoundLibrary()
    {
        // If there are multiple, prefer the one at LibraryAssetPath if present
        var existingAtPath = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryAssetPath);
        if (existingAtPath != null)
            return existingAtPath;

        // Otherwise find any SoundLibrary in project
        var guids = AssetDatabase.FindAssets("t:SoundLibrary");
        if (guids != null && guids.Length > 0)
        {
            var firstPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var found = AssetDatabase.LoadAssetAtPath<SoundLibrary>(firstPath);
            if (found != null)
            {
                // Optional: move/copy to our canonical path? (minimum change: do NOT move)
                return found;
            }
        }

        // Create new one at the canonical location
        var lib = ScriptableObject.CreateInstance<SoundLibrary>();
        // Ensure list is not null (in case class doesn't init it)
        if (lib.cues == null) lib.cues = new List<SoundCue>();

        AssetDatabase.CreateAsset(lib, LibraryAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return lib;
    }

    // ─────────────────────────────────────────────────────────────
    // Step 2: Find all SoundCues in project
    // ─────────────────────────────────────────────────────────────

    private static List<SoundCue> FindAllSoundCuesSortedByPath()
    {
        var guids = AssetDatabase.FindAssets("t:SoundCue");
        var paths = new List<string>(guids?.Length ?? 0);

        if (guids != null)
        {
            for (int i = 0; i < guids.Length; i++)
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
        }

        // Deterministic ordering across machines/Unity versions
        paths.Sort(StringComparer.OrdinalIgnoreCase);

        var result = new List<SoundCue>(paths.Count);
        for (int i = 0; i < paths.Count; i++)
        {
            var cue = AssetDatabase.LoadAssetAtPath<SoundCue>(paths[i]);
            if (cue != null)
                result.Add(cue);
        }

        return result;
    }

    // ─────────────────────────────────────────────────────────────
    // Step 3: Update library list
    // ─────────────────────────────────────────────────────────────

    private static bool UpdateLibraryContents(SoundLibrary library, List<SoundCue> allCues)
    {
        if (library.cues == null)
            library.cues = new List<SoundCue>();

        // Build unique set (no duplicates)
        var unique = new HashSet<SoundCue>();
        var newList = new List<SoundCue>(allCues.Count);

        for (int i = 0; i < allCues.Count; i++)
        {
            var cue = allCues[i];
            if (cue == null) continue;

            if (unique.Add(cue))
                newList.Add(cue);
        }

        // Compare to existing list
        bool changed = !ListsEqualByReference(library.cues, newList);

        if (changed)
        {
            Undo.RecordObject(library, "Update SoundLibrary cues");
            library.cues.Clear();
            library.cues.AddRange(newList);
            EditorUtility.SetDirty(library);
        }

        return changed;
    }

    private static bool ListsEqualByReference(List<SoundCue> a, List<SoundCue> b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;

        for (int i = 0; i < a.Count; i++)
            if (!ReferenceEquals(a[i], b[i])) return false;

        return true;
    }

    // ─────────────────────────────────────────────────────────────
    // Step 4: Ensure single enum file
    // ─────────────────────────────────────────────────────────────

    private static void DeleteAllEnumDuplicatesExcept(string keepPath)
    {
        keepPath = keepPath.Replace("\\", "/");

        // Search for MonoScripts named SoundId
        var scriptGuids = AssetDatabase.FindAssets("SoundId t:MonoScript");
        for (int i = 0; i < scriptGuids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(scriptGuids[i]).Replace("\\", "/");
            if (string.IsNullOrEmpty(path)) continue;

            var fileName = Path.GetFileName(path);
            if (!IsEnumFileName(fileName)) continue;

            if (string.Equals(path, keepPath, StringComparison.OrdinalIgnoreCase))
                continue;

            AssetDatabase.DeleteAsset(path);
        }

        // Delete keepPath too (clean rewrite)
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(keepPath) != null)
        {
            AssetDatabase.DeleteAsset(keepPath);
        }

        Directory.CreateDirectory(GeneratedFolder);
    }

    private static bool IsEnumFileName(string fileName)
    {
        for (int i = 0; i < EnumFileNamesToDelete.Length; i++)
        {
            if (string.Equals(fileName, EnumFileNamesToDelete[i], StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────
    // Step 5: Generate enum from library
    // ─────────────────────────────────────────────────────────────

    private static void GenerateEnumFromLibrary(SoundLibrary library, string outputPath)
    {
        if (library.cues == null)
            library.cues = new List<SoundCue>();

        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var names = new List<string>(library.cues.Count);

        for (int i = 0; i < library.cues.Count; i++)
        {
            var cue = library.cues[i];

            string raw = "";
            if (cue != null)
            {
                // Prefer your explicit ID naming method if present
                try { raw = cue.GetEffectiveIdName(); }
                catch { raw = cue.name; }

                if (string.IsNullOrWhiteSpace(raw))
                    raw = cue.name;
            }

            if (cue == null)
                raw = $"Missing_{i}";

            var safe = MakeSafeEnumName(raw);
            if (string.IsNullOrWhiteSpace(safe))
                safe = $"Sound_{i}";

            var finalName = safe;
            int suffix = 1;
            while (!usedNames.Add(finalName))
            {
                suffix++;
                finalName = $"{safe}_{suffix}";
            }

            names.Add(finalName);
        }

        var sb = new StringBuilder();
        sb.AppendLine("// AUTO-GENERATED. Do not edit by hand.");
        sb.AppendLine($"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("// Source of truth: SoundLibrary.cues (auto-filled from all SoundCues in project).");
        sb.AppendLine();
        sb.AppendLine("public enum SoundId");
        sb.AppendLine("{");
        for (int i = 0; i < names.Count; i++)
            sb.AppendLine($"    {names[i]} = {i},");
        sb.AppendLine("}");

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
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
                if (first && char.IsDigit(c)) sb.Append('_');
                sb.Append(c);
                first = false;
            }
            else
            {
                if (sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
                first = false;
            }
        }

        while (sb.Length > 0 && sb[sb.Length - 1] == '_')
            sb.Length--;

        return sb.ToString();
    }
}
#endif
