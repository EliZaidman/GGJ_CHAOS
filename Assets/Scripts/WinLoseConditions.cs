using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinLoseConditions : MonoBehaviour
{
    public int TimeForGameInSeconds = 60 * 4;
    public TMP_Text TimerText;
    public TMP_Text ScoreText;
    public GameObject WinScreen;

    private float score = 0;

    public float CurrentTime = 0;

    HashSet<GameObject> _found = new HashSet<GameObject>(100);

    public float Score
    {
        get => score;
        set
        {
            ScoreText.text = score.ToString() + "$";
            score = value;
        }
    }

    private void Awake()
    {
        score = 0;
        ScoreText.text = score.ToString() + "$";
        StartCoroutine(TimerRoutine());
    }

    private IEnumerator TimerRoutine()
    {
        CurrentTime = TimeForGameInSeconds;
        while (CurrentTime > 0)
        {
            TimerText.text = FormatMMSS((int)CurrentTime);
            CurrentTime -= Time.deltaTime;
            yield return null;
        }

        GetComponent<AudioSource>().Play();
        WinScreen.SetActive(true);
        WinScreen.GetComponentInChildren<Button>().onClick.AddListener(() => SceneManager.LoadScene(0));

        string FormatMMSS(int totalSeconds)
        {
            int m = totalSeconds / 60;
            int s = totalSeconds % 60;
            return $"{m:00}:{s:00}";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_found.Contains(other.gameObject))
            return;

        _found.Add(other.gameObject);
        
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
