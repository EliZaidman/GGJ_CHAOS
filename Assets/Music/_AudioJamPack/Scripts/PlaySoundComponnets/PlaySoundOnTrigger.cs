using UnityEngine;

/// <summary>
/// Plays a sound when something enters this trigger.
/// - For 3D: use a Collider with "Is Trigger" checked.
/// - For 2D: use a Collider2D with "Is Trigger" checked.
/// Optional tag filter.
/// </summary>
public class PlaySoundOnTrigger : MonoBehaviour
{
    [Tooltip("Which sound to play when something enters this trigger.")]
    public SoundId sound;

    [Tooltip("If not empty, only objects with this tag will trigger the sound.")]
    public string requiredTag;

    private bool TagMatches(GameObject other)
    {
        if (string.IsNullOrEmpty(requiredTag)) return true;
        return other.CompareTag(requiredTag);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (TagMatches(other.gameObject))
        {
            SoundManager.Play(sound);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TagMatches(other.gameObject))
        {
            SoundManager.Play(sound);
        }
    }
}