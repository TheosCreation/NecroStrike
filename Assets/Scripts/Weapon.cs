using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;
using Random = UnityEngine.Random;

public enum Firemode
{
    Auto,
    Burst,
    Single
}

public enum WeaponClass
{
    Rifle,
    Smg,
    Pistol,
    Sniper
}

[Serializable]
public struct WeaponStatData
{
    public float damage;
    public float rateOfFire;
    public int magSize;
    public float spread; // multiplier of max and min spreads
    public float penetration; //1 = no damge loss targetHit
    public float range;
    public float recoilReduction; // 1 = no recoil reduction
    public float aimInTime;
    public float sprintToFireTime; 
    public float reloadTime; 
    
    public WeaponStatData(
        float _damage,
        float _rateOfFire,
        int _magSize,
        float _spread,
        float _penetration,
        float _range,
        float _recoilReduction,
        float _aimInTime,
        float _sprintToFireTime,
        float _reloadTime)
    {
        damage = _damage;
        rateOfFire = _rateOfFire;
        magSize = _magSize;
        spread = _spread;
        penetration = _penetration;
        range = _range;
        recoilReduction = _recoilReduction;
        aimInTime = _aimInTime;
        sprintToFireTime = _sprintToFireTime;
        reloadTime = _reloadTime;
    }
}

[RequireComponent(typeof(AudioSource))]
public class Weapon : MonoBehaviour, IInteractable, IPausable
{
    protected Animator animator;
    private Rigidbody rb;
    public WeaponHolder holder;
    private WeaponStatData weaponStats;
    [SerializeField] private Collider collisionCollider;
    [SerializeField] private Transform muzzleTransform;
    [SerializeField] private Transform casingEjectionTransform;
    public float motionReduction = 0.5f;
    [SerializeField] private bool isHeld = false;
    [SerializeField] private bool isEquip = false;
    public bool isAiming = false;
    public bool aimingInput = false;
    public bool isAttacking = false;
    public bool isReloading = false;
    public bool isInspecting = false;
    public bool isBoltAction = false;
    public bool canResetSprint = true;

    public WeaponSettings settings;

    [Header("Attacking")]
    protected float lastShotTime = 0;

    private int burstShotsFired = 0;
    private float timeSinceLastBurst = 0f;
    public float currentSpread = 0f;
    private float currentMinSpread = 0f;
    private float currentMaxSpread = 0f;

    [Header("Recoil")]
    public Vector2 recoil;
    private int currentRecoilIndex = 0;

    private int ammoLeft = 0;
    private int ammoReserve = 0;

    private Timer reloadTimer;
    private Timer equipTimer;
    private Timer inspectTimer;
    private Timer aimingTimer;
    private Timer boltActionTimer;
    private Timer fireToSprintTimer;

    protected AudioSource audioSource;

    [Header("Animation Setup")]
    public Transform animationMotion;

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


    [SerializeField] private WeaponBody weaponBody;
    private Scope attachedScope;
    private Dictionary<WeaponPartSO.PartType, AttachedWeaponPart> attachedWeaponPartDic;

    private void Awake()
    {
        animator = GetComponent<Animator>(); 
        rb = GetComponent<Rigidbody>(); 
        audioSource = GetComponent<AudioSource>();
        holder = transform.root.GetComponent<WeaponHolder>();
        reloadTimer = gameObject.AddComponent<Timer>();
        equipTimer = gameObject.AddComponent<Timer>();
        inspectTimer = gameObject.AddComponent<Timer>();
        fireToSprintTimer = gameObject.AddComponent<Timer>();
        aimingTimer = gameObject.AddComponent<Timer>();

        if(settings.weaponClass == WeaponClass.Sniper && settings.firemode == Firemode.Single)
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
        CalculateWeaponStats();
    }

    void Start()
    {
        FillReserve();
        FillMag();

        if (holder != null)
        {
            PickUp(false);
        }
        else
        {
            Drop(0);
        }
    }

    public int GetID()
    {
        return settings.GetInstanceID();
    }

