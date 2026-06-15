using UnityEngine;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Drives the main game tick loop: rhythm, dimension flip, world generation, and scoring.
    /// </summary>
    public class GameLoop : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PigController player;
        [SerializeField] private RhythmManager rhythmManager;
        [SerializeField] private DimensionFlipper dimensionFlipper;
        [SerializeField] private WorldGenerator worldGenerator;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private SaveManager saveManager;

        [Header("Gameplay")]
        [SerializeField] private KeyCode flipKey = KeyCode.F;
        [SerializeField] private float killDepth = -8f;
        [SerializeField] private float ceilingKillOffset = 2f;

        private bool started;
        private Vector3 lastPlayerPos;

        private void Awake()
        {
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            if (player == null) player = FindFirstObjectByType<PigController>();
            if (rhythmManager == null) rhythmManager = FindFirstObjectByType<RhythmManager>();
            if (dimensionFlipper == null) dimensionFlipper = FindFirstObjectByType<DimensionFlipper>();
            if (worldGenerator == null) worldGenerator = FindFirstObjectByType<WorldGenerator>();
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
            if (saveManager == null) saveManager = FindFirstObjectByType<SaveManager>();

            if (worldGenerator != null && player != null)
                worldGenerator.Initialize(player.transform);

            if (gameManager != null)
                gameManager.StartGame();
        }

        private void Start()
        {
            if (player != null)
            {
                player.ResetPig(new Vector3(0f, 4f, 0f));
                lastPlayerPos = player.transform.position;
            }

            started = true;
        }

        private void Update()
        {
            if (!started || gameManager == null || player == null || rhythmManager == null)
                return;

            float dt = Time.deltaTime;

            if (Input.GetKeyDown(flipKey) || Input.GetKeyDown(KeyCode.Tab))
                dimensionFlipper?.ToggleDimension();

            rhythmManager.Tick(dt);
            dimensionFlipper?.Tick(dt, gameManager.Playing, player.Speed3D);
            worldGenerator?.Tick(player.transform.position);
            worldGenerator?.UpdateDynamics(Time.time, dt);

            TrackDistance();
            CheckDeath();
            UpdateHud();
        }

        private void TrackDistance()
        {
            Vector3 pos = player.transform.position;
            float delta = Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(lastPlayerPos.x, 0f, lastPlayerPos.z));
            gameManager.AccumulateDistance(delta);
            lastPlayerPos = pos;
        }

        private void CheckDeath()
        {
            if (gameManager.Dead) return;

            float y = player.transform.position.y;
            if (y < killDepth)
            {
                gameManager.Die();
                return;
            }

            float ceiling = dimensionFlipper != null && dimensionFlipper.Is3D ? player.CeilingHeight : player.CeilingHeight + ceilingKillOffset;
            if (y > ceiling)
            {
                gameManager.Die();
            }
        }

        private void UpdateHud()
        {
            if (uiManager == null) return;
            uiManager.UpdateHud(gameManager.Score, gameManager.RingsHit, gameManager.Level, rhythmManager.Multiplier, rhythmManager.BeatChain, player.Yaw);
        }
    }
}
