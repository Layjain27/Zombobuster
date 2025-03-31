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

    [Header("Weapon Stats")]
    public WeaponStats meleeStats;
    public WeaponStats pistolStats;
    public WeaponStats shotgunStats;
    public WeaponStats rifleStats;


    [Header("Recoil Settings")]
    public Vector3 recoilKick = new Vector3(0, 0.05f, -0.1f);
    public float recoilResetSpeed = 5f;

    [Header("Bullet Trail Settings")]
    public GameObject bulletTrailPrefab; // Assign a trail prefab in Inspector
    public float trailSpeed = 50f;
    public float trailLifetime = 0.5f;

    private WeaponType currentWeaponType;
    private WeaponStats activeWeapon;
    private Transform currentShootOrigin;

    private int pistolAmmo;
    private int shotgunAmmo;
    private int rifleAmmo;

    private float nextFireTime;
    private bool isReloading = false;
    private PlayerControls inputActions;
    private bool isShooting = false;
    private GameObject currentWeaponModel;

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
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Start()
    {
        pistolAmmo = pistolStats.maxAmmo;
        shotgunAmmo = shotgunStats.maxAmmo;
        rifleAmmo = rifleStats.maxAmmo;

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
            Vector3 targetPoint = hit.point;
            Vector3 direction = (targetPoint - transform.position).normalized;
            direction.y = 0;
            transform.forward = direction;
        }
    }

    void FireWeapon()
    {
        nextFireTime = Time.time + (1f / activeWeapon.fireRate);

        if (currentWeaponType != WeaponType.Melee && GetCurrentAmmo() <= 0)
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
            DecreaseAmmo();
    }

    void MeleeAttack()
    {
        Collider[] hits = Physics.OverlapSphere(currentShootOrigin.position, activeWeapon.meleeRange, hitLayers);
        foreach (var hit in hits)
        {
            Debug.Log($"Melee hit: {hit.gameObject.name}");
            //HandleEnemyHit(hit.collider);
        }
    }

    void RaycastShoot()
    {
        Ray ray = new Ray(currentShootOrigin.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, activeWeapon.range, hitLayers))
        {
            Debug.Log($"Hit: {hit.collider.name}");
            HandleEnemyHit(hit.collider);
            CreateBulletTrail(currentShootOrigin.position, hit.point);
        }
        else
        {
            // If no hit, make the trail go the full range
            CreateBulletTrail(currentShootOrigin.position, currentShootOrigin.position + transform.forward * activeWeapon.range);
        }
    }


    void ShotgunShoot()
    {
        for (int i = 0; i < activeWeapon.shotgunPellets; i++)
        {
            Vector3 spread = transform.forward +
                             new Vector3(Random.Range(-activeWeapon.spread, activeWeapon.spread),
                                         0,
                                         Random.Range(-activeWeapon.spread, activeWeapon.spread));

            Ray ray = new Ray(currentShootOrigin.position, spread.normalized);
            if (Physics.Raycast(ray, out RaycastHit hit, activeWeapon.range, hitLayers))
            {
                Debug.Log($"Shotgun hit: {hit.collider.name}");
                HandleEnemyHit(hit.collider);
                CreateBulletTrail(currentShootOrigin.position, hit.point);
            }
            else
            {
                CreateBulletTrail(currentShootOrigin.position, currentShootOrigin.position + spread.normalized * activeWeapon.range);
            }
        }
    }


    void HandleEnemyHit(Collider collider)
    {
        // Check GroundedEnemy first
        if (collider.TryGetComponent<GroundedEnemy>(out GroundedEnemy groundedEnemy))
        {
            groundedEnemy.TakeDamage();
            return;
        }


    }

    IEnumerator Reload()
    {
        if (isReloading || currentWeaponType == WeaponType.Melee) yield break;
        if (GetCurrentAmmo() == GetMaxAmmo()) yield break;

        isReloading = true;
        PlaySound(activeWeapon.reloadSound);
        yield return new WaitForSeconds(activeWeapon.reloadTime);

        SetAmmo(GetMaxAmmo());
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
        if (currentWeaponType == WeaponType.Melee)
            ammoUIText.text = "MELEE";
        else
            ammoUIText.text = $"{GetCurrentAmmo()} / {GetMaxAmmo()}";
    }

    int GetCurrentAmmo()
    {
        return currentWeaponType switch
        {
            WeaponType.Pistol => pistolAmmo,
            WeaponType.Shotgun => shotgunAmmo,
            WeaponType.Rifle => rifleAmmo,
            _ => 0
        };
    }

    int GetMaxAmmo()
    {
        return currentWeaponType switch
        {
            WeaponType.Pistol => pistolStats.maxAmmo,
            WeaponType.Shotgun => shotgunStats.maxAmmo,
            WeaponType.Rifle => rifleStats.maxAmmo,
            _ => 0
        };
    }

    void DecreaseAmmo()
    {
        if (currentWeaponType == WeaponType.Pistol) pistolAmmo--;
        else if (currentWeaponType == WeaponType.Shotgun) shotgunAmmo--;
        else if (currentWeaponType == WeaponType.Rifle) rifleAmmo--;
    }

    void SetAmmo(int amount)
    {
        if (currentWeaponType == WeaponType.Pistol) pistolAmmo = amount;
        else if (currentWeaponType == WeaponType.Shotgun) shotgunAmmo = amount;
        else if (currentWeaponType == WeaponType.Rifle) rifleAmmo = amount;
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


}

[System.Serializable]
public class WeaponStats
{
    public float fireRate = 1f;
    public int maxAmmo = 10;
    public float reloadTime = 2f;
    public float range = 50f;
    public float meleeRange = 2f;
    public float spread = 0.1f;
    public int shotgunPellets = 6;

    public AudioClip shootSound;
    public AudioClip reloadSound;
}

public enum WeaponType
{
    Melee,
    Pistol,
    Shotgun,
    Rifle
}