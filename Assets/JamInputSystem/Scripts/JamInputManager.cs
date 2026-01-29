using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// JamInputManager
/// --------------
/// Jam-first player input setup + runtime reconfiguration.
/// - Supports up to 4 player slots.
/// - Can either spawn our Jam player prefab OR "upgrade" existing player objects.
/// - Provides a small public API so a future UI (or any script) can change slot kinds/player count at runtime.
/// - (NEW) Can centrally control JamInputDebug settings from THIS inspector and push them to all players.
///
/// Notes:
/// - "AnyUnusedConnected" prefers an unused Gamepad first, then falls back to Keyboard+Mouse if free.
/// </summary>
[DisallowMultipleComponent]
public class JamInputManager : MonoBehaviour
{
    #region Inspector - Modes & Slots

    public enum SetupMode
    {
        /// <summary>Manager instantiates our Jam player prefab per active slot.</summary>
        SpawnJamPlayers,

        /// <summary>Manager uses your pre-created objects and ensures required components exist.</summary>
        UseExistingPlayerObjects
    }

    public enum JamPlayerKind
    {
        Disabled,
        HumanKeyboardMouse,
        HumanGamepadAny,

        /// <summary>
        /// Picks any connected device not already used by another player.
        /// Prefers an unused Gamepad first, then Keyboard+Mouse if free.
        /// </summary>
        HumanAnyUnusedConnected,

        /// <summary>
        /// Disables PlayerInput for this slot. Use your own AI/logic to drive behavior for this "player".
        /// </summary>
        SimulatedBot
    }

    [System.Serializable]
    public class JamPlayerSlot
    {
        [Tooltip("What kind of input this slot should use.")]
        public JamPlayerKind kind = JamPlayerKind.Disabled;
    }

    [Header("Mode")]
    [Tooltip(
        "How JamInputManager creates/assigns players.\n" +
        "- SpawnJamPlayers: instantiates the Jam Player Prefab per slot.\n" +
        "- UseExistingPlayerObjects: uses your existing objects and adds required components if missing.")]
    [SerializeField] SetupMode setupMode = SetupMode.SpawnJamPlayers;

    [Header("Jam Player Prefab / Template")]
    [Tooltip(
        "Prefab (recommended) or template containing PlayerInput + JamInputPlayer.  a ready prefab came with this package\n")]
    [SerializeField] JamInputPlayer jamPlayerPrefabOrTemplate = null;

    [Tooltip("Optional parent transform for spawned players (keeps hierarchy tidy).")]
    [SerializeField] Transform spawnedPlayersParent = null;

    [Header("Existing Players (only used in UseExistingPlayerObjects mode)")]
    [Tooltip(
        "If you already spawn players elsewhere or have custom player prefabs/scripts, assign them here.\n" +
        "JamInputManager will add missing PlayerInput + JamInputPlayer (if possible) and configure them per slot.")]
    [SerializeField] GameObject[] existingPlayers = new GameObject[4];

    [Header("Player Slots (max 4)")]
    [Tooltip("Each slot decides if it is active and what kind of device it should use.")]
    [SerializeField] JamPlayerSlot[] slots = new JamPlayerSlot[4];

    #endregion

    #region Inspector - Debug Control (Manager-Driven)

    public enum DebugPreset
    {
        Off,
        Basic,
        Verbose,
        Custom
    }

    [System.Serializable]
    public class DebugConfig
    {
        [Tooltip("If enabled, JamInputManager will add/enable JamInputDebug on each Jam player and push settings.")]
        public DebugPreset preset = DebugPreset.Off;

        [Header("Custom Toggles (only used when Preset=Custom)")]
        public bool logDevicesOnAwake = true;
        public bool logButtons = true;
        public bool logMoveAndLook = true;
        public bool logSchemeAndDeviceChanges = true;
        public bool logCycleDelta = true;
        public bool validateSetupOnAwake = true;

        [Header("Custom Advanced (only used when Preset=Custom)")]
        [Range(0.02f, 0.5f)] public float vectorSampleInterval = 0.08f;
        [Range(0f, 0.5f)] public float deadzone = 0.15f;
        [Range(0.0001f, 0.25f)] public float vectorChangeEpsilon = 0.03f;

