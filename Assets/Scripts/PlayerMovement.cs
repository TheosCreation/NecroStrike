using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("States")]
    public bool isWalking = false;
    public bool isJumping = false;
    public bool isFalling = false;
    public bool canSlide = true;
    public bool isSliding = false;
    public bool isSprinting = false;
    public bool isStanding = true;
    public bool isCrouching = false;
    public bool jumpOnCooldown = false;

    [Header("Movement Variables")]
    [SerializeField] private float walkSpeed = 30f;
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;
    [SerializeField] private float acceleration = 2.75f;
    [SerializeField] private float airAcceleration = 2f;
    [SerializeField] private float friction = 0f;
    [SerializeField] private float maxVerticalVelocity = 100f;
    [SerializeField] private Vector3 pushForce;

    [Header("Sprinting")]
    [SerializeField] public bool sprintingInput = false;
    [SerializeField] public bool canSprint = true;
    public float timeSinceSprintEnd; // Time.time stamp when sprint last ended

    [Header("Jump")]
    [SerializeField] private float jumpPower = 20f;
    [SerializeField] private float jumpCooldownTime = 0.25f;

    [Header("Sliding")]
    public float preSlideSpeed = 0f;
    [Range(0f, 1f)] public float slideConservationAmount = 1f;
    [SerializeField] private float minSlideSpeed = 10f;
    [SerializeField] private float slideSpeed = 10f;
    [SerializeField] private float slideSlowRate = 0.5f;

    [Header("Crouch")]
    [Range(0.01f, 1.0f)][SerializeField] private float crouchHeight = 0.5f;

    [Header("Setup")]
    public GroundCheck gc;
    public GroundCheck slopeCheck;
    public FootstepSetSO footstepSet;
    [SerializeField] private LayerMask environmentMask;

    [Header("Audio / FX")]
    [SerializeField] private AudioSource fallSoundAudioSource;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip endSlideSound;
    [SerializeField] private AudioClip slideJumpSound;

    // Generated at runtime if null
    private AudioSource footStepAudioSource;
    private AudioSource jumpAudioSource;
    private AudioSource slideAudioSource;
    private AudioSource landAudioSource;

    [HideInInspector] public Vector3 wallNormal;

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public float currentTargetMoveSpeed;
    public Animator animator;

    [HideInInspector] public CapsuleCollider playerCollider;
    private PlayerLook playerLook;
    private WeaponHolder weaponHolder;

    private Vector3 inputDirection;         // x,z from input
    private Vector3 airDirection;
    private Vector3 movementDirection;
    private Vector3 finalMovementDirection;

    private Vector3 groundCheckPos;
    private float footstepTimer = 0;
    private float fallTime = 0;
    private float fallSpeed = 0;
    private bool jumpCooldown = false;

    private int lastFootstep = 0;

    public Vector3 localVelocity = Vector2.zero;

    // Slide bookkeeping
    private Vector3 slideDirection = Vector3.zero;
    private float slideSafetyTimer = 0f;
    private int framesSinceSlide = 0;
    private float slideLength = 0f;
    private float longestSlide = 0f;
    private Vector3 velocityAfterSlide = Vector3.zero;

    private Vector3 originalColliderCenter = Vector3.zero;
    private float originalColliderHeight = 0f;

    // Removed systems
    private float slamForce = 0f; // retained as 0 for legacy math compatibility

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerLook = GetComponentInChildren<PlayerLook>();
        playerCollider = GetComponent<CapsuleCollider>();
        weaponHolder = GetComponent<WeaponHolder>();

        footStepAudioSource = EnsureAudio(footStepAudioSource);
        jumpAudioSource = EnsureAudio(jumpAudioSource);
        slideAudioSource = EnsureAudio(slideAudioSource);
        landAudioSource = EnsureAudio(landAudioSource);
        if (fallSoundAudioSource == null) fallSoundAudioSource = EnsureAudio(null);

        originalColliderCenter = playerCollider.center;
        originalColliderHeight = playerCollider.height;
    }

    private AudioSource EnsureAudio(AudioSource src)
    {
        return src != null ? src : gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        rb.solverIterations *= 5;
        rb.solverVelocityIterations *= 5;
        groundCheckPos = gc.transform.localPosition;
    }

    private void Update()
    {
        // Step sounds
        if (isWalking)
        {
            // Normalize the timer reduction by actual movement speed
            float speedRatio = rb.linearVelocity.magnitude / walkSpeed;
            footstepTimer = Mathf.MoveTowards(footstepTimer, 0f, speedRatio * Time.deltaTime * 1.75f);
        }

        if (footstepTimer <= 0f) Footstep();

        inputDirection = InputManager.Instance.PlayerInput.Move.ReadValue<Vector2>();
        movementDirection = Vector3.ClampMagnitude(inputDirection.x * transform.right + inputDirection.y * transform.forward, 1f);

        // Walk state
        if (!isWalking && inputDirection.sqrMagnitude > 0f && !isSliding && gc.onGround) isWalking = true;
        else if ((isWalking && Mathf.Approximately(inputDirection.sqrMagnitude, 0f)) || !gc.onGround || isSliding) isWalking = false;

        // Falling state
        if (!gc.onGround)
        {
            if (fallTime < 1f)
            {
                fallTime += Time.deltaTime * 5f;
                if (fallTime > 1f) isFalling = true;
            }
            else if (rb.linearVelocity.y < -2f)
            {
                fallSpeed = rb.linearVelocity.y;
            }
        }
        else
        {
            fallTime = 0f;
        }

        // Wind audio vs speed
        if (rb.linearVelocity.magnitude > 50f)
        {
            fallSoundAudioSource.pitch = rb.linearVelocity.magnitude / 120f;
            fallSoundAudioSource.volume = rb.linearVelocity.magnitude / 80f;
        }
        else if (rb.linearVelocity.magnitude < 40f)
        {
            fallSoundAudioSource.pitch = 0f;
            fallSoundAudioSource.volume = 0f;
        }

        // Clamp vertical
        if (rb.linearVelocity.y < -maxVerticalVelocity)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -maxVerticalVelocity, rb.linearVelocity.z);
        }

        // Land impact
        if (gc.onGround && isFalling && !jumpCooldown)
        {
            isFalling = false;
            AudioClip landSound = gc.currentGroundProperties != null
                ? footstepSet.GetLand(gc.currentGroundProperties.groundType)
                : footstepSet.GetLand(GroundType.Stone);

            landAudioSource.clip = landSound;
            landAudioSource.volume = 0.5f + fallSpeed * -0.01f;
            landAudioSource.pitch = Random.Range(0.9f, 1.1f);
            landAudioSource.Play();
            Footstep(0.5f, force: true);
            Footstep(0.5f, force: true, 0.05f);
            fallSpeed = 0f;
        }

        sprintingInput = InputManager.Instance.PlayerInput.Shift.IsPressed;
        if (InputManager.Instance.PlayerInput.Shift.WasCanceledThisFrame) timeSinceSprintEnd = Time.time;

        // Slide input
        if (InputManager.Instance.PlayerInput.Crouch.WasPerformedThisFrame && !isSliding)
        {
            if (isSprinting && rb.linearVelocity.magnitude > walkSpeed && (gc.onGround || gc.sinceLastGrounded < 0.03f))
            {
                // Sprint slide
                StartSlide();
            }
            else if (gc.onGround)
            {
                // Regular crouch
                isCrouching = true;
                SetCapsuleHeight(crouchHeight);
                isStanding = false;
            }
        }
        if (InputManager.Instance.PlayerInput.Crouch.WasCanceledThisFrame)
        {
            // Don't stop slide when releasing crouch - let it finish naturally
            if (!isSliding && isCrouching)
            {
                // End crouch
                isCrouching = false;
                SetCapsuleHeight(1.0f);
                isStanding = true;
            }
        }

        // Slide camera and stance
        if (isSliding)
        {
            if (gc.onGround) playerLook.CameraShake(0.1f);
        }

        // Slope helper
        if (!slopeCheck.onGround && slopeCheck.forcedOff <= 0 && !isJumping)
        {
            float num = playerCollider.height / 2f - playerCollider.center.y;
            if (rb.linearVelocity != Vector3.zero && Physics.Raycast(transform.position, Vector3.down, out var hit3, num + 0.35f, environmentMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 target = new Vector3(transform.position.x, transform.position.y - hit3.distance + num, transform.position.z);
                transform.position = Vector3.MoveTowards(transform.position, target, hit3.distance * Time.deltaTime * 10f);
                if (rb.linearVelocity.y > 0f) rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            }
        }

        // Sprint logic
        // external code should set sprintingInput; here we derive state
        bool sprintAllowed = canSprint && (gc.onGround || gc.sinceLastGrounded < 0.1f) && !isCrouching && !isSliding && inputDirection.sqrMagnitude > 0.01f;
        isSprinting = sprintingInput && sprintAllowed;

        if (isSprinting)
        {
            weaponHolder?.currentWeapon?.CancelReload();
        }
        if (!isSprinting && InputManager.Instance.PlayerInput.Shift.WasCanceledThisFrame)
        {
            // mark sprint end to gate weapon sprint-to-fire
            timeSinceSprintEnd = Time.time;
        }

        // Jump
        if (InputManager.Instance.PlayerInput.Jump.WasPerformedThisFrame && !jumpCooldown)
        {
            if (!isFalling || gc.touchingGround) Jump();
        }

        UpdateAnimations();
    }

    private void UpdateAnimations()
    {
        localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        animator.SetFloat("SpeedX", localVelocity.x);
        animator.SetFloat("SpeedY", localVelocity.z);
        animator.SetBool("IsMoving", isWalking || isSprinting);
        animator.SetBool("IsCrouching", isCrouching || isSliding);
        float movementParam = movementDirection.magnitude * (isSprinting ? 1.5f : 1f);
        animator.SetFloat("Movement", movementParam);
        animator.SetBool("IsSliding", isSliding);
    }

    public void CancelSprint()
    {
        canSprint = false;
        if (isSprinting) timeSinceSprintEnd = Time.time;
        isSprinting = false;
    }

    private void FixedUpdate()
    {
        // slide stop guard
        if (isSliding)
        {
            if (slideSafetyTimer <= 0f)
            {
                if (GetHorizontalVelocity().magnitude < minSlideSpeed)
                {
                    slideSafetyTimer = Mathf.MoveTowards(slideSafetyTimer, -0.1f, Time.deltaTime);
                    if (slideSafetyTimer <= -0.1f) StopSlide();
                }
                else
                {
                    slideSafetyTimer = 0f;
                }
            }
        }
        else
        {
            framesSinceSlide++;
        }

        Move();
    }

    private void Move()
    {
        currentTargetMoveSpeed = isSprinting ? walkSpeed * sprintSpeedMultiplier : walkSpeed;

        if (gc.onGround && friction > 0f && !isJumping)
        {
            float y = rb.linearVelocity.y;
            if (slopeCheck.onGround && movementDirection.x == 0f && movementDirection.z == 0f)
            {
                y = 0f;
                rb.useGravity = false;
            }
            else
            {
                rb.useGravity = true;
            }

            finalMovementDirection = new Vector3(
                movementDirection.x * currentTargetMoveSpeed,
                y,
                movementDirection.z * currentTargetMoveSpeed
            );

            if (isSliding)
            {
                Vector3 currentVelocity = rb.linearVelocity;
                Vector3 horizontalVel = new Vector3(currentVelocity.x, 0, currentVelocity.z);

                // Slow down gradually
                float slideDeceleration = slideSlowRate * Time.fixedDeltaTime;
                horizontalVel = Vector3.MoveTowards(horizontalVel, Vector3.zero, slideDeceleration);

                // Stop slide if too slow
                if (horizontalVel.magnitude < minSlideSpeed)
                {
                    StopSlide();
                    return;
                }

                // Apply the velocity
                rb.linearVelocity = new Vector3(horizontalVel.x, currentVelocity.y, horizontalVel.z);

                return;
            }
            else
            {
                rb.linearVelocity = Vector3.Lerp(
                    rb.linearVelocity,
                    finalMovementDirection + pushForce,
                    Time.fixedDeltaTime * acceleration * friction
                );
            }
        }
        else
        {
            rb.useGravity = true;
            if (isSliding) return;

            finalMovementDirection = new Vector3(
                movementDirection.x * currentTargetMoveSpeed,
                rb.linearVelocity.y,
                movementDirection.z * currentTargetMoveSpeed
            );

            airDirection.y = 0f;
            if ((finalMovementDirection.x > 0f && rb.linearVelocity.x < finalMovementDirection.x) || (finalMovementDirection.x < 0f && rb.linearVelocity.x > finalMovementDirection.x))
            {
                airDirection.x = finalMovementDirection.x;
            }
            else
            {
                airDirection.x = 0f;
            }
            if ((finalMovementDirection.z > 0f && rb.linearVelocity.z < finalMovementDirection.z) || (finalMovementDirection.z < 0f && rb.linearVelocity.z > finalMovementDirection.z))
            {
                airDirection.z = finalMovementDirection.z;
            }
            else
            {
                airDirection.z = 0f;
            }

            rb.AddForce(airAcceleration * airDirection.normalized, ForceMode.VelocityChange);

            Vector3 lv = rb.linearVelocity;
            Vector3 horizontal = new Vector3(lv.x, 0f, lv.z);

            Vector3 differenceHorizontal = Vector3.zero - horizontal;
            horizontal = horizontal + differenceHorizontal * Time.fixedDeltaTime * 0.15f * friction; //apply small slow down due to air friction
            rb.linearVelocity = new Vector3(horizontal.x, lv.y, horizontal.z);
        }
    }

    private bool ShouldAccelerateAxis(float target, float current)
    {
        return (target > 0f && current < target) || (target < 0f && current > target);
    }

    public void Jump(float bounceAmount = 0.0f)
    {
        isJumping = true;
        CancelInvoke(nameof(EndJumping));
        Invoke(nameof(EndJumping), jumpCooldownTime + 0.05f);
        isFalling = true;

        jumpAudioSource.clip = jumpSound;
        jumpAudioSource.volume = 0.75f;
        jumpAudioSource.pitch = 1f;
        jumpAudioSource.Play();
        ResetVerticalVelocity();

        float currentJumpPower = jumpPower + bounceAmount;

        if (isSliding)
        {
            rb.AddForce(2f * jumpPower * Vector3.up, ForceMode.VelocityChange);
            rb.AddForce(slideSpeed * slideDirection.normalized, ForceMode.VelocityChange);

            animator.SetTrigger("SlideCancel");
            StopSlide();

            slideAudioSource.clip = slideJumpSound;
            slideAudioSource.volume = 1.0f;
            slideAudioSource.pitch = 1f;
            slideAudioSource.Play();
        }
        else
        {
            rb.AddForce(3f * currentJumpPower * Vector3.up, ForceMode.VelocityChange);
        }

        jumpCooldown = true;
        CancelInvoke(nameof(JumpReady));
        Invoke(nameof(JumpReady), jumpCooldownTime);
    }

    private void EndJumping() => isJumping = false;
    private void JumpReady() => jumpCooldown = false;

    public void Footstep(float volume = 0.5f, bool force = false, float delay = 0f)
    {
        if (!gc.onGround) return;

        footstepTimer = 1f;
        footStepAudioSource.volume = volume;

        AudioClip[] footsteps = gc.currentGroundProperties != null
            ? footstepSet.GetFootsteps(gc.currentGroundProperties.groundType)
            : footstepSet.GetFootsteps(GroundType.Stone);

        if (footsteps == null || footsteps.Length == 0) return;
        PlayRandomFootstepClip(footsteps, delay);
    }

    private void PlayRandomFootstepClip(AudioClip[] clips, float delay = 0f)
    {
        int num = Random.Range(0, clips.Length);
        if (clips.Length > 1 && num == lastFootstep) num = (num + 1) % clips.Length;
        lastFootstep = num;
        PlayFootstepClip(clips[num], delay);
    }

    private void PlayFootstepClip(AudioClip clip, float delay = 0f)
    {
        if (clip == null) return;
        footStepAudioSource.clip = clip;
        footStepAudioSource.pitch = Random.Range(0.9f, 1.1f);
        if (delay == 0f) footStepAudioSource.Play();
        else footStepAudioSource.PlayDelayed(delay);
    }

    private void SetCapsuleHeight(float multiplier)
    {
        float targetHeight = originalColliderHeight * multiplier;
        playerCollider.height = targetHeight;

        // difference in height
        float delta = originalColliderHeight - targetHeight;

        // shift center downward by half the shrink
        Vector3 targetCenter = originalColliderCenter;
        targetCenter.y = originalColliderCenter.y - (delta * 0.5f);

        playerCollider.center = targetCenter;
    }


    public void StartSlide()
    {
        if (!isSliding && canSlide)
        {
            // Cancel sprint
            CancelSprint();

            // Set slide state
            isSliding = true;
            canSlide = false;

            // Adjust collider immediately
            SetCapsuleHeight(crouchHeight);

            // Determine slide direction
            Vector3 slideDirection;
            if (movementDirection.sqrMagnitude > 0.001f)
            {
                slideDirection = movementDirection.normalized;
            }
            else
            {
                slideDirection = transform.forward;
            }

            // Reset velocities and apply slide force
            ResetVerticalVelocity();
            ResetHorizontalVelocity();
            rb.AddForce(slideDirection * slideSpeed, ForceMode.VelocityChange);

            // Set slide direction for ongoing slide physics
            this.slideDirection = slideDirection;

            // Start slide timer - slide lasts 1 second regardless of input
            CancelInvoke(nameof(EndSlide));
            Invoke(nameof(EndSlide), 1.0f);
        }
    }

    public void StopSlide()
    {
        if (!isSliding) return;

        // Don't reset sprint immediately - add delay
        isSliding = false;

        // Reset collider height
        SetCapsuleHeight(1.0f);

        // Reset slide tracking
        if (slideLength > longestSlide) longestSlide = slideLength;
        slideLength = 0f;
        framesSinceSlide = 0;
        velocityAfterSlide = rb.linearVelocity;

        // Play end slide sound
        if (endSlideSound != null)
        {
            slideAudioSource.clip = endSlideSound;
            slideAudioSource.volume = 0.75f;
            slideAudioSource.pitch = 1f;
            slideAudioSource.Play();
        }

        // Reset slide availability after delay
        CancelInvoke(nameof(ResetSlide));
        Invoke(nameof(ResetSlide), 0.5f);

        // Reset sprint after delay (not immediately)
        CancelInvoke(nameof(ResetSprint));
        Invoke(nameof(ResetSprint), 0.75f); // 0.75 second delay before can sprint again

        // Reset stance
        isCrouching = false;
        isStanding = true;
    }

    private void EndSlide()
    {
        StopSlide();
    }

    private void ResetSlide()
    {
        canSlide = true;
    }

    public void ResetSprint()
    {
        canSprint = true;
    }

    private void ResetVerticalVelocity()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    public void ResetHorizontalVelocity()
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    public Vector3 GetMovementDirection() => movementDirection;
    public Vector3 GetInput() => inputDirection;
    public Vector3 GetHorizontalVelocity() => new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
    public Vector3 GetVerticalVelocity() => new Vector3(0f, rb.linearVelocity.y, 0f);
}