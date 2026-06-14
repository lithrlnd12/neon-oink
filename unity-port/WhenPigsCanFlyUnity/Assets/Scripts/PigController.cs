using UnityEngine;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Controls the player pig: flapping, gliding, 3D turning, banking, and speed.
    /// Mirrors the browser physics: gravity, flap impulse, glide lift cap, ceiling clamp, and yaw banking.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PigController : MonoBehaviour
    {
        [Header("Movement Constants")]
        [SerializeField] private float gravity = -30f;
        [SerializeField] private float flapVelocity = 11f;
        [SerializeField] private float ceilingHeight = 30f;
        [SerializeField] private float floorHeight = 0.7f;
        [SerializeField] private float speedMin = 7f;
        [SerializeField] private float speedMax = 26f;
        [SerializeField] private float turnRate = 1.7f;
        [SerializeField] private float glideLiftCap = -3.2f;
        [SerializeField] private float flapRateMs = 130f;

        [Header("3D Tuning")]
        [SerializeField] private float bankAngleMax = 0.55f;
        [SerializeField] private float bankSmooth = 6f;
        [SerializeField] private float turnSmooth = 8f;
        [SerializeField] private float pitchFactor = 0.045f;

        [Header("Input")]
        [SerializeField] private float joystickDeadzone = 0.12f;

        public Vector3 Velocity { get; private set; }
        public float VerticalSpeed { get; private set; }
        public float Yaw { get; private set; }
        public float Bank { get; private set; }
        public float Speed3D { get; set; }
        public float StunTimer { get; set; }
        public bool IsGliding { get; private set; }

        private float turnSmoothed;
        private float lastFlapTime;

        private CharacterController controller;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        /// <summary>
        /// Resets the pig to a starting state.
        /// </summary>
        public void ResetPig(Vector3 startPosition)
        {
            transform.position = startPosition;
            Velocity = Vector3.zero;
            VerticalSpeed = 4f;
            Yaw = 0f;
            Bank = 0f;
            turnSmoothed = 0f;
            StunTimer = 0f;
            Speed3D = 12f;
        }

        /// <summary>
        /// Applies a flap impulse, respecting the rate cap.
        /// </summary>
        public bool Flap()
        {
            if (StunTimer > 0f) return false;
            float now = Time.time * 1000f;
            if (now - lastFlapTime < flapRateMs) return false;
            lastFlapTime = now;
            VerticalSpeed = flapVelocity;
            return true;
        }

        /// <summary>
        /// Updates physics and rotation for a frame.
        /// </summary>
        /// <param name="dt">Delta time.</param>
        /// <param name="is3D">Whether the game is in 3D mode.</param>
        /// <param name="input">Normalized input (-1..1) for turn and speed.</param>
        /// <param name="glideHeld">Whether the glide button is held.</param>
        public void Tick(float dt, bool is3D, Vector2 input, bool glideHeld)
        {
            StunTimer = Mathf.Max(0f, StunTimer - dt);

            if (is3D)
            {
                float turn = Mathf.Abs(input.x) > joystickDeadzone ? -input.x : 0f;
                turnSmoothed += (Mathf.Clamp(turn, -1f, 1f) - turnSmoothed) * Mathf.Min(1f, dt * turnSmooth);
                Yaw += turnSmoothed * turnRate * dt;

                if (Mathf.Abs(input.y) > joystickDeadzone)
                    Speed3D = Mathf.Clamp(Speed3D - input.y * 14f * dt, speedMin, speedMax);
            }
            else
            {
                turnSmoothed += (0f - turnSmoothed) * Mathf.Min(1f, dt * turnSmooth);
            }

            float targetBank = is3D ? turnSmoothed * bankAngleMax : 0f;
            Bank += (targetBank - Bank) * Mathf.Min(1f, dt * bankSmooth);

            Quaternion yawRot = Quaternion.Euler(0f, Yaw * Mathf.Rad2Deg, 0f);
            Vector3 forward = yawRot * Vector3.forward;
            Vector3 right = yawRot * Vector3.right;

            float speed = is3D ? Mathf.Max(Speed3D, 12f * 0.8f) : Mathf.Min(18f, 10f);
            Vector3 move = forward * speed;

            VerticalSpeed += gravity * dt;
            IsGliding = glideHeld;
            if (glideHeld) VerticalSpeed = Mathf.Max(VerticalSpeed, glideLiftCap);

            move.y = VerticalSpeed;
            controller.Move(move * dt);

            if (transform.position.y > ceilingHeight)
            {
                transform.position = new Vector3(transform.position.x, ceilingHeight, transform.position.z);
                VerticalSpeed = Mathf.Min(0f, VerticalSpeed);
            }

            ApplyVisualRotation();
        }

        private void ApplyVisualRotation()
        {
            float pitch = Mathf.Clamp(VerticalSpeed * pitchFactor, -0.6f, 0.5f);
            transform.rotation = Quaternion.Euler(-Bank * Mathf.Rad2Deg, Yaw * Mathf.Rad2Deg, -pitch * Mathf.Rad2Deg);
        }

        private void Start() { }
        private void Update() { }
    }
}
