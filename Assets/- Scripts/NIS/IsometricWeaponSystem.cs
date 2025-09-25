using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class IsometricWeaponSystem : MonoBehaviour
{
    [Header("References")]
    public UpgradeManager upgradeManager;
    public Transform meleeShootOrigin;
    public Transform pistolShootOrigin;
    public Transform shotgunShootOrigin;
    public Transform rifleShootOrigin;
    public Camera mainCamera;
    public LayerMask hitLayers;

    [Header("Recoil")]
    public Vector3 recoilKick = new Vector3(0, 0.05f, -0.1f);
    public float recoilResetSpeed = 5f;

    [Header("Bullet Trail")]
    public GameObject bulletTrailPrefab;
    public float trailSpeed = 50f;
    public float trailLifetime = 0.5f;

    [Header("UI")]
    public TMPro.TextMeshProUGUI ammoUIText;

    private WeaponStatsSO activeWeapon;
    private Transform currentShootOrigin;
    private int currentAmmo;
    private bool isShooting = false;
    private bool isReloading = false;
    private float nextFireTime;
    private PlayerControls inputActions;
    private GameObject currentWeaponModel;

    public WeaponStatsSO ActiveWeapon => activeWeapon;
    public int CurrentAmmo => currentAmmo;

    private void Awake()
    {
        inputActions = new PlayerControls();

        inputActions.Player.Shoot.performed += ctx => StartShooting();
        inputActions.Player.Shoot.canceled += ctx => StopShooting();
        inputActions.Player.Reload.performed += ctx => StartCoroutine(Reload());

        // Example: switch weapons
        inputActions.Player.Weapon1.performed += ctx => SwitchWeapon(upgradeManager.meleeStats);
        inputActions.Player.Weapon2.performed += ctx => SwitchWeapon(upgradeManager.pistolStats);
        inputActions.Player.Weapon3.performed += ctx => SwitchWeapon(upgradeManager.shotgunStats);
        inputActions.Player.Weapon4.performed += ctx => SwitchWeapon(upgradeManager.rifleStats);
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Start()
    {
        // Default weapon
        SwitchWeapon(upgradeManager.pistolStats);

        // Subscribe to upgrades
        UpgradeManager.OnWeaponUpgraded += OnWeaponUpgraded;
    }

    private void OnDestroy()
    {
        UpgradeManager.OnWeaponUpgraded -= OnWeaponUpgraded;
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

    private void AimAtCursor()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, hitLayers))
        {
            Vector3 dir = hit.point - transform.position;
            dir.y = 0;
            transform.forward = dir.normalized;
        }
    }

    private void FireWeapon()
    {
        if (activeWeapon == null)
        {
            Debug.LogWarning("No active weapon assigned!");
            return;
        }

        if (activeWeapon != upgradeManager.meleeStats && currentAmmo <= 0)
        {
            Debug.Log("Out of ammo!");
            return;
        }

        nextFireTime = Time.time + (1f / activeWeapon.fireRate);

        if (activeWeapon == upgradeManager.meleeStats)
            MeleeAttack();
        else if (activeWeapon == upgradeManager.pistolStats || activeWeapon == upgradeManager.rifleStats)
            RaycastShoot();
        else if (activeWeapon == upgradeManager.shotgunStats)
            ShotgunShoot();

        ApplyRecoil();

        if (activeWeapon != upgradeManager.meleeStats)
            currentAmmo--;
    }

    private void MeleeAttack()
    {
        Collider[] hits = Physics.OverlapSphere(currentShootOrigin.position, activeWeapon.meleeRange, hitLayers);
        foreach (var hit in hits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(activeWeapon.currentDamage);
        }
    }

    private void RaycastShoot()
    {
        Ray ray = new Ray(currentShootOrigin.position, transform.forward);
        if (Physics.SphereCast(ray, activeWeapon.sphereRadius, out RaycastHit hit, activeWeapon.range, hitLayers))
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(activeWeapon.currentDamage);

            CreateBulletTrail(currentShootOrigin.position, hit.point);
        }
        else
        {
            CreateBulletTrail(currentShootOrigin.position, currentShootOrigin.position + transform.forward * activeWeapon.range);
        }
    }

    private void ShotgunShoot()
    {
        for (int i = 0; i < activeWeapon.shotgunPellets; i++)
        {
            Vector3 spreadDir = transform.forward + new Vector3(Random.Range(-activeWeapon.spread, activeWeapon.spread), 0, Random.Range(-activeWeapon.spread, activeWeapon.spread));
            Ray ray = new Ray(currentShootOrigin.position, spreadDir.normalized);
            if (Physics.SphereCast(ray, activeWeapon.sphereRadius, out RaycastHit hit, activeWeapon.range, hitLayers))
            {
                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(activeWeapon.currentDamage);

                CreateBulletTrail(currentShootOrigin.position, hit.point);
            }
            else
            {
                CreateBulletTrail(currentShootOrigin.position, currentShootOrigin.position + spreadDir.normalized * activeWeapon.range);
            }
        }
    }

    private void ApplyRecoil()
    {
        if (currentWeaponModel)
            currentWeaponModel.transform.localPosition += recoilKick;
    }

    private void StartShooting() => isShooting = true;
    private void StopShooting() => isShooting = false;

    private IEnumerator Reload()
    {
        if (isReloading || activeWeapon == upgradeManager.meleeStats || currentAmmo >= activeWeapon.MaxAmmo)
            yield break;

        isReloading = true;

        yield return new WaitForSeconds(activeWeapon.currentReloadTime);

        currentAmmo = activeWeapon.MaxAmmo;
        isReloading = false;
    }

    private void SwitchWeapon(WeaponStatsSO weapon)
    {
        activeWeapon = weapon;

        if (activeWeapon == upgradeManager.meleeStats) currentShootOrigin = meleeShootOrigin;
        else if (activeWeapon == upgradeManager.pistolStats) currentShootOrigin = pistolShootOrigin;
        else if (activeWeapon == upgradeManager.shotgunStats) currentShootOrigin = shotgunShootOrigin;
        else if (activeWeapon == upgradeManager.rifleStats) currentShootOrigin = rifleShootOrigin;

        currentAmmo = activeWeapon.MaxAmmo;
    }

    private void OnWeaponUpgraded(int tier)
    {
        // Reset ammo for currently active weapon
        currentAmmo = activeWeapon.MaxAmmo;
        Debug.Log($"Weapon upgraded to Tier {tier}. Ammo reset to {currentAmmo}");
    }

    private void UpdateAmmoUI()
    {
        if (!ammoUIText) return;
        ammoUIText.text = activeWeapon == upgradeManager.meleeStats ? "MELEE" : $"{currentAmmo} / {activeWeapon.MaxAmmo}";
    }

    private void CreateBulletTrail(Vector3 start, Vector3 end)
    {
        if (!bulletTrailPrefab) return;
        GameObject trail = Instantiate(bulletTrailPrefab, start, Quaternion.identity);
        StartCoroutine(MoveTrail(trail, start, end));
    }

    private IEnumerator MoveTrail(GameObject trail, Vector3 start, Vector3 end)
    {
        float elapsed = 0f;
        float distance = Vector3.Distance(start, end);
        float duration = distance / trailSpeed;

        while (elapsed < duration)
        {
            trail.transform.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(trail, trailLifetime);
    }
}
