#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundManager))]
public class SoundManagerEditor : Editor
{
    private static bool _showMissingFields = false;

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "JamAudio SoundManager\n\n" +
            "1) Create SoundCue assets (Right-click → Create → JamAudio → Sound Cue)\n" +
            "2) Run Tools → JamAudio → Regenerate SoundId Enum\n" +
            "3) Play sounds from code:\n" +
            "   SoundManager.Play(SoundId.YourSound);\n" +
            "   SoundManager.PlayMusic(SoundId.YourTrack);\n\n" +
            "You do NOT need to add any AudioSources manually.\n" +
            "They are created automatically at runtime.",
            MessageType.Info
        );

        serializedObject.Update();

        // Properties that exist in YOUR current SoundManager.cs
        var libraryProp        = serializedObject.FindProperty("library");
        var debugLogsProp      = serializedObject.FindProperty("debugLogs");

        var masterVolumeProp   = serializedObject.FindProperty("masterVolume");
        var musicVolumeProp    = serializedObject.FindProperty("musicVolume");
        var sfxVolumeProp      = serializedObject.FindProperty("sfxVolume");
        var uiVolumeProp       = serializedObject.FindProperty("uiVolume");
        var ambienceVolumeProp = serializedObject.FindProperty("ambienceVolume");

        var support3DProp      = serializedObject.FindProperty("support3DSound");

        // ─────────────────────────────────────────────────────────────
        // Library
        // ─────────────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Library", EditorStyles.boldLabel);
        DrawPropSafe(libraryProp, "SoundLibrary");

        if (libraryProp != null && libraryProp.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "No SoundLibrary assigned. Sounds cannot be resolved at runtime.\n" +
                "Assign the SoundLibrary asset here.",
                MessageType.Warning
            );
        }

        EditorGUILayout.Space();

        // ─────────────────────────────────────────────────────────────
        // Core Settings
        // ─────────────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Core Settings", EditorStyles.boldLabel);
        DrawPropSafe(support3DProp, "Support 3D SFX");
        DrawPropSafe(debugLogsProp, "Debug Logs");

        EditorGUILayout.Space();

        // ─────────────────────────────────────────────────────────────
        // Volumes
        // ─────────────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Volumes", EditorStyles.boldLabel);
        DrawPropSafe(masterVolumeProp, "Master Volume");
        DrawPropSafe(musicVolumeProp, "Music Volume");
        DrawPropSafe(sfxVolumeProp, "SFX Volume");
        DrawPropSafe(uiVolumeProp, "UI Volume");
        DrawPropSafe(ambienceVolumeProp, "Ambience Volume");

        EditorGUILayout.Space();

        // Optional debug section: tells you if the editor is looking for wrong fields
        _showMissingFields = EditorGUILayout.ToggleLeft("Show missing-field warnings", _showMissingFields);
        if (_showMissingFields)
        {
            WarnIfMissing(libraryProp, "library");
            WarnIfMissing(debugLogsProp, "debugLogs");
            WarnIfMissing(masterVolumeProp, "masterVolume");
            WarnIfMissing(musicVolumeProp, "musicVolume");
            WarnIfMissing(sfxVolumeProp, "sfxVolume");
            WarnIfMissing(uiVolumeProp, "uiVolume");
            WarnIfMissing(ambienceVolumeProp, "ambienceVolume");
            WarnIfMissing(support3DProp, "support3DSound");
        }

        // ✅ IMPORTANT: no DrawPropertiesExcluding here.
        // If you call it, you’ll draw everything twice.
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPropSafe(SerializedProperty prop, string label)
    {
        if (prop == null) return;
        EditorGUILayout.PropertyField(prop, new GUIContent(label), includeChildren: true);
    }

    private void WarnIfMissing(SerializedProperty prop, string fieldName)
    {
        if (prop != null) return;
        EditorGUILayout.HelpBox(
            $"SoundManager field not found: '{fieldName}'.\n" +
            $"If you renamed/removed it, this is expected.",
            MessageType.None
        );
    }
}
#endif
