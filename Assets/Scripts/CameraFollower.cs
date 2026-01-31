using System.Linq;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    float originalY;
    public GameObject[] Follow;

    private void Awake()
    {
        originalY = transform.position.y;
    }

    private void Update()
    {
        var dist = 0f;
        for (int i = 0; i < Follow.Length; i++)
        {
            GameObject follower = Follow[i];

            for (int j = i+1; j < Follow.Length; j++)
            {
                var dis = (follower.transform.position -Follow[j].transform.position).magnitude;
                if (dis > dist)
                {
                    dist = dis;
                }
            }
        }

        if (dist > 13f)
        {
            transform.position = new Vector3(transform.position.x, Mathf.Min(originalY + dist -13, 20), transform.position.z);
        }
        else
            transform.position = new Vector3(transform.position.x, originalY, transform.position.z);
    }
}