        public bool logHeld = false;
        [Range(0.05f, 1.0f)] public float heldSampleInterval = 0.2f;

        public bool logRawScroll = false;
        public bool includeObjectNamePrefix = true;
        [Range(1, 60)] public int maxLogsPerFrame = 20;

        public bool Enabled => preset != DebugPreset.Off;

        public void ApplyPresetTo(JamInputDebug dbg)
        {
            if (dbg == null) return;

            switch (preset)
            {
                case DebugPreset.Off:
                    dbg.enabled = false;
                    return;

                case DebugPreset.Basic:
                    dbg.enabled = true;
                    dbg.logDevicesOnAwake = true;
                    dbg.logButtons = true;
                    dbg.logMoveAndLook = true;
                    dbg.logSchemeAndDeviceChanges = true;
                    dbg.logCycleDelta = true;
                    dbg.validateSetupOnAwake = true;

                    // keep basic pretty quiet
                    dbg.logHeld = false;
                    dbg.logRawScroll = false;

                    dbg.vectorSampleInterval = 0.10f;
                    dbg.deadzone = 0.15f;
                    dbg.vectorChangeEpsilon = 0.04f;

                    dbg.heldSampleInterval = 0.25f;
                    dbg.includeObjectNamePrefix = true;
                    dbg.maxLogsPerFrame = 20;
                    return;

                case DebugPreset.Verbose:
                    dbg.enabled = true;
                    dbg.logDevicesOnAwake = true;
                    dbg.logButtons = true;
                    dbg.logMoveAndLook = true;
                    dbg.logSchemeAndDeviceChanges = true;
                    dbg.logCycleDelta = true;
                    dbg.validateSetupOnAwake = true;

                    dbg.logHeld = true;
                    dbg.logRawScroll = true;

                    dbg.vectorSampleInterval = 0.06f;
                    dbg.deadzone = 0.12f;
                    dbg.vectorChangeEpsilon = 0.03f;

                    dbg.heldSampleInterval = 0.18f;
                    dbg.includeObjectNamePrefix = true;
                    dbg.maxLogsPerFrame = 40;
                    return;

                case DebugPreset.Custom:
                    dbg.enabled = true;

                    dbg.logDevicesOnAwake = logDevicesOnAwake;
                    dbg.logButtons = logButtons;
                    dbg.logMoveAndLook = logMoveAndLook;
                    dbg.logSchemeAndDeviceChanges = logSchemeAndDeviceChanges;
                    dbg.logCycleDelta = logCycleDelta;
                    dbg.validateSetupOnAwake = validateSetupOnAwake;

                    dbg.vectorSampleInterval = vectorSampleInterval;
                    dbg.deadzone = deadzone;
                    dbg.vectorChangeEpsilon = vectorChangeEpsilon;

                    dbg.logHeld = logHeld;
                    dbg.heldSampleInterval = heldSampleInterval;

                    dbg.logRawScroll = logRawScroll;
                    dbg.includeObjectNamePrefix = includeObjectNamePrefix;
                    dbg.maxLogsPerFrame = maxLogsPerFrame;
                    return;
            }
        }
    }

    [Header("Debug (Manager Controls JamInputDebug)")]
    [SerializeField] DebugConfig debug = new DebugConfig();

    #endregion

    #region Public Runtime API

    /// <summary>Read-only access to slot array (for UI to display current state).</summary>
    public JamPlayerSlot[] Slots => slots;

    /// <summary>Sets a specific slot kind. Call ApplyRuntimeConfig() after making changes.</summary>
    public void SetSlotKind(int slotIndex, JamPlayerKind kind)
    {
        if (!EnsureSlotExists(slotIndex)) return;
        slots[slotIndex].kind = kind;
    }

