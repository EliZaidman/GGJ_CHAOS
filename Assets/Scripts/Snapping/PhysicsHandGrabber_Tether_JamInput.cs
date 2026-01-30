using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PhysicsHandGrabber_Tether_JamInput : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private CarryMotorState carryState;

    [Header("Palm Anchors (Transforms in the palm)")]
    [SerializeField] private Transform rightPalmAnchor; 
    [SerializeField] private Transform leftPalmAnchor;

    [Header("Hand Anchor RBs (kinematic)")]
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

    [Header("Reach then Snap")]
    [Tooltip("How long the arm reaches toward the object before the object snaps into the palm.")]
    [SerializeField] private float reachTime = 0.12f;

    [Tooltip("If true, match rotation when snapping into the palm.")]
    [SerializeField] private bool snapRotation = true;

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

    private Coroutine grabRoutine;

    void Awake()
    {
        if (playerInput == null)
            playerInput = GetComponentInParent<PlayerInput>();

        if (carryState == null)
            carryState = GetComponent<CarryMotorState>();

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
            else StartGrab();
        }
    }

    void StartGrab()
    {
        var g = FindNearestGrabbable();
        if (g == null || g.Rigidbody == null) return;

        if (grabRoutine != null) StopCoroutine(grabRoutine);

        held = g;
        heldRb = g.Rigidbody;

        heldRb.isKinematic = false;

        SetIKTargetsForReach(g);
        SetIKActive(true);

        grabRoutine = StartCoroutine(ReachThenSnap(g));
    }

    IEnumerator ReachThenSnap(Grabbable g)
    {
        float t = 0f;
        while (t < reachTime)
        {
            t += Time.deltaTime;
            if (held == null || heldRb == null || g == null) yield break;

            SetIKTargetsForReach(g);
            yield return null;
        }

        if (held == null || heldRb == null || g == null) yield break;

        SnapObjectIntoPalm(g);

        if (carryState != null)
            carryState.ApplyMass(heldRb.mass);

        if (ignorePlayerCollisionWhileHeld && g.ignorePlayerCollisionWhileHeld)
        {
            heldColliders = heldRb.GetComponentsInChildren<Collider>(true);
            SetHeldCollisionIgnored(true);
        }

        if (rightHandRb != null && g.rightHandle != null)
            rightJoint = CreateLockedJoint(heldRb, rightHandRb, g.rightHandle);

        if (leftHandRb != null && g.leftHandle != null)
            leftJoint = CreateLockedJoint(heldRb, leftHandRb, g.leftHandle);

        if (rightProxy != null) rightProxy.ClearOverride();
        if (leftProxy != null) leftProxy.ClearOverride();
    }

    void Drop()
    {
        if (grabRoutine != null) StopCoroutine(grabRoutine);
        grabRoutine = null;

        if (rightJoint) Destroy(rightJoint);
        if (leftJoint) Destroy(leftJoint);
        rightJoint = null;
        leftJoint = null;

        if (rightProxy != null) rightProxy.ClearOverride();
        if (leftProxy != null) leftProxy.ClearOverride();

        SetIKActive(false);

        if (heldColliders != null) SetHeldCollisionIgnored(false);
        heldColliders = null;

        if (carryState != null)
            carryState.Clear();

        held = null;
        heldRb = null;
    }

    void SetIKTargetsForReach(Grabbable g)
    {
        if (rightIK != null)
            rightIK.target = g.rightHandle;

        if (leftIK != null)
            leftIK.target = g.leftHandle;
    }

    void SnapObjectIntoPalm(Grabbable g)
    {
        if (rightPalmAnchor == null || g.rightHandle == null) return;

        if (g.disableGravityWhileHeld) heldRb.useGravity = false;

        Vector3 deltaPos = rightPalmAnchor.position - g.rightHandle.position;
        heldRb.position += deltaPos;

        if (snapRotation)
        {
            Quaternion rotDelta = rightPalmAnchor.rotation * Quaternion.Inverse(g.rightHandle.rotation);
            heldRb.rotation = rotDelta * heldRb.rotation;
        }

        heldRb.linearVelocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;
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
        Vector3 origin =
            rightPalmAnchor ? rightPalmAnchor.position :
            (rightHandRb ? rightHandRb.position : transform.position);

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
        if (rightIK != null) rightIK.active = false;
        if (leftIK != null) leftIK.active = false;
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
        Vector3 origin =
            rightPalmAnchor ? rightPalmAnchor.position :
            (rightHandRb ? rightHandRb.position : transform.position);

        Gizmos.DrawWireSphere(origin, grabRadius);
    }
#endif
}
