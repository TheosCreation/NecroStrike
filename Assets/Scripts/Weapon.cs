using System.Collections.Generic;
using UnityEngine;

public enum Firemode
{
    Auto,
    Burst,
    Single
}

public enum WeaponClass
{
    Rifle,
    Pistol
}

[RequireComponent(typeof(AudioSource))]
public class Weapon : MonoBehaviour, IInteractable
{
    private Animator animator;
    private Rigidbody rb;
    private WeaponHolder holder;
    [SerializeField] private Collider collisionCollider;
    [SerializeField] private Transform muzzleTransform;
    [SerializeField] private Transform casingEjectionTransform;
    public float motionReduction = 0.5f;
    [SerializeField] private bool isHeld = false;
    [SerializeField] private bool isEquip = false;
    public bool isAiming = false;
    public bool isAttacking = false;
    public bool isReloading = false;
    public bool isInspecting = false;
    [Header("Attacking")]
    [SerializeField] private float damage = 50.0f;
    [SerializeField] private float damageFallOffDistance = 100.0f;
    [SerializeField] private float fireRatePerSecond = 3f;
    [SerializeField] private WeaponClass weaponClass = WeaponClass.Rifle;
    float lastShotTime = 0;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private BulletTrail bulletTrailPrefab;
    [SerializeField] private MuzzleFlash[] muzzleFlashPrefabs;
    [SerializeField] private Casing casingPrefab;
    [SerializeField] private Firemode firemode = Firemode.Auto;
    [Header("Burst Firing")]
    [SerializeField] private float burstCooldown = 1f;
    private int burstShotsFired = 0;
    private float timeSinceLastBurst = 0f;

    [Header("Recoil")]
    public Vector2 recoil;
    [SerializeField] private RecoilPattern recoilPattern;
    [SerializeField] private float recoilResetTimeSeconds = 1f;
    private int currentRecoilIndex = 0;

    [SerializeField] private float screenShakeDuration = 0.1f;
    [SerializeField] private float screenShakeAmount = 1.0f;

    [Header("Ammo and Damage")]
    [SerializeField] private int magSize = 30;
    [SerializeField] private int startingAmmoReserve = 1000;
    private int ammoLeft = 0;
    private int ammoReserve = 0;

    [Header("Aiming")]
    [SerializeField] private float aimingZoomLevel = 1.2f;
    [SerializeField] private float cameraZOffset = 0.05f;

    [Header("Reloading")]
    [SerializeField] private float reloadTime = 0.5f;
    private Timer reloadTimer;

    [Header("Audio")]
    [SerializeField] private AudioClip[] firingSounds;
    [SerializeField] private AudioClip reloadSound; 
    [SerializeField] private AudioClip aimInSound; 
    private AudioSource audioSource;

    [Header("Right Hand Setup")]
    public Transform IKRightHandPos;
    public Transform IKRightIndexPos;
    public Transform IKRightMiddlePos;
    public Transform IKRightPinkyPos;
    public Transform IKRightRingPos;
    public Transform IKRightThumbPos;

    [Header("Left Hand Setup")]
    public Transform IKLeftHandPos;
    public Transform IKLeftIndexPos;
    public Transform IKLeftMiddlePos;
    public Transform IKLeftPinkyPos;
    public Transform IKLeftRingPos;
    public Transform IKLeftThumbPos;

    [Header("Debugging")]
    [SerializeField] private bool applyRecoil = true;

    public class AttachedWeaponPart
    {

        public WeaponBody.PartTypeAttachPoint partTypeAttachPoint;
        public WeaponPartSO weaponPartSO;
        public Transform spawnedTransform;

    }


    [SerializeField] private List<WeaponPartSO> defaultWeaponPartSOList;


    private WeaponBody weaponBody;
    private Scope attachedScope;
    private Dictionary<WeaponPartSO.PartType, AttachedWeaponPart> attachedWeaponPartDic;

