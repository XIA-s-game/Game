using UnityEngine;

public class CompassNavigationHUD : MonoBehaviour
{
    // Draws a simple heading bar using the player camera direction.
    [Header("References")]
    // Player fallback if there is no camera reference.
    [SerializeField] private Transform player;
    // Camera direction is preferred for heading.
    [SerializeField] private Camera targetCamera;
    // Master visibility toggle.
    [SerializeField] private bool showCompass = true;

    [Header("Layout")]
    // Left edge of the compass panel.
    [SerializeField] private float leftOffset = 150f;
    // Width as a portion of the screen.
    [SerializeField] private float widthRatio = 0.8f;
    // Top edge of the compass panel.
    [SerializeField] private float topOffset = 10f;
    // Requested panel height.
    [SerializeField] private float panelHeight = 120f;
    // Minimum panel height.
    [SerializeField] private float panelMinHeight = 120f;
    // Degrees shown around the current heading.
    [SerializeField] private float degreesVisible = 60f;
    // Small tick spacing in degrees.
    [SerializeField] private int minorTickStep = 5;
    // Larger tick spacing in degrees.
    [SerializeField] private int majorTickStep = 30;
    // Label spacing in degrees.
    [SerializeField] private int labelStep = 45;
    // Top position for direction labels.
    [SerializeField] private float tickTop = 8f;
    // Bottom offset used as the tick baseline.
    [SerializeField] private float tickBaseBottomOffset = 46f;
    // Width and height for minor ticks.
    [SerializeField] private Vector2 tickSize = new Vector2(2f, 12f);
    // Height for major ticks.
    [SerializeField] private float majorTickHeight = 21f;
    // Height for labelled ticks.
    [SerializeField] private float labelTickHeight = 28f;
    // Local rect for compass direction labels.
    [SerializeField] private Rect directionLabelRect = new Rect(-90f, 8f, 180f, 108f);
    // Local rect for degree labels.
    [SerializeField] private Rect degreeLabelRect = new Rect(-75f, -48f, 150f, 44f);
    // Size of the center line marker.
    [SerializeField] private Vector2 centerLineSize = new Vector2(4f, -8f);
    // Top padding for the center line.
    [SerializeField] private float centerLineTopPadding = 4f;
    // Local rect for the center heading label.
    [SerializeField] private Rect centerLabelRect = new Rect(-160f, -62f, 320f, 56f);
    // Font size for N/E/S/W labels.
    [SerializeField] private int directionLabelFontSize = 30;
    // Font size for degree labels.
    [SerializeField] private int degreeLabelFontSize = 25;
    // Font size for the center heading label.
    [SerializeField] private int centerLabelFontSize = 20;

    [Header("Colors")]
    // Background panel color.
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.42f);
    // Tick and label color.
    [SerializeField] private Color tickColor = new Color(1f, 1f, 1f, 0.82f);
    // Center marker color.
    [SerializeField] private Color centerColor = new Color(1f, 0.86f, 0.25f, 1f);

    // Compass labels ordered clockwise.
    private readonly string[] directionLabels = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    // Cached style for direction labels.
    private GUIStyle directionLabelStyle;
    // Cached style for degree labels.
    private GUIStyle degreeLabelStyle;
    // Cached style for the center label.
    private GUIStyle centerLabelStyle;
    
    // Draw the compass interface
    private void OnGUI()
    {
        if (!showCompass || Event.current.type != EventType.Repaint)
        {
            return;
        }

        float heading = GetHeading();
        Rect panel = GetPanelRect();

        DrawSolidRect(panel, panelColor);
        DrawTicks(panel, heading);
        DrawCenterMarker(panel, heading);
    }
 
    // Calculate the current orientation angle based on the player's/camera's direction.
    private float GetHeading()
    {
        // Camera forward is preferred so the compass matches what the player is looking at.
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

    // Calculate the position of the compass UI.
    private Rect GetPanelRect()
    {
        float height = Mathf.Max(panelHeight, panelMinHeight);
        return new Rect(leftOffset, topOffset, Screen.width * widthRatio, height);
    }

    private void DrawTicks(Rect panel, float heading)
    {
        // Tick marks are centered around the current heading rather than drawing the full 360 degrees.
        float tickBase = panel.height - tickBaseBottomOffset;
        float visibleDegrees = Mathf.Max(1f, degreesVisible);
        float pixelsPerDegree = panel.width / visibleDegrees;
        int startDegree = Mathf.FloorToInt((heading - visibleDegrees * 0.5f) / minorTickStep) * minorTickStep;
        int endDegree = Mathf.CeilToInt((heading + visibleDegrees * 0.5f) / minorTickStep) * minorTickStep;

        GUI.BeginGroup(panel);
        GUIStyle labelStyle = GetLabelStyle(ref directionLabelStyle, directionLabelFontSize, TextAnchor.UpperCenter, Color.white);
        GUIStyle degreeStyle = GetLabelStyle(ref degreeLabelStyle, degreeLabelFontSize, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.72f));

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
            float tickHeight = labelTick ? labelTickHeight : (majorTick ? majorTickHeight : tickSize.y);
            DrawSolidRect(new Rect(localX - tickSize.x * 0.5f, tickBase - tickHeight, tickSize.x, tickHeight), tickColor);

            if (labelTick)
            {
                Rect labelRect = directionLabelRect;
                labelRect.y = tickTop;
                GUI.Label(OffsetRect(labelRect, localX, 0f), BuildTickLabel(normalizedDegree), labelStyle);
            }
            else if (majorTick)
            {
                GUI.Label(OffsetRect(degreeLabelRect, localX, tickBase - tickHeight), BuildDegreeLabel(normalizedDegree), degreeStyle);
            }
        }

        GUI.EndGroup();
    }
    
    // Draw the intermediate indication mark.
    private void DrawCenterMarker(Rect panel, float heading)
    {
        float centerX = panel.x + panel.width * 0.5f;
        float centerHeight = centerLineSize.y >= 0f ? centerLineSize.y : panel.height + centerLineSize.y;
        DrawSolidRect(new Rect(centerX - centerLineSize.x * 0.5f, panel.y + centerLineTopPadding, centerLineSize.x, centerHeight), centerColor);

        GUIStyle style = GetLabelStyle(ref centerLabelStyle, centerLabelFontSize, TextAnchor.MiddleCenter, centerColor);
        GUI.Label(OffsetRect(centerLabelRect, centerX, panel.y + panel.height), BuildCenterLabel(heading), style);
    }
    
    // Get the compass direction label (N, NE, E, etc.) based on the degree value.
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

    private static Rect OffsetRect(Rect rect, float originX, float originY)
    {
        return new Rect(originX + rect.x, originY + rect.y, rect.width, rect.height);
    }

    private static void DrawSolidRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    // Generate the label for major ticks, combining the direction and degree information.
    private string BuildTickLabel(float degree)
    {
        string degreeLabel = BuildDegreeLabel(degree);
        return GetDirectionLabel(degree) + "\n" + degreeLabel;
    }

    // Generate centered text
    private string BuildCenterLabel(float degree)
    {
        return GetDirectionLabel(degree) + "  " + BuildDegreeLabel(degree);
    }
    
    // Generate angled text.
    private static string BuildDegreeLabel(float degree)
    {
        return Mathf.RoundToInt(Mathf.Repeat(degree, 360f)) + " deg";
    }
}
