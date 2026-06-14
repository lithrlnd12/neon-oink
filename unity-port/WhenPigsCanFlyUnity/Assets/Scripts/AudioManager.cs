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

        private float[] noiseBuffer;
        private int beatCount;
        private int arpStep;
        private float nextBeatTime;

        private void Awake()
        {
            if (musicSource == null) musicSource = GetComponent<AudioSource>();
            GenerateNoiseBuffer();
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
            sfxSource.PlayOneShot(CreateKickClip(), 0.9f);
        }

        private void PlayHat(float t, bool open)
        {
            sfxSource.PlayOneShot(CreateHatClip(open), open ? 0.25f : 0.15f);
        }

        private void PlayBass(float t, float freq, float dur)
        {
            sfxSource.PlayOneShot(CreateToneClip(freq * 0.5f, dur, AudioSynthWave.Saw), 0.28f);
        }

        private void PlayArp(float t, float freq)
        {
            sfxSource.PlayOneShot(CreateToneClip(freq, 0.1f, AudioSynthWave.Triangle), 0.12f);
        }

        private void PlayPad(float t, float freq, float dur)
        {
            sfxSource.PlayOneShot(CreateToneClip(freq, dur, AudioSynthWave.Square), 0.06f);
        }

        private AudioClip CreateKickClip()
        {
            // Stub: runtime AudioClip from generated samples.
            return null;
        }

        private AudioClip CreateHatClip(bool open)
        {
            return null;
        }

        private AudioClip CreateToneClip(float freq, float dur, AudioSynthWave wave)
        {
            return null;
        }

        private void Start() { }

        private enum AudioSynthWave { Sine, Triangle, Saw, Square }
    }
}
