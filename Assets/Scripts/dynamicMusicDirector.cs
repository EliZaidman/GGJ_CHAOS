using System;
using System.Collections;
using UnityEngine;

public class dynamicMusicDirector : MonoBehaviour
{
    AudioSource[] _sources;
    public int TimeBetweenLayers = 10;

    void Awake()
    {
        _sources = GetComponentsInChildren<AudioSource>();

        foreach (AudioSource source in _sources)
        {
            source.volume = 0;
            source.Play();
        }

        StartCoroutine(PlayAudiosRoutine());
    }

    private IEnumerator PlayAudiosRoutine()
    {
        int index = 0;

        _sources[index].volume = 1;
        index++;

        while (index < _sources.Length)
        {
            yield return new WaitForSeconds(TimeBetweenLayers);
            StartCoroutine(LerpVolumeRoutine(_sources[index]));
            index++;
        }
    }

    private IEnumerator LerpVolumeRoutine(AudioSource source)
    {
        for (float counter = 0; counter < 1; counter += Time.deltaTime)
        {
            source.volume = Mathf.SmoothStep(0, 1, counter);
            yield return null;
        }
    }
}