    /// <summary>
    /// Convenience: sets how many active players you want (0..4).
    /// Slots above count become Disabled.
    /// If a newly-enabled slot was Disabled, it becomes defaultKindForNewSlots.
    /// Call ApplyRuntimeConfig() to rebuild.
    /// </summary>
    public void SetPlayerCount(int count, JamPlayerKind defaultKindForNewSlots = JamPlayerKind.HumanAnyUnusedConnected)
    {
        if (slots == null || slots.Length == 0) return;

        count = Mathf.Clamp(count, 0, slots.Length);

        for (int i = 0; i < slots.Length; i++)
        {
            EnsureSlotExists(i);

            if (i < count)
            {
                if (slots[i].kind == JamPlayerKind.Disabled)
                    slots[i].kind = defaultKindForNewSlots;
            }
            else
            {
                slots[i].kind = JamPlayerKind.Disabled;
            }
        }
    }

    /// <summary>
    /// Call this after changing slots at runtime to rebuild player objects + device assignments.
    /// </summary>
    public void ApplyRuntimeConfig()
    {
        RebuildAllPlayers();
    }

    #endregion

    #region Constants

    const int MaxSlots = 4;
    const string SchemeKeyboardMouse = "KeyboardMouse";
    const string SchemeGamepad = "Gamepad";
    const string DefaultActionMap = "Player";

    #endregion

    #region Runtime State

    readonly List<GameObject> _spawned = new List<GameObject>();

    bool _claimedKeyboardMouse;
    bool[] _claimedGamepads; // indices align with Gamepad.all at rebuild time

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        NormalizeSlotsArray();

        // Minimal validation (log only when broken)
        if (setupMode == SetupMode.SpawnJamPlayers)
        {
            if (jamPlayerPrefabOrTemplate == null)
            {
                Debug.LogError("[JamInputManager] Missing Jam Player Prefab/Template (jamPlayerPrefabOrTemplate).", this);
                return;
            }
        }
        else // UseExistingPlayerObjects
        {
            if (existingPlayers == null || existingPlayers.Length == 0)
            {
                Debug.LogError("[JamInputManager] SetupMode=UseExistingPlayerObjects but existingPlayers is empty.", this);
                return;
            }

            if (jamPlayerPrefabOrTemplate == null)
            {
                Debug.LogWarning(
                    "[JamInputManager] No template prefab assigned. I can still add components to existing players, " +
                    "but PlayerInput may be missing Actions/default map unless you set them yourself.",
                    this);
            }
        }

        StartCoroutine(DelayedInitialBuild());
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Keep arrays stable in the editor (prevents null slot elements / wrong sizes).
        NormalizeSlotsArray();
    }
#endif

    #endregion

    #region Rebuild

    IEnumerator DelayedInitialBuild()
    {
        // Wait 1 frame so InputSystem has a chance to finish device initialization.
        yield return null;
        RebuildAllPlayers();
    }

    void RebuildAllPlayers()
    {
        // 1) Clean up old spawned players (only the ones we created)
        if (setupMode == SetupMode.SpawnJamPlayers)
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null)
                    Destroy(_spawned[i]);
            }
            _spawned.Clear();
        }

        // 2) Reset device claims for this rebuild
        _claimedKeyboardMouse = false;

        // Re-snapshot gamepads at rebuild time (after devices are initialized)
        _claimedGamepads = new bool[Gamepad.all.Count];

        // 3) Build from slots
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.kind == JamPlayerKind.Disabled)
                continue;

            if (setupMode == SetupMode.SpawnJamPlayers)
            {
                var go = SpawnSlot(i, slot.kind);
                if (go != null) _spawned.Add(go);
            }
            else
            {
                UseExistingAndConfigureSlot(i, slot.kind);
            }
        }

        // 4) After building, push manager debug settings to all relevant players
        ApplyDebugConfigToAllPlayers();
    }

    GameObject SpawnSlot(int slotIndex, JamPlayerKind kind)
    {
        if (jamPlayerPrefabOrTemplate == null)
        {
            Debug.LogError("[JamInputManager] Missing Jam Player Prefab/Template (jamPlayerPrefabOrTemplate).", this);
            return null;
        }

        var instance = Instantiate(jamPlayerPrefabOrTemplate, Vector3.zero, Quaternion.identity, spawnedPlayersParent);
        instance.name = $"[JamPlayer {slotIndex}] ({kind})";

        var pi = instance.GetComponent<PlayerInput>();
        if (pi == null)
        {
            Debug.LogError($"[JamInputManager] Spawned Jam player is missing PlayerInput (slot {slotIndex}).", instance);
            Destroy(instance.gameObject);
            return null;
        }

        ConfigurePlayerInputForSlot(pi, kind);
        return instance.gameObject;
    }

    #endregion

    #region Existing Players Mode

    void UseExistingAndConfigureSlot(int slotIndex, JamPlayerKind kind)
    {
        if (existingPlayers == null || slotIndex < 0 || slotIndex >= existingPlayers.Length)
        {
            Debug.LogWarning($"[JamInputManager] Slot {slotIndex} has no matching entry in existingPlayers.", this);
            return;
        }

        var owner = existingPlayers[slotIndex];
        if (owner == null)
        {
            Debug.LogWarning($"[JamInputManager] existingPlayers[{slotIndex}] is null.", this);
            return;
        }

        // Ensure a rig exists as a CHILD (or reuse an existing one)
        var rigGO = EnsureJamRigChild(owner, slotIndex, kind, out var rigPI);
        if (rigGO == null || rigPI == null)
            return;

        // Optional: stable naming, without endlessly prefixing
        if (!owner.name.StartsWith("[JamPlayer "))
            owner.name = $"[JamPlayer {slotIndex}] ({kind}) {owner.name}";

        // Configure the rig's PlayerInput (NOT the owner/root)
        ConfigurePlayerInputForSlot(rigPI, kind);
    }

