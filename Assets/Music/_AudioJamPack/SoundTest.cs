using System;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

public class SoundTest : MonoBehaviour
{
    [Header("Template Behavior")]
    [Tooltip("If ON, play template music on start.")]
    public bool playTemplateMusicOnStart = true;

    [Tooltip("If ON, Spacebar will play template SFX.")]
    public bool playTemplateSfxOnSpace = true;

    private void Awake()
    {
        if (playTemplateMusicOnStart)
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            if (currentSceneIndex == 0)
            {
                SoundManager.PlayMusic(SoundId.Lobby);
            }
            else
            {
                SoundManager.PlayMusic(SoundId.music1);
            }

            //SoundManager.PlayMusic(SoundId.);
            Debug.Log("[SoundTest] SoundManager instance: " + (SoundManager.Instance != null));
            SoundManager.PlayMusic(SoundId.music1);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)){
            print("f11");
            SoundManager.PlayAt(SoundId.HitWhooh, transform.position);}
    }
}

// for 3d sounds
// SoundManager.PlayAt(SoundId.Explosion, hitPos); // for 3D sounds with postition

