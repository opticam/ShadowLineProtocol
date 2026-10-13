using UnityEngine;

namespace ShadowLineProtocol
{
    public class RTSCamera : MonoBehaviour
    {
        const float Pitch = 52f;
        const float MinDistance = 10f;
        const float MaxDistance = 110f;
        const float RotateSpeed = 90f;

        Camera cam;
        Vector3 focus = new Vector3(0f, 0f, -2f);
        float distance = 40f;
        float yaw = 20f;

        public void Init(Camera cam)
        {
            this.cam = cam;
            Apply();
        }

        // Unscaled time keeps the camera usable on the end screen.
        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;

            Vector3 input = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.z -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
            float speed = distance * (Input.GetKey(KeyCode.LeftShift) ? 2f : 1f);
            focus += Quaternion.Euler(0f, yaw, 0f) * input.normalized * speed * dt;
            float limit = WorldBuilder.MapHalfSize;
            focus.x = Mathf.Clamp(focus.x, -limit, limit);
            focus.z = Mathf.Clamp(focus.z, -limit, limit);

            if (Input.GetKey(KeyCode.Q)) yaw += RotateSpeed * dt;
            if (Input.GetKey(KeyCode.E)) yaw -= RotateSpeed * dt;

            distance = Mathf.Clamp(distance * (1f - Input.mouseScrollDelta.y * 0.1f), MinDistance, MaxDistance);

            Apply();
        }

        void Apply()
        {
            var rotation = Quaternion.Euler(Pitch, yaw, 0f);
            cam.transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
        }
    }
}
