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
    [SerializeField] private TwoBoneIKConstraint rightHandIK;
    [SerializeField] private Transform rightHandTarget;

    [Header("Left Hand Target")]
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private Transform leftHandTarget;

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

    private void LateUpdate()
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
        if (currentWeapon.isAiming && !currentWeapon.isReloading)
        {
            transformToAttachWeapon = aimingPos;
            currentWeapon.attachedToAimPos = true;
        }
        else
        {
            currentWeapon.attachedToAimPos = false;
        }

        //set the position and rotation of the hand targets to the ik target on rifle
        leftHandTarget.position = currentWeapon.IKLeftHandPos.position;
        leftHandTarget.rotation = currentWeapon.IKLeftHandPos.rotation;
        rightHandTarget.position = currentWeapon.IKRightHandPos.position;
        rightHandTarget.rotation = currentWeapon.IKRightHandPos.rotation;

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