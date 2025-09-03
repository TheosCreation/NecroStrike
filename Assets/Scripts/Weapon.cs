using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum Firemode { Auto, Burst, Single }
public enum WeaponClass { Rifle, Smg, Pistol, Sniper }

[Serializable]
public struct WeaponStatData
{
    public float damage;
    public float fireRate;
    public int magSize;
    public int maxAmmoReserve;
    public float spread;          // multiplier of max and min spreads
    public float penetration;     // 1 = no damage loss targetHit
    public float range;
    public float recoilReduction; // 1 = no recoil reduction
    public float aimInTime;
    public float sprintToFireTime;
    public float reloadTime;

    public WeaponStatData(
        float _damage, float _rateOfFire, int _magSize, int _maxAmmoReserve, float _spread, float _penetration,
        float _range, float _recoilReduction, float _aimInTime, float _sprintToFireTime, float _reloadTime)
    {
        damage = _damage;
        fireRate = _rateOfFire;
        magSize = _magSize;
        maxAmmoReserve = _maxAmmoReserve;
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
    public bool tryingToAttack = false; 
    private bool prevTryingToAttack = false;
    public bool tryingToAim = false;
    public bool tryingToReload = false;
    public bool tryingToInspect = false;
    public bool isReloading = false;
    public bool isInspecting = false;
    public bool isBoltAction = false;
    public bool canResetSprint = true;
    public bool forceReload = false;

    public WeaponSettings settings;

    [Header("Attacking")]
    protected float lastShotTime = 0;
    private int burstShotsFired = 0;
    private float timeSinceLastBurst = 0f;
    public float currentSpread = 0f;
    private float currentMinSpread = 0f;
    private float currentMaxSpread = 0f;
    private bool singleFireLatch = false;

    [Header("Recoil")]
    public Vector2 recoil;
    private int currentRecoilIndex = 0;

    private int ammoLeft = 0;
    private int ammoReserve = 0;

    // Replaced timers with timestamps
    public bool equippingInProgress = false;
    private float equipEndTime = 0f;

    public bool aimingInProgress = false;
    private float aimEndTime = 0f;

    public bool reloadingInProgress = false; // mirrors isReloading for clarity during transition
    private float reloadEndTime = 0f;

    public bool inspectingInProgress = false;
    private float inspectEndTime = 0f;

    private float boltEndTime = 0f;

    public bool fireToSprintCooldown = false;
    private float fireToSprintEndTime = 0f;

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

        if (holder != null) PickUp(false);
        else Drop(0);
    }

