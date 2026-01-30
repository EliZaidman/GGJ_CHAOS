using UnityEngine;

[DefaultExecutionOrder(20000)] // after Animator
public class SimpleTwoBoneIK : MonoBehaviour
{
    [Header("Bones")]
    public Transform upper;
    public Transform lower;
    public Transform hand;

    [Header("Targets")]
    public Transform target;
    public Transform hint;

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

        Vector3 bendNormal;
        if (hintPos.HasValue)
        {
            Vector3 toHint = hintPos.Value - rootPos;
            bendNormal = Vector3.Cross(dir, toHint);
            if (bendNormal.sqrMagnitude < 0.000001f) bendNormal = Vector3.up;
            else bendNormal.Normalize();
        }
        else bendNormal = Vector3.up;

        Vector3 bendDir = Vector3.Cross(bendNormal, dir).normalized;

        float cosAngle = (lenUpper * lenUpper + dist * dist - lenLower * lenLower) / (2f * lenUpper * dist);
        cosAngle = Mathf.Clamp(cosAngle, -1f, 1f);
        float angleUpper = Mathf.Acos(cosAngle);

        float proj = Mathf.Cos(angleUpper) * lenUpper;
        float height = Mathf.Sin(angleUpper) * lenUpper;

        Vector3 desiredElbow = rootPos + dir * proj + bendDir * height;

        upper.rotation = Quaternion.LookRotation(desiredElbow - rootPos, bendNormal);
        lower.rotation = Quaternion.LookRotation(targetPos - desiredElbow, bendNormal);

        if (desiredHandRot.HasValue)
            hand.rotation = desiredHandRot.Value;
    }
}
