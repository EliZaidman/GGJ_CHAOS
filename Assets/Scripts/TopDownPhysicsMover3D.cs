using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class TopDownPhysicsMover3D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private CarryMotorState carryState;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float turnAssistMultiplier = 1.8f;
    [Range(0f, 0.5f)]
    [SerializeField] private float inputDeadzone = 0.20f;

    [Header("Rotation (Right Stick)")]
    [SerializeField] private float rotationSpeed = 720f; // degrees/sec
    [Range(0f, 0.5f)]
    [SerializeField] private float lookDeadzone = 0.25f;
    [Tooltip("If true, player rotates toward movement when right stick idle")]
    [SerializeField] private bool fallbackRotateToMove = true;

    public float LinearNormalizedSpeed => rb.linearVelocity.magnitude / maxSpeed;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>()
                ?? GetComponentInChildren<PlayerInput>(true)
                ?? GetComponentInParent<PlayerInput>();
        }

        if (carryState == null)
            carryState = GetComponent<CarryMotorState>();

        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        if (playerInput == null || !playerInput.user.valid) return;

        var input = JamInput.Get(playerInput.playerIndex);
        if (input == null) return;

        float speedMult = 1f;
        bool locked = false;

        if (carryState != null)
        {
            speedMult = carryState.speedMultiplier;
            locked = carryState.movementLocked;
        }


        Vector2 moveRaw = input.Move;
        float moveMag = moveRaw.magnitude;

        if (moveMag < inputDeadzone)
        {
            moveRaw = Vector2.zero;
            moveMag = 0f;
        }
        else
        {
            moveMag = Mathf.InverseLerp(inputDeadzone, 1f, moveMag);
            moveRaw = moveRaw.normalized * moveMag;
        }

        Vector3 wishDir = new Vector3(moveRaw.x, 0f, moveRaw.y);
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        // If movement is locked (super heavy)
        if (locked)
        {
            Vector3 v0 = rb.linearVelocity;
            Vector3 vXZ0 = new Vector3(v0.x, 0f, v0.z);
            Vector3 newVXZ0 = Vector3.MoveTowards(
                vXZ0,
                Vector3.zero,
                deceleration * Time.fixedDeltaTime
            );
            rb.linearVelocity = new Vector3(newVXZ0.x, v0.y, newVXZ0.z);
        }
        else
        {
            Vector3 v = rb.linearVelocity;
            Vector3 vXZ = new Vector3(v.x, 0f, v.z);

            Vector3 targetVXZ = wishDir * (maxSpeed * speedMult);

            float rate;
            if (wishDir.sqrMagnitude > 0f)
            {
                float dot = (vXZ.sqrMagnitude > 0.01f && targetVXZ.sqrMagnitude > 0.01f)
                    ? Vector3.Dot(vXZ.normalized, targetVXZ.normalized)
                    : 1f;

                float turnBoost01 = Mathf.InverseLerp(1f, -1f, dot);
                rate = Mathf.Lerp(acceleration, acceleration * turnAssistMultiplier, turnBoost01);
            }
            else
            {
                rate = deceleration;
            }

            Vector3 newVXZ = Vector3.MoveTowards(
                vXZ,
                targetVXZ,
                rate * Time.fixedDeltaTime
            );

            rb.linearVelocity = new Vector3(newVXZ.x, v.y, newVXZ.z);
        }

        Vector2 lookRaw = input.Look;
        float lookMag = lookRaw.magnitude;

        bool usingLook = lookMag > lookDeadzone;

        Vector3 lookDir = Vector3.zero;

        if (usingLook)
        {
            lookRaw = lookRaw.normalized;
            lookDir = new Vector3(lookRaw.x, 0f, lookRaw.y);
        }
        else if (fallbackRotateToMove && wishDir.sqrMagnitude > 0.01f)
        {
            lookDir = wishDir;
        }

        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
            Quaternion newRot = Quaternion.RotateTowards(
                rb.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime
            );
            rb.MoveRotation(newRot);
        }
    }
}
