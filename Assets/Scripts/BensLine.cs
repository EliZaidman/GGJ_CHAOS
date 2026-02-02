using System.Collections.Generic;
using UnityEngine;

public class BensLine : MonoBehaviour
{
    [SerializeField] List<GameObject> Objects_to_line_through;

    [SerializeField] LineRenderer lineRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (lineRenderer)
        {
            lineRenderer = GetComponent(typeof(LineRenderer)) as LineRenderer;
        }

        if (Objects_to_line_through == null)
        {
            return;
        }


        lineRenderer.positionCount = Objects_to_line_through.Count;

        for (int i = 0; i < Objects_to_line_through.Count; i++)
        {
            if (i == 0)
            {
                lineRenderer.SetPosition(i, transform.position);
            }
            lineRenderer.SetPosition(i, Objects_to_line_through[i].transform.position);
        }
    }
}