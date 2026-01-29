#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundManager))]
public class SoundManagerEditor : Editor
{
    private static bool _showMixer = false;

    public override void OnInspectorGUI()
    {
        // Usage hint
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

        // Grab properties we care about explicitly
        var libraryProp       = serializedObject.FindProperty("library");
        var defaultFadeProp   = serializedObject.FindProperty("defaultMusicFadeSeconds");
        var support3DProp     = serializedObject.FindProperty("support3DSound");
        var sfxPoolSizeProp   = serializedObject.FindProperty("sfxPoolSize");
        var sfx3dPoolSizeProp = serializedObject.FindProperty("sfx3dPoolSize");
        var debugLogsProp     = serializedObject.FindProperty("debugLogs");

        var mixerProp         = serializedObject.FindProperty("mixer");
        var masterGroupProp   = serializedObject.FindProperty("masterGroup");
        var musicGroupProp    = serializedObject.FindProperty("musicGroup");
        var sfxGroupProp      = serializedObject.FindProperty("sfxGroup");
        var uiGroupProp       = serializedObject.FindProperty("uiGroup");
        var ambienceGroupProp = serializedObject.FindProperty("ambienceGroup");
        var useMixerProp      = serializedObject.FindProperty("useAudioMixerRouting");

        // Library + warning
        EditorGUILayout.PropertyField(libraryProp);
        if (libraryProp.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "No SoundLibrary assigned. Sounds cannot be resolved at runtime.\n" +
                "Assign the SoundLibrary asset here.",
                MessageType.Warning
            );
        }

        EditorGUILayout.Space();

        // Core settings
        EditorGUILayout.LabelField("Core Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(defaultFadeProp,   new GUIContent("Default Music Fade (s)"));
        EditorGUILayout.PropertyField(support3DProp,     new GUIContent("Support 3D SFX"));
        EditorGUILayout.PropertyField(sfxPoolSizeProp,   new GUIContent("2D SFX Pool Size"));
        EditorGUILayout.PropertyField(sfx3dPoolSizeProp, new GUIContent("3D SFX Pool Size"));
        EditorGUILayout.PropertyField(debugLogsProp,     new GUIContent("Debug Logs"));

        EditorGUILayout.Space();

        // Audio Mixer foldout
        _showMixer = EditorGUILayout.Foldout(_showMixer, "Audio Mixer (Optional)", true);
        if (_showMixer)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(useMixerProp, new GUIContent("Use AudioMixer Routing"));
            EditorGUILayout.PropertyField(mixerProp,     new GUIContent("Mixer Asset"));

            EditorGUILayout.PropertyField(masterGroupProp,   new GUIContent("Master Group"));
            EditorGUILayout.PropertyField(musicGroupProp,    new GUIContent("Music Group"));
            EditorGUILayout.PropertyField(sfxGroupProp,      new GUIContent("SFX Group"));
            EditorGUILayout.PropertyField(uiGroupProp,       new GUIContent("UI Group"));
            EditorGUILayout.PropertyField(ambienceGroupProp, new GUIContent("Ambience Group"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Draw any remaining fields (if there are extra serialized ones)
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "library",
            "defaultMusicFadeSeconds",
            "support3DSound",
            "sfxPoolSize",
            "sfx3dPoolSize",
            "debugLogs",
            "mixer",
            "masterGroup",
            "musicGroup",
            "sfxGroup",
            "uiGroup",
            "ambienceGroup",
            "useAudioMixerRouting"
        );

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
