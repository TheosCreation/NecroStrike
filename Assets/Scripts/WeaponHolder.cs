using UnityEngine;
using UnityEngine.Animations.Rigging;

public class WeaponHolder : MonoBehaviour
{
    [HideInInspector] public PlayerController player;
    public Weapon currentWeapon;
    [SerializeField] private float throwForce = 0.5f;
    [SerializeField] private Transform currentWeaponPosition;
    [SerializeField] private Transform idlePos;
    [SerializeField] private Transform aimingPos;
    [SerializeField] float transitionSpeed = 5.0f;

    [Header("Right Hand Target")]
    //Base Hand
    [SerializeField] private TwoBoneIKConstraint rightHandIK;
    [SerializeField] private Transform rightHandTarget;

    [Header("Right Fingers Targets")]
    //Index
    [SerializeField] private TwoBoneIKConstraint rightIndexIK;
    [SerializeField] private Transform rightIndexTarget;
    //Middle
    [SerializeField] private TwoBoneIKConstraint rightMiddleIK;
    [SerializeField] private Transform rightMiddleTarget;
    //Pinky
    [SerializeField] private TwoBoneIKConstraint rightPinkyIK;
    [SerializeField] private Transform rightPinkyTarget;
    //Ring
    [SerializeField] private TwoBoneIKConstraint rightRingIK;
    [SerializeField] private Transform rightRingTarget;
    //Thumb
    [SerializeField] private TwoBoneIKConstraint rightThumbIK;
    [SerializeField] private Transform rightThumbTarget;

    [Header("Left Hand Target")]
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private Transform leftHandTarget;

    [Header("Left Fingers Targets")]
    //Index
    [SerializeField] private TwoBoneIKConstraint leftIndexIK;
    [SerializeField] private Transform leftIndexTarget;
    //Middle
    [SerializeField] private TwoBoneIKConstraint leftMiddleIK;
    [SerializeField] private Transform leftMiddleTarget;
    //Pinky
    [SerializeField] private TwoBoneIKConstraint leftPinkyIK;
    [SerializeField] private Transform leftPinkyTarget;
    //Ring
    [SerializeField] private TwoBoneIKConstraint leftRingIK;
    [SerializeField] private Transform leftRingTarget;
    //Thumb
    [SerializeField] private TwoBoneIKConstraint leftThumbIK;
    [SerializeField] private Transform leftThumbTarget;

    private Animator animator;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        player = GetComponent<PlayerController>();
        InputManager.Instance.playerInput.InGame.Attack.started += _ctx => TryStartAttacking();
        InputManager.Instance.playerInput.InGame.Attack.canceled += _ctx => TryStopAttacking();
        InputManager.Instance.playerInput.InGame.Aim.started += _ctx => TryStartAiming();
        InputManager.Instance.playerInput.InGame.Aim.canceled += _ctx => TryStopAiming();
        InputManager.Instance.playerInput.InGame.Drop.started += _ctx => TryDropWeapon();
        InputManager.Instance.playerInput.InGame.Reload.started += _ctx => TryReload();
        InputManager.Instance.playerInput.InGame.Inspect.started += _ctx => TryInspect();
    }

    private void Update()
    {
        if (currentWeapon == null)
        {
            //turn the ik on for both hands to attach a rifle
            leftHandIK.weight = 0f;
            rightHandIK.weight = 0f;
            return;
        }
        else
        {
            //turn the ik on for both hands to attach a rifle
            leftHandIK.weight = 1f;
            rightHandIK.weight = 1f;
        }

        Transform transformToAttachWeapon = idlePos;
        if (currentWeapon.isAiming && !currentWeapon.isReloading && !currentWeapon.isBoltAction)
        {
            transformToAttachWeapon = aimingPos;
        }

        //set the position and rotation of the hand targets to the ik target on rifle
        leftHandTarget.position = currentWeapon.IKLeftHandPos.position;
        leftHandTarget.rotation = currentWeapon.IKLeftHandPos.rotation;
        //left index
        leftIndexTarget.position = currentWeapon.IKLeftIndexPos.position;
        leftIndexTarget.rotation = currentWeapon.IKLeftIndexPos.rotation;
        //left middle
        leftMiddleTarget.position = currentWeapon.IKLeftMiddlePos.position;
        leftMiddleTarget.rotation = currentWeapon.IKLeftMiddlePos.rotation;
        //left pinky
        leftPinkyTarget.position = currentWeapon.IKLeftPinkyPos.position;
        leftPinkyTarget.rotation = currentWeapon.IKLeftPinkyPos.rotation;
        //left ring
        leftRingTarget.position = currentWeapon.IKLeftRingPos.position;
        leftRingTarget.rotation = currentWeapon.IKLeftRingPos.rotation;
        //left thumb
        leftThumbTarget.position = currentWeapon.IKLeftThumbPos.position;
        leftThumbTarget.rotation = currentWeapon.IKLeftThumbPos.rotation;

        //Right Hand
        rightHandTarget.position = currentWeapon.IKRightHandPos.position;
        rightHandTarget.rotation = currentWeapon.IKRightHandPos.rotation;
        // Right Index
        rightIndexTarget.position = currentWeapon.IKRightIndexPos.position;
        rightIndexTarget.rotation = currentWeapon.IKRightIndexPos.rotation;
        // Right Middle
        rightMiddleTarget.position = currentWeapon.IKRightMiddlePos.position;
        rightMiddleTarget.rotation = currentWeapon.IKRightMiddlePos.rotation;
        // Right Ring
        rightRingTarget.position = currentWeapon.IKRightRingPos.position;
        rightRingTarget.rotation = currentWeapon.IKRightRingPos.rotation;
        // Right Pinky
        rightPinkyTarget.position = currentWeapon.IKRightPinkyPos.position;
        rightPinkyTarget.rotation = currentWeapon.IKRightPinkyPos.rotation;
        // Right Thumb
        rightThumbTarget.position = currentWeapon.IKRightThumbPos.position;
        rightThumbTarget.rotation = currentWeapon.IKRightThumbPos.rotation;

        // Smoothly interpolate weapon's position and rotation to the target transform
        currentWeapon.transform.position = Vector3.Lerp(currentWeapon.transform.position, transformToAttachWeapon.position, Time.deltaTime * transitionSpeed);
        currentWeapon.transform.rotation = Quaternion.Slerp(currentWeapon.transform.rotation, transformToAttachWeapon.rotation, Time.deltaTime * transitionSpeed);
        currentWeapon.transform.parent = transformToAttachWeapon;
    }

    private void TryStartAttacking()
    {
        if (currentWeapon == null) return;

        currentWeapon.StartAttacking();
    }
    private void TryStopAttacking()
    {
        if (currentWeapon == null) return;

        currentWeapon.StopAttacking();
    }

    private void TryStartAiming()
    {
        if (currentWeapon == null) return;

        currentWeapon.StartAiming();
    }
    
    private void TryStopAiming()
    {
        if (currentWeapon == null) return;

        currentWeapon.StopAiming();
    }

    public void Add(Weapon weapon)
    {
        currentWeapon = weapon;
        currentWeapon.Equip();
    }

    private void TryDropWeapon()
    {
        if (currentWeapon == null) return;
        currentWeapon.Drop(throwForce);
        currentWeapon = null;
    }

    private void TryReload()
    {
        if (currentWeapon == null) return;
        currentWeapon.Reload();
    }

    private void TryInspect()
    {
        if (currentWeapon == null) return;
        currentWeapon.Inspect();
    }
}