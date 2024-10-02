using UnityEngine;

public class PlayerMovement : MonoBehaviour, IPausable
{
    private PlayerController playerController;
    [HideInInspector] public MovementController movementController;
    [Header("Audio Clips")]
    [SerializeField] private AudioClip audioClipWalking;
    [SerializeField] private AudioClip audioClipCrouching;
    [SerializeField] private AudioClip audioClipSprinting;

    [Header("Movement")]
    [SerializeField] private float walkMoveSpeed = 4.0f;
    [SerializeField] private float sprintMoveMultiplier = 1.5f;
    [SerializeField] private float crouchMoveMultiplier = 0.5f;
    [HideInInspector] public float movementMultiplier = 1.0f;
    private float currentMoveSpeed = 2.0f;
    [SerializeField] private float acceleration = 5.0f;
    [SerializeField] private float deceleration = 2.0f;

    [Header("Jump")]
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool isJumping = false;
    [SerializeField] private bool isCrouching = false;
    [SerializeField] private float jumpForce = 10.0f;
    [SerializeField] private float jumpDuration = 0.1f;
    private Timer jumpTimer;

    [Header("Crouching")]
    [SerializeField] private float crouchHeightMultiplier = 0.5f;

    [Header("Sprinting")]
    [SerializeField] public bool sprintingInput = false;
    [SerializeField] public bool isSprinting = false;
    [SerializeField] public bool canSprint = true;

    [Header("Sliding")]
    [SerializeField] public bool isSliding = false;
    [SerializeField] private bool canSlide = true;
    [SerializeField] private float slideForce = 15.0f;
    [SerializeField] private float slideDuration = 0.2f;
    [SerializeField] private float slideRefreashTime = 0.2f;
    Timer slideTimer;
    Timer slideRefreashTimer;

    public Vector3 localVelocity = Vector2.zero;
    Vector2 movementInput = Vector2.zero;
    private Rigidbody rb;
    private CapsuleCollider capsule;
    private float capsuleOriginalHeight = 0;
    private Vector3 capsuleOriginalCenter = Vector3.zero;
    [SerializeField] private Animator animator;
    private AudioSource audioSource;

    public Vector2 horizontalMovementSpeed = Vector2.zero;
    public float timeSinceSprintEnd = 0.0f;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        movementController = GetComponent<MovementController>();
        rb = GetComponent<Rigidbody>(); 
        capsule = GetComponent<CapsuleCollider>(); 

        audioSource = GetComponent<AudioSource>();
        audioSource.clip = audioClipWalking;

        InputManager.Instance.playerInput.InGame.Jump.started += _ctx => Jump();
        InputManager.Instance.playerInput.InGame.Crouch.started += _ctx => StartCrouching();
        InputManager.Instance.playerInput.InGame.Crouch.canceled += _ctx => EndCrouching();
        InputManager.Instance.playerInput.InGame.Sprint.started += _ctx => StartSprinting();
        InputManager.Instance.playerInput.InGame.Sprint.canceled += _ctx => EndSprinting();

        jumpTimer = gameObject.AddComponent<Timer>();
        slideTimer = gameObject.AddComponent<Timer>();
        slideRefreashTimer = gameObject.AddComponent<Timer>();

