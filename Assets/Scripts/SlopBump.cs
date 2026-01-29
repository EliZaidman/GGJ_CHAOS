using UnityEngine;

public class SlopBump : MonoBehaviour
{
    [SerializeField] float bumpStrength = 6f;
    Rigidbody rb;

    void Awake() => rb = GetComponent<Rigidbody>();

    void OnCollisionEnter(Collision c)
    {
        if (!c.rigidbody) return;

        // push away from contact point
        Vector3 dir = (rb.worldCenterOfMass - c.GetContact(0).point);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        rb.AddForce(dir * bumpStrength, ForceMode.VelocityChange);
    }
}
