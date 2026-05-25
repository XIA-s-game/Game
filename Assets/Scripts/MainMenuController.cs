using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string menuSceneName = "Mainmenu";
    [SerializeField] private string gameSceneName = "Enchanted Forest A";

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneLoaded()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureControllerForActiveMenuScene();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureControllerForActiveMenuScene();
    }

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

        BindButtons();
        ShowMainMenu();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowMainMenu();
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void ShowIntro()
    {
        ShowPanel(introPanel);
    }

    public void ShowControls()
    {
        ShowPanel(controlsPanel);
    }

    public void ShowSettings()
    {
        ShowPanel(settingsPanel);
    }

    public void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(introPanel, false);
        SetPanelActive(controlsPanel, false);
        SetPanelActive(settingsPanel, false);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static bool IsMenuScene(string sceneName)
    {
        return string.Equals(sceneName, "MainMenu", System.StringComparison.OrdinalIgnoreCase);
    }

    private bool IsConfiguredMenuScene(string sceneName)
    {
        return string.Equals(sceneName, menuSceneName, System.StringComparison.OrdinalIgnoreCase) || IsMenuScene(sceneName);
    }

    private void ShowPanel(GameObject panel)
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(introPanel, panel == introPanel);
        SetPanelActive(controlsPanel, panel == controlsPanel);
        SetPanelActive(settingsPanel, panel == settingsPanel);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

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

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

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

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

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
