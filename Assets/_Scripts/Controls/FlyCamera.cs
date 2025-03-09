using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    public float moveSpeed = 5f; // Base speed of movement
    public float lookSpeed = 2f; // Speed of looking around
    public float zoomSpeed = 5f; // Speed of zooming (FOV adjustment)
    public float shiftMultiplier = 2f; // Speed multiplier when Shift is held
    public float minFOV = 20f; // Minimum field of view
    public float maxFOV = 90f; // Maximum field of view
    public float smoothZoomTime = 0.2f; // Time to smooth the zoom

    private Vector3 _rotation;
    private Camera _camera;
    private float _targetFOV;
    private float _zoomVelocity; // Used for smooth damping

    void Start()
    {
        // Initialize rotation to the camera's current rotation
        _rotation = transform.eulerAngles;
        // Get the Camera component
        _camera = GetComponent<Camera>();
        // Set the initial target FOV to the camera's current FOV
        _targetFOV = _camera.fieldOfView;
    }

    void Update()
    {
        // Handle movement
        HandleMovement();
        // Handle looking around
        HandleLook();
        // Handle zooming (FOV adjustment)
        HandleZoom();
        // Smoothly adjust the FOV
        SmoothZoom();
    }

    void HandleMovement()
    {
        // Calculate movement speed (apply shift multiplier if Shift is held)
        float currentMoveSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            currentMoveSpeed *= shiftMultiplier;
        }

        // Get input from WASD keys
        float moveX = Input.GetAxis("Horizontal") * currentMoveSpeed * Time.deltaTime;
        float moveZ = Input.GetAxis("Vertical") * currentMoveSpeed * Time.deltaTime;

        // Move the camera
        transform.Translate(new Vector3(moveX, 0, moveZ));
    }

    void HandleLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        // Update rotation based on mouse movement
        _rotation.y += mouseX;
        _rotation.x -= mouseY;
        _rotation.x = Mathf.Clamp(_rotation.x, -90f, 90f); // Clamp vertical rotation

        // Apply rotation to the camera
        transform.eulerAngles = _rotation;
    }

    void HandleZoom()
    {
        // Get scroll wheel input
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // Adjust the target FOV based on scroll input
        if (scroll != 0)
        {
            _targetFOV -= scroll * zoomSpeed;
            _targetFOV = Mathf.Clamp(_targetFOV, minFOV, maxFOV);
        }
    }

    void SmoothZoom()
    {
        // Smoothly interpolate the camera's FOV towards the target FOV
        _camera.fieldOfView = Mathf.SmoothDamp(_camera.fieldOfView, _targetFOV, ref _zoomVelocity, smoothZoomTime);
    }
}