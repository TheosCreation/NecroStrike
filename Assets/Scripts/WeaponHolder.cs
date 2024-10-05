using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class WeaponHolder : MonoBehaviour
{
    [HideInInspector] public PlayerController player;
    public Weapon currentWeapon;
    public MeleeWeapon meleeWeapon;
    private Weapon previousWeapon;
    Timer meleeTimer;
    public List<Weapon> weapons = new List<Weapon>();
    [SerializeField] private int maxHeldWeaponCount = 2;
    [SerializeField] private Transform currentWeaponPosition;
    public Transform idlePos;
    [SerializeField] private Transform aimingPos;
    [SerializeField] float scrollSwitchDelay = 0.1f;
    [SerializeField] float transitionSpeed = 5.0f;
    int currentWeaponIndex = 0;
    private bool isSwitching = false;
    [SerializeField] private bool isMeleeing = false;
    public bool canAttach = true;
    private float lastMeleeTime = 0f;

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
        meleeTimer = gameObject.AddComponent<Timer>();
        InputManager.Instance.playerInput.InGame.WeaponSwitch.performed += ctx => WeaponSwitch(ctx.ReadValue<Vector2>());
        InputManager.Instance.playerInput.InGame.Attack.started += _ctx => TryStartAttacking();
        InputManager.Instance.playerInput.InGame.Attack.canceled += _ctx => currentWeapon?.StopAttacking();
        InputManager.Instance.playerInput.InGame.Aim.started += _ctx => TryStartAiming();
        InputManager.Instance.playerInput.InGame.Aim.canceled += _ctx => currentWeapon?.StopAiming();
        //InputManager.Instance.playerInput.InGame.Drop.started += _ctx => TryDropWeapon();
        InputManager.Instance.playerInput.InGame.Reload.started += _ctx => currentWeapon?.Reload();
        InputManager.Instance.playerInput.InGame.Inspect.started += _ctx => currentWeapon?.Inspect();
        InputManager.Instance.playerInput.InGame.Melee.started += _ctx => Melee();

        meleeWeapon.gameObject.SetActive(false);
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
        if (currentWeapon.aimingInput && !currentWeapon.isReloading && !currentWeapon.isBoltAction)
        {
            transformToAttachWeapon = aimingPos;
        }


        if (canAttach)
        {
            UpdateHandTargets();

            // Smoothly interpolate weapon's position and rotation to the target transform
            currentWeapon.transform.position = Vector3.Lerp(currentWeapon.transform.position, transformToAttachWeapon.position, Time.deltaTime * transitionSpeed);
            currentWeapon.transform.rotation = Quaternion.Slerp(currentWeapon.transform.rotation, transformToAttachWeapon.rotation, Time.deltaTime * transitionSpeed);
            currentWeapon.transform.parent = transformToAttachWeapon;
        }
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
        if (weapons.Count < maxHeldWeaponCount)
        {
            weapons.Add(weapon); // Add the weapon to the dynamic list
            SelectWeapon(weapon);
        }
        else
        {
            // Destroy the current weapon and remove its reference 
            Destroy(weapons[currentWeaponIndex].gameObject);
            weapons.RemoveAt(currentWeaponIndex);
            weapons.Add(weapon); // Add the weapon to the dynamic list
            SelectWeapon(weapon);
        }
    }
    public Weapon HasWeapon(Weapon weaponToCheck)
    {
        foreach(Weapon weapon in weapons)
        {
            if(weapon.GetID() == weaponToCheck.GetID()) return weapon;
        }

        return null;
    }

    //private void TryDropWeapon()
    //{
    //    if (isMeleeing || weapons.Count <= 1) return;
    //    if(currentWeapon != null)
    //    {
    //        currentWeapon.Drop(throwForce);
    //        weapons.RemoveAt(currentWeaponIndex); // Remove the dropped weapon
    //        currentWeapon = null;
    //        SelectWeapon(0);
    //        if(currentWeapon == null)
    //        {
    //            UiManager.Instance.UpdateAmmoReserveText(0);
    //            UiManager.Instance.UpdateAmmoText(0);
    //        }
    //    }
    //}

    private void WeaponSwitch(Vector2 direction)
    {
        if (!isSwitching && weapons.Count > 1)
        {
            StartCoroutine(WeaponSwitchDelay(direction));
        }
    }
    private IEnumerator WeaponSwitchDelay(Vector2 direction)
    {
        isSwitching = true;

        int previousWeaponIndex = currentWeaponIndex;

        if (direction.y > 0)
        {
            currentWeaponIndex = (currentWeaponIndex + 1) % weapons.Count;
        }
        else if (direction.y < 0)
        {
            currentWeaponIndex = (currentWeaponIndex - 1 + weapons.Count) % weapons.Count;
        }

        // Don't switch if the next weapon is the current weapon
        if (previousWeaponIndex != currentWeaponIndex)
        {
            currentWeapon?.Unequip();
            SelectWeapon(currentWeaponIndex);
        }

        yield return new WaitForSeconds(scrollSwitchDelay);
        isSwitching = false;
    }

    private void SelectWeapon(Weapon weapon)
    {
        for (int i = 0; i < weapons.Count; i++)
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
        if (weapons.Count == 0) return;
        if (weapons[index] == null) return;

        for (int i = 0; i < weapons.Count; i++)
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

    private void TryStartAttacking()
    {
        player.playerMovement.CancelSprint();
        currentWeapon?.StartAttacking();
    }
    
    private void TryStartAiming()
    {
        player.playerMovement.CancelSprint();
        currentWeapon?.StartAiming();
    }

    private void Melee()
    {
        if (isMeleeing || Time.time < lastMeleeTime + meleeWeapon.swingDuration + meleeWeapon.meleeCooldown) return;
        lastMeleeTime = Time.time;
        player.playerMovement.CancelSprint();
        if (currentWeapon != null)
        {
            currentWeapon.gameObject.SetActive(false);
            currentWeapon.isAttacking = false;
            previousWeapon = currentWeapon;
            currentWeapon?.Unequip();
        }
        isMeleeing = true;
        meleeWeapon.gameObject.SetActive(true);
        meleeWeapon.Swing();
        currentWeapon = meleeWeapon;
        meleeTimer.SetTimer(meleeWeapon.swingDuration, EndMelee);
    }

    private void EndMelee()
    {
        isMeleeing = false;
        meleeWeapon.gameObject.SetActive(false);
        if(previousWeapon == null)
        {
            currentWeapon = null;
        }
        else
        {
            SelectWeapon(previousWeapon);
        }
    }
}