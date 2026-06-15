using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Central game state machine: start, die, restart, score, level progression, and run codes.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Level Thresholds")]
        [SerializeField] private int[] levelThresholds = { 0, 30, 70, 125, 195, 285, 400, 540, 710, 920 };
        [SerializeField] private int maxLevel = 10;

        [Header("Run Code")]
        [SerializeField] private string codePrefix = "PIG-";

        [Header("References")]
        [SerializeField] private PigController player;
        [SerializeField] private WorldGenerator worldGenerator;
        [SerializeField] private RhythmManager rhythmManager;
        [SerializeField] private DimensionFlipper dimensionFlipper;
        [SerializeField] private PigBrawlManager brawlManager;

        public bool Playing { get; private set; }
        public bool Dead { get; private set; }
        public int Score { get; private set; }
        public int RingsHit { get; private set; }
        public int Level { get; private set; } = 1;
        public int BestScore { get; private set; }
        public int BestChain { get; private set; }
        public float TotalDistance { get; private set; }
        public float DistanceAccumulator { get; private set; }
        public int RunSeed { get; set; }

        public delegate void GameEvent();
        public event GameEvent OnGameStarted;
        public event GameEvent OnGameEnded;
        public event GameEvent OnLevelUp;

        private void Awake()
        {
            RunSeed = Random.Range(0, 0xFFFFFF);

            if (player == null) player = FindFirstObjectByType<PigController>();
            if (worldGenerator == null) worldGenerator = FindFirstObjectByType<WorldGenerator>();
            if (rhythmManager == null) rhythmManager = FindFirstObjectByType<RhythmManager>();
            if (dimensionFlipper == null) dimensionFlipper = FindFirstObjectByType<DimensionFlipper>();
            if (brawlManager == null) brawlManager = FindFirstObjectByType<PigBrawlManager>();

            var save = FindFirstObjectByType<SaveManager>();
            if (save != null)
            {
                BestScore = save.LoadLeaderboard().FirstOrDefault()?.score ?? 0;
            }
        }

        /// <summary>
        /// Starts a new endless flight run.
        /// </summary>
        public void StartGame()
        {
            Playing = true;
            Dead = false;
            Score = 0;
            RingsHit = 0;
            Level = 1;
            TotalDistance = 0f;
            DistanceAccumulator = 0f;
            player?.ResetPig(new Vector3(0f, 9f, 0f));
            rhythmManager?.ResetRhythm();
            dimensionFlipper?.Force3D();
            worldGenerator?.ClearWorld();
            if (worldGenerator != null) worldGenerator.RunSeed = RunSeed;
            OnGameStarted?.Invoke();
        }

        /// <summary>
        /// Ends the current run and prepares the result screen.
        /// </summary>
        public void Die()
        {
            if (Dead || !Playing) return;
            Dead = true;
            Playing = false;
            BestScore = Mathf.Max(BestScore, Score);
            BestChain = Mathf.Max(BestChain, rhythmManager?.BestChain ?? 0);
            var save = FindFirstObjectByType<SaveManager>();
            save?.SaveScore(Score, SeedToCode(RunSeed), Level, BestChain);
            RunSeed = Random.Range(0, 0xFFFFFF);
            OnGameEnded?.Invoke();
        }

        /// <summary>
        /// Adds score and checks for level up.
        /// </summary>
        public void AddScore(int amount)
        {
            Score += amount * rhythmManager.Multiplier;
            CheckLevel();
        }

        /// <summary>
        /// Adds distance-based score every 10 world units.
        /// </summary>
        public void AccumulateDistance(float distance)
        {
            TotalDistance += distance;
            DistanceAccumulator += distance;
            if (DistanceAccumulator >= 10f)
            {
                int units = Mathf.FloorToInt(DistanceAccumulator / 10f);
                Score += units * rhythmManager.Multiplier;
                DistanceAccumulator %= 10f;
                CheckLevel();
            }
        }

        /// <summary>
        /// Called when a ring is passed through.
        /// </summary>
        public void CollectRing()
        {
            RingsHit++;
            AddScore(5);
        }

        /// <summary>
        /// Called when a coin is collected.
        /// </summary>
        public void CollectCoin()
        {
            AddScore(10);
        }

        private void CheckLevel()
        {
            int newLevel = 1;
            for (int i = 1; i < maxLevel; i++)
            {
                if (Score >= levelThresholds[i])
                    newLevel = i + 1;
            }

            if (newLevel > Level)
            {
                Level = newLevel;
                rhythmManager.Level = Level;
                worldGenerator.Level = Level;
                OnLevelUp?.Invoke();
            }
        }

        /// <summary>
        /// Encodes the current run seed into a shareable code.
        /// </summary>
        public string SeedToCode(int seed)
        {
            return codePrefix + seed.ToString("X").PadLeft(5, '0');
        }

        /// <summary>
        /// Decodes a run code back to a seed.
        /// </summary>
        public int? CodeToSeed(string code)
        {
            string stripped = code.ToUpperInvariant().Replace(codePrefix, "").Replace("-", "");
            if (int.TryParse(stripped, System.Globalization.NumberStyles.HexNumber, null, out int seed))
                return seed;
            return null;
        }

        /// <summary>
        /// Loads a challenge seed from user input.
        /// </summary>
        public void LoadChallengeCode(string code)
        {
            int? seed = CodeToSeed(code);
            if (seed.HasValue) RunSeed = seed.Value;
        }

        private void Start() { }
        private void Update() { }
    }
}
