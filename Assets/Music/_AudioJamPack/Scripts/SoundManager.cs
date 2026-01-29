using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    private bool _initialized;
    
// Track active looping SFX sources by SoundId
    private readonly Dictionary<SoundId, List<AudioSource>> _loopingSfx = new();
    
    [Header("Debug")]
    [Tooltip("If ON, SoundManager will log what it plays, where and with what settings.\nTurn OFF for release builds.")]
    [SerializeField] private bool debugLogs = false;


    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing one
                _instance = FindObjectOfType<SoundManager>();

                // If still null, create a new one
                if (_instance == null)
                {
                    var go = new GameObject("[SoundManager]");
                    _instance = go.AddComponent<SoundManager>();
                }
            }

            _instance.InitIfNeeded();
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Library")] [SerializeField] private SoundLibrary library;

    [Header("Pooling")] [SerializeField, Min(1)]
    private int sfxPoolSize = 12;

    [SerializeField, Min(1)] private int sfx3dPoolSize = 8;

    [Header("Music Crossfade")] [SerializeField, Min(0f)]
    private float defaultMusicFadeSeconds = 0.75f;

    [Header("3D SFX")]
    [Tooltip("If OFF, all positional SFX (PlayAt/Play3D) are played as 2D.\n" +
             "Turn OFF for pure 2D games that don't need spatial audio.")]
    public bool support3DSound = true;

    // Audio sources
    private AudioSource _musicA;
    private AudioSource _musicB;
    private AudioSource _ambience;
    private AudioSource _ui;
    private List<AudioSource> _sfxPool;
    private List<AudioSource> _sfx3dPool;

    // State
    private readonly Dictionary<SoundCue, float> _cooldownUntil = new();
    private readonly HashSet<int> _warnedMissing = new(); // warn-once

    // Settings (PlayerPrefs)
    private const string Pref_Master = "JamAudio.Master";
    private const string Pref_Music = "JamAudio.Music";
    private const string Pref_Sfx = "JamAudio.Sfx";
    private const string Pref_Ui = "JamAudio.Ui";
    private const string Pref_Amb = "JamAudio.Amb";

    private const string Pref_MuteMusic = "JamAudio.Mute.Music";
    private const string Pref_MuteSfx = "JamAudio.Mute.Sfx";
    private const string Pref_MuteUi = "JamAudio.Mute.Ui";
    private const string Pref_MuteAmb = "JamAudio.Mute.Amb";

    private float _masterVol = 1f;
    private float _musicVol = 1f;
    private float _sfxVol = 1f;
    private float _uiVol = 1f;
    private float _ambVol = 1f;

    private bool _muteMusic = false;
    private bool _muteSfx = false;
    private bool _muteUi = false;
    private bool _muteAmb = false;

    private Coroutine _musicFadeRoutine;
    
    private void InitIfNeeded()
    {
        if (_initialized) return;

        EnsureSources();
        LoadSettings();
        ApplyAllVolumes();

        _initialized = true;
    }

    #region Unity

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitIfNeeded();
    }

    #endregion
    
    #region Mixer settings
// At top of class fields:
    [Header("Audio Mixer (optional)")]
    [Tooltip("If assigned, AudioSources will route to these mixer groups.\n" +
             "If left empty, SoundManager uses plain AudioSource routing only.")]
    public UnityEngine.Audio.AudioMixer mixer;

    public UnityEngine.Audio.AudioMixerGroup masterGroup;
    public UnityEngine.Audio.AudioMixerGroup musicGroup;
    public UnityEngine.Audio.AudioMixerGroup sfxGroup;
    public UnityEngine.Audio.AudioMixerGroup uiGroup;
    public UnityEngine.Audio.AudioMixerGroup ambienceGroup;

    [Tooltip("If OFF, ignores mixer groups even if they are assigned.")]
    public bool useAudioMixerRouting = false;
    
    private UnityEngine.Audio.AudioMixerGroup GetGroupForBus(SoundBus bus)
    {
        if (!useAudioMixerRouting || mixer == null)
            return null;

        switch (bus)
        {
            case SoundBus.Music:    return musicGroup   ?? masterGroup;
            case SoundBus.SFX:      return sfxGroup     ?? masterGroup;
            case SoundBus.UI:       return uiGroup      ?? masterGroup;
            case SoundBus.Ambience: return ambienceGroup ?? masterGroup;
            default:                return masterGroup;
        }
    }
    private void AssignMixerGroup(AudioSource src, SoundBus bus)
    {
        if (src == null) return;
        var g = GetGroupForBus(bus);
        if (g != null)
            src.outputAudioMixerGroup = g;
    }


    #endregion
    
   #region Public API (this is all you need to use)

