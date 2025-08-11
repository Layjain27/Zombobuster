using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerBuildPoint : MonoBehaviour
{
    public GameObject towerPrefab;
    public float buildTime = 5f;
    public int requiredHellstone = 10;

    private bool playerInRange = false;
    private float holdTimer = 0f;
    private PlayerInventory playerInventory;

    private TextMeshProUGUI buildText;
    private Canvas floatingCanvas;

    void Start()
    {
        // Create floating world-space canvas
        GameObject canvasGO = new GameObject("FloatingBuildCanvas");
        floatingCanvas = canvasGO.AddComponent<Canvas>();
        floatingCanvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Position canvas above build point
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = new Vector3(0, 2f, 0);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * 0.01f; // small world scale

        // Create text
        GameObject textGO = new GameObject("BuildText");
        textGO.transform.SetParent(canvasGO.transform, false);
        buildText = textGO.AddComponent<TextMeshProUGUI>();

        // Styling
        buildText.fontSize = 150; // Big
        buildText.alignment = TextAlignmentOptions.Center;
        buildText.color = Color.black;
        buildText.fontStyle = FontStyles.Bold;
        buildText.enableWordWrapping = false;
        buildText.outlineWidth = 0.3f;
        buildText.outlineColor = Color.white;

        // Stretch text to fit canvas
        RectTransform rt = buildText.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, 200);
        rt.localPosition = Vector3.zero;

        buildText.enabled = false; // hidden until player enters
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

        if (playerInRange && Input.GetKey(KeyCode.E))
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
        else if (playerInRange)
        {
            buildText.text = "Hold E to build";
            buildText.enabled = true;
        }
        else
        {
            buildText.enabled = false;
            holdTimer = 0f;
        }
    }

    private void TryBuildTower()
    {
        if (playerInventory != null && playerInventory.hellstoneCount >= requiredHellstone)
        {
            playerInventory.hellstoneCount -= requiredHellstone;

            // Auto height
            float prefabHeight = GetPrefabHeight(towerPrefab);
            Vector3 spawnPos = transform.position + Vector3.up * (prefabHeight / 2f);
            Instantiate(towerPrefab, spawnPos, Quaternion.identity);

            buildText.text = "Tower Built!";
        }
        else
        {
            buildText.text = "<color=black>Not enough Hellstone!</color>";
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
            buildText.text = "Hold E to build";
            buildText.enabled = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            buildText.enabled = false;
        }
    }
}
