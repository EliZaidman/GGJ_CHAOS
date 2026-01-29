using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static access point for jam-friendly player input.
/// <para>
/// <see cref="JamInputPlayer"/> components register/unregister themselves here, enabling:
/// - Simple single-player shortcuts (player 0)
/// - Optional multiplayer access via <see cref="Get(int)"/>
/// </para>
/// </summary>
public static class JamInput
{
    static readonly List<JamInputPlayer> _players = new List<JamInputPlayer>();

#if UNITY_EDITOR
    static bool _warnedNoPlayersRegistered;
    static bool _warnedPlayer0Mismatch;
    static readonly HashSet<int> _warnedInvalidGetIndices = new HashSet<int>();
#endif

    /// <summary>
    /// Number of registered <see cref="JamInputPlayer"/> instances.
    /// </summary>
    public static int PlayerCount => _players.Count;

    /// <summary>
    /// Returns the registered player by list index (NOT by PlayerIndex).
    /// If the index is invalid, logs a warning and safely falls back to player 0 (if any).
    /// Returns null only if no players are registered at all.
    /// </summary>
    public static JamInputPlayer Get(int index)
    {
        // Valid index → return immediately
        if (index >= 0 && index < _players.Count)
            return _players[index];

#if UNITY_EDITOR
        // Warn only once per invalid index
        if (_warnedInvalidGetIndices.Add(index))
        {
            Debug.LogWarning(
                $"JamInput.Get({index}) was requested but only {_players.Count} player(s) are registered. " +
                "Falling back to player 0 if available."
            );
        }
#endif

        // Fallback: return player 0 if possible
        if (_players.Count > 0)
            return _players[0];

        // Absolute fallback: no players exist
        return null;
    }



    /// <summary>
    /// Called by <see cref="JamInputPlayer"/> when it becomes active.
    /// Keeps the internal list sorted by <see cref="JamInputPlayer.PlayerIndex"/>.
    /// </summary>
    internal static void Register(JamInputPlayer player)
    {
        if (player == null || _players.Contains(player))
            return;

        _players.Add(player);
        _players.Sort((a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));

#if UNITY_EDITOR
        // If the system is now "fixed", allow future warnings again.
        // (Example: you hit Play with a broken setup, stop, fix, Play again.)
        _warnedNoPlayersRegistered = false;
        _warnedPlayer0Mismatch = false;
#endif
    }

    /// <summary>
    /// Called by <see cref="JamInputPlayer"/> when it becomes inactive/destroyed.
    /// Keeps the internal list sorted by <see cref="JamInputPlayer.PlayerIndex"/>.
    /// </summary>
    internal static void Unregister(JamInputPlayer player)
    {
        if (player == null)
            return;

        _players.Remove(player);
        _players.Sort((a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));

#if UNITY_EDITOR
        // If removing a player causes a broken setup, we want to warn again next time input is accessed.
        _warnedNoPlayersRegistered = false;
        _warnedPlayer0Mismatch = false;
#endif
    }

    // ===== Single-player shortcuts (player 0) =====

    /// <summary>
    /// The "player 0" shortcut target.
    /// Returns the first registered player (sorted by PlayerIndex), or null if none are registered.
    /// </summary>
    static JamInputPlayer P0
    {
        get
        {
#if UNITY_EDITOR
            WarnIfSinglePlayerShortcutsAreBroken();
#endif
            return Get(0);
        }
    }

#if UNITY_EDITOR
    static void WarnIfSinglePlayerShortcutsAreBroken()
    {
        // Warn only when broken, and only once per Play session.
        if (_players.Count == 0)
        {
            if (_warnedNoPlayersRegistered) return;

            Debug.LogWarning(
                "JamInput: No JamInputPlayer is registered. Single-player shortcuts (JamInput.Move, PrimaryDown, etc.) " +
                "will return default values. Add a JamInputPlayer to the scene (usually PlayerIndex = 0)."
            );
            _warnedNoPlayersRegistered = true;
            return;
        }

        // If the lowest PlayerIndex isn't 0, the shortcuts will still function (they'll read the first player),
        // but the setup is likely not what the jam dev expects.
        if (_players[0] != null && _players[0].PlayerIndex != 0)
        {
            if (_warnedPlayer0Mismatch) return;

            Debug.LogWarning(
                $"JamInput: Single-player shortcuts expect PlayerIndex 0, but the lowest registered PlayerIndex is {_players[0].PlayerIndex}. " +
                "Shortcuts will read the lowest-index player anyway. If you intended true 'player 0', set a JamInputPlayer to PlayerIndex = 0."
            );
            _warnedPlayer0Mismatch = true;
        }
    }
#endif

    /// <summary>Movement input for player 0. Returns Vector2.zero if unavailable.</summary>
    public static Vector2 Move => P0 != null ? P0.Move : Vector2.zero;

    /// <summary>Look/aim input for player 0. Returns Vector2.zero if unavailable.</summary>
    public static Vector2 Look => P0 != null ? P0.Look : Vector2.zero;

    /// <summary>True only on the frame player 0 pressed Primary.</summary>
    public static bool PrimaryDown => P0 != null && P0.PrimaryDown;

    /// <summary>True while player 0 is holding Primary.</summary>
    public static bool PrimaryHeld => P0 != null && P0.PrimaryHeld;

    /// <summary>True only on the frame player 0 released Primary.</summary>
    public static bool PrimaryUp => P0 != null && P0.PrimaryUp;

    /// <summary>True only on the frame player 0 pressed Secondary.</summary>
    public static bool SecondaryDown => P0 != null && P0.SecondaryDown;

    /// <summary>True only on the frame player 0 pressed Jump.</summary>
    public static bool JumpDown => P0 != null && P0.JumpDown;

    /// <summary>True only on the frame player 0 pressed Interact.</summary>
    public static bool InteractDown => P0 != null && P0.InteractDown;

    /// <summary>True only on the frame player 0 pressed Pause.</summary>
    public static bool PauseDown => P0 != null && P0.PauseDown;

    /// <summary>True only on the frame player 0 pressed Special.</summary>
    public static bool SpecialDown => P0 != null && P0.SpecialDown;

    /// <summary>True only on the frame player 0 pressed Ultimate.</summary>
    public static bool UltimateDown => P0 != null && P0.UltimateDown;

    /// <summary>True only on the frame player 0 pressed Crouch.</summary>
    public static bool CrouchDown => P0 != null && P0.CrouchDown;

    /// <summary>True only on the frame player 0 pressed Sprint.</summary>
    public static bool SprintDown => P0 != null && P0.SprintDown;

    /// <summary>
    /// Menu/slot cycling delta for player 0 (e.g. -1 / +1).
    /// Returns 0 if unavailable.
    /// </summary>
    public static int CycleDelta => P0 != null ? P0.CycleDelta : 0;
}
