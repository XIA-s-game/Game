using System.IO;
using UnityEngine;

public static class GameUiStyle
{
    // Built with AI assistance to keep shared menu layout consistent across scenes.
    // Shared outer margin for generated UI.
    public static float Margin { get; private set; } = 24f;
    // Default bottom prompt baseline.
    public static float BottomPromptY { get; private set; } = 132f;
    // Global text scale used by helper styles.
    public static float FontScale { get; private set; } = 1.2f;
    // Reference screen size for layout values.
    public static Vector2 UiReferenceResolution { get; private set; } = new Vector2(1920f, 1080f);
    // Shared main menu button size.
    public static Vector2 MainMenuButtonSize { get; private set; } = new Vector2(450f, 82f);
    // Main menu start button offset.
    public static Vector2 MainMenuStartButton { get; private set; } = new Vector2(0f, 46f);
    // Main menu intro button offset.
    public static Vector2 MainMenuIntroButton { get; private set; } = new Vector2(0f, -45f);
    // Main menu controls button offset.
    public static Vector2 MainMenuControlsButton { get; private set; } = new Vector2(0f, -136f);
    // Main menu settings button offset.
    public static Vector2 MainMenuSettingsButton { get; private set; } = new Vector2(0f, -227f);
    // Main menu credits button offset.
    public static Vector2 MainMenuCreditsButton { get; private set; } = new Vector2(0f, -318f);
    // Main menu exit button offset.
    public static Vector2 MainMenuExitButton { get; private set; } = new Vector2(0f, -409f);

    // Minimum interaction prompt size.
    private static Vector2 interactionPromptMinSize = new Vector2(640f, 112f);
    // Minimum system prompt size.
    private static Vector2 systemPromptMinSize = new Vector2(920f, 156f);
    // Largest width used by dialogue panels.
    private static float dialogueMaxWidth = 1280f;
    // Horizontal space reserved around dialogue panels.
    private static float dialogueHorizontalPadding = 96f;
    // Multiplier applied to requested dialogue height.
    private static float dialogueHeightScale = 1.45f;
    // Smallest allowed dialogue height.
    private static float dialogueMinHeight = 300f;
    // Distance from bottom screen edge to dialogue panels.
    private static float dialogueBottomOffset = 34f;
    // Vertical space reserved around dialogue panels.
    private static float dialogueVerticalPadding = 80f;
    // Gap for right-side stacked quest panels.
    private static float sideQuestStackGap = 12f;

    // Standard panel artwork.
    private static Texture2D panelTexture;
    // Dialogue panel artwork.
    private static Texture2D dialoguePanelTexture;
    // Backpack icon artwork.
    private static Texture2D bagTexture;
    // Main menu panel artwork.
    private static Texture2D menuTexture;

    // Fallback Resources path for standard panel art.
    private const string PanelTexturePath = "new/panel.png";
    // Fallback Resources path for dialogue art.
    private const string DialoguePanelTexturePath = "new/dialogue_panel.png";
    // Fallback Resources path for bag art.
    private const string BagTexturePath = "new/bag.png";
    // Fallback Resources path for menu art.
    private const string MenuTexturePath = "new/menu.png";

    public static void SetLayout(
        float margin,
        float bottomPromptY,
        float fontScale,
        Vector2 referenceResolution,
        Vector2 interactionPromptMin,
        Vector2 systemPromptMin,
        float dialogueWidthMax,
        float dialoguePaddingX,
        float dialogueScale,
        float dialogueHeightMin,
        float dialogueBottom,
        float dialoguePaddingY,
        float sideQuestGap)
    {
        Margin = Mathf.Max(0f, margin);
        BottomPromptY = bottomPromptY;
        FontScale = Mathf.Max(0.1f, fontScale);
        UiReferenceResolution = new Vector2(Mathf.Max(1f, referenceResolution.x), Mathf.Max(1f, referenceResolution.y));
        interactionPromptMinSize = MaxVector(interactionPromptMin, Vector2.one);
        systemPromptMinSize = MaxVector(systemPromptMin, Vector2.one);
        dialogueMaxWidth = Mathf.Max(1f, dialogueWidthMax);
        dialogueHorizontalPadding = Mathf.Max(0f, dialoguePaddingX);
        dialogueHeightScale = Mathf.Max(0.1f, dialogueScale);
        dialogueMinHeight = Mathf.Max(1f, dialogueHeightMin);
        dialogueBottomOffset = dialogueBottom;
        dialogueVerticalPadding = Mathf.Max(0f, dialoguePaddingY);
        sideQuestStackGap = Mathf.Max(0f, sideQuestGap);
    }

