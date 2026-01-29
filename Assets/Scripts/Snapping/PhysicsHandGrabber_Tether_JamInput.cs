using UnityEngine;
using UnityEngine.InputSystem;

public class PhysicsHandGrabber_Tether_JamInput : MonoBehaviour
{
    [Header("Player Link")]
    [SerializeField] PlayerInput playerInput;

    [Header("Anchor Rigidbody (must exist!)")]
    [Tooltip("Ragdoll hand RB OR proxy RB that follows the hand.")]
    [SerializeField] Rigidbody handAnchorRb;

    [Header("Grab Search")]
    [SerializeField] float grabRadius = 0.85f;
    [SerializeField] LayerMask grabbableMask;

    [Header("Handling")]
    [SerializeField] float handStrength = 3f;

    [Header("Tether Feel (Slop)")]
    [Tooltip("How hard the hand pulls the object toward it.")]
    [SerializeField] float positionSpring = 900f;

    [Tooltip("Damping to reduce jitter.")]
    [SerializeField] float positionDamping = 80f;

    [Tooltip("Max pull force. Lower = more draggy / easier to yank away.")]
    [SerializeField] float maxForce = 2500f;

    [Tooltip("How much the hand tries to match rotation.")]
    [SerializeField] float rotationSpring = 120f;

    [Tooltip("Max torque for rotation.")]
    [SerializeField] float maxTorque = 250f;

    [Tooltip("If true, collisions between player and held object are ignored.")]
    [SerializeField] bool ignorePlayerCollision = true;

    Grabbable held;
    Rigidbody heldRb;
    ConfigurableJoint joint;

    Collider[] playerColliders;
    Collider[] heldColliders;

    void Awake()
    {
        if (playerInput == null)
            playerInput = GetComponentInParent<PlayerInput>();

        if (handAnchorRb == null)
            handAnchorRb = GetComponent<Rigidbody>(); // if you put this on the ragdoll hand RB

        if (playerInput != null)
            playerColliders = playerInput.GetComponentsInChildren<Collider>(true);
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

        if (!target.CanBeHeld(handStrength)) return false;

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

        if (heldRb != null)
        {
            heldRb.useGravity = true; // if you use gravity in your game, keep this how you want
        }

        held = null;
        heldRb = null;
        heldColliders = null;
    }

    Grabbable FindBestGrabbable()
    {
        var hits = Physics.OverlapSphere(handAnchorRb.position, grabRadius, grabbableMask, QueryTriggerInteraction.Collide);

        Grabbable best = null;
        float bestD = float.MaxValue;

        foreach (var h in hits)
        {
            var g = h.GetComponentInParent<Grabbable>();
            if (g == null || g.Rigidbody == null) continue;

            float d = Vector3.Distance(handAnchorRb.position, g.Rigidbody.worldCenterOfMass);
            if (d < bestD)
            {
                bestD = d;
                best = g;
            }
        }
        return best;
    }

    void Grab(Grabbable g)
    {
        held = g;
        heldRb = g.Rigidbody;

        heldRb.isKinematic = false;          // keep full physics
        heldRb.useGravity = false;           // top-down (usually off). Flip if you want falling.
        heldRb.linearVelocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;

        // Create tether joint ON the held object, connected to the hand anchor RB
        joint = heldRb.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = handAnchorRb;

        joint.autoConfigureConnectedAnchor = false;

        // Where on the object we “grab” (handle if exists)
        Vector3 grabPointWorld = (g.handle != null) ? g.handle.position : heldRb.worldCenterOfMass;
        joint.anchor = heldRb.transform.InverseTransformPoint(grabPointWorld);

        // Where on the hand we grab (hand RB position)
        joint.connectedAnchor = Vector3.zero;

        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;

        var limit = new SoftJointLimit { limit = 0.05f }; // small slack = drag feel
        joint.linearLimit = limit;

        var drive = new JointDrive
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
            positionDamper = positionDamping,
            maximumForce = maxTorque
        };

        joint.enableCollision = true;

        if (ignorePlayerCollision)
        {
            heldColliders = heldRb.GetComponentsInChildren<Collider>(true);
            SetHeldCollisionIgnored(true);
        }
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
}
