using UnityEngine;
using System.Collections;
using Vector3 = UnityEngine.Vector3;

public class PlayerLook : MonoBehaviour //MonoPrefsBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerController player;

    //private Rigidbody rb;
    [Header("Looking")]
    [SerializeField] private float cameraOffsetY = 0.1f;
    [SerializeField] private float maxRotZCamera = 87f;
    public bool mouseInvertX = false;
    public bool mouseInvertY = false;

    [Header("Player Options")]
    public float lookSensitivity = 50f;
    public bool tiltStatus = true;
    public float shakeAmount = 1.0f;

    [Header("Camera Bobbing")]
    [SerializeField] private float offsetY = 0.05f;
    [SerializeField] private float bobbingSpeedFactor = 15f;
    [Header("Camera Tilt")]
    [SerializeField] private float tiltAmount = 2.5f;
    [SerializeField] private float tiltSmoothTime = 0.1f;


    [Header("Zooming")]
    public float zoomSmoothness = 1.0f;
    public float defaultFov;
    public float zoomTarget;
    public bool zooming = false;

    [Header("Body Transforms")]
    [SerializeField] private Transform neckTransform;
    [SerializeField] private Transform cameraOffsetTransform;
    public Camera playerCamera;


    [Header("FOV")]
    [SerializeField] private float targetFov = 60f;

    private float currentTilt = 0;
    private float tiltVelocity = 0;

    [Header("Screen Shake")]
    public float cameraShaking = 0f;
    private float screenShake;

    public Vector3 originalPos;
    public Vector3 defaultPos;
    public Vector3 defaultTarget;
    public Vector3 targetPos;
    public float currentXRotation;
    public float currentYRotation;

    private Vector3 originalCameraPosition;
    private Vector3 originalcameraOffsetPosition;
    private Vector3 originalNeckRotation;
    private Vector3 cameraTargetLocalPosition;

    private Vector2 currentRecoilOffset = Vector2.zero;
    [SerializeField] private float recoilSmoothTime = 0.1f;  // Recoil smooth damping
    private void Awake()
    {
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
        playerMovement = GetComponent<PlayerMovement>();
        player = GetComponent<PlayerController>();

        originalPos = playerCamera.transform.localPosition;
        defaultPos = playerCamera.transform.localPosition;
        defaultTarget = playerCamera.transform.localPosition;
        targetPos = new Vector3(defaultPos.x, defaultPos.y - offsetY, defaultPos.z);
        defaultFov = playerCamera.fieldOfView;

        ResetZoomLevel();
    }


    //public override void OnPrefChanged(OptionType option, object value)
    //{
    //    switch (option)
    //    {
    //        case OptionType.PlayerSensitivity:
    //            lookSensitivity = (float)value;
    //            break;
    //        case OptionType.FieldOfView:
    //            defaultFov = (float)value;
    //            playerCamera.fieldOfView = defaultFov;
    //            break;
    //        case OptionType.CameraTilt:
    //            tiltStatus = (bool)value;
    //            break;
    //        case OptionType.ScreenShake:
    //            screenShake = (float)value;
    //            break;
    //        case OptionType.InvertX:
    //            mouseInvertX = (bool)value;
    //            break;
    //        case OptionType.InvertY:
    //            mouseInvertY = (bool)value;
    //            break;
    //    }
    //}

    private void Update()
    {
        //if(InputManager.Instance.PlayerInput.F.WasPerformedThisFrame)
        //{
        //
        //    PauseManager.Instance.StopTimeFor(0.5f);
        //}
        if (playerCamera != null)
        {
            Look();
            UpdateRecoil();
        }


        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * zoomSmoothness);
    }

    // Sets FOV to the given value
    public void SetFOV(float newFOV)
    {
        playerCamera.fieldOfView = newFOV;
    }

    // Sets the FOV relative to default
    public void SetDeltaFOV(float deltaFOV)
    {
        zooming = true;
        playerCamera.fieldOfView = defaultFov + deltaFOV;
    }

    // Resets FOV to the default setting
    public void ResetFOV()
    {
        zooming = false;
        playerCamera.fieldOfView = defaultFov;
    }

    private void Look()
    {
        if(Time.deltaTime == 0) return;
        Vector2 currentMouseDelta = InputManager.Instance.currentMouseDelta;


        // Calculate horizontal rotation as a Quaternion

        float zoomDifference = 1f;
        if (zooming)
        {
            zoomDifference = playerCamera.fieldOfView / defaultFov;
        }
        if (!mouseInvertX)
        {
            currentXRotation += currentMouseDelta.x * (lookSensitivity / 100f) * zoomDifference;
        }
        else
        {
            currentXRotation -= currentMouseDelta.x * (lookSensitivity / 100f) * zoomDifference;
        }
        if (currentXRotation > 180f)
        {
            currentXRotation -= 360f;
        }
        else if (currentXRotation < -180f)
        {
            currentXRotation += 360f;
        }

        if (Time.timeScale > 0f)
        {
            playerMovement.rb.MoveRotation(Quaternion.Euler(0f, currentXRotation + currentRecoilOffset.x, 0f));
        }
        else
        {
            playerMovement.transform.rotation = Quaternion.Euler(0f, currentXRotation + currentRecoilOffset.x, 0f);
        }
        if (tiltStatus)
        {
            float targetTilt = 0f;
            targetTilt = playerMovement.GetInput().x * tiltAmount;

            currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, tiltSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }
        else
        {
            currentTilt = 0f;
        }

        // Calculate vertical rotation and clamp it
        if (!mouseInvertY)
        {
            currentYRotation -= currentMouseDelta.y * (lookSensitivity / 100f) * zoomDifference;
        }
        else
        {
            currentYRotation += currentMouseDelta.y * (lookSensitivity / 100f) * zoomDifference;
        }
        currentYRotation = Mathf.Clamp(currentYRotation, -maxRotZCamera, maxRotZCamera);

        neckTransform.localRotation = Quaternion.Euler(currentYRotation + originalNeckRotation.x,
                                                           originalNeckRotation.y,
                                                           originalNeckRotation.z);

        Vector3 cameraPositionOffset = new Vector3(0f, currentYRotation * 0.01f * cameraOffsetY, 0f);
        cameraOffsetTransform.localPosition = originalcameraOffsetPosition + cameraPositionOffset;

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * zoomSmoothness);
        playerCamera.transform.localRotation = Quaternion.Euler(0f, 0f, -currentTilt);
        //playerCamera.transform.localRotation = Quaternion.Euler(-currentYRotation, playerCamera.transform.localRotation.y, -currentTilt);

        if (cameraShaking > 0f)
        {
            if (PauseManager.Instance.isPaused)
            {
                playerCamera.transform.localPosition = defaultPos;
            }
            else
            {
                Vector3 basePosition = playerCamera.transform.parent.position + defaultPos;
                Vector3 shakeOffset = Vector3.zero;

                if (cameraShaking > 1f)
                {
                    // Stronger shake effect
                    shakeOffset += playerCamera.transform.right * UnityEngine.Random.Range(-1f, 1f) * shakeAmount;
                    shakeOffset += playerCamera.transform.up * UnityEngine.Random.Range(-1f, 1f) * shakeAmount;
                }
                else
                {
                    // Weaker shake effect
                    shakeOffset += playerCamera.transform.right * (cameraShaking * UnityEngine.Random.Range(-0.5f, 0.5f)) * shakeAmount;
                    shakeOffset += playerCamera.transform.up * (cameraShaking * UnityEngine.Random.Range(-0.5f, 0.5f)) * shakeAmount;
                }

                Vector3 targetPosition = basePosition + shakeOffset;

                // Check for collisions
                if (Physics.Raycast(basePosition, (targetPosition - basePosition).normalized, out RaycastHit hitInfo, Vector3.Distance(targetPosition, basePosition) + 0.4f, LayerMaskDefaults.GetLayer(LMD.Environment)))
                {
                    playerCamera.transform.position = hitInfo.point - (targetPosition - basePosition).normalized * 0.1f;
                }
                else
                {
                    playerCamera.transform.position = targetPosition;
                }

                // Reduce shake intensity over time
                cameraShaking = Mathf.Max(0f, cameraShaking - Time.unscaledDeltaTime * 3f);
            }
        }
        //if (zooming)
        //{
        //    playerCamera.fieldOfView = Mathf.MoveTowards(playerCamera.fieldOfView, zoomTarget, Time.deltaTime * 300f);
        //}
        //else
        //{
        //    playerCamera.fieldOfView = Mathf.MoveTowards(playerCamera.fieldOfView, defaultFov, Time.deltaTime * 300f);
        //}

        if (defaultPos != defaultTarget)
        {
            // Move the default position towards the target at a rate based on the distance.
            defaultPos = Vector3.MoveTowards(defaultPos, defaultTarget, ((defaultTarget - defaultPos).magnitude + 0.5f) * Time.unscaledDeltaTime * 10f);
        }

        playerCamera.transform.localPosition = new Vector3(
                Mathf.MoveTowards(playerCamera.transform.localPosition.x, cameraTargetLocalPosition.x, Time.unscaledDeltaTime),
                Mathf.MoveTowards(playerCamera.transform.localPosition.y, cameraTargetLocalPosition.y, Time.unscaledDeltaTime),
                Mathf.MoveTowards(playerCamera.transform.localPosition.z, cameraTargetLocalPosition.z, Time.unscaledDeltaTime)
        );

        if (!playerMovement.isActiveAndEnabled || cameraShaking > 0f)
        {
            return;
        }
    }


    private void UpdateRecoil()
    {
        Weapon weapon = player.weaponHolder.currentWeapon;
        if (weapon != null)
        {
            weapon.recoil = Vector2.Lerp(weapon.recoil, Vector2.zero, Time.deltaTime / recoilSmoothTime);
            currentRecoilOffset = weapon.recoil * 0.15f;
        }
        else
        {
            currentRecoilOffset = Vector2.Lerp(currentRecoilOffset, Vector2.zero, Time.deltaTime / recoilSmoothTime);
        }
    }

    public void ResetZoomLevel()
    {
        cameraTargetLocalPosition = originalCameraPosition;
        targetFov = defaultFov;
    }

    public void SetZoomLevel(float zoomLevel, float cameraZoomZ)
    {
        targetFov = defaultFov * (2 - zoomLevel);
        cameraTargetLocalPosition = new Vector3(originalCameraPosition.x, originalCameraPosition.y, cameraZoomZ);
    }

    public void ResetToDefaultPos()
    {
        playerCamera.transform.localPosition = defaultPos;
        targetPos = new Vector3(defaultPos.x, defaultPos.y - offsetY, defaultPos.z);
    }

    public void CameraShake(float shakeAmount)
    {
        if (screenShake != 0f && cameraShaking < shakeAmount * screenShake)
        {
            cameraShaking = shakeAmount * screenShake;
        }
    }
}