using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class GroundCheck : MonoBehaviour
{
    public bool slopeCheck = false;
    public bool onGround = false;
    public bool touchingGround = false;

    public TimeSince sinceLastGrounded;
    public int forcedOff = 0; // used to push the player upwards
    public List<Collider> cols = new List<Collider>();

    private PlayerMovement playerMovement;
    private PlayerController playerController;
    private PlayerMovementParenting movementParenting;
    public CustomGroundProperties currentGroundProperties;

    private void Start()
    {
        playerMovement = transform.parent.GetComponent<PlayerMovement>();
        movementParenting = transform.parent.GetComponent<PlayerMovementParenting>();
        playerController = transform.parent.GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (forcedOff > 0)
        {
            onGround = false;
        }
        else if (onGround != touchingGround)
        {
            onGround = touchingGround;
        }
        if (onGround)
        {
            sinceLastGrounded = 0f;
        }

        if (cols.Count > 0)
        {
            for (int num = cols.Count - 1; num >= 0; num--)
            {
                if (!ColliderIsStillUsable(cols[num]))
                {
                    cols.RemoveAt(num);
                }
            }
        }
        if (touchingGround && cols.Count == 0)
        {
            touchingGround = false;
        }
    }
    private void Bounce(float bounceAmount)
    {
        Vector3 position2 = playerController.playerLook.playerCamera.transform.position;
        playerController.playerLook.playerCamera.transform.position = position2;
        playerController.playerLook.defaultPos = playerController.playerLook.playerCamera.transform.localPosition;
        playerMovement.Jump(bounceAmount);

    }

    private void OnTriggerEnter(Collider other)
    {
        if (ColliderIsCheckable(other) && !cols.Contains(other))
        {
            cols.Add(other);
            touchingGround = true;
            if (!slopeCheck && (other.gameObject.CompareTag("Moving")) && other.attachedRigidbody != null && !movementParenting.IsObjectTracked(other.transform))
            {
                movementParenting.AttachPlayer(other.transform);
            }
            if (other.TryGetComponent<CustomGroundProperties>(out CustomGroundProperties groundProperties))
            {
                currentGroundProperties = groundProperties;

                if (currentGroundProperties.bounceAmount > 0 && !playerMovement.isJumping)
                {
                    Bounce(currentGroundProperties.bounceAmount);
                }
            }
            else
            {
                currentGroundProperties = null;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (ColliderIsCheckable(other) && cols.Contains(other))
        {
            if (cols.IndexOf(other) == cols.Count - 1)
            {
                cols.Remove(other);
                if (cols.Count > 0)
                {
                    for (int num = cols.Count - 1; num >= 0; num--)
                    {
                        if (ColliderIsStillUsable(cols[num]))
                        {
                            // can collect custom ground properties here
                            break;
                        }
                        cols.RemoveAt(num);
                    }
                }
            }
            else
            {
                cols.Remove(other);
            }
            if (cols.Count == 0)
            {
                touchingGround = false;
                //reset the ground properties here
            }
            if (!slopeCheck && (other.gameObject.CompareTag("Moving")) && movementParenting.IsObjectTracked(other.transform))
            {
                movementParenting.DetachPlayer(other.transform);
            }
        }
    }

    public bool ColliderIsCheckable(Collider col)
    {
        if (!col.isTrigger && !col.gameObject.CompareTag("Slippery"))
        {
            if (!LayerMaskDefaults.IsMatchingLayer(col.gameObject.layer, LMD.Environment))
            {
                return true;
            }
            return true;
        }
        return false;
    }
    public bool ColliderIsStillUsable(Collider col)
    {
        if (!(col == null) && col.enabled && !col.isTrigger && col.gameObject.activeInHierarchy)
        {
            return true;
        }
        return false;
    }


    public void ForceOff()
    {
        onGround = false;
        forcedOff++;
    }

    public void StopForceOff()
    {
        forcedOff--;
        if (forcedOff <= 0)
        {
            onGround = touchingGround;
        }
    }
}
