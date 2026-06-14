#if UNITY_XR
using UnityEngine;
using UnityEngine.XR;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Optional XR rig setup: controller input, comfort vignette, and VR HUD.
    /// Compile only when an XR plug-in is installed.
    /// </summary>
    public class VRManager : MonoBehaviour
    {
        [Header("Rig")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Camera vrCamera;

        [Header("Input")]
        [SerializeField] private float joystickDeadzone = 0.12f;
        [SerializeField] private float turnSmooth = 7f;
        [SerializeField] private float heightSmooth = 7f;

        [Header("Comfort")]
        [SerializeField] private GameObject comfortVignette;
        [SerializeField] private float maxVignetteOpacity = 0.6f;

        public bool IsPresenting { get; private set; }
        public float TurnInput { get; private set; }
        public float SpeedInput { get; private set; }
        public bool LeftTrigger { get; private set; }
        public bool RightTrigger { get; private set; }
        public bool LeftGrip { get; private set; }
        public bool RightGrip { get; private set; }

        private InputData left;
        private InputData right;
        private float rigYaw;
        private float? rigHeight;

        /// <summary>
        /// Attempts to start an immersive VR session.
        /// </summary>
        public void TryStartVR()
        {
            if (XRSettings.isDeviceActive)
                IsPresenting = true;
        }

        private void Update()
        {
            if (!IsPresenting) return;
            PollInput();
            UpdateComfort();
        }

        /// <summary>
        /// Updates the camera rig to follow the player pig smoothly.
        /// </summary>
        public void Tick(float dt, Vector3 pigPosition, float pigYaw, bool is3D)
        {
            if (!IsPresenting || cameraRig == null) return;

            if (is3D)
            {
                float targetY = pigPosition.y + 0.7f;
                rigHeight = rigHeight == null ? targetY : Mathf.Lerp(rigHeight.Value, targetY, dt * heightSmooth);
                rigYaw = Mathf.LerpAngle(rigYaw, pigYaw, dt * turnSmooth);

                Vector3 offset = new Vector3(Mathf.Cos(rigYaw) * 1.2f, 0f, -Mathf.Sin(rigYaw) * 1.2f);
                cameraRig.position = new Vector3(pigPosition.x, rigHeight.Value, pigPosition.z) + offset;
                cameraRig.rotation = Quaternion.Euler(0f, (rigYaw - Mathf.PI / 2f) * Mathf.Rad2Deg, 0f);
            }
            else
            {
                float targetY = 12f;
                rigHeight = rigHeight == null ? targetY : Mathf.Lerp(rigHeight.Value, targetY, dt * heightSmooth * 0.4f);
                rigYaw = Mathf.LerpAngle(rigYaw, pigYaw, dt * turnSmooth * 0.7f);

                cameraRig.position = new Vector3(pigPosition.x, rigHeight.Value, pigPosition.z);
                cameraRig.rotation = Quaternion.Euler(0f, (rigYaw - Mathf.PI / 2f) * Mathf.Rad2Deg, 0f);
            }
        }

        private void PollInput()
        {
            // Modern Unity XR input should use InputSystem XR controllers or TrackedPoseDriver.
            // This is a low-level fallback stub.
            TurnInput = 0f;
            SpeedInput = 0f;
            LeftTrigger = false;
            RightTrigger = false;
            LeftGrip = false;
            RightGrip = false;
        }

        private void UpdateComfort()
        {
            if (comfortVignette == null) return;
            float target = Mathf.Abs(TurnInput) * 0.55f + Mathf.Max(0f, Mathf.Abs(SpeedInput) - 7f) * 0.02f;
            target = Mathf.Min(maxVignetteOpacity, target);
            CanvasGroup cg = comfortVignette.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = Mathf.Lerp(cg.alpha, target, Time.deltaTime * 5f);
                comfortVignette.SetActive(cg.alpha > 0.015f);
            }
        }

        private void Start() { }

        private struct InputData
        {
            public Vector2 axis;
            public bool trigger;
            public bool grip;
        }
    }
}
#else
using UnityEngine;

namespace WhenPigsCanFly
{
    /// <summary>
    /// Stubbed VR manager when no XR plug-in is installed.
    /// </summary>
    public class VRManager : MonoBehaviour
    {
        public bool IsPresenting => false;
        public float TurnInput { get; private set; }
        public float SpeedInput { get; private set; }
        public bool LeftTrigger { get; private set; }
        public bool RightTrigger { get; private set; }
        public void TryStartVR() { }
        public void Tick(float dt, Vector3 pigPosition, float pigYaw, bool is3D) { }
        private void Start() { }
        private void Update() { }
    }
}
#endif
