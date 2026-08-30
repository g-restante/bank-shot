using UnityEngine;

namespace BankShot
{
    /// <summary>
    /// Punto unico per i suoni. Carica gli asset (Kenney, CC0) da Resources/Sfx;
    /// se una cartella è vuota ripiega su un suono procedurale, così il gioco
    /// suona sempre anche senza asset. Le varianti vengono pescate a caso.
    /// </summary>
    public static class Sfx
    {
        const int SampleRate = 44100;

        static AudioClip[] shootClips;
        static AudioClip[] bounceClips;
        static AudioClip[] hitClips;
        static AudioClip whistleClip;

        /// <summary>Sparo (variante casuale).</summary>
        public static AudioClip Shoot => Pick(shootClips ??= LoadOrFallback("Sfx/Shoot", ProceduralShoot));

        /// <summary>Impatto metallico di rimbalzo (variante casuale, pitch a cura del chiamante).</summary>
        public static AudioClip Bounce => Pick(bounceClips ??= LoadOrFallback("Sfx/Bounce", ProceduralBounce));

        /// <summary>Dink di conferma colpo.</summary>
        public static AudioClip Hit => Pick(hitClips ??= LoadOrFallback("Sfx/Hit", ProceduralHit));

        /// <summary>Fischio in loop del proiettile in volo.</summary>
        public static AudioClip WhistleLoop
        {
            get
            {
                if (whistleClip == null)
                {
                    whistleClip = Resources.Load<AudioClip>("Sfx/Whistle/loop");
                    if (whistleClip == null)
                        whistleClip = ProceduralWhistle();
                }
                return whistleClip;
            }
        }

        static AudioClip[] LoadOrFallback(string path, System.Func<AudioClip> fallback)
        {
            var clips = Resources.LoadAll<AudioClip>(path);
            return clips is { Length: > 0 } ? clips : new[] { fallback() };
        }

        static AudioClip Pick(AudioClip[] clips) => clips[Random.Range(0, clips.Length)];

        // ---- Riproduzione ----

        /// <summary>Suono 3D one-shot a una posizione, con pitch e volume.</summary>
        public static void PlayAt(AudioClip clip, Vector3 position, float pitch = 1f, float volume = 1f)
        {
            var go = new GameObject("Sfx");
            go.transform.position = position;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.pitch = pitch;
            src.volume = volume;
            src.spatialBlend = 1f;
            src.dopplerLevel = 0f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.maxDistance = 60f;
            src.Play();
            Object.Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }

        /// <summary>Suono 2D one-shot (feedback per il giocatore locale).</summary>
        public static void Play2D(AudioClip clip, float pitch = 1f, float volume = 1f)
        {
            var go = new GameObject("Sfx2D");
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.pitch = pitch;
            src.volume = volume;
            src.spatialBlend = 0f;
            src.Play();
            Object.Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }

        // ---- Fallback procedurali (usati solo se mancano gli asset) ----

        static AudioClip ProceduralShoot() => Generate("sfx_shoot", 0.14f, (t, rng) =>
        {
            float env = Mathf.Exp(-t * 28f);
            float f = Mathf.Lerp(240f, 60f, Mathf.Clamp01(t * 10f));
            float body = Mathf.Sin(2f * Mathf.PI * f * t);
            float crack = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 90f) * 0.6f;
            return (body * 0.8f + crack) * env;
        });

        static AudioClip ProceduralBounce() => Generate("sfx_bounce", 0.09f, (t, rng) =>
        {
            float env = Mathf.Exp(-t * 45f);
            return (Mathf.Sin(2f * Mathf.PI * 650f * t)
                  + Mathf.Sin(2f * Mathf.PI * 1300f * t) * 0.35f) * env * 0.8f;
        });

        static AudioClip ProceduralHit() => Generate("sfx_hit", 0.12f, (t, rng) =>
        {
            float env = Mathf.Exp(-t * 30f);
            return (Mathf.Sin(2f * Mathf.PI * 1150f * t)
                  + Mathf.Sin(2f * Mathf.PI * 1725f * t) * 0.4f) * env * 0.7f;
        });

        /// <summary>Fruscio d'aria tagliata: rumore filtrato in banda media, loop senza click.</summary>
        static AudioClip ProceduralWhistle()
        {
            const float duration = 1f;
            int count = Mathf.RoundToInt(SampleRate * duration);
            var data = new float[count];
            var rng = new System.Random(4242);
            float lp = 0f, bp = 0f;
            for (int i = 0; i < count; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.12f * (white - lp);
                bp += 0.03f * (lp - bp);
                data[i] = Mathf.Clamp((lp - bp) * 2.5f, -1f, 1f); // banda media: "aria"
            }

            // Crossfade coda→testa e taglio della coda: il loop non clicca
            int fade = SampleRate / 10;
            for (int i = 0; i < fade; i++)
            {
                float a = i / (float)fade;
                data[i] = data[i] * a + data[count - fade + i] * (1f - a);
            }
            int loopCount = count - fade;
            var clip = AudioClip.Create("sfx_whistle", loopCount, 1, SampleRate, stream: false);
            var loopData = new float[loopCount];
            System.Array.Copy(data, loopData, loopCount);
            clip.SetData(loopData, 0);
            return clip;
        }

        static AudioClip Generate(string name, float duration, System.Func<float, System.Random, float> sample)
        {
            int count = Mathf.RoundToInt(SampleRate * duration);
            var data = new float[count];
            var rng = new System.Random(12345); // deterministico
            for (int i = 0; i < count; i++)
                data[i] = Mathf.Clamp(sample(i / (float)SampleRate, rng), -1f, 1f);

            var clip = AudioClip.Create(name, count, 1, SampleRate, stream: false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