    private void Awake()
    {
        weaponBody = GetComponent<WeaponBody>();
        animator = GetComponent<Animator>(); 
        rb = GetComponent<Rigidbody>(); 
        audioSource = GetComponent<AudioSource>();
        holder = transform.root.GetComponent<WeaponHolder>();
        reloadTimer = gameObject.AddComponent<Timer>();

        attachedWeaponPartDic = new Dictionary<WeaponPartSO.PartType, AttachedWeaponPart>();

        foreach (WeaponBody.PartTypeAttachPoint partTypeAttachPoint in weaponBody.GetPartTypeAttachPointList())
        {
            attachedWeaponPartDic[partTypeAttachPoint.partType] = new AttachedWeaponPart
            {
                partTypeAttachPoint = partTypeAttachPoint,
            };
        }

        foreach (WeaponPartSO weaponPartSO in defaultWeaponPartSOList)
        {
            SetPart(weaponPartSO);
        }
    }

    void Start()
    {
        ammoReserve = startingAmmoReserve;
        FillMag();

        if (holder != null)
        {
            PickUp();
        }
        else
        {
            Drop(0);
        }
    }

    public void PickUp()
    {
        isHeld = true;
        rb.isKinematic = true;
        collisionCollider.enabled = false;
        LayerController.Instance.SetGameObjectAndChildrenLayer(gameObject, LayerMask.NameToLayer("Weapon"));
        //holder = transform.root.GetComponent<WeaponHolder>();
    }

    public void Drop(float _force)
    {
        StopAiming();
        StopAttacking();
        Unequip();

        holder = null;
        transform.parent = null;
        isHeld = false;
        rb.isKinematic = false;
        collisionCollider.enabled = true;
        LayerController.Instance.SetGameObjectAndChildrenLayer(gameObject, LayerMask.NameToLayer("Default"));

        rb.AddForce(transform.forward * _force, ForceMode.Impulse);
    }

    public void Equip()
    {
        isEquip = true;

        UiManager.Instance.UpdateAmmoText(ammoLeft);
        UiManager.Instance.UpdateAmmoReserveText(ammoReserve);
    }

    public void Unequip()
    {
        isEquip = false;

        UiManager.Instance.UpdateAmmoText(0);
        UiManager.Instance.UpdateAmmoReserveText(0);
    }

    void Update()
    {
        if (isAttacking)
        {
            if (CanShoot() && isEquip && !isReloading && !isInspecting)
            {
                if (ammoLeft > 0)
                {
                    Attack();
                }
                else
                {
                    Reload();
                }
            }
        }
    }

    private bool CanShoot()
    {
        if (firemode == Firemode.Burst)
        {
            // Check if we are within the burst cooldown
            if (Time.time - lastShotTime >= 1 / fireRatePerSecond)
            {
                // Allow to shoot until 3 shots are fired
                if (burstShotsFired < 3)
                {
                    burstShotsFired++;
                    lastShotTime = Time.time; // Reset last shot time for next shot in the burst
                    return true;
                }
                else
                {
                    // If 3 shots are fired, reset burst shots after cooldown
                    if (Time.time - timeSinceLastBurst >= burstCooldown)
                    {
                        burstShotsFired = 0; // Reset burst shots
                        timeSinceLastBurst = Time.time; // Reset time since the last burst
                    }
                    return false;
                }
            }
            return false;
        }
        else
        {
            // Handle Auto and Single Fire modes
            if (Time.time - lastShotTime >= 1 / fireRatePerSecond)
            {
                return true;
            }
            return false;
        }
    }

    public void Inspect()
    {
        if (isReloading || isInspecting) return;

        isInspecting = true;
        animator.SetTrigger("Inspect");
    }

    public void FinishInpect()
    {
        isInspecting = false;
    }

    public void Interact(PlayerController player)
    {
        player.weaponHolder.Add(this);
        holder = player.weaponHolder;
        PickUp();
    }

    public void StartAttacking()
    {
        isAttacking = true;
    }

