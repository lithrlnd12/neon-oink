using UnityEngine;
using UnityEngine.Audio;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Procedural/generated audio equivalent: synthesizes kick, hat, bass, arp, pad, and SFX at runtime.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Synth Settings")]
        [SerializeField] private int sampleRate = 44100;
        [SerializeField] private float masterVolume = 0.5f;

        [Header("Pentatonic Scale")]
        [SerializeField] private float[] pentatonic = { 130.81f, 155.56f, 174.61f, 196f, 233.08f };

        public int Level { get; set; } = 1;
        public bool Playing { get; set; }
        public static AudioManager Instance { get; private set; }

        private float[] noiseBuffer;
        private int beatCount;
        private int arpStep;
        private float nextBeatTime;

        private void Awake()
        {
            Instance = this;
            if (musicSource == null) musicSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            GenerateNoiseBuffer();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Plays a synthesized flap sound.
        /// </summary>
        public void PlayFlap()
        {
            sfxSource?.PlayOneShot(CreateToneClip(250f, 0.12f, AudioSynthWave.Sine), 0.4f);
        }

        /// <summary>
        /// Plays a ring collection sound.
        /// </summary>
        public void PlayRing()
        {
            sfxSource?.PlayOneShot(CreateToneClip(880f, 0.15f, AudioSynthWave.Triangle), 0.35f);
        }

        /// <summary>
        /// Plays a damage/hurt sound.
        /// </summary>
        public void PlayHurt()
        {
            sfxSource?.PlayOneShot(CreateToneClip(110f, 0.25f, AudioSynthWave.Saw), 0.5f);
        }

        /// <summary>
        /// Plays a laser shot sound.
        /// </summary>
        public void PlayLaser()
        {
            sfxSource?.PlayOneShot(CreateToneClip(1200f, 0.08f, AudioSynthWave.Square), 0.25f);
        }

        /// <summary>
        /// Plays a poop bomb sound.
        /// </summary>
        public void PlayPoop()
        {
            sfxSource?.PlayOneShot(CreateToneClip(90f, 0.2f, AudioSynthWave.Saw), 0.4f);
        }

        /// <summary>
        /// Generates a short noise buffer for hats and splats.
        /// </summary>
        private void GenerateNoiseBuffer()
        {
            int length = Mathf.RoundToInt(sampleRate * 0.3f);
            noiseBuffer = new float[length];
            for (int i = 0; i < length; i++)
                noiseBuffer[i] = Random.value * 2f - 1f;
        }

        private void Update()
        {
            if (!Playing) return;
            float bpm = 90f + (Level - 1) * 8f;
            float interval = 60f / bpm;
            double time = AudioSettings.dspTime;

            while (nextBeatTime < time + 0.12f)
            {
                ScheduleBeat(nextBeatTime, interval);
                nextBeatTime += interval;
                beatCount++;
            }
        }

        /// <summary>
        /// Schedules one bar of procedural drums and synths.
        /// </summary>
        private void ScheduleBeat(float t, float interval)
        {
            PlayKick(t);
            PlayHat(t + interval * 0.5f, false);

            if (Level >= 4)
            {
                PlayHat(t + interval * 0.25f, false);
                PlayHat(t + interval * 0.75f, beatCount % 2 == 0);
            }

            if (Level >= 2)
            {
                int heightIndex = Mathf.Clamp(Mathf.FloorToInt(5f * 0f /* player height / ceiling */), 0, 4);
                PlayBass(t, pentatonic[heightIndex], interval * 0.9f);
            }

            if (Level >= 3)
            {
                for (int s = 0; s < 4; s++)
                    PlayArp(t + interval * s / 4f, pentatonic[(arpStep + s) % 5] * 4f);
                arpStep = (arpStep + 1) % 5;
            }

            if (Level >= 5 && beatCount % 4 == 0)
                PlayBass(t, pentatonic[0] * 0.5f, interval * 3.5f);

            if (Level >= 7 && beatCount % 2 == 0)
                PlayPad(t, pentatonic[(beatCount / 2) % 5] * 2f, interval * 1.8f);
        }

        private void PlayKick(float t)
        {
            if (sfxSource != null && CreateKickClip() != null)
                sfxSource.PlayOneShot(CreateKickClip(), 0.9f);
        }

        private void PlayHat(float t, bool open)
        {
            if (sfxSource != null && CreateHatClip(open) != null)
                sfxSource.PlayOneShot(CreateHatClip(open), open ? 0.25f : 0.15f);
        }

        private void PlayBass(float t, float freq, float dur)
        {
            if (sfxSource != null && CreateToneClip(freq * 0.5f, dur, AudioSynthWave.Saw) != null)
                sfxSource.PlayOneShot(CreateToneClip(freq * 0.5f, dur, AudioSynthWave.Saw), 0.28f);
        }

        private void PlayArp(float t, float freq)
        {
            if (sfxSource != null && CreateToneClip(freq, 0.1f, AudioSynthWave.Triangle) != null)
                sfxSource.PlayOneShot(CreateToneClip(freq, 0.1f, AudioSynthWave.Triangle), 0.12f);
        }

        private void PlayPad(float t, float freq, float dur)
        {
            if (sfxSource != null && CreateToneClip(freq, dur, AudioSynthWave.Square) != null)
                sfxSource.PlayOneShot(CreateToneClip(freq, dur, AudioSynthWave.Square), 0.06f);
        }

        private AudioClip CreateKickClip()
        {
            int length = Mathf.RoundToInt(sampleRate * 0.15f);
            float[] samples = new float[length];
            float phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float freq = 120f * Mathf.Exp(-t * 25f);
                phase += freq * 2f * Mathf.PI / sampleRate;
                float env = Mathf.Exp(-t * 12f);
                samples[i] = Mathf.Sin(phase) * env;
            }
            AudioClip clip = AudioClip.Create("Kick", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateHatClip(bool open)
        {
            int length = Mathf.RoundToInt(sampleRate * (open ? 0.2f : 0.05f));
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-t * (open ? 20f : 60f));
                samples[i] = (Random.value * 2f - 1f) * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create("Hat", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateToneClip(float freq, float dur, AudioSynthWave wave)
        {
            int length = Mathf.RoundToInt(sampleRate * dur);
            if (length <= 0) return null;
            float[] samples = new float[length];
            float phase = 0f;
            float inc = freq * 2f * Mathf.PI / sampleRate;
            for (int i = 0; i < length; i++)
            {
                phase += inc;
                float env = 1f - i / (float)length;
                float sample = wave switch
                {
                    AudioSynthWave.Sine => Mathf.Sin(phase),
                    AudioSynthWave.Triangle => Mathf.PingPong(phase / Mathf.PI, 1f) * 2f - 1f,
                    AudioSynthWave.Saw => (phase % (2f * Mathf.PI)) / (2f * Mathf.PI) * 2f - 1f,
                    AudioSynthWave.Square => Mathf.Sin(phase) >= 0f ? 1f : -1f,
                    _ => Mathf.Sin(phase)
                };
                samples[i] = sample * env * masterVolume;
            }
            AudioClip clip = AudioClip.Create($"Tone_{freq}_{wave}", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void Start() { }

        private enum AudioSynthWave { Sine, Triangle, Saw, Square }
    }
}
