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

    // ===================== HIGHLIGHT (FIXED: reliable reset + multi-renderer) =====================
    public enum HighlightMode { None, Color, OutlineClone }

    [Header("Highlight")]
    public HighlightMode highlightMode = HighlightMode.Color;

    [Tooltip("Used when Highlight Mode = Color")]
    public Color Highlight = Color.cyan;

    [Tooltip("Used when Highlight Mode = OutlineClone (your outline/mask material)")]
    public Material OutlineMaterial;

    [Header("Highlight Auto Clear")]
    [Tooltip("If > 0, highlight will be removed after this many seconds unless refreshed while overlapping.")]
    public float HighlightLifetime = 0.25f;

    float _highlightExpireTime;

    // Color highlight (MaterialPropertyBlock) - does not mutate materials
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    MaterialPropertyBlock _mpb;

    Rigidbody _highlightedRB;
    Renderer[] _highlightedRenderers;

    // Outline clones (never touch original materials -> reset is always perfect)
    const string OutlineClonePrefix = "__OutlineClone__";
    readonly System.Collections.Generic.List<GameObject> _outlineClones = new System.Collections.Generic.List<GameObject>(16);

    // Robust overlap tracking (fixes multi-collider targets causing stuck visuals)
    int _targetOverlapCount;

    // ===================== GRAB POINT / FORCES (UNCHANGED PHYSICS) =====================
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

    float _originalDrag;
    float _lastSawTargetTime;

    // Compatibility with your other scripts
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

        _mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        // Visual-only safety: auto-clear highlight if lifetime expired
        if (HighlightLifetime > 0f && _highlightedRB != null && Time.time >= _highlightExpireTime)
        {
            ClearTargetVisuals();
        }
    }

    void OnDisable()
    {
        // Ensure visuals never stay on if this gets disabled
        ClearTargetVisuals();
    }

    bool GrabHeld()
    {
        if (_grabAction == null) return false;
        return _grabAction.ReadValue<float>() > 0.05f;
    }

    void FixedUpdate()
    {
        bool held = GrabHeld();

        // RELEASE (PHYSICS UNCHANGED)
        if (_connection != null && !held)
        {
            Destroy(_connection);
            _connection = null;

            if (otherRB != null)
                otherRB.linearDamping = _originalDrag;

            // polish: ensure visuals are cleared immediately on release
            ClearTargetVisuals();
            return;
        }

        // Sticky target memory: if we haven't seen target recently and not holding, forget it
        // polish: only forget if we're not overlapping any collider of it (avoids flicker)
        if (_connection == null && otherRB != null)
        {
            bool expired = (Time.time - _lastSawTargetTime) > TargetKeepTime;
            bool notOverlapping = _targetOverlapCount <= 0;

            if (expired && notOverlapping)
            {
                ClearTargetVisuals();
                otherRB = null;
            }
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
            _targetOverlapCount = 0;
            return;
        }

        _connection = otherRB.gameObject.AddComponent<FixedJoint>();
        _connection.connectedBody = _rb;
        _connection.enableCollision = false;
        BensCameraShake.Instance.Shake(0.12f, 0.12f); // ben
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

        // If we touched a NEW rigidbody, reset overlap count + visuals for old target
        if (otherRB != cand)
        {
            ClearTargetVisuals();
            otherRB = cand;
            _targetOverlapCount = 0;
        }

        _targetOverlapCount++;
        _lastSawTargetTime = Time.time;

        ApplyTargetVisuals(otherRB);
        RefreshHighlightTimer();
    }

    void OnTriggerStay(Collider other)
    {
        if (_connection != null) return;
        if (otherRB == null) return;
        if (other.attachedRigidbody != otherRB) return;

        // keep target "alive" while inside trigger
        _lastSawTargetTime = Time.time;

        // keep visuals alive while inside trigger
        RefreshHighlightTimer();

        // If timer cleared visuals while still overlapping, re-apply
        if (_highlightedRB == null)
            ApplyTargetVisuals(otherRB);
    }

    void OnTriggerExit(Collider other)
    {
        if (_connection != null) return;
        if (otherRB == null) return;
        if (other.attachedRigidbody != otherRB) return;

        _targetOverlapCount = Mathf.Max(0, _targetOverlapCount - 1);

        // polish: if we truly left the target (all its colliders), clear visuals immediately
        if (_targetOverlapCount == 0)
        {
            ClearTargetVisuals();
            _lastSawTargetTime = Time.time; // keep sticky time for re-enter
        }
    }

    void OnJointBreak(float breakForce)
    {
        _connection = null;
        BensCameraShake.Instance.Shake(0.18f, 0.22f);// ben

        if (otherRB != null)
            otherRB.linearDamping = _originalDrag;

        // polish: joint break should also clear visuals
        ClearTargetVisuals();
    }

    // ===================== VISUALS =====================

    void RefreshHighlightTimer()
    {
        if (HighlightLifetime <= 0f) return;
        _highlightExpireTime = Time.time + HighlightLifetime;
    }

    void ApplyTargetVisuals(Rigidbody rb)
    {
        if (rb == null) return;

        if (_highlightedRB == rb)
            return;

        ClearTargetVisuals();

        _highlightedRB = rb;
        _highlightedRenderers = rb.GetComponentsInChildren<Renderer>(true);

        if (highlightMode == HighlightMode.None)
            return;

        if (highlightMode == HighlightMode.OutlineClone && OutlineMaterial != null)
        {
            ApplyOutlineClone();
        }
        else if (highlightMode == HighlightMode.Color)
        {
            ApplyColorHighlight();
        }
    }

    void ApplyColorHighlight()
    {
        if (_highlightedRenderers == null) return;

        _mpb.Clear();
        _mpb.SetColor(BaseColorId, Highlight); // URP/HDRP
        _mpb.SetColor(ColorId, Highlight);     // Built-in

        for (int i = 0; i < _highlightedRenderers.Length; i++)
        {
            var r = _highlightedRenderers[i];
            if (r == null) continue;
            r.SetPropertyBlock(_mpb);
        }
    }

    void ApplyOutlineClone()
    {
        if (_highlightedRenderers == null) return;

        // Create child renderers that use ONLY the outline material.
        // We never modify the original materials -> reset is always guaranteed.

        for (int i = 0; i < _highlightedRenderers.Length; i++)
        {
            var r = _highlightedRenderers[i];
            if (r == null) continue;

            // MeshRenderer
            var mr = r as MeshRenderer;
            if (mr != null)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                // avoid duplicating if something already exists (extra safety)
                if (mr.transform.Find(OutlineClonePrefix + mr.name) != null) continue;

                var go = new GameObject(OutlineClonePrefix + mr.name);
                go.transform.SetParent(mr.transform, false);

                var cloneMF = go.AddComponent<MeshFilter>();
                cloneMF.sharedMesh = mf.sharedMesh;

                var cloneMR = go.AddComponent<MeshRenderer>();
                cloneMR.sharedMaterial = OutlineMaterial;
                cloneMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                cloneMR.receiveShadows = false;

                _outlineClones.Add(go);
                continue;
            }

            // SkinnedMeshRenderer
            var smr = r as SkinnedMeshRenderer;
            if (smr != null && smr.sharedMesh != null)
            {
                if (smr.transform.Find(OutlineClonePrefix + smr.name) != null) continue;

                var go = new GameObject(OutlineClonePrefix + smr.name);
                go.transform.SetParent(smr.transform, false);

                var cloneSMR = go.AddComponent<SkinnedMeshRenderer>();
                cloneSMR.sharedMesh = smr.sharedMesh;
                cloneSMR.bones = smr.bones;
                cloneSMR.rootBone = smr.rootBone;
                cloneSMR.updateWhenOffscreen = true;

                cloneSMR.sharedMaterial = OutlineMaterial;
                cloneSMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                cloneSMR.receiveShadows = false;

                _outlineClones.Add(go);
            }
        }
    }

    void ClearTargetVisuals()
    {
        // Clear property blocks
        if (_highlightedRenderers != null)
        {
            for (int i = 0; i < _highlightedRenderers.Length; i++)
            {
                var r = _highlightedRenderers[i];
                if (r == null) continue;
                r.SetPropertyBlock(null);
            }
        }

        // Destroy outline clones (guaranteed reset)
        if (_outlineClones.Count > 0)
        {
            for (int i = 0; i < _outlineClones.Count; i++)
            {
                if (_outlineClones[i] != null)
                    Destroy(_outlineClones[i]);
            }
            _outlineClones.Clear();
        }

        _highlightedRB = null;
        _highlightedRenderers = null;
    }
}