    private void Attack()
    {
        Vector3 shootDirection = holder.player.playerLook.playerCamera.transform.forward;
        Vector3 startPosition = holder.player.playerLook.playerCamera.transform.position;
        Debug.DrawRay(startPosition, shootDirection * 20.0f, Color.red, 1f);
        if (Physics.Raycast(startPosition, shootDirection, out RaycastHit hit, damageFallOffDistance, hitLayers))
        {
            HandleHit(hit);
        }
        else
        {
            HandleMiss(shootDirection);
        }

        if(applyRecoil)
            ApplyRecoil();

        TakeAmmoFromMag(1);
        holder.player.playerLook.TriggerScreenShake(screenShakeDuration, screenShakeAmount * motionReduction * 0.01f);
        PlayRandomFiringSound(); 
        SpawnRandomMuzzleFlash(); 
        SpawnCasing();
        animator.SetTrigger("Shoot");

        // Update the last shot time
        lastShotTime = Time.time;

        if(firemode == Firemode.Single)
        {
            isAttacking = false;
        }
    }

    private void ApplyRecoil()
    {
        float aimingReduction = isAiming ? motionReduction : 1f;

        // Ensure recoil is updated only when enough time has passed
        if (Time.time - lastShotTime >= recoilResetTimeSeconds)
        {
            recoil = Vector2.zero;
            recoil += recoilPattern.pattern[0] * aimingReduction;
            currentRecoilIndex = 1;
        }
        else
        {
            recoil += recoilPattern.pattern[currentRecoilIndex] * aimingReduction;

            if (currentRecoilIndex + 1 < recoilPattern.pattern.Length)
            {
                currentRecoilIndex++;
            }
            else
            {
                currentRecoilIndex = 0;
            }
        }

    }

