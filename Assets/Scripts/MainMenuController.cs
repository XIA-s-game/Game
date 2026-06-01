// Main function: Manages the main menu scene, including buttons, intro and settings panels, default UI creation, and starting or exiting the game.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string menuSceneName = "Mainmenu";
    [SerializeField] private string gameSceneName = "Enchanted Forest A";

    [Header("Opening Video")]
    [SerializeField] private VideoClip openingVideo;
    [SerializeField] private string openingVideoPath = "Assets/Audio/openingvideo.mp4";

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Main Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button introButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Back Buttons")]
    [SerializeField] private Button introBackButton;
    [SerializeField] private Button controlsBackButton;
    [SerializeField] private Button settingsBackButton;

    [Header("Panel Text")]
    [TextArea(3, 8)]
    [SerializeField] private string introText =
        "Welcome to Magic Forest. Explore, solve puzzles, help characters, and uncover the story."; 

    [TextArea(3, 8)]
    [SerializeField] private string controlsText =
        "WASD: Move\nMouse: Look\nSpace: Jump\nE: Interact\nEsc: Back or close"; 

    private bool openingVideoStarted;
    private GameObject openingVideoRoot;
    private RenderTexture openingVideoTexture;

    // Function: Registers the scene-loaded callback for automatic menu controller setup.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneLoaded()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureControllerForActiveMenuScene();
    }

    // Function: Checks newly loaded scenes and creates a menu controller when needed.
    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureControllerForActiveMenuScene();
    }

    // Function: Ensures the active menu scene has exactly the controller it needs.
    private static void EnsureControllerForActiveMenuScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!IsMenuScene(activeScene.name))
        {
            return;
        }

        if (FindObjectOfType<MainMenuController>() != null)
        {
            return;
        }

        GameObject host = new GameObject("MainMenuController");
        host.AddComponent<MainMenuController>();
    }

    // Function: Initializes component references, cached state, and default runtime data.
    private void Awake()
    {
        if (!IsConfiguredMenuScene(SceneManager.GetActiveScene().name))
        {
            enabled = false;
            return;
        }

        if (mainMenuPanel == null)
        {
            BuildDefaultMenuUi();
        }

        LoadDefaultAssetsInEditor();
        BindButtons();
        ShowMainMenu();
    }

    // Function: Updates input handling, interaction checks, and active gameplay flow each frame.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowMainMenu();
        }
    }

    // Function: Starts the game flow.
    public void StartGame()
    {
        if (openingVideoStarted)
        {
            return;
        }

        openingVideoStarted = true;
        PlayOpeningVideo();
    }

    // Function: Shows intro.
    public void ShowIntro()
    {
        ShowPanel(introPanel);
    }

    // Function: Shows controls.
    public void ShowControls()
    {
        ShowPanel(controlsPanel);
    }

    // Function: Shows settings.
    public void ShowSettings()
    {
        ShowPanel(settingsPanel);
    }

    // Function: Shows main menu.
    public void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(introPanel, false);
        SetPanelActive(controlsPanel, false);
        SetPanelActive(settingsPanel, false);
    }

    // Function: Exits game and restores exploration state.
    public void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Function: Checks whether menu scene is true.
    private static bool IsMenuScene(string sceneName)
    {
        return string.Equals(sceneName, "Mainmenu", System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(sceneName, "MainMenu", System.StringComparison.OrdinalIgnoreCase);
    }

    // Function: Checks whether configured menu scene is true.
    private bool IsConfiguredMenuScene(string sceneName)
    {
        return string.Equals(sceneName, menuSceneName, System.StringComparison.OrdinalIgnoreCase) || IsMenuScene(sceneName);
    }

    // Function: Shows panel.
    private void ShowPanel(GameObject panel)
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(introPanel, panel == introPanel);
        SetPanelActive(controlsPanel, panel == controlsPanel);
        SetPanelActive(settingsPanel, panel == settingsPanel);
    }

    // Function: Sets panel active.
    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    // Function: Runs the bind buttons logic.
    private void BindButtons()
    {
        BindButton(startGameButton, StartGame);
        BindButton(introButton, ShowIntro);
        BindButton(controlsButton, ShowControls);
        BindButton(settingsButton, ShowSettings);
        BindButton(exitButton, ExitGame);
        BindButton(introBackButton, ShowMainMenu);
        BindButton(controlsBackButton, ShowMainMenu);
        BindButton(settingsBackButton, ShowMainMenu);
    }

    // Function: Runs the bind button logic.
    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    // Function: Builds the data or scene objects needed for default menu UI.
    private void BuildDefaultMenuUi()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("MainMenuCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        CreateBackground(canvasObject.transform);
        mainMenuPanel = CreateMainMenuPanel(canvasObject.transform);
        introPanel = CreateInfoPanel(canvasObject.transform, "IntroPanel", "Game Intro", introText, out introBackButton);
        controlsPanel = CreateInfoPanel(canvasObject.transform, "ControlsPanel", "Controls", controlsText, out controlsBackButton);
        settingsPanel = CreateInfoPanel(canvasObject.transform, "SettingsPanel", "Settings", "Settings are not available yet.", out settingsBackButton);
    }

    private void PlayOpeningVideo()
    {
        GameAudioController audioController = FindObjectOfType<GameAudioController>();
        if (audioController != null)
        {
            audioController.StopMusic();
        }

        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(introPanel, false);
        SetPanelActive(controlsPanel, false);
        SetPanelActive(settingsPanel, false);

        if (openingVideo == null)
        {
            LoadGameScene();
            return;
        }

        openingVideoRoot = new GameObject("OpeningVideoPlayer");
        openingVideoRoot.transform.SetParent(transform, false);

        Canvas canvas = openingVideoRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        CanvasScaler scaler = openingVideoRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject("VideoImage");
        imageObject.transform.SetParent(openingVideoRoot.transform, false);
        RawImage image = imageObject.AddComponent<RawImage>();
        image.color = Color.black;
        RectTransform imageRect = image.rectTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        openingVideoTexture = new RenderTexture(1920, 1080, 0);
        openingVideoTexture.Create();
        image.texture = openingVideoTexture;

        VideoPlayer videoPlayer = openingVideoRoot.AddComponent<VideoPlayer>();
        AudioSource audioSource = openingVideoRoot.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = openingVideoTexture;
        videoPlayer.clip = openingVideo;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
        videoPlayer.prepareCompleted += player => player.Play();
        videoPlayer.loopPointReached += _ => LoadGameScene();
        videoPlayer.errorReceived += (_, message) =>
        {
            Debug.LogWarning("Opening video failed: " + message);
            LoadGameScene();
        };
        videoPlayer.Prepare();
    }

    private void LoadGameScene()
    {
        if (openingVideoTexture != null)
        {
            openingVideoTexture.Release();
            openingVideoTexture = null;
        }

        if (openingVideoRoot != null)
        {
            Destroy(openingVideoRoot);
            openingVideoRoot = null;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    private void LoadDefaultAssetsInEditor()
    {
#if UNITY_EDITOR
        if (openingVideo == null && !string.IsNullOrEmpty(openingVideoPath))
        {
            openingVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(openingVideoPath);
        }
#endif
    }

    // Function: Creates the objects, textures, or UI needed for main menu panel.
    private GameObject CreateMainMenuPanel(Transform parent)
    {
        GameObject panel = CreatePanelRoot("MainMenuPanel", parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(560f, 760f);
        rect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 46, 46);
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text title = CreateText("Title", panel.transform, "Magic Forest", 58, TextAnchor.MiddleCenter);
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 120f;

        startGameButton = CreateButton("StartGame", panel.transform, "Start Game");
        introButton = CreateButton("Intro", panel.transform, "Game Intro");
        controlsButton = CreateButton("Controls", panel.transform, "Controls");
        settingsButton = CreateButton("Settings", panel.transform, "Settings");
        exitButton = CreateButton("Exit", panel.transform, "Exit Game");

        return panel;
    }

    // Function: Creates the objects, textures, or UI needed for info panel.
    private GameObject CreateInfoPanel(Transform parent, string objectName, string title, string body, out Button backButton)
    {
        GameObject panel = CreatePanelRoot(objectName, parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(900f, 620f);
        rect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(58, 58, 46, 46);
        layout.spacing = 28f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text titleText = CreateText("Title", panel.transform, title, 44, TextAnchor.MiddleCenter);
        LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 70f;

        Text bodyText = CreateText("Body", panel.transform, body, 30, TextAnchor.UpperLeft);
        LayoutElement bodyLayout = bodyText.gameObject.AddComponent<LayoutElement>();
        bodyLayout.preferredHeight = 340f;

        backButton = CreateButton("Back", panel.transform, "Back");
        return panel;
    }

    // Function: Creates the objects, textures, or UI needed for panel root.
    private static GameObject CreatePanelRoot(string objectName, Transform parent)
    {
        GameObject panel = new GameObject(objectName);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.06f, 0.08f, 0.09f, 0.88f);

        return panel;
    }

    // Function: Creates the objects, textures, or UI needed for background.
    private static void CreateBackground(Transform parent)
    {
        GameObject background = new GameObject("Background");
        background.transform.SetParent(parent, false);

        Image image = background.AddComponent<Image>();
        image.color = new Color(0.03f, 0.10f, 0.08f, 1f);

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Function: Creates the objects, textures, or UI needed for button.
    private static Button CreateButton(string objectName, Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.82f, 0.68f, 0.36f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.82f, 0.68f, 0.36f, 1f);
        colors.highlightedColor = new Color(0.96f, 0.82f, 0.48f, 1f);
        colors.pressedColor = new Color(0.62f, 0.48f, 0.22f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 78f;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;

        Text text = CreateText("Text", buttonObject.transform, label, 30, TextAnchor.MiddleCenter);
        text.color = new Color(0.08f, 0.07f, 0.05f, 1f);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    // Function: Creates the objects, textures, or UI needed for text.
    private static Text CreateText(string objectName, Transform parent, string value, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;

        RectTransform rect = text.rectTransform;
        rect.localScale = Vector3.one;

        return text;
    }

    // Function: Gets or calculates default font.
    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    // Function: Ensures event system exists, is configured, or is ready to use.
    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
