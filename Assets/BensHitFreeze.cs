using UnityEngine;

public class BensHitFreezelHitFreezeusing : MonoBehaviour
{
    static BensHitFreezelHitFreezeusing instance;

    bool freezing;
    float cooldownTimer;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.unscaledDeltaTime;
    }

    public static void Freeze(float duration = 0.05f)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("GlobalHitFreeze");
            instance = go.AddComponent<BensHitFreezelHitFreezeusing>();
        }

        if (instance.freezing) return;
        if (instance.cooldownTimer > 0f) return;

        instance.StartCoroutine(instance.DoFreeze(duration));
    }

    System.Collections.IEnumerator DoFreeze(float duration)
    {
        freezing = true;
        cooldownTimer = 0.15f; // prevents spam

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;

        freezing = false;
    }
}
