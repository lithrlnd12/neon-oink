using Unity.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Infinite chunked world generator and recycler.
    /// Spawns barns, silos, trees, balloons, rings, coins, crows, and neon fences.
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        [Header("Chunk Settings")]
        [SerializeField] private float chunkSize = 60f;
        [SerializeField] private int viewChunks = 3;
        [SerializeField] private float corridorWidth = 3.2f;

        [Header("Obstacle Prefabs")]
        [SerializeField] private GameObject barnPrefab;
        [SerializeField] private GameObject siloPrefab;
        [SerializeField] private GameObject treePrefab;
        [SerializeField] private GameObject balloonPrefab;
        [SerializeField] private GameObject ringPrefab;
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private GameObject crowPrefab;
        [SerializeField] private GameObject fencePrefab;

        [Header("Density / Chance")]
        [SerializeField] private int baseDensity = 4;
        [SerializeField] private int maxDensity = 15;
        [SerializeField] private AnimationCurve crowChanceByLevel = AnimationCurve.Linear(0f, 0f, 10f, 0.26f);
        [SerializeField] private AnimationCurve fenceChanceByLevel = AnimationCurve.Linear(0f, 0f, 10f, 0.22f);
        [SerializeField] private AnimationCurve ringChanceByLevel = AnimationCurve.Linear(0f, 0.05f, 10f, 0.05f);

        [Header("Level Scaling")]
        [SerializeField] private float balloonBobAmp = 1f;
        [SerializeField] private float ceilingHeight = 30f;

        public int Level { get; set; } = 1;
        public int RunSeed { get; set; }

        private Transform player;
        private ObjectPool<GameObject>[] pools;

        /// <summary>
        /// Initialize the generator with a player reference.
        /// </summary>
        public void Initialize(Transform playerTransform)
        {
            player = playerTransform;
        }

        /// <summary>
        /// Updates chunks around the player and recycles distant ones.
        /// </summary>
        public void Tick(Vector3 playerPos)
        {
            int cx = Mathf.FloorToInt(playerPos.x / chunkSize);
            int cz = Mathf.FloorToInt(playerPos.z / chunkSize);

            for (int dx = -viewChunks; dx <= viewChunks; dx++)
                for (int dz = -viewChunks; dz <= viewChunks; dz++)
                    BuildChunk(cx + dx, cz + dz);

            RecycleDistant(cx, cz);
        }

        private void BuildChunk(int cx, int cz)
        {
            // Stub: seed RNG, place obstacles, track active chunks.
        }

        private void RecycleDistant(int cx, int cz)
        {
            // Stub: remove chunks outside viewChunks + 1.
        }

        /// <summary>
        /// Returns a deterministic RNG for a chunk coordinate.
        /// </summary>
        private System.Random GetRng(int cx, int cz)
        {
            uint seed = (uint)((cx * 73856093) ^ (cz * 19349663) + Level * 7919 + RunSeed);
            return new System.Random((int)(seed == 0 ? 1 : seed));
        }

        /// <summary>
        /// Updates dynamic obstacles: crow orbits, balloon bob, moving fences.
        /// </summary>
        public void UpdateDynamics(float time, float dt)
        {
            // Stub: animate crows, balloons, fences, rings, coins.
        }

        /// <summary>
        /// Clears all spawned chunks (used on restart).
        /// </summary>
        public void ClearWorld()
        {
            // Stub: return all active objects to pools.
        }

        private void Start() { }
        private void Update() { }
    }
}
