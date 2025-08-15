using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class MovementController : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;

    [Header("Velocity Safety Measures")]
    [SerializeField] private float velocityThreshold = 0.01f;
    private float velocityThresholdSqrd = 0.0f;
    [SerializeField] private float airControl = 0.4f;

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
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float slideSpeed = 5f;
    private RaycastHit slopeHit;
    private bool isOnSlope = false;

    [Header("Debugging")]
    [SerializeField] private bool debug = false;

    public bool movement = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        velocityThresholdSqrd = velocityThreshold * velocityThreshold;
    }

    private void FixedUpdate()
    {
        // Ground check
        isGrounded = CheckGrounded();

        // Handle slope movement
        isOnSlope = CheckOnSlope();

        if (isGrounded)
        {
            SetGravity(false);
            AddForce(Vector3.down * 0.1f);

            if (!movement && useFriction)
            {
                ApplyFriction(friction);
            }

            // Handle slope sliding if the slope is too steep
            if (isOnSlope && Vector3.Angle(slopeHit.normal, Vector3.up) > maxSlopeAngle)
            {
                SlideDownSlope();
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

        // Reset velocity if below threshold
        if (rb.linearVelocity.sqrMagnitude < velocityThresholdSqrd)
        {
            rb.linearVelocity = Vector3.zero;
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
    private void StepHandling(Vector3 moveDirection)
    {
        Vector3 origin = stepRayLower.transform.position;
        Vector3 upOffset = Vector3.up * maxStepHeight;
        Vector3 forwardOffset = moveDirection.normalized * stepCheckDistance;

        // Debugging rays
        if (debug)
        {
            Debug.DrawRay(origin, Vector3.up * maxStepHeight, Color.red);
            Debug.DrawRay(origin + upOffset, moveDirection.normalized * stepCheckDistance, Color.green);
            Debug.DrawRay(origin + upOffset + forwardOffset, Vector3.down * maxStepHeight, Color.blue);
        }

        // Check if there's room to step up
        if (!Physics.Raycast(origin, Vector3.up, maxStepHeight, groundMask))
        {
            if (!Physics.Raycast(origin + upOffset, moveDirection.normalized, stepCheckDistance, groundMask))
            {
                if (Physics.Raycast(origin + upOffset + forwardOffset, Vector3.down, out RaycastHit downHit, maxStepHeight, groundMask))
                {
                    float stepHeight = downHit.point.y - transform.position.y;

                    if (stepHeight <= maxStepHeight && stepHeight > 0)
                    {
                        float stepSlopeAngle = Vector3.Angle(downHit.normal, Vector3.up);

                        if (stepSlopeAngle <= maxSlopeAngle)
                        {
                            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit slopeHit, maxGroundDistance, groundMask) ||
                                Vector3.Angle(slopeHit.normal, Vector3.up) <= maxSlopeAngle)
                            {
                                // Adjust position to step height smoothly
                                Vector3 newPosition = transform.position;
                                newPosition.y += stepHeight;

                                transform.position = Vector3.Lerp(transform.position, newPosition, stepSmooth * Time.deltaTime);
                            }
                        }
                    }
                }
            }
        }
    }

    // Ground check using raycasts
    private bool CheckGrounded()
    {
        Vector3[] cornerOffsets = new Vector3[]
        {
            new Vector3(-groundCheckBoxWidth, 0, 0),
            new Vector3(groundCheckBoxWidth, 0, 0),
            new Vector3(0, 0, groundCheckBoxWidth),
            new Vector3(0, 0, -groundCheckBoxWidth)
        };

        foreach (Vector3 offset in cornerOffsets)
        {
            if (Physics.Raycast(groundCheckPosition.position + offset, Vector3.down, maxGroundDistance, groundMask))
            {
                return true;
            }
        }
        return false;
    }

    // Check if player is on a slope
    private bool CheckOnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, maxGroundDistance, groundMask))
        {
            float slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);
            return slopeAngle > 0 && slopeAngle <= maxSlopeAngle;
        }
        return false;
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
        Vector3 currentVelocity = rb.linearVelocity;

        // Calculate the desired velocity based on the direction vector and target speed
        Vector3 desiredVelocity = directionVector.normalized * maxSpeed;

        // Maintain the current vertical velocity
        desiredVelocity.y = currentVelocity.y;

        StepHandling(desiredVelocity);

        Vector3 velocityDifference = desiredVelocity - currentVelocity;

        movement = true;

        if (velocityDifference.sqrMagnitude > Mathf.Epsilon)
        {

            // If on a slope, project movement onto the slope's surface
            if (isOnSlope)
            {
                velocityDifference = Vector3.ProjectOnPlane(velocityDifference, slopeHit.normal);
            }

            // Apply acceleration or deceleration based on movement direction
            float rateX = (Mathf.Sign(velocityDifference.x) == Mathf.Sign(directionVector.x)) ? acceleration : deceleration;
            float rateZ = (Mathf.Sign(velocityDifference.z) == Mathf.Sign(directionVector.z)) ? acceleration : deceleration;

            // Apply air control when not grounded
            if (!isGrounded)
            {
                rateX *= airControl;
                rateZ *= airControl;
            }

            // Calculate new velocity values with acceleration or deceleration rates
            float newVelocityX = Mathf.MoveTowards(currentVelocity.x, desiredVelocity.x, rateX * Time.fixedDeltaTime);
            float newVelocityZ = Mathf.MoveTowards(currentVelocity.z, desiredVelocity.z, rateZ * Time.fixedDeltaTime);

            Vector3 newVelocity = new Vector3(newVelocityX, currentVelocity.y, newVelocityZ);

            // Apply the new velocity to the rigidbody
            rb.linearVelocity = newVelocity;
        }
    }


    public void StopMovement()
    {
        rb.linearVelocity = Vector3.zero;
    }

    public void ResetVerticalVelocity()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
    }

    public void ResetHorizontalVelocity()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0, currentVelocity.y, 0);
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
        return rb.linearVelocity.magnitude;
    }

    public void SetFriction(bool useFriction)
    {
        this.useFriction = useFriction;
    }

    private void ApplyFriction(float friction)
    {
        // Get the current velocity of the rigidbody
        Vector3 currentVelocity = rb.linearVelocity;

        // Apply friction only to the horizontal components (x and z)
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

        // Apply the friction coefficient to the horizontal velocity over time
        horizontalVelocity = horizontalVelocity * friction;

        // Update the rigidbody's velocity with the new horizontal velocity and keep the vertical component unchanged
        rb.linearVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
    }

    private void SlideDownSlope()
    {
        // Project the player's movement along the slope normal
        Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, slopeHit.normal).normalized;

        // Add force to simulate sliding down the slope
        rb.linearVelocity += slideDirection * slideSpeed * Time.fixedDeltaTime;
    }
}