    public int GetID() => settings.GetInstanceID();

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
        if (playSound) PlayPickUpSound();
    }

    public virtual void Drop(float _force)
    {
        StopAiming();
        tryingToAim = false;
        tryingToAttack = false;
        tryingToReload = false;

        CancelReload();
        Unequip();

        if (attachedScope && holder) attachedScope.SetZoom(false);
        if (holder != null) holder.player.playerLook.ResetZoomLevel();

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
        //animator.SetTrigger("Equip");
        equippingInProgress = true;
        equipEndTime = Time.time + settings.equipTime;

        UiManager.Instance.UpdateAmmoText(ammoLeft);
        UiManager.Instance.UpdateAmmoReserveText(ammoReserve);
    }

    private void FinishEquip()
    {
        isEquip = true;
        equippingInProgress = false;
    }

    public void Unequip()
    {
        isEquip = false;
        CancelBoltAction();
        CancelReload();
    }

    public void StopAiming()
    {
        aimingInProgress = false;
        isAiming = false;
        UiManager.Instance.SetCrosshair(true);

        if (holder && !isInspecting && !isReloading && !tryingToAttack && canResetSprint)
        {
            holder.player.playerMovement.ResetSprint();
        }
    }

    void Update()
    {
        // guard
        if (!isEquip)
        {
            // still allow equip completion timing
            if (equippingInProgress && Time.time >= equipEndTime) FinishEquip();
            return;
        }

        if (!tryingToAttack && prevTryingToAttack)
        {
            canResetSprint = false;
            fireToSprintCooldown = true;
            fireToSprintEndTime = Time.time + settings.baseFireToSprintDelay;
        }
        prevTryingToAttack = tryingToAttack;

        // Equip completion
        if (equippingInProgress && Time.time >= equipEndTime) FinishEquip();

        // Aiming state machine
        if (tryingToAim && !isReloading && !isBoltAction)
        {
            if (!isAiming && !aimingInProgress)
            {
                aimingInProgress = true;
                aimEndTime = Time.time + weaponStats.aimInTime;
            }
            if (!isAiming && aimingInProgress && Time.time >= aimEndTime)
            {
                aimingInProgress = false;
                isAiming = true;
                PlayAimInSound();
                UiManager.Instance.SetCrosshair(false);
            }
        }
        else
        {
            if (isAiming || aimingInProgress) StopAiming();
        }

        // Inspect state machine
        if (tryingToInspect && !isInspecting && !inspectingInProgress && !isReloading && !tryingToAim && !tryingToAttack)
        {
            isInspecting = true;
            inspectingInProgress = true;
            animator.SetTrigger("Inspect");
            inspectEndTime = Time.time + settings.inspectTime;
        }
        else if (!tryingToInspect && (isInspecting || inspectingInProgress))
        {
            CancelInspect();
        }

        // Reload state machine
        if ((tryingToReload || forceReload) && !isReloading && !reloadingInProgress && ammoLeft < weaponStats.magSize && ammoReserve > 0)
        {
            isReloading = true;
            reloadingInProgress = true;
            reloadEndTime = Time.time + weaponStats.reloadTime;
            forceReload = false;

            PlayReloadSound();
            animator.SetTrigger("Reload");
            if (holder) holder.player.playerMovement.canSprint = false;
        }
        if (reloadingInProgress && Time.time >= reloadEndTime)
        {
            isReloading = false;
            reloadingInProgress = false;

            if (holder && !isInspecting && !tryingToAim && !tryingToAttack && canResetSprint)
            {
                holder.player.playerMovement.ResetSprint();
            }
            FillMag();
        }

        // Attacking
        if (!tryingToAttack)
        {
            singleFireLatch = false;
            if (settings.firemode == Firemode.Burst) burstShotsFired = 0;
        }

        if (tryingToAttack && !isInspecting && !tryingToInspect)
        {
            CancelInspect();
            if (CanShoot())
            {
                if (ammoLeft > 0 && !isReloading)
                {
                    Attack();
                }
                else if(!reloadingInProgress)
                {
                    forceReload = true;
                }
            }
        }

        // Inspect timing
        if (inspectingInProgress && Time.time >= inspectEndTime)
        {
            isInspecting = false;
            inspectingInProgress = false;
        }

        // Bolt action timing
        if (isBoltAction && Time.time >= boltEndTime)
        {
            isBoltAction = false;
            animator.SetBool("BoltAction", false);
        }

        // Fire-to-sprint cooldown
        if (fireToSprintCooldown && Time.time >= fireToSprintEndTime)
        {
            fireToSprintCooldown = false;
            canResetSprint = true;
            if (holder && !isInspecting && !isReloading && !tryingToAim)
            {
                holder.player.playerMovement.ResetSprint();
            }
        }

        // Zoom and sprint anim
        if (holder != null)
        {
            if (CanZoom())
            {
                if (attachedScope) attachedScope.SetZoom(true);
                holder.player.playerLook.SetZoomLevel(settings.aimingZoomLevel, settings.cameraZOffset);
            }
            else
            {
                if (attachedScope) attachedScope.SetZoom(false);
                holder.player.playerLook.ResetZoomLevel();
            }

            animator.SetBool("Sprint", holder.player.playerMovement.isSprinting);
        }

        // spread return
        currentSpread = Mathf.Lerp(currentSpread, settings.minSpread, Time.deltaTime * settings.spreadRecoverRate);
    }

    private bool CanShoot()
    {
        float timeBetweenShots = (weaponStats.fireRate > 0f) ? 1f / weaponStats.fireRate : float.PositiveInfinity;

        // sprint-to-fire gate
        if (holder != null && Time.time - holder.player.playerMovement.timeSinceSprintEnd < weaponStats.sprintToFireTime)
            return false;

        // Single: require trigger release between shots
        if (settings.firemode == Firemode.Single)
        {
            if (singleFireLatch) return false;
            return Time.time >= lastShotTime + timeBetweenShots;
        }

        // Burst
        if (settings.firemode == Firemode.Burst)
        {
            if (Time.time >= lastShotTime + timeBetweenShots)
            {
                if (burstShotsFired < 3)
                {
                    burstShotsFired++;
                    lastShotTime = Time.time;
                    return true;
                }
                if (Time.time - timeSinceLastBurst >= settings.burstCooldown)
                {
                    burstShotsFired = 0;
                    timeSinceLastBurst = Time.time;
                }
            }
            return false;
        }

        // Auto
        return Time.time >= lastShotTime + timeBetweenShots;
    }

    private bool CanZoom() => isAiming && !isReloading && !isBoltAction;

    public virtual void Inspect()
    {
        if (isReloading || isInspecting || tryingToAim) return;

        isInspecting = true;
        inspectingInProgress = true;
        animator.SetTrigger("Inspect");
        inspectEndTime = Time.time + settings.inspectTime;
    }

    public void CancelInspect()
    {
        if (isInspecting || inspectingInProgress)
        {
            isInspecting = false;
            inspectingInProgress = false;
            animator.SetTrigger("CancelInspect");
        }
    }

    public void FinishInpect()
    {
        isInspecting = false;
        inspectingInProgress = false;
    }

    public void Interact(PlayerController player)
    {
        player.weaponHolder.Add(this);
        holder = player.weaponHolder;
        PickUp(true);
    }

    protected virtual void Attack()
    {
        bool spawnedBulletTrail = false;
        Vector3 shootDirection = holder.player.playerLook.playerCamera.transform.forward;

        if (!isAiming)
        {
            shootDirection.x += Random.Range(-currentSpread, currentSpread);
            shootDirection.y += Random.Range(-currentSpread, currentSpread);
            shootDirection.z += Random.Range(-currentSpread, currentSpread);
            shootDirection.Normalize();
        }
        if (settings.firemode == Firemode.Single)
        {
            singleFireLatch = true;
        }

        Vector3 startPosition = holder.player.playerLook.playerCamera.transform.position;

        Debug.DrawRay(startPosition, shootDirection * weaponStats.range, Color.red, 1f);
        RaycastHit[] hits = Physics.RaycastAll(startPosition, shootDirection, weaponStats.range, settings.hitLayers);
        Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        BulletTrail bulletTrail = Instantiate(settings.bulletTrailPrefab, muzzleTransform.position, Quaternion.identity);
        bool hitATrigger = false;

        if (hits.Length > 0)
        {
            float remainingDamage = weaponStats.damage;
            UiManager.Instance.FlashHitMarker();

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.isTrigger) { hitATrigger = true; continue; }

                if (!spawnedBulletTrail)
                {
                    bulletTrail.Init(hit.point, hit.normal);
                    spawnedBulletTrail = true;
                }

                HandleHit(hit, remainingDamage, bulletTrail);
                remainingDamage *= weaponStats.penetration;

                if (remainingDamage <= 0) break;
                if (hit.collider.gameObject.layer == 0) break;
            }
        }
        else
        {
            HandleMiss(startPosition, shootDirection, bulletTrail);
            spawnedBulletTrail = true;
        }

        if (hitATrigger && !spawnedBulletTrail)
        {
            spawnedBulletTrail = true;
            HandleMiss(startPosition, shootDirection, bulletTrail);
        }

        if (applyRecoil) ApplyRecoil();
        TakeAmmoFromMag(1);
        holder.player.playerLook.CameraShake(settings.screenShakeAmount * motionReduction);
        PlayRandomFiringSound();
        SpawnRandomMuzzleFlash();
        SpawnCasing();
        animator.SetTrigger("Shoot");

        currentSpread += settings.spreadIncreasePerShot;
        currentSpread = Mathf.Clamp(currentSpread, currentMinSpread, currentMaxSpread);

        lastShotTime = Time.time;

        if (settings.firemode == Firemode.Single)
        {
            tryingToAttack = false;
            if (settings.weaponClass == WeaponClass.Sniper) PullBolt();
        }
        if (ammoLeft <= 0)
        {
            forceReload = true;
        }
    }

    protected void ApplyRecoil()
    {
        float currentRecoilReduction = weaponStats.recoilReduction;
        if (isAiming) currentRecoilReduction *= (1 - settings.aimingRecoilReduction);

        if (Time.time - lastShotTime >= settings.recoilResetTimeSeconds)
        {
            recoil = Vector2.zero;
            recoil += settings.recoilPattern.pattern[0] * currentRecoilReduction;
            currentRecoilIndex = 1;
        }
        else
        {
            recoil += settings.recoilPattern.pattern[currentRecoilIndex] * currentRecoilReduction;
            if (currentRecoilIndex + 1 < settings.recoilPattern.pattern.Length) currentRecoilIndex++;
            else currentRecoilIndex = 0;
        }
    }

    protected void HandleMiss(Vector3 startPosition, Vector3 shootDirection, BulletTrail trail)
    {
        Vector3 missPosition = startPosition + shootDirection * weaponStats.range;
        Debug.DrawLine(startPosition, missPosition, Color.red, 6.0f);
        trail.Init(missPosition, Vector3.zero);
        trail.spawnImpact = false;
    }

    protected void HandleHit(RaycastHit hit, float damage, BulletTrail trail)
    {
        Collider collider = hit.collider;
        Enemy enemy = collider.GetComponent<Enemy>();
        if (enemy == null) enemy = collider.transform.root.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.SetLastHitPlayerReference(holder.player);
            enemy.hitFromMelee = false;
            trail.hitCharacter = true;
            float hitDamage = damage;
            if (collider.tag == "Head")
            {
                if (Random.Range(0, 100) < 20) enemy.HitHead();
                hitDamage *= settings.headShotMultiplier;
            }

            holder.player.Points += 10;
            enemy.Damage(hitDamage, hit.point, hit.normal);
        }
    }

    protected void TakeAmmoFromMag(int magChange)
    {
        ammoLeft -= magChange;
        if (!isHeld) return;
        UiManager.Instance.UpdateAmmoText(ammoLeft);
    }

    private void PullBolt()
    {
        PlayBoltActionSound();
        isBoltAction = true;
        animator.SetBool("BoltAction", true);
        boltEndTime = Time.time + settings.boltActionLength;
    }

    public void FillMag()
    {
        int ammoNeeded = weaponStats.magSize - ammoLeft;
        if (ammoReserve >= ammoNeeded)
        {
            ammoLeft += ammoNeeded;
            ammoReserve -= ammoNeeded;
        }
        else
        {
            ammoLeft += ammoReserve;
            ammoReserve = 0;
        }

        if (!isHeld && !isEquip) return;
        UiManager.Instance.UpdateAmmoText(ammoLeft);
        UiManager.Instance.UpdateAmmoReserveText(ammoReserve);
    }
    public bool IsReserveOrMagNotFill()
    {
        return ammoReserve < weaponStats.maxAmmoReserve || ammoLeft < weaponStats.magSize;
    }
    public void FillReserve()
    {
        ammoReserve = weaponStats.maxAmmoReserve;
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
        if (settings.pickUpClip == null) { Debug.Log("Weapon does not have a assigned pickup sound"); return; }
        audioSource.PlayOneShot(settings.pickUpClip);
    }

    private void PlayBoltActionSound()
    {
        if (settings.boltAction == null) { Debug.Log("Weapon does not have a assigned bolt action sound"); return; }
        audioSource.clip = settings.boltAction;
        audioSource.PlayDelayed(settings.boltDelay);
    }

    protected void PlayRandomFiringSound()
    {
        if (settings.firingSounds.Length == 0) return;
        int randomIndex = Random.Range(0, settings.firingSounds.Length);
        AudioClip randomClip = settings.firingSounds[randomIndex];
        audioSource.PlayOneShot(randomClip);
    }

    protected void SpawnRandomMuzzleFlash()
    {
        if (settings.muzzleFlashPrefabs.Length == 0) return;
        int randomIndex = Random.Range(0, settings.muzzleFlashPrefabs.Length);
        Instantiate(settings.muzzleFlashPrefabs[randomIndex], muzzleTransform.position, muzzleTransform.rotation, muzzleTransform);
    }

    protected void SpawnCasing()
    {
        if (settings.casingPrefab == null) return;
        Casing casing = Instantiate(settings.casingPrefab, casingEjectionTransform.position, casingEjectionTransform.rotation);
        casing.rb.linearVelocity = holder.player.playerMovement.rb.linearVelocity;
        casing.rb.angularVelocity = holder.player.playerMovement.rb.angularVelocity;
        casing.Activate();
    }

    private void PlayReloadSound()
    {
        if (settings.reloadSound != null) audioSource.PlayOneShot(settings.reloadSound);
    }

    private void PlayAimInSound()
    {
        if (settings.aimInSound != null) audioSource.PlayOneShot(settings.aimInSound);
    }

    public void CancelBoltAction()
    {
        if (isBoltAction)
        {
            isBoltAction = false;
            animator.SetTrigger("CancelBoltAction");
            animator.SetBool("BoltAction", false);
        }
    }

    public void CancelReload()
    {
        if (reloadingInProgress)
        {
            isReloading = false;
            reloadingInProgress = false;
            if (holder) holder.player.playerMovement.canSprint = true;
            audioSource.Stop();

            animator.SetTrigger("CancelReload");
        }
    }

    public void SetPart(WeaponPartSO weaponPartSO)
    {
        if (attachedWeaponPartDic[weaponPartSO.partType].spawnedTransform != null)
        {
            Destroy(attachedWeaponPartDic[weaponPartSO.partType].spawnedTransform.gameObject);
        }

        Transform spawnedPartTransform = Instantiate(weaponPartSO.prefab);
        AttachedWeaponPart attachedWeaponPart = attachedWeaponPartDic[weaponPartSO.partType];
        attachedWeaponPart.spawnedTransform = spawnedPartTransform;

        Transform attachPointTransform = attachedWeaponPart.partTypeAttachPoint.attachPointTransform;
        spawnedPartTransform.parent = attachPointTransform;
        spawnedPartTransform.localEulerAngles = Vector3.zero;
        spawnedPartTransform.localPosition = weaponPartSO.spawnOffset;
        spawnedPartTransform.localScale = new Vector3(1, 1, 1);

        attachedWeaponPart.weaponPartSO = weaponPartSO;
        attachedWeaponPartDic[weaponPartSO.partType] = attachedWeaponPart;

        if (weaponPartSO.partType == WeaponPartSO.PartType.Barrel)
        {
            BarrelWeaponPartSO barrelWeaponPartSO = (BarrelWeaponPartSO)weaponPartSO;

            AttachedWeaponPart barrelPartTypeAttachedWeaponPart = attachedWeaponPartDic[WeaponPartSO.PartType.Barrel];
            AttachedWeaponPart muzzlePartTypeAttachedWeaponPart = attachedWeaponPartDic[WeaponPartSO.PartType.Muzzle];

            muzzlePartTypeAttachedWeaponPart.partTypeAttachPoint.attachPointTransform.position =
                barrelPartTypeAttachedWeaponPart.partTypeAttachPoint.attachPointTransform.position +
                barrelPartTypeAttachedWeaponPart.partTypeAttachPoint.attachPointTransform.forward * barrelWeaponPartSO.muzzleOffset;
        }

        if (weaponPartSO.partType == WeaponPartSO.PartType.Scope)
        {
            attachedScope = spawnedPartTransform.GetComponent<Scope>();
        }

        CalculateWeaponStats();
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

    public WeaponBodySO GetWeaponBodySO() => weaponBody.GetWeaponBodySO();
    public GameObject GetWeaponBodyBase() => weaponBody.GetBaseBody();

    public void OnPause() { audioSource.Pause(); }
    public void OnUnPause() { audioSource.UnPause(); }

    public string GetInteractionText(PlayerController player) => $"Pick up {gameObject.name}";

    public void CalculateWeaponStats()
    {
        weaponStats = new WeaponStatData(
            settings.baseDamage,
            settings.baseRateOfFire,
            settings.baseMagSize,
            settings.baseMaxAmmoReserve,
            1f,
            settings.penetrationFactor,
            settings.baseRange,
            1f,
            settings.baseAimInTime,
            settings.baseSprintToFireDelay,
            settings.baseReloadTime);

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
                //Debug.Log($"Attachment does not have Attachment script: {spawnedTranform.name}");
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