using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HandAnchorProxyRB : MonoBehaviour
{
    [Header("Normal follow target (bone child anchor)")]
    [SerializeField] private Transform followBone; // e.g. RightHand_Anchor

    private Transform overrideTarget;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Safety: if not assigned, try parent
        if (followBone == null && transform.parent != null)
            followBone = transform.parent;
    }

    public void SetOverride(Transform t) => overrideTarget = t;
    public void ClearOverride() => overrideTarget = null;

    void FixedUpdate()
    {
        Transform t = overrideTarget != null ? overrideTarget : followBone;

        // Critical: if we have no target, DO NOTHING (never go to zero)
        if (t == null) return;

        rb.MovePosition(t.position);
        rb.MoveRotation(t.rotation);
    }
}
