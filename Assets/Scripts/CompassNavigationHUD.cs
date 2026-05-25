using UnityEngine;
using UnityEngine.SceneManagement;

public class CompassNavigationHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;

    [Header("Layout")]
    [SerializeField] private float topOffset = 16f;
    [SerializeField] private float widthRatio = 0.72f;
    [SerializeField] private float panelHeight = 76f;
    [SerializeField] private float degreesVisible = 120f;
    [SerializeField] private int minorTickStep = 5;
    [SerializeField] private int majorTickStep = 15;
    [SerializeField] private int labelStep = 45;

    [Header("Colors")]
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.42f);
    [SerializeField] private Color tickColor = new Color(1f, 1f, 1f, 0.82f);
    [SerializeField] private Color centerColor = new Color(1f, 0.86f, 0.25f, 1f);

    private static CompassNavigationHUD instance;

    private readonly string[] directionLabels = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    private Texture2D panelTexture;
    private Texture2D tickTexture;
    private Texture2D centerTexture;
    private GUIStyle directionLabelStyle;
    private GUIStyle degreeLabelStyle;
    private GUIStyle centerLabelStyle;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshReferences();
        CreateTextures();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshReferences();
    }

    private void Update()
    {
        if (targetCamera == null || !targetCamera.gameObject.activeInHierarchy)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnGUI()
    {
        if (IsMenuScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        float heading = GetHeading();
        Rect panel = GetPanelRect();

        GUI.DrawTexture(panel, panelTexture);
        DrawTicks(panel, heading);
        DrawCenterMarker(panel, heading);
    }

    private float GetHeading()
    {
        Transform basis = targetCamera != null ? targetCamera.transform : player;
        if (basis == null)
        {
            return 0f;
        }

        Vector3 forward = basis.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return 0f;
        }

        float heading = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        return Mathf.Repeat(heading, 360f);
    }

    private Rect GetPanelRect()
    {
        float width = Mathf.Clamp(Screen.width * widthRatio, 420f, Screen.width - 32f);
        return new Rect((Screen.width - width) * 0.5f, topOffset, width, panelHeight);
    }

    private void DrawTicks(Rect panel, float heading)
    {
        float centerX = panel.x + panel.width * 0.5f;
        float top = panel.y + 8f;
        float tickBase = panel.y + panel.height - 18f;
        float pixelsPerDegree = panel.width / degreesVisible;
        int startDegree = Mathf.FloorToInt((heading - degreesVisible * 0.5f) / minorTickStep) * minorTickStep;
        int endDegree = Mathf.CeilToInt((heading + degreesVisible * 0.5f) / minorTickStep) * minorTickStep;

        GUI.BeginGroup(panel);
        GUIStyle labelStyle = GetLabelStyle(ref directionLabelStyle, 15, TextAnchor.UpperCenter, Color.white);
        GUIStyle degreeStyle = GetLabelStyle(ref degreeLabelStyle, 12, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.72f));

        for (int degree = startDegree; degree <= endDegree; degree += minorTickStep)
        {
            float normalizedDegree = Mathf.Repeat(degree, 360f);
            float delta = Mathf.DeltaAngle(heading, normalizedDegree);
            float localX = panel.width * 0.5f + delta * pixelsPerDegree;
            if (localX < 0f || localX > panel.width)
            {
                continue;
            }

            bool majorTick = Mathf.RoundToInt(normalizedDegree) % majorTickStep == 0;
            bool labelTick = Mathf.RoundToInt(normalizedDegree) % labelStep == 0;
            float tickHeight = labelTick ? 28f : (majorTick ? 21f : 12f);
            GUI.DrawTexture(new Rect(localX - 1f, tickBase - tickHeight, 2f, tickHeight), tickTexture);

            if (labelTick)
            {
                string direction = GetDirectionLabel(normalizedDegree);
                string label = direction + "\n" + Mathf.RoundToInt(normalizedDegree) + " deg";
                GUI.Label(new Rect(localX - 34f, top, 68f, 42f), label, labelStyle);
            }
            else if (majorTick)
            {
                GUI.Label(new Rect(localX - 22f, tickBase - tickHeight - 16f, 44f, 16f), Mathf.RoundToInt(normalizedDegree) + " deg", degreeStyle);
            }
        }

        GUI.EndGroup();
    }

    private void DrawCenterMarker(Rect panel, float heading)
    {
        float centerX = panel.x + panel.width * 0.5f;
        GUI.DrawTexture(new Rect(centerX - 2f, panel.y + 4f, 4f, panel.height - 8f), centerTexture);

        GUIStyle style = GetLabelStyle(ref centerLabelStyle, 18, TextAnchor.MiddleCenter, centerColor);
        string label = GetDirectionLabel(heading) + "  " + Mathf.RoundToInt(heading) + " deg";
        GUI.Label(new Rect(centerX - 70f, panel.y + panel.height - 24f, 140f, 22f), label, style);
    }

    private string GetDirectionLabel(float degree)
    {
        int index = Mathf.RoundToInt(Mathf.Repeat(degree, 360f) / 45f) % directionLabels.Length;
        return directionLabels[index];
    }

    private GUIStyle GetLabelStyle(ref GUIStyle style, int fontSize, TextAnchor alignment, Color color)
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
        }

        style.alignment = alignment;
        style.fontSize = fontSize;
        style.wordWrap = false;
        style.normal.textColor = color;
        return style;
    }

    private void RefreshReferences()
    {
        targetCamera = Camera.main;
    }

    private void CreateTextures()
    {
        panelTexture = CreateSolidTexture(panelColor);
        tickTexture = CreateSolidTexture(tickColor);
        centerTexture = CreateSolidTexture(centerColor);
    }

    private static Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private static bool IsMenuScene(string sceneName)
    {
        return string.Equals(sceneName, "MainMenu", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sceneName, "Mainmenu", System.StringComparison.OrdinalIgnoreCase);
    }
}
