using System.IO;
using UnityEngine;

public static class GameUiStyle
{
    public const float Margin = 24f;
    public const float BottomPromptY = 132f;
    public const float FontScale = 1.2f;
    public static readonly Vector2 UiReferenceResolution = new Vector2(1920f, 1080f);
    public static readonly Vector2 MainMenuButtonSize = new Vector2(450f, 82f);
    public static readonly Vector2 MainMenuStartButton = new Vector2(0f, 46f);
    public static readonly Vector2 MainMenuIntroButton = new Vector2(0f, -45f);
    public static readonly Vector2 MainMenuControlsButton = new Vector2(0f, -136f);
    public static readonly Vector2 MainMenuSettingsButton = new Vector2(0f, -227f);
    public static readonly Vector2 MainMenuCreditsButton = new Vector2(0f, -318f);
    public static readonly Vector2 MainMenuExitButton = new Vector2(0f, -409f);

    public const string BagImagePath = "new/bag.png";
    public const string MenuImagePath = "new/menu.png";
    public const string PanelImagePath = "new/panel.png";
    public const string DialoguePanelImagePath = "new/dialogue_panel.png";

    private static Texture2D panelTexture;
    private static Texture2D dialoguePanelTexture;
    private static Texture2D bagTexture;
    private static Texture2D menuTexture;

    public static Rect InteractionPromptRect(float width = 440f, float height = 60f)
    {
        width = Mathf.Max(width, 640f);
        height = Mathf.Max(height, 112f);
        width = Mathf.Min(width, Screen.width - Margin * 2f);
        height = Mathf.Min(height, Screen.height - Margin * 2f);
        return new Rect((Screen.width - width) * 0.5f, Screen.height - height - Margin * 2f, width, height);
    }

    public static Rect SystemPromptRect(float width = 760f, float height = 92f)
    {
        width = Mathf.Max(width, 920f);
        height = Mathf.Max(height, 156f);
        width = Mathf.Min(width, Screen.width - Margin * 2f);
        height = Mathf.Min(height, Screen.height - Margin * 2f);
        return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
    }

    public static Rect DialogueRect(float height = 220f)
    {
        float width = Mathf.Min(1280f, Screen.width - 96f);
        height = Mathf.Clamp(height * 1.45f, 300f, Screen.height - 80f);
        return new Rect((Screen.width - width) * 0.5f, Screen.height - height - 34f, width, height);
    }

    public static Rect SideQuestRect(float width, float height, int stackIndex = 0)
    {
        return new Rect(Screen.width - width - Margin, Margin + stackIndex * (height + 12f), width, height);
    }

    public static Rect BackpackRect(float width, float height)
    {
        return new Rect(Screen.width - width - Margin, Screen.height - height - Margin, width, height);
    }

    public static void DrawPanel(Rect rect)
    {
        GUI.DrawTexture(rect, GetPanelTexture(), ScaleMode.StretchToFill, true);
    }

    public static void DrawDialoguePanel(Rect rect)
    {
        GUI.DrawTexture(rect, GetDialoguePanelTexture(), ScaleMode.StretchToFill, true);
    }

    public static void DrawBag(Rect rect)
    {
        GUI.DrawTexture(rect, GetBagTexture(), ScaleMode.ScaleToFit, true);
    }

    public static void DrawMenuArt(Rect rect)
    {
        GUI.DrawTexture(rect, GetMenuTexture(), ScaleMode.StretchToFill, true);
    }

    public static GUIStyle LabelStyle(ref GUIStyle style, int fontSize, TextAnchor alignment, FontStyle fontStyle = FontStyle.Normal, bool wordWrap = false)
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
        }

        style.fontSize = ScaledFontSize(fontSize);
        style.alignment = alignment;
        style.fontStyle = fontStyle;
        style.wordWrap = wordWrap;
        style.normal.textColor = Color.white;
        return style;
    }

    public static GUIStyle ButtonStyle(ref GUIStyle style, int fontSize = 20)
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.button);
        }

        style.fontSize = ScaledFontSize(fontSize);
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.wordWrap = true;
        return style;
    }

    public static int ScaledFontSize(int fontSize)
    {
        return Mathf.RoundToInt(fontSize * FontScale);
    }

    public static Texture2D GetPanelTexture()
    {
        if (panelTexture == null)
        {
            panelTexture = LoadTexture(PanelImagePath, new Color(0.04f, 0.06f, 0.06f, 0.78f));
        }

        return panelTexture;
    }

    public static Texture2D GetDialoguePanelTexture()
    {
        if (dialoguePanelTexture == null)
        {
            dialoguePanelTexture = LoadTexture(DialoguePanelImagePath, new Color(0.04f, 0.06f, 0.06f, 0.86f));
        }

        return dialoguePanelTexture;
    }

    public static Texture2D GetBagTexture()
    {
        if (bagTexture == null)
        {
            bagTexture = LoadTexture(BagImagePath, new Color(0.04f, 0.06f, 0.06f, 0.78f));
        }

        return bagTexture;
    }

    public static Texture2D GetMenuTexture()
    {
        if (menuTexture == null)
        {
            menuTexture = LoadTexture(MenuImagePath, new Color(0.04f, 0.06f, 0.06f, 0.9f));
        }

        return menuTexture;
    }

    private static Texture2D LoadTexture(string relativePath, Color fallbackColor)
    {
        string path = Path.Combine(Application.dataPath, relativePath);
        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D loaded = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            if (loaded.LoadImage(bytes))
            {
                loaded.hideFlags = HideFlags.HideAndDontSave;
                return loaded;
            }
        }

        return CreateSolidTexture(fallbackColor);
    }

    private static Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }
}
