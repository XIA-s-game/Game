using UnityEngine;

public class CompassNavigationHUD : MonoBehaviour
{
    // Draws a simple heading bar using the player camera direction.
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool showCompass = true;

    [Header("Layout")]
    [SerializeField] private float leftOffset = 150f;
    [SerializeField] private float widthRatio = 0.8f;
    [SerializeField] private float topOffset = 10f;
    [SerializeField] private float panelHeight = 120f;
    [SerializeField] private float panelMinHeight = 120f;
    [SerializeField] private float degreesVisible = 60f;
    [SerializeField] private int minorTickStep = 5;
    [SerializeField] private int majorTickStep = 30;
    [SerializeField] private int labelStep = 45;
    [SerializeField] private float tickTop = 8f;
    [SerializeField] private float tickBaseBottomOffset = 46f;
    [SerializeField] private Vector2 tickSize = new Vector2(2f, 12f);
    [SerializeField] private float majorTickHeight = 21f;
    [SerializeField] private float labelTickHeight = 28f;
    [SerializeField] private Rect directionLabelRect = new Rect(-90f, 8f, 180f, 108f);
    [SerializeField] private Rect degreeLabelRect = new Rect(-75f, -48f, 150f, 44f);
    [SerializeField] private Vector2 centerLineSize = new Vector2(4f, -8f);
    [SerializeField] private float centerLineTopPadding = 4f;
    [SerializeField] private Rect centerLabelRect = new Rect(-160f, -62f, 320f, 56f);
    [SerializeField] private int directionLabelFontSize = 30;
    [SerializeField] private int degreeLabelFontSize = 25;
    [SerializeField] private int centerLabelFontSize = 20;

    [Header("Colors")]
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.42f);
    [SerializeField] private Color tickColor = new Color(1f, 1f, 1f, 0.82f);
    [SerializeField] private Color centerColor = new Color(1f, 0.86f, 0.25f, 1f);

    private readonly string[] directionLabels = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    private GUIStyle directionLabelStyle;
    private GUIStyle degreeLabelStyle;
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
 
    // CCalculate the current orientation angle based on the player's/camera's direction.
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
