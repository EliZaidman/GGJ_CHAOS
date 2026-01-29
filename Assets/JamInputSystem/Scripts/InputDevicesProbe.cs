using UnityEngine;
using UnityEngine.InputSystem;

public class InputDevicesProbe : MonoBehaviour
{
    void Start()
    {
        Debug.Log("==== InputSystem.devices dump ====");
        foreach (var d in InputSystem.devices)
        {
            Debug.Log($"Device: {d.displayName}, layout={d.layout}, path={d.path}, type={d.GetType().Name}");
        }

        Debug.Log($"Gamepads count: {Gamepad.all.Count}");
        foreach (var g in Gamepad.all)
        {
            Debug.Log($"Gamepad: {g.displayName}, layout={g.layout}, path={g.path}");
        }

        Debug.Log($"Joysticks count: {Joystick.all.Count}");
        foreach (var j in Joystick.all)
        {
            Debug.Log($"Joystick: {j.displayName}, layout={j.layout}, path={j.path}");
        }
    }
}