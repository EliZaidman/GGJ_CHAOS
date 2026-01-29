#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

[CustomEditor(typeof(SoundCue))]
public class SoundCueEditor : Editor
{
    private AudioClip _previewClip;
    private bool _isPlaying;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        var cue = (SoundCue)target;

        EditorGUILayout.Space();
        DrawWarnings(cue);
        DrawPreviewControls(cue);
    }

    private void DrawWarnings(SoundCue cue)
    {
        if (cue.clips == null || cue.clips.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "This SoundCue has no clips assigned and will not play any sound.",
                MessageType.Warning
            );
        }
    }

    private void DrawPreviewControls(SoundCue cue)
    {
        bool hasClips = cue.clips != null && cue.clips.Length > 0;

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!hasClips))
            {
                if (GUILayout.Button("Play original sound Preview "))
                    PlayPreview(cue);
            }

            using (new EditorGUI.DisabledScope(!_isPlaying))
            {
                if (GUILayout.Button("Stop"))
                    StopPreview();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    // INTERNAL AUDIOUTIL REFLECTION HACK
    // ─────────────────────────────────────────────────────────────
    private static Type audioUtilType;
    private static MethodInfo playPreviewClipMethod;
    private static MethodInfo stopAllPreviewClipsMethod;

    private static void InitAudioUtil()
    {
        if (audioUtilType != null) return;

        audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        playPreviewClipMethod = audioUtilType.GetMethod(
            "PlayPreviewClip",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null
        );

        stopAllPreviewClipsMethod = audioUtilType.GetMethod(
            "StopAllPreviewClips",
            BindingFlags.Static | BindingFlags.Public
        );
    }

    private void PlayPreview(SoundCue cue)
    {
        StopPreview();

        _previewClip = cue.GetRandomClip();
        if (_previewClip == null)
            return;

        InitAudioUtil();

        // Play clip at 0 sample, looping=false
        playPreviewClipMethod.Invoke(null, new object[] { _previewClip, 0, false });

        _isPlaying = true;
    }

    private void StopPreview()
    {
        if (!_isPlaying) return;

        InitAudioUtil();
        stopAllPreviewClipsMethod.Invoke(null, null);

        _isPlaying = false;
        _previewClip = null;
    }

    private void OnDisable()
    {
        StopPreview();
    }
}
#endif
