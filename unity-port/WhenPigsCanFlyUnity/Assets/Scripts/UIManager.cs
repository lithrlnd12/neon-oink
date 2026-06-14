using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Manages HUD, menus, overlays, and combat UI.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI ringsText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI multiplierText;
        [SerializeField] private TextMeshProUGUI chainText;
        [SerializeField] private TextMeshProUGUI headingText;
        [SerializeField] private TextMeshProUGUI modeText;
        [SerializeField] private RectTransform hpBarFill;

        [Header("Overlays")]
        [SerializeField] private GameObject menuOverlay;
        [SerializeField] private GameObject deathOverlay;
        [SerializeField] private GameObject levelBanner;
        [SerializeField] private TextMeshProUGUI levelBannerTitle;
        [SerializeField] private TextMeshProUGUI levelBannerSubtitle;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI runCodeText;
        [SerializeField] private TMP_InputField codeInput;

        [Header("Combat")]
        [SerializeField] private GameObject combatControls;
        [SerializeField] private GameObject crosshair;

        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PigBrawlManager brawlManager;
        [SerializeField] private PigController player;
        [SerializeField] private DimensionFlipper flipper;

        private readonly string[] levelSubtitles = {
            "",
            "",
            "crows incoming · bass drops in",
            "neon gates · arp unlocked · balloons bob",
            "crow swarms · double hats",
            "moving gates · sub bass",
            "dusk falls",
            "neon night · synth pads",
            "crow chaos",
            "darkness",
            "MAX — full synth wall"
        };

        private void Awake()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStarted += OnGameStarted;
                gameManager.OnGameEnded += OnGameEnded;
                gameManager.OnLevelUp += OnLevelUp;
            }
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStarted -= OnGameStarted;
                gameManager.OnGameEnded -= OnGameEnded;
                gameManager.OnLevelUp -= OnLevelUp;
            }
        }

        /// <summary>
        /// Refreshes the main HUD.
        /// </summary>
        public void UpdateHud(int score, int rings, int level, int multiplier, int chain, float yaw)
        {
            if (scoreText != null) scoreText.text = score.ToString();
            if (ringsText != null) ringsText.text = $"⭕ {rings}";
            if (levelText != null) levelText.text = $"LVL {level}";
            if (multiplierText != null) multiplierText.text = multiplier > 1 ? $"{multiplier}×" : "";
            if (chainText != null) chainText.text = chain > 0 ? $"chain {chain}" : "";

            float deg = ((yaw * Mathf.Rad2Deg) % 360f + 360f) % 360f;
            if (headingText != null) headingText.text = $"heading {deg:F0}°";
        }

        /// <summary>
        /// Refreshes the combat HUD.
        /// </summary>
        public void UpdateCombatHud(int points, int kos, float hp, float maxHp, bool kidsMode, float matchTime)
        {
            if (scoreText != null) scoreText.text = points.ToString();
            if (ringsText != null) ringsText.text = $"💀 {kos}   ❤ {Mathf.Max(0, Mathf.RoundToInt(hp))}";
            if (levelText != null)
                levelText.text = kidsMode
                    ? "PIG BRAWL · SPACE=flap+laser · TAB/click=poop"
                    : "PIG BRAWL · SPACE/F/click laser · TAB/C/R-click poop · Esc quit";

            if (hpBarFill != null)
            {
                float ratio = Mathf.Max(0f, hp / maxHp);
                hpBarFill.anchorMax = new Vector2(ratio, 1f);
            }
        }

        private void OnGameStarted()
        {
            menuOverlay.SetActive(false);
            deathOverlay.SetActive(false);
        }

        private void OnGameEnded()
        {
            deathOverlay.SetActive(true);
            finalScoreText.text = $"Score {gameManager.Score} · LVL {gameManager.Level} · ⭕{gameManager.RingsHit} · Best chain {gameManager.BestChain}";
            runCodeText.text = "YOUR RUN CODE: " + gameManager.SeedToCode(gameManager.RunSeed) + " — share it!";
        }

        private void OnLevelUp()
        {
            levelBannerTitle.text = "LEVEL " + gameManager.Level;
            levelBannerSubtitle.text = levelSubtitles[Mathf.Clamp(gameManager.Level, 0, levelSubtitles.Length - 1)];
            levelBanner.SetActive(true);
            // Animator trigger can be fired here.
        }

        /// <summary>
        /// Reads the run-code input and asks GameManager to load it.
        /// </summary>
        public void OnChallengeCodeSubmitted()
        {
            if (gameManager != null && codeInput != null)
                gameManager.LoadChallengeCode(codeInput.text);
        }

        private void Update()
        {
            if (modeText != null && flipper != null)
                modeText.text = flipper.Is3D ? "3D MODE" : "2D MODE";
        }
    }
}
