using UnityEngine;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Hitscan laser weapon with aim-assist cone. Supports player and bot fire.
    /// </summary>
    public class LaserWeapon : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] private float laserDamage = 22f;
        [SerializeField] private float laserRange = 70f;
        [SerializeField] private float cooldown = 0.26f;

        [Header("Aim Assist")]
        [SerializeField] private float cone3D = 0.34f;
        [SerializeField] private float cone2D = 0.16f;
        [SerializeField] private float kidsConeMultiplier = 2.2f;

        [Header("Effects")]
        [SerializeField] private LineRenderer beamPrefab;
        [SerializeField] private float beamDuration = 0.16f;
        [SerializeField] private AudioClip zapClip;

        public float Cooldown { get; private set; }
        public bool KidsMode { get; set; }

        private Transform owner;
        private LayerMask targetMask;

        /// <summary>
        /// Sets up the weapon owner and target layer.
        /// </summary>
        public void Initialize(Transform ownerTransform, LayerMask targets)
        {
            owner = ownerTransform;
            targetMask = targets;
        }

        private void Update()
        {
            if (Cooldown > 0f) Cooldown -= Time.deltaTime;
        }

        /// <summary>
        /// Fires the laser from the owner in the given yaw direction.
        /// Returns true if a target was hit.
        /// </summary>
        public bool Fire(float yaw, bool is3D, out RaycastHit hitInfo)
        {
            hitInfo = default;
            if (Cooldown > 0f) return false;

            Vector3 eye = owner.position + Vector3.up * 0.5f + new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw)) * 1.5f;
            Vector3 forward = new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw));
            float cone = (is3D ? cone3D : cone2D) * (KidsMode ? kidsConeMultiplier : 1f);

            Transform best = BestTargetInCone(eye, forward, cone);
            Vector3 end;
            bool hit = false;

            if (best != null)
            {
                end = best.position;
                hit = true;
                ApplyDamage(best);
            }
            else
            {
                end = eye + forward * laserRange;
                hit = Physics.Raycast(eye, forward, out RaycastHit rayInfo, laserRange, targetMask);
                if (hit && rayInfo.collider != null)
                    ApplyDamage(rayInfo.collider.transform);
            }

            SpawnBeam(eye, end);
            Cooldown = cooldown;
            return hit;
        }

        private Transform BestTargetInCone(Vector3 eye, Vector3 forward, float cone)
        {
            float bestDistance = float.MaxValue;
            Transform best = null;
            float cosCone = Mathf.Cos(cone);

            Collider[] candidates = Physics.OverlapSphere(eye, laserRange, targetMask);
            foreach (Collider c in candidates)
            {
                Vector3 to = c.transform.position - eye;
                float hd = Mathf.Sqrt(to.x * to.x + to.z * to.z);
                if (hd < 0.01f) continue;
                if ((to.x * forward.x + to.z * forward.z) / hd < cosCone) continue;

                float d = to.magnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = c.transform;
                }
            }
            return best;
        }

        private void ApplyDamage(Transform target)
        {
            // Dispatch to health component; BotPigAI or GameManager handles the value.
        }

        private void SpawnBeam(Vector3 a, Vector3 b)
        {
            if (beamPrefab == null) return;
            LineRenderer beam = Instantiate(beamPrefab, a, Quaternion.identity);
            beam.SetPosition(0, a);
            beam.SetPosition(1, b);
            Destroy(beam.gameObject, beamDuration);
        }

        private void Start() { }
    }
}
