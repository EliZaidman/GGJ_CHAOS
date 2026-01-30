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
    public Transform GrabPoint;

    [Header("Pull (only while NOT latched)")]
    public float Force = 50f;
    public float Damping = 10f;
    public float MaxPullForce = 700f;
    public float SnapDistance = 0.25f;

    [Header("Hold Stabilizers")]
    public float HeldExtraDrag = 3f;
    public float JointBreakForce = 6000f;

    public ForceMode ForceMode = ForceMode.Force;

    MeshRenderer _mr;
    Color _originalColor;
    bool _hasColor;

    float _originalDrag;

    Transform _selfRoot; // used to block self-grab
    PlayerInput _selfPlayerInput;

    public bool IsHoldingSomething() => _connection != null && otherRB != null;
    public Rigidbody CurrentHeldRigidbody() => otherRB;

    private void Awake()
    {
        _rb = GetComponentInParent<Rigidbody>();
        _selfRoot = _rb != null ? _rb.transform.root : transform.root;
        _selfPlayerInput = GetComponentInParent<PlayerInput>();

        grab.Enable();
    }

    private void FixedUpdate()
    {
        float g = grab.ReadValue<float>();

        // RELEASE
        if (_connection != null && g <= 0.05f)
        {
            Destroy(_connection);
            _connection = null;

            if (otherRB != null) otherRB.linearDamping = _originalDrag;
            return;
        }

        if (otherRB == null) return;
        if (g <= 0.05f) return;

        // already holding with this hand
        if (_connection != null) return;

        Vector3 grabPos = (GrabPoint != null) ? GrabPoint.position : _rb.worldCenterOfMass;
        Vector3 targetPos = otherRB.worldCenterOfMass;

        Vector3 delta = targetPos - grabPos;
        float dist = delta.magnitude;

        if (dist <= SnapDistance)
        {
            Latch();
            return;
        }

        Vector3 dir = (dist > 0.0001f) ? (delta / dist) : Vector3.zero;

        float spring = Force * dist;
        float relVel = Vector3.Dot((_rb.linearVelocity - otherRB.linearVelocity), dir);

        float f = spring - (Damping * relVel);
        f = Mathf.Clamp(f, 0f, MaxPullForce);

        Vector3 pull = dir * f * g;

        // keep it ground-y (no levitation)
        pull.y = 0f;

        if (dist <= SnapDistance * 0.8f)
        {
            Latch();
            return;
        }

        _rb.AddForce(pull, ForceMode);
        otherRB.AddForce(-pull * 0.5f, ForceMode);
    }

    bool CanTarget(Rigidbody candidate)
    {
        if (candidate == null) return false;
        if (candidate.isKinematic) return false;
        if (candidate == _rb) return false;

        // If the candidate belongs to the same PlayerInput as this hand -> it's self
        if (_selfPlayerInput != null)
        {
            var otherPI = candidate.GetComponentInParent<PlayerInput>();
            if (otherPI != null && otherPI == _selfPlayerInput)
                return false;
        }

        return true;
    }

    void Latch()
    {
        if (_connection != null) return;
        if (otherRB == null) return;

        // extra safety (in case otherRB was set somehow)
        if (!CanTarget(otherRB))
        {
            otherRB = null;
            return;
        }

        _connection = otherRB.gameObject.AddComponent<FixedJoint>();
        _connection.connectedBody = _rb;
        _connection.enableCollision = false;

        _connection.breakForce = JointBreakForce;
        _connection.breakTorque = JointBreakForce;

        _originalDrag = otherRB.linearDamping;
        otherRB.linearDamping = Mathf.Max(_originalDrag, HeldExtraDrag);
    }

    // highlight targets
    private void OnTriggerEnter(Collider other)
    {
        var cand = other.attachedRigidbody;
        if (!CanTarget(cand)) return;

        if (otherRB == null && _connection == null)
        {
            otherRB = cand;

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
            // If we're holding, keep the reference (don’t drop target just because collider left trigger)
            if (_connection != null) return;

            if (_hasColor && _mr != null && _mr.material != null)
                _mr.material.color = _originalColor;

            _mr = null;
            _hasColor = false;
            otherRB = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_connection != null) return;
        if (otherRB == null) return;

        if (collision.rigidbody == otherRB && grab.ReadValue<float>() > 0.05f)
            Latch();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (_connection != null && grab.ReadValue<float>() <= 0.05f)
        {
            Destroy(_connection);
            _connection = null;
            if (otherRB != null) otherRB.linearDamping = _originalDrag;
        }
    }

    private void OnJointBreak(float breakForce)
    {
        _connection = null;
        if (otherRB != null) otherRB.linearDamping = _originalDrag;
    }
}
