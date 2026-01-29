using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SnapToPoint : MonoBehaviour
{
    [Header("Snapping")]
    public float searchRadius = 1.0f;
    public LayerMask snapPointMask;
    public bool snapRotation = true;

    [Tooltip("If true, makes it kinematic while snapped (very stable).")]
    public bool kinematicWhileSnapped = true;

    [Tooltip("If false, uses a FixedJoint (keeps physics feel).")]
    public bool useKinematicSnap = true;

    [Header("Optional: press to snap/unsnap")]
    public KeyCode debugSnapKey = KeyCode.E;

    Rigidbody rb;
    SnapPoint snappedPoint;
    FixedJoint joint;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // You’ll replace this with your controller input later.
        if (Input.GetKeyDown(debugSnapKey))
        {
            if (snappedPoint != null) Unsnap();
            else TrySnap();
        }
    }

    public bool TrySnap()
    {
        SnapPoint best = FindBestSnapPoint();
        if (best == null) return false;
        if (!best.CanAccept(rb)) return false;

        Snap(best);
        return true;
    }

    SnapPoint FindBestSnapPoint()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, snapPointMask, QueryTriggerInteraction.Collide);

        SnapPoint best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            SnapPoint sp = hits[i].GetComponentInParent<SnapPoint>();
            if (sp == null) continue;

            float d = Vector3.Distance(transform.position, sp.transform.position);
            if (d > sp.snapRadius) continue;
            if (!sp.CanAccept(rb)) continue;

            if (d < bestDist)
            {
                bestDist = d;
                best = sp;
            }
        }
        return best;
    }

    void Snap(SnapPoint point)
    {
        snappedPoint = point;
        point.occupant = rb;

        // Stop chaotic motion so the snap feels clean
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (useKinematicSnap)
        {
            if (kinematicWhileSnapped) rb.isKinematic = true;

            transform.position = point.transform.position;
            if (snapRotation) transform.rotation = point.transform.rotation;
        }
        else
        {
            // Physics-feel snap: attach with joint
            joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = point.occupant; // usually null; we just anchor by freezing transforms below
            joint.breakForce = Mathf.Infinity;
            joint.breakTorque = Mathf.Infinity;

            // Anchor by teleporting first
            transform.position = point.transform.position;
            if (snapRotation) transform.rotation = point.transform.rotation;

            // If the snap point has no rigidbody, the joint won't anchor.
            // So for joint mode, put a kinematic Rigidbody on the SnapPoint object.
        }
    }

    public void Unsnap()
    {
        if (snappedPoint != null && snappedPoint.occupant == rb)
            snappedPoint.occupant = null;

        snappedPoint = null;

        if (joint != null) Destroy(joint);
        joint = null;

        rb.isKinematic = false;
    }
}