        capsuleOriginalHeight = capsule.height;
        capsuleOriginalCenter = capsule.center;
        CheckMoveSpeed();
    }

    private void FixedUpdate()
    {
        Vector3 horizontalVelocity = rb.velocity;
        horizontalVelocity.y = 0f;

        horizontalMovementSpeed = new Vector2(horizontalVelocity.x, horizontalVelocity.z);

        float speed = Mathf.Abs(horizontalMovementSpeed.magnitude);
        UiManager.Instance.UpdateSpeedText(speed);

        UpdateAnimations();
        PlayFootstepSounds();


        movementInput = InputManager.Instance.MovementVector;

        localVelocity = transform.InverseTransformDirection(rb.velocity);
        animator.SetFloat("SpeedX", localVelocity.x);
        animator.SetFloat("SpeedY", localVelocity.z);

        CheckMoveSpeed();

        if (isSliding) return;

        if (movementInput == Vector2.zero)
        {
            movementController.movement = false;
            return;
        }

        Vector3 movement = new Vector3(movementInput.x, 0f, movementInput.y);
        movement = movement.normalized;

        movementController.MoveLocal(movement, walkMoveSpeed * movementMultiplier, acceleration, deceleration);
    }

    private void CheckMoveSpeed()
    {
        float multiplier = 1f;

        if (isCrouching)
        {
            multiplier = crouchMoveMultiplier;
        }
        else if (sprintingInput && canSprint && movementController.isGrounded)
        {
            if (playerController.weaponHolder.currentWeapon != null)
            {
                // Ensure we are not aiming or attacking while sprinting
                if (!playerController.weaponHolder.currentWeapon.isAiming && !playerController.weaponHolder.currentWeapon.isAttacking)
                {
                    // Check if moving forward (positive y direction) to allow sprinting
                    if (movementInput.y > 0)
                    {
                        isSprinting = true;
                        multiplier = sprintMoveMultiplier;
                    }
                    else
                    {
                        isSprinting = false;
                    }
                }
            }
            else if (movementInput.y > 0) // Sprint only if moving forward
            {
                multiplier = sprintMoveMultiplier;
                isSprinting = true;
            }
        }
        else
        {
            isSprinting = false;
            multiplier = 1f;
        }

        Weapon currentWeapon = playerController.weaponHolder.currentWeapon;
        if (currentWeapon != null)
        {
            if (currentWeapon.isAiming)
            {
                multiplier -= currentWeapon.settings.aimingMoveReduction;
            }
        }

        movementMultiplier = multiplier;
    }

    private void UpdateAnimations()
    {
        animator.SetBool("IsMoving", movementController.movement);
        animator.SetBool("IsCrouching", isCrouching);
        animator.SetFloat("Movement", movementMultiplier * 1.5f); //multiplied by constant for more effect
        animator.SetBool("IsSliding", isSliding);
    }

    void Jump()
    {
        if (movementController.isGrounded && canJump)
        {
            PerformJump();
        }
    }

    void PerformJump()
    {
        canJump = false;
        movementController.AddForce(Vector3.up * jumpForce);

        // Play a CameraJumpAnimation

        isJumping = true;
        jumpTimer.StopTimer();
        jumpTimer.SetTimer(jumpDuration, JumpEnd);

        //cancel slide
        if(isSliding)
        {
            slideTimer.StopTimer();
            EndSlide();
            animator.SetTrigger("SlideCancel");
        }
    }


    void JumpEnd()
    {
        isJumping = false;
        canJump = true;
    }

    private void StartCrouching()
    {
        if (isSprinting && Mathf.Abs(horizontalMovementSpeed.magnitude) > walkMoveSpeed && movementController.isGrounded)
        {
            Slide(transform.forward, slideForce, slideDuration);
        }
        else
        {
            isCrouching = true;
            CancelSprint();
        }

        SetCapsuleHeight(crouchHeightMultiplier);
    }

    private void EndCrouching()
    {
        isCrouching = false;
        if(!isSliding)
        {
            SetCapsuleHeight(1.0f);
        }
    }

    private void StartSprinting()
    {
        if (sprintingInput || isCrouching) return;
        sprintingInput = true;
        playerController.weaponHolder.currentWeapon?.CancelReload();
    }

    public void CancelSprint()
    {
        canSprint = false;
        if (isSprinting)
        {
            timeSinceSprintEnd = Time.time;
        }
    }

    public void ResetSprint()
    {
        canSprint = true;
    }

    public void EndSprinting()
    {
        sprintingInput = false;

        if (isSprinting)
        {
            timeSinceSprintEnd = Time.time;
        }
    }

    private void SetCapsuleHeight(float crouchHeightMultiplier)
    {
        capsule.height = capsuleOriginalHeight * crouchHeightMultiplier;
        capsule.center = new Vector3(capsuleOriginalCenter.x, capsuleOriginalCenter.y * crouchHeightMultiplier, capsuleOriginalCenter.z);
    }

    public void Slide(Vector3 slideDirection, float slideForce, float slideDuration, bool ignoreInput = false)
    {
        if (!isSliding && canSlide)
        {
            CancelSprint();

            // If input detected then apply it
            if (movementInput.sqrMagnitude > Mathf.Epsilon && !ignoreInput)
            {
                slideDirection = new Vector3(movementInput.x, 0, movementInput.y);
                slideDirection.Normalize();
                slideDirection = transform.TransformDirection(slideDirection);
            }

            movementController.ResetVerticalVelocity();
            movementController.ResetHorizontalVelocity();
            movementController.AddForce(slideDirection * slideForce);
            //movementController.SetFriction(false);
            isSliding = true;
            canSlide = false;
            slideTimer.StopTimer();
            slideTimer.SetTimer(slideDuration, EndSlide);
        }
    }

    void EndSlide()
    {
        isSliding = false;
        SetCapsuleHeight(1.0f);
        slideRefreashTimer.SetTimer(slideRefreashTime, ResetSlide);

        Weapon currentWeapon = playerController.weaponHolder.currentWeapon;
        if (currentWeapon != null)
        {
            if(currentWeapon.canResetSprint && !currentWeapon.isAttacking && !currentWeapon.isAiming && !currentWeapon.isReloading)
            {
                canSprint = true;
            }
        }
        //movementController.SetFriction(true);
    }

    void ResetSlide()
    {
        canSlide = true;
    }

    private void PlayFootstepSounds()
    {
        if (movementController.isGrounded && horizontalMovementSpeed.sqrMagnitude > 0.1f)
        {
            if (playerController.playerMovement.isSprinting)
            {
                audioSource.clip = audioClipSprinting;
            }
            else if (playerController.playerMovement.isCrouching)
            {
                audioSource.clip = audioClipCrouching;
            }
            else
            {
                audioSource.clip = audioClipWalking;
            }

            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else if (audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    public void OnPause()
    {
        audioSource.Pause();
    }

    public void OnUnPause()
    {
        PlayFootstepSounds();
    }
}