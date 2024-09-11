using UnityEngine;

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
    [SerializeField] private float targetFov = 60f;

    [Header("Aiming/Zoom")]
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
    private Vector3 cameraTargetLocalPosition;

    private Vector2 currentRecoilOffset = Vector2.zero;
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

        ResetZoomLevel();
    }

    private void Update()
    {
        if (neckTransform != null && playerCamera != null)
        {
            Look(); 
            UpdateRecoil();
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
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * zoomSmoothness);
        playerCamera.transform.localRotation = Quaternion.Euler(0f, 0f, -currentTilt); 
        playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, cameraTargetLocalPosition, Time.deltaTime * zoomSmoothness) + (Time.time < shakeEndTime ? Random.insideUnitSphere * shakeMagnitude : Vector3.zero);
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

    public void ResetZoomLevel()
    {
        cameraTargetLocalPosition = originalCameraPosition;
        targetFov = startFov;
    }

    public void SetZoomLevel(float zoomLevel, float cameraZoomZ)
    {
        targetFov = startFov * (2 - zoomLevel);
        cameraTargetLocalPosition = new Vector3(originalCameraPosition.x, originalCameraPosition.y, cameraZoomZ);
    }
}