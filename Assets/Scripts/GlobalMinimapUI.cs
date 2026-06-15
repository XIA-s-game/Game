using UnityEngine;

public class GlobalMinimapUI : MonoBehaviour
{
    // Built with AI assistance to keep shared menu layout consistent across scenes.
    [Header("Scene References")]
    // Each scene drags its own map camera and player instead of relying on scene-name checks.
    [SerializeField] private Camera mapCamera;
    [SerializeField] private Transform player;

    [Header("Display")]
    [SerializeField] private bool showMap = true;
    [SerializeField] private bool showCompactMap = true;

    [Header("Render")]
    [SerializeField] private int textureWidth = 640;
    [SerializeField] private int textureHeight = 360;
    [SerializeField] private float mapWidth = 340f;
    [SerializeField] private float mapHeight = 230f;
    [SerializeField] private float expandedWidthRatio = 0.72f;
    [SerializeField] private float expandedHeightRatio = 0.72f;
    [SerializeField] private float sideQuestPanelHeight = 260f;
    [SerializeField] private Color playerColor = new Color(1f, 0.86f, 0.18f, 1f);

    [Header("Map UI")]
    [SerializeField] private Rect titleRect = new Rect(14f, 8f, -28f, 30f);
    [SerializeField] private Rect mapImageRect = new Rect(12f, 42f, -24f, -54f);
    [SerializeField] private int titleFontSize = 17;
    [SerializeField] private float compactStackGap = 12f;
    [SerializeField] private Vector2 expandedMinSize = new Vector2(520f, 380f);
    [SerializeField] private float playerMarkerSize = 18f;

    private RenderTexture mapTexture;
    private Rect mapWorldRect;
    private bool hasMapView;
    private bool mapExpanded;
    private Texture2D markerTexture;
    private GUIStyle titleStyle;
    private bool mapNeedsRender;

    private void Awake() 
    {
        // Initialize the instance and reference of the mini-map. 
        if (mapTexture == null)
        {
            mapTexture = new RenderTexture(Mathf.Max(64, textureWidth), Mathf.Max(64, textureHeight), 16, RenderTextureFormat.ARGB32);
            mapTexture.name = "GlobalMinimapTexture";
            mapTexture.Create();
        }

        if (markerTexture == null)
        {
            markerTexture = CreateCircleTexture(48, playerColor);
        }

        if (mapCamera != null)
        {
            mapCamera.targetTexture = mapTexture;
            mapCamera.aspect = (float)mapTexture.width / mapTexture.height;
            mapCamera.enabled = false;
        }

        hasMapView = false;
        mapWorldRect = default;
        mapNeedsRender = true;
        SetCameraActive(false);
        DrawMapOnce();
    }
    // Clean up static references during destruction.
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
        // For each frame, determine whether the map should be displayed and update the map rendering.
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

        if (!hasMapView)
        {
            ReadMapCameraArea();
        }

