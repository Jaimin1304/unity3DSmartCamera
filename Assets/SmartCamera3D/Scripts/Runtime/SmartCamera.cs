using System;
using UnityEngine;

namespace SmartCamera
{
    /// <summary>
    /// A versatile camera controller that supports two operation modes:
    /// 1. Free Mode: Similar to spectator mode in FPS games, allowing full 3D movement
    /// 2. Focus Mode: CAD-style orbiting around a focal point with zoom capability
    /// </summary>

    // Define the camera modes enum
    // We use the [Serializable] attribute to make it visible in the Unity Inspector
    [Serializable]
    public enum CamMode
    {
        Free,   // Free-flying camera mode
        Focus   // Focus-based orbit mode
    }

    public class SmartCamera : MonoBehaviour
    {
        [Header("Default Camera Mode")]
        [Tooltip("Select the camera mode to start with")]
        [SerializeField]
        private CamMode defaultCamMode = CamMode.Free;  // Set Free mode as default


        [Header("Camera Mode Settings")]
        [Tooltip("Key used to toggle between free and focus modes")]
        public KeyCode modeToggleKey = KeyCode.Tab;

        [Header("Free Mode Settings")]
        [Tooltip("Base movement speed in free mode")]
        public float freeMovementSpeed = 50f;

        [Tooltip("Mouse sensitivity for camera rotation")]
        public float freeLookSensitivity = 2.5f;

        [Tooltip("Movement speed multiplier when holding shift")]
        public float sprintMultiplier = 4f;

        [Header("Focus Mode Settings")]
        [Tooltip("Orbit rotation speed around focus point")]
        public float orbitSpeed = 3f;

        [Tooltip("Zoom speed when using mouse wheel")]
        public float zoomSpeedMultiplier = 0.05f;

        [Tooltip("Pan speed multiplier when using middle mouse button")]
        public float panSpeedMultiplier = 0.1f;

        [Tooltip("Maximum distance allowed from focus point")]
        public float maxFocusDistance = 600f;

        [Tooltip("Minimum distance allowed from focus point")]
        public float minFocusDistance = 1f;

        [Tooltip("Maximum raycast distance for focus point detection")]
        public float maxRayDistance = 1000f;

        [Tooltip("Layer mask for focus point raycast")]
        public LayerMask raycastLayers = -1; // Default to all layers

        // Internal camera state
        private CamMode currCamMode;
        private Vector3 focusPoint;
        private float currFocusDist;

        // Camera rotation state
        private float rotationX = 0f;
        private float rotationY = 0f;

        private void Start()
        {
            // Initialize the camera with the default mode
            currCamMode = defaultCamMode;
            // Initialize camera angles from current transform
            Vector3 angles = transform.eulerAngles;
            rotationX = angles.y;
            rotationY = angles.x;
        }

        private void Update()
        {
            // Handle mode switching
            if (Input.GetKeyDown(modeToggleKey))
            {
                // toggle camera mode
                currCamMode = (currCamMode == CamMode.Free) ? CamMode.Focus : CamMode.Free;
            }

            // Update camera based on current mode
            if (currCamMode == CamMode.Focus)
            {
                UpdateFocusMode();
            }
            else
            {
                UpdateFreeMode();
            }
        }

        private void UpdateFreeMode()
        {
            // Process mouse input for rotation
            float mouseX = Input.GetAxis("Mouse X") * freeLookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * freeLookSensitivity;

            // Update camera angles
            rotationX += mouseX;
            rotationY -= mouseY;
            rotationY = Mathf.Clamp(rotationY, -90f, 90f); // Prevent over-rotation

            transform.rotation = Quaternion.Euler(rotationY, rotationX, 0);

            // Handle keyboard input for movement
            float speedMultiplier = Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f;
            float moveSpeed = freeMovementSpeed * speedMultiplier;

            // Combine all movement inputs
            Vector3 movement = new Vector3(
                Input.GetAxis("Horizontal"),
                (Input.GetKey(KeyCode.Space) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftControl) ? 1f : 0f),
                Input.GetAxis("Vertical")
            );

            // Apply movement in local space
            transform.Translate(movement * moveSpeed * Time.deltaTime, Space.Self);
        }

        private void UpdateFocusMode()
        {
            // Initialize focus point on right click
            if (Input.GetMouseButtonDown(1) || Input.GetAxis("Mouse ScrollWheel") != 0 || Input.GetMouseButton(2))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, maxRayDistance, raycastLayers))
                {
                    focusPoint = hit.point;
                    currFocusDist = Vector3.Distance(transform.position, focusPoint);
                }
            }

            // Handle zoom with mouse wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                currFocusDist = Mathf.Clamp(
                    currFocusDist - (Mathf.Pow(currFocusDist + scroll, 2) - Mathf.Pow(currFocusDist, 2)),
                    minFocusDistance,
                    maxFocusDistance
                );
                // Update position while maintaining direction
                Vector3 direction = (transform.position - focusPoint).normalized;
                transform.position = focusPoint + direction * currFocusDist;
            }

            // Handle orbital movement while focusing
            if (Input.GetMouseButton(1))
            {
                // Process mouse input for orbit
                float mouseX = Input.GetAxis("Mouse X") * orbitSpeed;
                float mouseY = Input.GetAxis("Mouse Y") * orbitSpeed;
                // Orbit around focus point
                transform.RotateAround(focusPoint, Vector3.up, mouseX);
                transform.RotateAround(focusPoint, transform.right, -mouseY);
                // sync rotation angles
                Vector3 angles = transform.eulerAngles;
                rotationX = angles.y;
                rotationY = angles.x;
                return;
            }

            // Handle panning with middle mouse button
            if (Input.GetMouseButton(2)) // 2 represents middle mouse button
            {
                // Calculate pan amount based on mouse movement
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");
                // Scale pan speed based on distance to focus point
                float distanceScale = currFocusDist * panSpeedMultiplier;
                // Calculate pan vectors in world space
                Vector3 rightPan = transform.right * (-mouseX * distanceScale);
                Vector3 upPan = transform.up * (-mouseY * distanceScale);
                Vector3 totalPan = rightPan + upPan;
                // Apply pan to both camera and focus point
                transform.position += totalPan;
                focusPoint += totalPan;
                return;
            }
        }
    }
}
