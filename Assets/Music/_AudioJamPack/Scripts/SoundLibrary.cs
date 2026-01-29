using System.Collections.Generic;
using UnityEngine;

    [CreateAssetMenu(menuName = "JamAudio/Sound Library", fileName = "SL_SoundLibrary")]
    public sealed class SoundLibrary : ScriptableObject
    {
        [Tooltip("All cues. This list is also the source of truth for the generated SoundId enum.")]
        public List<SoundCue> cues = new List<SoundCue>();

        public int Count => cues?.Count ?? 0;

        public SoundCue GetCue(int index)
        {
            if (cues == null) return null;
            if (index < 0 || index >= cues.Count) return null;
            return cues[index];
        }
    }