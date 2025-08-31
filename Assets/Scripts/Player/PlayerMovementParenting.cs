using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMovementParenting : MonoBehaviour
{
    public Transform deltaReceiver;

    private Vector3 lastTrackedPos;

    private float lastAngle;

    private Transform playerTracker;

    [HideInInspector]
    public bool lockParent;

    private Vector3 teleportLockDelta;

    private Rigidbody rb;

    private List<Transform> trackedObjects = new List<Transform>();

    public Vector3 currentDelta { get; private set; }

    public List<Transform> TrackedObjects => trackedObjects;

    private PlayerMovement playerMovement;
    private PlayerLook playerLook;

    private void Awake()
    {
        if (deltaReceiver == null)
        {
            deltaReceiver = base.transform;
        }
        if (!rb)
        {
            rb = GetComponent<Rigidbody>();
        }

        playerLook = GetComponentInChildren<PlayerLook>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void FixedUpdate()
    {
        currentDelta = Vector3.zero;
        if (playerTracker == null)
        {
            return;
        }
        if (!playerMovement.enabled)
        {
            DetachPlayer();
            return;
        }
        Vector3 position = playerTracker.transform.position;
        float y = playerTracker.transform.eulerAngles.y;
        Vector3 vector = position - lastTrackedPos;
        lastTrackedPos = position;

        float num = y - lastAngle;
        lastAngle = y;
        float num2 = Mathf.Abs(num);
        if (num2 > 180f)
        {
            num2 = 360f - num2;
        }
        if (num2 > 5f)
        {
            DetachPlayer();
            return;
        }
        if (vector.magnitude > 2f)
        {
            DetachPlayer();
            return;
        }
        if ((bool)rb)
        {
            rb.MovePosition(rb.position + vector);
        }
        else
        {
            deltaReceiver.position += vector;
        }
        playerTracker.transform.position = deltaReceiver.position;
        lastTrackedPos = playerTracker.transform.position;
        currentDelta = vector;
        if (playerMovement.gc.touchingGround)
        {
            playerLook.currentYRotation += num;
        }
    }

    public bool IsPlayerTracking()
    {
        return playerTracker != null;
    }

    public bool IsObjectTracked(Transform other)
    {
        return trackedObjects.Contains(other);
    }

    public void AttachPlayer(Transform other)
    {
        if (!lockParent)
        {
            trackedObjects.Add(other);
            GameObject obj = new GameObject("Player Position Proxy");
            obj.transform.parent = other;
            obj.transform.position = deltaReceiver.position;
            obj.transform.rotation = deltaReceiver.rotation;
            GameObject gameObject = obj;
            lastTrackedPos = gameObject.transform.position;
            lastAngle = gameObject.transform.eulerAngles.y;
            if (playerTracker != null)
            {
                Object.Destroy(playerTracker.gameObject);
            }
            playerTracker = gameObject.transform;
            ClearTrackedNulls();
        }
    }

    public void DetachPlayer([CanBeNull] Transform other = null)
    {
        if (lockParent)
        {
            return;
        }
        if (other == null)
        {
            trackedObjects.Clear();
        }
        else
        {
            trackedObjects.Remove(other);
        }
        if (trackedObjects.Count == 0)
        {
            Object.Destroy(playerTracker.gameObject);
            playerTracker = null;
            return;
        }
        ClearTrackedNulls();
        if (playerTracker != null && trackedObjects.Count > 0)
        {
            playerTracker.SetParent(trackedObjects.First());
        }
    }

    private void ClearTrackedNulls()
    {
        for (int num = trackedObjects.Count - 1; num >= 0; num--)
        {
            if (trackedObjects[num] == null)
            {
                trackedObjects.RemoveAt(num);
            }
        }
    }

    public void LockMovementParent(bool locked)
    {
        lockParent = locked;
    }

    public void LockMovementParentTeleport(bool locked)
    {
        if ((bool)playerTracker)
        {
            if (locked)
            {
                teleportLockDelta = lastTrackedPos - playerTracker.position;
            }
            if (lockParent && !locked)
            {
                lastTrackedPos = playerTracker.position - teleportLockDelta;
            }
        }
        else
        {
            teleportLockDelta = lastTrackedPos;
        }
        lockParent = locked;
    }
}
