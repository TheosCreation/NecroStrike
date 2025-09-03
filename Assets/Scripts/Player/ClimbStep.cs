using UnityEngine;

public class ClimbStep : MonoBehaviour
{
    private InputManager inman;
    private Rigidbody rb;
    private int layerMask;
    private PlayerMovement playerMovement;
    private PlayerLook playerLook;

    [Header("Step Settings")]
    [SerializeField] private float step = 1.2f; // Adjusted for 3.5 height capsule
    [SerializeField] private float allowedAngle = 0.1f;
    [SerializeField] private float allowedSpeed = 0.1f;
    [SerializeField] private float allowedInput = 0.5f;

    [Header("Cooldown")]
    private float cooldown;
    [SerializeField] private float cooldownMax = 0.1f;

    [Header("Positioning")]
    private float deltaVertical;
    private float halfHeightCollider = 1.0f;
    private float halfHeightMinusLittle = 1.0f;
    [SerializeField] private float deltaHorizontal = 0.6f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color gizmoColor = Color.green;
    [SerializeField] private Color obstructionColor = Color.red;
    [SerializeField] private Color playerCapsuleColor = Color.blue;

    // Debug visualization variables
    private Vector3 position;
    private Vector3 gizmoPosition1;
    private Vector3 gizmoPosition2;
    private Vector3 movementDirection;
    private bool hasValidStep;
    private Vector3 lastContactNormal;
    private Vector3 lastStepPosition;
    private bool foundObstruction;
    private Vector3[] obstructionPositions = new Vector3[4];
    private float playerRadius;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        playerLook = GetComponent<PlayerLook>();
        layerMask = LayerMaskDefaults.GetLayer(LMD.Environment) | LayerMaskDefaults.GetLayer(LMD.Default);
    }

    private void FixedUpdate()
    {
        if (cooldown <= 0f)
        {
            cooldown = 0f;
        }
        else
        {
            cooldown -= Time.deltaTime;
        }
        movementDirection = playerMovement.GetMovementDirection();

        // Reset debug flags
        hasValidStep = false;
        foundObstruction = false;
    }

    private void OnCollisionStay(Collision collisionInfo)
    {
        halfHeightCollider = playerMovement.playerCollider.height / 2f;
        halfHeightMinusLittle = halfHeightCollider - 0.3f;
        playerRadius = ((CapsuleCollider)playerMovement.playerCollider).radius;

        if (playerMovement.gc.forcedOff > 0 ||
            layerMask != (layerMask | (1 << collisionInfo.collider.gameObject.layer)) ||
            cooldown != 0f)
        {
            return;
        }

        ContactPoint[] contacts = collisionInfo.contacts;
        for (int i = 0; i < contacts.Length; i++)
        {
            ContactPoint contactPoint = contacts[i];

            // Store for debugging
            lastContactNormal = contactPoint.normal;

            // Check conditions for stepping
            bool speedCheck = allowedSpeed == 0f || rb.linearVelocity.y < allowedSpeed;
            bool inputCheck = Vector3.Dot(movementDirection, -Vector3.ProjectOnPlane(contactPoint.normal, Vector3.up).normalized) > allowedInput;
            bool angleCheck = Mathf.Abs(Vector3.Dot(Vector3.up, contactPoint.normal)) < allowedAngle;

            if (!speedCheck || cooldown != 0f || !inputCheck || !angleCheck)
            {
                continue;
            }

            // Calculate step position using player collider dimensions
            position = transform.position + Vector3.up * step;
            if (playerMovement.isSliding || playerMovement.isCrouching)
            {
                //position += Vector3.up * (halfHeightMinusLittle - 0.2f);
            }

            // Store positions for gizmo drawing
            lastStepPosition = position;
            hasValidStep = true;

            // Check for obstructions above step target - using player collider dimensions
            Vector3 capsuleBottom1 = position - Vector3.up * step;
            Vector3 capsuleTop1 = position + Vector3.up * halfHeightMinusLittle;

            gizmoPosition1 = capsuleBottom1;
            gizmoPosition2 = capsuleTop1;

            Collider[] array = Physics.OverlapCapsule(
                capsuleBottom1,
                capsuleTop1,
                playerRadius * 0.999f, // Use actual player radius, slightly smaller to avoid edge cases
                layerMask,
                QueryTriggerInteraction.Ignore
            );

            // Check for obstructions in step path - using player collider dimensions
            Vector3 stepDirection = Vector3.ProjectOnPlane(contactPoint.normal, Vector3.up);
            Vector3 capsuleBottom2 = position - Vector3.up * halfHeightMinusLittle - stepDirection * 0.25f;
            Vector3 capsuleTop2 = position + Vector3.up * halfHeightMinusLittle - stepDirection * 0.25f;

            obstructionPositions[0] = capsuleBottom2;
            obstructionPositions[1] = capsuleTop2;
            obstructionPositions[2] = capsuleBottom1;
            obstructionPositions[3] = capsuleTop1;

            Collider[] array2 = Physics.OverlapCapsule(
                capsuleBottom2,
                capsuleTop2,
                playerRadius, // Use actual player radius
                layerMask,
                QueryTriggerInteraction.Ignore
            );

            if (array.Length != 0 || array2.Length != 0)
            {
                foundObstruction = true;
                continue;
            }

            // Perform the step
            cooldown = cooldownMax;
            Vector3 vector = playerLook.playerCamera.transform.position;

            Vector3 raycastStart = position - Vector3.up * halfHeightCollider - Vector3.ProjectOnPlane(contactPoint.normal, Vector3.up).normalized * deltaHorizontal;

            if (Physics.Raycast(raycastStart, -Vector3.up, out var hitInfo, step, layerMask, QueryTriggerInteraction.Ignore))
            {
                rb.linearVelocity -= new Vector3(0f, rb.linearVelocity.y, 0f);
                transform.position += Vector3.up * (step + deltaVertical - hitInfo.distance) - Vector3.ProjectOnPlane(contactPoint.normal, Vector3.up).normalized * deltaHorizontal;
                rb.linearVelocity = -collisionInfo.relativeVelocity;
            }
            else
            {
                transform.position += Vector3.up * (step + deltaVertical) - Vector3.ProjectOnPlane(contactPoint.normal, Vector3.up).normalized * deltaHorizontal;
                rb.linearVelocity = -collisionInfo.relativeVelocity;
            }

            //playerLook.playerCamera.transform.position = vector;
            //playerLook.defaultPos = playerLook.playerCamera.transform.localPosition;

            break; // Exit after successful step
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Get current collider dimensions
        if (playerMovement?.playerCollider != null)
        {
            halfHeightCollider = playerMovement.playerCollider.height / 2f;
            playerRadius = ((CapsuleCollider)playerMovement.playerCollider).radius;
        }

        // Draw current player capsule
        Gizmos.color = playerCapsuleColor;
        Vector3 playerBottom = transform.position - Vector3.up * (halfHeightCollider - playerRadius);
        Vector3 playerTop = transform.position + Vector3.up * (halfHeightCollider - playerRadius);
        DrawWireCapsule(playerBottom, playerTop, playerRadius);

        if (hasValidStep)
        {
            // Draw step target position
            Gizmos.color = foundObstruction ? obstructionColor : gizmoColor;
            Vector3 stepBottom = gizmoPosition1;
            Vector3 stepTop = gizmoPosition2;

            // Draw step target capsule
            DrawWireCapsule(stepBottom + Vector3.up * playerRadius, stepTop - Vector3.up * playerRadius, playerRadius);

            // Draw movement direction arrow
            Gizmos.color = Color.yellow;
            Vector3 arrowStart = transform.position + Vector3.up * 0.1f;
            Vector3 arrowEnd = arrowStart + movementDirection * 0.5f;
            Gizmos.DrawLine(arrowStart, arrowEnd);
            DrawArrowHead(arrowEnd, movementDirection);

            // Draw contact normal
            Gizmos.color = Color.cyan;
            Vector3 normalStart = transform.position;
            Vector3 normalEnd = normalStart + lastContactNormal * 0.5f;
            Gizmos.DrawLine(normalStart, normalEnd);
            DrawArrowHead(normalEnd, lastContactNormal);

            if (foundObstruction)
            {
                // Draw obstruction check areas
                Gizmos.color = obstructionColor;
                // Draw first obstruction check
                DrawWireCapsule(obstructionPositions[0] + Vector3.up * playerRadius,
                               obstructionPositions[1] - Vector3.up * playerRadius, playerRadius);

                // Draw second obstruction check
                Gizmos.color = Color.magenta;
                DrawWireCapsule(obstructionPositions[2] + Vector3.up * playerRadius,
                               obstructionPositions[3] - Vector3.up * playerRadius, playerRadius);
            }
        }

        // Draw cooldown indicator
        if (cooldown > 0f)
        {
            Gizmos.color = Color.red;
            float cooldownRatio = cooldown / cooldownMax;
            Vector3 cubePos = transform.position + Vector3.up * (halfHeightCollider + 0.2f);
            Gizmos.DrawWireCube(cubePos, Vector3.one * 0.1f * cooldownRatio);
        }

        // Draw velocity vector
        Gizmos.color = Color.magenta;
        if (rb != null)
        {
            Vector3 velocityEnd = transform.position + rb.linearVelocity * 0.2f;
            Gizmos.DrawLine(transform.position, velocityEnd);
            if (rb.linearVelocity.magnitude > 0.1f)
            {
                DrawArrowHead(velocityEnd, rb.linearVelocity.normalized);
            }
        }
    }

    private void DrawWireCapsule(Vector3 bottom, Vector3 top, float radius)
    {
        // Draw spheres at ends
        Gizmos.DrawWireSphere(bottom, radius);
        Gizmos.DrawWireSphere(top, radius);

        // Draw connecting lines
        Gizmos.DrawLine(bottom + Vector3.forward * radius, top + Vector3.forward * radius);
        Gizmos.DrawLine(bottom - Vector3.forward * radius, top - Vector3.forward * radius);
        Gizmos.DrawLine(bottom + Vector3.right * radius, top + Vector3.right * radius);
        Gizmos.DrawLine(bottom - Vector3.right * radius, top - Vector3.right * radius);
    }

    private void DrawArrowHead(Vector3 position, Vector3 direction)
    {
        if (direction.magnitude < 0.01f) return;

        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized * 0.05f;
        Vector3 up = Vector3.Cross(direction, right).normalized * 0.05f;

        Gizmos.DrawLine(position, position - direction.normalized * 0.1f + right);
        Gizmos.DrawLine(position, position - direction.normalized * 0.1f - right);
        Gizmos.DrawLine(position, position - direction.normalized * 0.1f + up);
        Gizmos.DrawLine(position, position - direction.normalized * 0.1f - up);
    }
}