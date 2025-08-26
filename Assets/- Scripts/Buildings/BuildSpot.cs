using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerBuildPoint : MonoBehaviour
{
    [Header("Tower Settings")]
    public GameObject towerPrefab;
    public float buildTime = 5f;
    public int requiredHellstone = 10;

    private bool playerInRange = false;
    private bool isBuilt = false;
    private float holdTimer = 0f;

    private PlayerInventory playerInventory;
    private TextMeshProUGUI buildText;
    private Canvas floatingCanvas;

    void Start()
    {
        // --- Floating Canvas ---
        GameObject canvasGO = new GameObject("FloatingBuildCanvas");
        floatingCanvas = canvasGO.AddComponent<Canvas>();
        floatingCanvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Position canvas above build point
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = new Vector3(0, 2f, 0);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * 0.02f;

        // --- Text ---
        GameObject textGO = new GameObject("BuildText");
        textGO.transform.SetParent(canvasGO.transform, false);
        buildText = textGO.AddComponent<TextMeshProUGUI>();

        // Styling
        buildText.fontSize = 120;
        buildText.alignment = TextAlignmentOptions.Center;
        buildText.color = Color.black;
        buildText.fontStyle = FontStyles.Bold;
        buildText.outlineWidth = 0.3f;
        buildText.outlineColor = Color.white;
        buildText.enableWordWrapping = false;

        RectTransform rt = buildText.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, 200);
        rt.localPosition = Vector3.zero;

        buildText.enabled = false;
    }

    void Update()
    {
        // Make UI face camera
        if (floatingCanvas != null && Camera.main != null)
        {
            floatingCanvas.transform.rotation = Quaternion.LookRotation(
                floatingCanvas.transform.position - Camera.main.transform.position
            );
        }

        if (!playerInRange || isBuilt) return;

        if (Input.GetKey(KeyCode.E))
        {
            holdTimer += Time.deltaTime;
            buildText.text = $"Building... {holdTimer:F1}/{buildTime}";
            buildText.enabled = true;

            if (holdTimer >= buildTime)
            {
                TryBuildTower();
                holdTimer = 0f;
            }
        }
        else
        {
            // Reset UI when player stops holding
            holdTimer = 0f;
            buildText.text = "Hold E to build";
            buildText.enabled = true;
        }
    }

    private void TryBuildTower()
    {
        if (playerInventory == null) return;

        if (playerInventory.HasHellstone(requiredHellstone))
        {
            playerInventory.SpendHellstone(requiredHellstone);

            float prefabHeight = GetPrefabHeight(towerPrefab);
            Vector3 spawnPos = transform.position + Vector3.up * (prefabHeight / 2f);
            Instantiate(towerPrefab, spawnPos, Quaternion.identity);

            buildText.text = "<color=green>Tower Built!</color>";
            isBuilt = true; // prevent further builds
        }
        else
        {
            buildText.text = "<color=red>Not enough Hellstone!</color>";
        }
    }

    private float GetPrefabHeight(GameObject prefab)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 0f;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        return bounds.size.y;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
            if (!isBuilt)
            {
                buildText.text = "Hold E to build";
                buildText.enabled = true;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            buildText.enabled = false;
            holdTimer = 0f;
        }
    }
}
