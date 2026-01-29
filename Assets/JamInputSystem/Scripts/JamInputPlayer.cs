using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Input wrapper for a single player, backed by Unity's Input System (PlayerInput).
/// <para>
/// This component auto-registers with <see cref="JamInput"/> on enable/disable.
/// Action map and action names are intentionally locked for a streamlined jam workflow.
/// </para>
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class JamInputPlayer : MonoBehaviour
{
    // Locked action-map contract (jam package convention)
    const string MapName = "Player";

    const string ActMove        = "Move";
    const string ActLook        = "Look";
    const string ActPrimary     = "Primary";
    const string ActSecondary   = "Secondary";
    const string ActJump        = "Jump";
    const string ActInteract    = "Interact";
    const string ActPause       = "Pause";

    const string ActSpecial     = "Special";
    const string ActUltimate    = "Ultimate";
    const string ActCrouch      = "Crouch";
    const string ActSprint      = "Sprint";

    const string ActAction2     = "Action 2";
    const string ActMenu        = "Menu";

    const string ActCyclePrev   = "CyclePrev";
    const string ActCycleNext   = "CycleNext";
    const string ActCycleScroll = "CycleScroll";

    [Header("Debug")]
    [SerializeField] bool logDevicesOnAwake = false;

    PlayerInput _playerInput;

    // Core actions
    InputAction _move;
    InputAction _look;
    InputAction _primary;
    InputAction _secondary;
    InputAction _jump;
    InputAction _interact;
    InputAction _pause;

    // Extended actions
    InputAction _special;
    InputAction _ultimate;
    InputAction _crouch;
    InputAction _sprint;

    // Extra actions
    InputAction _action2;
    InputAction _menu;

    // Cycling
    InputAction _cyclePrev;
    InputAction _cycleNext;
    InputAction _cycleScroll;

    /// <summary>
    /// Raw scroll value read from the "CycleScroll" action (typically mouse wheel).
    /// </summary>
    public Vector2 CycleScrollRaw => SafeReadVector2(_cycleScroll);

    /// <summary>
    /// PlayerInput's player index, or -1 if the PlayerInput reference is missing.
    /// </summary>
    public int PlayerIndex => _playerInput != null ? _playerInput.playerIndex : -1;

    // --------- Unity ---------

    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();

        // If Actions asset is missing, keep behavior: log error and leave actions unassigned.
        if (_playerInput.actions == null)
        {
            Debug.LogError("[JamInputPlayer] PlayerInput has no Actions asset assigned.", this);
            return;
        }

        // Keep current behavior: FindActionMap(..., true) and FindAction(..., true)
        // will throw if the setup is wrong (fast feedback for jams).
        var map = _playerInput.actions.FindActionMap(MapName, true);

        _move      = map.FindAction(ActMove, true);
        _look      = map.FindAction(ActLook, true);
        _primary   = map.FindAction(ActPrimary, true);
        _secondary = map.FindAction(ActSecondary, true);
        _jump      = map.FindAction(ActJump, true);
        _interact  = map.FindAction(ActInteract, true);
        _pause     = map.FindAction(ActPause, true);

        _special   = map.FindAction(ActSpecial, true);
        _ultimate  = map.FindAction(ActUltimate, true);
        _crouch    = map.FindAction(ActCrouch, true);
        _sprint    = map.FindAction(ActSprint, true);

        _action2   = map.FindAction(ActAction2, true);
        _menu      = map.FindAction(ActMenu, true);

        _cyclePrev   = map.FindAction(ActCyclePrev, true);
        _cycleNext   = map.FindAction(ActCycleNext, true);
        _cycleScroll = map.FindAction(ActCycleScroll, true);

        if (logDevicesOnAwake)
        {
            var assetName = _playerInput.actions != null ? _playerInput.actions.name : "<null>";
            Debug.Log($"[JamInputPlayer] Actions wired (PlayerIndex={_playerInput.playerIndex}, Actions='{assetName}', Map='{MapName}').", this);
        }
    }

    void OnEnable()  => JamInput.Register(this);
    void OnDisable() => JamInput.Unregister(this);

    // --------- PUBLIC API: VALUES ---------

    /// <summary>Movement input (Vector2). Returns Vector2.zero if setup is broken.</summary>
    public Vector2 Move => SafeReadVector2(_move);

    /// <summary>Look/aim input (Vector2). Returns Vector2.zero if setup is broken.</summary>
    public Vector2 Look => SafeReadVector2(_look);

    // --------- BUTTON HELPERS (Down / Held / Up) ---------

    /// <summary>True only on the frame Primary was performed.</summary>
    public bool PrimaryDown => SafePerformedThisFrame(_primary);

    /// <summary>True while Primary is pressed.</summary>
    public bool PrimaryHeld => SafeIsPressed(_primary);

    /// <summary>True only on the frame Primary was released.</summary>
    public bool PrimaryUp => SafeReleasedThisFrame(_primary);

    public bool SecondaryDown => SafePerformedThisFrame(_secondary);
    public bool SecondaryHeld => SafeIsPressed(_secondary);
    public bool SecondaryUp   => SafeReleasedThisFrame(_secondary);

    public bool JumpDown => SafePerformedThisFrame(_jump);
    public bool JumpHeld => SafeIsPressed(_jump);
    public bool JumpUp   => SafeReleasedThisFrame(_jump);

    public bool InteractDown => SafePerformedThisFrame(_interact);
    public bool InteractHeld => SafeIsPressed(_interact);
    public bool InteractUp   => SafeReleasedThisFrame(_interact);

    /// <summary>Pause is typically a one-shot press.</summary>
    public bool PauseDown => SafePerformedThisFrame(_pause);

    /// <summary>Menu is typically a one-shot press.</summary>
    public bool MenuDown => SafePerformedThisFrame(_menu);

    public bool SpecialDown => SafePerformedThisFrame(_special);
    public bool SpecialHeld => SafeIsPressed(_special);
    public bool SpecialUp   => SafeReleasedThisFrame(_special);

    public bool UltimateDown => SafePerformedThisFrame(_ultimate);
    public bool UltimateHeld => SafeIsPressed(_ultimate);
    public bool UltimateUp   => SafeReleasedThisFrame(_ultimate);

    public bool CrouchDown => SafePerformedThisFrame(_crouch);
    public bool CrouchHeld => SafeIsPressed(_crouch);
    public bool CrouchUp   => SafeReleasedThisFrame(_crouch);

    public bool SprintDown => SafePerformedThisFrame(_sprint);
    public bool SprintHeld => SafeIsPressed(_sprint);
    public bool SprintUp   => SafeReleasedThisFrame(_sprint);

    /// <summary>Generic extra action for jams ("Action 2").</summary>
    public bool Action2Down => SafePerformedThisFrame(_action2);
    public bool Action2Held => SafeIsPressed(_action2);
    public bool Action2Up   => SafeReleasedThisFrame(_action2);

    // --------- CYCLING: COMBINED DELTA ---------

    /// <summary>
    /// -1 = cycle backwards (CyclePrev or scroll down)
    /// +1 = cycle forwards (CycleNext or scroll up)
    ///  0 = no cycle input this frame
    /// </summary>
    public int CycleDelta
    {
        get
        {
            int delta = 0;

            // Buttons
            if (SafePerformedThisFrame(_cyclePrev))
                delta -= 1;

            if (SafePerformedThisFrame(_cycleNext))
                delta += 1;

            // Mouse wheel: expects Vector2, usually (0, +/-value)
            if (_cycleScroll != null)
            {
                Vector2 scroll = _cycleScroll.ReadValue<Vector2>();
                const float threshold = 0.01f;

                if (scroll.y > threshold)      delta += 1;
                else if (scroll.y < -threshold) delta -= 1;
            }

            // Clamp just in case both sides fired
            if (delta > 1)  delta = 1;
            if (delta < -1) delta = -1;

            return delta;
        }
    }

    // --------- Safe read helpers (no behavior change when properly wired) ---------

    static Vector2 SafeReadVector2(InputAction action)
        => action != null ? action.ReadValue<Vector2>() : Vector2.zero;

    static bool SafePerformedThisFrame(InputAction action)
        => action != null && action.WasPerformedThisFrame();

    static bool SafeReleasedThisFrame(InputAction action)
        => action != null && action.WasReleasedThisFrame();

    static bool SafeIsPressed(InputAction action)
        => action != null && action.IsPressed();

