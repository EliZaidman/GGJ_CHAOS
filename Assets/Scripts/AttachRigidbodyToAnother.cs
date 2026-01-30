using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class AttachRigidbodyToAnother : MonoBehaviour
{
    Rigidbody _rb;
    FixedJoint _connection;
    public Rigidbody otherRB;
    public InputAction grab;
    public Color Highlight = Color.cyan;
    public AttachRigidbodyToAnother[] otherSystemsInContext;

    [Header("Pull")]
    public float Force = 50f;
    public float Damping = 8f;
    public float MaxPullForce = 800f;
    public float SnapDistance = 0.25f;
    public ForceMode ForceMode = ForceMode.Force;

    [Header("Where to snap (optional)")]
    public Transform GrabPoint;

    [Header("Co-op Stability")]
    public float HeldExtraDrag = 3f;        // NEW: calms jitter when 2 players hold
    public bool RemoveVerticalPull = true;  // NEW: prevents levitation/climbing

    Color _originalColor = Color.gray;
    MeshRenderer _mr;
    float _originalDrag;
    bool _dragOverridden;

    private void Awake()
    {
        _rb = GetComponentInParent<Rigidbody>();
        grab.Enable();
    }

    private void FixedUpdate()
    {
        float g = grab.ReadValue<float>();

        // Release ONLY your own joint, don't touch other player's joint
        if (_connection != null && g <= 0.05f)
        {
            Destroy(_connection);
            _connection = null;

            // restore drag only if we changed it
            if (otherRB != null && _dragOverridden)
            {
                otherRB.linearDamping = _originalDrag;
                _dragOverridden = false;
            }

            // don't force color changes here (co-op safe)
            return;
        }

        if (otherRB == null) return;
        if (_connection != null) return;
        if (g <= 0.05f) return;

        Vector3 grabPos = (GrabPoint != null) ? GrabPoint.position : _rb.worldCenterOfMass;
        Vector3 targetPos = otherRB.worldCenterOfMass;

        Vector3 delta = targetPos - grabPos;
        float dist = delta.magnitude;

        // Snap / latch
        if (dist <= SnapDistance)
        {
            _connection = otherRB.gameObject.AddComponent<FixedJoint>();
            _connection.connectedBody = _rb;
            _connection.enableCollision = false;

            // Add drag while held (helps massively with 2 players)
            if (!_dragOverridden)
            {
                _originalDrag = otherRB.linearDamping;
                otherRB.linearDamping = Mathf.Max(otherRB.linearDamping, HeldExtraDrag);
                _dragOverridden = true;
            }

            return;
        }

        // Pull (spring + damping)
        Vector3 dir = (dist > 0.0001f) ? (delta / dist) : Vector3.zero;

        float spring = Force * dist;

        float relVel = Vector3.Dot((_rb.linearVelocity - otherRB.linearVelocity), dir);

        float f = spring - (Damping * relVel);
        f = Mathf.Clamp(f, 0f, MaxPullForce);

        Vector3 pull = dir * f * g;

        // Prevent levitation / climbing when pushing into objects
        if (RemoveVerticalPull)
            pull.y = 0f;

        _rb.AddForce(pull, ForceMode);
        otherRB.AddForce(-pull * 0.5f, ForceMode);
    }

    // highlight targets (co-op safe: only set on enter, restore only on exit when not holding)
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
                _originalColor = _mr.material.color;
                _mr.material.color = Highlight;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody != null && other.attachedRigidbody == otherRB)
        {
            // Only restore color if THIS hand is not holding
            if (_connection == null && _mr != null && _mr.material != null)
                _mr.material.color = _originalColor;

            _mr = null;
            otherRB = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_connection != null) return;
        if (otherRB == null) return;

        if (collision.rigidbody == otherRB && grab.ReadValue<float>() > 0.05f)
        {
            _connection = otherRB.gameObject.AddComponent<FixedJoint>();
            _connection.connectedBody = _rb;
            _connection.enableCollision = false;

            if (!_dragOverridden)
            {
                _originalDrag = otherRB.linearDamping;
                otherRB.linearDamping = Mathf.Max(otherRB.linearDamping, HeldExtraDrag);
                _dragOverridden = true;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (_connection != null && grab.ReadValue<float>() <= 0.05f)
        {
            Destroy(_connection);
            _connection = null;

            if (otherRB != null && _dragOverridden)
            {
                otherRB.linearDamping = _originalDrag;
                _dragOverridden = false;
            }
        }
    }
}
