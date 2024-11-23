using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed of the camera in units per second.")]
    public float movementSpeed = 5.0f;

    [Tooltip("Enable smooth movement.")]
    public bool smoothMovement = true;

    [Tooltip("Smooth movement interpolation speed.")]
    public float smoothSpeed = 10.0f;

    [Header("Movement Directions")]
    [Tooltip("Move the camera forward and backward.")]
    public bool moveForwardBackward = true;

    [Tooltip("Move the camera left and right.")]
    public bool moveLeftRight = true;

    [Tooltip("Move the camera up and down.")]
    public bool moveUpDown = false;

    // Movement directions
    private Vector3 movement;

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        MoveCamera();
    }

    /// <summary>
    /// Captures input from arrow keys and calculates movement vector.
    /// </summary>
    void HandleInput()
    {
        movement = Vector3.zero;

        // Horizontal Movement (Left and Right)
        if (moveLeftRight)
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                movement += Vector3.left;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                movement += Vector3.right;
            }
        }

        // Vertical Movement (Up and Down)
        if (moveUpDown)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                movement += Vector3.up;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                movement += Vector3.down;
            }
        }

        // Forward and Backward Movement (W and S keys)
        if (moveForwardBackward)
        {
            if (Input.GetKey(KeyCode.W))
            {
                movement += Vector3.forward;
            }
            if (Input.GetKey(KeyCode.S))
            {
                movement += Vector3.back;
            }
        }

        // Normalize movement to ensure consistent speed
        if (movement.magnitude > 1)
        {
            movement = movement.normalized;
        }
    }

    /// <summary>
    /// Moves the camera based on the calculated movement vector.
    /// </summary>
    void MoveCamera()
    {
        Vector3 targetPosition = transform.position + movement * movementSpeed * Time.fixedDeltaTime;

        if (smoothMovement)
        {
            // Smoothly interpolate to the target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // Move instantly to the target position
            transform.position = targetPosition;
        }
    }
}
