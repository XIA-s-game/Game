// Main function: Provides shared IMGUI layout rectangles, panel drawing, and label styling for consistent game UI.

using UnityEngine;

public static class GameUiStyle
{
    public const float Margin = 24f;
    public const float BottomPromptY = 132f;

    private static Texture2D panelTexture;

    // Function: Calculates the bottom interaction prompt rectangle.
    public static Rect InteractionPromptRect(float width = 440f, float height = 60f)
    {
        width = Mathf.Min(width, Screen.width - Margin * 2f);
        return new Rect((Screen.width - width) * 0.5f, Screen.height - BottomPromptY, width, height);
    }

    // Function: Calculates the centered system prompt rectangle.
    public static Rect SystemPromptRect(float width = 760f, float height = 92f)
    {
        width = Mathf.Min(width, Screen.width - Margin * 2f);
        return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
    }

    // Function: Calculates the bottom dialogue panel rectangle.
    public static Rect DialogueRect(float height = 220f)
    {
        float width = Mathf.Min(1120f, Screen.width - 120f);
        return new Rect((Screen.width - width) * 0.5f, Screen.height - height - 40f, width, height);
    }

    // Function: Calculates the stacked side-quest panel rectangle.
    public static Rect SideQuestRect(float width, float height, int stackIndex = 0)
    {
        return new Rect(Screen.width - width - Margin, Margin + stackIndex * (height + 12f), width, height);
    }

    // Function: Calculates the backpack panel rectangle.
    public static Rect BackpackRect(float width, float height)
    {
        return new Rect(Margin, Screen.height - height - Margin, width, height);
    }

    // Function: Draws a reusable dark UI panel background.
    public static void DrawPanel(Rect rect)
    {
        EnsurePanelTexture();
        GUI.DrawTexture(rect, panelTexture);
    }

    // Function: Creates or reuses a consistent label style.
    public static GUIStyle LabelStyle(ref GUIStyle style, int fontSize, TextAnchor alignment, FontStyle fontStyle = FontStyle.Normal, bool wordWrap = false)
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
        }

        style.fontSize = fontSize;
        style.alignment = alignment;
        style.fontStyle = fontStyle;
        style.wordWrap = wordWrap;
        style.normal.textColor = Color.white;
        return style;
    }

    // Function: Creates the shared panel texture if it does not already exist.
    private static void EnsurePanelTexture()
    {
        if (panelTexture != null)
        {
            return;
        }

        panelTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        panelTexture.SetPixel(0, 0, new Color(0.04f, 0.06f, 0.06f, 0.78f));
        panelTexture.Apply();
        panelTexture.hideFlags = HideFlags.HideAndDontSave;
    }
}
