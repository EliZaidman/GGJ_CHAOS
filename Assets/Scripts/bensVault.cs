using UnityEngine;

public class bensVault : MonoBehaviour
{
    public bool unlocked;
    public bool fullyOpen;
    public float openangle = 0.8f;

    public int speed = 20;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (unlocked && !fullyOpen )
        {
            this.transform.Rotate(new Vector3(0, 0, 1)* Time.deltaTime* speed );
            if (this.transform.rotation.z < openangle)
            {
                fullyOpen = true;
            }
        }
    }
}
