#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundManager))]
public class SoundManagerEditor : Editor
{
    private static bool _showMixer = false;
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

        // ─────────────────────────────────────────────────────────────
        // Find properties (safe: they may not exist in your current SoundManager)
        // ─────────────────────────────────────────────────────────────
        var libraryProp       = serializedObject.FindProperty("library");
        var debugLogsProp     = serializedObject.FindProperty("debugLogs");

        var support3DProp     = serializedObject.FindProperty("support3DSound");

        // Volume fields (from the simplified SoundManager I gave you)
        var masterVolumeProp  = serializedObject.FindProperty("masterVolume");
        var musicVolumeProp   = serializedObject.FindProperty("musicVolume");
        var sfxVolumeProp     = serializedObject.FindProperty("sfxVolume");
        var uiVolumeProp      = serializedObject.FindProperty("uiVolume");
        var ambienceVolumeProp= serializedObject.FindProperty("ambienceVolume");

        // Old / optional fields (only drawn if they exist)
        var defaultFadeProp   = serializedObject.FindProperty("defaultMusicFadeSeconds");
        var sfxPoolSizeProp   = serializedObject.FindProperty("sfxPoolSize");
        var sfx3dPoolSizeProp = serializedObject.FindProperty("sfx3dPoolSize");

        var mixerProp         = serializedObject.FindProperty("mixer");
        var masterGroupProp   = serializedObject.FindProperty("masterGroup");
        var musicGroupProp    = serializedObject.FindProperty("musicGroup");
        var sfxGroupProp      = serializedObject.FindProperty("sfxGroup");
        var uiGroupProp       = serializedObject.FindProperty("uiGroup");
        var ambienceGroupProp = serializedObject.FindProperty("ambienceGroup");
        var useMixerProp      = serializedObject.FindProperty("useAudioMixerRouting");

        // ─────────────────────────────────────────────────────────────
        // Library
        // ─────────────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Library", EditorStyles.boldLabel);
        DrawPropSafe(libraryProp, "library", "SoundLibrary");

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

        // Draw whichever settings exist in your current SoundManager
        DrawPropSafe(support3DProp, "support3DSound", "Support 3D SFX");
        DrawPropSafe(debugLogsProp, "debugLogs", "Debug Logs");

        // If you still have these old fields, they’ll show; if not, nothing breaks.
        //DrawPropSafe(defaultFadeProp, "defaultMusicFadeSeconds", "Default Music Fade (s)");
        DrawPropSafe(sfxPoolSizeProp, "sfxPoolSize", "2D SFX Pool Size");
        DrawPropSafe(sfx3dPoolSizeProp, "sfx3dPoolSize", "3D SFX Pool Size");

        EditorGUILayout.Space();

        // ─────────────────────────────────────────────────────────────
        // Volumes (for the simplified SoundManager)
        // ─────────────────────────────────────────────────────────────
        bool anyVolume =
            masterVolumeProp != null ||
            musicVolumeProp != null ||
            sfxVolumeProp != null ||
            uiVolumeProp != null ||
            ambienceVolumeProp != null;

        if (anyVolume)
        {
            EditorGUILayout.LabelField("Volumes", EditorStyles.boldLabel);
            DrawPropSafe(masterVolumeProp, "masterVolume", "Master Volume");
            DrawPropSafe(musicVolumeProp, "musicVolume", "Music Volume");
            DrawPropSafe(sfxVolumeProp, "sfxVolume", "SFX Volume");
            DrawPropSafe(uiVolumeProp, "uiVolume", "UI Volume");
            DrawPropSafe(ambienceVolumeProp, "ambienceVolume", "Ambience Volume");
            EditorGUILayout.Space();
        }

        // ─────────────────────────────────────────────────────────────
        // Audio Mixer foldout (optional legacy)
        // ─────────────────────────────────────────────────────────────
        bool mixerSectionExists =
            useMixerProp != null ||
            mixerProp != null ||
            masterGroupProp != null ||
            musicGroupProp != null ||
            sfxGroupProp != null ||
            uiGroupProp != null ||
            ambienceGroupProp != null;

        if (mixerSectionExists)
        {
            _showMixer = EditorGUILayout.Foldout(_showMixer, "Audio Mixer (Optional)", true);
            if (_showMixer)
            {
                EditorGUI.indentLevel++;
                DrawPropSafe(useMixerProp, "useAudioMixerRouting", "Use AudioMixer Routing");
                DrawPropSafe(mixerProp, "mixer", "Mixer Asset");
                DrawPropSafe(masterGroupProp, "masterGroup", "Master Group");
                DrawPropSafe(musicGroupProp, "musicGroup", "Music Group");
                DrawPropSafe(sfxGroupProp, "sfxGroup", "SFX Group");
                DrawPropSafe(uiGroupProp, "uiGroup", "UI Group");
                DrawPropSafe(ambienceGroupProp, "ambienceGroup", "Ambience Group");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
        }

        // ─────────────────────────────────────────────────────────────
        // Optional: show missing fields list to help you delete old inspector code
        // ─────────────────────────────────────────────────────────────
        _showMissingFields = EditorGUILayout.ToggleLeft("Show missing-field warnings", _showMissingFields);

        if (_showMissingFields)
        {
            DrawMissingIfNull(libraryProp, "library");
            DrawMissingIfNull(debugLogsProp, "debugLogs");
            DrawMissingIfNull(support3DProp, "support3DSound");

            DrawMissingIfNull(masterVolumeProp, "masterVolume");
            DrawMissingIfNull(musicVolumeProp, "musicVolume");
            DrawMissingIfNull(sfxVolumeProp, "sfxVolume");
            DrawMissingIfNull(uiVolumeProp, "uiVolume");
            DrawMissingIfNull(ambienceVolumeProp, "ambienceVolume");

            DrawMissingIfNull(defaultFadeProp, "defaultMusicFadeSeconds");
            DrawMissingIfNull(sfxPoolSizeProp, "sfxPoolSize");
            DrawMissingIfNull(sfx3dPoolSizeProp, "sfx3dPoolSize");

            DrawMissingIfNull(useMixerProp, "useAudioMixerRouting");
            DrawMissingIfNull(mixerProp, "mixer");
            DrawMissingIfNull(masterGroupProp, "masterGroup");
            DrawMissingIfNull(musicGroupProp, "musicGroup");
            DrawMissingIfNull(sfxGroupProp, "sfxGroup");
            DrawMissingIfNull(uiGroupProp, "uiGroup");
            DrawMissingIfNull(ambienceGroupProp, "ambienceGroup");
        }

        // ─────────────────────────────────────────────────────────────
        // Draw remaining fields without risking crashes
        // (Only exclude fields that actually exist to avoid confusion)
        // ─────────────────────────────────────────────────────────────
        DrawPropertiesExcluding(serializedObject, "m_Script");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPropSafe(SerializedProperty prop, string fieldName, string label)
    {
        if (prop == null)
            return;

        EditorGUILayout.PropertyField(prop, new GUIContent(label), includeChildren: true);
    }

    private void DrawMissingIfNull(SerializedProperty prop, string fieldName)
    {
        if (prop != null) return;

        EditorGUILayout.HelpBox(
            $"Field not found on SoundManager: '{fieldName}'.\n" +
            $"If you removed/renamed it in SoundManager.cs, this is expected.",
            MessageType.None
        );
    }
}
#endif
