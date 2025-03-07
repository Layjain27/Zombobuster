using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WeaponSystem : MonoBehaviour
{
    [System.Serializable]
    public class Weapon
    {
        public string name;
        public Animator weaponAnimator;
        public RectTransform weaponUI;
        public AudioClip shootSound;
        public AudioClip reloadSound;
        public int maxAmmo;
        public float fireRate;
        public float reloadTime;

        public bool enablePropulsion = false; // Custom per weapon
        public float basePropelForce = 10f;
        public float maxPropelMultiplier = 2f;
    }

    public Weapon[] weapons;
    private int currentWeaponIndex = 0;
    private Weapon currentWeapon;

    private int currentAmmo;
    private bool isReloading = false;

    public AudioSource audioSource;
    public RectTransform weaponCanvas;
    public float bobbingSpeed = 1.5f;
    public float bobbingAmount = 5f;

    public float groundCheckDistance = 1.5f;
    public float bounceDuration = 0.3f;
    public CharacterController characterController;

    private float verticalVelocity = 0f;
    private float gravity = -9.81f;
    private bool isBouncing = false;
    private float bounceTimer = 0f;

    private Vector3 defaultCanvasPosition;
    private float bobbingTime = 0;

    private void Start()
    {
        SwitchWeapon(0);
        defaultCanvasPosition = weaponCanvas.anchoredPosition;
        characterController.stepOffset = 0.3f; // Default step offset for stairs
    }

    private void Update()
    {
        HandleWeaponBobbing();
        ApplyGravity();

        if (Input.GetMouseButtonDown(0) && currentAmmo > 0 && !isReloading)
        {
            StartCoroutine(FireWeapon());
        }

        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < currentWeapon.maxAmmo && !isReloading)
        {
            StartCoroutine(ReloadWeapon());
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchWeapon(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchWeapon(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchWeapon(2);
    }

    private IEnumerator FireWeapon()
    {
        if (currentAmmo <= 0) yield break;

        currentAmmo--;

        currentWeapon.weaponAnimator.SetBool("isShooting", true);
        audioSource.PlayOneShot(currentWeapon.shootSound);

        ShootRaycast(); // Check for enemy hit

        if (currentWeapon.enablePropulsion)
        {
            ApplyRecoilBoost();
        }

        yield return new WaitForSeconds(currentWeapon.fireRate);

        currentWeapon.weaponAnimator.SetBool("isShooting", false);
    }

    private void ShootRaycast()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 100f))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                GroundedEnemy enemy1 = hit.collider.GetComponent<GroundedEnemy>();
                PropellingEnemyAI enemy2 = hit.collider.GetComponent<PropellingEnemyAI>();
                if (enemy2 != null)
                {
                    enemy2.TakeDamage();
                }

                if (enemy1 != null)
                {
                    enemy1.TakeDamage();
                }
            }
        }
    }

    private void ApplyRecoilBoost()
    {
        if (!characterController) return;

        Vector3 cameraDirection = Camera.main.transform.forward;
        float heightFromGround = GetHeightFromGround();

        if (Vector3.Dot(cameraDirection, Vector3.down) > 0.5f) // Only when aiming down
        {
            float forceMultiplier = Mathf.Lerp(currentWeapon.maxPropelMultiplier, 1f, heightFromGround / groundCheckDistance);
            verticalVelocity = currentWeapon.basePropelForce * forceMultiplier;
        }
    }

    private float GetHeightFromGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance))
        {
            return hit.distance;
        }
        return groundCheckDistance;
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = 0f; // Fully disable CharacterController's default bouncing
            characterController.stepOffset = 0.3f; // Allow climbing stairs

            if (isBouncing)
            {
                isBouncing = false;
                verticalVelocity = currentWeapon.basePropelForce * 0.5f; // Apply a small bounce
                bounceTimer = bounceDuration;
            }
        }
        else
        {
            characterController.stepOffset = 0f; // Disable stepOffset when airborne
        }

        if (isBouncing)
        {
            bounceTimer -= Time.deltaTime;
            if (bounceTimer <= 0)
            {
                isBouncing = false; // Stop bouncing after duration
            }
        }

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(new Vector3(0, verticalVelocity * Time.deltaTime, 0));
    }

    private IEnumerator ReloadWeapon()
    {
        isReloading = true;
        currentWeapon.weaponAnimator.SetBool("isShooting", false);
        audioSource.PlayOneShot(currentWeapon.reloadSound);

        Vector3 startPos = currentWeapon.weaponUI.anchoredPosition;
        Vector3 endPos = startPos + new Vector3(0, -200, 0);

        float elapsedTime = 0;
        while (elapsedTime < currentWeapon.reloadTime / 2)
        {
            currentWeapon.weaponUI.anchoredPosition = Vector3.Lerp(startPos, endPos, elapsedTime / (currentWeapon.reloadTime / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(currentWeapon.reloadTime / 2);

        elapsedTime = 0;
        while (elapsedTime < currentWeapon.reloadTime / 2)
        {
            currentWeapon.weaponUI.anchoredPosition = Vector3.Lerp(endPos, startPos, elapsedTime / (currentWeapon.reloadTime / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentAmmo = currentWeapon.maxAmmo;
        isReloading = false;
    }

    private void HandleWeaponBobbing()
    {
        bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

        if (isMoving)
        {
            bobbingTime += Time.deltaTime * bobbingSpeed;

            float xBobbing = (Mathf.PerlinNoise(bobbingTime, 0) - 0.5f) * bobbingAmount;
            float yBobbing = (Mathf.PerlinNoise(0, bobbingTime) - 0.5f) * bobbingAmount;

            weaponCanvas.anchoredPosition = defaultCanvasPosition + new Vector3(xBobbing, yBobbing, 0);
        }
        else
        {
            weaponCanvas.anchoredPosition = defaultCanvasPosition;
        }
    }

    private void SwitchWeapon(int index)
    {
        if (index >= weapons.Length) return;

        currentWeaponIndex = index;
        currentWeapon = weapons[currentWeaponIndex];
        currentAmmo = currentWeapon.maxAmmo;
    }
}
