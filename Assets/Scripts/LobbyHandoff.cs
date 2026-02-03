using UnityEngine;

public static class LobbyHandoff
{
    public const int MaxPlayers = 4;

    public static bool HasData;

    // For each slot (0..3):
    public static bool[] Active = new bool[MaxPlayers];
    public static bool[] IsKeyboardMouse = new bool[MaxPlayers];
    public static int[] GamepadDeviceId = new int[MaxPlayers]; // -1 means none

    public static void Clear()
    {
        HasData = false;
        for (int i = 0; i < MaxPlayers; i++)
        {
            Active[i] = false;
            IsKeyboardMouse[i] = false;
            GamepadDeviceId[i] = -1;
        }
    }
}