// ─────────────────────────────────────────────────────────────
// BASIC PLAYBACK
// ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Stop all currently-playing looping SFX for a specific SoundId.
    /// Does nothing for one-shot SFX or UI sounds.
    /// </summary>
    public static void Stop(SoundId id)
    {
        Instance?.StopInternal(id);
    }

    private void StopInternal(SoundId id)
    {
        if (!_loopingSfx.TryGetValue(id, out var list)) return;

        DebugLog($"Stop LOOPING SFX id={id}, count={list.Count}");

        for (int i = 0; i < list.Count; i++)
        {
            var src = list[i];
            if (src != null)
            {
                src.Stop();
                src.clip = null;
            }
        }

        list.Clear();
    }



    /// <summary>
/// Play a sound once. The SoundCue's Bus decides how it is routed:
/// SFX / UI / Ambience / Music.
/// </summary>
public static void Play(SoundId id) =>
    Instance.PlayInternal(id, worldPos: null);

/// <summary>
/// Play a sound at a world position. If 3D is disabled globally
/// or on the SoundCue, it falls back to 2D playback.
/// </summary>
public static void PlayAt(SoundId id, Vector3 position) =>
    Instance.PlayInternal(id, worldPos: position);

// Convenience aliases (all just call Play / PlayAt)

/// <summary>Explicit alias for SFX – same as Play(id).</summary>
public static void PlaySfx(SoundId id) => Play(id);

/// <summary>Explicit alias for UI sounds – same as Play(id).</summary>
public static void PlayUi(SoundId id) => Play(id);

/// <summary>
/// Explicit alias for 3D SFX – same as PlayAt(id, position).
/// </summary>
public static void Play3D(SoundId id, Vector3 position) =>
    PlayAt(id, position);

// ─────────────────────────────────────────────────────────────
// MUSIC
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Play a music track by SoundId, auto-crossfading from the previous track.
/// Optional fade seconds; if null, uses the default fade.
/// </summary>
public static void PlayMusic(SoundId id, float? fade = null) =>
    Instance.PlayMusicInternal(id, fade ?? Instance.defaultMusicFadeSeconds);

/// <summary>
/// Stop any playing music, with optional fade.
/// </summary>
public static void StopMusic(float? fade = null) =>
    Instance.StopMusicInternal(fade ?? Instance.defaultMusicFadeSeconds);

// ─────────────────────────────────────────────────────────────
// AMBIENCE
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Start an ambience loop (bus = Ambience). Volume is controlled
/// by the Ambience bus + Master volume.
/// </summary>
public static void PlayAmbience(SoundId id, float fade = 0.5f) =>
    Instance.PlayAmbienceInternal(id, fade);

/// <summary>
/// Stop ambience playback (simple fade-out).
/// </summary>
public static void StopAmbience(float fade = 0.5f) =>
    Instance.StopAmbienceInternal(fade);

// ─────────────────────────────────────────────────────────────
// VOLUMES
// ─────────────────────────────────────────────────────────────

/// <summary>Set global master volume (0..1). Saved to PlayerPrefs.</summary>
public static void SetMasterVolume(float v) =>
    Instance.SetVolumeInternal(Pref_Master, ref Instance._masterVol, v);

/// <summary>Set music bus volume (0..1). Saved to PlayerPrefs.</summary>
public static void SetMusicVolume(float v) =>
    Instance.SetVolumeInternal(Pref_Music, ref Instance._musicVol, v);

