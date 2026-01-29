#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JamInputDebug))]
public class JamInputDebugEditor : Editor
{
    // Basic
    SerializedProperty logDevicesOnAwake;
    SerializedProperty logButtons;
    SerializedProperty logMoveAndLook;
    SerializedProperty logSchemeAndDeviceChanges;
    SerializedProperty logCycleDelta;
    SerializedProperty validateSetupOnAwake;

    // Advanced
    SerializedProperty vectorSampleInterval;
    SerializedProperty vectorChangeEpsilon;
    SerializedProperty deadzone;
    SerializedProperty logHeld;
    SerializedProperty heldSampleInterval;
    SerializedProperty logRawScroll;
    SerializedProperty includeObjectNamePrefix;
    SerializedProperty maxLogsPerFrame;

    bool showAdvanced;

    void OnEnable()
    {
        // Basic
        logDevicesOnAwake = serializedObject.FindProperty("logDevicesOnAwake");
        logButtons = serializedObject.FindProperty("logButtons");
        logMoveAndLook = serializedObject.FindProperty("logMoveAndLook");
        logSchemeAndDeviceChanges = serializedObject.FindProperty("logSchemeAndDeviceChanges");
        logCycleDelta = serializedObject.FindProperty("logCycleDelta");
        validateSetupOnAwake = serializedObject.FindProperty("validateSetupOnAwake");

        // Advanced
        vectorSampleInterval = serializedObject.FindProperty("vectorSampleInterval");
        vectorChangeEpsilon = serializedObject.FindProperty("vectorChangeEpsilon");
        deadzone = serializedObject.FindProperty("deadzone");
        logHeld = serializedObject.FindProperty("logHeld");
        heldSampleInterval = serializedObject.FindProperty("heldSampleInterval");
        logRawScroll = serializedObject.FindProperty("logRawScroll");
        includeObjectNamePrefix = serializedObject.FindProperty("includeObjectNamePrefix");
        maxLogsPerFrame = serializedObject.FindProperty("maxLogsPerFrame");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Jam Input Debug", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "This component is controlled by JamInputManager.\n\n" +
            "All debug options are managed centrally from the manager inspector.\n" +
            "Do not configure this component directly.",
            MessageType.Info
        );

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