bool TryGetExistingJamRig(GameObject owner, out GameObject rigGO, out PlayerInput rigPI)
{
    rigGO = null;
    rigPI = null;
    if (owner == null) return false;

    // Prefer a JamInputPlayer-driven rig (this is the most "intentful" signal)
    var jip = owner.GetComponentInChildren<JamInputPlayer>(true);
    if (jip == null) return false;

    rigGO = jip.gameObject;

    // PlayerInput is usually on the same object; fallback to its children just in case
    rigPI = jip.GetComponent<PlayerInput>();
    if (rigPI == null)
        rigPI = jip.GetComponentInChildren<PlayerInput>(true);

    return rigPI != null;
}

GameObject EnsureJamRigChild(GameObject owner, int slotIndex, JamPlayerKind kind, out PlayerInput rigPI)
{
    rigPI = null;
    if (owner == null) return null;

    // If already present, do nothing (idempotent)
    if (TryGetExistingJamRig(owner, out var existingRig, out var existingPI))
    {
        rigPI = existingPI;
        return existingRig;
    }

    // If we have a prefab/template, instantiate it under the existing player
    if (jamPlayerPrefabOrTemplate != null)
    {
        var rig = Instantiate(jamPlayerPrefabOrTemplate, owner.transform);
        rig.name = "JamInputRig"; // stable name to help debugging/searching

        // Keep transform clean
        var t = rig.transform;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;

        rigPI = rig.GetComponent<PlayerInput>();
        if (rigPI == null)
        {
            Debug.LogError(
                $"[JamInputManager] jamPlayerPrefabOrTemplate is missing PlayerInput. Can't build rig for slot {slotIndex}.",
                rig);
            Destroy(rig.gameObject);
            return null;
        }

        // JamInputPlayer expected too; warn (don’t hard fail)
        if (rig.GetComponent<JamInputPlayer>() == null)
        {
            Debug.LogWarning(
                $"[JamInputManager] JamInputRig instantiated but has no JamInputPlayer (slot {slotIndex}).",
                rig);
        }

        return rig.gameObject;
    }

    // Fallback: preserve old behavior (adds onto root) to avoid breaking existing users
    var pi = owner.GetComponent<PlayerInput>();
    if (pi == null)
    {
        pi = owner.AddComponent<PlayerInput>();
        ApplyTemplatePlayerInputDefaultsIfPossible(pi);
        Debug.Log($"[JamInputManager] Added PlayerInput to existing player '{owner.name}' (slot {slotIndex}).", owner);
    }

    var jipRoot = owner.GetComponent<JamInputPlayer>();
    if (jipRoot == null)
    {
        owner.AddComponent<JamInputPlayer>();
        Debug.Log($"[JamInputManager] Added JamInputPlayer to existing player '{owner.name}' (slot {slotIndex}).", owner);
    }

    rigPI = pi;
    return owner;
}

    void ApplyTemplatePlayerInputDefaultsIfPossible(PlayerInput target)
    {
        if (jamPlayerPrefabOrTemplate == null || target == null)
            return;

        var templatePI = jamPlayerPrefabOrTemplate.GetComponent<PlayerInput>();
        if (templatePI == null || templatePI.actions == null)
            return;

        // Copy only safe, high-value defaults (version-stable across Unity releases)
        target.actions = templatePI.actions;
        target.defaultActionMap = string.IsNullOrWhiteSpace(templatePI.defaultActionMap)
            ? DefaultActionMap
            : templatePI.defaultActionMap;

        target.neverAutoSwitchControlSchemes = templatePI.neverAutoSwitchControlSchemes;
        target.defaultControlScheme = templatePI.defaultControlScheme;
        target.notificationBehavior = templatePI.notificationBehavior;
    }

    #endregion

    #region PlayerInput Configuration

    void ConfigurePlayerInputForSlot(PlayerInput pi, JamPlayerKind kind)
    {
        if (pi == null) return;

        // Short warnings only when likely to break input
        if (pi.actions == null)
            Debug.LogWarning("[JamInputManager] PlayerInput has no Actions asset assigned. Assign JamInputActions.", pi);

        if (string.IsNullOrWhiteSpace(pi.defaultActionMap))
            pi.defaultActionMap = DefaultActionMap;

        switch (kind)
        {
            case JamPlayerKind.HumanKeyboardMouse:
                ConfigureAsKeyboardMouse(pi);
                break;

            case JamPlayerKind.HumanGamepadAny:
                ConfigureAsAnyGamepad(pi);
                break;

            case JamPlayerKind.HumanAnyUnusedConnected:
                ConfigureAsAnyUnusedConnected(pi);
                break;

            case JamPlayerKind.SimulatedBot:
                ConfigureAsBot(pi);
                break;
        }
    }

    void ConfigureAsKeyboardMouse(PlayerInput pi)
    {
        pi.enabled = true;
        pi.neverAutoSwitchControlSchemes = true;

        // If KBM already used by another slot, this slot must wait.
        if (_claimedKeyboardMouse)
        {
            SetWaiting(pi, "Keyboard+Mouse already claimed");
            return;
        }

        if (Keyboard.current != null && Mouse.current != null)
        {
            pi.defaultControlScheme = SchemeKeyboardMouse;
            pi.SwitchCurrentControlScheme(SchemeKeyboardMouse, Keyboard.current, Mouse.current);
            _claimedKeyboardMouse = true;
        }
        else
        {
            SetWaiting(pi, "Keyboard/Mouse not found");
        }
    }

    void ConfigureAsAnyGamepad(PlayerInput pi)
    {
        pi.enabled = true;
        pi.neverAutoSwitchControlSchemes = true;
        pi.defaultControlScheme = SchemeGamepad;

        var gp = ClaimNextUnusedGamepad();
        if (gp != null)
        {
            pi.SwitchCurrentControlScheme(SchemeGamepad, gp);
        }
        else
        {
            SetWaiting(pi, "No unused gamepad available");
        }
    }

    bool EnsurePlayerInputUserReady(PlayerInput pi)
    {
        if (pi == null) return false;

        // Force initialization sequence
        if (!pi.enabled) pi.enabled = true;

        // ActivateInput helps PlayerInput create its InputUser in many cases
        try { pi.ActivateInput(); } catch { /* ignore */ }

        return pi.user.valid;
    }

    void ConfigureAsAnyUnusedConnected(PlayerInput pi)
    {
        pi.enabled = true;
        pi.neverAutoSwitchControlSchemes = true;

        // If user isn't ready yet, don't call SwitchCurrentControlScheme this frame.
        if (!EnsurePlayerInputUserReady(pi))
        {
            StartCoroutine(DelayedSwitchForAnyUnused(pi));
            return;
        }

        var gp = ClaimNextUnusedGamepad();
        if (gp != null)
        {
            pi.defaultControlScheme = SchemeGamepad;
            pi.SwitchCurrentControlScheme(SchemeGamepad, gp);
            return;
        }

        if (!_claimedKeyboardMouse && Keyboard.current != null && Mouse.current != null)
        {
            pi.defaultControlScheme = SchemeKeyboardMouse;
            pi.SwitchCurrentControlScheme(SchemeKeyboardMouse, Keyboard.current, Mouse.current);
            _claimedKeyboardMouse = true;
            return;
        }

        // NEW: quiet waiting state (no scheme, no input)
        SetWaiting(pi, "No unused device (gamepad or KBM) available");
    }


    IEnumerator DelayedSwitchForAnyUnused(PlayerInput pi)
    {
        yield return null; // wait 1 frame
        if (pi == null) yield break;

        if (!EnsurePlayerInputUserReady(pi))
        {
       //     Debug.LogWarning("[JamInputManager] PlayerInput user still invalid after 1 frame; skipping SwitchCurrentControlScheme.", pi);
            yield break;
        }

        // Call again now that user exists
        ConfigureAsAnyUnusedConnected(pi);
    }

    void ConfigureAsBot(PlayerInput pi)
    {
        // Bot should not react to real devices.
        pi.enabled = false;
    }

    Gamepad ClaimNextUnusedGamepad()
    {
        var list = Gamepad.all;

        if (_claimedGamepads == null || _claimedGamepads.Length != list.Count)
            _claimedGamepads = new bool[list.Count];

        for (int i = 0; i < list.Count; i++)
        {
            if (!_claimedGamepads[i])
            {
                _claimedGamepads[i] = true;
                return list[i];
            }
        }

        return null;
    }

    #endregion

    #region Debug Application (Manager -> Players)

    void ApplyDebugConfigToAllPlayers()
    {
        if (!debug.Enabled)
        {
            // If debug is OFF, disable JamInputDebug wherever it exists (but don’t destroy it).
            ForEachManagedPlayer(go =>
            {
                if (go == null) return;
                var dbg = go.GetComponent<JamInputDebug>();
                if (dbg != null) dbg.enabled = false;
            });
            return;
        }

        ForEachManagedPlayer(go =>
        {
            if (go == null) return;

            var dbg = go.GetComponent<JamInputDebug>();
            if (dbg == null) dbg = go.AddComponent<JamInputDebug>();

            debug.ApplyPresetTo(dbg);
        });
    }

    void ForEachManagedPlayer(System.Action<GameObject> action)
    {
        if (action == null) return;

        if (setupMode == SetupMode.SpawnJamPlayers)
        {
            for (int i = 0; i < _spawned.Count; i++)
                action(_spawned[i]);
        }
        else
        {
            // Only apply to slots that are not Disabled (avoid toggling random objects).
            for (int i = 0; i < slots.Length && i < existingPlayers.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.kind == JamPlayerKind.Disabled) continue;

                var owner = existingPlayers[i];
                if (owner == null) continue;

                // Prefer the rig (child with JamInputPlayer + PlayerInput); fallback to owner
                if (TryGetExistingJamRig(owner, out var rigGO, out _))
                    action(rigGO);
                else
                    action(owner);
            }
        }
    }

    #endregion

    #region Slot Helpers
    void SetWaiting(PlayerInput pi, string reason)
    {
        if (pi == null) return;

        // Prevent PlayerInput from trying to auto-pair or auto-switch.
        pi.neverAutoSwitchControlSchemes = true;

        // Important: stop input so this slot is "quiet" until it gets a real device.
        pi.DeactivateInput();

        // Optional: clear default scheme so OnEnable doesn't try to match anything.
        pi.defaultControlScheme = null;

        // Keep this as Log (not Warning) so it doesn't feel like an error.
        Debug.Log($"[JamInputManager] {pi.gameObject.name} is waiting for a device. ({reason})", pi);
    }

    

    void NormalizeSlotsArray()
    {
        // slots array
        if (slots == null || slots.Length != MaxSlots)
        {
            var newSlots = new JamPlayerSlot[MaxSlots];
            for (int i = 0; i < newSlots.Length; i++)
            {
                newSlots[i] = (slots != null && i < slots.Length && slots[i] != null)
                    ? slots[i]
                    : new JamPlayerSlot();
            }
            slots = newSlots;
        }
        else
        {
            for (int i = 0; i < slots.Length; i++)
                EnsureSlotExists(i);
        }

        // existingPlayers array
        if (existingPlayers == null || existingPlayers.Length != MaxSlots)
        {
            var newArr = new GameObject[MaxSlots];
            if (existingPlayers != null)
            {
                for (int i = 0; i < Mathf.Min(existingPlayers.Length, MaxSlots); i++)
                    newArr[i] = existingPlayers[i];
            }
            existingPlayers = newArr;
        }
    }

    bool EnsureSlotExists(int slotIndex)
    {
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length)
            return false;

        if (slots[slotIndex] == null)
            slots[slotIndex] = new JamPlayerSlot();

        return true;
    }

    #endregion

