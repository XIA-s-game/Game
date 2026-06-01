// Shared IMGUI styles so the older UI screens look consistent.
using UnityEngine;

public static class GameUiStyle
{
    public const float Margin = 24f;
    public const float BottomPromptY = 132f;

    private static Texture2D panelTexture;

    public static Rect InteractionPromptRect(float width = 440f, float height = 60f)
    {
        width = Mathf.Min(width, Screen.width - Margin * 2f);
        return new Rect((Screen.width - width) * 0.5f, Screen.height - BottomPromptY, width, height);
    }

    public static Rect SystemPromptRect(float width = 760f, float height = 92f)
    {
        width = Mathf.Min(width, Screen.width - Margin * 2f);
        return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
    }

    public static Rect DialogueRect(float height = 220f)
    {
        float width = Mathf.Min(1120f, Screen.width - 120f);
        return new Rect((Screen.width - width) * 0.5f, Screen.height - height - 40f, width, height);
    }

    public static Rect SideQuestRect(float width, float height, int stackIndex = 0)
    {
        return new Rect(Screen.width - width - Margin, Margin + stackIndex * (height + 12f), width, height);
    }

    public static Rect BackpackRect(float width, float height)
    {
        return new Rect(Margin, Screen.height - height - Margin, width, height);
    }

    public static void DrawPanel(Rect rect)
    {
        EnsurePanelTexture();
        GUI.DrawTexture(rect, panelTexture);
    }

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
