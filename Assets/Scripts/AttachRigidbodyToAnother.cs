using UnityEngine;
using UnityEngine.InputSystem;

public class AttachRigidbodyToAnother : MonoBehaviour
{
    Rigidbody _rb;
    FixedJoint _connection;

    public Rigidbody otherRB;
    public InputAction grab;

    [Header("Indicator")]
    public Color Highlight = Color.cyan;
    public Color DefaultColor = Color.gray;

    [Header("Optional snap point (child near palm)")]
    public Transform GrabPoint; // assign RightGrabPoint / LeftGrabPoint if you want palm snap

    [Header("Pull (only while NOT latched)")]
    public float Force = 50f;             // spring strength
    public float Damping = 10f;           // kills rubber banding
    public float MaxPullForce = 700f;     // clamp
    public float SnapDistance = 0.25f;    // when close, create joint

    [Header("Hold Stabilizers")]
    public float HeldExtraDrag = 3f;      // add drag to held object while held
    public float JointBreakForce = 6000f; // break instead of exploding

    public ForceMode ForceMode = ForceMode.Force;

    MeshRenderer _mr;
    Color _originalColor;
    bool _hasColor;

    float _originalDrag;

    public bool IsHoldingSomething() => _connection != null && otherRB != null;
    public Rigidbody CurrentHeldRigidbody() => otherRB;
    private void Awake()
    {
        _rb = GetComponentInParent<Rigidbody>();
        grab.Enable();
    }

    private void FixedUpdate()
    {
        float g = grab.ReadValue<float>();

        // RELEASE ALWAYS (not only during collision)
        if (_connection != null && g <= 0.05f)
        {
            Destroy(_connection);
            _connection = null;

            if (otherRB != null) otherRB.linearDamping = _originalDrag;
            return;
        }

        if (otherRB == null) return;
        if (g <= 0.05f) return;

        // If already holding with this hand, do NOT keep pulling (coop rubber-band fix)
        if (_connection != null) return;

        Vector3 grabPos = (GrabPoint != null) ? GrabPoint.position : _rb.worldCenterOfMass;
        Vector3 targetPos = otherRB.worldCenterOfMass;

        Vector3 delta = targetPos - grabPos;
        float dist = delta.magnitude;

        // If close enough, latch now (no collision timing needed)
        if (dist <= SnapDistance)
        {
            Latch();
            return;
        }

        // Pull toward target (spring + damping)
        Vector3 dir = (dist > 0.0001f) ? (delta / dist) : Vector3.zero;

        // spring proportional to distance
        float spring = Force * dist;

        // damp relative velocity along the pull direction
        float relVel = Vector3.Dot((_rb.linearVelocity - otherRB.linearVelocity), dir);

        float f = spring - (Damping * relVel);
        f = Mathf.Clamp(f, 0f, MaxPullForce);

        Vector3 pull = dir * f * g;

        // IMPORTANT: remove vertical pull to prevent levitation/climbing
        pull.y = 0f;

        // If extremely close / intersecting, don't pull � latch instead (prevents solver pop)
        if (dist <= SnapDistance * 0.8f)
        {
            Latch();
            return;
        }

        _rb.AddForce(pull, ForceMode);
        otherRB.AddForce(-pull * 0.5f, ForceMode);
    }

    void Latch()
    {
        if (_connection != null) return;
        if (otherRB == null) return;

        // Multi-hand friendly: each hand adds its own joint component to the object
        _connection = otherRB.gameObject.AddComponent<FixedJoint>();
        _connection.connectedBody = _rb;
        _connection.enableCollision = false;

        // Break instead of infinite fight
        _connection.breakForce = JointBreakForce;
        _connection.breakTorque = JointBreakForce;

        // Add drag while held to reduce jitter
        _originalDrag = otherRB.linearDamping;
        otherRB.linearDamping = Mathf.Max(_originalDrag, HeldExtraDrag);
    }

    // highlight targets
    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null) return;
        if (other.attachedRigidbody.isKinematic) return;

        if (otherRB == null)
        {
            otherRB = other.attachedRigidbody;

            _mr = otherRB.GetComponent<MeshRenderer>();
            if (_mr != null && _mr.material != null)
            {
                _hasColor = true;
                _originalColor = _mr.material.color;
                _mr.material.color = Highlight;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody != null && other.attachedRigidbody == otherRB)
        {
            // Only restore color if this hand is not currently holding
            if (_connection == null && _hasColor && _mr != null && _mr.material != null)
                _mr.material.color = _originalColor;

            _mr = null;
            _hasColor = false;
            otherRB = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Keep your original pattern, but it's not required anymore
        if (_connection != null) return;
        if (otherRB == null) return;

        if (collision.rigidbody == otherRB && grab.ReadValue<float>() > 0.05f)
            Latch();
    }

    private void OnCollisionStay(Collision collision)
    {
        // harmless fallback
        if (_connection != null && grab.ReadValue<float>() <= 0.05f)
        {
            Destroy(_connection);
            _connection = null;
            if (otherRB != null) otherRB.linearDamping = _originalDrag;
        }
    }

    private void OnJointBreak(float breakForce)
    {
        // If it breaks because two players pull opposite directions
        _connection = null;
        if (otherRB != null) otherRB.linearDamping = _originalDrag;
    }
}