/// <summary>Set SFX bus volume (0..1). Saved to PlayerPrefs.</summary>
public static void SetSfxVolume(float v) =>
    Instance.SetVolumeInternal(Pref_Sfx, ref Instance._sfxVol, v);

/// <summary>Set UI bus volume (0..1). Saved to PlayerPrefs.</summary>
public static void SetUiVolume(float v) =>
    Instance.SetVolumeInternal(Pref_Ui, ref Instance._uiVol, v);

/// <summary>Set Ambience bus volume (0..1). Saved to PlayerPrefs.</summary>
public static void SetAmbienceVolume(float v) =>
    Instance.SetVolumeInternal(Pref_Amb, ref Instance._ambVol, v);

// ─────────────────────────────────────────────────────────────
// MUTES
// ─────────────────────────────────────────────────────────────

/// <summary>Mute/unmute Music bus. Saved to PlayerPrefs.</summary>
public static void SetMuteMusic(bool m) =>
    Instance.SetMuteInternal(Pref_MuteMusic, ref Instance._muteMusic, m);

/// <summary>Mute/unmute SFX bus. Saved to PlayerPrefs.</summary>
public static void SetMuteSfx(bool m) =>
    Instance.SetMuteInternal(Pref_MuteSfx, ref Instance._muteSfx, m);

/// <summary>Mute/unmute UI bus. Saved to PlayerPrefs.</summary>
public static void SetMuteUi(bool m) =>
    Instance.SetMuteInternal(Pref_MuteUi, ref Instance._muteUi, m);

/// <summary>Mute/unmute Ambience bus. Saved to PlayerPrefs.</summary>
public static void SetMuteAmbience(bool m) =>
    Instance.SetMuteInternal(Pref_MuteAmb, ref Instance._muteAmb, m);

// ─────────────────────────────────────────────────────────────
// OPTIONAL: READING BACK (for options UI)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Get current volume levels for all buses (0..1).
/// </summary>
public static (float master, float music, float sfx, float ui, float amb) GetVolumes()
{
    var inst = Instance;
    return (inst._masterVol, inst._musicVol, inst._sfxVol, inst._uiVol, inst._ambVol);
}

/// <summary>
/// Get current mute flags for all buses.
/// </summary>
public static (bool music, bool sfx, bool ui, bool amb) GetMutes()
{
    var inst = Instance;
    return (inst._muteMusic, inst._muteSfx, inst._muteUi, inst._muteAmb);
}

