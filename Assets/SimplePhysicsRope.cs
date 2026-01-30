using System.Collections.Generic;
using UnityEngine;

public class SimplePhysicsRope : MonoBehaviour
{
    [Header("Endpoints")]
    public Transform startAnchor;          // where rope starts
    public Rigidbody endBody;              // optional: attach rope end to a rigidbody (can be null)

    [Header("Rope Build")]
    public GameObject segmentPrefab;       // prefab with Rigidbody + CapsuleCollider
    public int segmentCount = 20;
    public float segmentLength = 0.25f;

    [Header("Joint Tuning (simple)")]
    [Tooltip("Higher = stiffer rope. Too high can jitter.")]
    public float positionSpring = 8000f;
    public float positionDamper = 80f;

    [Tooltip("Enable to reduce self-collision jitter by ignoring collisions between neighboring segments.")]
    public bool ignoreNeighborCollisions = true;

    [Header("Visual")]
    public LineRenderer line;

    readonly List<Rigidbody> _bodies = new();

    void Reset()
    {
        line = GetComponent<LineRenderer>();
    }

    void Start()
    {
        BuildRope();
    }

    void LateUpdate()
    {
        if (line == null || _bodies.Count == 0) return;

        line.positionCount = _bodies.Count + 1;
        line.SetPosition(0, startAnchor.position);

        for (int i = 0; i < _bodies.Count; i++)
            line.SetPosition(i + 1, _bodies[i].position);
    }

    [ContextMenu("Rebuild Rope")]
    public void BuildRope()
    {
        // Cleanup old
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        _bodies.Clear();

        if (startAnchor == null || segmentPrefab == null)
        {
            Debug.LogError("Missing startAnchor or segmentPrefab.");
            return;
        }

        Vector3 dir = startAnchor.forward; // spawn direction
        Vector3 pos = startAnchor.position;

        Rigidbody prevBody = null;

        for (int i = 0; i < segmentCount; i++)
        {
            pos += dir * segmentLength;

            var seg = Instantiate(segmentPrefab, pos, Quaternion.LookRotation(dir), transform);
            seg.name = $"RopeSeg_{i:00}";

            var rb = seg.GetComponent<Rigidbody>();
            if (rb == null) rb = seg.AddComponent<Rigidbody>();

            // Create joint and connect
            var joint = seg.GetComponent<ConfigurableJoint>();
            if (joint == null) joint = seg.AddComponent<ConfigurableJoint>();

            joint.autoConfigureConnectedAnchor = false;
            joint.enableCollision = false; // usually better OFF for stability

            // Connect first segment to startAnchor, others to previous segment
            if (i == 0)
            {
                joint.connectedBody = null;
                joint.connectedAnchor = startAnchor.position;

                // anchor on this segment: "top" of capsule
                joint.anchor = new Vector3(0, 0, -segmentLength * 0.5f);
                joint.connectedAnchor = startAnchor.position;
            }
            else
            {
                joint.connectedBody = prevBody;
                joint.anchor = new Vector3(0, 0, -segmentLength * 0.5f);
                joint.connectedAnchor = new Vector3(0, 0, segmentLength * 0.5f);
            }

            // Lock distance-ish using joint drives (simple & stable enough)
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            // Allow rotation like a rope
            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;

            var drive = new JointDrive
            {
                positionSpring = positionSpring,
                positionDamper = positionDamper,
                maximumForce = Mathf.Infinity
            };

            // Not used when x/y/z are locked, but keeps settings obvious if you experiment
            joint.xDrive = drive;
            joint.yDrive = drive;
            joint.zDrive = drive;

            _bodies.Add(rb);

            // Ignore collisions between neighbors (big stability boost)
            if (ignoreNeighborCollisions && i > 0)
            {
                var cA = seg.GetComponent<Collider>();
                var cB = prevBody.GetComponent<Collider>();
                if (cA && cB) Physics.IgnoreCollision(cA, cB, true);
            }

            prevBody = rb;
        }

        // Attach end to a rigidbody (optional)
        if (endBody != null && _bodies.Count > 0)
        {
            var last = _bodies[^1].gameObject;
            var endJoint = last.GetComponent<ConfigurableJoint>();

            endJoint.connectedBody = endBody;
            endJoint.connectedAnchor = Vector3.zero; // attach to endBody center
            endJoint.anchor = new Vector3(0, 0, segmentLength * 0.5f);
        }
    }
}
