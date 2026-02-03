using UnityEngine;

public class GameSceneLobbyApply : MonoBehaviour
{
    public JamInputManager jamInputManager;

    private void Start()
    {
        if (jamInputManager != null)
            jamInputManager.ApplyLobbyHandoffAndRebuild();
    }
}