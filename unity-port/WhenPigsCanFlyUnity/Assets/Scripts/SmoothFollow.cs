using UnityEngine;

namespace WhenPigsCanFly
{
    public class SmoothFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 2f, -6f);
        public float smoothTime = 0.2f;
        private Vector3 velocity;

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 targetPos = target.position + target.rotation * offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
            transform.LookAt(target.position + Vector3.up * 0.5f);
        }
    }
}