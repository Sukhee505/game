using UnityEngine;

namespace SevenMinutes.Audio
{
    /// <summary>
    /// Synthesises every sound effect and music loop at runtime, so the game needs no audio assets.
    /// (Replace any clip in AudioManager with your own files if you have better sounds.)
    /// </summary>
    public static class ProceduralAudio
    {
        public const int Rate = 22050;
        private const float TwoPi = Mathf.PI * 2f;
        private static System.Random rng = new System.Random(1234);

        private static float Rand() => (float)(rng.NextDouble() * 2.0 - 1.0);
        private static float Rand01() => (float)rng.NextDouble();
        private static float[] Buf(float seconds) => new float[Mathf.Max(1, (int)(seconds * Rate))];

        public static AudioClip ToClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ---------------- DSP helpers ----------------

        private static void LowPass(float[] d, float cutoff)
        {
            float rc = 1f / (TwoPi * cutoff);
            float dt = 1f / Rate;
            float a = dt / (rc + dt);
            float y = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                y += a * (d[i] - y);
                d[i] = y;
            }
        }

        private static void HighPass(float[] d, float cutoff)
        {
            float rc = 1f / (TwoPi * cutoff);
            float dt = 1f / Rate;
            float a = rc / (rc + dt);
            float prevX = 0f, prevY = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float x = d[i];
                float y = a * (prevY + x - prevX);
                prevX = x;
                prevY = y;
                d[i] = y;
            }
        }

        private static void Normalize(float[] d, float peak)
        {
            float max = 0.0001f;
            for (int i = 0; i < d.Length; i++) max = Mathf.Max(max, Mathf.Abs(d[i]));
            float g = peak / max;
            for (int i = 0; i < d.Length; i++) d[i] *= g;
        }

        private static float[] MakeLoop(float[] d, float fadeSeconds)
        {
            int fade = Mathf.Min((int)(fadeSeconds * Rate), d.Length / 3);
            int len = d.Length - fade;
            var o = new float[len];
            for (int i = 0; i < len; i++) o[i] = d[i];
            for (int i = 0; i < fade; i++)
            {
                float w = (float)i / fade;
                o[i] = d[i] * w + d[len + i] * (1f - w);
            }
            return o;
        }

        private static float[] Noise(float seconds, float amp = 1f)
        {
            var d = Buf(seconds);
            for (int i = 0; i < d.Length; i++) d[i] = Rand() * amp;
            return d;
        }

        private static void AddTone(float[] d, float start, float dur, float f0, float f1, float amp, float decay, int wave = 0)
        {
            int s = (int)(start * Rate);
            int n = (int)(dur * Rate);
            float ph = 0f;
            for (int i = 0; i < n && s + i < d.Length; i++)
            {
                if (s + i < 0) continue;
                float t = (float)i / Rate;
                float f = Mathf.Lerp(f0, f1, t / dur);
                ph += TwoPi * f / Rate;
                float v;
                switch (wave)
                {
                    case 1: v = Mathf.Sign(Mathf.Sin(ph)) * 0.6f; break;                 // square
                    case 2: v = ((ph / TwoPi) % 1f) * 2f - 1f; break;                     // saw
                    default: v = Mathf.Sin(ph); break;
                }
                float env = Mathf.Exp(-t * decay) * Mathf.Clamp01(t * 400f);
                d[s + i] += v * amp * env;
            }
        }

        private static void AddNoiseBurst(float[] d, float start, float dur, float amp, float decay)
        {
            int s = (int)(start * Rate);
            int n = (int)(dur * Rate);
            for (int i = 0; i < n && s + i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[s + i] += Rand() * amp * Mathf.Exp(-t * decay);
            }
        }

        // ---------------- footsteps ----------------

        public static AudioClip Footstep(int surface, int variant)
        {
            rng = new System.Random(100 + surface * 10 + variant);
            float len = surface == 4 ? 0.22f : 0.16f;
            var d = Buf(len);
            AddNoiseBurst(d, 0f, len, 1f, surface == 4 ? 18f : 38f);
            switch (surface)
            {
                case 0: LowPass(d, 900f + variant * 120f); AddTone(d, 0f, len, 95f + variant * 8, 70f, 0.8f, 30f); break; // wood
                case 1: LowPass(d, 2200f + variant * 200f); AddTone(d, 0f, len, 70f, 55f, 0.4f, 40f); break;              // concrete
                case 2: LowPass(d, 3000f); AddTone(d, 0f, len, 1150f + variant * 90, 1150f, 0.25f, 35f); AddTone(d, 0f, len, 1870f, 1870f, 0.15f, 45f); break; // metal
                case 3: LowPass(d, 2600f); AddTone(d, 0f, len, 420f, 400f, 0.2f, 60f); break;                              // tile
                default: HighPass(d, 1500f); for (int i = 0; i < d.Length; i++) d[i] *= 0.6f; break;                       // grass
            }
            Normalize(d, 0.8f);
            return ToClip("Footstep_" + surface + "_" + variant, d);
        }

        public static AudioClip RobotStep(int variant)
        {
            rng = new System.Random(300 + variant);
            var d = Buf(0.12f);
            AddNoiseBurst(d, 0f, 0.12f, 0.4f, 70f);
            LowPass(d, 2500f);
            AddTone(d, 0f, 0.12f, 720f + variant * 60f, 700f, 0.5f, 50f);
            AddTone(d, 0f, 0.12f, 1650f, 1600f, 0.25f, 60f);
            Normalize(d, 0.6f);
            return ToClip("RobotStep_" + variant, d);
        }

        // ---------------- horror / tv ----------------

        public static AudioClip Static()
        {
            rng = new System.Random(1);
            var d = Noise(1.2f, 0.5f);
            for (int i = 0; i < 40; i++)
            {
                int p = rng.Next(d.Length - 200);
                for (int j = 0; j < 120; j++) d[p + j] += Rand() * 0.8f;
            }
            HighPass(d, 300f);
            Normalize(d, 0.7f);
            return ToClip("Static", MakeLoop(d, 0.15f));
        }

        public static AudioClip Glitch()
        {
            rng = new System.Random(2);
            var d = Buf(0.8f);
            float t = 0f;
            while (t < 0.78f)
            {
                float seg = 0.02f + Rand01() * 0.05f;
                if (Rand01() < 0.5f) AddNoiseBurst(d, t, seg, 0.8f, 5f);
                else AddTone(d, t, seg, 200f + Rand01() * 1800f, 100f + Rand01() * 2500f, 0.6f, 2f, 1);
                t += seg + Rand01() * 0.03f;
            }
            Normalize(d, 0.8f);
            return ToClip("Glitch", d);
        }

        public static AudioClip Stinger()
        {
            rng = new System.Random(3);
            var d = Buf(2.8f);
            float[] f = { 98f, 103.8f, 146.8f, 207.7f, 311f, 466f };
            for (int k = 0; k < f.Length; k++)
            {
                float ph = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = (float)i / Rate;
                    float vib = 1f + 0.006f * Mathf.Sin(TwoPi * (4f + k) * t);
                    ph += TwoPi * f[k] * vib / Rate;
                    float env = Mathf.Clamp01(t * 20f) * Mathf.Exp(-t * 1.1f);
                    d[i] += Mathf.Sin(ph) * env * (k < 2 ? 0.5f : 0.25f);
                }
            }
            var hit = Noise(2.8f, 1f);
            for (int i = 0; i < hit.Length; i++) hit[i] *= Mathf.Exp(-(float)i / Rate * 6f);
            LowPass(hit, 700f);
            for (int i = 0; i < d.Length; i++) d[i] += hit[i] * 1.2f;
            Normalize(d, 0.9f);
            return ToClip("Stinger", d);
        }

        public static AudioClip Laugh()
        {
            rng = new System.Random(4);
            var d = Buf(2.6f);
            for (int k = 0; k < 7; k++)
            {
                float start = 0.1f + k * 0.3f;
                float f = 170f - k * 9f;
                AddTone(d, start, 0.22f, f * 1.1f, f, 0.8f, 7f, 2);
                AddNoiseBurst(d, start, 0.12f, 0.3f, 20f);
            }
            LowPass(d, 1100f);
            Normalize(d, 0.85f);
            return ToClip("Laugh", d);
        }

        public static AudioClip Heartbeat()
        {
            var d = Buf(0.9f);
            AddTone(d, 0f, 0.3f, 62f, 45f, 1f, 13f);
            AddTone(d, 0.26f, 0.3f, 58f, 42f, 0.7f, 14f);
            LowPass(d, 300f);
            Normalize(d, 0.9f);
            return ToClip("Heartbeat", d);
        }

        public static AudioClip Tick()
        {
            rng = new System.Random(5);
            var d = Buf(0.08f);
            AddTone(d, 0f, 0.08f, 2600f, 2500f, 0.6f, 80f);
            AddNoiseBurst(d, 0f, 0.02f, 0.5f, 200f);
            Normalize(d, 0.7f);
            return ToClip("Tick", d);
        }

        public static AudioClip Warning()
        {
            var d = Buf(0.9f);
            AddTone(d, 0f, 0.2f, 880f, 880f, 0.5f, 3f, 1);
            AddTone(d, 0.22f, 0.2f, 660f, 660f, 0.5f, 3f, 1);
            AddTone(d, 0.44f, 0.2f, 880f, 880f, 0.5f, 3f, 1);
            AddTone(d, 0.66f, 0.2f, 660f, 660f, 0.5f, 3f, 1);
            LowPass(d, 3000f);
            Normalize(d, 0.6f);
            return ToClip("Warning", d);
        }

        // ---------------- interface / puzzles ----------------

        public static AudioClip Beep(float freq, float len, string name)
        {
            var d = Buf(len);
            AddTone(d, 0f, len, freq, freq, 0.6f, 6f);
            Normalize(d, 0.5f);
            return ToClip(name, d);
        }

        public static AudioClip Error()
        {
            var d = Buf(0.45f);
            AddTone(d, 0f, 0.42f, 150f, 140f, 0.5f, 2f, 1);
            AddTone(d, 0f, 0.42f, 156f, 146f, 0.5f, 2f, 1);
            LowPass(d, 2000f);
            Normalize(d, 0.6f);
            return ToClip("Error", d);
        }

        public static AudioClip Success()
        {
            var d = Buf(0.9f);
            AddTone(d, 0f, 0.6f, 784f, 784f, 0.5f, 5f);
            AddTone(d, 0.12f, 0.6f, 988f, 988f, 0.5f, 5f);
            AddTone(d, 0.24f, 0.6f, 1319f, 1319f, 0.5f, 4f);
            Normalize(d, 0.6f);
            return ToClip("Success", d);
        }

        public static AudioClip Unlock()
        {
            rng = new System.Random(6);
            var d = Buf(0.8f);
            AddNoiseBurst(d, 0f, 0.2f, 0.9f, 25f);
            AddNoiseBurst(d, 0.28f, 0.3f, 1f, 18f);
            LowPass(d, 700f);
            AddTone(d, 0.28f, 0.5f, 420f, 410f, 0.35f, 8f);
            AddTone(d, 0.28f, 0.5f, 913f, 900f, 0.15f, 10f);
            Normalize(d, 0.8f);
            return ToClip("Unlock", d);
        }

        public static AudioClip Drawer()
        {
            rng = new System.Random(7);
            var d = Noise(0.55f, 1f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] *= Mathf.Sin(Mathf.PI * t / 0.55f) * (0.6f + 0.4f * Mathf.Sin(TwoPi * 28f * t));
            }
            LowPass(d, 1300f);
            AddTone(d, 0.5f, 0.05f, 300f, 200f, 0.4f, 60f);
            Normalize(d, 0.6f);
            return ToClip("Drawer", d);
        }

        public static AudioClip Creak()
        {
            rng = new System.Random(8);
            var d = Buf(1.6f);
            float ph = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float f = 260f + 140f * (t / 1.6f) + 25f * Mathf.Sin(TwoPi * 7f * t) + Rand() * 10f;
                ph += TwoPi * f / Rate;
                float am = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(TwoPi * 18f * t));
                d[i] = (((ph / TwoPi) % 1f) * 2f - 1f) * am * Mathf.Sin(Mathf.PI * t / 1.6f);
            }
            LowPass(d, 1500f);
            Normalize(d, 0.45f);
            return ToClip("Creak", d);
        }

        public static AudioClip Rattle()
        {
            rng = new System.Random(9);
            var d = Buf(0.55f);
            for (int k = 0; k < 5; k++) AddNoiseBurst(d, k * 0.1f, 0.08f, 1f, 50f);
            LowPass(d, 1600f);
            AddTone(d, 0f, 0.5f, 640f, 600f, 0.15f, 8f);
            Normalize(d, 0.6f);
            return ToClip("Rattle", d);
        }

        public static AudioClip Click()
        {
            rng = new System.Random(10);
            var d = Buf(0.12f);
            AddNoiseBurst(d, 0f, 0.03f, 0.8f, 150f);
            AddTone(d, 0f, 0.1f, 950f, 800f, 0.5f, 50f);
            Normalize(d, 0.6f);
            return ToClip("Click", d);
        }

        public static AudioClip Lever()
        {
            rng = new System.Random(11);
            var d = Noise(0.7f, 0.5f);
            for (int i = 0; i < d.Length; i++) d[i] *= Mathf.Clamp01(1f - (float)i / d.Length * 1.4f);
            LowPass(d, 500f);
            AddNoiseBurst(d, 0.45f, 0.2f, 1f, 30f);
            AddTone(d, 0.45f, 0.25f, 180f, 120f, 0.8f, 15f);
            Normalize(d, 0.8f);
            return ToClip("Lever", d);
        }

        public static AudioClip Whoosh()
        {
            rng = new System.Random(12);
            var d = Noise(0.7f, 1f);
            for (int i = 0; i < d.Length; i++) d[i] *= Mathf.Pow(Mathf.Sin(Mathf.PI * i / d.Length), 2f);
            LowPass(d, 900f);
            HighPass(d, 150f);
            Normalize(d, 0.7f);
            return ToClip("Whoosh", d);
        }

        public static AudioClip Impact(float pitch, string name)
        {
            rng = new System.Random(13);
            var d = Buf(1f);
            AddTone(d, 0f, 1f, 70f * pitch, 40f * pitch, 1f, 5f);
            AddNoiseBurst(d, 0f, 0.6f, 0.9f, 12f);
            LowPass(d, 900f * pitch);
            AddTone(d, 0f, 0.8f, 520f * pitch, 510f * pitch, 0.2f, 6f);
            Normalize(d, 0.9f);
            return ToClip(name, d);
        }

        public static AudioClip LaserHum()
        {
            var d = Buf(1f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] = Mathf.Sin(TwoPi * 120f * t) * 0.5f + Mathf.Sin(TwoPi * 240f * t) * 0.25f + Mathf.Sin(TwoPi * 362f * t) * 0.1f;
            }
            Normalize(d, 0.5f);
            return ToClip("LaserHum", d);
        }

        public static AudioClip Zap()
        {
            rng = new System.Random(14);
            var d = Buf(0.5f);
            AddTone(d, 0f, 0.5f, 1500f, 250f, 0.7f, 6f, 2);
            AddNoiseBurst(d, 0f, 0.4f, 0.6f, 10f);
            LowPass(d, 4000f);
            Normalize(d, 0.8f);
            return ToClip("Zap", d);
        }

        public static AudioClip LavaLoop()
        {
            rng = new System.Random(15);
            var d = Noise(4.4f, 0.6f);
            LowPass(d, 120f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] += Mathf.Sin(TwoPi * 40f * t) * 0.25f * (0.7f + 0.3f * Mathf.Sin(TwoPi * 0.5f * t));
            }
            for (int k = 0; k < 26; k++)
            {
                float s = Rand01() * 4.2f;
                float f = 150f + Rand01() * 250f;
                AddTone(d, s, 0.07f, f, f * 2.5f, 0.35f, 30f);
            }
            Normalize(d, 0.8f);
            return ToClip("Lava", MakeLoop(d, 0.4f));
        }

        public static AudioClip Grind()
        {
            rng = new System.Random(16);
            var d = Noise(1.5f, 1f);
            LowPass(d, 380f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] = d[i] * (0.6f + 0.4f * Mathf.Sin(TwoPi * 14f * t)) + (((55f * t) % 1f) * 2f - 1f) * 0.15f;
            }
            Normalize(d, 0.8f);
            return ToClip("Grind", MakeLoop(d, 0.25f));
        }

        public static AudioClip Fall()
        {
            rng = new System.Random(17);
            var d = Buf(1.1f);
            AddTone(d, 0f, 1.1f, 900f, 110f, 0.5f, 1.2f);
            var n = Noise(1.1f, 0.6f);
            LowPass(n, 1200f);
            for (int i = 0; i < d.Length; i++) d[i] += n[i] * Mathf.Sin(Mathf.PI * i / d.Length);
            Normalize(d, 0.8f);
            return ToClip("Fall", d);
        }

        public static AudioClip Burn()
        {
            rng = new System.Random(18);
            var d = Noise(1.4f, 1f);
            HighPass(d, 1800f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] *= Mathf.Exp(-t * 2f) * (0.5f + 0.5f * Mathf.Abs(Mathf.Sin(TwoPi * 23f * t)));
            }
            AddTone(d, 0f, 0.8f, 80f, 50f, 0.6f, 4f);
            Normalize(d, 0.8f);
            return ToClip("Burn", d);
        }

        public static AudioClip Clank()
        {
            var d = Buf(1.8f);
            AddTone(d, 0f, 1.8f, 523f, 520f, 0.5f, 3f);
            AddTone(d, 0f, 1.8f, 1187f, 1180f, 0.35f, 4f);
            AddTone(d, 0f, 1.8f, 1789f, 1780f, 0.25f, 6f);
            AddTone(d, 0f, 1.8f, 2489f, 2480f, 0.15f, 8f);
            rng = new System.Random(19);
            AddNoiseBurst(d, 0f, 0.05f, 0.8f, 80f);
            Normalize(d, 0.6f);
            return ToClip("Clank", d);
        }

        public static AudioClip PowerUp()
        {
            var d = Buf(1.6f);
            AddTone(d, 0f, 1.6f, 70f, 380f, 0.6f, 0.6f, 2);
            AddTone(d, 0f, 1.6f, 140f, 760f, 0.3f, 0.8f);
            LowPass(d, 2500f);
            Normalize(d, 0.7f);
            return ToClip("PowerUp", d);
        }

        public static AudioClip Victory()
        {
            var d = Buf(3.2f);
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int k = 0; k < notes.Length; k++) AddTone(d, k * 0.18f, 3f - k * 0.18f, notes[k], notes[k], 0.45f, 1.1f);
            AddTone(d, 0f, 3.2f, 261.6f, 261.6f, 0.3f, 0.7f);
            AddTone(d, 0f, 3.2f, 392f, 392f, 0.2f, 0.7f);
            Normalize(d, 0.8f);
            return ToClip("Victory", d);
        }

        public static AudioClip RobotChirp()
        {
            var d = Buf(0.3f);
            AddTone(d, 0f, 0.08f, 1250f, 1400f, 0.5f, 10f);
            AddTone(d, 0.11f, 0.11f, 1600f, 1900f, 0.5f, 10f);
            Normalize(d, 0.5f);
            return ToClip("RobotChirp", d);
        }

        public static AudioClip Servo()
        {
            var d = Buf(0.4f);
            float ph = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                ph += TwoPi * Mathf.Lerp(380f, 540f, t / 0.4f) / Rate;
                d[i] = Mathf.Sin(ph) * (0.6f + 0.4f * Mathf.Sin(TwoPi * 60f * t)) * Mathf.Sin(Mathf.PI * t / 0.4f);
            }
            Normalize(d, 0.35f);
            return ToClip("Servo", d);
        }

        public static AudioClip Paper()
        {
            rng = new System.Random(20);
            var d = Noise(0.4f, 1f);
            HighPass(d, 2500f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] *= Mathf.Sin(Mathf.PI * t / 0.4f) * (Rand01() < 0.1f ? 1.5f : 0.6f);
            }
            Normalize(d, 0.45f);
            return ToClip("Paper", d);
        }

        // ---------------- loops (music / ambience) ----------------

        public static AudioClip DroneLoop()
        {
            rng = new System.Random(21);
            var d = Buf(8.5f);
            float[] f = { 55f, 55.75f, 82.5f, 110.25f, 164.75f };
            float[] a = { 0.5f, 0.4f, 0.3f, 0.2f, 0.08f };
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float lfo = 0.75f + 0.25f * Mathf.Sin(TwoPi * 0.25f * t);
                float v = 0f;
                for (int k = 0; k < f.Length; k++) v += Mathf.Sin(TwoPi * f[k] * t) * a[k];
                d[i] = v * lfo;
            }
            var n = Noise(8.5f, 0.25f);
            LowPass(n, 200f);
            for (int i = 0; i < d.Length; i++) d[i] += n[i];
            Normalize(d, 0.7f);
            return ToClip("Drone", MakeLoop(d, 0.5f));
        }

        public static AudioClip TensionLoop()
        {
            var d = Buf(4.3f);
            for (int b = 0; b < 9; b++)
            {
                AddTone(d, b * 0.5f, 0.45f, 70f, 50f, 0.8f, 8f);
            }
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] += Mathf.Sin(TwoPi * 660f * t) * 0.06f * (0.5f + 0.5f * Mathf.Sin(TwoPi * 0.5f * t));
                d[i] += Mathf.Sin(TwoPi * 698.5f * t) * 0.04f;
            }
            Normalize(d, 0.7f);
            return ToClip("Tension", MakeLoop(d, 0.3f));
        }

        public static AudioClip WindLoop()
        {
            rng = new System.Random(22);
            var d = Noise(6.5f, 1f);
            LowPass(d, 420f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] *= 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(TwoPi * 0.16f * t));
            }
            Normalize(d, 0.6f);
            return ToClip("Wind", MakeLoop(d, 0.5f));
        }

        public static AudioClip GardenLoop()
        {
            rng = new System.Random(23);
            var d = Noise(6.5f, 0.4f);
            LowPass(d, 600f);
            for (int k = 0; k < 18; k++)
            {
                float s = Rand01() * 6f;
                float f = 2200f + Rand01() * 1600f;
                AddTone(d, s, 0.09f, f, f * 1.25f, 0.35f, 15f);
                AddTone(d, s + 0.12f, 0.09f, f * 1.1f, f * 0.9f, 0.3f, 15f);
            }
            Normalize(d, 0.6f);
            return ToClip("Garden", MakeLoop(d, 0.5f));
        }
    }
}
