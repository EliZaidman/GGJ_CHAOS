using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// JamInputDebug
/// -------------
/// Attach to the same GameObject as:
/// - PlayerInput
/// - JamInputPlayer
///
/// Goals:
/// - Jam-friendly logs (no spam, change-only, sampled).
/// - Print connected devices on Awake (optional).
/// - Print button events & move/look changes.
/// - Validate common "why doesn't it work" setup mistakes and warn once.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(JamInputPlayer))]
[RequireComponent(typeof(PlayerInput))]
public class JamInputDebug : MonoBehaviour
{
    #region Inspector - Basic

    [Header("Basic")]
    [Tooltip("Print InputSystem devices + this PlayerInput paired devices once on Awake.")]
    public bool logDevicesOnAwake = true;

    [Tooltip("Log button DOWN/UP events (Primary, Jump, Sprint, etc.).")]
    public bool logButtons = true;

    [Tooltip("Log Move/Look when they meaningfully change (sampled).")]
    public bool logMoveAndLook = true;

    [Tooltip("Log when PlayerInput scheme / paired devices change.")]
    public bool logSchemeAndDeviceChanges = true;

    [Tooltip("Log CycleDelta when non-zero.")]
    public bool logCycleDelta = true;

    [Tooltip("Run setup validation on Awake and warn about common misconfigurations.")]
    public bool validateSetupOnAwake = true;

    #endregion

    #region Inspector - Advanced

    [Header("Advanced")]
    [Tooltip("How often to sample Move/Look (seconds). Lower = more responsive, higher = less spam.")]
    [Range(0.02f, 0.5f)]
    public float vectorSampleInterval = 0.08f;

    [Tooltip("Deadzone to ignore tiny stick drift (0.1 to 0.2 recommended).")]
    [Range(0f, 0.5f)]
    public float deadzone = 0.15f;

    [Tooltip("Minimum delta to count as 'changed' (prevents micro jitter logs).")]
    [Range(0.0001f, 0.25f)]
    public float vectorChangeEpsilon = 0.03f;

    [Tooltip("Also log HELD (sampled, never per-frame).")]
    public bool logHeld = false;

    [Tooltip("How often to log HELD states (seconds).")]
    [Range(0.05f, 1.0f)]
    public float heldSampleInterval = 0.2f;

    [Tooltip("Log raw mouse scroll vector when it changes.")]
    public bool logRawScroll = false;

    [Tooltip("Prefix logs with the object name.")]
    public bool includeObjectNamePrefix = true;

    [Tooltip("Safety cap: maximum debug logs allowed per frame.")]
    [Range(1, 60)]
    public int maxLogsPerFrame = 20;

    #endregion

    #region Cached Components / State

    JamInputPlayer _input;
    PlayerInput _pi;

    Vector2 _lastMove;
    Vector2 _lastLook;

    string _lastScheme;
    string _lastDevicesSummary;

    float _nextVectorSampleTime;
    float _nextHeldSampleTime;

    int _logsThisFrame;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        _input = GetComponent<JamInputPlayer>();
        _pi = GetComponent<PlayerInput>();

        int p = _pi.playerIndex;
      //  SafeLog($"{Prefix(p)} JamInputDebug attached. (playerIndex={p})", this);

        if (validateSetupOnAwake)
            ValidateSetupOnce();

        if (logDevicesOnAwake)
            LogConnectedDevicesOnce(p);

        // Prime caches
        _lastMove = ApplyDeadzone(_input.Move);
        _lastLook = ApplyDeadzone(_input.Look);

        _lastScheme = _pi.currentControlScheme ?? "None";
        _lastDevicesSummary = BuildPlayerInputDevicesSummary(_pi);

