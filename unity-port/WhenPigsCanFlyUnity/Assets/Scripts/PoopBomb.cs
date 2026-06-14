using UnityEngine;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Gravity poop bomb: arcing projectile, explosion, splatter damage, and gibs.
    /// </summary>
    public class PoopBomb : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] private float gravity = -30f;
        [SerializeField] private float initialUpward = -3f;
        [SerializeField] private float lifeTime = 6f;
        [SerializeField] private float floorHeight = 0.7f;

        [Header("Explosion")]
        [SerializeField] private float explosionRadius = 5.5f;
        [SerializeField] private float maxDamage = 60f;
        [SerializeField] private float pushForce = 2f;

        [Header("Effects")]
        [SerializeField] private GameObject puffPrefab;
        [SerializeField] private GameObject gibPrefab;
        [SerializeField] private AudioClip splatClip;
        [SerializeField] private AudioClip plopClip;

        public Vector3 Velocity { get; set; }
        public Vector3 Spin { get; set; }

        private float age;

        /// <summary>
        /// Launches the bomb from origin with a small backward/downward arc.
        /// </summary>
        public void Launch(Vector3 origin)
        {
            transform.position = origin + Vector3.down * 0.7f;
            Velocity = new Vector3(0f, initialUpward, 0f);
            Spin = new Vector3(7f, 0f, 5f) * (Random.value > 0.5f ? 1f : -1f);
            age = 0f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            age += dt;

            Velocity.y += gravity * dt;
            transform.position += Velocity * dt;
            transform.Rotate(Spin * dt);

            bool hitBot = false;
            // Simple proximity check against bots; replace with Physics.OverlapSphere for production.
            Collider[] hits = Physics.OverlapSphere(transform.position, 2.2f);
            foreach (Collider c in hits)
            {
                if (c.CompareTag("Bot"))
                {
                    hitBot = true;
                    break;
                }
            }

            if (transform.position.y <= floorHeight || hitBot || age >= lifeTime)
            {
                if (transform.position.y < floorHeight)
                    transform.position = new Vector3(transform.position.x, floorHeight, transform.position.z);
                Explode();
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Detonates the bomb, applying radial damage and spawning gibs.
        /// </summary>
        private void Explode()
        {
            if (puffPrefab != null) Instantiate(puffPrefab, transform.position, Quaternion.identity);
            if (gibPrefab != null)
                for (int i = 0; i < 14; i++)
                    Instantiate(gibPrefab, transform.position, Random.rotation);

            Collider[] victims = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (Collider c in victims)
            {
                if (!c.CompareTag("Bot")) continue;
                float d = Vector3.Distance(transform.position, c.transform.position);
                float damage = maxDamage * (1f - d / (explosionRadius + 1f));
                Vector3 push = (c.transform.position - transform.position);
                push.y = 0f;
                push.Normalize();
                c.transform.position += push * pushForce;
                // Dispatch damage to BotPigAI.
            }
        }

        private void Start() { }
    }
}
