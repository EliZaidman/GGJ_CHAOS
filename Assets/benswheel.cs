using UnityEngine;

public class BensWheelUnlock : MonoBehaviour
{
    [SerializeField] bensVault bensVault;
    [SerializeField] HingeJoint hinge;

    [Header("Unlock Condition")]
    [SerializeField] bool unlockedWheel = false;
    [SerializeField] int requiredRightSpins = 2;

    [Header("Debug")]
    [SerializeField] bool spamDebug = true; // turn off later
    [SerializeField] float accumulatedRightRotation = 0f;

    float lastAngle;

    void Reset()
    {
        hinge = GetComponent<HingeJoint>();
    }

    void Awake()
    {
        if (hinge == null) hinge = GetComponent<HingeJoint>();
    }

    void Start()
    {
        lastAngle = hinge.angle;
        Debug.Log("Wheel tracker started. Hinge: " + (hinge ? hinge.name : "NULL"));
    }

    void Update()
    {
        if (unlockedWheel) return;

        float currentAngle = hinge.angle;

        // hinge.angle is typically -180..180-ish, so we still use DeltaAngle to be safe
        float delta = Mathf.DeltaAngle(lastAngle, currentAngle);

        // Debug every few frames to reduce spam
        if (spamDebug && Time.frameCount % 10 == 0)
        {
            Debug.Log($"HingeAngle: {currentAngle:F2} | Delta: {delta:F2} | Accum: {accumulatedRightRotation:F2}");
        }

        // Decide which sign is "right"
        // Your log earlier showed positive deltas while turning, so try POSITIVE = right first.
        if (delta > 0f)
        {
            accumulatedRightRotation += Mathf.Abs(delta);
        }
        else if (delta < 0f)
        {
            // optional: reset if turning opposite direction
            accumulatedRightRotation = 0f;
        }

        if (accumulatedRightRotation >= requiredRightSpins * 360f)
        {
            Debug.Log("UNLOCK CONDITION MET -> bensVault.unlocked = true");
            bensVault.unlocked = true;
            unlockedWheel = true;
        }

        lastAngle = currentAngle;
    }
}