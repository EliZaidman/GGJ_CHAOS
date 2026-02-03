public static class LobbyHandoff
{
    public const int MaxPlayers = 4;

    public static bool HasData;

    public static bool[] Active = new bool[MaxPlayers];
    public static bool[] IsKeyboardMouse = new bool[MaxPlayers];
    public static int[] GamepadDeviceId = new int[MaxPlayers];

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