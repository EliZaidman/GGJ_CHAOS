using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple per-player input reader.
/// Attach this to the SAME GameObject that has PlayerInput.
///
/// This script:
/// - Reads input only for this player
/// - Prints player name, player index, control scheme, devices
/// - Prints which basic control was pressed
///
/// This is meant to later become part of JamInput itself,
/// so it stays intentionally simple and explicit.
/// </summary>
[DisallowMultipleComponent]
public class BasicControlsTemplate : MonoBehaviour
{
    [SerializeField] PlayerInput playerInput;

    void Awake()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }
    }

    void Update()
    {
        if (playerInput == null)
            return;

        // PlayerInput exists but user not ready yet
        if (!playerInput.user.valid)
            return;

        // No devices paired yet
        if (playerInput.devices.Count == 0)
            return;

        int playerIndex = playerInput.playerIndex;
        var input = JamInput.Get(playerIndex);

        if (input.PrimaryDown)   Log(playerIndex, "Primary DOWN");
        if (input.SecondaryDown) Log(playerIndex, "Secondary DOWN");
        if (input.JumpDown)      Log(playerIndex, "Jump DOWN");
        if (input.InteractDown)  Log(playerIndex, "Interact DOWN");
        if (input.SpecialDown)   Log(playerIndex, "Special DOWN");
        if (input.UltimateDown)  Log(playerIndex, "Ultimate DOWN");
        if (input.CrouchDown)    Log(playerIndex, "Crouch DOWN");
        if (input.SprintDown)    Log(playerIndex, "Sprint DOWN");
        if (input.PauseDown)     Log(playerIndex, "Pause DOWN");

        if (input.CycleDelta > 0) Log(playerIndex, "Cycle NEXT");
        if (input.CycleDelta < 0) Log(playerIndex, "Cycle PREVIOUS");
    }

    void Log(int playerIndex, string message)
    {
        string scheme = string.IsNullOrEmpty(playerInput.currentControlScheme)
            ? "UnknownScheme"
            : playerInput.currentControlScheme;

        string devices = "";
        for (int i = 0; i < playerInput.devices.Count; i++)
        {
            if (i > 0) devices += ", ";
            devices += playerInput.devices[i].displayName;
        }

        Debug.Log(
            $"[{gameObject.name}][P{playerIndex}][{scheme}][{devices}] {message}",
            this
        );
    }
}