    public void SetAnimationActive(bool active)
    {
        if (!active)
        {
            animationMotion.localPosition = Vector3.zero;
            animationMotion.localRotation = Quaternion.identity;
        }
    }

    public void PickUp(bool playSound)
    {
        isHeld = true;
        rb.isKinematic = true;
        collisionCollider.enabled = false;
        LayerController.Instance.SetGameObjectAndChildrenLayer(gameObject, LayerMask.NameToLayer("Weapon"));

        if(playSound)
        {
            PlayPickUpSound();
        }
        //holder = transform.root.GetComponent<WeaponHolder>();
    }

    public virtual void Drop(float _force)
    {
        StopAiming();
        StopAttacking();
        CancelReload();
        Unequip();

        if (attachedScope && holder)
        {
            attachedScope.SetZoom(false);
        }
        if (holder != null)
        {
            holder.player.playerLook.ResetZoomLevel();
        }

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
        animator.SetTrigger("Equip");

        equipTimer.StopTimer();
        equipTimer.SetTimer(settings.equipTime, FinishEquip);

        UiManager.Instance.UpdateAmmoText(ammoLeft);
        UiManager.Instance.UpdateAmmoReserveText(ammoReserve);
    }

    private void FinishEquip()
    {
        isEquip = true;
    }

    public void Unequip()
    {
        isEquip = false;
        CancelBoltAction();

        CancelReload();
    }

    void Update()
    {
        if (!isEquip) return;

        //attacking
        if (isAttacking)
        {
            if (CanShoot() && !isInspecting)
            {
                if (ammoLeft > 0 && !isReloading)
                {
                    Attack();
                }
                else
                {
                    Reload();
                }
            }
        }

        //zooming and sprinting
        if (holder != null)
        {
            if (CanZoom())
            {
                if (attachedScope && holder)
                {
                    attachedScope.SetZoom(true);
                    holder.player.playerLook.SetZoomLevel(settings.aimingZoomLevel, settings.cameraZOffset, attachedScope.cameraFov / 30);
                }
                else
                {
                    holder.player.playerLook.SetZoomLevel(settings.aimingZoomLevel, settings.cameraZOffset, 0.8f);
                }
            }
            else
            {
                if (attachedScope && holder)
                {
                    attachedScope.SetZoom(false);
                }
                holder.player.playerLook.ResetZoomLevel();
            }

            if (holder.player.playerMovement.isSprinting)
            {
                animator.SetBool("Sprint", true);
            }
            else
            {
                animator.SetBool("Sprint", false);
            }
        }

        //spread return
        currentSpread = Mathf.Lerp(currentSpread, settings.minSpread, Time.deltaTime * settings.spreadRecoverRate);
    }

