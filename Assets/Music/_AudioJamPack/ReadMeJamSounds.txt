

### 1️⃣ Create a Sound Cue

* Right-click in Project → **Create → JamAudio → Sound Cue**
* Drag **AudioClip(s)** into it
* Set **Bus**: Music / SFX / UI / Ambience

---

### 2️⃣ Update the Sound List

Whenever you add/rename Sound Cues:
👉 **Tools → JamAudio → Regenerate SoundId Enum**

This gives you `SoundId.YourCueName` to use in code.

---

### 3️⃣ Play sounds in code

SoundManager.Play(SoundId.SFX_Click);          // play SFX
SoundManager.PlayAt(SoundId.SFX_Explosion, pos); // 3D SFX
SoundManager.PlayMusic(SoundId.Music_Main);   // music
SoundManager.StopMusic();                     // stop music
```

That’s it.

---

### 4️⃣ Troubleshooting

If a sound doesn’t play:

* Did you regenerate SoundId?
* Did you assign a clip in the cue?
* Is volume > 0 and not muted?

---


