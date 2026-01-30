using UnityEngine;

public class TwoHandCarryAssist : MonoBehaviour
{
    public Rigidbody rootRB;
    public AttachRigidbodyToAnother leftHand;
    public AttachRigidbodyToAnother rightHand;

    public float forwardAssistForce = 180f;
    public float maxSpeedWhileCarrying = 6f;

    void Awake()
    {
        if (rootRB == null) rootRB = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (rootRB == null || leftHand == null || rightHand == null) return;

        // two hands holding SAME rigidbody
        bool twoHanded =
            leftHand.IsHoldingSomething() &&
            rightHand.IsHoldingSomething() &&
            leftHand.CurrentHeldRigidbody() == rightHand.CurrentHeldRigidbody();

        if (!twoHanded) return;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) return;
        forward.Normalize();

        Vector3 vel = rootRB.linearVelocity;
        vel.y = 0f;
        if (vel.magnitude > maxSpeedWhileCarrying) return;

        rootRB.AddForce(forward * forwardAssistForce, ForceMode.Force);
    }
}