#if UNITY_EDITOR
    // --------- Inspector-only warnings (only when broken) ---------
    void OnValidate()
    {
        // Keep it lightweight. No action lookups here (can be expensive/noisy),
        // only check obvious setup errors the inspector can fix.
        var pi = GetComponent<PlayerInput>();
        if (pi == null) return;

        // If actions are missing, this IS broken.
        // We don't log here (spam); we show a HelpBox in a custom editor below.
    }

    [CustomEditor(typeof(JamInputPlayer))]
    sealed class JamInputPlayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var jip = (JamInputPlayer)target;
            var pi = jip.GetComponent<PlayerInput>();

            if (pi == null)
            {
                EditorGUILayout.HelpBox("Missing PlayerInput component (required).", MessageType.Error);
            }
            else
            {
                if (pi.actions == null)
                {
                    EditorGUILayout.HelpBox(
                        "PlayerInput has no InputActionAsset assigned.\n" +
                        "Assign an Actions asset that contains a 'Player' action map with the expected actions.",
                        MessageType.Error
                    );
                }
                else
                {
                    // Map existence check (safe + cheap)
                    var map = pi.actions.FindActionMap(MapName, false);
                    if (map == null)
                    {
                        EditorGUILayout.HelpBox(
                            "Actions asset is missing the required 'Player' action map.\n" +
                            "Create an action map named 'Player' and add the locked actions (Move, Look, Primary, etc.).",
                            MessageType.Error
                        );
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            "JamInputPlayer uses locked action names for speed.\n" +
                            "If inputs don't work: verify the 'Player' map contains the expected actions and bindings.",
                            MessageType.Info
                        );
                    }
                }
            }

            EditorGUILayout.Space(6);
            DrawDefaultInspector();
        }
    }
#endif
}
