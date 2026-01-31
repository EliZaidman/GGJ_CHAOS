using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class ForgetPlayers : MonoBehaviour
{
    [Header("Remove conditions (both must pass)")]
    [Tooltip("Absolute distance in world units required to remove a source.")]
    public float worldDistanceThreshold = 25f;

    [Tooltip("Normalized distance (0..1) required to remove a source. 0.9 means 'near the farthest one'.")]
    [Range(0f, 1f)] public float normalizedThreshold = 0.9f;

    [Header("How often to check")]
    [Tooltip("Seconds between scans. Keeps it safe and avoids list churn every frame.")]
    public float checkInterval = 5f;

    [Tooltip("If true, ignores null / missing transforms and removes them too.")]
    public bool removeNullSources = true;

    PositionConstraint positionConstraint;
    float _nextCheckTime;

    void Awake()
    {
        positionConstraint = GetComponent<PositionConstraint>();
    }

    void Update()
    {
        if (!positionConstraint) return;
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + checkInterval;

        PruneFarSources(positionConstraint);
    }

    void PruneFarSources(PositionConstraint pc)
    {
        int count = pc.sourceCount;
        if (count <= 1) return;

        // Pull current sources
        var sources = new List<ConstraintSource>(count);
        pc.GetSources(sources);

        // Collect positions and compute centroid (skip nulls if configured)
        Vector3 centroid = Vector3.zero;
        int valid = 0;

        for (int i = 0; i < sources.Count; i++)
        {
            Transform t = sources[i].sourceTransform;
            if (!t)
            {
                if (removeNullSources) { /* handled later by removal */ }
                continue;
            }

            centroid += t.position;
            valid++;
        }

        if (valid <= 1)
        {
            // If only 0-1 valid transforms remain, just strip nulls if asked
            if (removeNullSources)
            {
                bool changed = false;
                for (int i = sources.Count - 1; i >= 0; i--)
                {
                    if (!sources[i].sourceTransform)
                    {
                        sources.RemoveAt(i);
                        changed = true;
                    }
                }
                if (changed) pc.SetSources(sources);
            }
            return;
        }

        centroid /= valid;

        // Compute distances to centroid and find max distance (for normalization)
        float maxDist = 0f;
        var dists = new float[sources.Count];

        for (int i = 0; i < sources.Count; i++)
        {
            Transform t = sources[i].sourceTransform;
            if (!t)
            {
                dists[i] = -1f; // mark null
                continue;
            }

            float d = Vector3.Distance(t.position, centroid);
            dists[i] = d;
            if (d > maxDist) maxDist = d;
        }

        // Avoid divide by zero if everyone is in same spot
        if (maxDist <= 0.0001f) return;

        // Remove from end to start so indices stay valid
        bool anyRemoved = false;
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            Transform t = sources[i].sourceTransform;

            if (!t)
            {
                if (removeNullSources)
                {
                    sources.RemoveAt(i);
                    anyRemoved = true;
                }
                continue;
            }

            float worldDist = dists[i];
            float norm = worldDist / maxDist; // 0..1

            // "Very far" = absolute + relative-to-group both pass
            if (worldDist >= worldDistanceThreshold && norm >= normalizedThreshold)
            {
                sources.RemoveAt(i);
                anyRemoved = true;
            }
        }

        if (anyRemoved)
        {
            pc.SetSources(sources);
        }
    }
}
