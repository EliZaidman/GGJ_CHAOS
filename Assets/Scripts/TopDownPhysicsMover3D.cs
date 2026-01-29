using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class TopDownPhysicsMover3D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private CarryMotorState carryState;

    [Header("Controller Feel (Slop-Friendly)")]
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float turnAssistMultiplier = 1.8f;

    [Range(0f, 0.5f)]
    [SerializeField] private float inputDeadzone = 0.20f;

    [Header("Physics")]
    [SerializeField] private bool freezeRotation = true;
    [SerializeField] private bool cameraRelative = false;
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (freezeRotation)
            rb.freezeRotation = true;

        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
            if (playerInput == null) playerInput = GetComponentInChildren<PlayerInput>(true);
            if (playerInput == null) playerInput = GetComponentInParent<PlayerInput>();
        }

        if (carryState == null)
            carryState = GetComponent<CarryMotorState>();

        if (cameraTransform == null && cameraRelative && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void FixedUpdate()
    {
        if (playerInput == null || !playerInput.user.valid)
            return;

        int p = playerInput.playerIndex;
        var input = JamInput.Get(p);
        if (input == null)
            return;

        // Carry modifiers
        float speedMult = 1f;
        bool locked = false;
        if (carryState != null)
        {
            speedMult = carryState.speedMultiplier;
            locked = carryState.movementLocked;
        }

        // Read move
        Vector2 raw = input.Move;

        // Radial deadzone + preserve analog magnitude
        float mag = raw.magnitude;
        if (mag < inputDeadzone)
        {
            raw = Vector2.zero;
            mag = 0f;
        }
        else
        {
            mag = Mathf.InverseLerp(inputDeadzone, 1f, mag);
            raw = raw.normalized * mag;
        }

        // If locked, ignore input movement (still physical, can be pushed)
        if (locked)
        {
            Vector3 v0 = rb.linearVelocity;
            Vector3 vXZ0 = new Vector3(v0.x, 0f, v0.z);
            Vector3 newVXZ0 = Vector3.MoveTowards(vXZ0, Vector3.zero, deceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(newVXZ0.x, v0.y, newVXZ0.z);
            return;
        }

        // Convert to world
        Vector3 wishDir = new Vector3(raw.x, 0f, raw.y);

        if (cameraRelative && cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            wishDir = camRight * raw.x + camForward * raw.y;
        }

        if (wishDir.sqrMagnitude > 1f)
            wishDir.Normalize();

        // Current velocity XZ
        Vector3 v = rb.linearVelocity;
        Vector3 vXZ = new Vector3(v.x, 0f, v.z);

        // Target velocity XZ (carry speed multiplier)
        Vector3 targetVXZ = wishDir * (maxSpeed * speedMult);

        // Turn assist
        float rate;
        if (wishDir.sqrMagnitude > 0f)
        {
            float dot = 1f;
            if (vXZ.sqrMagnitude > 0.01f && targetVXZ.sqrMagnitude > 0.01f)
                dot = Vector3.Dot(vXZ.normalized, targetVXZ.normalized);

            float turnBoost01 = Mathf.InverseLerp(1f, -1f, dot);
            float boostedAccel = Mathf.Lerp(acceleration, acceleration * turnAssistMultiplier, turnBoost01);
            rate = boostedAccel;
        }
        else
        {
            rate = deceleration;
        }

        Vector3 newVXZ = Vector3.MoveTowards(vXZ, targetVXZ, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(newVXZ.x, v.y, newVXZ.z);
    }
}
