using UnityEngine;

[DisallowMultipleComponent]
public class Grabbable : MonoBehaviour
{
    [Header("Hand Snap Points")]
    [Tooltip("Where the RIGHT hand should lock onto this object.")]
    public Transform rightHandle;

    [Tooltip("Where the LEFT hand should lock onto this object (optional).")]
    public Transform leftHandle;

    [Header("Optional")]
    [Tooltip("If true, gravity is disabled on this object while held.")]
    public bool disableGravityWhileHeld = true;

    [Tooltip("If true, ignore player collision with this object while held (usually feels better).")]
    public bool ignorePlayerCollisionWhileHeld = true;

    public Rigidbody Rigidbody { get; private set; }

    void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        if (Rigidbody == null) Rigidbody = GetComponentInParent<Rigidbody>();
    }
}
