using UnityEngine;

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
            //Debug.Log("[SoundTest] SoundManager instance: " + (SoundManager.Instance != null));
            //SoundManager.PlayMusic(SoundId.music1);
        }
    }

}

// for 3d sounds
// SoundManager.PlayAt(SoundId.Explosion, hitPos); // for 3D sounds with postition

