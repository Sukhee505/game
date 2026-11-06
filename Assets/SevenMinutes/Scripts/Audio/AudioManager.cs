using System.Collections.Generic;
using UnityEngine;
using SevenMinutes.Core;
using SevenMinutes.Environment;

namespace SevenMinutes.Audio
{
    public enum SoundId
    {
        Footstep, RobotStep, RobotChirp, Servo,
        Static, Glitch, Stinger, Laugh, Heartbeat, Tick, Warning,
        Beep, KeyPress, Error, Success, Unlock, Drawer, Creak, Rattle, Click, Lever, Paper,
        Whoosh, Impact, Slam, LaserHum, Zap, Lava, Grind, Fall, Burn, Clank, PowerUp, Victory,
        Drone, Tension, Wind, Garden
    }

    /// <summary>
    /// All audio playback: pooled one-shots (2D/3D), registered 3D loops (lasers, lava, machines),
    /// layered music (drone + tension + heartbeat) that intensifies as time runs out.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private readonly Dictionary<SoundId, AudioClip[]> clips = new Dictionary<SoundId, AudioClip[]>();
        private readonly Dictionary<SurfaceType, AudioClip[]> footsteps = new Dictionary<SurfaceType, AudioClip[]>();
        private readonly List<AudioSource> pool = new List<AudioSource>();
        private readonly List<(AudioSource src, float vol)> loops = new List<(AudioSource, float)>();
        private int poolIndex;

        private AudioSource drone, tension, heartbeat, wind, garden, ui;
        private float droneTarget = 0.35f, tensionTarget, heartbeatTarget, windTarget = 0.25f, gardenTarget;
        private float heartbeatPitch = 1f;

