using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Plays a sound when this object is clicked.
/// - For UI: attach to a UI element in a Canvas (requires EventSystem).
/// - For world objects: leave "useMouseDown" ON and add a Collider.
/// </summary>
public class PlaySoundOnClick : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Which sound to play when this object is clicked.")]
    public SoundId sound;

    [Tooltip("Also play when this object receives OnMouseDown (Collider required).")]
    public bool useMouseDown = true;

    // UI click (Button, etc.)
    public void OnPointerClick(PointerEventData eventData)
    {
        SoundManager.Play(sound);
    }

    // World click (Collider + Camera ray)
    private void OnMouseDown()
    {
        if (!useMouseDown) return;
        SoundManager.Play(sound);
    }

    /// <summary>
    /// Helper method so you can hook this directly from a Button's OnClick
    /// if you don't want IPointerClickHandler.
    /// </summary>
    public void PlayNow()
    {
        SoundManager.Play(sound);
    }
}