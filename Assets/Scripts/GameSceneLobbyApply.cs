using UnityEngine;

public class GameSceneLobbyApply : MonoBehaviour
{
    public JamInputManager jamInputManager;

    private void Awake()
    {
        if (jamInputManager != null)
            jamInputManager.ApplyLobbyHandoffAndRebuild();
    }
}