using UnityEngine;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Enemy pig AI: approach, retreat, strafe, and fire at the player.
    /// </summary>
    [RequireComponent(typeof(PigController))]
    public class BotPigAI : MonoBehaviour
    {
        [Header("Combat")]
        [SerializeField] private float laserRange = 70f;
        [SerializeField] private float fireCooldownBase = 1.1f;
        [SerializeField] private float fireCooldownRandom = 1.4f;
        [SerializeField] private float kidsFireCooldownBase = 2.2f;
        [SerializeField] private float approachDistanceFar = 22f;
        [SerializeField] private float retreatDistanceNear = 12f;
        [SerializeField] private float hoverAmplitude = 3f;

        [Header("Movement")]
        [SerializeField] private float strafeSpeed = 5f;
        [SerializeField] private float turnSpeed = 1.6f;
        [SerializeField] private float verticalChase = 1.3f;
        [SerializeField] private float ceilingHeight = 30f;
        [SerializeField] private float floorHeight = 0.7f;

        public float Hp { get; set; } = 100f;
        public bool Alive { get; set; }
        public float FireCooldown { get; set; }
        public float PoopStainTimer { get; set; }
        public float HitFlashTimer { get; set; }

        private PigController pig;
        private Transform target;
        private float bob;

        private void Awake()
        {
            pig = GetComponent<PigController>();
            bob = Random.value * Mathf.PI * 2f;
        }

        /// <summary>
        /// Assigns the player target.
        /// </summary>
        public void SetTarget(Transform player)
        {
            target = player;
        }

        /// <summary>
        /// Respawns the bot at the given position facing the player.
        /// </summary>
        public void Respawn(Vector3 position, float yaw)
        {
            transform.position = position;
            pig.Yaw = yaw;
            Hp = 100f;
            Alive = true;
            FireCooldown = fireCooldownBase + Random.value * fireCooldownRandom;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Updates bot behavior for the frame.
        /// </summary>
        public void Tick(float dt, float time, bool kidsMode)
        {
            if (!Alive || target == null) return;

            FireCooldown -= dt;
            PoopStainTimer = Mathf.Max(0f, PoopStainTimer - dt);
            HitFlashTimer = Mathf.Max(0f, HitFlashTimer - dt);

            Vector3 toPlayer = target.position - transform.position;
            float dist = toPlayer.magnitude;

            // Face the player smoothly.
            float wantYaw = Mathf.Atan2(-toPlayer.z, toPlayer.x);
            pig.Yaw += AngleDelta(pig.Yaw, wantYaw) * Mathf.Min(1f, dt * turnSpeed);

            // Choose speed based on distance: close = retreat, far = approach, medium = hover.
            float speed;
            if (dist > approachDistanceFar) speed = 11f;
            else if (dist < retreatDistanceNear) speed = -7f;
            else speed = 2.5f;

            if (kidsMode) speed *= 0.65f;

            // Strafe / bob in world space.
            Vector3 forward = new Vector3(Mathf.Cos(pig.Yaw), 0f, -Mathf.Sin(pig.Yaw));
            Vector3 lateral = new Vector3(-forward.z, 0f, forward.x);
            Vector3 move = forward * speed + lateral * (Mathf.Sin(time * 1.3f + bob) * strafeSpeed);

            // Vertical chase with bob.
            float targetY = target.position.y + Mathf.Sin(time * 1f + bob) * hoverAmplitude;
            float vy = (targetY - transform.position.y) * verticalChase;
            move.y = vy;

            transform.position += move * dt;
            transform.position = new Vector3(
                transform.position.x,
                Mathf.Clamp(transform.position.y, floorHeight + 1f, ceilingHeight),
                transform.position.z);

            // Fire when in range.
            if (FireCooldown <= 0f && dist < laserRange)
            {
                float fireChance = kidsMode ? 0.25f : 0.6f;
                if (Random.value < fireChance)
                    FireLaser();
                FireCooldown = (kidsMode ? kidsFireCooldownBase : fireCooldownBase) + Random.value * fireCooldownRandom;
            }
        }

        private void FireLaser()
        {
            // Hook into LaserWeapon or directly fire a raycast.
        }

        private static float AngleDelta(float current, float target)
        {
            float d = (target - current) % (Mathf.PI * 2f);
            if (d > Mathf.PI) d -= Mathf.PI * 2f;
            if (d < -Mathf.PI) d += Mathf.PI * 2f;
            return d;
        }

        private void Start() { }
        private void Update() { }
    }
}