#endregion

    #region Core Playback

    private void PlayInternal(SoundId id, Vector3? worldPos)
    {
        var cue = ResolveCue(id);
        if (cue == null) return;

        if (!CanPlayByCooldown(cue)) return;

        var clip = cue.GetRandomClip();
        if (clip == null)
        {
            WarnOnce((int)id, $"SoundCue '{cue.name}' has no clips.");
            return;
        }

        switch (cue.bus)
        {
            case SoundBus.Music:
                PlayMusicInternal(id, defaultMusicFadeSeconds);
                return;

            case SoundBus.Ambience:
                PlayAmbienceInternal(id, fade: 0.5f);
                return;

            case SoundBus.UI:
                PlayUI(cue, clip);
                return;

            case SoundBus.SFX:
            default:
                if (worldPos.HasValue)
                    PlaySfxAt(id, cue, clip, worldPos.Value);   // <-- pass id
                else
                    PlaySfx2D(id, cue, clip);                    // <-- pass id
                return;
        }
    }


    private void PlaySfx2D(SoundId id, SoundCue cue, AudioClip clip)
    {
        if (_muteSfx) return;

        var src = GetNextFromPool(_sfxPool);
        Configure2DSource(src);
        AssignMixerGroup(src, SoundBus.SFX);

        src.pitch  = cue.GetPitch();
        src.volume = ComputeBusVolume(SoundBus.SFX) * cue.volume;

        if (cue.loop)
        {
            // Looping SFX – interruptible via SoundManager.Stop(id)
            src.clip = clip;
            src.loop = true;
            src.Play();

            TrackLoopingSfx(id, src);

            DebugLog($"Play SFX 2D (LOOP) id={id}, cue='{cue.name}', clip='{clip.name}', " +
                     $"vol={src.volume:F2}, pitch={src.pitch:F2}");
        }
        else
        {
            // Normal one-shot
            src.PlayOneShot(clip);

            DebugLog($"Play SFX 2D (ONE-SHOT) id={id}, cue='{cue.name}', clip='{clip.name}', " +
                     $"busVol={ComputeBusVolume(SoundBus.SFX):F2}, cueVol={cue.volume:F2}");
        }
    }




    private void PlaySfxAt(SoundId id, SoundCue cue, AudioClip clip, Vector3 pos)
    {
        if (_muteSfx) return;

        // If 3D unsupported globally or for this cue, treat as 2D SFX
        if (!support3DSound || !cue.use3DWhenPositional)
        {
            DebugLog($"Play SFX 3D requested, but 3D disabled → fallback to 2D. id={id}, pos={pos}");
            PlaySfx2D(id, cue, clip);
            return;
        }

        var src = GetNextFromPool(_sfx3dPool);
        Configure3DSource(src, cue);
        AssignMixerGroup(src, SoundBus.SFX);

        src.transform.position = pos;
        src.pitch  = cue.GetPitch();
        src.volume = ComputeBusVolume(SoundBus.SFX) * cue.volume;

        if (cue.loop)
        {
            src.clip = clip;
            src.loop = true;
            src.Play();

            TrackLoopingSfx(id, src);

            DebugLog($"Play SFX 3D (LOOP) id={id}, cue='{cue.name}', clip='{clip.name}', " +
                     $"pos={pos}, vol={src.volume:F2}, pitch={src.pitch:F2}, " +
                     $"minDist={cue.minDistance:F1}, maxDist={cue.maxDistance:F1}");
        }
        else
        {
            src.clip = clip;
            src.loop = false;
            src.Play();

            DebugLog($"Play SFX 3D (ONE-SHOT) id={id}, cue='{cue.name}', clip='{clip.name}', " +
                     $"pos={pos}, vol={src.volume:F2}, pitch={src.pitch:F2}, " +
                     $"minDist={cue.minDistance:F1}, maxDist={cue.maxDistance:F1}");
        }
    }


    private void TrackLoopingSfx(SoundId id, AudioSource src)
    {
        if (!_loopingSfx.TryGetValue(id, out var list))
        {
            list = new List<AudioSource>();
            _loopingSfx[id] = list;
        }

        list.Add(src);
    }



    private void PlayUI(SoundCue cue, AudioClip clip)
    {
        if (_muteUi) return;

        _ui.pitch  = cue.GetPitch();
        _ui.volume = ComputeBusVolume(SoundBus.UI) * cue.volume;
        _ui.PlayOneShot(clip);

        DebugLog($"Play UI ONE-SHOT cue='{cue.name}', clip='{clip.name}', vol={_ui.volume:F2}, pitch={_ui.pitch:F2}");
    }


    #endregion

    #region Music / Ambience

    private void PlayMusicInternal(SoundId id, float fadeSeconds)
    {
        var cue = ResolveCue(id);
        if (cue == null) return;

        if (cue.bus != SoundBus.Music)
        {
            // Allow calling PlayMusic on non-music IDs; just fallback to general play.
            PlayInternal(id, null);
            return;
        }

        var clip = cue.GetRandomClip();
        if (clip == null)
        {
            WarnOnce((int)id, $"Music cue '{cue.name}' has no clips.");
            return;
        }

        if (_muteMusic) return;

        // If already playing this clip, do nothing.
        if ((_musicA != null && _musicA.isPlaying && _musicA.clip == clip) ||
            (_musicB != null && _musicB.isPlaying && _musicB.clip == clip))
            return;

        // Pick "from" and "to" tracks for crossfade.
        // Prefer the one that's currently playing as "from".
        AudioSource from = null;
        AudioSource to = null;

        if (_musicA != null && _musicA.isPlaying)
        {
            from = _musicA;
            to = _musicB;
        }
        else if (_musicB != null && _musicB.isPlaying)
        {
            from = _musicB;
            to = _musicA;
        }
        else
        {
            // Nothing is playing yet, arbitrarily use A as target.
            from = null;
            to = _musicA ?? _musicB;
        }

        if (to == null)
        {
            // Safety net: if somehow both are null, bail.
            WarnOnce((int)id, "Music sources are not initialized.");
            return;
        }

        to.clip = clip;
        to.loop = cue.loop;
        to.pitch = 1f;
        to.volume = 0f;
        to.Play();
        
        DebugLog($"Play MUSIC id={id}, cue='{cue.name}', clip='{clip.name}', " +
                $"fade={fadeSeconds:F2}, loop={cue.loop}, " +
                $"targetBusVol={ComputeBusVolume(SoundBus.Music):F2}, cueVol={cue.volume:F2}");

        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(
            CrossFade(from, to, fadeSeconds, SoundBus.Music, cue.volume)
        );
        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(
            CrossFade(from, to, fadeSeconds, SoundBus.Music, cue.volume)
        );
    }


    private void StopMusicInternal(float fadeSeconds)
    {
        DebugLog($"Stop MUSIC fade={fadeSeconds:F2}");

        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(FadeOutBothMusic(fadeSeconds));
    }


    private void PlayAmbienceInternal(SoundId id, float fade)
    {
        var cue = ResolveCue(id);
        if (cue == null) return;
        if (cue.bus != SoundBus.Ambience)
        {
            PlayInternal(id, null);
            return;
        }

        var clip = cue.GetRandomClip();
        if (clip == null)
        {
            WarnOnce((int)id, $"Ambience cue '{cue.name}' has no clips.");
            return;
        }

        if (_muteAmb) return;

        _ambience.clip = clip;
        _ambience.loop = true; // ambience is always looped
        _ambience.pitch = 1f;

        if (!_ambience.isPlaying) _ambience.Play();

        // snap volume (simple). You can fade later if you want.
        _ambience.volume = ComputeBusVolume(SoundBus.Ambience) * cue.volume;

        DebugLog($"Play AMBIENCE id={id}, cue='{cue.name}', clip='{clip.name}', " +
                 $"vol={_ambience.volume:F2}, loop=true");

    }

    private void StopAmbienceInternal(float fade)
    {
        if (!_ambience.isPlaying) return;

        DebugLog($"Stop AMBIENCE fade={fade:F2}");
        StartCoroutine(FadeOutAndStop(_ambience, fade));
    }


    #endregion

    #region Volumes / Mutes (PlayerPrefs)

    private void SetVolumeInternal(string key, ref float field, float v)
    {
        field = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(key, field);
        PlayerPrefs.Save();
        ApplyAllVolumes();
    }

    private void SetMuteInternal(string key, ref bool field, bool m)
    {
        field = m;
        PlayerPrefs.SetInt(key, field ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAllVolumes();
    }

    private void LoadSettings()
    {
        _masterVol = PlayerPrefs.GetFloat(Pref_Master, 1f);
        _musicVol = PlayerPrefs.GetFloat(Pref_Music, 1f);
        _sfxVol = PlayerPrefs.GetFloat(Pref_Sfx, 1f);
        _uiVol = PlayerPrefs.GetFloat(Pref_Ui, 1f);
        _ambVol = PlayerPrefs.GetFloat(Pref_Amb, 1f);

        _muteMusic = PlayerPrefs.GetInt(Pref_MuteMusic, 0) == 1;
        _muteSfx = PlayerPrefs.GetInt(Pref_MuteSfx, 0) == 1;
        _muteUi = PlayerPrefs.GetInt(Pref_MuteUi, 0) == 1;
        _muteAmb = PlayerPrefs.GetInt(Pref_MuteAmb, 0) == 1;
    }

    private void ApplyAllVolumes()
    {
        // Music sources
        var musicBus = ComputeBusVolume(SoundBus.Music);
        if (_muteMusic) musicBus = 0f;

        // Keep crossfade volumes meaningful:
        if (_musicA.isPlaying) _musicA.volume = Mathf.Min(_musicA.volume, musicBus);
        if (_musicB.isPlaying) _musicB.volume = Mathf.Min(_musicB.volume, musicBus);

        // Ambience
        _ambience.volume = _muteAmb ? 0f : ComputeBusVolume(SoundBus.Ambience);

        // UI base volume (PlayOneShot uses current volume multiplier)
        _ui.volume = _muteUi ? 0f : ComputeBusVolume(SoundBus.UI);
    }

    private float ComputeBusVolume(SoundBus bus)
    {
        float busVol = bus switch
        {
            SoundBus.Music => _musicVol,
            SoundBus.SFX => _sfxVol,
            SoundBus.UI => _uiVol,
            SoundBus.Ambience => _ambVol,
            _ => 1f
        };

        bool muted = bus switch
        {
            SoundBus.Music => _muteMusic,
            SoundBus.SFX => _muteSfx,
            SoundBus.UI => _muteUi,
            SoundBus.Ambience => _muteAmb,
            _ => false
        };

        return muted ? 0f : (_masterVol * busVol);
    }

    #endregion

    #region helpers

    private SoundCue ResolveCue(SoundId id)
    {
        if (library == null)
        {
            // -1 = "global" warning, not per-sound
            WarnOnce(-1, "SoundManager has no SoundLibrary assigned. " +
                         "Assign the SoundLibrary asset on the SoundManager component.");
            return null;
        }

        if (library.cues == null || library.cues.Count == 0)
        {
            WarnOnce(-2, "SoundManager: SoundLibrary has no SoundCues. " +
                         "Create SoundCue assets and run Tools → JamAudio → Regenerate SoundId Enum.");
            return null;
        }

        int index = (int)id;
        if (index < 0 || index >= library.cues.Count)
        {
            WarnOnce(index, $"SoundManager: SoundId '{id}' (index {index}) not found in SoundLibrary.cues. " +
                            "Did you add / rename SoundCues and forget to run " +
                            "Tools → JamAudio → Regenerate SoundId Enum?");
            return null;
        }

        var cue = library.cues[index];
        if (cue == null)
        {
            WarnOnce(index, $"SoundManager: SoundLibrary.cues[{index}] is null.");
            return null;
        }

        if (cue.clips == null || cue.clips.Length == 0)
        {
            WarnOnce(index, $"SoundManager: SoundCue '{cue.name}' has no clips assigned. It will play silence.");
        }

        return cue;
    }
    
    private void DebugLog(string msg)
    {
        if (!debugLogs) return;
        Debug.Log("[SoundManager] " + msg);
    }
    private bool CanPlayByCooldown(SoundCue cue)
    {
        if (cue.cooldownSeconds <= 0f) return true;

        float now = Time.unscaledTime;
        if (_cooldownUntil.TryGetValue(cue, out float until) && now < until)
            return false;

        _cooldownUntil[cue] = now + cue.cooldownSeconds;
        return true;
    }

    private void WarnOnce(int key, string msg)
    {
#if UNITY_EDITOR
        if (_warnedMissing.Add(key))
            Debug.LogWarning($"[SoundManager] {msg}", this);
#else
            // In build, keep it quiet (change to warn-once if you want).
#endif
    }

    #endregion
  
    #region Other
private void EnsureSources()
{
    // --- Music A ---
    if (_musicA == null)
    {
        _musicA = CreateChildSource("MusicA", spatialBlend: 0f);
        AssignMixerGroup(_musicA, SoundBus.Music);
    }

    // --- Music B (for crossfade) ---
    if (_musicB == null)
    {
        _musicB = CreateChildSource("MusicB", spatialBlend: 0f);
        AssignMixerGroup(_musicB, SoundBus.Music);
    }

    // --- Ambience ---
    if (_ambience == null)
    {
        _ambience = CreateChildSource("Ambience", spatialBlend: 0f);
        AssignMixerGroup(_ambience, SoundBus.Ambience);
    }

    // --- UI ---
    if (_ui == null)
    {
        _ui = CreateChildSource("UI", spatialBlend: 0f);
        AssignMixerGroup(_ui, SoundBus.UI);
    }

    // --- 2D SFX pool ---
    if (_sfxPool == null || _sfxPool.Count == 0)
    {
        _sfxPool = new List<AudioSource>(sfxPoolSize);

        var sfxRoot = new GameObject("SFXPool");
        sfxRoot.transform.SetParent(transform, false);

        for (int i = 0; i < sfxPoolSize; i++)
        {
            var src = sfxRoot.AddComponent<AudioSource>();
            Configure2DSource(src);
            AssignMixerGroup(src, SoundBus.SFX);
            _sfxPool.Add(src);
        }
    }

    // --- 3D SFX pool ---
    if (_sfx3dPool == null || _sfx3dPool.Count == 0)
    {
        _sfx3dPool = new List<AudioSource>(sfx3dPoolSize);

        var sfx3dRoot = new GameObject("SFX3DPool");
        sfx3dRoot.transform.SetParent(transform, false);

        for (int i = 0; i < sfx3dPoolSize; i++)
        {
            var go = new GameObject($"SFX3D_{i:00}");
            go.transform.SetParent(sfx3dRoot.transform, false);
            var src = go.AddComponent<AudioSource>();

            // Generic 3D defaults; cue-specific 3D settings are applied at play time.
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;

            AssignMixerGroup(src, SoundBus.SFX);
            _sfx3dPool.Add(src);
        }
    }
}


    private AudioSource CreateChildSource(string name, float spatialBlend)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = spatialBlend;
        src.rolloffMode = AudioRolloffMode.Linear;
        return src;
    }

    private static void Configure2DSource(AudioSource src)
    {
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        src.rolloffMode = AudioRolloffMode.Linear;
    }

    private static void Configure3DSource(AudioSource src, SoundCue cue)
    {
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = Mathf.Max(0.01f, cue.minDistance);
        src.maxDistance = Mathf.Max(src.minDistance, cue.maxDistance);
    }

    private static AudioSource GetNextFromPool(List<AudioSource> pool)
    {
        // Prefer a free source; otherwise reuse the first
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].isPlaying)
                return pool[i];
        }

        return pool[0];
    }


    private IEnumerator CrossFade(AudioSource from, AudioSource to, float duration, SoundBus bus, float cueVolume = 1f)
    {
        float t = 0f;

        float targetBus = ComputeBusVolume(bus) * cueVolume;
        float fromStart = from ? from.volume : 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);

            if (to) to.volume = Mathf.Lerp(0f, targetBus, a);
            if (from) from.volume = Mathf.Lerp(fromStart, 0f, a);

            yield return null;
        }

        if (to)
            to.volume = targetBus;

        if (from)
        {
            from.Stop();
            from.clip = null;
            from.volume = 0f;
        }
    }


    private IEnumerator FadeOutBothMusic(float duration)
    {
        float t = 0f;
        float aStart = _musicA != null ? _musicA.volume : 0f;
        float bStart = _musicB != null ? _musicB.volume : 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);

            if (_musicA) _musicA.volume = Mathf.Lerp(aStart, 0f, a);
            if (_musicB) _musicB.volume = Mathf.Lerp(bStart, 0f, a);

            yield return null;
        }

        if (_musicA)
        {
            _musicA.Stop();
            _musicA.clip = null;
            _musicA.volume = 0f;
        }

        if (_musicB)
        {
            _musicB.Stop();
            _musicB.clip = null;
            _musicB.volume = 0f;
        }
    }

    private static IEnumerator FadeOutAndStop(AudioSource src, float duration)
    {
        if (src == null)
            yield break;

        float t = 0f;
        float start = src.volume;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            src.volume = Mathf.Lerp(start, 0f, a);
            yield return null;
        }

        src.Stop();
        src.clip = null;
        src.volume = 0f;
    }
    #endregion
}