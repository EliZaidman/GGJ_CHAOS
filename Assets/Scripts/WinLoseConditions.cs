using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class WinLoseConditions : MonoBehaviour
{
    public float TimeForGameInSeconds = 60 * 4;
    public TMP_Text TimerText;
    public TMP_Text ScoreText;

    public float Score = 0;

    public float CurrentTime = 0;

    private void Awake()
    {
        StartCoroutine(TimerRoutine());
    }

    private IEnumerator TimerRoutine()
    {
        CurrentTime = TimeForGameInSeconds;
        while (CurrentTime > 0)
        {
            TimerText.text = CurrentTime.ToString("mm:ss");
            CurrentTime -= Time.deltaTime;
            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out ScoreEntity score))
        {
            Score += score.Score;
        }
        else
        {
            Score += 1;
            Debug.LogWarning("Score item registered without score entity component.");
        }
    }
}
