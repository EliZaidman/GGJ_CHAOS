using UnityEngine;

/// <summary>
/// Which "channel" this sound plays through.
/// Used by the SoundManager to apply the right volume & mute.
/// </summary>
public enum SoundBus
{
    Music,
    SFX,
    UI,
    Ambience
}

/// <summary>
/// One sound definition in the game:
/// - Which AudioClip(s) to play
/// - Which bus (Music / SFX / UI / Ambience)
/// - Volume, loop, pitch randomization
/// - Optional 3D settings for positional audio
///
/// You don't call this directly. You:
/// 1) Create SoundCue assets
/// 2) Run Tools → JamAudio → Regenerate SoundId Enum
/// 3) Use SoundManager.Play(SoundId.YourCueName) in code
/// </summary>
[CreateAssetMenu(menuName = "JamAudio/Sound Cue", fileName = "SC_NewSoundCue")]
public sealed class SoundCue : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────
    // ID / ENUM NAME
    // ─────────────────────────────────────────────────────────────

    [Header("ID (optional)")]
    [Tooltip("Advanced: override the name used in the generated SoundId enum.\n" +
             "If empty, this asset's name is used.\n" +
             "Example: asset name 'SC_Click', override 'Click' → SoundId.Click.")]
    public string overrideIdName;

    // ─────────────────────────────────────────────────────────────
    // ROUTING
    // ─────────────────────────────────────────────────────────────

    [Header("Channel")]
    [Tooltip("Which channel this sound belongs to.\n" +
             "Music     → background tracks\n" +
             "SFX       → gameplay sound effects\n" +
             "UI        → button clicks, menus, etc.\n" +
             "Ambience  → looping background ambience")]
    public SoundBus bus = SoundBus.SFX;

    // ─────────────────────────────────────────────────────────────
    // CLIPS
    // ─────────────────────────────────────────────────────────────

    [Header("Sound Clip")]
    [Tooltip("One or more AudioClips (the actual audio file).\n" +
             "If multiple clips are set, one is picked randomly each time.")]
    public AudioClip[] clips;

    // ─────────────────────────────────────────────────────────────
    // BASIC PLAYBACK
    // ─────────────────────────────────────────────────────────────

    [Header("Playback")]
    [Tooltip("Base volume for this sound (multiplied by bus/master volumes).")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("Minimum time in seconds between plays of this cue.\n" +
             "Use this to avoid spam (e.g. rapid gunfire UI clicks).")]
    [Min(0f)]
    public float cooldownSeconds = 0f;

    [Tooltip("Loop this sound when played as Music or Ambience.\n" +
             "For SFX/UI this is usually OFF.")]
    public bool loop = false;

    // ─────────────────────────────────────────────────────────────
    // PITCH
    // ─────────────────────────────────────────────────────────────

    [Header("Pitch Randomization")]
    [Tooltip("If ON, pitch is picked randomly between Min and Max each play.\n" +
             "Great for making repeated SFX less repetitive.")]
    public bool randomizePitch = false;

    [Tooltip("Minimum pitch when randomizing (usually around 0.9–1.1).")]
    [Range(-3f, 3f)]
    public float pitchMin = 1f;

    [Tooltip("Maximum pitch when randomizing (usually around 0.9–1.1).")]
    [Range(-3f, 3f)]
    public float pitchMax = 1f;

    // ─────────────────────────────────────────────────────────────
    // 3D SETTINGS (ONLY FOR PlayAt)
    // ─────────────────────────────────────────────────────────────

    [Header("3D (only used by PlayAt)")]
    [Tooltip("If ON and you call SoundManager.PlayAt(...), this sound uses 3D audio.\n" +
             "If OFF, positional sounds will be forced to 2D.")]
    public bool use3DWhenPositional = true;

    [Tooltip("3D audio min distance (sound is at full volume inside this radius).")]
    [Min(0f)]
    public float minDistance = 1f;

    [Tooltip("3D audio max distance (beyond this, sound is inaudible).")]
    [Min(0f)]
    public float maxDistance = 25f;

    // ─────────────────────────────────────────────────────────────
    // RUNTIME HELPERS (used by SoundManager & SoundIdGenerator)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns one random clip from the list (or null if none).
    /// </summary>
    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];
        return clips[Random.Range(0, clips.Length)];
    }

    /// <summary>
    /// Returns a pitch value, optionally randomized between pitchMin/pitchMax.
    /// </summary>
    public float GetPitch()
    {
        if (!randomizePitch) return 1f;
        return Random.Range(pitchMin, pitchMax);
    }

    /// <summary>
    /// Name used by the SoundId generator.
    /// If overrideIdName is empty, uses the asset name.
    /// </summary>
    public string GetEffectiveIdName()
    {
        return string.IsNullOrWhiteSpace(overrideIdName) ? name : overrideIdName;
    }
}
