using UnityEngine;

public class SnapPoint : MonoBehaviour
{
    [Header("What can snap here")]
    public string acceptTag = "Snappable";

    [Header("How close to snap")]
    public float snapRadius = 0.6f;

    [Header("Keep only one object snapped")]
    public bool singleOccupant = true;

    [HideInInspector] public Rigidbody occupant;

    public bool CanAccept(Rigidbody rb)
    {
        if (rb == null) return false;
        if (!string.IsNullOrEmpty(acceptTag) && !rb.CompareTag(acceptTag)) return false;
        if (singleOccupant && occupant != null && occupant != rb) return false;
        return true;
    }
}
