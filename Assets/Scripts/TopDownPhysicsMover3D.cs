using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class TopDownPhysicsMover3D : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("If empty, this will search on self, children, then parent.")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Controller Feel (Slop-Friendly)")]
    [Tooltip("Max horizontal speed (XZ).")]
    [SerializeField] private float maxSpeed = 10f;

    [Tooltip("Acceleration toward desired speed when pushing stick.")]
    [SerializeField] private float acceleration = 30f;

    [Tooltip("How quickly you slow down when NOT pushing stick (lower = more slide).")]
    [SerializeField] private float deceleration = 10f;

    [Tooltip("Extra accel when reversing direction (helps controller turning feel).")]
    [SerializeField] private float turnAssistMultiplier = 1.8f;

    [Tooltip("Radial deadzone for stick drift.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float inputDeadzone = 0.20f;

    [Header("Physics")]
    [Tooltip("Keep upright / prevent tipping from collisions.")]
    [SerializeField] private bool freezeRotation = true;

    [Tooltip("If true, movement is relative to camera forward/right (good for couch co-op).")]
    [SerializeField] private bool cameraRelative = false;

    [Tooltip("Camera used for camera-relative movement. If empty, uses Camera.main.")]
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;

    private void Awake()
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

        if (cameraTransform == null && cameraRelative && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        if (playerInput == null || !playerInput.user.valid)
            return;

        int p = playerInput.playerIndex;
        var input = JamInput.Get(p);
        if (input == null)
            return;

        // --- Read move from your JamInput system ---
        Vector2 raw = input.Move;

        // --- Radial deadzone + preserve analog magnitude (controller-first) ---
        float mag = raw.magnitude;
        if (mag < inputDeadzone)
        {
            raw = Vector2.zero;
            mag = 0f;
        }
        else
        {
            // Remap [deadzone..1] -> [0..1], keep direction
            mag = Mathf.InverseLerp(inputDeadzone, 1f, mag);
            raw = raw.normalized * mag;
        }

        // --- Convert input into world direction ---
        Vector3 wishDir = new Vector3(raw.x, 0f, raw.y);

        if (cameraRelative && cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            wishDir = (camRight * raw.x) + (camForward * raw.y);
        }

        // Clamp for safety (should already be <= 1)
        if (wishDir.sqrMagnitude > 1f)
            wishDir.Normalize();

        // --- Current velocity (XZ only) ---
        Vector3 v = rb.linearVelocity;
        Vector3 vXZ = new Vector3(v.x, 0f, v.z);

        // --- Target velocity (analog magnitude already applied) ---
        Vector3 targetVXZ = wishDir * maxSpeed;

        // --- Turn assist: if reversing direction, boost accel so it feels good on stick ---
        float rate;
        if (wishDir.sqrMagnitude > 0f)
        {
            float dot = 1f;
            if (vXZ.sqrMagnitude > 0.01f && targetVXZ.sqrMagnitude > 0.01f)
                dot = Vector3.Dot(vXZ.normalized, targetVXZ.normalized);

            float turnBoost01 = Mathf.InverseLerp(1f, -1f, dot); // 0 = same dir, 1 = opposite dir
            float boostedAccel = Mathf.Lerp(acceleration, acceleration * turnAssistMultiplier, turnBoost01);
            rate = boostedAccel;
        }
        else
        {
            rate = deceleration;
        }

        // --- Smoothly move current velocity toward target velocity ---
        Vector3 newVXZ = Vector3.MoveTowards(vXZ, targetVXZ, rate * Time.fixedDeltaTime);

        // Apply back, keep Y unchanged
        rb.linearVelocity = new Vector3(newVXZ.x, v.y, newVXZ.z);
    }
}
