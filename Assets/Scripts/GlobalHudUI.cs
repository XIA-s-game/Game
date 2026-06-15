using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalHudUI : MonoBehaviour
{
    // Shared in-game Esc menu drawn in gameplay scenes.
    [SerializeField] private string mainMenuSceneName = "Mainmenu";
    [SerializeField] private string hintText = "Press Esc";
    [SerializeField] private bool lockCursorWhenClosed = true;

    [Header("Global Dialogue Layout")]
    [SerializeField] private float margin = 24f;
    [SerializeField] private float bottomPromptY = 132f;
    [SerializeField] private float fontScale = 1.2f;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private Vector2 interactionPromptMinSize = new Vector2(640f, 112f);
    [SerializeField] private Vector2 systemPromptMinSize = new Vector2(920f, 156f);
    [SerializeField] private float dialogueMaxWidth = 1280f;
    [SerializeField] private float dialogueHorizontalPadding = 96f;
    [SerializeField] private float dialogueHeightScale = 1.45f;
    [SerializeField] private float dialogueMinHeight = 300f;
    [SerializeField] private float dialogueBottomOffset = 34f;
    [SerializeField] private float dialogueVerticalPadding = 80f;
    [SerializeField] private float sideQuestStackGap = 12f;

    [Header("Top Left Hint")]
    [SerializeField] private Rect hintRect = new Rect(20f, 16f, 190f, 48f);
    [SerializeField] private UiPadding hintTextPadding;
    [SerializeField] private int hintFontSize = 15;

    [Header("Pause Menu")]
    [SerializeField] private Vector2 menuPanelSize = new Vector2(520f, 390f);
    [SerializeField] private UiPadding menuTitlePadding;
    [SerializeField] private UiPadding menuButtonPadding;
    [SerializeField] private float menuButtonHeight = 54f;
    [SerializeField] private float menuButtonGap = 14f;
    [SerializeField] private int menuTitleFontSize = 22;
    [SerializeField] private int menuButtonFontSize = 18;

    [Header("Settings Panel")]
    [SerializeField] private Vector2 settingsPanelReferenceSize = new Vector2(900f, 620f);
    [SerializeField] private UiPadding settingsContentPadding;
    [SerializeField] private float settingsVolumeLabelY = 156f;
    [SerializeField] private float settingsSliderY = 224f;
    [SerializeField] private float settingsMuteY = 306f;
    [SerializeField] private int settingsTitleFontSize = 24;
    [SerializeField] private int settingsLabelFontSize = 20;

    [Header("Controls Panel")]
    [SerializeField] private Vector2 controlsPanelReferenceSize = new Vector2(1688f, 1080f);
    [SerializeField] private Vector2 controlsImageReferenceSize = new Vector2(1380f, 780f);
    [SerializeField] private Vector2 controlsImageOffset = new Vector2(0f, -10f);
    [SerializeField] private string controlsImagePath = "new/control.png";
    [SerializeField] private Texture2D controlsPanelTexture;

    [Header("Back Button")]
    [SerializeField] private Vector2 backButtonSize = new Vector2(150f, 58f);
    [SerializeField] private float backButtonBottomOffset = 110f;
    [SerializeField] private int toggleFontSize = 20;

    private GUIStyle hintStyle;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private GUIStyle toggleStyle;
    private Texture2D loadedControlsTexture;
    private bool menuOpen;
    private bool settingsOpen;
    private bool controlsOpen;
    private bool cursorWasLocked;
    private bool cursorWasVisible;
    private float volumeValue = 1f;
    private bool muted;

    private void OnValidate()
    {
        FillMissingInspectorDefaults();
        ApplyGlobalUiLayout();
    }

    private void Awake()
    {
        FillMissingInspectorDefaults();
        ApplyGlobalUiLayout();
    }

    private void Update()
    {
        // Esc toggles the menu and releases the cursor while the menu is open.
        if (IsMenuScene())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetMenuOpen(!menuOpen);
            settingsOpen = false;
            controlsOpen = false;
            volumeValue = GameAudioSettings.MasterVolume;
            muted = GameAudioSettings.Muted;
        }
    }

    private void LateUpdate()
    {
        if (!menuOpen)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AquariusMax.Fae.demo.DemoCharacter.SetControlLocked(true);
    }

    private void OnDisable()
    {
        if (menuOpen)
        {
            RestoreCursorState();
        }
    }

    private void OnGUI()
    {
        FillMissingInspectorDefaults();
        ApplyGlobalUiLayout();
        if (IsMenuScene())
        {
            return;
        }

        GameUiStyle.DrawPanel(hintRect);
        GUI.Label(ApplyPadding(hintRect, hintTextPadding),
            hintText,
            GameUiStyle.LabelStyle(ref hintStyle, hintFontSize, TextAnchor.MiddleCenter, FontStyle.Bold));

        if (!menuOpen)
        {
            return;
        }

        DrawMenu();
    }

    private bool IsMenuScene()
    {
        return string.Equals(SceneManager.GetActiveScene().name, mainMenuSceneName, System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(SceneManager.GetActiveScene().name, "MainMenu", System.StringComparison.OrdinalIgnoreCase);
    }

    private void DrawMenu()
    {
        // Main pause panel can save/exit, open settings, open controls, or resume.
        if (settingsOpen)
        {
            DrawSettings(MainMenuPanelRect(settingsPanelReferenceSize.x, settingsPanelReferenceSize.y));
            return;
        }

        if (controlsOpen)
        {
            DrawControls(MainMenuPanelRect(controlsPanelReferenceSize.x, controlsPanelReferenceSize.y));
            return;
        }

        Rect rect = new Rect((Screen.width - menuPanelSize.x) * 0.5f, (Screen.height - menuPanelSize.y) * 0.5f, menuPanelSize.x, menuPanelSize.y);
        GameUiStyle.DrawPanel(rect);
        Rect titleRect = menuTitlePadding.Apply(rect);
        GUI.Label(new Rect(titleRect.x, titleRect.y, titleRect.width, 34f), "Game Menu", GameUiStyle.LabelStyle(ref titleStyle, menuTitleFontSize, TextAnchor.MiddleCenter, FontStyle.Bold));

        Rect buttonArea = menuButtonPadding.Apply(rect);
        float buttonY = buttonArea.y;
        float buttonWidth = buttonArea.width;
        if (GUI.Button(new Rect(buttonArea.x, buttonY, buttonWidth, menuButtonHeight), "Save and Exit to Menu", GameUiStyle.ButtonStyle(ref buttonStyle, menuButtonFontSize)))
        {
            GameSaveManager.SaveCurrentGame();
            GlobalBackpackUI.DisableForGameSession();
            MainMenuController.SkipCoverOnNextMenuLoad();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        buttonY += menuButtonHeight + menuButtonGap;
        if (GUI.Button(new Rect(buttonArea.x, buttonY, buttonWidth, menuButtonHeight), "Settings", GameUiStyle.ButtonStyle(ref buttonStyle, menuButtonFontSize)))
        {
            settingsOpen = true;
            volumeValue = GameAudioSettings.MasterVolume;
            muted = GameAudioSettings.Muted;
        }

        buttonY += menuButtonHeight + menuButtonGap;
        if (GUI.Button(new Rect(buttonArea.x, buttonY, buttonWidth, menuButtonHeight), "Controls", GameUiStyle.ButtonStyle(ref buttonStyle, menuButtonFontSize)))
        {
            controlsOpen = true;
        }

        buttonY += menuButtonHeight + menuButtonGap;
        if (GUI.Button(new Rect(buttonArea.x, buttonY, buttonWidth, menuButtonHeight), "Resume", GameUiStyle.ButtonStyle(ref buttonStyle, menuButtonFontSize)))
        {
            SetMenuOpen(false);
        }
    }

    private void DrawSettings(Rect rect)
    {
        // Settings mirrors the main menu audio controls.
        GameUiStyle.DrawPanel(rect);
        Rect settingsArea = settingsContentPadding.Apply(rect);
        GUI.Label(new Rect(settingsArea.x, settingsArea.y, settingsArea.width, 44f), "Settings", GameUiStyle.LabelStyle(ref titleStyle, settingsTitleFontSize, TextAnchor.MiddleCenter, FontStyle.Bold));

        float contentX = settingsArea.x;
        float contentWidth = settingsArea.width;
        GUI.Label(new Rect(contentX, rect.y + settingsVolumeLabelY, contentWidth, 40f), "Volume", GameUiStyle.LabelStyle(ref labelStyle, settingsLabelFontSize, TextAnchor.MiddleLeft, FontStyle.Bold));
        volumeValue = GUI.HorizontalSlider(new Rect(contentX, rect.y + settingsSliderY, contentWidth, 36f), volumeValue, 0f, 1f);

        muted = GUI.Toggle(new Rect(contentX, rect.y + settingsMuteY, 220f, 44f), muted, "Mute", ToggleStyle());
        GameAudioSettings.SetAudioSettings(volumeValue, muted);

        if (GUI.Button(BackButtonRect(rect), "Back", GameUiStyle.ButtonStyle(ref buttonStyle, 18)))
        {
            settingsOpen = false;
        }
    }

    private void DrawControls(Rect rect)
    {
        // Controls uses the same image artwork as the main menu controls panel.
        GameUiStyle.DrawPanel(rect);
        Texture2D texture = GetControlsTexture();
        if (texture != null)
        {
            GUI.DrawTexture(ControlsImageRect(rect), texture, ScaleMode.StretchToFill, true);
        }

        if (GUI.Button(BackButtonRect(rect), "Back", GameUiStyle.ButtonStyle(ref buttonStyle, 18)))
        {
            controlsOpen = false;
        }
    }

    private static Rect MainMenuPanelRect(float referenceWidth, float referenceHeight)
    {
        float scale = Mathf.Min(Screen.width / GameUiStyle.UiReferenceResolution.x, Screen.height / GameUiStyle.UiReferenceResolution.y);
        scale = Mathf.Min(scale, 1f);
        float width = Mathf.Min(referenceWidth * scale, Screen.width - GameUiStyle.Margin * 2f);
        float height = Mathf.Min(referenceHeight * scale, Screen.height - GameUiStyle.Margin * 2f);
        return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
    }

    private Rect BackButtonRect(Rect panelRect)
    {
        float width = Mathf.Min(backButtonSize.x, panelRect.width - 148f);
        float height = backButtonSize.y;
        return new Rect(panelRect.center.x - width * 0.5f, panelRect.yMax - backButtonBottomOffset - height, width, height);
    }

    private Rect ControlsImageRect(Rect panelRect)
    {
        float scale = Mathf.Min(panelRect.width / controlsPanelReferenceSize.x, panelRect.height / controlsPanelReferenceSize.y);
        float width = controlsImageReferenceSize.x * scale;
        float height = controlsImageReferenceSize.y * scale;
        return new Rect(
            panelRect.center.x - width * 0.5f + controlsImageOffset.x * scale,
            panelRect.center.y - height * 0.5f - controlsImageOffset.y * scale,
            width,
            height);
    }

    private GUIStyle ToggleStyle()
    {
        if (toggleStyle == null)
        {
            toggleStyle = new GUIStyle(GUI.skin.toggle);
        }

        toggleStyle.fontSize = GameUiStyle.ScaledFontSize(toggleFontSize);
        toggleStyle.fontStyle = FontStyle.Bold;
        toggleStyle.normal.textColor = Color.white;
        toggleStyle.onNormal.textColor = Color.white;
        toggleStyle.hover.textColor = Color.white;
        toggleStyle.onHover.textColor = Color.white;
        return toggleStyle;
    }

    private static Rect ApplyPadding(Rect rect, UiPadding padding)
    {
        return padding.Apply(rect);
    }

    private void FillMissingInspectorDefaults()
    {
        if (margin <= 0f)
        {
            margin = 24f;
        }

        if (bottomPromptY <= 0f)
        {
            bottomPromptY = 132f;
        }

        if (fontScale <= 0f)
        {
            fontScale = 1.2f;
        }

        if (referenceResolution == Vector2.zero)
        {
            referenceResolution = new Vector2(1920f, 1080f);
        }

        if (interactionPromptMinSize == Vector2.zero)
        {
            interactionPromptMinSize = new Vector2(640f, 112f);
        }

        if (systemPromptMinSize == Vector2.zero)
        {
            systemPromptMinSize = new Vector2(920f, 156f);
        }

        if (dialogueMaxWidth <= 0f)
        {
            dialogueMaxWidth = 1280f;
        }

        if (dialogueHorizontalPadding <= 0f)
        {
            dialogueHorizontalPadding = 96f;
        }

        if (dialogueHeightScale <= 0f)
        {
            dialogueHeightScale = 1.45f;
        }

        if (dialogueMinHeight <= 0f)
        {
            dialogueMinHeight = 300f;
        }

        if (dialogueVerticalPadding <= 0f)
        {
            dialogueVerticalPadding = 80f;
        }

        if (sideQuestStackGap <= 0f)
        {
            sideQuestStackGap = 12f;
        }

        if (hintRect.width <= 0f || hintRect.height <= 0f)
        {
            hintRect = new Rect(20f, 16f, 190f, 48f);
        }

        if (hintTextPadding.IsZero)
        {
            hintTextPadding = UiPadding.Create(16f, 32f, 5f, 10f);
        }

        if (hintFontSize <= 0)
        {
            hintFontSize = 15;
        }

        if (menuPanelSize == Vector2.zero)
        {
            menuPanelSize = new Vector2(520f, 390f);
        }

        if (menuTitlePadding.IsZero)
        {
            menuTitlePadding = UiPadding.Create(24f, 48f, 18f, 0f);
        }

        if (menuButtonPadding.IsZero)
        {
            menuButtonPadding = UiPadding.Create(66f, 132f, 76f, 0f);
        }

        if (menuButtonHeight <= 0f)
        {
            menuButtonHeight = 54f;
        }

        if (menuButtonGap <= 0f)
        {
            menuButtonGap = 14f;
        }

        if (menuTitleFontSize <= 0)
        {
            menuTitleFontSize = 22;
        }

        if (menuButtonFontSize <= 0)
        {
            menuButtonFontSize = 18;
        }

        if (settingsPanelReferenceSize == Vector2.zero)
        {
            settingsPanelReferenceSize = new Vector2(900f, 620f);
        }

        if (settingsContentPadding.IsZero)
        {
            settingsContentPadding = UiPadding.Create(74f, 74f, 60f, 0f);
        }

        if (settingsVolumeLabelY <= 0f)
        {
            settingsVolumeLabelY = 156f;
        }

        if (settingsSliderY <= 0f)
        {
            settingsSliderY = 224f;
        }

        if (settingsMuteY <= 0f)
        {
            settingsMuteY = 306f;
        }

        if (settingsTitleFontSize <= 0)
        {
            settingsTitleFontSize = 24;
        }

        if (settingsLabelFontSize <= 0)
        {
            settingsLabelFontSize = 20;
        }

        if (controlsPanelReferenceSize == Vector2.zero)
        {
            controlsPanelReferenceSize = new Vector2(1688f, 1080f);
        }

        if (controlsImageReferenceSize == Vector2.zero)
        {
            controlsImageReferenceSize = new Vector2(1380f, 780f);
        }

        if (string.IsNullOrWhiteSpace(controlsImagePath))
        {
            controlsImagePath = "new/control.png";
        }

        if (backButtonSize == Vector2.zero)
        {
            backButtonSize = new Vector2(150f, 58f);
        }

        if (backButtonBottomOffset <= 0f)
        {
            backButtonBottomOffset = 110f;
        }

        if (toggleFontSize <= 0)
        {
            toggleFontSize = 20;
        }
    }

    private void ApplyGlobalUiLayout()
    {
        GameUiStyle.SetLayout(
            margin,
            bottomPromptY,
            fontScale,
            referenceResolution,
            interactionPromptMinSize,
            systemPromptMinSize,
            dialogueMaxWidth,
            dialogueHorizontalPadding,
            dialogueHeightScale,
            dialogueMinHeight,
            dialogueBottomOffset,
            dialogueVerticalPadding,
            sideQuestStackGap);
    }

    private void SetMenuOpen(bool open)
    {
        // Locks player input while the cursor is free for menu buttons.
        if (menuOpen == open)
        {
            return;
        }

        menuOpen = open;
        if (menuOpen)
        {
            AquariusMax.Fae.demo.DemoCharacter.SetControlLocked(true);
            cursorWasLocked = Cursor.lockState != CursorLockMode.None;
            cursorWasVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            RestoreCursorState();
        }
    }

    private void RestoreCursorState()
    {
        AquariusMax.Fae.demo.DemoCharacter.SetControlLocked(false);
        if (lockCursorWhenClosed && cursorWasLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }

        Cursor.lockState = CursorLockMode.None;
            Cursor.visible = cursorWasVisible;
    }

    private Texture2D GetControlsTexture()
    {
        if (controlsPanelTexture != null)
        {
            return controlsPanelTexture;
        }

        if (loadedControlsTexture != null)
        {
            return loadedControlsTexture;
        }

        string path = Path.Combine(Application.dataPath, controlsImagePath);
        if (!File.Exists(path))
        {
            Debug.LogWarning("GlobalHudUI could not load controls image: " + controlsImagePath, this);
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        loadedControlsTexture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        if (!loadedControlsTexture.LoadImage(bytes))
        {
            Destroy(loadedControlsTexture);
            loadedControlsTexture = null;
        }

        return loadedControlsTexture;
    }
}
