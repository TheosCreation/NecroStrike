using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponAnim : MonoBehaviour
{
    [Header("Weapon Sway")]
    [SerializeField] private float positionalSway = 1f;
    [SerializeField] private float rotationalSway = 1f;
    [SerializeField] private float swaySmoothness = 1f;


    [Header("Weapon Bobbing")]
    [SerializeField] private float timeScale = 5f;
    [SerializeField] private float walkingToStopTransitionSpeed = 0.5f;
    [SerializeField] private float bobbingHorizontalOffset = 0.08f;
    [SerializeField] private float bobbingVerticalOffset = 0.025f;
    [SerializeField] private float velocityOffsetMultiplier = 0.005f;
    [SerializeField] private float maxVelocityOffset = 0.1f;
    public bool bobbingEnabled = true;

    private PlayerMovement movement;
    private PlayerController playerController;

    private Quaternion initialRotation = Quaternion.identity;
    private Vector3 originalPos;
    private Vector3 bobbingPosition;
    private Vector3 rightPos;
    private Vector3 leftPos;
    private Vector3 target;
    private Vector3 velocityOffset; // field used for smoothing
    private Vector3 swayPositionOffset = Vector3.zero;
    private Quaternion swayRotationOffset = Quaternion.identity;

    private bool backToStart;
    private float speed;
    private Vector3 currentOffset;
    private Vector3 currentSwayOffset;

    private void Awake()
    {
        movement = GetComponentInParent<PlayerMovement>();
        playerController = GetComponentInParent<PlayerController>();
        originalPos = transform.localPosition;
        initialRotation = transform.localRotation;
        bobbingPosition = originalPos;
        // targets will be computed in Update each frame
        target = originalPos + Vector3.right * bobbingHorizontalOffset + Vector3.down * bobbingVerticalOffset;
    }

    private void Update()
    {
        // Recompute targets so inspector changes take effect live
        rightPos = new Vector3(originalPos.x + bobbingHorizontalOffset, originalPos.y - bobbingVerticalOffset, originalPos.z);
        leftPos = new Vector3(originalPos.x - bobbingHorizontalOffset, originalPos.y - bobbingVerticalOffset, originalPos.z);

        bool isAiming = false;
        if (playerController.weaponHolder.currentWeapon != null) 
        {
            isAiming = playerController.weaponHolder.currentWeapon.isAiming;
        }
        if (bobbingEnabled && movement.isWalking && !isAiming)
        {
            speed = Time.deltaTime * (2f - Vector3.Distance(bobbingPosition, originalPos) * 3f) *
                    (Mathf.Min(movement.rb.linearVelocity.magnitude, timeScale) / timeScale);

            if (backToStart)
                bobbingPosition = Vector3.MoveTowards(bobbingPosition, originalPos, speed * 0.25f);
            else
                bobbingPosition = Vector3.MoveTowards(bobbingPosition, target, speed * 0.25f);

            // cycle targets
            if ((bobbingPosition - originalPos).sqrMagnitude < 1e-6f)
            {
                backToStart = false;
            }
            else if ((bobbingPosition - rightPos).sqrMagnitude < 1e-6f)
            {
                backToStart = true;
                target = leftPos;
            }
            else if ((bobbingPosition - leftPos).sqrMagnitude < 1e-6f)
            {
                backToStart = true;
                target = rightPos;
            }
        }
        else
        {
            bobbingPosition = Vector3.MoveTowards(bobbingPosition, originalPos, Time.deltaTime * walkingToStopTransitionSpeed);
        }
        float maxVelocityOffsetLocal = maxVelocityOffset;
        float velocityOffsetMultiplierLocal = velocityOffsetMultiplier;
        if (isAiming)
        {
            maxVelocityOffsetLocal *= 0.05f;
            velocityOffsetMultiplierLocal *= 0.1f;
        }
        CalculateSway();
        // Velocity sway
        Vector3 localVelocity = playerController.playerLook.playerCamera.transform.InverseTransformDirection(movement.rb.linearVelocity);
        velocityOffset = Vector3.ClampMagnitude(-localVelocity * velocityOffsetMultiplierLocal, maxVelocityOffsetLocal);
        currentOffset = Vector3.Lerp(currentOffset, velocityOffset, Time.deltaTime * timeScale);
        currentSwayOffset = Vector3.Lerp(currentSwayOffset, swayPositionOffset, Time.deltaTime * swaySmoothness);

        transform.localPosition = bobbingPosition + currentOffset + currentSwayOffset;

        Quaternion finalRotation = initialRotation * swayRotationOffset;
        transform.localRotation = Quaternion.Lerp(transform.localRotation, finalRotation, Time.deltaTime * swaySmoothness);
    }
    private void CalculateSway()
    {
        if (playerController.weaponHolder.currentWeapon == null) return;

        float swayReduction = playerController.weaponHolder.currentWeapon.isAiming ? playerController.weaponHolder.currentWeapon.motionReduction : 1.0f;

        float mouseX = InputManager.Instance.currentMouseDelta.x * 0.1f;
        float mouseY = InputManager.Instance.currentMouseDelta.y * 0.1f;

        swayPositionOffset = positionalSway * swayReduction * new Vector3(mouseX, mouseY, 0);
        swayRotationOffset = Quaternion.Euler(new Vector3(-mouseY, mouseX, 0) * rotationalSway * swayReduction);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // keep names consistent if you renamed the serialized field
        if (bobbingVerticalOffset < 0f) bobbingVerticalOffset = 0f;
        if (maxVelocityOffset < 0f) maxVelocityOffset = 0f;
    }
#endif
}
