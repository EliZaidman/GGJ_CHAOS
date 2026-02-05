
using System.Collections.Generic;
using UnityEngine;

public class DynamicCameraHeight : MonoBehaviour
{
    [Header("Targets (players)")]
    public List<Transform> targets = new List<Transform>(4);

    [Header("Height Range")]
    public float minY = 8f;
    public float maxY = 20f;

    [Header("Spread -> Height")]
    [Tooltip("If players are within this spread (meters), camera stays at minY.")]
    public float spreadStart = 10f;

    [Tooltip("At this spread (meters) camera reaches maxY.")]
    public float spreadEnd = 30f;

    [Header("Smoothing")]
    public float smoothSpeedUp = 6f;
    public float smoothSpeedDown = 3f;

    [Header("Options")]
    [Tooltip("Use the farthest pair distance instead of bounds size. Slightly heavier but more accurate.")]
    public bool useFarthestPair = false;

    public bool doneSetup;
    void LateUpdate()
    {
        if (!doneSetup || targets == null || targets.Count == 0) return;

        // Count valid targets (so spread works even if list has nulls)
        int validCount = 0;
        for (int i = 0; i < targets.Count; i++)
            if (targets[i]) validCount++;

        if (validCount <= 1)
        {
            // With 0/1 player just ease toward min height
            float currentY0 = transform.position.y;
            float newY0 = Mathf.Lerp(currentY0, minY, Time.deltaTime * smoothSpeedDown);
            var p0 = transform.position;
            p0.y = newY0;
            transform.position = p0;
            return;
        }

        // 1) Compute spread on XZ plane
        float spread = useFarthestPair ? GetFarthestPairXZ(targets) : GetBoundsXZ(targets);

        // 2) Convert spread to 0..1
        float t = Mathf.Clamp01(Mathf.InverseLerp(spreadStart, spreadEnd, spread));

        // 3) Target height
        float targetY = Mathf.Lerp(minY, maxY, t);

        // 4) Smooth Y (faster up, slower down)
        float currentY = transform.position.y;
        float speed = targetY > currentY ? smoothSpeedUp : smoothSpeedDown;
        float newY = Mathf.Lerp(currentY, targetY, Time.deltaTime * speed);

        // 5) Apply only Y (XZ stays 100% owned by the constraint)
        Vector3 pos = transform.position;
        pos.y = newY;
        transform.position = pos;
    }


    float GetBoundsXZ(List<Transform> list)
    {
        bool hasAny = false;
        float minX = 0, maxX = 0, minZ = 0, maxZ = 0;

        for (int i = 0; i < list.Count; i++)
        {
            var tr = list[i];
            if (!tr) continue;

            Vector3 p = tr.position;
            if (!hasAny)
            {
                minX = maxX = p.x;
                minZ = maxZ = p.z;
                hasAny = true;
            }
            else
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.z < minZ) minZ = p.z;
                if (p.z > maxZ) maxZ = p.z;
            }
        }

        if (!hasAny) return 0f;

        float sizeX = maxX - minX;
        float sizeZ = maxZ - minZ;
        return Mathf.Max(sizeX, sizeZ);
    }

    float GetFarthestPairXZ(List<Transform> list)
    {
        float maxDist = 0f;

        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (!a) continue;

            Vector3 pa = a.position;

            for (int j = i + 1; j < list.Count; j++)
            {
                var b = list[j];
                if (!b) continue;

                Vector3 pb = b.position;
                float dx = pa.x - pb.x;
                float dz = pa.z - pb.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);

                if (d > maxDist) maxDist = d;
            }
        }

        return maxDist;
    }

    // Optional helper if you spawn players dynamically:
    public void SetTargets(params Transform[] newTargets)
    {
        targets.Clear();
        if (newTargets == null) return;
        for (int i = 0; i < newTargets.Length; i++)
            if (newTargets[i]) targets.Add(newTargets[i]);
    }
}
