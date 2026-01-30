using UnityEngine;
using UnityEngine.InputSystem;

public class AttachRigidbodyToAnother : MonoBehaviour
{
    Rigidbody _rb;
    FixedJoint _connection;

    [Header("Owner / Input")]
    [SerializeField] PlayerInput ownerPlayerInput;

    [Tooltip("Must match the action name in your Input Actions asset (e.g. GrabLeft / GrabRight)")]
    [SerializeField] string grabActionName = "GrabLeft";

    InputAction _grabAction;
    PlayerInput _selfPI;

    [Header("Target")]
    public Rigidbody otherRB;

    [Header("Indicator")]
    public Color Highlight = Color.cyan;

    [Header("Optional snap point (child near palm)")]
    public Transform GrabPoint;

    [Header("Pull / Snap (tuned for 1-hand grabbing)")]
    public float Force = 250f;
    public float Damping = 35f;
    public float MaxPullForce = 3500f;
    public float SnapDistance = 0.60f;

    [Header("Hold Stabilizers")]
    public float HeldExtraDrag = 6f;
    public float JointBreakForce = 15000f;

    [Header("Target Memory (sticky grab)")]
    public float TargetKeepTime = 0.35f;

    public ForceMode ForceMode = ForceMode.Force;

    MeshRenderer _mr;
    Color _originalColor;
    bool _hasColor;
    float _originalDrag;

    float _lastSawTargetTime;

    // Keep compatibility with your other scripts
    public bool IsHoldingSomething() => _connection != null && otherRB != null;
    public Rigidbody CurrentHeldRigidbody() => otherRB;

    void Awake()
    {
        _rb = GetComponentInParent<Rigidbody>();
        if (_rb == null) _rb = GetComponent<Rigidbody>();

        if (ownerPlayerInput == null)
            ownerPlayerInput = GetComponentInParent<PlayerInput>();

        _selfPI = ownerPlayerInput;

        if (ownerPlayerInput != null && ownerPlayerInput.actions != null)
        {
            _grabAction = ownerPlayerInput.actions.FindAction(grabActionName, true);
            _grabAction.Enable();
        }
    }

    bool GrabHeld()
    {
        if (_grabAction == null) return false;
        return _grabAction.ReadValue<float>() > 0.05f;
    }

    void FixedUpdate()
    {
        bool held = GrabHeld();

        // RELEASE
        if (_connection != null && !held)
        {
            Destroy(_connection);
            _connection = null;

            if (otherRB != null) otherRB.linearDamping = _originalDrag;
            return;
        }

        // Sticky target memory: if we haven't seen target recently and not holding, forget it
        if (_connection == null && otherRB != null && (Time.time - _lastSawTargetTime) > TargetKeepTime)
        {
            ClearTargetVisuals();
            otherRB = null;
        }

        if (otherRB == null) return;
        if (!held) return;
        if (_connection != null) return;

        Vector3 grabPos = (GrabPoint != null) ? GrabPoint.position : _rb.worldCenterOfMass;
        Vector3 targetPos = otherRB.worldCenterOfMass;

        Vector3 delta = targetPos - grabPos;
        float dist = delta.magnitude;

        // snap earlier (easier 1-hand)
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

        Vector3 pull = dir * f;

        // allow lifting but prevent rocket-launch
        pull.y = Mathf.Clamp(pull.y, -300f, 300f);

        // extra close -> snap to avoid solver pop
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

        // block self, allow other players
        if (_selfPI != null)
        {
            var otherPI = candidate.GetComponentInParent<PlayerInput>();
            if (otherPI != null && otherPI == _selfPI)
                return false;
        }

        return true;
    }

    void Latch()
    {
        if (_connection != null) return;
        if (otherRB == null) return;

        if (!CanTarget(otherRB))
        {
            ClearTargetVisuals();
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

        // once latched, visuals don't matter
        ClearTargetVisuals();
    }

    void OnTriggerEnter(Collider other)
    {
        var cand = other.attachedRigidbody;
        if (!CanTarget(cand)) return;

        // while holding something with this hand, ignore new targets
        if (_connection != null) return;

        // take/refresh target
        if (otherRB == null || otherRB == cand)
        {
            otherRB = cand;
            _lastSawTargetTime = Time.time;

            // highlight (optional)
            if (!_hasColor)
            {
                _mr = otherRB.GetComponent<MeshRenderer>();
                if (_mr != null && _mr.material != null)
                {
                    _hasColor = true;
                    _originalColor = _mr.material.color;
                    _mr.material.color = Highlight;
                }
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (_connection != null) return;
        if (otherRB == null) return;
        if (other.attachedRigidbody != otherRB) return;

        // keep target "alive" while inside trigger
        _lastSawTargetTime = Time.time;
    }

    void OnTriggerExit(Collider other)
    {
        if (_connection != null) return;

        if (other.attachedRigidbody != null && other.attachedRigidbody == otherRB)
        {
            // don't clear instantly; let TargetKeepTime handle it
            _lastSawTargetTime = Time.time;
        }
    }

    void OnJointBreak(float breakForce)
    {
        _connection = null;
        if (otherRB != null) otherRB.linearDamping = _originalDrag;
    }

    void ClearTargetVisuals()
    {
        if (_hasColor && _mr != null && _mr.material != null)
            _mr.material.color = _originalColor;

        _mr = null;
        _hasColor = false;
    }
}
