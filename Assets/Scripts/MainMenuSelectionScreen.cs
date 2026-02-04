using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenuSelectionScreen : MonoBehaviour
{
    [Header("UI Slots (size 4)")] public GameObject[] enable;
    public GameObject[] disable;
    private readonly List<PlayerInput> joinOrder = new();

    [Header("Rules")] [SerializeField] private int maxPlayers = 4;
    [SerializeField] private int minPlayersToStart = 1;
    [SerializeField] private int gameSceneBuildIndex = 1;

    [Header("Rotation")] public float rotationSpeed = 3f;

    private int index = 0;

    // Track join order so "left" removes the correct slot
    private readonly Dictionary<PlayerInput, int> playerToSlot = new();
    private readonly Dictionary<PlayerInput, Coroutine> playerToRoutine = new();
    private bool keyboardSlotTaken = false;


    private void Awake()
    {
        // Reset UI to "no players"
        for (int i = 0; i < enable.Length; i++)
        {
            if (enable[i]) enable[i].SetActive(false);
            if (disable[i]) disable[i].SetActive(true);
        }

        LobbyHandoff.Clear();

        index = 0;
        playerToSlot.Clear();
        playerToRoutine.Clear();
    }

   public void PlayerJoinedHandler(PlayerInput input)
{
    if (!input) return;

    // Don't register the same PlayerInput twice
    if (playerToSlot.ContainsKey(input))
        return;
    joinOrder.Add(input);

    // Cap players
    if (index >= maxPlayers || index >= enable.Length || index >= disable.Length)
    {
        Destroy(input.gameObject);
        return;
    }

    // Decide which slot this join gets
    int slot = index;

    // Determine if this joined player is using Keyboard/Mouse AND capture gamepad deviceId if any
    bool usesKeyboardOrMouse = false;
    int gamepadDeviceId = -1;

    for (int i = 0; i < input.devices.Count; i++)
    {
        var d = input.devices[i];
        if (d is Keyboard || d is Mouse)
            usesKeyboardOrMouse = true;

        if (d is Gamepad gp)
            gamepadDeviceId = gp.deviceId;
    }

    // Allow ONLY ONE Keyboard+Mouse player total (host choice)
    if (usesKeyboardOrMouse)
    {
        if (keyboardSlotTaken)
        {
            Destroy(input.gameObject);
            return;
        }
        keyboardSlotTaken = true;
    }

    // ---- Persist lobby selection for the game scene ----
    // Assumes you created LobbyHandoff (static) as described:
    // LobbyHandoff.HasData, Active[], IsKeyboardMouse[], GamepadDeviceId[]
    LobbyHandoff.HasData = true;
    LobbyHandoff.Active[slot] = true;
    LobbyHandoff.IsKeyboardMouse[slot] = usesKeyboardOrMouse;
    LobbyHandoff.GamepadDeviceId[slot] = gamepadDeviceId;
    // -----------------------------------------------

    // Track slot locally for menu UI / leave handling
    playerToSlot[input] = slot;

    if (enable[slot]) enable[slot].SetActive(true);
    if (disable[slot]) disable[slot].SetActive(false);

    index++;

    // Find Move action safely (for your rotating preview)
    InputAction move = null;
    var map = input.currentActionMap;
    if (map != null)
    {
        foreach (var a in map.actions)
        {
            if (a != null && a.name == "Move")
            {
                move = a;
                break;
            }
        }
    }

    if (move != null && enable[slot] != null)
        playerToRoutine[input] = StartCoroutine(ReadInputRoutine(move, enable[slot]));
    else
        Debug.LogWarning($"Joined player has no 'Move' action in map '{map?.name}'");
}



public void PlayerLeftHandler(PlayerInput input)
{
    if (!input) return;

    if (!playerToSlot.TryGetValue(input, out int slot))
        return;

    // Detect if this leaving player used keyboard/mouse
    bool usedKeyboardOrMouse = false;
    for (int i = 0; i < input.devices.Count; i++)
    {
        var d = input.devices[i];
        if (d is Keyboard || d is Mouse)
        {
            usedKeyboardOrMouse = true;
            break;
        }
    }

    if (usedKeyboardOrMouse)
        keyboardSlotTaken = false;

    // Stop preview routine
    if (playerToRoutine.TryGetValue(input, out var routine) && routine != null)
        StopCoroutine(routine);

    playerToRoutine.Remove(input);
    playerToSlot.Remove(input);
    joinOrder.Remove(input);

    // Reset slot UI
    if (enable != null && slot >= 0 && slot < enable.Length && enable[slot])
        enable[slot].SetActive(false);

    if (disable != null && slot >= 0 && slot < disable.Length && disable[slot])
        disable[slot].SetActive(true);

    // ---- IMPORTANT: Clear persisted lobby handoff for this slot ----
    if (slot >= 0 && slot < LobbyHandoff.MaxPlayers)
    {
        LobbyHandoff.Active[slot] = false;
        LobbyHandoff.IsKeyboardMouse[slot] = false;
        LobbyHandoff.GamepadDeviceId[slot] = -1;

        // Recompute HasData (any active slot)
        bool anyActive = false;
        for (int i = 0; i < LobbyHandoff.MaxPlayers; i++)
        {
            if (LobbyHandoff.Active[i]) { anyActive = true; break; }
        }
        LobbyHandoff.HasData = anyActive;
    }

    // Optional: if you still rely on index elsewhere, keep it sane
    index = playerToSlot.Count;
}



    private IEnumerator ReadInputRoutine(InputAction action, GameObject target)
    {
        float rotation = 0f;
        float vel = 0f;

        while (true)
        {
            if (action == null || target == null)
                yield break;

            var v2 = action.ReadValue<Vector2>();

            target.transform.Rotate(0, -rotation * Time.deltaTime, 0);

            rotation += v2.x * rotationSpeed;
            rotation = Mathf.SmoothDamp(rotation, 0, ref vel, 0.75f);
            rotation = Mathf.Clamp(rotation, -720f, 720f);

            yield return null;
        }
    }

    // Hook this to a UI button OR call it from Update when pressing Enter, etc.
    private bool _starting = false;

public void StartGame()
{
    if (_starting) return;

    // Use joinOrder as the authoritative order
    int joinedCount = (joinOrder != null) ? joinOrder.Count : playerToSlot.Count;
    if (joinedCount < minPlayersToStart)
        return;

    _starting = true;

    // Build a clean, compact handoff (0..N-1) in join order
    LobbyHandoff.Clear();                // IMPORTANT: clear here (safe) then fill
    LobbyHandoff.HasData = true;

    int slot = 0;
    var usedGamepadIds = new HashSet<int>();

    for (int j = 0; j < joinOrder.Count && slot < LobbyHandoff.MaxPlayers; j++)
    {
        var input = joinOrder[j];
        if (!input) continue;

        bool usesKM = false;
        int gamepadId = -1;

        // Detect devices for this player
        for (int d = 0; d < input.devices.Count; d++)
        {
            var dev = input.devices[d];
            if (dev is Keyboard || dev is Mouse) usesKM = true;
            if (dev is Gamepad gp) gamepadId = gp.deviceId;
        }

        // Only allow one KBM entry; skip extras defensively
        if (usesKM && keyboardSlotTaken && slot > 0)
        {
            // If you want to hard-reject extra KBM joins earlier, do it in PlayerJoinedHandler.
            // Here we just avoid corrupting the handoff.
            continue;
        }

        // Prevent duplicate gamepad ids ending up in two slots
        if (!usesKM && gamepadId != -1)
        {
            if (!usedGamepadIds.Add(gamepadId))
                continue;
        }

        LobbyHandoff.Active[slot] = true;
        LobbyHandoff.IsKeyboardMouse[slot] = usesKM;
        LobbyHandoff.GamepadDeviceId[slot] = usesKM ? -1 : gamepadId;

        slot++;
    }

    // If we ended up with fewer than min players after filtering, don't start
    if (slot < minPlayersToStart)
    {
        _starting = false;
        LobbyHandoff.Clear(); // leave lobby clean if we didn't start
        return;
    }

    // Release devices from menu PlayerInputs before switching scenes
    // (this prevents gameplay pairing from failing)
    foreach (var pi in FindObjectsOfType<PlayerInput>())
    {
        Destroy(pi.gameObject);
    }

    SceneManager.LoadScene(gameSceneBuildIndex);
}




    // Optional: quick keyboard start (Enter)
    private void Update()
    {
        // Keyboard host start
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            StartGame();

        // Any gamepad start (works for local + Remote Play virtual pads)
      /*  var pads = Gamepad.all;
        for (int i = 0; i < pads.Count; i++)
        {
            var pad = pads[i];
            if (pad != null && pad.startButton.wasPressedThisFrame) // best "Start" button
            {
                StartGame();
                break;
            }
        }*/
    }
}