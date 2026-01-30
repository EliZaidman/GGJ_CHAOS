using UnityEngine;
using UnityEngine.InputSystem;

public class PhysicsHandGrabber_Tether_JamInput : MonoBehaviour
{
    [Header("Player Link")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Hand Anchor Rigidbody (this object is best)")]
    [SerializeField] private Rigidbody handAnchorRb;

    [Header("Hand Strength")]
    [SerializeField] private float handStrength = 3f;

    [Header("Grab Search")]
    [SerializeField] private float grabRadius = 0.85f;
    [SerializeField] private LayerMask grabbableMask;

    [Header("Tether Feel (Slop)")]
    [SerializeField] private float positionSpring = 900f;
    [SerializeField] private float positionDamping = 80f;
    [SerializeField] private float maxForce = 2500f;

    [SerializeField] private float rotationSpring = 120f;
    [SerializeField] private float rotationDamping = 60f;
    [SerializeField] private float maxTorque = 250f;

    [Tooltip("Small slack = drag feel. Bigger slack = more leash.")]
    [SerializeField] private float linearSlack = 0.06f;

    [Tooltip("Ignore collisions between player colliders and held object.")]
    [SerializeField] private bool ignorePlayerCollision = true;

    [Header("Carry Mode Multipliers")]
    [SerializeField] private float lightSpeedMult = 0.90f;
    [SerializeField] private float mediumSpeedMult = 0.65f;
    [SerializeField] private float heavySpeedMult = 0.35f;

    private Grabbable held;
    private Rigidbody heldRb;
    private ConfigurableJoint joint;

    private CarryMotorState carryStateOnPlayer;
    private Collider[] playerColliders;
    private Collider[] heldColliders;

    void Awake()
    {
        if (playerInput == null)
            playerInput = GetComponentInParent<PlayerInput>();

        if (handAnchorRb == null)
            handAnchorRb = GetComponent<Rigidbody>();

        if (playerInput != null)
        {
            carryStateOnPlayer = playerInput.GetComponent<CarryMotorState>();
            playerColliders = playerInput.GetComponentsInChildren<Collider>(true);
        }
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

    public bool TryGrab()
    {
        if (held != null) return false;
        if (handAnchorRb == null) return false;

        Grabbable target = FindBestGrabbable();
        if (target == null) return false;

        if (!target.CanBeHeld(handStrength))
            return false;

        Grab(target);
        return true;
    }

    public void Drop()
    {
        if (held == null) return;

        if (joint != null) Destroy(joint);
        joint = null;

        if (ignorePlayerCollision)
            SetHeldCollisionIgnored(false);

        if (carryStateOnPlayer != null)
            carryStateOnPlayer.Clear();

        held = null;
        heldRb = null;
        heldColliders = null;
    }

    private Grabbable FindBestGrabbable()
    {
        var hits = Physics.OverlapSphere(handAnchorRb.position, grabRadius, grabbableMask, QueryTriggerInteraction.Collide);

        Grabbable best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            var g = h.GetComponentInParent<Grabbable>();
            if (g == null || g.Rigidbody == null) continue;

            float d = Vector3.Distance(handAnchorRb.position,
                (g.handle != null) ? g.handle.position : g.Rigidbody.worldCenterOfMass);

            if (d < bestDist)
            {
                bestDist = d;
                best = g;
            }
        }

        return best;
    }

    private void Grab(Grabbable g)
    {
        held = g;
        heldRb = g.Rigidbody;

        heldRb.isKinematic = false;
        if (g.disableGravityWhileHeld) heldRb.useGravity = false;

        heldRb.linearVelocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;

        ApplyCarryEffects(g);

        joint = heldRb.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = handAnchorRb;
        joint.autoConfigureConnectedAnchor = false;

        Vector3 grabWorld = (g.handle != null) ? g.handle.position : heldRb.worldCenterOfMass;
        joint.anchor = heldRb.transform.InverseTransformPoint(grabWorld);

        joint.connectedAnchor = Vector3.zero;

        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;

        joint.linearLimit = new SoftJointLimit { limit = linearSlack };

        JointDrive drive = new JointDrive
        {
            positionSpring = positionSpring,
            positionDamper = positionDamping,
            maximumForce = maxForce
        };
        joint.xDrive = drive;
        joint.yDrive = drive;
        joint.zDrive = drive;

        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.slerpDrive = new JointDrive
        {
            positionSpring = rotationSpring,
            positionDamper = rotationDamping,
            maximumForce = maxTorque
        };

        joint.enableCollision = true;

        if (ignorePlayerCollision)
        {
            heldColliders = heldRb.GetComponentsInChildren<Collider>(true);
            SetHeldCollisionIgnored(true);
        }
    }

    private void ApplyCarryEffects(Grabbable g)
    {
        if (carryStateOnPlayer == null) return;

        // SuperHeavy: stuck (locked)
        // Heavy: drag slow
        // Medium: carry slower
        // Light: minimal change
        switch (g.carryMode)
        {
            case Grabbable.CarryMode.SuperHeavy:
                carryStateOnPlayer.Apply(0f, true);
                break;

            case Grabbable.CarryMode.Heavy:
                carryStateOnPlayer.Apply(heavySpeedMult, false);
                break;

            case Grabbable.CarryMode.Medium:
                carryStateOnPlayer.Apply(mediumSpeedMult, false);
                break;

            default:
                carryStateOnPlayer.Apply(lightSpeedMult, false);
                break;
        }
    }

    private void SetHeldCollisionIgnored(bool ignored)
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
        if (handAnchorRb != null)
            Gizmos.DrawWireSphere(handAnchorRb.position, grabRadius);
        else
            Gizmos.DrawWireSphere(transform.position, grabRadius);
    }
#endif
}
