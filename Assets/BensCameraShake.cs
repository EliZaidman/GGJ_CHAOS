using UnityEngine;

public class BensCameraShake : MonoBehaviour
{
    public static BensCameraShake Instance;
    Vector3 originalPos;

    void Awake()
    {
        print("Hey im ben, and if you are having camera issues its me and i made it here");
        Instance = this;
        originalPos = transform.localPosition;
    }

    public void Shake(float strength = 0.2f, float duration = 0.15f)
    {
        StartCoroutine(DoShake(strength, duration));
    }

    System.Collections.IEnumerator DoShake(float strength, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localPosition = originalPos + Random.insideUnitSphere * strength;
            yield return null;
        }
        transform.localPosition = originalPos;
      
    }
}