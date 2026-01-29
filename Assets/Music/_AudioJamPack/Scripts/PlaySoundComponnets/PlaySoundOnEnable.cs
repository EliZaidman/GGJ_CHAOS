using UnityEngine;

/// <summary>
/// Plays a sound once when this GameObject becomes active.
/// Great for simple "spawn" / "open" / "appear" sounds.
/// </summary>
public class PlaySoundOnEnable : MonoBehaviour
{
    [Tooltip("Which sound to play when this object is enabled.")]
    public SoundId sound;

    [Tooltip("If true, also plays once on Start (in case the object starts enabled).")]
    public bool alsoPlayOnStart = false;

    private void OnEnable()
    {
        if (!gameObject.activeInHierarchy) return;
        SoundManager.Play(sound);
    }

    private void Start()
    {
        if (alsoPlayOnStart)
        {
            SoundManager.Play(sound);
        }
    }
}