    public static void SetMainMenuButtons(
        Vector2 buttonSize,
        Vector2 startButton,
        Vector2 introButton,
        Vector2 controlsButton,
        Vector2 settingsButton,
        Vector2 creditsButton,
        Vector2 exitButton)
    {
        MainMenuButtonSize = MaxVector(buttonSize, Vector2.one);
        MainMenuStartButton = startButton;
        MainMenuIntroButton = introButton;
        MainMenuControlsButton = controlsButton;
        MainMenuSettingsButton = settingsButton;
        MainMenuCreditsButton = creditsButton;
        MainMenuExitButton = exitButton;
    }

    public static void SetTextures(Texture2D panel, Texture2D dialoguePanel, Texture2D bag, Texture2D menu)
    {
        // Main menu can pass loaded textures here so gameplay UI uses the same artwork.
        panelTexture = panel;
        dialoguePanelTexture = dialoguePanel;
        bagTexture = bag;
        menuTexture = menu;
    }

    public static Rect InteractionPromptRect(float width = 440f, float height = 60f)
    {
        width = Mathf.Max(width, interactionPromptMinSize.x);
        height = Mathf.Max(height, interactionPromptMinSize.y);
        width = Mathf.Min(width, Screen.width - Margin * 2f);
        height = Mathf.Min(height, Screen.height - Margin * 2f);
        return new Rect((Screen.width - width) * 0.5f, Screen.height - height - Margin * 2f, width, height);
    }

    public static Rect SystemPromptRect(float width = 760f, float height = 92f)
    {
        width = Mathf.Max(width, systemPromptMinSize.x);
        height = Mathf.Max(height, systemPromptMinSize.y);
        width = Mathf.Min(width, Screen.width - Margin * 2f);
        height = Mathf.Min(height, Screen.height - Margin * 2f);
        return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
    }

    public static Rect DialogueRect(float height = 220f)
    {
        // Shared bottom dialogue panel used by side quests and main story conversations.
        float width = Mathf.Min(dialogueMaxWidth, Screen.width - dialogueHorizontalPadding);
        height = Mathf.Clamp(height * dialogueHeightScale, dialogueMinHeight, Screen.height - dialogueVerticalPadding);
        return new Rect((Screen.width - width) * 0.5f, Screen.height - height - dialogueBottomOffset, width, height);
    }

    public static Rect SideQuestRect(float width, float height, int stackIndex = 0)
    {
        // Right-side stacked panel slot for quest trackers.
        return new Rect(Screen.width - width - Margin, Margin + stackIndex * (height + sideQuestStackGap), width, height);
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
            panelTexture = LoadTexture(PanelTexturePath, new Color(0.04f, 0.06f, 0.06f, 0.78f));
        }

        return panelTexture;
    }

    public static Texture2D GetDialoguePanelTexture()
    {
        if (dialoguePanelTexture == null)
        {
            dialoguePanelTexture = LoadTexture(DialoguePanelTexturePath, new Color(0.04f, 0.06f, 0.06f, 0.86f));
        }

        return dialoguePanelTexture;
    }

    public static Texture2D GetBagTexture()
    {
        if (bagTexture == null)
        {
            bagTexture = LoadTexture(BagTexturePath, new Color(0.04f, 0.06f, 0.06f, 0.78f));
        }

        return bagTexture;
    }

    public static Texture2D GetMenuTexture()
    {
        if (menuTexture == null)
        {
            menuTexture = LoadTexture(MenuTexturePath, new Color(0.04f, 0.06f, 0.06f, 0.9f));
        }

        return menuTexture;
    }

    private static Texture2D LoadTexture(string relativeAssetPath, Color fallbackColor)
    {
        // Resources are included in player builds; direct file paths are only an editor fallback.
        Texture2D resourceTexture = LoadResourceTexture(relativeAssetPath);
        if (resourceTexture != null)
        {
            return resourceTexture;
        }

        string path = Path.Combine(Application.dataPath, relativeAssetPath);
        if (!File.Exists(path))
        {
            return CreateSolidTexture(fallbackColor);
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        if (!texture.LoadImage(bytes))
        {
            Object.Destroy(texture);
            return CreateSolidTexture(fallbackColor);
        }

        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private static Texture2D LoadResourceTexture(string relativeAssetPath)
    {
        if (string.IsNullOrWhiteSpace(relativeAssetPath))
        {
            return null;
        }

        string resourcePath = Path.ChangeExtension(relativeAssetPath.Replace('\\', '/'), null);
        return Resources.Load<Texture2D>(resourcePath);
    }

    private static Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private static Vector2 MaxVector(Vector2 value, Vector2 min)
    {
        return new Vector2(Mathf.Max(value.x, min.x), Mathf.Max(value.y, min.y));
    }
}