    private void HandleHit(RaycastHit hit)
    {
        BulletTrail bulletTrail = Instantiate(bulletTrailPrefab, muzzleTransform.position, Quaternion.identity);
        bulletTrail.Init(hit.point, hit.normal);

        var damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.Damage(damage);
            bulletTrail.hitCharacter = true;
        }
    }
    
    private void HandleMiss(Vector3 shootDirection)
    {
        BulletTrail bulletTrail = Instantiate(bulletTrailPrefab, muzzleTransform.position, Quaternion.identity);

        Vector3 pointAlongShootDirection = shootDirection * damageFallOffDistance;
        bulletTrail.Init(pointAlongShootDirection, Vector3.zero);
        bulletTrail.spawnImpact = false;
    }

    private void TakeAmmoFromMag(int magChange)
    {
        //take the ammo from the mag
        ammoLeft -= magChange;

        //If the weapon is held/equiped then update the ui
        if (!isHeld) return;

        UiManager.Instance.UpdateAmmoText(ammoLeft);
    }

    private void FillMag()
    {
        // Calculate the amount of ammo needed to fill the magazine
        int ammoNeeded = magSize - ammoLeft;

        // Check if the reserve has enough ammo
        if (ammoReserve >= ammoNeeded)
        {
            // If there's enough ammo, fill the magazine completely
            ammoLeft += ammoNeeded;
            ammoReserve -= ammoNeeded;
        }
        else
        {
            // If not enough ammo, fill the magazine with whatever is left
            ammoLeft += ammoReserve;
            ammoReserve = 0;
        }

        //If the weapon is held/equiped then update the ui
        if (!isHeld) return;

        UiManager.Instance.UpdateAmmoText(ammoLeft);
        UiManager.Instance.UpdateAmmoReserveText(ammoReserve);
    }

    public void AddAmmoToReserve(int ammoAmount)
    {
        ammoReserve += ammoAmount;
        UiManager.Instance.UpdateAmmoReserveText(ammoReserve);
    }

    private void PlayRandomFiringSound()
    {
        if(firingSounds.Length == 0) return; //cannot play a firing sound because none is set

        int randomIndex = Random.Range(0, firingSounds.Length);
        AudioClip randomClip = firingSounds[randomIndex];

        audioSource.PlayOneShot(randomClip);
    }

    private void SpawnRandomMuzzleFlash()
    {
        if (muzzleFlashPrefabs.Length == 0) return; //cannot spawn a muzzle flash because none is set

        int randomIndex = Random.Range(0, muzzleFlashPrefabs.Length);
        Instantiate(muzzleFlashPrefabs[randomIndex], muzzleTransform.position, muzzleTransform.rotation, muzzleTransform);
    }

    private void SpawnCasing()
    {
        if (casingPrefab == null) return;

        Instantiate(casingPrefab, casingEjectionTransform.position, casingEjectionTransform.rotation);
    }

    private void PlayReloadSound()
    {
        audioSource.PlayOneShot(reloadSound);
    }
    
    private void PlayAimInSound()
    {
        audioSource.PlayOneShot(aimInSound);
    }

    public void StopAttacking()
    {
        isAttacking = false;
    }

    public void StartAiming()
    {
        if(isInspecting) return;

        isAiming = true;

        PlayAimInSound();

        UiManager.Instance.SetCrosshair(false);

        if(attachedScope)
        {
            attachedScope.SetZoom(true);
        }

        if (holder != null)
        {
            holder.player.playerLook.SetZoomLevel(aimingZoomLevel, cameraZOffset);
        }
    }

    public void StopAiming()
    { 
        isAiming = false;

        if (attachedScope)
        {
            attachedScope.SetZoom(false);
        }

        UiManager.Instance.SetCrosshair(true);
        if (holder != null)
        {
            holder.player.playerLook.ResetZoomLevel();
        }
    }

    public void Reload()
    {
        if (!isReloading && ammoLeft < magSize && ammoReserve > 0 && !isInspecting)
        {
            isReloading = true;

            PlayReloadSound(); 
            animator.SetTrigger("Reload");

            reloadTimer.SetTimer(reloadTime, FinishReload);
        }
    }

    private void FinishReload()
    {
        isReloading = false;
        FillMag();
    }

    public void SetPart(WeaponPartSO weaponPartSO)
    {
        // Destroy currently attached part
        if (attachedWeaponPartDic[weaponPartSO.partType].spawnedTransform != null)
        {
            Destroy(attachedWeaponPartDic[weaponPartSO.partType].spawnedTransform.gameObject);
        }

        // Spawn new part
        Transform spawnedPartTransform = Instantiate(weaponPartSO.prefab);
        AttachedWeaponPart attachedWeaponPart = attachedWeaponPartDic[weaponPartSO.partType];
        attachedWeaponPart.spawnedTransform = spawnedPartTransform;

        Transform attachPointTransform = attachedWeaponPart.partTypeAttachPoint.attachPointTransform;
        spawnedPartTransform.parent = attachPointTransform;
        spawnedPartTransform.localEulerAngles = Vector3.zero;
        spawnedPartTransform.localPosition = Vector3.zero;
        spawnedPartTransform.localScale = new Vector3(1,1,1);

        attachedWeaponPart.weaponPartSO = weaponPartSO;

        attachedWeaponPartDic[weaponPartSO.partType] = attachedWeaponPart;

        // Is it a barrel?
        if (weaponPartSO.partType == WeaponPartSO.PartType.Barrel)
        {
            BarrelWeaponPartSO barrelWeaponPartSO = (BarrelWeaponPartSO)weaponPartSO;

            AttachedWeaponPart barrelPartTypeAttachedWeaponPart = attachedWeaponPartDic[WeaponPartSO.PartType.Barrel];
            AttachedWeaponPart muzzlePartTypeAttachedWeaponPart = attachedWeaponPartDic[WeaponPartSO.PartType.Muzzle];

            muzzlePartTypeAttachedWeaponPart.partTypeAttachPoint.attachPointTransform.position =
                barrelPartTypeAttachedWeaponPart.partTypeAttachPoint.attachPointTransform.position +
                barrelPartTypeAttachedWeaponPart.partTypeAttachPoint.attachPointTransform.forward * barrelWeaponPartSO.muzzleOffset;
        }

        if(weaponPartSO.partType == WeaponPartSO.PartType.Scope)
        {
            attachedScope = spawnedPartTransform.GetComponent<Scope>();
        }
    }

    public WeaponPartSO GetWeaponPartSO(WeaponPartSO.PartType partType)
    {
        AttachedWeaponPart attachedWeaponPart = attachedWeaponPartDic[partType];
        return attachedWeaponPart.weaponPartSO;
    }

    public List<WeaponPartSO.PartType> GetWeaponPartTypeList()
    {
        return new List<WeaponPartSO.PartType>(attachedWeaponPartDic.Keys);
    }

    public WeaponBodySO GetWeaponBodySO()
    {
        return weaponBody.GetWeaponBodySO();
    }
}
