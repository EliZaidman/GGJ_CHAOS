using System.Collections;
using UnityEngine;
using UnityEngine.Animations;   // IMPORTANT


public class RoundIntroFlyover : MonoBehaviour
{
    public PositionConstraint positionConstraint;
    
    [Header("Delay")]
    public float startDelay = 2f;

    [Header("Refs")]
    public Camera introCam;                 // your main camera
    public Transform[] points;              // P0..Pn
    public GameObject gameplayRoot;         // disable input/player scripts etc during intro

    [Header("Timing")]
    public float totalDuration = 2.5f;      // whole intro length
    public float ease = 2.0f;               // 1=linear, 2=smoother

    [Header("Start / End")]
    public Transform gameplayCamTarget;     // where camera should end for gameplay (optional)
    public bool playOnStart = true;

    bool _running;

    void Start()
    {
        if (positionConstraint == null)
        {
            positionConstraint=  GetComponent<PositionConstraint>();
        }
        if (playOnStart) Play();
    }

    public void Play()
    {
        if (_running) return;
        if (!introCam || points == null || points.Length < 2) return;

        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        _running = true;

       

        //if (gameplayRoot) gameplayRoot.SetActive(false);

        // snap to first point
        introCam.transform.SetPositionAndRotation(points[0].position, points[0].rotation);

        float segmentDuration = totalDuration / (points.Length - 1);

        for (int i = 0; i < points.Length - 1; i++)
        {

            if (i == 0)
            {
               // SoundManager.PlaySfx(SoundId.CountToRoundStart); // ben plays Round start sound
                yield return new WaitForSecondsRealtime(startDelay);
            }
            Transform a = points[i];
            Transform b = points[i + 1];

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, segmentDuration);
                float u = Ease01(Mathf.Clamp01(t), ease);

                introCam.transform.position = Vector3.LerpUnclamped(a.position, b.position, u);
                introCam.transform.rotation = Quaternion.SlerpUnclamped(a.rotation, b.rotation, u);

                yield return null;
            }
            if (positionConstraint)
            {
                positionConstraint.constraintActive = true;
            }

        }

        // optional: snap to gameplay cam target
        if (gameplayCamTarget)
            introCam.transform.SetPositionAndRotation(gameplayCamTarget.position, gameplayCamTarget.rotation);

        if (gameplayRoot) gameplayRoot.SetActive(true);

        _running = false;
    }

    static float Ease01(float t, float power)
    {
        // smoothstep-ish: 0..1 with adjustable softness
        t = Mathf.Clamp01(t);
        float a = Mathf.Pow(t, power);
        float b = Mathf.Pow(1f - t, power);
        return a / (a + b);
    }
}
