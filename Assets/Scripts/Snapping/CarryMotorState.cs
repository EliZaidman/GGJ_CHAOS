using UnityEngine;

public class CarryMotorState : MonoBehaviour
{
    [Range(0f, 1f)] public float speedMultiplier = 1f;
    public bool movementLocked = false;

    public void Clear()
    {
        speedMultiplier = 1f;
        movementLocked = false;
    }

    public void Apply(float mult, bool lockMove)
    {
        speedMultiplier = Mathf.Clamp01(mult);
        movementLocked = lockMove;
    }
}
