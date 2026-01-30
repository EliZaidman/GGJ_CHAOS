#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundLibrary))]
public class SoundLibraryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Regenerate SoundId Enum", GUILayout.Height(26)))
            {
                SoundIdGenerator.Regenerate();
            }

            if (GUILayout.Button("Validate", GUILayout.Height(26)))
            {
                ValidateThisLibrary();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Selected SoundCues"))
            {
                AddSelectedSoundCues();
            }

            if (GUILayout.Button("Remove Nulls"))
            {
                RemoveNulls();
            }

            if (GUILayout.Button("Sort A→Z"))
            {
                SortByName();
            }
        }
    }

    private void ValidateThisLibrary()
    {
        var lib = (SoundLibrary)target;

        // Ensure list exists
        if (lib.cues == null)
            lib.cues = new List<SoundCue>();

        int nullCount = 0;
        int emptyClipCount = 0;
        int duplicateNameGroups = 0;

        var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < lib.cues.Count; i++)
        {
            var cue = lib.cues[i];
            if (cue == null)
            {
                nullCount++;
                continue;
            }

            var n = (cue.name ?? "").Trim();
            if (!string.IsNullOrEmpty(n))
            {
                nameCounts.TryGetValue(n, out int c);
                nameCounts[n] = c + 1;
            }

            // Best-effort clip count, supports private [SerializeField] "clips"
            if (TryCountClipsOnCue(cue, out int clipCount))
            {
                if (clipCount <= 0) emptyClipCount++;
            }
        }

        foreach (var kv in nameCounts)
            if (kv.Value > 1) duplicateNameGroups++;

        var msg =
            $"Validation Results:\n" +
            $"- Total cues: {lib.cues.Count}\n" +
            $"- Null entries: {nullCount}\n" +
            $"- Cues with 0 clips: {emptyClipCount}\n" +
            $"- Duplicate cue names: {duplicateNameGroups}\n\n" +
            $"Tip: duplicate names can create enum suffixes like _2/_3.";

        Debug.Log($"[SoundLibraryEditor]\n{msg}", lib);
        EditorUtility.DisplayDialog("Sound Library Validation", msg, "OK");
    }

    private void AddSelectedSoundCues()
    {
        var lib = (SoundLibrary)target;
        if (lib.cues == null)
            lib.cues = new List<SoundCue>();

        var selected = Selection.objects;
        if (selected == null || selected.Length == 0) return;

        Undo.RecordObject(lib, "Add Selected SoundCues");

        int added = 0;
        foreach (var obj in selected)
        {
            if (obj is SoundCue cue && cue != null && !lib.cues.Contains(cue))
            {
                lib.cues.Add(cue);
                added++;
            }
        }

        if (added > 0)
        {
            EditorUtility.SetDirty(lib);
            serializedObject.Update();
            Debug.Log($"[SoundLibraryEditor] Added {added} cues.", lib);
        }
    }

    private void RemoveNulls()
    {
        var lib = (SoundLibrary)target;
        if (lib.cues == null)
            lib.cues = new List<SoundCue>();

        Undo.RecordObject(lib, "Remove Null SoundCues");

        int before = lib.cues.Count;
        lib.cues.RemoveAll(c => c == null);
        int removed = before - lib.cues.Count;

        if (removed > 0)
        {
            EditorUtility.SetDirty(lib);
            serializedObject.Update();
            Debug.Log($"[SoundLibraryEditor] Removed {removed} null entries.", lib);
        }
    }

    private void SortByName()
    {
        var lib = (SoundLibrary)target;
        if (lib.cues == null)
            lib.cues = new List<SoundCue>();

        Undo.RecordObject(lib, "Sort SoundCues A-Z");

        lib.cues.Sort((a, b) =>
        {
            var an = a ? a.name : "";
            var bn = b ? b.name : "";
            return string.Compare(an, bn, StringComparison.OrdinalIgnoreCase);
        });

        EditorUtility.SetDirty(lib);
        serializedObject.Update();
        Debug.Log("[SoundLibraryEditor] Sorted cues A→Z.", lib);
    }

    private bool TryCountClipsOnCue(SoundCue cue, out int clipCount)
    {
        clipCount = 0;
        if (cue == null) return false;

        const string fieldName = "clips";
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var f = cue.GetType().GetField(fieldName, flags);
        if (f == null) return false;

        object value = f.GetValue(cue);
        if (value == null) { clipCount = 0; return true; }

        if (value is AudioClip[] arr)
        {
            clipCount = arr.Length;
            return true;
        }

        if (value is IList list)
        {
            clipCount = list.Count;
            return true;
        }

        return false;
    }
}
#endif
