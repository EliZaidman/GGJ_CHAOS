using UnityEngine;

[DefaultExecutionOrder(20000)] // run after Animator
public class SimpleTwoBoneIK : MonoBehaviour
{
    [Header("Bones")]
    public Transform upper;   // mixamorig:RightArm
    public Transform lower;   // mixamorig:RightForeArm
    public Transform hand;    // mixamorig:RightHand

    [Header("Targets")]
    public Transform target;  // handle / target point
    public Transform hint;    // elbow hint (optional)

    [Header("Enabled")]
    public bool active = false;

    [Header("Tuning")]
    public bool matchHandRotation = true;
    public float maxReachPadding = 0.001f;

    void LateUpdate()
    {
        if (!active) return;
        if (!upper || !lower || !hand || !target) return;

        SolveTwoBoneInstant(
            upper, lower, hand,
            target.position,
            hint ? hint.position : (Vector3?)null,
            matchHandRotation ? target.rotation : (Quaternion?)null,
            maxReachPadding
        );
    }

    static void SolveTwoBoneInstant(
        Transform upper, Transform lower, Transform hand,
        Vector3 targetPos,
        Vector3? hintPos,
        Quaternion? desiredHandRot,
        float reachPadding)
    {
        Vector3 rootPos = upper.position;
        Vector3 jointPos = lower.position;
        Vector3 endPos = hand.position;

        float lenUpper = Vector3.Distance(rootPos, jointPos);
        float lenLower = Vector3.Distance(jointPos, endPos);

        Vector3 toTarget = targetPos - rootPos;
        float dist = toTarget.magnitude;

        float maxReach = Mathf.Max(0.0001f, lenUpper + lenLower - reachPadding);
        float minReach = Mathf.Max(0.0001f, Mathf.Abs(lenUpper - lenLower) + reachPadding);
        dist = Mathf.Clamp(dist, minReach, maxReach);

        Vector3 dir = toTarget.sqrMagnitude > 0.000001f ? toTarget.normalized : upper.forward;

        // bend plane
        Vector3 bendNormal;
        if (hintPos.HasValue)
        {
            Vector3 toHint = hintPos.Value - rootPos;
            bendNormal = Vector3.Cross(dir, toHint);
            if (bendNormal.sqrMagnitude < 0.000001f)
                bendNormal = Vector3.up;
            else
                bendNormal.Normalize();
        }
        else
        {
            bendNormal = Vector3.up;
        }

        Vector3 bendDir = Vector3.Cross(bendNormal, dir).normalized;

        // law of cosines
        float cosAngle = (lenUpper * lenUpper + dist * dist - lenLower * lenLower) / (2f * lenUpper * dist);
        cosAngle = Mathf.Clamp(cosAngle, -1f, 1f);
        float angleUpper = Mathf.Acos(cosAngle);

        float proj = Mathf.Cos(angleUpper) * lenUpper;
        float height = Mathf.Sin(angleUpper) * lenUpper;

        Vector3 desiredElbow = rootPos + dir * proj + bendDir * height;

        // INSTANT rotations (no blending)
        Quaternion upperRot = Quaternion.LookRotation(desiredElbow - rootPos, bendNormal);
        upper.rotation = upperRot;

        Quaternion lowerRot = Quaternion.LookRotation(targetPos - desiredElbow, bendNormal);
        lower.rotation = lowerRot;

        if (desiredHandRot.HasValue)
            hand.rotation = desiredHandRot.Value;
    }
}
