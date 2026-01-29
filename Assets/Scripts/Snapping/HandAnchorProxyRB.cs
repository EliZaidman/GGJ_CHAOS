using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HandAnchorProxyRB : MonoBehaviour
{
    [SerializeField] Transform followTarget; // set to RightHand_Anchor
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        if (!followTarget) return;
        rb.MovePosition(followTarget.position);
        rb.MoveRotation(followTarget.rotation);
    }
}
