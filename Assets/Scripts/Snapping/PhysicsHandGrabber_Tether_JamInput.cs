using UnityEngine;
using UnityEngine.InputSystem;

public class PhysicsHandGrabber_Tether_JamInput : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Hand Anchors (RBs + Proxy scripts)")]
    [SerializeField] private Rigidbody rightHandRb;
    [SerializeField] private Rigidbody leftHandRb;
    [SerializeField] private HandAnchorProxyRB rightProxy;
    [SerializeField] private HandAnchorProxyRB leftProxy;

    [Header("Fake IK (no packages, instant)")]
    [SerializeField] private SimpleTwoBoneIK rightIK;
    [SerializeField] private SimpleTwoBoneIK leftIK;

    [Header("Grab Search")]
    [SerializeField] private float grabRadius = 0.9f;
    [SerializeField] private LayerMask grabbableMask;

    [Header("Hard Lock (still physics)")]
    [SerializeField] private bool lockLinear = true;
    [SerializeField] private bool lockAngular = true;

    [Header("Collision")]
    [SerializeField] private bool ignorePlayerCollisionWhileHeld = true;

    private Grabbable held;
    private Rigidbody heldRb;

    private ConfigurableJoint rightJoint;
    private ConfigurableJoint leftJoint;

    private Collider[] playerColliders;
    private Collider[] heldColliders;

    void Awake()
    {
        if (playerInput == null)
            playerInput = GetComponentInParent<PlayerInput>();

        if (playerInput != null)
            playerColliders = playerInput.transform.root.GetComponentsInChildren<Collider>(true);

        SetIKActive(false);
    }

    void Update()
    {
        if (playerInput == null || !playerInput.user.valid) return;

        var input = JamInput.Get(playerInput.playerIndex);
        if (input == null) return;

        if (input.InteractDown)
        {
            if (held != null) Drop();
            else TryGrab();
        }
    }

    void TryGrab()
    {
        var g = FindNearestGrabbable();
        if (g == null || g.Rigidbody == null) return;

        held = g;
        heldRb = g.Rigidbody;

        // Keep full physics
        heldRb.isKinematic = false;
        if (g.disableGravityWhileHeld) heldRb.useGravity = false;

        // Override proxies so HAND goes to OBJECT handles (no object lerp to hand)
        if (rightProxy != null && g.rightHandle != null) rightProxy.SetOverride(g.rightHandle);
        if (leftProxy != null && g.leftHandle != null) leftProxy.SetOverride(g.leftHandle);

        // Instant IK to make arms reach straight (no blend)
        if (rightIK != null && g.rightHandle != null)
        {
            rightIK.target = g.rightHandle;
            rightIK.active = true;
        }
        if (leftIK != null && g.leftHandle != null)
        {
            leftIK.target = g.leftHandle;
            leftIK.active = true;
        }

        // Locked joints (tight)
        if (rightHandRb != null && g.rightHandle != null)
            rightJoint = CreateLockedJoint(heldRb, rightHandRb, g.rightHandle);

        if (leftHandRb != null && g.leftHandle != null)
            leftJoint = CreateLockedJoint(heldRb, leftHandRb, g.leftHandle);

        // Optional ignore collisions
        if (ignorePlayerCollisionWhileHeld && g.ignorePlayerCollisionWhileHeld)
        {
            heldColliders = heldRb.GetComponentsInChildren<Collider>(true);
            SetHeldCollisionIgnored(true);
        }
    }

    void Drop()
    {
        if (rightJoint) Destroy(rightJoint);
        if (leftJoint) Destroy(leftJoint);
        rightJoint = null;
        leftJoint = null;

        if (rightProxy != null) rightProxy.ClearOverride();
        if (leftProxy != null) leftProxy.ClearOverride();

        SetIKActive(false);

        if (heldColliders != null) SetHeldCollisionIgnored(false);
        heldColliders = null;

        held = null;
        heldRb = null;
    }

    ConfigurableJoint CreateLockedJoint(Rigidbody objectRb, Rigidbody handRb, Transform handle)
    {
        var j = objectRb.gameObject.AddComponent<ConfigurableJoint>();
        j.connectedBody = handRb;
        j.autoConfigureConnectedAnchor = false;

        j.anchor = objectRb.transform.InverseTransformPoint(handle.position);
        j.connectedAnchor = Vector3.zero;

        if (lockLinear)
        {
            j.xMotion = ConfigurableJointMotion.Locked;
            j.yMotion = ConfigurableJointMotion.Locked;
            j.zMotion = ConfigurableJointMotion.Locked;
        }
        else
        {
            j.xMotion = ConfigurableJointMotion.Limited;
            j.yMotion = ConfigurableJointMotion.Limited;
            j.zMotion = ConfigurableJointMotion.Limited;
            j.linearLimit = new SoftJointLimit { limit = 0.02f };
        }

        if (lockAngular)
        {
            j.angularXMotion = ConfigurableJointMotion.Locked;
            j.angularYMotion = ConfigurableJointMotion.Locked;
            j.angularZMotion = ConfigurableJointMotion.Locked;
        }
        else
        {
            j.angularXMotion = ConfigurableJointMotion.Free;
            j.angularYMotion = ConfigurableJointMotion.Free;
            j.angularZMotion = ConfigurableJointMotion.Free;
        }

        j.enableCollision = true;
        return j;
    }

    Grabbable FindNearestGrabbable()
    {
        Vector3 origin = rightHandRb ? rightHandRb.position : transform.position;

        var hits = Physics.OverlapSphere(origin, grabRadius, grabbableMask, QueryTriggerInteraction.Collide);

        Grabbable best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            var g = h.GetComponentInParent<Grabbable>();
            if (g == null || g.Rigidbody == null) continue;

            float d = Vector3.Distance(origin, g.Rigidbody.worldCenterOfMass);
            if (d < bestDist) { bestDist = d; best = g; }
        }

        return best;
    }

    void SetIKActive(bool on)
    {
        if (rightIK != null) rightIK.active = on;
        if (leftIK != null) leftIK.active = on;
    }

    void SetHeldCollisionIgnored(bool ignored)
    {
        if (playerColliders == null || heldColliders == null) return;

        foreach (var pc in playerColliders)
            foreach (var hc in heldColliders)
            {
                if (pc && hc) Physics.IgnoreCollision(pc, hc, ignored);
            }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 origin = rightHandRb ? rightHandRb.position : transform.position;
        Gizmos.DrawWireSphere(origin, grabRadius);
    }
#endif
}
