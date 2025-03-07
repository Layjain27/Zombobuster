using UnityEngine;
using System.Collections;
using TMPro;

public class WeaponSystemNIS : MonoBehaviour
{
    [System.Serializable]
    public class Weapon
    {
        public string name;
        public Transform weaponModel;
        public Transform muzzleFlashPoint; 
        public AudioClip shootSound;
        public AudioClip reloadSound;
        public int maxAmmo;
        public float fireRate;
        public float reloadTime;
        public bool enablePropulsion = false;
        public float basePropelForce = 10f;
        public float maxPropelMultiplier = 2f;
        public float recoilAmount = 5f;
        public float recoilSpeed = 10f;
        public float reloadDropAmount = 0.3f;
    }

    public Weapon[] weapons;
    private int currentWeaponIndex = 0;
    private Weapon currentWeapon;

    private int currentAmmo;
    private bool isReloading = false;

    public AudioSource audioSource;
    public CharacterController characterController;

    public GameObject muzzleFlashPrefab;
    private GameObject muzzleFlashInstance;

    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI scoreText;

    private int score = 0;
    private float verticalVelocity = 0f;
    private float gravity = -9.81f;

    public GameObject gameOverCanvas; 

    private Quaternion originalRotation;
    private Vector3 defaultWeaponPosition;

    private PlayerControls inputActions;

    private void Awake()
    {
        inputActions = new PlayerControls();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Fire.performed += ctx => StartCoroutine(FireWeapon());
        inputActions.Player.Reload.performed += ctx => StartCoroutine(ReloadWeapon());
    }

    private void OnDisable()
    {
        inputActions.Player.Fire.performed -= ctx => StartCoroutine(FireWeapon());
        inputActions.Player.Reload.performed -= ctx => StartCoroutine(ReloadWeapon());
        inputActions.Disable();
    }

    private void Start()
    {
        SwitchWeapon(0);
        gameOverCanvas.SetActive(false); // Hide Game Over UI at start
        LockCursor(true);
    }

    private void Update()
    {
        ApplyGravity();
        ApplyRecoilRecovery();

        //if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchWeapon(0);
        //if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchWeapon(1);
        //if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchWeapon(2);

        UpdateAmmoUI();
    }

    private IEnumerator FireWeapon()
    {
        if (currentAmmo <= 0 || isReloading) yield break;

        currentAmmo--;
        currentWeapon.weaponModel.localRotation *= Quaternion.Euler(+currentWeapon.recoilAmount, 0, 0);
        audioSource.PlayOneShot(currentWeapon.shootSound);

        ShootRaycast();

        if (currentWeapon.enablePropulsion)
        {
            ApplyRecoilBoost();
        }

        if (muzzleFlashInstance)
        {
            muzzleFlashInstance.transform.position = currentWeapon.muzzleFlashPoint.position;
            muzzleFlashInstance.transform.rotation = currentWeapon.muzzleFlashPoint.rotation;
            muzzleFlashInstance.SetActive(true);
        }

        yield return new WaitForSeconds(currentWeapon.fireRate);

        if (muzzleFlashInstance)
        {
            muzzleFlashInstance.SetActive(false);
        }
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

                if (enemy1 != null)
                {
                    enemy1.TakeDamage();
                    score += 100;
                }
                if (enemy2 != null)
                {
                    enemy2.TakeDamage();
                    score += 200;
                }

                UpdateScoreUI();
                //CheckGameOver();
            }
        }
    }

    //private void CheckGameOver()
    //{
    //    if (score >= 1400)
    //    {
    //        GameOver();
    //    }
    //}

    private void GameOver()
    {
        gameOverCanvas.SetActive(true);
        LockCursor(false);
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void UpdateAmmoUI()
    {
        ammoText.text = currentAmmo.ToString();
        ammoText.color = (currentAmmo == 0) ? Color.red : Color.white;
    }

    private void UpdateScoreUI()
    {
        scoreText.text = "Score: " + score;
    }

    private void ApplyRecoilBoost()
    {
        if (!characterController) return;

        Vector3 cameraDirection = Camera.main.transform.forward;
        float heightFromGround = GetHeightFromGround();

        if (Vector3.Dot(cameraDirection, Vector3.down) > 0.5f)
        {
            float forceMultiplier = Mathf.Lerp(currentWeapon.maxPropelMultiplier, 1f, heightFromGround / 1.5f);
            verticalVelocity = currentWeapon.basePropelForce * forceMultiplier;
        }
    }

    private float GetHeightFromGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.5f))
        {
            return hit.distance;
        }
        return 1.5f;
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = 0f;
            characterController.stepOffset = 0.3f;
        }
        else
        {
            characterController.stepOffset = 0f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(new Vector3(0, verticalVelocity * Time.deltaTime, 0));
    }

    private IEnumerator ReloadWeapon()
    {
        if (currentAmmo == currentWeapon.maxAmmo || isReloading) yield break;

        isReloading = true;
        audioSource.PlayOneShot(currentWeapon.reloadSound);

        Vector3 startPosition = currentWeapon.weaponModel.localPosition;
        Vector3 endPosition = startPosition + Vector3.down * currentWeapon.reloadDropAmount;

        float elapsedTime = 0;
        while (elapsedTime < currentWeapon.reloadTime / 2)
        {
            currentWeapon.weaponModel.localPosition = Vector3.Lerp(startPosition, endPosition, elapsedTime / (currentWeapon.reloadTime / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(currentWeapon.reloadTime / 2);

        elapsedTime = 0;
        while (elapsedTime < currentWeapon.reloadTime / 2)
        {
            currentWeapon.weaponModel.localPosition = Vector3.Lerp(endPosition, startPosition, elapsedTime / (currentWeapon.reloadTime / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentAmmo = currentWeapon.maxAmmo;
        isReloading = false;
    }

    private void ApplyRecoilRecovery()
    {
        if (currentWeapon.weaponModel)
        {
            currentWeapon.weaponModel.localRotation = Quaternion.Lerp(currentWeapon.weaponModel.localRotation, originalRotation, Time.deltaTime * currentWeapon.recoilSpeed);
        }
    }

    private void SwitchWeapon(int index)
    {
        if (index >= weapons.Length) return;

        currentWeaponIndex = index;
        currentWeapon = weapons[currentWeaponIndex];
        currentAmmo = currentWeapon.maxAmmo;
        originalRotation = currentWeapon.weaponModel.localRotation;
        defaultWeaponPosition = currentWeapon.weaponModel.localPosition;

        if (muzzleFlashInstance)
        {
            Destroy(muzzleFlashInstance);
        }

        if (muzzleFlashPrefab && currentWeapon.muzzleFlashPoint)
        {
            muzzleFlashInstance = Instantiate(muzzleFlashPrefab, currentWeapon.muzzleFlashPoint.position, currentWeapon.muzzleFlashPoint.rotation);
            muzzleFlashInstance.transform.SetParent(currentWeapon.muzzleFlashPoint);
            muzzleFlashInstance.SetActive(false);
        }

        UpdateAmmoUI();
    }
}
