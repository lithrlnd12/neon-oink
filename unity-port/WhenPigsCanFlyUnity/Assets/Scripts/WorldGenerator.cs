using UnityEngine;

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
        private readonly System.Collections.Generic.HashSet<Vector2Int> activeChunks = new();
        private readonly System.Collections.Generic.List<GameObject> activeObjects = new();
        private readonly System.Collections.Generic.List<Transform> bobbingObjects = new();

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
            int cx = Mathf.FloorToInt(playerPos.z / chunkSize);
            int cz = 0;

            for (int dx = -viewChunks; dx <= viewChunks; dx++)
                BuildChunk(cx + dx, cz);

            RecycleDistant(cx, cz);
        }

        private void BuildChunk(int cx, int cz)
        {
            if (activeChunks.Contains(new Vector2Int(cx, cz))) return;
            activeChunks.Add(new Vector2Int(cx, cz));

            System.Random rng = GetRng(cx, cz);
            float zMin = cx * chunkSize;
            float zMax = zMin + chunkSize;
            int density = Mathf.Min(baseDensity + Level, maxDensity);

            for (int i = 0; i < density; i++)
            {
                float z = zMin + (float)rng.NextDouble() * chunkSize;
                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * corridorWidth * 3f;
                float y = Mathf.Lerp(3f, ceilingHeight - 3f, (float)rng.NextDouble());
                Vector3 pos = new Vector3(x, y, z);

                double roll = rng.NextDouble();
                if (roll < ringChanceByLevel.Evaluate(Level))
                    Spawn(ringPrefab, pos, Quaternion.identity);
                else if (roll < 0.25f)
                    Spawn(balloonPrefab, pos, Quaternion.identity);
                else if (roll < 0.4f)
                    Spawn(coinPrefab, pos + Vector3.up * 0.5f, Quaternion.identity);
                else if (roll < 0.6f)
                    Spawn(treePrefab, new Vector3(x, 0f, z), Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
                else if (roll < 0.75f)
                    Spawn(barnPrefab, new Vector3(x, 0f, z), Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
                else if (roll < 0.85f)
                    Spawn(siloPrefab, new Vector3(x, 0f, z), Quaternion.identity);
                else if (roll < 0.95f)
                    Spawn(fencePrefab, new Vector3(x, 1f, z), Quaternion.identity);
                else
                    Spawn(crowPrefab, pos, Quaternion.identity);
            }
        }

        private void RecycleDistant(int cx, int cz)
        {
            var toRemove = new System.Collections.Generic.List<Vector2Int>();
            foreach (var chunk in activeChunks)
            {
                if (Mathf.Abs(chunk.x - cx) > viewChunks + 1)
                    toRemove.Add(chunk);
            }

            foreach (var chunk in toRemove)
            {
                activeChunks.Remove(chunk);
            }

            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                if (activeObjects[i] == null)
                {
                    activeObjects.RemoveAt(i);
                    continue;
                }

                float z = activeObjects[i].transform.position.z;
                if (z < (cx - viewChunks - 1) * chunkSize || z > (cx + viewChunks + 1) * chunkSize)
                {
                    bobbingObjects.Remove(activeObjects[i].transform);
                    Destroy(activeObjects[i]);
                    activeObjects.RemoveAt(i);
                }
            }
        }

        private void Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return;
            GameObject go = Instantiate(prefab, position, rotation);
            go.transform.SetParent(transform);
            activeObjects.Add(go);
            if (prefab == balloonPrefab || prefab == ringPrefab || prefab == coinPrefab)
                bobbingObjects.Add(go.transform);
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
            for (int i = 0; i < bobbingObjects.Count; i++)
            {
                if (bobbingObjects[i] == null)
                {
                    bobbingObjects.RemoveAt(i);
                    i--;
                    continue;
                }
                Vector3 pos = bobbingObjects[i].position;
                pos.y += Mathf.Sin(time * 2f + pos.x) * balloonBobAmp * dt;
                bobbingObjects[i].position = pos;
            }
        }

        /// <summary>
        /// Clears all spawned chunks (used on restart).
        /// </summary>
        public void ClearWorld()
        {
            foreach (var go in activeObjects)
            {
                if (go != null) Destroy(go);
            }
            activeObjects.Clear();
            bobbingObjects.Clear();
            activeChunks.Clear();
        }

        private void Start() { }
        private void Update() { }
    }
}