        _nextVectorSampleTime = Time.unscaledTime;
        _nextHeldSampleTime = Time.unscaledTime;
    }

    void LateUpdate()
    {
        _logsThisFrame = 0;
    }

    void Update()
    {
        int p = _pi.playerIndex;

        // Scheme/device changes (only when changed)
        if (logSchemeAndDeviceChanges)
        {
            string scheme = _pi.currentControlScheme ?? "None";
            string devices = BuildPlayerInputDevicesSummary(_pi);

            if (scheme != _lastScheme || devices != _lastDevicesSummary)
            {
                _lastScheme = scheme;
                _lastDevicesSummary = devices;
                SafeLog($"{Prefix(p)} ControlScheme={_lastScheme} | PairedDevices={_lastDevicesSummary}");
            }
        }

        // Move/Look (sampled, change-only)
        if (logMoveAndLook && Time.unscaledTime >= _nextVectorSampleTime)
        {
            _nextVectorSampleTime = Time.unscaledTime + Mathf.Max(0.02f, vectorSampleInterval);

            // If this player doesn't have the right device paired, do not log vectors.
            if (!ShouldLogVectorsForThisPlayer())
                return;

            Vector2 move = ApplyDeadzone(_input.Move);
            Vector2 look = ApplyDeadzone(_input.Look);

            if (HasMeaningfulVectorChange(_lastMove, move))
            {
                _lastMove = move;
                SafeLog($"{Prefix(p)} Move={move}");
            }

            if (HasMeaningfulVectorChange(_lastLook, look))
            {
                _lastLook = look;
                SafeLog($"{Prefix(p)} Look={look}");
            }

            if (logRawScroll)
            {
                Vector2 scroll = _input.CycleScrollRaw;
                if (Mathf.Abs(scroll.y) > 0.001f)
                    SafeLog($"{Prefix(p)} RawScroll={scroll}");
            }
        }

        // Buttons DOWN/UP (instant)
        if (logButtons)
        {
            LogButtonDU(p, "Primary", _input.PrimaryDown, _input.PrimaryUp);
            LogButtonDU(p, "Secondary", _input.SecondaryDown, _input.SecondaryUp);
            LogButtonDU(p, "Jump", _input.JumpDown, _input.JumpUp);
            LogButtonDU(p, "Interact", _input.InteractDown, _input.InteractUp);
            LogButtonDU(p, "Special", _input.SpecialDown, _input.SpecialUp);
            LogButtonDU(p, "Ultimate", _input.UltimateDown, _input.UltimateUp);
            LogButtonDU(p, "Crouch", _input.CrouchDown, _input.CrouchUp);
            LogButtonDU(p, "Sprint", _input.SprintDown, _input.SprintUp);
            LogButtonDU(p, "Action2", _input.Action2Down, _input.Action2Up);

            if (_input.PauseDown) SafeLog($"{Prefix(p)} Pause DOWN");
            if (_input.MenuDown) SafeLog($"{Prefix(p)} Menu DOWN");
        }

        // HELD (sampled)
        if (logHeld && Time.unscaledTime >= _nextHeldSampleTime)
        {
            _nextHeldSampleTime = Time.unscaledTime + Mathf.Max(0.05f, heldSampleInterval);

            LogButtonHeld(p, "Primary", _input.PrimaryHeld);
            LogButtonHeld(p, "Secondary", _input.SecondaryHeld);
            LogButtonHeld(p, "Jump", _input.JumpHeld);
            LogButtonHeld(p, "Interact", _input.InteractHeld);
            LogButtonHeld(p, "Special", _input.SpecialHeld);
            LogButtonHeld(p, "Ultimate", _input.UltimateHeld);
            LogButtonHeld(p, "Crouch", _input.CrouchHeld);
            LogButtonHeld(p, "Sprint", _input.SprintHeld);
            LogButtonHeld(p, "Action2", _input.Action2Held);
        }

        // Cycle
        if (logCycleDelta)
        {
            int cycle = _input.CycleDelta;
            if (cycle != 0)
                SafeLog($"{Prefix(p)} CycleDelta={cycle} (-1=Prev, +1=Next)");
        }
    }

    #endregion

    #region Manager Hook

    /// <summary>
    /// Optional helper: lets a manager/editor push settings without touching fields one-by-one.
    /// (JamInputManager uses its own ApplyPresetTo, but this is useful if you reuse this component elsewhere.)
    /// </summary>
    public void ApplyFromManager(
        bool enabled,
        bool logDevices,
        bool logBtns,
        bool logVectors,
        bool logSchemeChanges,
        bool logCycle,
        bool validate,
        float sampleInterval,
        float dz,
        float epsilon,
        bool held,
        float heldInterval,
        bool rawScroll,
        bool objectPrefix,
        int maxPerFrame)
    {
        this.enabled = enabled;
        logDevicesOnAwake = logDevices;
        logButtons = logBtns;
        logMoveAndLook = logVectors;
        logSchemeAndDeviceChanges = logSchemeChanges;
        logCycleDelta = logCycle;
        validateSetupOnAwake = validate;

        vectorSampleInterval = sampleInterval;
        deadzone = dz;
        vectorChangeEpsilon = epsilon;

        logHeld = held;
        heldSampleInterval = heldInterval;

        logRawScroll = rawScroll;
        includeObjectNamePrefix = objectPrefix;
        maxLogsPerFrame = maxPerFrame;
    }

    #endregion

    #region Validation / Warnings

    void ValidateSetupOnce()
    {
        // Keep warnings short and only when likely to block usage.

#if UNITY_EDITOR
#if !ENABLE_INPUT_SYSTEM
        Debug.LogWarning(
            "[JamInput] Project setting: the new Input System is not enabled. " +
            "Go to Project Settings > Player > Active Input Handling and set it to 'Input System Package' (or 'Both').",
            this);
#endif
#endif

        if (_pi.actions == null)
        {
            Debug.LogWarning(
                "[JamInput] PlayerInput has no Actions asset assigned. Assign JamInputActions.inputactions.",
                this);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_pi.defaultActionMap))
            {
                Debug.LogWarning("[JamInput] PlayerInput Default Action Map is empty. Set it to 'Player'.", this);
            }
        }

        // UI module mismatch (warn once)
        var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem != null)
        {
            var uiModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (uiModule == null)
            {
                Debug.LogWarning(
                    "[JamInput] EventSystem exists but InputSystemUIInputModule is missing. " +
                    "If you want UI navigation with the new input system, add InputSystemUIInputModule to the EventSystem.",
                    this);
            }
        }

        if (_input == null)
        {
            Debug.LogWarning("[JamInput] JamInputPlayer component missing (required).", this);
        }
    }

    #endregion

    #region Logging Helpers

    bool HasPairedKeyboardOrMouse()
    {
        var devices = _pi.devices;
        for (int i = 0; i < devices.Count; i++)
            if (devices[i] is Keyboard || devices[i] is Mouse)
                return true;
        return false;
    }

    bool HasPairedGamepad()
    {
        var devices = _pi.devices;
        for (int i = 0; i < devices.Count; i++)
            if (devices[i] is Gamepad)
                return true;
        return false;
    }

    /// <summary>
    /// For vector logs, decide if this player should log at all:
    /// - If scheme says KeyboardMouse -> require keyboard/mouse paired.
    /// - If scheme says Gamepad -> require a gamepad paired.
    /// - If scheme is None/unknown -> require ANY paired device.
    /// </summary>
    bool ShouldLogVectorsForThisPlayer()
    {
        string scheme = _pi.currentControlScheme ?? "None";

        if (scheme == "KeyboardMouse")
            return HasPairedKeyboardOrMouse();

        if (scheme == "Gamepad")
            return HasPairedGamepad();

        return _pi.devices.Count > 0;
    }

    string Prefix(int playerIndex)
    {
        return includeObjectNamePrefix ? $"[{gameObject.name}][P{playerIndex}]" : $"[P{playerIndex}]";
    }

    void SafeLog(string msg, Object context = null)
    {
        if (_logsThisFrame >= maxLogsPerFrame)
            return;

        _logsThisFrame++;

        if (context != null) Debug.Log(msg, context);
        else Debug.Log(msg);
    }

    void LogButtonDU(int p, string name, bool down, bool up)
    {
        if (down) SafeLog($"{Prefix(p)} {name} DOWN");
        if (up) SafeLog($"{Prefix(p)} {name} UP");
    }

    void LogButtonHeld(int p, string name, bool held)
    {
        if (held) SafeLog($"{Prefix(p)} {name} HELD");
    }

    void LogConnectedDevicesOnce(int p)
    {
        var sb = new StringBuilder(512);

        sb.AppendLine($"{Prefix(p)} Connected devices (InputSystem.devices):");
        foreach (var d in InputSystem.devices)
            sb.AppendLine($"  - {d.displayName} | layout={d.layout} | path={d.path} | type={d.GetType().Name}");

        sb.AppendLine($"{Prefix(p)} Gamepad.all.Count={Gamepad.all.Count}");
        sb.AppendLine($"{Prefix(p)} Joystick.all.Count={Joystick.all.Count}");

        sb.AppendLine($"{Prefix(p)} PlayerInput paired devices:");
        sb.AppendLine($"  ControlScheme={_pi.currentControlScheme ?? "None"}");
        sb.AppendLine($"  Devices={BuildPlayerInputDevicesSummary(_pi)}");

        SafeLog(sb.ToString());
    }

    static string BuildPlayerInputDevicesSummary(PlayerInput pi)
    {
        if (pi == null) return "None";
        var devices = pi.devices;
        if (devices.Count == 0) return "None";

        var sb = new StringBuilder(128);
        for (int i = 0; i < devices.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(devices[i].displayName);
            sb.Append(" (");
            sb.Append(devices[i].layout);
            sb.Append(")");
        }

        return sb.ToString();
    }

    #endregion

    #region Vector Helpers

    Vector2 ApplyDeadzone(Vector2 v)
    {
        return v.sqrMagnitude < deadzone * deadzone ? Vector2.zero : v;
    }

    bool HasMeaningfulVectorChange(Vector2 previous, Vector2 current)
    {
        float eps = Mathf.Max(0.0001f, vectorChangeEpsilon);
        if ((current - previous).sqrMagnitude >= eps * eps)
            return true;

        // Also log transitions into/out of zero
        return (previous == Vector2.zero) != (current == Vector2.zero);
    }

    #endregion
}
