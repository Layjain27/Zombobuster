using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HellstoneMine : MonoBehaviour
{
    public int maxHellstone = 15;
    public float cooldownTime = 180f; // 3 minutes
    public float mineInterval = 1.2f; // time between stones while holding E

    private int currentHellstone;
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;
    private float mineTimer = 0f;

    private bool playerInRange = false;
    private PlayerInventory playerInventory;

    private TextMeshProUGUI mineText;
    private Canvas floatingCanvas;

    void Start()
    {
        currentHellstone = maxHellstone;

        // Create floating world-space canvas
        GameObject canvasGO = new GameObject("FloatingMineCanvas");
        floatingCanvas = canvasGO.AddComponent<Canvas>();
        floatingCanvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Attach to mine
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = new Vector3(0, 2f, 0);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * 0.01f;

        // Create text
        GameObject textGO = new GameObject("MineText");
        textGO.transform.SetParent(canvasGO.transform, false);
        mineText = textGO.AddComponent<TextMeshProUGUI>();

        // Style
        mineText.fontSize = 50;
        mineText.alignment = TextAlignmentOptions.Center;
        mineText.color = Color.black;
        mineText.fontStyle = FontStyles.Bold;
        mineText.outlineWidth = 0.3f;
        mineText.outlineColor = Color.white;

        RectTransform rt = mineText.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, 200);
        rt.localPosition = Vector3.zero;

        mineText.enabled = false;
    }

    void Update()
    {
        // --- Billboard: Always face camera ---
        if (floatingCanvas != null && Camera.main != null)
        {
            floatingCanvas.transform.rotation = Quaternion.LookRotation(
                floatingCanvas.transform.position - Camera.main.transform.position
            );
        }

        // Cooldown handling
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                currentHellstone = maxHellstone;
            }

            if (playerInRange)
            {
                mineText.text = $"Cooldown: {Mathf.CeilToInt(cooldownTimer)}s";
                mineText.enabled = true;
            }
            return;
        }

        if (playerInRange)
        {
            if (Input.GetKey(KeyCode.E))
            {
                mineTimer += Time.deltaTime;
                if (mineTimer >= mineInterval && currentHellstone > 0)
                {
                    playerInventory.AddHellstone(1);
                    currentHellstone--;
                    mineTimer = 0f;
                }

                mineText.text = $"Mining... {currentHellstone}/{maxHellstone}";
            }
            else
            {
                mineText.text = $"Hold E to collect Hellstone ({currentHellstone}/{maxHellstone})";
            }

            mineText.enabled = true;

            // Start cooldown if mine is empty
            if (currentHellstone <= 0)
            {
                isOnCooldown = true;
                cooldownTimer = cooldownTime;
                mineText.text = "Mine depleted!";
            }
        }
        else
        {
            mineText.enabled = false;
            mineTimer = 0f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
            mineText.text = $"Hold E to collect Hellstone ({currentHellstone}/{maxHellstone})";
            mineText.enabled = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            mineText.enabled = false;
        }
    }
}
