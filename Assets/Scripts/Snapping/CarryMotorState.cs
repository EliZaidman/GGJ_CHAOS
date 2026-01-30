using UnityEngine;

public class CarryMotorState : MonoBehaviour
{
    [Header("Output to movement")]
    [Range(0f, 1f)] public float speedMultiplier = 1f;
    public bool movementLocked = false;

    [Header("Mass thresholds")]
    public float lightMax = 5f;
    public float mediumMax = 20f;
    public float heavyMax = 80f;

    [Header("Speed multipliers")]
    [Range(0f, 1f)] public float lightSpeed = 0.95f;
    [Range(0f, 1f)] public float mediumSpeed = 0.65f;
    [Range(0f, 1f)] public float heavySpeed = 0.35f;

    [Header("Player drag add (physics feel)")]
    public float mediumDragAdd = 1.0f;
    public float heavyDragAdd = 3.0f;
    public float superHeavyDragAdd = 6.0f;

    Rigidbody playerRb;
    float baseDrag;

    void Awake()
    {
        playerRb = GetComponent<Rigidbody>();
        if (playerRb != null) baseDrag = playerRb.linearDamping;
    }

    public void Clear()
    {
        speedMultiplier = 1f;
        movementLocked = false;

        if (playerRb != null) playerRb.linearDamping = baseDrag;
    }

    public void ApplyMass(float mass)
    {
        // Default
        movementLocked = false;

        if (mass <= lightMax)
        {
            speedMultiplier = lightSpeed;
            if (playerRb != null) playerRb.linearDamping = baseDrag;
            return;
        }

        if (mass <= mediumMax)
        {
            speedMultiplier = mediumSpeed;
            if (playerRb != null) playerRb.linearDamping = baseDrag + mediumDragAdd;
            return;
        }

        if (mass <= heavyMax)
        {
            speedMultiplier = heavySpeed;
            if (playerRb != null) playerRb.linearDamping = baseDrag + heavyDragAdd;
            return;
        }

        // Super heavy: you can still HOLD, but you can�t move (you said �stuck in place�)
        speedMultiplier = 0f;
        movementLocked = true;
        if (playerRb != null) playerRb.linearDamping = baseDrag + superHeavyDragAdd;
    }
}
