using UnityEngine;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Sets up and runs the Pig Brawl combat sandbox: bots, timer, scoring, and player health.
    /// </summary>
    public class PigBrawlManager : MonoBehaviour
    {
        [Header("Match")]
        [SerializeField] private float matchDuration = 90f;
        [SerializeField] private float kidsMatchDuration = 9999f;
        [SerializeField] private float respawnTime = 3f;

        [Header("Player")]
        [SerializeField] private float playerMaxHp = 100f;
        [SerializeField] private float invulnerabilityTime = 2.4f;

        [Header("Scoring")]
        [SerializeField] private int pointsLaser = 10;
        [SerializeField] private int pointsPoop = 25;
        [SerializeField] private int pointsKO = 50;

        [Header("Bots")]
        [SerializeField] private int botCount = 3;
        [SerializeField] private GameObject botPrefab;
        [SerializeField] private Transform[] spawnPoints;

        public bool InCombat { get; private set; }
        public float MatchTimer { get; private set; }
        public int Points { get; private set; }
        public int Kos { get; private set; }
        public int Deaths { get; private set; }
        public float PlayerHp { get; private set; }
        public float InvulnTimer { get; private set; }
        public float HurtTimer { get; private set; }

        public bool KidsMode { get; set; }

        public delegate void CombatEvent();
        public event CombatEvent OnMatchStarted;
        public event CombatEvent OnMatchEnded;

        private BotPigAI[] bots;

        /// <summary>
        /// Starts a new brawl match.
        /// </summary>
        public void StartBrawl()
        {
            InCombat = true;
            Points = 0;
            Kos = 0;
            Deaths = 0;
            PlayerHp = playerMaxHp;
            InvulnTimer = 1.5f;
            HurtTimer = 0f;
            MatchTimer = KidsMode ? kidsMatchDuration : matchDuration;
            SpawnBots();
            OnMatchStarted?.Invoke();
        }

        /// <summary>
        /// Ends the match and reports the final score.
        /// </summary>
        public void EndBrawl()
        {
            InCombat = false;
            DespawnBots();
            OnMatchEnded?.Invoke();
        }

        /// <summary>
        /// Applies damage to the player, handling KO/respawn invulnerability.
        /// </summary>
        public void DamagePlayer(float amount)
        {
            if (InvulnTimer > 0f) return;
            PlayerHp -= amount;
            HurtTimer = 0.32f;
            if (PlayerHp <= 0f)
            {
                PlayerHp = playerMaxHp;
                InvulnTimer = invulnerabilityTime;
                Deaths++;
                HurtTimer = 0.6f;
            }
        }

        /// <summary>
        /// Adds points for a laser hit, poop hit, or KO.
        /// </summary>
        public void AddScore(ScoreType type)
        {
            switch (type)
            {
                case ScoreType.LaserHit: Points += pointsLaser; break;
                case ScoreType.PoopHit: Points += pointsPoop; break;
                case ScoreType.KO: Points += pointsKO; Kos++; break;
            }
        }

        private void SpawnBots()
        {
            // Stub: instantiate bots at spawn points and assign BotPigAI.
        }

        private void DespawnBots()
        {
            // Stub: return bots to pool or destroy.
        }

        /// <summary>
        /// Advances timers and checks match end.
        /// </summary>
        public void Tick(float dt)
        {
            if (!InCombat) return;

            HurtTimer = Mathf.Max(0f, HurtTimer - dt);
            InvulnTimer = Mathf.Max(0f, InvulnTimer - dt);

            if (!KidsMode)
            {
                MatchTimer = Mathf.Max(0f, MatchTimer - dt);
                if (MatchTimer <= 0f) EndBrawl();
            }
        }

        private void Start() { }
        private void Update() { }

        public enum ScoreType { LaserHit, PoopHit, KO }
    }
}
