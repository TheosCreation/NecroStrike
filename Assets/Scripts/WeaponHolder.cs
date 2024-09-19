using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class WeaponHolder : MonoBehaviour
{
    [HideInInspector] public PlayerController player;
    public Weapon currentWeapon;
    [SerializeField] private Weapon[] weapons;
    [SerializeField] private int maxHeldWeaponCount = 2;
    [SerializeField] private float throwForce = 0.5f;
    [SerializeField] private Transform currentWeaponPosition;
    [SerializeField] private Transform idlePos;
    [SerializeField] private Transform aimingPos;
    [SerializeField] float scrollSwitchDelay = 0.1f;
    [SerializeField] float transitionSpeed = 5.0f;
    int currentWeaponIndex = 0;
    private bool isSwitching = false;

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
        InputManager.Instance.playerInput.InGame.WeaponSwitch.performed += ctx => WeaponSwitch(ctx.ReadValue<Vector2>());
        InputManager.Instance.playerInput.InGame.Attack.started += _ctx => currentWeapon?.StartAttacking();
        InputManager.Instance.playerInput.InGame.Attack.canceled += _ctx => currentWeapon?.StopAttacking();
        InputManager.Instance.playerInput.InGame.Aim.started += _ctx => currentWeapon?.StartAiming();
        InputManager.Instance.playerInput.InGame.Aim.canceled += _ctx => currentWeapon?.StopAiming();
        InputManager.Instance.playerInput.InGame.Drop.started += _ctx => TryDropWeapon();
        InputManager.Instance.playerInput.InGame.Reload.started += _ctx => currentWeapon?.Reload();
        InputManager.Instance.playerInput.InGame.Inspect.started += _ctx => currentWeapon?.Inspect();
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

        UpdateHandTargets();

        // Smoothly interpolate weapon's position and rotation to the target transform
        currentWeapon.transform.position = Vector3.Lerp(currentWeapon.transform.position, transformToAttachWeapon.position, Time.deltaTime * transitionSpeed);
        currentWeapon.transform.rotation = Quaternion.Slerp(currentWeapon.transform.rotation, transformToAttachWeapon.rotation, Time.deltaTime * transitionSpeed);
        currentWeapon.transform.parent = transformToAttachWeapon;
    }
    private void UpdateHandTargets()
    {
        // Left Hand and fingers
        SetHandIK(leftHandTarget, currentWeapon.IKLeftHandPos);
        SetHandIK(leftIndexTarget, currentWeapon.IKLeftIndexPos);
        SetHandIK(leftMiddleTarget, currentWeapon.IKLeftMiddlePos);
        SetHandIK(leftPinkyTarget, currentWeapon.IKLeftPinkyPos);
        SetHandIK(leftRingTarget, currentWeapon.IKLeftRingPos);
        SetHandIK(leftThumbTarget, currentWeapon.IKLeftThumbPos);

        // Right Hand and fingers
        SetHandIK(rightHandTarget, currentWeapon.IKRightHandPos);
        SetHandIK(rightIndexTarget, currentWeapon.IKRightIndexPos);
        SetHandIK(rightMiddleTarget, currentWeapon.IKRightMiddlePos);
        SetHandIK(rightRingTarget, currentWeapon.IKRightRingPos);
        SetHandIK(rightPinkyTarget, currentWeapon.IKRightPinkyPos);
        SetHandIK(rightThumbTarget, currentWeapon.IKRightThumbPos);
    }
    private void SetHandIK(Transform handTarget, Transform ikTarget)
    {
        // Set the hand target's position and rotation based on the IK target from the weapon
        handTarget.position = ikTarget.position;
        handTarget.rotation = ikTarget.rotation;
    }

    public void Add(Weapon weapon)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == null)
            {
                weapons[i] = weapon;
                SelectWeapon(weapon);
                return;
            }
        }

        // If array is full, replace the current weapon
        if (weapons.Length >= maxHeldWeaponCount)
        {
            weapons[currentWeaponIndex] = weapon;
            SelectWeapon(currentWeaponIndex);
        }
    }

    private void TryDropWeapon()
    {
        if (currentWeapon == null) return;
        currentWeapon.Drop(throwForce);
        weapons[currentWeaponIndex] = null;
        currentWeapon = null;
        SelectWeapon(0);
    }
    private void WeaponSwitch(Vector2 direction)
    {
        if (!isSwitching)
        {
            StartCoroutine(WeaponSwitchDelay(direction));
        }
    }

    private IEnumerator WeaponSwitchDelay(Vector2 direction)
    {
        isSwitching = true;
        if (direction.y > 0)
        {
            // Scroll up, switch to the next weapon
            currentWeaponIndex = (currentWeaponIndex + 1) % weapons.Length;
        }
        else if (direction.y < 0)
        {
            // Scroll down, switch to the previous weapon
            currentWeaponIndex = (currentWeaponIndex - 1 + weapons.Length) % weapons.Length;
        }

        SelectWeapon(currentWeaponIndex);

        yield return new WaitForSeconds(scrollSwitchDelay);

        isSwitching = false;
    }

    private void SelectWeapon(Weapon weapon)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].gameObject.SetActive(weapons[i] == weapon);
                weapons[i].isAttacking = false;

                if (weapons[i] == weapon)
                {
                    currentWeaponIndex = i;
                    currentWeapon = weapons[i];
                    currentWeapon.holder = this;
                    currentWeapon.Equip();
                }
            }
        }
    }

    private void SelectWeapon(int index)
    {
        if (weapons[index] == null) return;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].gameObject.SetActive(i == index);
                weapons[i].isAttacking = false;

                if (i == index)
                {
                    currentWeaponIndex = i;
                    currentWeapon = weapons[i];
                    currentWeapon.holder = this;
                    currentWeapon.Equip();
                }
            }
        }
    }
}