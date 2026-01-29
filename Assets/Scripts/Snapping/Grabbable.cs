using UnityEngine;

[DisallowMultipleComponent]
public class Grabbable : MonoBehaviour
{
    [Header("Handling Requirement")]
    [Tooltip("How much 'strength' is required to grab this object.")]
    public float requiredStrength = 1f;

    [Tooltip("If > 0, use this instead of Rigidbody.mass for handling checks.")]
    public float overrideWeight = 0f;

    [Header("Optional Handle")]
    [Tooltip("If set, the hand snaps to this point/rotation when grabbed.")]
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
        // Simple rule: you can hold it if you have enough strength.
        return handStrength >= requiredStrength && handStrength >= Weight;
    }
}
