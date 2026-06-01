// Main function: Draws the minimap HUD in allowed scenes, including map bounds, grid lines, player direction, and movement trail.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniMapHUD : MonoBehaviour
{
    [System.Serializable]
    private struct MapArea
    {
        public string sceneName;
        public Vector2 center;
        public Vector2 size;
        public float rotation;
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;

    [Header("Scenes")]
    [SerializeField] private string[] sceneNames =
    {
        "Enchanted Forest A",
        "Fae Homes Demo",
        "my scene"
    };
    [SerializeField] private MapArea[] mapAreas;
    [SerializeField] private bool calculateSceneSize = true;
    [SerializeField] private float worldPadding = 10f;

    [Header("Layout")]
    [SerializeField] private float mapSize = 190f;
    [SerializeField] private float expandedMapSize = 520f;
    [SerializeField] private float margin = 24f;
    [SerializeField] private float border = 10f;
    [SerializeField] private KeyCode toggleKey = KeyCode.M;

    [Header("Trail")]
    [SerializeField] private bool showTrail = true;
    [SerializeField] private int maxTrailPoints = 80;
    [SerializeField] private float trailSpacing = 2.5f;

    [Header("Colors")]
    [SerializeField] private Color mapColor = new Color(0.03f, 0.08f, 0.08f, 0.82f);
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private Color trailColor = new Color(0.45f, 0.95f, 1f, 0.55f);
    [SerializeField] private Color playerColor = new Color(1f, 0.85f, 0.2f, 1f);

    private readonly List<Vector2> trail = new List<Vector2>();
    private Texture2D mapTexture;
    private Texture2D gridTexture;
    private Texture2D trailTexture;
    private Texture2D playerTexture;
    private GUIStyle labelStyle;
    private Vector2 mapCenter;
    private Vector2 mapWorldSize;
    private float mapRotation;
    private string loadedSceneName;
    private bool mapReady;
    private bool expanded;

    // Function: Initializes component references, cached state, and default runtime data.
    private void Awake()
    {
        CreateTextures();
        RefreshScene();
    }

    // Function: Updates input handling, interaction checks, and active gameplay flow each frame.
    private void Update()
    {
        if (!IsAllowedScene())
        {
            return;
        }

        RefreshReferences();
        EnsureMapArea();
        AddTrailPoint();

        if (Input.GetKeyDown(toggleKey))
        {
            expanded = !expanded;
        }
    }

    // Function: Draws this script's IMGUI prompts, panels, and dialogue.
    private void OnGUI()
    {
        if (!IsAllowedScene())
        {
            return;
        }

        RefreshReferences();
        EnsureMapArea();

        if (!mapReady)
        {
            return;
        }

        Rect panel = GetMapRect();
        GameUiStyle.DrawPanel(panel);

        Rect mapRect = new Rect(panel.x + border, panel.y + border, panel.width - border * 2f, panel.height - border * 2f);
        GUI.DrawTexture(mapRect, mapTexture);
        DrawGrid(mapRect);
        DrawTrail(mapRect);
        DrawPlayer(mapRect);
        DrawLabel(panel);
    }

    // Function: Gets or calculates map rect.
    private Rect GetMapRect()
    {
        if (!expanded)
        {
            return new Rect(Screen.width - mapSize - margin, margin, mapSize, mapSize);
        }

        float size = Mathf.Min(expandedMapSize, Screen.width - margin * 2f, Screen.height - margin * 2f);
        return new Rect((Screen.width - size) * 0.5f, (Screen.height - size) * 0.5f, size, size);
    }

    // Function: Refreshes cached references or state for scene.
    private void RefreshScene()
    {
        loadedSceneName = SceneManager.GetActiveScene().name;
        mapReady = false;
        trail.Clear();
        RefreshReferences();
        EnsureMapArea();
    }

    // Function: Refreshes cached references or state for references.
    private void RefreshReferences()
    {
        if (playerCamera == null || !playerCamera.gameObject.activeInHierarchy)
        {
            playerCamera = Camera.main;
        }

        if (player == null && playerCamera != null)
        {
            Transform root = playerCamera.transform.root;
            if (root != transform)
            {
                player = root;
            }
        }
    }

    // Function: Ensures map area exists, is configured, or is ready to use.
    private void EnsureMapArea()
    {
        if (mapReady && loadedSceneName == SceneManager.GetActiveScene().name)
        {
            return;
        }

        loadedSceneName = SceneManager.GetActiveScene().name;
        if (TryGetManualArea(loadedSceneName, out MapArea area))
        {
            mapCenter = area.center;
            mapWorldSize = ClampWorldSize(area.size);
            mapRotation = area.rotation;
            mapReady = true;
            return;
        }

        if (calculateSceneSize && TryCalculateSceneArea(out Vector2 center, out Vector2 size))
        {
            mapCenter = center;
            mapWorldSize = ClampWorldSize(size);
            mapRotation = 0f;
            mapReady = true;
            return;
        }

        if (player != null)
        {
            Vector3 position = player.position;
            mapCenter = new Vector2(position.x, position.z);
            mapWorldSize = new Vector2(100f, 100f);
            mapRotation = 0f;
            mapReady = true;
        }
    }

    // Function: Tries to get manual area and returns whether it was found.
    private bool TryGetManualArea(string sceneName, out MapArea area)
    {
        if (mapAreas != null)
        {
            for (int i = 0; i < mapAreas.Length; i++)
            {
                if (string.Equals(mapAreas[i].sceneName, sceneName, System.StringComparison.OrdinalIgnoreCase) &&
                    mapAreas[i].size.x > 0f &&
                    mapAreas[i].size.y > 0f)
                {
                    area = mapAreas[i];
                    return true;
                }
            }
        }

        area = new MapArea();
        return false;
    }

    // Function: Tries to calculate scene area and returns whether the calculation succeeded.
    private bool TryCalculateSceneArea(out Vector2 center, out Vector2 size)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;
        Scene activeScene = SceneManager.GetActiveScene();

        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null || terrain.gameObject.scene != activeScene)
            {
                continue;
            }

            Vector3 terrainSize = terrain.terrainData.size;
            Bounds terrainBounds = new Bounds(terrain.transform.position + terrainSize * 0.5f, terrainSize);
            AddBounds(ref bounds, ref hasBounds, terrainBounds);
        }

        Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
        Transform playerRoot = player != null ? player.root : null;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null ||
                !renderer.enabled ||
                !renderer.gameObject.scene.IsValid() ||
                renderer.gameObject.scene != activeScene)
            {
                continue;
            }

            if (playerRoot != null && renderer.transform.root == playerRoot)
            {
                continue;
            }

            AddBounds(ref bounds, ref hasBounds, renderer.bounds);
        }

        if (!hasBounds)
        {
            center = Vector2.zero;
            size = Vector2.zero;
            return false;
        }

        center = new Vector2(bounds.center.x, bounds.center.z);
        size = new Vector2(bounds.size.x + worldPadding * 2f, bounds.size.z + worldPadding * 2f);
        return true;
    }

    // Function: Adds bounds.
    private void AddBounds(ref Bounds bounds, ref bool hasBounds, Bounds next)
    {
        if (!hasBounds)
        {
            bounds = next;
            hasBounds = true;
        }
        else
        {
            bounds.Encapsulate(next);
        }
    }

    // Function: Runs the clamp world size logic.
    private Vector2 ClampWorldSize(Vector2 size)
    {
        return new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
    }

    // Function: Adds trail point.
    private void AddTrailPoint()
    {
        if (!showTrail || player == null)
        {
            return;
        }

        Vector2 point = new Vector2(player.position.x, player.position.z);
        if (trail.Count > 0 && Vector2.Distance(trail[trail.Count - 1], point) < trailSpacing)
        {
            return;
        }

        trail.Add(point);
        while (trail.Count > maxTrailPoints)
        {
            trail.RemoveAt(0);
        }
    }

    // Function: Draws the UI elements for grid.
    private void DrawGrid(Rect rect)
    {
        for (int i = 1; i < 4; i++)
        {
            float x = rect.x + rect.width * i / 4f;
            float y = rect.y + rect.height * i / 4f;
            GUI.DrawTexture(new Rect(x, rect.y, 1f, rect.height), gridTexture);
            GUI.DrawTexture(new Rect(rect.x, y, rect.width, 1f), gridTexture);
        }
    }

    // Function: Draws the UI elements for trail.
    private void DrawTrail(Rect rect)
    {
        if (!showTrail)
        {
            return;
        }

        for (int i = 0; i < trail.Count; i++)
        {
            Vector2 point = WorldToMap(rect, trail[i]);
            GUI.DrawTexture(new Rect(point.x - 2f, point.y - 2f, 4f, 4f), trailTexture);
        }
    }

    // Function: Draws the UI elements for player.
    private void DrawPlayer(Rect rect)
    {
        Transform basis = player != null ? player : (playerCamera != null ? playerCamera.transform : null);
        if (basis == null)
        {
            return;
        }

        Vector2 mapPoint = WorldToMap(rect, new Vector2(basis.position.x, basis.position.z));
        GUI.DrawTexture(new Rect(mapPoint.x - 5f, mapPoint.y - 5f, 10f, 10f), playerTexture);

        float heading = GetHeading(basis);
        DrawRotatedTexture(new Rect(mapPoint.x - 1.5f, mapPoint.y - 14f, 3f, 12f), playerTexture, heading - mapRotation);
    }

    // Function: Runs the world to map logic.
    private Vector2 WorldToMap(Rect rect, Vector2 world)
    {
        Vector2 offset = world - mapCenter;
        if (Mathf.Abs(mapRotation) > 0.001f)
        {
            offset = Rotate(offset, -mapRotation);
        }

        float x = Mathf.Clamp01(offset.x / mapWorldSize.x + 0.5f);
        float y = Mathf.Clamp01(offset.y / mapWorldSize.y + 0.5f);
        return new Vector2(rect.x + x * rect.width, rect.yMax - y * rect.height);
    }

    // Function: Gets or calculates heading.
    private float GetHeading(Transform basis)
    {
        Vector3 forward = basis.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return 0f;
        }

        return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
    }

    // Function: Rotates the current script or calculates a rotation result.
    private Vector2 Rotate(Vector2 value, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }

    // Function: Draws the UI elements for rotated texture.
    private void DrawRotatedTexture(Rect rect, Texture2D texture, float angle)
    {
        Vector2 pivot = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height);
        Matrix4x4 oldMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, pivot);
        GUI.DrawTexture(rect, texture);
        GUI.matrix = oldMatrix;
    }

    // Function: Draws the UI elements for label.
    private void DrawLabel(Rect panel)
    {
        GUIStyle style = GameUiStyle.LabelStyle(ref labelStyle, 12, TextAnchor.UpperLeft, FontStyle.Bold);
        GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, 20f), "MAP", style);
    }

    // Function: Checks whether allowed scene is true.
    private bool IsAllowedScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneNames == null || sceneNames.Length == 0)
        {
            return !IsMenuScene(sceneName);
        }

        for (int i = 0; i < sceneNames.Length; i++)
        {
            if (string.Equals(sceneNames[i], sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // Function: Creates the objects, textures, or UI needed for textures.
    private void CreateTextures()
    {
        mapTexture = CreateTexture(mapColor);
        gridTexture = CreateTexture(gridColor);
        trailTexture = CreateTexture(trailColor);
        playerTexture = CreateTexture(playerColor);
    }

    // Function: Creates the objects, textures, or UI needed for texture.
    private Texture2D CreateTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    // Function: Checks whether menu scene is true.
    private static bool IsMenuScene(string sceneName)
    {
        return string.Equals(sceneName, "MainMenu", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sceneName, "Mainmenu", System.StringComparison.OrdinalIgnoreCase);
    }
}