        private void Awake()
        {
            Instance = this;
            Generate();
            for (int i = 0; i < 24; i++) pool.Add(CreateSource("Sfx" + i, false));

            drone = CreateSource("Music_Drone", true, Get(SoundId.Drone));
            tension = CreateSource("Music_Tension", true, Get(SoundId.Tension));
            heartbeat = CreateSource("Heartbeat", true, Get(SoundId.Heartbeat));
            wind = CreateSource("Ambience_Wind", true, Get(SoundId.Wind));
            garden = CreateSource("Ambience_Garden", true, Get(SoundId.Garden));
            ui = CreateSource("UI", false);
            ui.ignoreListenerPause = true;

            foreach (var s in new[] { drone, tension, heartbeat, wind, garden })
            {
                s.volume = 0f;
                s.Play();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private AudioSource CreateSource(string name, bool loop, AudioClip clip = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.clip = clip;
            s.spatialBlend = 0f;
            s.dopplerLevel = 0f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 1.5f;
            s.maxDistance = 28f;
            return s;
        }

        private void Generate()
        {
            Put(SoundId.RobotStep, ProceduralAudio.RobotStep(0), ProceduralAudio.RobotStep(1), ProceduralAudio.RobotStep(2));
            Put(SoundId.RobotChirp, ProceduralAudio.RobotChirp());
            Put(SoundId.Servo, ProceduralAudio.Servo());
            Put(SoundId.Static, ProceduralAudio.Static());
            Put(SoundId.Glitch, ProceduralAudio.Glitch());
            Put(SoundId.Stinger, ProceduralAudio.Stinger());
            Put(SoundId.Laugh, ProceduralAudio.Laugh());
            Put(SoundId.Heartbeat, ProceduralAudio.Heartbeat());
            Put(SoundId.Tick, ProceduralAudio.Tick());
            Put(SoundId.Warning, ProceduralAudio.Warning());
            Put(SoundId.Beep, ProceduralAudio.Beep(1200f, 0.14f, "Beep"));
            Put(SoundId.KeyPress, ProceduralAudio.Beep(1500f, 0.07f, "Key"));
            Put(SoundId.Error, ProceduralAudio.Error());
            Put(SoundId.Success, ProceduralAudio.Success());
            Put(SoundId.Unlock, ProceduralAudio.Unlock());
            Put(SoundId.Drawer, ProceduralAudio.Drawer());
            Put(SoundId.Creak, ProceduralAudio.Creak());
            Put(SoundId.Rattle, ProceduralAudio.Rattle());
            Put(SoundId.Click, ProceduralAudio.Click());
            Put(SoundId.Lever, ProceduralAudio.Lever());
            Put(SoundId.Paper, ProceduralAudio.Paper());
            Put(SoundId.Whoosh, ProceduralAudio.Whoosh());
            Put(SoundId.Impact, ProceduralAudio.Impact(1f, "Impact"));
            Put(SoundId.Slam, ProceduralAudio.Impact(0.7f, "Slam"));
            Put(SoundId.LaserHum, ProceduralAudio.LaserHum());
            Put(SoundId.Zap, ProceduralAudio.Zap());
            Put(SoundId.Lava, ProceduralAudio.LavaLoop());
            Put(SoundId.Grind, ProceduralAudio.Grind());
            Put(SoundId.Fall, ProceduralAudio.Fall());
            Put(SoundId.Burn, ProceduralAudio.Burn());
            Put(SoundId.Clank, ProceduralAudio.Clank());
            Put(SoundId.PowerUp, ProceduralAudio.PowerUp());
            Put(SoundId.Victory, ProceduralAudio.Victory());
            Put(SoundId.Drone, ProceduralAudio.DroneLoop());
            Put(SoundId.Tension, ProceduralAudio.TensionLoop());
            Put(SoundId.Wind, ProceduralAudio.WindLoop());
            Put(SoundId.Garden, ProceduralAudio.GardenLoop());

            footsteps[SurfaceType.Wood] = Variants(0);
            footsteps[SurfaceType.Concrete] = Variants(1);
            footsteps[SurfaceType.Metal] = Variants(2);
            footsteps[SurfaceType.Tile] = Variants(3);
            footsteps[SurfaceType.Grass] = Variants(4);
            Put(SoundId.Footstep, footsteps[SurfaceType.Wood]);
        }

        private static AudioClip[] Variants(int surface) => new[]
        {
            ProceduralAudio.Footstep(surface, 0), ProceduralAudio.Footstep(surface, 1), ProceduralAudio.Footstep(surface, 2)
        };

        private void Put(SoundId id, params AudioClip[] c) => clips[id] = c;

        public AudioClip Get(SoundId id)
        {
            if (!clips.TryGetValue(id, out var arr) || arr.Length == 0) return null;
            return arr[Random.Range(0, arr.Length)];
        }

        // ---------------- one shots ----------------

        public void Play(SoundId id, Vector3? position = null, float volume = 1f, float pitch = 1f)
        {
            PlayClip(Get(id), position, volume, pitch);
        }

        public void PlayClip(AudioClip clip, Vector3? position, float volume, float pitch)
        {
            if (clip == null) return;
            var s = pool[poolIndex];
            poolIndex = (poolIndex + 1) % pool.Count;
            s.Stop();
            s.clip = clip;
            s.pitch = pitch;
            s.volume = Mathf.Clamp01(volume * GameSettings.SfxVolume);
            if (position.HasValue)
            {
                s.spatialBlend = 1f;
                s.transform.position = position.Value;
            }
            else s.spatialBlend = 0f;
            s.Play();
        }

        public void PlayUI(SoundId id, float volume = 0.6f)
        {
            var c = Get(id);
            if (c != null) ui.PlayOneShot(c, volume * GameSettings.SfxVolume);
        }

        public void PlayFootstep(SurfaceType surface, Vector3 position, float volume)
        {
            if (!footsteps.TryGetValue(surface, out var arr)) arr = footsteps[SurfaceType.Wood];
            PlayClip(arr[Random.Range(0, arr.Length)], position, volume, Random.Range(0.9f, 1.1f));
        }

        public void PlayFail(FailKind kind)
        {
            switch (kind)
            {
                case FailKind.Fall: Play(SoundId.Fall, null, 0.9f); break;
                case FailKind.Burn: Play(SoundId.Burn, null, 1f); Play(SoundId.Impact, null, 0.5f, 1.3f); break;
                case FailKind.Laser: Play(SoundId.Zap, null, 1f); break;
                case FailKind.Crush: Play(SoundId.Slam, null, 1f); break;
                default: Play(SoundId.Impact, null, 1f); break;
            }
            Play(SoundId.Stinger, null, 0.35f, 1.4f);
        }

        // ---------------- registered loops ----------------

        /// <summary>Creates a looping 3D source parented to the given transform (lasers, lava, machines).</summary>
        public AudioSource CreateLoop(SoundId id, Transform parent, float volume, float maxDistance = 18f, float pitch = 1f)
        {
            var go = new GameObject("Loop_" + id);
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = Get(id);
            s.loop = true;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 1f;
            s.maxDistance = maxDistance;
            s.dopplerLevel = 0f;
            s.pitch = pitch;
            s.volume = volume * GameSettings.SfxVolume;
            s.time = Random.Range(0f, s.clip != null ? s.clip.length * 0.9f : 0f);
            s.Play();
            loops.Add((s, volume));
            return s;
        }

        // ---------------- music & tension ----------------

        public void StartHouseAmbience()
        {
            droneTarget = 0.35f;
            windTarget = 0.22f;
        }

        /// <summary>0 = calm, 1 = 3 min warning, 2 = last minute (heartbeat), 3 = last 30 s.</summary>
        public void SetTension(int level)
        {
            switch (level)
            {
                case 0: tensionTarget = 0f; heartbeatTarget = 0f; break;
                case 1: tensionTarget = 0.18f; heartbeatTarget = 0f; break;
                case 2: tensionTarget = 0.4f; heartbeatTarget = 0.45f; heartbeatPitch = 1f; break;
                default: tensionTarget = 0.55f; heartbeatTarget = 0.8f; heartbeatPitch = 1.35f; droneTarget = 0.45f; break;
            }
        }

        public void StopGameplayLoops()
        {
            droneTarget = 0f;
            tensionTarget = 0f;
            heartbeatTarget = 0f;
            windTarget = 0f;
            foreach (var l in loops)
                if (l.src != null) l.src.Stop();
        }

        public void StartGardenAmbience()
        {
            gardenTarget = 0.4f;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float music = GameSettings.MusicVolume;
            float sfx = GameSettings.SfxVolume;
            Fade(drone, droneTarget * music, dt * 0.4f);
            Fade(tension, tensionTarget * music, dt * 0.3f);
            Fade(heartbeat, heartbeatTarget * sfx, dt * 0.5f);
            Fade(wind, windTarget * sfx, dt * 0.3f);
            Fade(garden, gardenTarget * sfx, dt * 0.2f);
            heartbeat.pitch = Mathf.MoveTowards(heartbeat.pitch, heartbeatPitch, dt * 0.2f);

            for (int i = loops.Count - 1; i >= 0; i--)
            {
                var l = loops[i];
                if (l.src == null) { loops.RemoveAt(i); continue; }
                l.src.volume = l.vol * sfx;
            }
        }

        private static void Fade(AudioSource s, float target, float step)
        {
            s.volume = Mathf.MoveTowards(s.volume, target, step);
        }
    }
}
