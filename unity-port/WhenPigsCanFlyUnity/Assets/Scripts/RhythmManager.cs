using UnityEngine;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Keeps the beat, exposes BPM-scaled timing, and tracks the on-beat chain multiplier.
    /// </summary>
    public class RhythmManager : MonoBehaviour
    {
        [Header("Base BPM")]
        [SerializeField] private int baseBpm = 90;
        [SerializeField] private int bpmPerLevel = 8;

        [Header("Beat Window")]
        [SerializeField] private float normalBeatWindow = 0.09f;
        [SerializeField] private float kidsBeatWindow = 0.22f;

        [Header("Multiplier Thresholds")]
        [SerializeField] private int multiplier2x = 3;
        [SerializeField] private int multiplier3x = 5;
        [SerializeField] private int multiplier4x = 8;

        public int Level { get; set; } = 1;
        public bool KidsMode { get; set; }
        public int BeatChain { get; private set; }
        public int BestChain { get; private set; }
        public int Multiplier { get; private set; } = 1;
        public float BeatInterval { get; private set; }
        public float BeatPhase { get; private set; }

        private float beatRef;
        private float nextBeat;
        private float accumulatedTime;

        public int Bpm => baseBpm + (Level - 1) * bpmPerLevel;

        private void Start()
        {
            ResetRhythm();
        }

        /// <summary>
        /// Resets beat tracking for a new run.
        /// </summary>
        public void ResetRhythm()
        {
            BeatChain = 0;
            BestChain = 0;
            Multiplier = 1;
            accumulatedTime = 0f;
            beatRef = 0f;
            nextBeat = 0f;
            RecalculateInterval();
        }

        private void RecalculateInterval()
        {
            BeatInterval = 60f / Bpm;
        }

        /// <summary>
        /// Advances the metronome. Call from the main game loop.
        /// </summary>
        public void Tick(float dt)
        {
            RecalculateInterval();
            accumulatedTime += dt;
            if (accumulatedTime >= nextBeat)
            {
                beatRef = nextBeat;
                nextBeat += BeatInterval;
                OnBeat();
            }
            BeatPhase = (accumulatedTime - beatRef) / BeatInterval;
        }

        private void OnBeat()
        {
            // Hook for AudioManager to play kick/hat/etc.
        }

        /// <summary>
        /// Returns true if the current time is within the on-beat window.
        /// </summary>
        public bool CheckBeat()
        {
            float window = KidsMode ? kidsBeatWindow : normalBeatWindow;
            float phase = (accumulatedTime - beatRef) % BeatInterval;
            return phase < window || BeatInterval - phase < window;
        }

        /// <summary>
        /// Registers a flap attempt; returns whether it was on-beat and updates the chain.
        /// </summary>
        public bool RegisterFlap()
        {
            if (CheckBeat())
            {
                BeatChain++;
                BestChain = Mathf.Max(BestChain, BeatChain);
                Multiplier = BeatChain >= multiplier4x ? 4 : BeatChain >= multiplier3x ? 3 : BeatChain >= multiplier2x ? 2 : 1;
                return true;
            }

            BeatChain = 0;
            Multiplier = 1;
            return false;
        }

        private void Update() { }
    }
}
