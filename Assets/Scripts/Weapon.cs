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
    Pistol,
    Sniper
}

[RequireComponent(typeof(AudioSource))]
public class Weapon : MonoBehaviour, IInteractable
{
    protected Animator animator;
    private Rigidbody rb;
    protected WeaponHolder holder;
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
    public bool isBoltAction = false;

    [Header("Attacking")]
    [SerializeField] private float fireRatePerSecond = 3f;
    [SerializeField] private WeaponClass weaponClass = WeaponClass.Rifle;
    protected float lastShotTime = 0;
    [SerializeField] protected LayerMask hitLayers;
    [SerializeField] private BulletTrail bulletTrailPrefab;
    [SerializeField] private MuzzleFlash[] muzzleFlashPrefabs;
    [SerializeField] private Casing casingPrefab;
    [SerializeField] protected Firemode firemode = Firemode.Auto;

    [Header("Penetration")] 
    [SerializeField] private float penetrationFactor = 0.5f;
    [SerializeField] private int allowedHitCount = 3;

    [Header("Burst Firing")]
    [SerializeField] private float burstCooldown = 1f;
    private int burstShotsFired = 0;
    private float timeSinceLastBurst = 0f;

    [Header("Recoil")]
    public Vector2 recoil;
    [SerializeField] private RecoilPattern recoilPattern;
    [SerializeField] private float recoilResetTimeSeconds = 1f;
    private int currentRecoilIndex = 0;

    [SerializeField] protected float screenShakeDuration = 0.1f;
    [SerializeField] protected float screenShakeAmount = 1.0f;

    [Header("Ammo and Damage")]
    [SerializeField] private int magSize = 30;
    [SerializeField] private int startingAmmoReserve = 1000;
    [SerializeField] private float baseDamage = 50.0f;
    [SerializeField] protected float damageFallOffDistance = 100.0f;
    [SerializeField] private float headShotMultiplier = 1.5f;
    private int ammoLeft = 0;
    private int ammoReserve = 0;

    [Header("Aiming")]
    [SerializeField] private float aimingZoomLevel = 1.2f;
    [SerializeField] private float cameraZOffset = 0.05f;

    [Header("Reloading")]
    [SerializeField] private float reloadTime = 0.5f;
    private Timer reloadTimer;

    [Header("Bolt Action")]
    [SerializeField] private float boltDelay = 0.2f;
    [SerializeField] private float boltActionLength = 0.5f;
    private Timer boltActionTimer;

    [Header("Audio")]
    [SerializeField] private AudioClip[] firingSounds;
    [SerializeField] private AudioClip reloadSound; 
    [SerializeField] private AudioClip aimInSound; 
    [SerializeField] private AudioClip pickUpClip;
    [SerializeField] private AudioClip boltAction; //optional
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
    [SerializeField] protected bool applyRecoil = true;

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

        if(weaponClass == WeaponClass.Sniper && firemode == Firemode.Single)
        {
            boltActionTimer = gameObject.AddComponent<Timer>();
        }

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

        weaponBody.ApplyWeaponSkin();
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

        PlayPickUpSound();
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
        //attacking
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

