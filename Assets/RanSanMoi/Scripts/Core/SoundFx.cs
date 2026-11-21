using System.Collections.Generic;
using UnityEngine;

namespace MiniGames
{
    public enum Sfx { Shoot, Explode, Coin, Hurt, Jump, PowerUp, Point, Bomb, GameOver, Win, Click, Splat }

    /// <summary>
    /// Âm thanh 8-bit được "tổng hợp" bằng code (sóng vuông, sóng sin, nhiễu trắng),
    /// nên không cần file .wav nào. Gọi SoundFx.Play(Sfx.Coin) ở bất kỳ đâu.
    /// </summary>
    public static class SoundFx
    {
        const int Rate = 44100;
        static AudioSource source;
        static readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        public static float Volume = 0.35f;

        public static void Play(Sfx s, float volume = 1f)
        {
            if (source == null)
            {
                var go = new GameObject("SoundFx");
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
            }
            if (!clips.TryGetValue(s, out var clip) || clip == null)
            {
                clip = Build(s);
                clips[s] = clip;
            }
            source.PlayOneShot(clip, Volume * volume);
        }

        static AudioClip Build(Sfx s)
        {
            switch (s)
            {
                case Sfx.Shoot: return Sweep("shoot", 0.09f, 880f, 330f, Wave.Square, 0.5f);
                case Sfx.Explode: return Noise("explode", 0.35f, 1f);
                case Sfx.Bomb: return Noise("bomb", 0.6f, 1f);
                case Sfx.Coin: return Notes("coin", new[] { 988f, 1319f }, 0.07f, Wave.Square);
                case Sfx.Hurt: return Sweep("hurt", 0.28f, 420f, 90f, Wave.Square, 0.7f);
                case Sfx.Jump: return Sweep("jump", 0.14f, 280f, 720f, Wave.Square, 0.45f);
                case Sfx.PowerUp: return Notes("powerup", new[] { 523f, 659f, 784f, 1047f }, 0.06f, Wave.Square);
                case Sfx.Point: return Notes("point", new[] { 784f, 1175f }, 0.05f, Wave.Sine);
                case Sfx.GameOver: return Notes("gameover", new[] { 523f, 415f, 349f, 262f }, 0.16f, Wave.Square);
                case Sfx.Win: return Notes("win", new[] { 523f, 659f, 784f, 659f, 784f, 1047f }, 0.1f, Wave.Square);
                case Sfx.Click: return Notes("click", new[] { 660f }, 0.04f, Wave.Sine);
                case Sfx.Splat: return Sweep("splat", 0.12f, 200f, 60f, Wave.Sine, 0.8f);
            }
            return Notes("none", new[] { 440f }, 0.05f, Wave.Sine);
        }

        enum Wave { Square, Sine }

        static float Osc(Wave w, float phase)
        {
            return w == Wave.Square ? (Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f) : Mathf.Sin(phase * 2f * Mathf.PI);
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Sweep(string name, float dur, float f0, float f1, Wave w, float gain)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += f / Rate;
                d[i] = Osc(w, phase) * gain * (1f - t) * 0.5f;
            }
            return Make(name, d);
        }

        static AudioClip Notes(string name, float[] freqs, float noteDur, Wave w)
        {
            int per = (int)(noteDur * Rate);
            var d = new float[per * freqs.Length];
            float phase = 0f;
            for (int k = 0; k < freqs.Length; k++)
                for (int i = 0; i < per; i++)
                {
                    float t = (float)i / per;
                    phase += freqs[k] / Rate;
                    d[k * per + i] = Osc(w, phase) * 0.3f * (1f - t * 0.6f);
                }
            return Make(name, d);
        }

        static AudioClip Noise(string name, float dur, float gain)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            var rng = new System.Random(1234);
            float last = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                last = Mathf.Lerp(last, white, 0.35f); // lọc bớt cho tiếng "bùm" trầm hơn
                d[i] = last * gain * (1f - t) * (1f - t) * 0.6f;
            }
            return Make(name, d);
        }
    }
}
