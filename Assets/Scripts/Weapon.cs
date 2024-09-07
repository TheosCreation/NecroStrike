using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Weapon : MonoBehaviour, IInteractable
{
    private Animator animator;
    private Rigidbody rb;
    private WeaponHolder holder;
    [SerializeField] private Collider collisionCollider;
    public float motionReduction = 0.5f;
    [SerializeField] private bool isHeld = false;
    [SerializeField] private bool isEquip = false;
    public bool isAiming = false;
    public bool isAttacking = false;
    public bool isReloading = false;
    public bool isInspecting = false;
    [Header("Attacking")]
    [SerializeField] private float attackDelay = 0.1f;
    float attackTimer = 0;

    [Header("Recoil")]
    public Vector2 recoil; 
    [SerializeField] private Vector2 horizontalVariation = new Vector2(-0.5f, 0.5f);
    [SerializeField] private float initialRecoilStep = 1f;
    [SerializeField] private float acceleration = 0.1f;
    [SerializeField] private float maxStep = 0.1f;
    private float recoilStep = 0f;


    [SerializeField] private float screenShakeDuration = 0.1f;
    [SerializeField] private float screenShakeAmount = 1.0f;


    [Header("Ammo and Damage")]
    [SerializeField] private int magSize = 30;
    [SerializeField] private int startingAmmoReserve = 1000;
    private int ammoLeft = 0;
    private int ammoReserve = 0;

    [Header("Reloading")]
    [SerializeField] private float reloadTime = 0.5f;
    private Timer reloadTimer;

    [Header("Audio")]
    [SerializeField] private AudioClip[] firingSounds;
    [SerializeField] private AudioClip reloadSound; 
    private AudioSource audioSource;

    [Header("IK hand positions")]
    public Transform IKRightHandPos;
    public Transform IKLeftHandPos;


    private void Awake()
    {
        animator = GetComponent<Animator>(); 
        rb = GetComponent<Rigidbody>(); 
        audioSource = GetComponent<AudioSource>();
        holder = transform.root.GetComponent<WeaponHolder>();
        reloadTimer = gameObject.AddComponent<Timer>();
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
        holder = null;
        transform.parent = null;
        isHeld = false;
        rb.isKinematic = false;
        collisionCollider.enabled = true;
        LayerController.Instance.SetGameObjectAndChildrenLayer(gameObject, LayerMask.NameToLayer("Default"));

        rb.AddForce(transform.forward * _force, ForceMode.Impulse);

        StopAiming();
        StopAttacking();
        Unequip();
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
        attackTimer -= Time.deltaTime;
        if (attackTimer < 0.0f && isAttacking && isEquip && !isReloading && !isInspecting)
        {
            if (ammoLeft > 0)
            {
                attackTimer = attackDelay;
                Attack();
            }
            else
            {
                Reload();
            }
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
        recoilStep = initialRecoilStep;
    }

    private void Attack()
    {
        TakeAmmoFromMag(1);

        float aimRatio = isAiming ? motionReduction : 1f;
        float hRecoil = Random.Range(horizontalVariation.x,
            horizontalVariation.y);
        recoil += new Vector2(hRecoil, recoilStep) * aimRatio;

        if(recoilStep < maxStep)
        {
            recoilStep += (acceleration * 0.01f);
        }

        holder.player.playerLook.TriggerScreenShake(screenShakeDuration, screenShakeAmount * motionReduction * 0.01f);

        PlayRandomFiringSound();
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

    private void PlayRandomFiringSound()
    {
        if(firingSounds.Length == 0) return;

        int randomIndex = Random.Range(0, firingSounds.Length);
        AudioClip randomClip = firingSounds[randomIndex];

        audioSource.PlayOneShot(randomClip);
    }

    private void PlayReloadSound()
    {
        audioSource.PlayOneShot(reloadSound);
    }

    public void StopAttacking()
    {
        isAttacking = false;
        recoilStep = 0f;
    }

    public void StartAiming()
    {
        if(isInspecting) return;

        isAiming = true;
    }

    public void StopAiming()
    { 
        isAiming = false; 
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
}