        //zooming
        if (holder != null)
        {
            if (CanZoom())
            {
                if (attachedScope && holder)
                {
                    attachedScope.SetZoom(true, holder.player.playerLook);
                }
                holder.player.playerLook.SetZoomLevel(aimingZoomLevel, cameraZOffset);
            }
            else
            {
                if (attachedScope && holder)
                {
                    attachedScope.SetZoom(false, holder.player.playerLook);
                }
                holder.player.playerLook.ResetZoomLevel();
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

    private bool CanZoom()
    {
        if (isAiming && !isReloading && !isBoltAction) return true;
        return false;
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

    protected virtual void Attack()
    {
        Vector3 shootDirection = holder.player.playerLook.playerCamera.transform.forward;
        Vector3 startPosition = holder.player.playerLook.playerCamera.transform.position;
        Debug.DrawRay(startPosition, shootDirection * 20.0f, Color.red, 1f);
        RaycastHit[] hits = Physics.RaycastAll(startPosition, shootDirection, damageFallOffDistance, hitLayers);

        float remainingDamage = baseDamage; 
        int remainingHits = allowedHitCount;
        if (hits.Length > 0)
        {
            foreach (RaycastHit hit in hits)
            {
                HandleHit(hit, remainingDamage); // Handle hit with remaining damage

                // Reduce penetration force after each hit
                remainingDamage *= penetrationFactor;

                // Decrease the remaining allowed hits
                remainingHits--;

                if (remainingHits <= 0 || remainingDamage <= 0)
                {
                    // Stop if we've reached the hit limit or damage is too low
                    break;
                }
            }
        }
        else
        {
            HandleMiss(shootDirection); // Handle the case where no enemies are hit
        }

        if (applyRecoil)
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
            if (weaponClass == WeaponClass.Sniper) PullBolt();
        }

    }

    protected void ApplyRecoil()
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

    protected void HandleHit(RaycastHit hit, float damage)
    {
        BulletTrail bulletTrail = Instantiate(bulletTrailPrefab, muzzleTransform.position, Quaternion.identity);
        bulletTrail.Init(hit.point, hit.normal);
        var collider = hit.collider;
        var damageable = collider.GetComponent<IDamageable>();
        if(damageable == null) damageable = collider.transform.root.GetComponent<IDamageable>();
        if (damageable != null)
        {
            float hitDamage = damage;
            if (collider.tag == "Head") hitDamage *= headShotMultiplier;

            damageable.Damage(hitDamage, hit.point, hit.normal);
            bulletTrail.hitCharacter = true;
        }
    }

    protected void HandleMiss(Vector3 shootDirection)
    {
        BulletTrail bulletTrail = Instantiate(bulletTrailPrefab, muzzleTransform.position, Quaternion.identity);

        Vector3 pointAlongShootDirection = shootDirection * damageFallOffDistance;
        bulletTrail.Init(pointAlongShootDirection, Vector3.zero);
        bulletTrail.spawnImpact = false;
    }

    protected void TakeAmmoFromMag(int magChange)
    {
        //take the ammo from the mag
        ammoLeft -= magChange;

        //If the weapon is held/equiped then update the ui
        if (!isHeld) return;

        UiManager.Instance.UpdateAmmoText(ammoLeft);
    }

    private void PullBolt()
    {
        PlayBoltActionSound();
        
        isBoltAction = true;
        animator.SetBool("BoltAction", true);
        boltActionTimer.SetTimer(boltActionLength, FinishBolt);
    }

    private void FinishBolt()
    {
        isBoltAction = false;
        animator.SetBool("BoltAction", false);
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

    private void PlayPickUpSound()
    {
        if(pickUpClip == null)
        {
            Debug.Log("Weapon does not have a assigned pickup sound");
        }
        audioSource.PlayOneShot(pickUpClip);
    }
    
    private void PlayBoltActionSound()
    {
        if(boltAction == null)
        {
            Debug.Log("Weapon does not have a assigned bolt action sound");
        }
        audioSource.clip = boltAction;
        audioSource.PlayDelayed(boltDelay);
    }

    protected void PlayRandomFiringSound()
    {
        if(firingSounds.Length == 0) return; //cannot play a firing sound because none is set

        int randomIndex = Random.Range(0, firingSounds.Length);
        AudioClip randomClip = firingSounds[randomIndex];

        audioSource.PlayOneShot(randomClip);
    }

    protected void SpawnRandomMuzzleFlash()
    {
        if (muzzleFlashPrefabs.Length == 0) return; //cannot spawn a muzzle flash because none is set

        int randomIndex = Random.Range(0, muzzleFlashPrefabs.Length);
        Instantiate(muzzleFlashPrefabs[randomIndex], muzzleTransform.position, muzzleTransform.rotation, muzzleTransform);
    }

    protected void SpawnCasing()
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
    }

    public void StopAiming()
    { 
        isAiming = false;

        UiManager.Instance.SetCrosshair(true);
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