#if UNITY_EDITOR
    #region Editor (Inline, so we still ship only 3 scripts)

    [CustomEditor(typeof(JamInputManager))]
    class JamInputManagerEditor : Editor
    {
        SerializedProperty setupMode;
        SerializedProperty jamPlayerPrefabOrTemplate;
        SerializedProperty spawnedPlayersParent;
        SerializedProperty existingPlayers;
        SerializedProperty slots;
        SerializedProperty debug;

        bool showDebug = true;

        void OnEnable()
        {
            setupMode = serializedObject.FindProperty("setupMode");
            jamPlayerPrefabOrTemplate = serializedObject.FindProperty("jamPlayerPrefabOrTemplate");
            spawnedPlayersParent = serializedObject.FindProperty("spawnedPlayersParent");
            existingPlayers = serializedObject.FindProperty("existingPlayers");
            slots = serializedObject.FindProperty("slots");
            debug = serializedObject.FindProperty("debug");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Jam Input Manager", EditorStyles.boldLabel);

            // Lightweight, inspector-only warnings (no spam in console)
            var mgr = (JamInputManager)target;
            DrawSetupWarnings(mgr);

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(setupMode);
            EditorGUILayout.PropertyField(jamPlayerPrefabOrTemplate);
            EditorGUILayout.PropertyField(spawnedPlayersParent);

            if ((SetupMode)setupMode.enumValueIndex == SetupMode.UseExistingPlayerObjects)
                EditorGUILayout.PropertyField(existingPlayers, true);

            EditorGUILayout.Space(8);
            EditorGUILayout.PropertyField(slots, true);

            EditorGUILayout.Space(10);
            showDebug = EditorGUILayout.Foldout(showDebug, "Debug (controls JamInputDebug)", true);
            if (showDebug)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(debug, true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);


            serializedObject.ApplyModifiedProperties();
        }

        static void DrawSetupWarnings(JamInputManager mgr)
        {
            if (mgr == null) return;

            if (mgr.setupMode == SetupMode.SpawnJamPlayers && mgr.jamPlayerPrefabOrTemplate == null)
            {
                EditorGUILayout.HelpBox(
                    "SpawnJamPlayers needs a Jam Player Prefab/Template with PlayerInput + JamInputPlayer.",
                    MessageType.Error);
            }

            if (mgr.setupMode == SetupMode.UseExistingPlayerObjects)
            {
                bool anyAssigned = false;
                if (mgr.existingPlayers != null)
                {
                    for (int i = 0; i < mgr.existingPlayers.Length; i++)
                    {
                        if (mgr.existingPlayers[i] != null) { anyAssigned = true; break; }
                    }
                }

                if (!anyAssigned)
                {
                    EditorGUILayout.HelpBox(
                        "UseExistingPlayerObjects: assign at least one existing player object (size 4 array).",
                        MessageType.Warning);
                }

                if (mgr.jamPlayerPrefabOrTemplate == null)
                {
                    EditorGUILayout.HelpBox(
                        "No template assigned: I can still add PlayerInput, but Actions/default map may be empty unless you set them.",
                        MessageType.Info);
                }
            }
        }
    }

    #endregion
#endif
    
    void OnEnable()  => InputSystem.onDeviceChange += OnDeviceChange;
    void OnDisable() => InputSystem.onDeviceChange -= OnDeviceChange;

    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
            RebuildAllPlayers();
    }
}
