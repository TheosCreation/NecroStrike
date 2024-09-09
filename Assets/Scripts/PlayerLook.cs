using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    private PlayerController player;
    [Header("Looking")]
    public float lookSensitivity = 1f;
    [SerializeField] private float cameraOffsetY = 0.1f;

    [Header("Camera Tilt")]
    [SerializeField] private int tiltStatus = 1;
    [SerializeField] private float tiltAmount = 2.5f;
    [SerializeField] private float tiltSmoothTime = 0.1f;

    [Header("Body Transforms")]
    [SerializeField] private Transform neckTransform;
    [SerializeField] private Transform cameraOffsetTransform;
    public Camera playerCamera;

    [Header("FOV")]
    [SerializeField] private float startFov = 60f;
    private float targetFov = 60f;
    [SerializeField] private float zoomSmoothness = 60f;

    private float currentXRotation = 0f;
    private float currentTilt = 0;
    private float tiltVelocity = 0;
    private Rigidbody rb;

    [Header("Screen Shake")]
    [SerializeField] private float shakeAmount = 1.0f;
    private float shakeMagnitude = 0.1f;
    private float shakeEndTime = 0f;
    private Vector3 originalCameraPosition;
    private Vector3 originalcameraOffsetPosition;
    private Vector3 originalNeckRotation;

    private Vector2 currentRecoilOffset = Vector2.zero;  // New field to handle recoil offset
    [SerializeField] private float recoilSmoothTime = 0.1f;  // Recoil smooth damping

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();
        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.transform.localPosition;
        }
        if (playerCamera != null)
        {
            originalcameraOffsetPosition = cameraOffsetTransform.localPosition;
        }
        
        if (neckTransform != null)
        {
            originalNeckRotation = neckTransform.localRotation.eulerAngles;
        }
    }

    private void Update()
    {
        if (neckTransform != null && playerCamera != null)
        {
            Look(); 
            UpdateRecoil();
            HandleScreenShake();
        }
    }

    private void Look()
    {
        Vector2 currentMouseDelta = InputManager.Instance.currentMouseDelta;

        // Calculate vertical rotation and clamp it
        currentXRotation -= (currentMouseDelta.y * lookSensitivity) + currentRecoilOffset.y;
        currentXRotation = Mathf.Clamp(currentXRotation, -85f, 85f);

        // Calculate horizontal rotation as a Quaternion
        Quaternion horizontalRotation = Quaternion.Euler(0f, (currentMouseDelta.x * lookSensitivity) + currentRecoilOffset.x, 0f);

        // Apply the horizontal rotation to the player body
        rb.MoveRotation(rb.rotation * horizontalRotation);

        if (tiltStatus == 1)
        {
            float targetTilt = InputManager.Instance.MovementVector.x * tiltAmount;
            currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, tiltSmoothTime);
        }

        // Rotate the camera vertically and apply tilt
        neckTransform.localRotation = Quaternion.Euler(currentXRotation, originalNeckRotation.y, originalNeckRotation.z);

        //slight offset
        Vector3 cameraPositionOffset = new Vector3(0f, currentXRotation * 0.01f * cameraOffsetY, 0f);
        cameraOffsetTransform.localPosition = originalcameraOffsetPosition + cameraPositionOffset;
        //if (currentXRotation >= 0f)
        //{
        //    Vector3 cameraPositionOffset = new Vector3(0f, currentXRotation * 0.01f * cameraOffsetY, 0f);
        //    cameraOffsetTransform.localPosition = originalcameraOffsetPosition + cameraPositionOffset;
        //}
        //else
        //{
        //    cameraOffsetTransform.localPosition = originalcameraOffsetPosition;
        //}
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * zoomSmoothness);
        playerCamera.transform.localRotation = Quaternion.Euler(0f, 0f, -currentTilt);
    }

    private void UpdateRecoil()
    {
        Weapon weapon = player.weaponHolder.currentWeapon;
        if (weapon != null)
        {
            weapon.recoil = Vector2.Lerp(weapon.recoil, Vector2.zero, Time.deltaTime / recoilSmoothTime);
            currentRecoilOffset = weapon.recoil;
        }
        else
        {
            currentRecoilOffset = Vector2.Lerp(currentRecoilOffset, Vector2.zero, Time.deltaTime / recoilSmoothTime);
        }
    }

    public void TriggerScreenShake(float duration, float magnitude)
    {
        shakeEndTime = Time.time + duration;
        shakeMagnitude = magnitude * shakeAmount;
    }

    private void HandleScreenShake()
    {
        if (Time.time < shakeEndTime)
        {
            playerCamera.transform.localPosition = originalCameraPosition + Random.insideUnitSphere * shakeMagnitude;
        }
        else
        {
            playerCamera.transform.localPosition = originalCameraPosition;
        }
    }

    public void SetZoomLevel(float zoomLevel)
    {
        targetFov = startFov * (2 - zoomLevel);
    }
}