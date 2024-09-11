using System;
using UnityEngine;
using UnityEngine.Audio;

public class PlayerMovement : MonoBehaviour
{
    private PlayerController playerController;
    [HideInInspector] public MovementController movementController;
    [Header("Audio Clips")]
    [SerializeField] private AudioClip audioClipWalking;

    [Header("Movement")]
    [SerializeField] private float walkMoveSpeed = 4.0f;
    [SerializeField] private float crouchMoveSpeed = 2.0f;
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

    [Header("Dash")]
    [SerializeField] public bool isDashing = false;
    [SerializeField] private bool canDash = true;
    [SerializeField] private float dashForce = 15.0f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 2.0f;
    Timer dashTimer;
    Timer dashCoolDownTimer;

    Vector2 movementInput = Vector2.zero;
    private Rigidbody rb;
    [SerializeField] private Animator animator;
    private AudioSource audioSource;

    public Vector2 horizontalMovementSpeed = Vector2.zero;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        movementController = GetComponent<MovementController>();
        rb = GetComponent<Rigidbody>(); 

        audioSource = GetComponent<AudioSource>();
        audioSource.clip = audioClipWalking;

        InputManager.Instance.playerInput.InGame.Jump.started += _ctx => Jump();
        InputManager.Instance.playerInput.InGame.Crouch.started += _ctx => StartCrouching();
        InputManager.Instance.playerInput.InGame.Crouch.canceled += _ctx => EndCrouching();
        InputManager.Instance.playerInput.InGame.Dash.started += _ctx => Dash(transform.forward, dashForce, dashDuration);

        jumpTimer = gameObject.AddComponent<Timer>();
        dashTimer = gameObject.AddComponent<Timer>();
        dashCoolDownTimer = gameObject.AddComponent<Timer>();

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

        if (isDashing) return;

        movementInput = InputManager.Instance.MovementVector;
        animator.SetFloat("SpeedX", movementInput.x);
        animator.SetFloat("SpeedY", movementInput.y);
        if (movementInput == Vector2.zero)
        {
            movementController.movement = false;
            return;
        }

        Vector3 movement = new Vector3(movementInput.x, 0f, movementInput.y);
        movement = movement.normalized;

        movementController.MoveLocal(movement, currentMoveSpeed, acceleration, deceleration);
    }

    private void CheckMoveSpeed()
    {
        if(isCrouching)
        {
            currentMoveSpeed = crouchMoveSpeed;
        }
        else
        {
            currentMoveSpeed = walkMoveSpeed;
        }
    }

    private void UpdateAnimations()
    {
        animator.SetBool("IsMoving", movementController.movement);
        animator.SetBool("IsCrouching", isCrouching);
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
    }


    void JumpEnd()
    {
        isJumping = false;
        canJump = true;
    }

    private void StartCrouching()
    {
        isCrouching = true;
        CheckMoveSpeed();
    }

    private void EndCrouching()
    {
        isCrouching = false;
        CheckMoveSpeed();
    }

    public void Dash(Vector3 dashDirection, float dashForce, float dashDuration, bool ignoreInput = false)
    {
        if (canDash)
        {
            // If input detected then apply it
            if (movementInput.sqrMagnitude > Mathf.Epsilon && !ignoreInput)
            {
                dashDirection = new Vector3(movementInput.x, 0, movementInput.y);
                dashDirection.Normalize();
                dashDirection = transform.TransformDirection(dashDirection);
            }

            movementController.ResetVerticalVelocity();
            movementController.ResetHorizontalVelocity();
            movementController.AddForce(dashDirection * dashForce);
            movementController.SetFriction(false);
            isDashing = true;
            canDash = false;
            dashTimer.StopTimer();
            dashTimer.SetTimer(dashDuration, EndDash);

            dashCoolDownTimer.StopTimer();
            dashCoolDownTimer.SetTimer(dashCooldown, RefreshDash);
        }
    }

    void EndDash()
    {
        isDashing = false;
        movementController.SetFriction(true);
    }

    void RefreshDash()
    {
        movementController.SetFriction(true);
        canDash = true;
    }
    private void PlayFootstepSounds()
    {
        if (movementController.isGrounded && horizontalMovementSpeed.sqrMagnitude > 0.1f)
        {
            //audioSource.clip = playerCharacter.IsRunning() ? audioClipRunning : audioClipWalking;

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
}