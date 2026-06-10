using System.Collections;
using UnityEngine;

public class GlobalMinimapUI : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Camera mapCamera;
    [SerializeField] private Transform player;
    [SerializeField] private Transform sceneBoundsRoot;
    [SerializeField] private int textureSize = 512;
    [SerializeField] private float scenePadding = 24f;
    [SerializeField] private float cameraExtraHeight = 80f;
    [SerializeField] private float mapWidth = 340f;
    [SerializeField] private float mapHeight = 230f;
    [SerializeField] private float expandedWidthRatio = 0.72f;
    [SerializeField] private float expandedHeightRatio = 0.72f;
    [SerializeField] private float sideQuestPanelHeight = 260f;
    [SerializeField] private Vector3 faeHomesMazeCenter = new Vector3(356.34f, 16.58f, 663.52f);
    [SerializeField] private Vector2 faeHomesMazeSize = new Vector2(150f, 150f);
    [SerializeField] private float faeHomesMazeForwardOffset = 18f;
    [SerializeField] private float faeHomesMazeHeight = 60f;
    [SerializeField] private Color playerColor = new Color(1f, 0.86f, 0.18f, 1f);
    private RenderTexture mapTexture;
    private Bounds sceneBounds;
    private Rect mapWorldRect;
    private bool hasSceneBounds;
    private bool mapExpanded;
    private bool showCompactMap = true;
    private Texture2D markerTexture;
    private GUIStyle titleStyle;
    private bool mapNeedsRender;

    private void Awake()
    {
        if (mapTexture == null)
        {
            mapTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32);
            mapTexture.name = "GlobalMinimapTexture";
            mapTexture.Create();
        }

        if (markerTexture == null)
        {
            markerTexture = CreateCircleTexture(48, playerColor);
        }

        if (mapCamera != null)
        {
            mapCamera.orthographic = true;
            mapCamera.clearFlags = CameraClearFlags.Skybox;
            mapCamera.targetTexture = mapTexture;
            mapCamera.depth = -100f;
            mapCamera.enabled = false;
            mapCamera.allowHDR = false;
            mapCamera.allowMSAA = false;
        }

        hasSceneBounds = false;
        mapWorldRect = default;
        mapNeedsRender = true;
        SetCameraActive(false);
        RefreshScene();
    }

    private void OnDestroy()
    {
        if (mapTexture != null)
        {
            mapTexture.Release();
            Destroy(mapTexture);
        }

        if (markerTexture != null)
        {
            Destroy(markerTexture);
        }
    }

    private void Update()
    {
        if (!ShouldShowMap())
        {
            mapExpanded = false;
            SetCameraActive(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            mapExpanded = !mapExpanded;
            mapNeedsRender = true;
        }

        if (!hasSceneBounds)
        {
            CalculateSceneBounds();
            ConfigureCamera();
        }

        UpdateMapRender();
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || !ShouldShowMap() || mapTexture == null || !hasSceneBounds)
        {
            return;
        }

        if (!mapExpanded && !showCompactMap)
        {
            return;
        }

        Rect rect = GetMapRect();
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.DrawTexture(new Rect(rect.x + 12f, rect.y + 42f, rect.width - 24f, rect.height - 54f), mapTexture, ScaleMode.StretchToFill, false);
        string title = mapExpanded ? "Map - Press M to close" : "Map - Press M";
        GUI.Label(new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, 30f), title, GameUiStyle.LabelStyle(ref titleStyle, 17, TextAnchor.MiddleLeft, FontStyle.Bold));
        DrawPlayerMarker(rect);
    }

    private void RefreshScene()
    {
        CalculateSceneBounds();
        ConfigureCamera();
        RenderMap();
        mapNeedsRender = false;
    }

    private void CalculateSceneBounds()
    {
        hasSceneBounds = false;
        sceneBounds = new Bounds(Vector3.zero, Vector3.zero);

        if (TryUseFaeHomesMazeBounds())
        {
            return;
        }

        if (sceneBoundsRoot != null)
        {
            Renderer[] renderers = sceneBoundsRoot.GetComponentsInChildren<Renderer>(true);
            Collider[] colliders = sceneBoundsRoot.GetComponentsInChildren<Collider>(true);
            bool foundBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!foundBounds)
                {
                    sceneBounds = renderer.bounds;
                    foundBounds = true;
                }
                else
                {
                    sceneBounds.Encapsulate(renderer.bounds);
                }
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                {
                    continue;
                }

                if (!foundBounds)
                {
                    sceneBounds = collider.bounds;
                    foundBounds = true;
                }
                else
                {
                    sceneBounds.Encapsulate(collider.bounds);
                }
            }

            if (foundBounds)
            {
                hasSceneBounds = true;
                return;
            }
        }

        if (!hasSceneBounds && player != null)
        {
            sceneBounds = new Bounds(player.position, new Vector3(120f, 40f, 120f));
            hasSceneBounds = true;
        }
    }

    private void ConfigureCamera()
    {
        if (mapCamera == null || !hasSceneBounds)
        {
            return;
        }

        Vector3 center = sceneBounds.center;
        float height = sceneBounds.max.y + cameraExtraHeight;
        float width = Mathf.Max(sceneBounds.size.x + scenePadding * 2f, 1f);
        float depth = Mathf.Max(sceneBounds.size.z + scenePadding * 2f, 1f);
        float aspect = Mathf.Clamp(width / depth, 0.25f, 4f);
        float orthographicSize = Mathf.Max(depth * 0.5f, width / aspect * 0.5f);
        float viewWidth = orthographicSize * 2f * aspect;
        float viewDepth = orthographicSize * 2f;

        mapCamera.transform.position = new Vector3(center.x, height, center.z);
        mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        mapCamera.aspect = aspect;
        mapCamera.orthographicSize = orthographicSize;
        mapCamera.nearClipPlane = 0.1f;
        mapCamera.farClipPlane = Mathf.Max(cameraExtraHeight * 2f, height - sceneBounds.min.y + cameraExtraHeight);
        mapWorldRect = new Rect(center.x - viewWidth * 0.5f, center.z - viewDepth * 0.5f, viewWidth, viewDepth);
    }

    private void DrawPlayerMarker(Rect panelRect)
    {
        if (player == null || markerTexture == null)
        {
            return;
        }

        Rect mapRect = new Rect(panelRect.x + 12f, panelRect.y + 42f, panelRect.width - 24f, panelRect.height - 54f);
        float x = Mathf.InverseLerp(mapWorldRect.xMin, mapWorldRect.xMax, player.position.x);
        float z = Mathf.InverseLerp(mapWorldRect.yMin, mapWorldRect.yMax, player.position.z);
        float markerSize = 18f;
        Rect markerRect = new Rect(
            mapRect.x + x * mapRect.width - markerSize * 0.5f,
            mapRect.y + (1f - z) * mapRect.height - markerSize * 0.5f,
            markerSize,
            markerSize);

        GUI.DrawTexture(markerRect, markerTexture);
    }

    private bool TryUseFaeHomesMazeBounds()
    {
        if (!IsFaeHomesScene())
        {
            return false;
        }

        Vector3 center = faeHomesMazeCenter + Vector3.forward * faeHomesMazeForwardOffset;
        sceneBounds = new Bounds(
            center,
            new Vector3(
                Mathf.Max(1f, faeHomesMazeSize.x),
                Mathf.Max(1f, faeHomesMazeHeight),
                Mathf.Max(1f, faeHomesMazeSize.y)));
        hasSceneBounds = true;
        return true;
    }

    private Rect GetMapRect()
    {
        if (mapExpanded)
        {
            float expandedWidth = Mathf.Clamp(Screen.width * expandedWidthRatio, 520f, Screen.width - GameUiStyle.Margin * 2f);
            float expandedHeight = Mathf.Clamp(Screen.height * expandedHeightRatio, 380f, Screen.height - GameUiStyle.Margin * 2f);
            return new Rect((Screen.width - expandedWidth) * 0.5f, (Screen.height - expandedHeight) * 0.5f, expandedWidth, expandedHeight);
        }

        float width = Mathf.Min(mapWidth, Screen.width - GameUiStyle.Margin * 2f);
        float height = Mathf.Min(mapHeight, Screen.height - GameUiStyle.Margin * 2f);
        float y = GameUiStyle.Margin + sideQuestPanelHeight + 12f;
        if (y + height > Screen.height - GameUiStyle.Margin)
        {
            y = Screen.height - height - GameUiStyle.Margin;
        }

        return new Rect(Screen.width - width - GameUiStyle.Margin, y, width, height);
    }

    private void SetCameraActive(bool active)
    {
        if (mapCamera != null && mapCamera.enabled != active)
        {
            mapCamera.enabled = active;
        }
    }

    private void UpdateMapRender()
    {
        if (mapCamera == null || !hasSceneBounds)
        {
            return;
        }

        if (!mapNeedsRender)
        {
            return;
        }

        RenderMap();
        mapNeedsRender = false;
    }

    private void RenderMap()
    {
        if (mapCamera == null || mapTexture == null || !ShouldShowMap() || !hasSceneBounds)
        {
            return;
        }

        bool wasEnabled = mapCamera.enabled;
        if (wasEnabled)
        {
            mapCamera.enabled = false;
        }

        mapCamera.Render();
    }

    private bool ShouldShowMap()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return IsEnchantedForestScene() ||
            IsFaeHomesScene() ||
            string.Equals(sceneName, "my scene", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFaeHomesScene()
    {
        return string.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "Fae Homes Demo", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEnchantedForestScene()
    {
        return string.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "Enchanted Forest A", System.StringComparison.OrdinalIgnoreCase);
    }

    private static Texture2D CreateCircleTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float radius = center;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(1f - (distance - radius * 0.72f) / (radius * 0.28f));
                texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
            }
        }

        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }
}
