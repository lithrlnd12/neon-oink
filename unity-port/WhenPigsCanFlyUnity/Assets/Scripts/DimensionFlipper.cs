using UnityEngine;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Handles 2D/3D dimension switching and the cinematic camera/overlay transition.
    /// Drives a smooth morph timer, FOV kick, and canvas crossfade.
    /// </summary>
    public class DimensionFlipper : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float morphDuration = 0.6f;
        [SerializeField] private float switchCooldownBase = 0.4f;

        [Header("Camera")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private float baseFov = 62f;
        [SerializeField] private float morphFovBoost = 24f;
        [SerializeField] private float fovSmooth = 4f;
        [SerializeField] private float speedFovBonus = 0.55f;
        [SerializeField] private float maxFov = 76f;

        [Header("Canvas Overlay")]
        [SerializeField] private CanvasGroup flatCanvasGroup;
        [SerializeField] private RectTransform flatCanvasRect;

        public bool Is3D { get; private set; }
        public bool IsMorphing { get; private set; }
        public float MorphT { get; private set; } = 1f;
        public float Cooldown { get; private set; }

        /// <summary>
        /// Returns the smoothstep-eased morph value.
        /// </summary>
        public float MorphEase => MorphT <= 0f ? 0f : MorphT >= 1f ? 1f : MorphT * MorphT * (3f - 2f * MorphT);

        public delegate void DimensionChanged(bool is3D);
        public event DimensionChanged OnDimensionChanged;

        private void Awake()
        {
            if (gameCamera == null) gameCamera = Camera.main;
        }

        /// <summary>
        /// Requests a dimension flip, honoring the cooldown.
        /// </summary>
        public void ToggleDimension()
        {
            if (Cooldown > 0f || IsMorphing) return;
            Is3D = !Is3D;
            Cooldown = Mathf.Max(switchCooldownBase, morphDuration);
            IsMorphing = true;
            MorphT = 0f;
            OnDimensionChanged?.Invoke(Is3D);
        }

        /// <summary>
        /// Forces a dimension without transition (e.g. entering combat).
        /// </summary>
        public void Force3D()
        {
            Is3D = true;
            IsMorphing = false;
            MorphT = 1f;
            OnDimensionChanged?.Invoke(true);
        }

        /// <summary>
        /// Advances the morph and camera FOV each frame.
        /// </summary>
        public void Tick(float dt, bool playing, float speed3D)
        {
            Cooldown = Mathf.Max(0f, Cooldown - dt);

            if (IsMorphing)
            {
                MorphT = Mathf.Min(1f, MorphT + dt / morphDuration);
                if (MorphT >= 1f) IsMorphing = false;
            }

            float targetFov = baseFov;
            if (Is3D && playing)
                targetFov = Mathf.Min(maxFov, baseFov + (speed3D - 7f) * speedFovBonus);

            if (IsMorphing)
                gameCamera.fieldOfView = targetFov + (1f - MorphEase) * morphFovBoost;
            else
                gameCamera.fieldOfView += (targetFov - gameCamera.fieldOfView) * Mathf.Min(1f, dt * fovSmooth);

            UpdateOverlay();
        }

        private void UpdateOverlay()
        {
            if (flatCanvasGroup == null) return;

            float ease = MorphEase;
            if (IsMorphing)
            {
                float opacity = Is3D ? 1f - ease : ease;
                float scale = 1f + ease * 0.14f;
                flatCanvasGroup.alpha = opacity;
                flatCanvasRect.localScale = Vector3.one * scale;
            }
            else
            {
                flatCanvasGroup.alpha = Is3D ? 0f : 1f;
                flatCanvasRect.localScale = Vector3.one;
            }
        }

        private void Start() { }
        private void Update() { }
    }
}
