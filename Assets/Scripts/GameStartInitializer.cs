using UnityEngine;
using UnityEngine.InputSystem;

public class GameStartInitializer : MonoBehaviour
{
    public PlayerInput[] inputs;
    public PlayerInputManager manager;

    private void Start()
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            manager.JoinPlayer(i, i);
        }
    }
}
