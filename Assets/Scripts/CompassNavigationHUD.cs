using UnityEngine;
using UnityEngine.SceneManagement;

public class CompassNavigationHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool showCompass = true;

    [Header("Layout")]
    [SerializeField] private float topOffset = 10f;
    [SerializeField] private float panelHeight = 120f;
    [SerializeField] private float degreesVisible = 60f;
    [SerializeField] private int minorTickStep = 5;
    [SerializeField] private int majorTickStep = 30;
    [SerializeField] private int labelStep = 45;

    [Header("Colors")]
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.42f);
    [SerializeField] private Color tickColor = new Color(1f, 1f, 1f, 0.82f);
    [SerializeField] private Color centerColor = new Color(1f, 0.86f, 0.25f, 1f);

    private readonly string[] directionLabels = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    private GUIStyle directionLabelStyle;
    private GUIStyle degreeLabelStyle;
    private GUIStyle centerLabelStyle;

    private void Awake()
    {
        RefreshReferences();
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

        if (player == null && targetCamera != null)
        {
            player = targetCamera.transform.root;
        }
    }

    private void OnGUI()
    {
        if (!showCompass || Event.current.type != EventType.Repaint || IsMenuScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        float heading = GetHeading();
        Rect panel = GetPanelRect();

        DrawSolidRect(panel, panelColor);
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
        float height = Mathf.Max(panelHeight, 120f);
        return new Rect(150f, topOffset, Screen.width * 0.8f, height);
    }

    private void DrawTicks(Rect panel, float heading)
    {
        float top = 8f;
        float tickBase = panel.height - 46f;
        float visibleDegrees = Mathf.Min(degreesVisible, 60f);
        float pixelsPerDegree = panel.width / visibleDegrees;
        int startDegree = Mathf.FloorToInt((heading - visibleDegrees * 0.5f) / minorTickStep) * minorTickStep;
        int endDegree = Mathf.CeilToInt((heading + visibleDegrees * 0.5f) / minorTickStep) * minorTickStep;

        GUI.BeginGroup(panel);
        GUIStyle labelStyle = GetLabelStyle(ref directionLabelStyle, 30, TextAnchor.UpperCenter, Color.white);
        GUIStyle degreeStyle = GetLabelStyle(ref degreeLabelStyle, 25, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.72f));

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
            DrawSolidRect(new Rect(localX - 1f, tickBase - tickHeight, 2f, tickHeight), tickColor);

            if (labelTick)
            {
                GUI.Label(new Rect(localX - 90f, top, 180f, 108f), BuildTickLabel(normalizedDegree), labelStyle);
            }
            else if (majorTick)
            {
                GUI.Label(new Rect(localX - 75f, tickBase - tickHeight - 48f, 150f, 44f), BuildDegreeLabel(normalizedDegree), degreeStyle);
            }
        }

        GUI.EndGroup();
    }

    private void DrawCenterMarker(Rect panel, float heading)
    {
        float centerX = panel.x + panel.width * 0.5f;
        DrawSolidRect(new Rect(centerX - 2f, panel.y + 4f, 4f, panel.height - 8f), centerColor);

        GUIStyle style = GetLabelStyle(ref centerLabelStyle, 20, TextAnchor.MiddleCenter, centerColor);
        GUI.Label(new Rect(centerX - 160f, panel.y + panel.height - 62f, 320f, 56f), BuildCenterLabel(heading), style);
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

    private static void DrawSolidRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private string BuildTickLabel(float degree)
    {
        string degreeLabel = BuildDegreeLabel(degree);
        return GetDirectionLabel(degree) + "\n" + degreeLabel;
    }

    private string BuildCenterLabel(float degree)
    {
        return GetDirectionLabel(degree) + "  " + BuildDegreeLabel(degree);
    }

    private static string BuildDegreeLabel(float degree)
    {
        return Mathf.RoundToInt(Mathf.Repeat(degree, 360f)) + " deg";
    }

    private static bool IsMenuScene(string sceneName)
    {
        return string.Equals(sceneName, "MainMenu", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sceneName, "Mainmenu", System.StringComparison.OrdinalIgnoreCase);
    }
}
