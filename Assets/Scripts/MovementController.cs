using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class MovementController : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;

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
    [SerializeField] GameObject stepRayLower;
    [SerializeField] float maxStepHeight = 0.3f;
    [SerializeField] float stepCheckDistance = 0.3f;
    [SerializeField] float stepSmooth = 2f;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle = 45.0f;
    [SerializeField] private float maxDownwardSlopeChangeAngle = 30.0f;
    [SerializeField] private float verticalOffset = 0.1f;
    [SerializeField] private float deltaTimeIntoFuture = 0.5f;
    [SerializeField] private float downDetectionDepth = 1.0f;
    [SerializeField] private float secondaryNoGroundingCheckDistance = 0.1f;
    private RaycastHit slopeHit;
    public bool isOnSlope = false;

    [Header("Debugging")]
    [SerializeField] private bool debug = false;

    public bool movement = false;

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
            AddForce(Vector3.down * 0.01f);
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

    }
    private void Update_PreventGroundingFromFutureSlopeChange(Vector3 movementDirection)
    {
        // Get the character's movement direction
        Vector3 origin = groundCheckPosition.position + Vector3.up * verticalOffset;

        // Draw future slope change check rays
        Debug.DrawRay(origin, movementDirection.normalized * deltaTimeIntoFuture * rb.velocity.magnitude, Color.green);

        Debug.DrawRay(origin + movementDirection.normalized * deltaTimeIntoFuture * rb.velocity.magnitude, Vector3.down * (downDetectionDepth + verticalOffset), Color.cyan);
        Debug.DrawRay(origin + movementDirection.normalized * (deltaTimeIntoFuture * rb.velocity.magnitude + secondaryNoGroundingCheckDistance), Vector3.down * (downDetectionDepth + verticalOffset), Color.cyan);

        // A. Raycast in the movement direction to detect slope change
        Ray movementRay = new Ray(origin, movementDirection.normalized);
        bool hitMovement = Physics.Raycast(movementRay, out RaycastHit movementHit, deltaTimeIntoFuture * rb.velocity.magnitude, groundMask);

        if (hitMovement)
        {
            float futureSlopeChangeAngle = Vector3.Angle(movementHit.normal, Vector3.up);
            if (futureSlopeChangeAngle > maxSlopeAngle)
            {
                // Character is moving towards invalid ground
                return;
            }
        }

        // B. First downward raycast in the direction of movement
        Ray downwardRay = new Ray(origin + movementDirection.normalized * deltaTimeIntoFuture * rb.velocity.magnitude, Vector3.down);
        bool hitDown = Physics.Raycast(downwardRay, out RaycastHit downHit, downDetectionDepth + verticalOffset, groundMask);

        if (hitDown)
        {
            float futureSlopeChangeAngle = Vector3.Angle(downHit.normal, Vector3.up);
            if (futureSlopeChangeAngle > maxDownwardSlopeChangeAngle)
            {
                // Prevent grounding on downward slope change
                return;
            }
        }

        // C. Check for further no-grounding conditions in the movement direction
        Ray secondaryDownwardRay = new Ray(origin + movementDirection.normalized * (deltaTimeIntoFuture * rb.velocity.magnitude + secondaryNoGroundingCheckDistance), Vector3.down);
        bool hitSecondaryDown = Physics.Raycast(secondaryDownwardRay, out RaycastHit secondaryDownHit, downDetectionDepth + verticalOffset, groundMask);

        if (!hitSecondaryDown)
        {
            // No grounding detected further ahead, prevent grounding
            return;
        }

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


        StepHandling(directionVector);
        Update_PreventGroundingFromFutureSlopeChange(directionVector);
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
    private void StepHandling(Vector3 moveDirection)
    {
        // Step detection rays start points
        Vector3 origin = stepRayLower.transform.position;
        Vector3 upOffset = Vector3.up * maxStepHeight; // Position offset for step height
        Vector3 forwardOffset = moveDirection.normalized * stepCheckDistance; // Position offset for step forward check

        // Draw debug rays for step detection
        if (debug)
        {
            // Upward raycast to check for space above the character
            Debug.DrawRay(origin, Vector3.up * maxStepHeight, Color.red);

            // Forward raycast after moving up
            Debug.DrawRay(origin + upOffset, moveDirection.normalized * stepCheckDistance, Color.green);

            // Downward raycast to check for step height
            Debug.DrawRay(origin + upOffset + forwardOffset, Vector3.down * maxStepHeight, Color.blue);
        }

        // Cast upwards to check if there is room to step up
        if (!Physics.Raycast(origin, Vector3.up, maxStepHeight, groundMask))
        {
            // No obstruction above, potential step
            // Cast forward to check if the character can move forward after stepping up
            if (!Physics.Raycast(origin + upOffset, moveDirection.normalized, stepCheckDistance, groundMask))
            {
                // No obstruction in the movement direction, now check downward to detect step height
                if (Physics.Raycast(origin + upOffset + forwardOffset, Vector3.down, out RaycastHit downHit, maxStepHeight, groundMask))
                {
                    // Calculate the height difference for stepping up
                    float stepHeight = downHit.point.y - transform.position.y;

                    // Ensure the height difference is within the step height limit
                    if (stepHeight <= maxStepHeight && stepHeight > 0)
                    {
                        // Check the slope angle of the surface we're stepping onto
                        float stepSlopeAngle = Vector3.Angle(downHit.normal, Vector3.up);

                        // Ensure the slope angle is within the allowed limit
                        if (stepSlopeAngle <= maxSlopeAngle)
                        {
                            // Smoothly move the character up to the step height
                            Vector3 newPosition = transform.position;
                            newPosition.y += stepHeight;
                            transform.position = Vector3.Lerp(transform.position, newPosition, stepSmooth * Time.deltaTime);
                        }
                    }
                }
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