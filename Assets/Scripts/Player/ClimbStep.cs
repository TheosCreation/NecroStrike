using UnityEngine;

public class ClimbStep : MonoBehaviour
{
    private InputManager inman;

    private Rigidbody rb;

    private int layerMask;

    private PlayerMovement playerMovement;
    private PlayerLook playerLook;

    [SerializeField] private float step = 2.1f;

    [SerializeField] private float allowedAngle = 0.1f;

    [SerializeField] private float allowedSpeed = 0.1f;

    [SerializeField] private float allowedInput = 0.5f;

    private float cooldown;

    [SerializeField] private float cooldownMax = 0.1f;

    private float deltaVertical;

   [SerializeField] private float deltaHorizontal = 0.6f;

    private Vector3 position;

    private Vector3 gizmoPosition1;

    private Vector3 gizmoPosition2;

    private Vector3 movementDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        playerLook = GetComponent<PlayerLook>();
        layerMask = LayerMaskDefaults.GetLayer(LMD.Environment);
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
    }

    private void OnCollisionStay(Collision collisionInfo)
    {
        if (playerMovement.gc.forcedOff > 0 || layerMask != (layerMask | (1 << collisionInfo.collider.gameObject.layer)) || cooldown != 0f)
        {
            return;
        }
        ContactPoint[] contacts = collisionInfo.contacts;
        for (int i = 0; i < contacts.Length; i++)
        {
            ContactPoint contactPoint = contacts[i];
            if ((!(rb.linearVelocity.y < allowedSpeed) && allowedSpeed != 0f) || cooldown != 0f || (!(Vector3.Dot(movementDirection, -Vector3.ProjectOnPlane(contactPoint.normal, Vector3.up).normalized) > allowedInput)) || !(Mathf.Abs(Vector3.Dot(Vector3.up, contactPoint.normal)) < allowedAngle))
            {
                continue;
            }
            position = transform.position + Vector3.up * step + Vector3.up * 0.25f;
            if (playerMovement.isSliding)
            {
                position += Vector3.up * 1.125f;
            }
            Collider[] array = Physics.OverlapCapsule(position - Vector3.up * step, position + Vector3.up * 1.25f, 0.499999f, layerMask, QueryTriggerInteraction.Ignore);
            Collider[] array2 = Physics.OverlapCapsule(position - Vector3.up * 1.25f - Vector3.ProjectOnPlane(contactPoint.normal, Vector3.up) * 0.5f, position + Vector3.up * 1.25f - Vector3.ProjectOnPlane(contactPoint.normal, Vector3.up) * 0.5f, 0.5f, layerMask, QueryTriggerInteraction.Ignore);
            if (array.Length != 0 || array2.Length != 0)
            {
                continue;
            }
            cooldown = cooldownMax;
            Vector3 vector = playerLook.playerCamera.transform.position;
            float num = 1.75f;
            if (Physics.Raycast(position - Vector3.up * num - Vector3.ProjectOnPlane(contactPoint.normal, Vector3.up).normalized * deltaHorizontal, -Vector3.up, out var hitInfo, step, layerMask, QueryTriggerInteraction.Ignore))
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
            playerLook.playerCamera.transform.position = vector;
            playerLook.defaultPos = playerLook.playerCamera.transform.localPosition;
        }
    }
}
