using UnityEngine;

[DisallowMultipleComponent]
public class Grabbable : MonoBehaviour
{
    public enum CarryMode { Light, Medium, Heavy, SuperHeavy }

    [Header("Carry Mode")]
    public CarryMode carryMode = CarryMode.Light;

    [Header("Handling Requirement")]
    [Tooltip("Minimum strength needed to even attempt holding this.")]
    public float requiredStrength = 1f;

    [Tooltip("If > 0, this weight is used instead of Rigidbody.mass.")]
    public float overrideWeight = 0f;

    [Header("Optional Handle")]
    [Tooltip("If set, grab happens from this point on the object.")]
    public Transform handle;

    [Header("While Held")]
    public bool disableGravityWhileHeld = true;

    public Rigidbody Rigidbody { get; private set; }

    void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        if (Rigidbody == null) Rigidbody = GetComponentInParent<Rigidbody>();
    }

    public float Weight
    {
        get
        {
            if (overrideWeight > 0f) return overrideWeight;
            if (Rigidbody != null) return Rigidbody.mass;
            return 1f;
        }
    }

public bool CanBeHeld(float handStrength)
{
    return handStrength >= requiredStrength;
}
}