    private bool CanShoot()
    {
        float timeBetweenShots = 60f / weaponStats.rateOfFire; // Converts RPM to seconds per shot

        // Ensure enough time has passed since sprinting to allow shooting
        if (Time.time - holder.player.playerMovement.timeSinceSprintEnd < weaponStats.sprintToFireTime)
        {
            return false; // Block shooting until sprint-to-fire delay is over
        }

        if (settings.firemode == Firemode.Burst)
        {
            // Check if we are within the burst cooldown
            if (Time.time - lastShotTime >= timeBetweenShots)
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
                    if (Time.time - timeSinceLastBurst >= settings.burstCooldown)
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
            if (Time.time - lastShotTime >= timeBetweenShots)
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

    public virtual void Inspect()
    {
        if (isReloading || isInspecting || aimingInput) return;

        isInspecting = true;
        animator.SetTrigger("Inspect");
        inspectTimer.SetTimer(settings.inspectTime, FinishInpect);
    }

    public void CancelInspect()
    {   
        if(isInspecting)
        {
            isInspecting = false;
            inspectTimer.StopTimer();
            animator.SetTrigger("CancelInspect");
        }
    }

    public void FinishInpect()
    {
        isInspecting = false;
    }

    public void Interact(PlayerController player)
    {
        player.weaponHolder.Add(this);
        holder = player.weaponHolder;
        PickUp(true);
    }

    public void StartAttacking()
    {
        isAttacking = true;
        CancelInspect();
    }

    protected virtual void Attack()
    {
        bool spawnedBulletTrail = false;
        Vector3 shootDirection = holder.player.playerLook.playerCamera.transform.forward;

        // If not aiming, add spread
        if (!isAiming)
        {
            shootDirection.x += Random.Range(-currentSpread, currentSpread);
            shootDirection.y += Random.Range(-currentSpread, currentSpread);
            shootDirection.z += Random.Range(-currentSpread, currentSpread);
            shootDirection.Normalize();
        }

        Vector3 startPosition = holder.player.playerLook.playerCamera.transform.position;

        Debug.DrawRay(startPosition, shootDirection * weaponStats.range, Color.red, 1f);
        RaycastHit[] hits = Physics.RaycastAll(startPosition, shootDirection, weaponStats.range, settings.hitLayers);

        // Sort the hits by distance to ensure the closest object is processed first
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        // Instantiate the bullet trail at the muzzle position
        BulletTrail bulletTrail = Instantiate(settings.bulletTrailPrefab, muzzleTransform.position, Quaternion.identity);
        bool hitATrigger = false;
        if (hits.Length > 0)
        {
            float remainingDamage = weaponStats.damage;

            // Loop through sorted hits and process each hit
            foreach (RaycastHit hit in hits)
            {
                // Ignore trigger colliders
                if (hit.collider.isTrigger)
                {
                    hitATrigger = true;
                    continue;
                }

                if (!spawnedBulletTrail)
                {
                    // Spawn bullet trail on the first (closest) hit point
                    bulletTrail.Init(hit.point, hit.normal);
                    spawnedBulletTrail = true;
                }

                // Handle the hit and reduce damage based on penetration factor
                HandleHit(hit, remainingDamage, bulletTrail);
                remainingDamage *= weaponStats.penetration;

                // Stop processing if the remaining damage is too low
                if (remainingDamage <= 0)
                {
                    break;
                }

                //if it was unpenerable lets not continue
                if (hit.collider.gameObject.layer == 0) break;
            }
        }
        else
        {
            HandleMiss(startPosition, shootDirection, bulletTrail);
            spawnedBulletTrail = true;
        }

        if(hitATrigger && !spawnedBulletTrail)
        {
            spawnedBulletTrail = true;
            HandleMiss(startPosition, shootDirection, bulletTrail);
        }

        // Apply recoil and handle ammo consumption
        if (applyRecoil) ApplyRecoil();
        TakeAmmoFromMag(1);
        holder.player.playerLook.TriggerScreenShake(settings.screenShakeDuration, settings.screenShakeAmount * motionReduction * 0.01f);
        PlayRandomFiringSound();
        SpawnRandomMuzzleFlash();
        SpawnCasing();
        animator.SetTrigger("Shoot");

        //Add spread
        currentSpread += settings.spreadIncreasePerShot;
        currentSpread = Mathf.Clamp(currentSpread, currentMinSpread, currentMaxSpread);

        // Update the last shot time and handle weapon fire mode
        lastShotTime = Time.time;

        if (settings.firemode == Firemode.Single)
        {
            isAttacking = false;
            if (settings.weaponClass == WeaponClass.Sniper) PullBolt();
        }
    }


    protected void ApplyRecoil()
    {
        float currentRecoilReduction = weaponStats.recoilReduction; //gets any recoil reduction from the attachments
        if(isAiming) currentRecoilReduction *= (1 - settings.aimingRecoilReduction); // reduces it further when we are aiming

        if (Time.time - lastShotTime >= settings.recoilResetTimeSeconds)
        {
            recoil = Vector2.zero;
            recoil += settings.recoilPattern.pattern[0] * currentRecoilReduction;
            currentRecoilIndex = 1;
        }
        else
        {
            recoil += settings.recoilPattern.pattern[currentRecoilIndex] * currentRecoilReduction;

            if (currentRecoilIndex + 1 < settings.recoilPattern.pattern.Length)
            {
                currentRecoilIndex++;
            }
            else
            {
                currentRecoilIndex = 0;
            }
        }

    }

    protected void HandleMiss(Vector3 startPosition, Vector3 shootDirection, BulletTrail trail)
    {
        // No hits, create a bullet trail toward the maximum distance
        Vector3 missPosition = startPosition + shootDirection * weaponStats.range;
        Debug.DrawLine(startPosition, missPosition, Color.red, 6.0f);
        trail.Init(missPosition, Vector3.zero);
        trail.spawnImpact = false;
    }

    protected void HandleHit(RaycastHit hit, float damage, BulletTrail trail)
    {
        var collider = hit.collider;
        var enemy = collider.GetComponent<Enemy>();
        if(enemy == null) enemy = collider.transform.root.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.SetLastHitPlayerReference(holder.player);
            enemy.hitFromMelee = false;
            trail.hitCharacter = true;
            float hitDamage = damage;
            if (collider.tag == "Head")
            {
                if (Random.Range(0, 100) < 20)  // 20% chance (0-19 out of 100)
                {
                    enemy.HitHead();
                }

                hitDamage *= settings.headShotMultiplier;
            }

            holder.player.Points += 10; //give the player 10 points
            enemy.Damage(hitDamage, hit.point, hit.normal);
        }
    }

    protected void TakeAmmoFromMag(int magChange)
    {
        //take the ammo from the mag
        ammoLeft -= magChange;

        if (!isHeld) return;

        UiManager.Instance.UpdateAmmoText(ammoLeft);
    }

    private void PullBolt()
    {
        PlayBoltActionSound();
        
        isBoltAction = true;
        animator.SetBool("BoltAction", true);
        boltActionTimer.SetTimer(settings.boltActionLength, FinishBolt);
    }

    private void FinishBolt()
    {
        isBoltAction = false;
        animator.SetBool("BoltAction", false);
    }

    public void FillMag()
    {
        // Calculate the amount of ammo needed to fill the magazine
        int ammoNeeded = weaponStats.magSize - ammoLeft;

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
        if (!isHeld && !isEquip) return;

        UiManager.Instance.UpdateAmmoText(ammoLeft);
        UiManager.Instance.UpdateAmmoReserveText(ammoReserve);
    }

    public void FillReserve()
    {
        ammoReserve = settings.maxAmmoReserve;

        if (!isHeld && !isEquip) return;

        UiManager.Instance.UpdateAmmoReserveText(ammoReserve);
    }

    public void AddAmmoToReserve(int ammoAmount)
    {
        ammoReserve += ammoAmount;
        UiManager.Instance.UpdateAmmoReserveText(ammoReserve);
    }

    private void PlayPickUpSound()
    {
        if(settings.pickUpClip == null)
        {
            Debug.Log("Weapon does not have a assigned pickup sound");
        }
        audioSource.PlayOneShot(settings.pickUpClip);
    }
    
    private void PlayBoltActionSound()
    {
        if(settings.boltAction == null)
        {
            Debug.Log("Weapon does not have a assigned bolt action sound");
        }
        audioSource.clip = settings.boltAction;
        audioSource.PlayDelayed(settings.boltDelay);
    }

    protected void PlayRandomFiringSound()
    {
        if(settings.firingSounds.Length == 0) return; //cannot play a firing sound because none is set

        int randomIndex = Random.Range(0, settings.firingSounds.Length);
        AudioClip randomClip = settings.firingSounds[randomIndex];

        audioSource.PlayOneShot(randomClip);
    }

    protected void SpawnRandomMuzzleFlash()
    {
        if (settings.muzzleFlashPrefabs.Length == 0) return; //cannot spawn a muzzle flash because none is set

        int randomIndex = Random.Range(0, settings.muzzleFlashPrefabs.Length);
        Instantiate(settings.muzzleFlashPrefabs[randomIndex], muzzleTransform.position, muzzleTransform.rotation, muzzleTransform);
    }

    protected void SpawnCasing()
    {
        if (settings.casingPrefab == null) return;

        Instantiate(settings.casingPrefab, casingEjectionTransform.position, casingEjectionTransform.rotation);
    }

    private void PlayReloadSound()
    {
        audioSource.PlayOneShot(settings.reloadSound);
    }
    
    private void PlayAimInSound()
    {
        audioSource.PlayOneShot(settings.aimInSound);
    }

    public void StopAttacking()
    {
        isAttacking = false;

        canResetSprint = false;
        fireToSprintTimer.StopTimer();
        fireToSprintTimer.SetTimer(settings.baseFireToSprintDelay, () =>
        {
            canResetSprint = true;
            if (holder && !isInspecting && !isReloading & !aimingInput)
            {
                holder.player.playerMovement.ResetSprint();
            }
        });
    }

    public virtual void StartAiming()
    {
        CancelInspect();

        aimingInput = true;

        aimingTimer.StopTimer();
        aimingTimer.SetTimer(weaponStats.aimInTime, FinishAimIn);

        UiManager.Instance.SetCrosshair(false);
    }
    private void FinishAimIn()
    {
        PlayAimInSound();
        isAiming = true;
    }
    public void StopAiming()
    {
        aimingTimer.StopTimer();
        isAiming = false;
        aimingInput = false;
        UiManager.Instance.SetCrosshair(true);

        if(holder && !isInspecting && !isReloading && !isAttacking && canResetSprint)
        {
            holder.player.playerMovement.ResetSprint();
        }
    }

    public virtual void Reload()
    {
        if (!isReloading && ammoLeft < weaponStats.magSize && ammoReserve > 0)
        {
            CancelInspect();

            isReloading = true;

            PlayReloadSound(); 
            animator.SetTrigger("Reload");

            reloadTimer.SetTimer(weaponStats.reloadTime, FinishReload);

            holder.player.playerMovement.canSprint = false;
        }
    }

    private void FinishReload()
    {
        isReloading = false; 
        if (holder && !isInspecting && !aimingInput && !isAttacking && canResetSprint)
        {
            holder.player.playerMovement.ResetSprint();
        }
        FillMag();
    }

    public void CancelBoltAction()
    {
        if (isBoltAction)
        {
            boltActionTimer.StopTimer();
            isBoltAction = false;
            animator.SetTrigger("CancelBoltAction");
        }
    }

    public void CancelReload()
    {
        if(isReloading)
        {
            reloadTimer.StopTimer();
            isReloading = false;
            holder.player.playerMovement.canSprint = true;
            audioSource.Stop();
            animator.SetTrigger("CancelReload");
        }
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
        spawnedPartTransform.localPosition = weaponPartSO.spawnOffset;
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
    
    public GameObject GetWeaponBodyBase()
    {
        return weaponBody.GetBaseBody();
    }

    public void OnPause()
    {
        audioSource.Pause();
    }

    public void OnUnPause()
    {
        audioSource.UnPause();
    }

    public string GetInteractionText(PlayerController player)
    {
        return $"Pick up {gameObject.name}";
    }

    private void CalculateWeaponStats()
    {
        weaponStats = new WeaponStatData(settings.baseDamage, settings.baseRateOfFire, settings.baseMagSize, 1f, settings.penetrationFactor, settings.baseRange, 
            1f, settings.baseAimInTime, settings.baseSprintToFireDelay, settings.baseReloadTime);

        //retreive the stats and apply them
        foreach (WeaponPartSO.PartType part in GetWeaponPartTypeList())
        {
            Transform spawnedTranform = attachedWeaponPartDic[part].spawnedTransform;
            if (spawnedTranform == null) continue;

            if (spawnedTranform.TryGetComponent<Attachment>(out var attachment))
            {
                attachment.Apply(ref weaponStats);
            }
            else
            {
                Debug.Log($"Attachment does not have Attachment script: {spawnedTranform.name}");
            }
        }

        CalculateSpread();
    }

    private void CalculateSpread()
    {
        currentMinSpread = settings.minSpread * weaponStats.spread;
        currentMaxSpread = settings.maxSpread * weaponStats.spread;

        currentMinSpread = Mathf.Clamp(currentMinSpread, 0f, currentMaxSpread);
        currentMaxSpread = Mathf.Clamp(currentMaxSpread, currentMinSpread, Mathf.Infinity);
    }
}