        UpdateMapRender();
    }
    
    // Draws the map and player marker on the GUI when the map is active.
    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || !ShouldShowMap() || mapTexture == null || !hasMapView)
        {
            return;
        }

        if (!mapExpanded && !showCompactMap)
        {
            return;
        }

        Rect rect = GetMapRect();
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.DrawTexture(InnerRect(rect, mapImageRect), mapTexture, ScaleMode.StretchToFill, false);
        string title = mapExpanded ? "Map - Press M to close" : "Map - Press M";
        GUI.Label(InnerRect(rect, titleRect), title, GameUiStyle.LabelStyle(ref titleStyle, titleFontSize, TextAnchor.MiddleLeft, FontStyle.Bold));
        DrawPlayerMarker(rect);
    }
    // Reads the map camera's view area and renders the map once during initialization or when the camera changes, avoiding unnecessary renders.
    private void DrawMapOnce()
    {
        ReadMapCameraArea();
        RenderMap();
        mapNeedsRender = false;
    }
    
    // Reads the map camera's orthographic bounds to determine the world area shown on the map, which is used to position the player marker correctly. If the camera is not orthographic, it defaults to a 1x1 area around the camera's position.
    private void ReadMapCameraArea()
    {
        // Orthographic camera bounds define the playable area shown by the marker.
        hasMapView = false;

        if (mapCamera == null)
        {
            return;
        }

        if (mapCamera.orthographic)
        {
            float viewDepth = mapCamera.orthographicSize * 2f;
            float viewWidth = viewDepth * mapCamera.aspect;
            Vector3 center = mapCamera.transform.position;
            mapWorldRect = new Rect(center.x - viewWidth * 0.5f, center.z - viewDepth * 0.5f, viewWidth, viewDepth);
        }
        else
        {
            mapWorldRect = new Rect(mapCamera.transform.position.x - 0.5f, mapCamera.transform.position.z - 0.5f, 1f, 1f);
        }

        hasMapView = true;
    }

    // Draw player markers on the map.
    private void DrawPlayerMarker(Rect panelRect)
    {
        // Player marker is drawn from the map camera viewport position.
        if (player == null || markerTexture == null || mapCamera == null)
        {
            return;
        }

        Rect mapRect = InnerRect(panelRect, mapImageRect);
        Vector3 viewportPoint = mapCamera.WorldToViewportPoint(player.position);
        if (viewportPoint.z < 0f)
        {
            return;
        }

        float x = viewportPoint.x;
        float z = viewportPoint.y;
        if (x < 0f || x > 1f || z < 0f || z > 1f)
        {
            return;
        }

        float markerSize = playerMarkerSize;
        Rect markerRect = new Rect(
            mapRect.x + x * mapRect.width - markerSize * 0.5f,
            mapRect.y + (1f - z) * mapRect.height - markerSize * 0.5f,
            markerSize,
            markerSize);

        GUI.DrawTexture(markerRect, markerTexture);
    }
    
    // Calculate the position and size of the mini-map on the screen.
    private Rect GetMapRect()
    {
        if (mapExpanded)
        {
            float expandedWidth = Mathf.Clamp(Screen.width * expandedWidthRatio, expandedMinSize.x, Screen.width - GameUiStyle.Margin * 2f);
            float expandedHeight = Mathf.Clamp(Screen.height * expandedHeightRatio, expandedMinSize.y, Screen.height - GameUiStyle.Margin * 2f);
            return new Rect((Screen.width - expandedWidth) * 0.5f, (Screen.height - expandedHeight) * 0.5f, expandedWidth, expandedHeight);
        }

        float width = Mathf.Min(mapWidth, Screen.width - GameUiStyle.Margin * 2f);
        float height = Mathf.Min(mapHeight, Screen.height - GameUiStyle.Margin * 2f);
        float y = GameUiStyle.Margin + sideQuestPanelHeight + compactStackGap;
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
        if (mapCamera == null || !hasMapView)
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
        // Renders only when the map is opened or marked dirty, avoiding a camera render every frame.
        if (mapCamera == null || mapTexture == null || !ShouldShowMap() || !hasMapView)
        {
            return;
        }

        mapCamera.aspect = (float)mapTexture.width / mapTexture.height;
        bool wasEnabled = mapCamera.enabled;
        if (wasEnabled)
        {
            mapCamera.enabled = false;
        }

        mapCamera.Render();
    }
    
    // Determine whether the map should be displayed at present.
    private bool ShouldShowMap()
    {
        return showMap;
    }

    private static Rect InnerRect(Rect parent, Rect localRect)
    {
        float y = localRect.y >= 0f ? parent.y + localRect.y : parent.yMax + localRect.y;
        float width = localRect.width >= 0f ? localRect.width : parent.width + localRect.width;
        float height = localRect.height >= 0f ? localRect.height : parent.height + localRect.height;
        return new Rect(parent.x + localRect.x, y, width, height);
    }
    // Create a small circular texture for player markers.
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
