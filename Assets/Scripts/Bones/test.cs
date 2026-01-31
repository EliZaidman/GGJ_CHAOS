using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    [Header("Targets")]
    public List<Transform> targets = new List<Transform>(4);
    public string autoFindTag = "Player";
    public bool autoFindIfEmpty = true;

    [Header("Camera Offset")]
    public Vector3 offset = new Vector3(0, 15, -18);

    [Header("Zoom")]
    public float zoomPadding = 4f;
    public float minDistance = 12f;
    public float maxDistance = 30f;
    public float zoomSmooth = 6f;

    [Header("Movement")]
    public float moveSmooth = 8f;

    float currentDistance;

    void Start()
    {
        currentDistance = offset.magnitude;
    }

    void LateUpdate()
    {
        if (autoFindIfEmpty && targets.Count == 0)
            AutoFindTargets();

        CleanNullTargets();
        if (targets.Count == 0) return;

        Bounds b = GetBounds();
        Vector3 center = b.center;

        // Calculate required zoom based on player spread
        float spread = Mathf.Max(b.size.x, b.size.z);
        float targetDistance = Mathf.Clamp(spread + zoomPadding, minDistance, maxDistance);

        currentDistance = Mathf.Lerp(currentDistance, targetDistance,
            1f - Mathf.Exp(-zoomSmooth * Time.deltaTime));

        // Maintain fixed rotation
        Vector3 direction = offset.normalized;
        Vector3 desiredPos = center + direction * currentDistance;

        transform.position = Vector3.Lerp(transform.position, desiredPos,
            1f - Mathf.Exp(-moveSmooth * Time.deltaTime));

        // Look at center (stable rotation)
        transform.rotation = Quaternion.LookRotation(center - transform.position);
    }

    Bounds GetBounds()
    {
        Bounds b = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 1; i < targets.Count; i++)
            b.Encapsulate(targets[i].position);
        return b;
    }

    void AutoFindTargets()
    {
        GameObject[] gos = GameObject.FindGameObjectsWithTag(autoFindTag);
        targets.Clear();
        foreach (var g in gos)
            targets.Add(g.transform);
    }

    void CleanNullTargets()
    {
        for (int i = targets.Count - 1; i >= 0; i--)
            if (targets[i] == null)
                targets.RemoveAt(i);
    }
}
