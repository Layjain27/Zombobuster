using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class IsometricWeaponSystem : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public LayerMask hitLayers;
    public AudioSource audioSource;
    public TextMeshProUGUI ammoUIText;
    public PlayerInventory inventory; // For upgrades

    [Header("Weapon Models")]
    public GameObject meleeModel;
    public GameObject pistolModel;
    public GameObject shotgunModel;
    public GameObject rifleModel;

    [Header("Shoot Origins")]
    public Transform meleeShootOrigin;
    public Transform pistolShootOrigin;
    public Transform shotgunShootOrigin;
    public Transform rifleShootOrigin;

    [Header("Weapon Stats (SO)")]
    public WeaponStatsSO meleeStats;
    public WeaponStatsSO pistolStats;
    public WeaponStatsSO shotgunStats;
    public WeaponStatsSO rifleStats;

    [Header("Upgrade Settings")]
    public UpgradeTierSO[] upgradeTiers;
    public int currentTier = 0;
    private const int maxTier = 3;

    [Header("Upgrade Costs")]
    public int[] soulsCost = { 50, 100, 200 };
    public int[] hellstoneCost = { 5, 10, 20 };
    public int[] divineDewCost = { 0, 0, 1 };

    [Header("Audio")]
    public AudioClip weaponSwitchSound;
    public AudioSource reloadSounds;

    [Header("Recoil Settings")]
    public Vector3 recoilKick = new Vector3(0, 0.05f, -0.1f);
    public float recoilResetSpeed = 5f;

    [Header("Bullet Trail Settings")]
    public GameObject bulletTrailPrefab;
    public float trailSpeed = 50f;
    public float trailLifetime = 0.5f;

    private WeaponType currentWeaponType;
    private WeaponStatsSO activeWeapon;
    private Transform currentShootOrigin;

    private float nextFireTime;
    private bool isReloading = false;
    private PlayerControls inputActions;
    private bool isShooting = false;
    private GameObject currentWeaponModel;

    [HideInInspector] public int currentAmmo;
    [HideInInspector] public WeaponStatsSO ActiveWeapon => activeWeapon;

    private void Awake()
    {
        inputActions = new PlayerControls();

        inputActions.Player.Shoot.performed += ctx => StartShooting();
        inputActions.Player.Shoot.canceled += ctx => StopShooting();
        inputActions.Player.Reload.performed += ctx => StartCoroutine(Reload());

        inputActions.Player.Weapon1.performed += ctx => SwitchWeapon(WeaponType.Melee);
        inputActions.Player.Weapon2.performed += ctx => SwitchWeapon(WeaponType.Pistol);
        inputActions.Player.Weapon3.performed += ctx => SwitchWeapon(WeaponType.Shotgun);
        inputActions.Player.Weapon4.performed += ctx => SwitchWeapon(WeaponType.Rifle);
        //inputActions.Player.UpgradeWeapon.performed += ctx => TryUpgradeWeapon();   
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Start()
    {
        SwitchWeapon(WeaponType.Pistol);
    }

    private void Update()
    {
        AimAtCursor();

        if (isShooting && !isReloading && Time.time >= nextFireTime)
            FireWeapon();

        UpdateAmmoUI();

        if (currentWeaponModel)
            currentWeaponModel.transform.localPosition = Vector3.Lerp(currentWeaponModel.transform.localPosition, Vector3.zero, recoilResetSpeed * Time.deltaTime);
    }

    void AimAtCursor()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, hitLayers))
        {
            Vector3 direction = (hit.point - transform.position).normalized;
            direction.y = 0;
            transform.forward = direction;
        }
    }

    void FireWeapon()
    {
        nextFireTime = Time.time + (1f / activeWeapon.fireRate);

        if (currentWeaponType != WeaponType.Melee && currentAmmo <= 0)
        {
            Debug.Log("Out of ammo!");
            return;
        }

        if (currentWeaponType == WeaponType.Melee)
            MeleeAttack();
        else if (currentWeaponType == WeaponType.Pistol)
            RaycastShoot();
        else if (currentWeaponType == WeaponType.Shotgun)
            ShotgunShoot();
        else if (currentWeaponType == WeaponType.Rifle)
            RaycastShoot();

        ApplyRecoil();
        PlaySound(activeWeapon.shootSound);

        if (currentWeaponType != WeaponType.Melee)
            currentAmmo--;
    }

    void MeleeAttack()
    {
        Collider[] hits = Physics.OverlapSphere(currentShootOrigin.position, activeWeapon.meleeRange, hitLayers);
        foreach (var hit in hits)
            HandleEnemyHit(hit, transform.position);
    }

    void RaycastShoot()
    {
        Ray ray = new Ray(currentShootOrigin.position, transform.forward);
        if (Physics.SphereCast(ray, activeWeapon.sphereRadius, out RaycastHit hit, activeWeapon.range, hitLayers))
        {
            HandleEnemyHit(hit.collider, transform.position);
            CreateBulletTrail(currentShootOrigin.position, hit.point);
        }
        else
        {
            CreateBulletTrail(currentShootOrigin.position, currentShootOrigin.position + transform.forward * activeWeapon.range);
        }
    }

    void ShotgunShoot()
    {
        for (int i = 0; i < activeWeapon.shotgunPellets; i++)
        {
            Vector3 spread = transform.forward + new Vector3(Random.Range(-activeWeapon.spread, activeWeapon.spread), 0, Random.Range(-activeWeapon.spread, activeWeapon.spread));
            Ray ray = new Ray(currentShootOrigin.position, spread.normalized);
            if (Physics.SphereCast(ray, activeWeapon.sphereRadius, out RaycastHit hit, activeWeapon.range, hitLayers))
            {
                HandleEnemyHit(hit.collider, transform.position);
                CreateBulletTrail(currentShootOrigin.position, hit.point);
            }
            else
            {
                CreateBulletTrail(currentShootOrigin.position, currentShootOrigin.position + spread.normalized * activeWeapon.range);
            }
        }
    }

    void HandleEnemyHit(Collider collider, Vector3 attackerPosition)
    {
        IDamageable damageableTarget = collider.GetComponentInParent<IDamageable>();
        if (damageableTarget == null) return;

        TowerHealth friendlyTower = collider.GetComponentInParent<TowerHealth>();
        if (friendlyTower != null && friendlyTower.faction == Faction.Player)
            return;

        damageableTarget.TakeDamage(activeWeapon.currentDamage);
    }

    IEnumerator Reload()
    {
        if (isReloading || currentWeaponType == WeaponType.Melee || currentAmmo == activeWeapon.MaxAmmo) yield break;

        isReloading = true;

        if (activeWeapon.reloadSound)
        {
            reloadSounds.clip = activeWeapon.reloadSound;
            reloadSounds.Play();
        }

        yield return new WaitForSeconds(activeWeapon.currentReloadTime);

        currentAmmo = activeWeapon.MaxAmmo;
        isReloading = false;
    }

    void ApplyRecoil()
    {
        if (currentWeaponModel)
            currentWeaponModel.transform.localPosition += recoilKick;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip)
            audioSource.PlayOneShot(clip);
    }

    void StartShooting() => isShooting = true;
    void StopShooting() => isShooting = false;

    void SwitchWeapon(WeaponType newWeapon)
    {
        if (isReloading)
        {
            StopCoroutine(Reload());
            isReloading = false;
            reloadSounds.Stop();
        }

        PlaySound(weaponSwitchSound);

        currentWeaponType = newWeapon;
        activeWeapon = newWeapon switch
        {
            WeaponType.Melee => meleeStats,
            WeaponType.Pistol => pistolStats,
            WeaponType.Shotgun => shotgunStats,
            WeaponType.Rifle => rifleStats,
            _ => pistolStats
        };

        currentShootOrigin = newWeapon switch
        {
            WeaponType.Melee => meleeShootOrigin,
            WeaponType.Pistol => pistolShootOrigin,
            WeaponType.Shotgun => shotgunShootOrigin,
            WeaponType.Rifle => rifleShootOrigin,
            _ => pistolShootOrigin
        };

        ActivateWeaponModel(newWeapon);
        currentAmmo = activeWeapon.MaxAmmo; // Reset ammo on switch
    }

    void ActivateWeaponModel(WeaponType type)
    {
        meleeModel.SetActive(type == WeaponType.Melee);
        pistolModel.SetActive(type == WeaponType.Pistol);
        shotgunModel.SetActive(type == WeaponType.Shotgun);
        rifleModel.SetActive(type == WeaponType.Rifle);

        currentWeaponModel = type switch
        {
            WeaponType.Melee => meleeModel,
            WeaponType.Pistol => pistolModel,
            WeaponType.Shotgun => shotgunModel,
            WeaponType.Rifle => rifleModel,
            _ => pistolModel
        };
    }

    void UpdateAmmoUI()
    {
        ammoUIText.text = currentWeaponType == WeaponType.Melee ? "MELEE" : $"{currentAmmo} / {activeWeapon.MaxAmmo}";
    }

    private void OnDrawGizmosSelected()
    {
        if (meleeShootOrigin && currentWeaponType == WeaponType.Melee)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(meleeShootOrigin.position, activeWeapon.meleeRange);
        }
    }

    void CreateBulletTrail(Vector3 start, Vector3 end)
    {
        if (!bulletTrailPrefab) return;

        GameObject trail = Instantiate(bulletTrailPrefab, start, Quaternion.identity);
        StartCoroutine(MoveTrail(trail, start, end));
    }

    IEnumerator MoveTrail(GameObject trail, Vector3 start, Vector3 end)
    {
        float elapsedTime = 0f;
        float distance = Vector3.Distance(start, end);
        float duration = distance / trailSpeed;

        while (elapsedTime < duration)
        {
            trail.transform.position = Vector3.Lerp(start, end, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(trail, trailLifetime);
    }

    // --- Weapon Upgrade System ---
    void TryUpgradeWeapon()
    {
        if (currentTier >= maxTier)
        {
            Debug.Log("Already max weapon tier!");
            return;
        }

        int nextTier = currentTier;

        // Check resources
        if (inventory.souls < soulsCost[nextTier] ||
            inventory.hellstone < hellstoneCost[nextTier] ||
            inventory.divineDew < divineDewCost[nextTier])
        {
            Debug.Log("Not enough resources to upgrade!");
            return;
        }

        // Spend resources
        inventory.souls -= soulsCost[nextTier];
        inventory.hellstone -= hellstoneCost[nextTier];
        inventory.divineDew -= divineDewCost[nextTier];

        currentTier++;
        ApplyUpgradeTier();

        Debug.Log("Weapons upgraded to Tier " + currentTier);
        currentAmmo = activeWeapon.MaxAmmo; // Update current ammo after upgrade
    }

    void ApplyUpgradeTier()
    {
        WeaponStatsSO[] allWeapons = { meleeStats, pistolStats, shotgunStats, rifleStats };

        float damagePercent = 0;
        float magPercent = 0;
        float reloadPercent = 0;

        for (int i = 0; i < currentTier; i++)
        {
            damagePercent += upgradeTiers[i].damagePercent;
            magPercent += upgradeTiers[i].magPercent;
            reloadPercent += upgradeTiers[i].reloadPercent;
        }

        foreach (var weapon in allWeapons)
        {
            weapon.ResetStats();
            weapon.ApplyUpgrade(damagePercent, magPercent, reloadPercent);
        }
    }

}
