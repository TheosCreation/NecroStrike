using UnityEngine;

[CreateAssetMenu()]
public class WeaponSettings : ScriptableObject
{
    [Header("Attacking")]
    [Tooltip("Minimum force on Z axis")]
    public float rateOfFire = 700f;
    public float sprintToFireDelay = 0.2f;
    public float fireToSprintDelay = 0.15f;
    public WeaponClass weaponClass = WeaponClass.Rifle;
    public BulletTrail bulletTrailPrefab;
    public MuzzleFlash[] muzzleFlashPrefabs;
    public Casing casingPrefab;
    public Firemode firemode = Firemode.Auto;
    public LayerMask hitLayers;

    public float screenShakeDuration = 0.1f;
    public float screenShakeAmount = 1.0f;

    [Header("Penetration")]
    public float penetrationFactor = 0.5f;
    public int allowedHitCount = 3;

    [Header("Burst Firing")]
    public float burstCooldown = 1f;

    [Header("Recoil")]
    public float recoilResetTimeSeconds = 1f;
    public RecoilPattern recoilPattern;

    [Header("Ammo and Damage")]
    public int magSize = 30;
    public int maxAmmoReserve = 260;
    public float baseDamage = 50.0f;
    public float damageFallOffDistance = 100.0f;
    public float headShotMultiplier = 1.5f;

    [Header("Equip")]
    public float equipTime = 0.3f;

    [Header("Inspect")]
    public float inspectTime = 2.2f;

    [Header("Hipfire")]
    public float spreadAmount = 0.05f;
    public float spreadIncreasePerShot = 0.01f;
    public float maxSpread = 0.2f;
    public float spreadRecoverRate = 1.5f;

    [Header("Aiming")]
    public float aimInTime = 0.15f;
    public float aimingZoomLevel = 1.2f;
    public float cameraZOffset = 0.05f;
    public float aimingMoveReduction = 0.2f;
    public float aimingRecoilReduction = 0.5f;

    [Header("Reloading")]
    public float reloadTime = 0.5f;

    [Header("Bolt Action")]
    public float boltDelay = 0.2f;
    public float boltActionLength = 0.5f;

    [Header("Audio")]
    public AudioClip[] firingSounds;
    public AudioClip reloadSound;
    public AudioClip aimInSound;
    public AudioClip pickUpClip;
    public AudioClip boltAction; //optional
}