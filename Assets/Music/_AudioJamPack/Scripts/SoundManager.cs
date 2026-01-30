using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SoundManager>();
                if (_instance == null)
                {
                    var go = new GameObject("[SoundManager]");
                    _instance = go.AddComponent<SoundManager>();
                }
            }

            _instance.InitIfNeeded();
            return _instance;
        }
    }

    [Header("Library")]
    [SerializeField] private SoundLibrary library;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [Header("Global Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float uiVolume = 1f;
    [Range(0f, 1f)] public float ambienceVolume = 1f;

    [Header("3D Defaults (only for PlayAt)")]
    public bool support3DSound = true;

    private bool _initialized;

    // Dedicated sources
    private AudioSource _oneShot2D;
    private AudioSource _music;
    private AudioSource _ambience;

    // Fast lookup
    private readonly Dictionary<SoundId, SoundCue> _map = new();

    // Cooldowns
    private readonly Dictionary<SoundCue, float> _cooldownUntil = new();

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

    private void InitIfNeeded()
    {
        if (_initialized) return;

        EnsureSources();
        RebuildMap();
        ApplyBusVolumes();

        _initialized = true;
    }

    private void EnsureSources()
    {
        if (_oneShot2D == null) _oneShot2D = CreateChildSource("OneShot2D", spatialBlend: 0f, loop: false);
        if (_music == null) _music = CreateChildSource("Music", spatialBlend: 0f, loop: true);
        if (_ambience == null) _ambience = CreateChildSource("Ambience", spatialBlend: 0f, loop: true);
    }

    private AudioSource CreateChildSource(string name, float spatialBlend, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = spatialBlend; // 0=2D, 1=3D
        src.rolloffMode = AudioRolloffMode.Logarithmic;

        return src;
    }

    private void RebuildMap()
    {
        _map.Clear();

        if (library == null || library.cues == null)
        {
            Log($"SoundLibrary missing on SoundManager.");
            return;
        }

        for (int i = 0; i < library.cues.Count; i++)
        {
            var cue = library.cues[i];
            if (cue == null) continue;

            // Enum values are generated in the same order as library.cues.
            // So we map (SoundId)i -> cue.
            var id = (SoundId)i;

            if (_map.ContainsKey(id))
                continue;

            _map.Add(id, cue);
        }
    }

    private void ApplyBusVolumes()
    {
        // Dedicated channels:
        _music.volume = masterVolume * musicVolume;
        _ambience.volume = masterVolume * ambienceVolume;

        // OneShot2D volume is set per-play based on bus.
        // (Still keep it sane in case something plays without applying volume.)
        _oneShot2D.volume = masterVolume;
    }

    private float GetBusVolume(SoundBus bus)
    {
        return bus switch
        {
            SoundBus.Music => masterVolume * musicVolume,
            SoundBus.Ambience => masterVolume * ambienceVolume,
            SoundBus.UI => masterVolume * uiVolume,
            _ => masterVolume * sfxVolume,
        };
    }

    // ─────────────────────────────────────────────────────────────
    // PUBLIC API (keep calls simple)
    // ─────────────────────────────────────────────────────────────

    public static void Play(SoundId id) =>
        Instance.PlayInternal(id, worldPos: null);

    public static void PlayAt(SoundId id, Vector3 position) =>
        Instance.PlayInternal(id, worldPos: position);

    public static void PlaySfx(SoundId id) => Play(id);
    public static void PlayUi(SoundId id) => Play(id);
    public static void Play3D(SoundId id, Vector3 position) => PlayAt(id, position);

    public static void PlayMusic(SoundId id) =>
        Instance.PlayMusicInternal(id);

    public static void StopMusic() =>
        Instance.StopMusicInternal();

    public static void PlayAmbience(SoundId id) =>
        Instance.PlayAmbienceInternal(id);

    public static void StopAmbience() =>
        Instance.StopAmbienceInternal();

    // Kept for compatibility: stops music/ambience if you pass their ids,
    // otherwise no-op (one-shots don’t need Stop).
    public static void Stop(SoundId id) =>
        Instance.StopInternal(id);

    // ─────────────────────────────────────────────────────────────

    private void PlayInternal(SoundId id, Vector3? worldPos)
    {
        if (!_map.TryGetValue(id, out var cue) || cue == null)
        {
            Log($"Missing cue for id={id}. (Did you regenerate SoundId + assign SoundLibrary?)");
            return;
        }

        // Music/Ambience should use their dedicated methods, but if a designer calls Play() on them:
        if (cue.bus == SoundBus.Music) { PlayMusicInternal(id); return; }
        if (cue.bus == SoundBus.Ambience) { PlayAmbienceInternal(id); return; }

        if (!CanPlayCooldown(cue))
            return;

        var clip = cue.GetRandomClip();
        if (clip == null) return;

        var volume = cue.volume * GetBusVolume(cue.bus);
        var pitch = cue.GetPitch();

        if (worldPos.HasValue && support3DSound && cue.use3DWhenPositional)
        {
            // Super simple 3D one-shot: create a temp AudioSource at position.
            PlayOneShot3D(cue, clip, worldPos.Value, volume, pitch);
        }
        else
        {
            // 2D one-shot: single AudioSource using PlayOneShot.
            _oneShot2D.pitch = pitch;
            _oneShot2D.PlayOneShot(clip, volume);
        }

        if (debugLogs)
        {
            Log($"Play {id} bus={cue.bus} vol={volume:0.00} pitch={pitch:0.00} 3D={(worldPos.HasValue ? "yes" : "no")}");
        }
    }

    private void PlayOneShot3D(SoundCue cue, AudioClip clip, Vector3 pos, float volume, float pitch)
    {
        var go = new GameObject($"OneShot3D_{clip.name}");
        go.transform.position = pos;

        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 1f;
        src.minDistance = Mathf.Max(0f, cue.minDistance);
        src.maxDistance = Mathf.Max(src.minDistance + 0.01f, cue.maxDistance);
        src.rolloffMode = AudioRolloffMode.Logarithmic;

        src.pitch = pitch;
        src.volume = volume;
        src.clip = clip;
        src.Play();

        Destroy(go, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)) + 0.2f);
    }

    private void PlayMusicInternal(SoundId id)
    {
        if (!_map.TryGetValue(id, out var cue) || cue == null) return;

        var clip = cue.GetRandomClip();
        if (clip == null) return;

        _music.Stop();
        _music.clip = clip;
        _music.loop = true;
        _music.pitch = 1f;
        _music.volume = GetBusVolume(SoundBus.Music) * cue.volume;
        _music.Play();

        if (debugLogs) Log($"PlayMusic {id} clip={clip.name}");
    }

    private void StopMusicInternal()
    {
        _music.Stop();
        _music.clip = null;
        if (debugLogs) Log("StopMusic");
    }

    private void PlayAmbienceInternal(SoundId id)
    {
        if (!_map.TryGetValue(id, out var cue) || cue == null) return;

        var clip = cue.GetRandomClip();
        if (clip == null) return;

        _ambience.Stop();
        _ambience.clip = clip;
        _ambience.loop = true;
        _ambience.pitch = 1f;
        _ambience.volume = GetBusVolume(SoundBus.Ambience) * cue.volume;
        _ambience.Play();

        if (debugLogs) Log($"PlayAmbience {id} clip={clip.name}");
    }

    private void StopAmbienceInternal()
    {
        _ambience.Stop();
        _ambience.clip = null;
        if (debugLogs) Log("StopAmbience");
    }

    private void StopInternal(SoundId id)
    {
        // Minimal behavior:
        // If it’s the currently playing music/ambience id, stop them.
        // (We can’t perfectly know without storing “current id”, so just stop based on bus.)
        if (_map.TryGetValue(id, out var cue) && cue != null)
        {
            if (cue.bus == SoundBus.Music) StopMusicInternal();
            if (cue.bus == SoundBus.Ambience) StopAmbienceInternal();
        }
    }

    private bool CanPlayCooldown(SoundCue cue)
    {
        if (cue.cooldownSeconds <= 0f) return true;

        var now = Time.unscaledTime;
        if (_cooldownUntil.TryGetValue(cue, out var until) && now < until)
            return false;

        _cooldownUntil[cue] = now + cue.cooldownSeconds;
        return true;
    }

    private void Log(string msg) => Debug.Log($"[SoundManager] {msg}");
}
