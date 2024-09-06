using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class MovementController : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Velocity Safety Measures")]
    [SerializeField] private float velocityThreshold = 0.01f;

    [Header("Gravity")]
    [SerializeField] private bool controlGravity = true;

    [Header("Friction")]
    [SerializeField] private bool useFriction = true;
    [SerializeField, Range(0f, 1f)] private float friction = 0.9f;
    [SerializeField, Range(0f, 1f)] private float airFriction = 1f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float maxGroundDistance = 1.0f;
    [SerializeField] private Transform groundCheckPosition;
    [SerializeField] private float groundCheckBoxWidth = 1.0f;
    public bool isGrounded = false;

    [Header("Step Handling")]
    [SerializeField] GameObject stepRayUpper;
    [SerializeField] GameObject stepRayLower;
    [SerializeField] float stepHeight = 0.3f;
    [SerializeField] float stepSmooth = 2f;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle = 45.0f;
    private RaycastHit slopeHit;
    public bool isOnSlope = false;

    [Header("Debugging")]
    [SerializeField] private bool debug = false;

    public bool movement = false;

    private void Awake()
    {
        stepRayUpper.transform.position = new Vector3(stepRayUpper.transform.position.x, stepHeight, stepRayUpper.transform.position.z);
    }
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        isGrounded = CheckGrounded();

        if (isGrounded)
        {
            SetGravity(false);
            AddForce(Vector3.down * 0.1f);
            if (!movement && useFriction)
            {
                ApplyFriction(friction);
            }
        }
        else
        {
            SetGravity(true);
            if (!movement && useFriction)
            {
                ApplyFriction(airFriction);
            }
        }

        if (rb.velocity.sqrMagnitude < velocityThreshold * velocityThreshold)
        {
            rb.velocity = Vector3.zero;
        }

        stepHandling();
    }

    private void OnDrawGizmos()
    {
        if (!debug) return;

        // Define the positions of the corners relative to groundCheckPosition.position
        Vector3[] cornerOffsets = new Vector3[]
        {
            new Vector3(-groundCheckBoxWidth, 0, 0), // Left
            new Vector3(groundCheckBoxWidth, 0, 0),  // Right
            new Vector3(0, 0, groundCheckBoxWidth),  // Front
            new Vector3(0, 0, -groundCheckBoxWidth)  // Back
        };

        // Set the Gizmos color for the box
        Gizmos.color = Color.red;

        // Draw lines between the corners to form the 2D box
        Gizmos.DrawLine(groundCheckPosition.position + cornerOffsets[0], groundCheckPosition.position + cornerOffsets[2]);
        Gizmos.DrawLine(groundCheckPosition.position + cornerOffsets[2], groundCheckPosition.position + cornerOffsets[1]);
        Gizmos.DrawLine(groundCheckPosition.position + cornerOffsets[1], groundCheckPosition.position + cornerOffsets[3]);
        Gizmos.DrawLine(groundCheckPosition.position + cornerOffsets[3], groundCheckPosition.position + cornerOffsets[0]);

        // Draw the ground check rays
        Gizmos.color = Color.blue;
        foreach (Vector3 offset in cornerOffsets)
        {
            Gizmos.DrawRay(groundCheckPosition.position + offset, Vector3.down * maxGroundDistance);
        }
    }

    public void Rotate(Transform transform)
    {
        rb.rotation = transform.rotation;
    }

    public void AddForce(Vector3 directionalForce)
    {
        rb.AddForce(directionalForce, ForceMode.VelocityChange);
    }

    public void AddForce(Vector3 direction, float force)
    {
        Vector3 directionalForce = direction * force;
        AddForce(directionalForce);
    }

    public void MoveLocal(Vector3 movementVector, float maxSpeed, float acceleration, float deceleration)
    {
        // Transform the movement vector to world space and normalize it
        movementVector = transform.TransformDirection(movementVector);

        MoveWorld(movementVector, maxSpeed, acceleration, deceleration);
    }

    public void MoveWorld(Vector3 directionVector, float maxSpeed, float acceleration, float deceleration)
    {
        if (directionVector.sqrMagnitude <= 0)
        {
            movement = false;
            return;
        }

        // Get the current velocity of the rigidbody
        Vector3 currentVelocity = rb.velocity;

        // Calculate the desired velocity based on the direction vector and target speed
        Vector3 desiredVelocity = directionVector.normalized * maxSpeed;

        // Maintain the current vertical velocity
        desiredVelocity.y = currentVelocity.y;

        Vector3 velocityDifference = desiredVelocity - currentVelocity;

        movement = true;

        if (velocityDifference.sqrMagnitude > Mathf.Epsilon * Mathf.Epsilon)
        {
            if (isOnSlope)
            {
                velocityDifference = Vector3.ProjectOnPlane(velocityDifference, slopeHit.normal);
            }

            // Determine the rate to use (acceleration or deceleration)
            float rateX = (Mathf.Sign(velocityDifference.x) == Mathf.Sign(directionVector.x)) ? acceleration : deceleration;
            float rateZ = (Mathf.Sign(velocityDifference.z) == Mathf.Sign(directionVector.z)) ? acceleration : deceleration;

            // Calculate new velocities with the appropriate rate
            float newVelocityX = currentVelocity.x + (velocityDifference.x * rateX * Time.fixedDeltaTime);
            float newVelocityZ = currentVelocity.z + (velocityDifference.z * rateZ * Time.fixedDeltaTime);

            // Apply the new velocity to the rigidbody
            rb.velocity = new Vector3(newVelocityX, currentVelocity.y, newVelocityZ);
        }
    }

    public void StopMovement()
    {
        rb.velocity = Vector3.zero;
    }

    public void ResetVerticalVelocity()
    {
        Vector3 currentVelocity = rb.velocity;
        rb.velocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
    }

    public void ResetHorizontalVelocity()
    {
        Vector3 currentVelocity = rb.velocity;
        rb.velocity = new Vector3(0, currentVelocity.y, 0);
    }

    public void SetGravity(bool useGravity)
    {
        if (controlGravity)
        {
            rb.useGravity = useGravity;
        }
    }

    public float GetLinearVelocityMagnitude()
    {
        return rb.velocity.magnitude;
    }

    private bool CheckGrounded()
    {
        // Define the positions of the corners relative to groundCheckPosition
        Vector3[] cornerOffsets = new Vector3[]
        {
        new Vector3(-groundCheckBoxWidth, 0, 0), // Left
        new Vector3(groundCheckBoxWidth, 0, 0),  // Right
        new Vector3(0, 0, groundCheckBoxWidth),  // Front
        new Vector3(0, 0, -groundCheckBoxWidth)  // Back
        };

        // Perform raycasts from each corner
        foreach (Vector3 offset in cornerOffsets)
        {
            if (Physics.Raycast(groundCheckPosition.position + offset, Vector3.down, maxGroundDistance, groundMask))
            {
                return true;
            }
        }

        // If none of the raycasts hit the ground, return false
        return false;
    }

    void stepHandling()
    {
        // Debug rays for step detection
        if (debug)
        {
            Debug.DrawRay(stepRayLower.transform.position, transform.TransformDirection(Vector3.forward) * 0.1f, Color.yellow);
            Debug.DrawRay(stepRayUpper.transform.position, transform.TransformDirection(Vector3.forward) * 0.2f, Color.cyan);
            Debug.DrawRay(stepRayLower.transform.position, transform.TransformDirection(1.5f, 0, 1) * 0.1f, Color.yellow);
            Debug.DrawRay(stepRayUpper.transform.position, transform.TransformDirection(1.5f, 0, 1) * 0.2f, Color.cyan);
            Debug.DrawRay(stepRayLower.transform.position, transform.TransformDirection(-1.5f, 0, 1) * 0.1f, Color.yellow);
            Debug.DrawRay(stepRayUpper.transform.position, transform.TransformDirection(-1.5f, 0, 1) * 0.2f, Color.cyan);
        }

        // Step handling logic
        RaycastHit hitLower;
        if (Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(Vector3.forward), out hitLower, 0.1f, groundMask))
        {
            RaycastHit hitUpper;
            if (!Physics.Raycast(stepRayUpper.transform.position, transform.TransformDirection(Vector3.forward), out hitUpper, 0.2f, groundMask))
            {
                rb.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
            }
        }

        RaycastHit hitLower45;
        if (Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(1.5f, 0, 1), out hitLower45, 0.1f, groundMask))
        {
            RaycastHit hitUpper45;
            if (!Physics.Raycast(stepRayUpper.transform.position, transform.TransformDirection(1.5f, 0, 1), out hitUpper45, 0.2f, groundMask))
            {
                rb.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
            }
        }

        RaycastHit hitLowerMinus45;
        if (Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(-1.5f, 0, 1), out hitLowerMinus45, 0.1f, groundMask))
        {
            RaycastHit hitUpperMinus45;
            if (!Physics.Raycast(stepRayUpper.transform.position, transform.TransformDirection(-1.5f, 0, 1), out hitUpperMinus45, 0.2f, groundMask))
            {
                rb.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
            }
        }
    }

    private bool CheckOnSlope()
    {
        // Define the positions of the corners relative to feetTransform
        Vector3[] cornerOffsets = new Vector3[]
        {
            new Vector3(-groundCheckBoxWidth, 0, 0), // Left
            new Vector3(groundCheckBoxWidth, 0, 0),  // Right
            new Vector3(0, 0, groundCheckBoxWidth),  // Front
            new Vector3(0, 0, -groundCheckBoxWidth)  // Back
        };

        // Perform raycasts from each corner
        foreach (Vector3 offset in cornerOffsets)
        {
            if (Physics.Raycast(groundCheckPosition.position + offset, Vector3.down, out slopeHit, maxGroundDistance, groundMask))
            {
                float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
                if (angle < maxSlopeAngle && angle != 0)
                {
                    return true;
                }
            }
        }

        // If none of the raycasts hit the ground, return false
        return false;
    }

    public void SetFriction(bool useFriction)
    {
        this.useFriction = useFriction;
    }

    private void ApplyFriction(float friction)
    {
        // Get the current velocity of the rigidbody
        Vector3 currentVelocity = rb.velocity;

        // Apply friction only to the horizontal components (x and z)
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

        // Apply the friction coefficient to the horizontal velocity over time
        horizontalVelocity = horizontalVelocity * friction;

        // Update the rigidbody's velocity with the new horizontal velocity and keep the vertical component unchanged
        rb.velocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
    }
}