using UnityEngine;
using UnityEngine.Animations;

public class DynamicCameraHeight : MonoBehaviour
{
    public PositionConstraint positionConstraint;

    [Header("Height Settings")]
    public float minHeight = 8f;
    public float maxHeight = 20f;
    public float heightPerUnit = 0.5f; // how much camera rises per player spread
    public float smoothSpeed = 5f;
    public bool setupdone;

    float currentHeight;

   public void Setup()
    {
        if (!positionConstraint)
            positionConstraint = GetComponent<PositionConstraint>();

        currentHeight = minHeight;
        setupdone = true;
        print("setupdone");
    }

    void LateUpdate()
    {
        if (!setupdone||positionConstraint.sourceCount == 0 ||   !positionConstraint.enabled) return;
        // Calculate bounds of all sources
        Bounds bounds = new Bounds(
            positionConstraint.GetSource(0).sourceTransform.position,
            Vector3.zero);

        for (int i = 1; i < positionConstraint.sourceCount; i++)
        {
            var source = positionConstraint.GetSource(i);
            bounds.Encapsulate(source.sourceTransform.position);
        }

        float spread = Mathf.Max(bounds.size.x, bounds.size.z);

        float targetHeight = minHeight + spread * heightPerUnit;
        targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);

        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * smoothSpeed);

        Vector3 pos = transform.position;
        pos.y = currentHeight;
        transform.position = pos;
    }
}