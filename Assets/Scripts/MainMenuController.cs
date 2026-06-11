// Builds the cover screen, menu buttons, credits, music, and opening video flow.
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
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

    [Header("Audio And Video")]
    [SerializeField] private string backgroundAudioPath = "Audio/background.mp3";
    [SerializeField] private string openingVideoPath = "Audio/openingvideo.mp4";
    [SerializeField] private float backgroundVolume = 0.55f;
    [SerializeField] private float videoVolume = 1f;

    [Header("Cover")]
    [SerializeField] private string coverImagePath = "new/cover.png";
    [SerializeField] private float coverOnlyDuration = 5f;

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Main Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button introButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;

    [Header("Back Buttons")]
    [SerializeField] private Button introBackButton;
    [SerializeField] private Button controlsBackButton;
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button creditsBackButton;

    [Header("Panel Text")]
    [TextArea(3, 8)]
    [SerializeField] private string introText =
        "You wake up in a strange forest with a missing story to put back together. Talk to the locals, solve their puzzles, and follow the pages home."; 

    [TextArea(3, 8)]
    [SerializeField] private string controlsText =
        "WASD: Move\nMouse: Look\nSpace: Jump\nE: Interact\nEsc: Back or close"; 

    [TextArea(12, 24)]
    [SerializeField] private string creditsText =
        "================== CREDITS ==================\n\n" +
        "[Unity Asset Pack]\n" +
        "Aquarius Fantasy - Fae Pack (Unity Asset Store)\n" +
        "License: Single Entity License\n\n" +
        "[Character Animations and Models]\n" +
        "Mixamo (Adobe) - all character animations and some models\n" +
        "License: Royalty Free\n\n" +
        "[3D Models]\n" +
        "CGTrader - portals, bird nests, keys, feathers, workbench, etc.\n" +
        "Sketchfab - fairies, foxes, treasure chests, magic circles, etc.\n" +
        "License: Royalty Free / CC Attribution / CC BY-NC\n\n" +
        "[Sound Effects]\n" +
        "Game Sound Factory - all game sound effects\n" +
        "License: Non-Commercial Only\n\n" +
        "============================================\n" +
        "Made as a class project and shared only for study and discussion.\n" +
        "See the project document Credits.md for the full attribution list.";

    private static GameObject backgroundHost;
    private static AudioSource backgroundSource;
    private static AudioClip backgroundClip;
    private static bool backgroundLoading;

    private Canvas menuCanvas;
    private RawImage coverBackgroundImage;
    private AspectRatioFitter coverAspectFitter;
    private bool coverSequenceStarted;
    private bool coverSequenceComplete;
    private bool startingGame;

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
        HideAllPanels();
        StartCoroutine(ShowCoverThenMenu());
        StartCoroutine(EnsureBackgroundMusic());
    }

    private void Update()
    {
        if (coverSequenceComplete && Input.GetKeyDown(KeyCode.Escape))
        {
            ShowMainMenu();
        }
    }

    public void StartGame()
    {
        if (startingGame)
        {
            return;
        }

        StartCoroutine(StartGameRoutine());
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

    public void ShowCredits()
    {
        ShowPanel(creditsPanel);
    }

    public void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(introPanel, false);
        SetPanelActive(controlsPanel, false);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(creditsPanel, false);
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
        return string.Equals(sceneName, "MainMenu", System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(sceneName, "Mainmenu", System.StringComparison.OrdinalIgnoreCase);
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
        SetPanelActive(creditsPanel, panel == creditsPanel);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private void HideAllPanels()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(introPanel, false);
        SetPanelActive(controlsPanel, false);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(creditsPanel, false);
    }

    private void BindButtons()
    {
        BindButton(startGameButton, StartGame);
        BindButton(introButton, ShowIntro);
        BindButton(controlsButton, ShowControls);
        BindButton(settingsButton, ShowSettings);
        BindButton(creditsButton, ShowCredits);
        BindButton(exitButton, ExitGame);
        BindButton(introBackButton, ShowMainMenu);
        BindButton(controlsBackButton, ShowMainMenu);
        BindButton(settingsBackButton, ShowMainMenu);
        BindButton(creditsBackButton, ShowMainMenu);
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
        menuCanvas = canvas;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        CreateCoverBackground(canvasObject.transform);
        mainMenuPanel = CreateMainMenuPanel(canvasObject.transform);
        introPanel = CreateInfoPanel(canvasObject.transform, "IntroPanel", "Game Intro", introText, out introBackButton);
        controlsPanel = CreateInfoPanel(canvasObject.transform, "ControlsPanel", "Controls", controlsText, out controlsBackButton);
        settingsPanel = CreateInfoPanel(canvasObject.transform, "SettingsPanel", "Settings", "No extra settings for this build.", out settingsBackButton);
        creditsPanel = CreateInfoPanel(canvasObject.transform, "CreditsPanel", "CREDITS", creditsText, out creditsBackButton);
    }

    private GameObject CreateMainMenuPanel(Transform parent)
    {
        GameObject panel = CreatePanelRoot("MainMenuPanel", parent);
        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(0f, 0f, 0f, 0f);
        }

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

       

        startGameButton = CreateButton("StartGame", panel.transform, "Start Game");
        introButton = CreateButton("Intro", panel.transform, "Game Intro");
        controlsButton = CreateButton("Controls", panel.transform, "Controls");
        settingsButton = CreateButton("Settings", panel.transform, "Settings");
        creditsButton = CreateButton("Credits", panel.transform, "CREDITS");
        exitButton = CreateButton("Exit", panel.transform, "Exit Game");

        return panel;
    }

    private GameObject CreateInfoPanel(Transform parent, string objectName, string title, string body, out Button backButton)
    {
        GameObject panel = CreatePanelRoot(objectName, parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(980f, 780f);
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

        Text bodyText = CreateText("Body", panel.transform, body, 28, TextAnchor.UpperLeft);
        LayoutElement bodyLayout = bodyText.gameObject.AddComponent<LayoutElement>();
        bodyLayout.preferredHeight = 460f;

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

    private void CreateCoverBackground(Transform parent)
    {
        GameObject background = new GameObject("CoverBackground");
        background.transform.SetParent(parent, false);

        coverBackgroundImage = background.AddComponent<RawImage>();
        coverBackgroundImage.color = Color.black;
        coverAspectFitter = background.AddComponent<AspectRatioFitter>();
        coverAspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        coverAspectFitter.aspectRatio = 16f / 9f;

        RectTransform rect = coverBackgroundImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        background.transform.SetAsFirstSibling();
    }

    private void EnsureCoverBackground()
    {
        if (coverBackgroundImage != null)
        {
            coverBackgroundImage.transform.SetAsFirstSibling();
            return;
        }

        Canvas canvas = EnsureMenuCanvas();
        CreateCoverBackground(canvas.transform);
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

    private IEnumerator StartGameRoutine()
    {
        // The menu hands off to the opening video first, then loads the first playable scene.
        startingGame = true;
        SetMenuButtonsInteractable(false);
        StopBackgroundMusic();

        yield return StartCoroutine(PlayOpeningVideo());

        yield return StartCoroutine(EnsureBackgroundMusic());
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator ShowCoverThenMenu()
    {
        // Let the cover sit for a moment before the buttons appear.
        if (coverSequenceStarted)
        {
            yield break;
        }

        coverSequenceStarted = true;
        coverSequenceComplete = false;
        HideAllPanels();
        EnsureCoverBackground();

        yield return StartCoroutine(LoadCoverImage());
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, coverOnlyDuration));

        coverSequenceComplete = true;
        ShowMainMenu();
    }

    private IEnumerator LoadCoverImage()
    {
        EnsureCoverBackground();
        if (coverBackgroundImage == null)
        {
            yield break;
        }

        string path = Path.Combine(Application.dataPath, coverImagePath);
        if (!File.Exists(path))
        {
            coverBackgroundImage.color = Color.black;
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(new System.Uri(path).AbsoluteUri))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                coverBackgroundImage.color = Color.black;
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            coverBackgroundImage.texture = texture;
            coverBackgroundImage.color = Color.white;
            if (texture.width > 0 && texture.height > 0 && coverAspectFitter != null)
            {
                coverAspectFitter.aspectRatio = (float)texture.width / texture.height;
            }
        }
    }

    private IEnumerator PlayOpeningVideo()
    {
        // The video is optional. If the file is missing, the game just starts normally.
        string path = Path.Combine(Application.dataPath, openingVideoPath);
        if (!File.Exists(path))
        {
            yield break;
        }

        EnsureEventSystem();
        Canvas canvas = EnsureMenuCanvas();
        GameObject overlay = CreateVideoOverlay(canvas.transform, out RawImage videoImage);
        RenderTexture texture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        texture.Create();
        videoImage.texture = texture;

        GameObject playerObject = new GameObject("OpeningVideoPlayer");
        playerObject.transform.SetParent(transform, false);

        VideoPlayer player = playerObject.AddComponent<VideoPlayer>();
        AudioSource audio = playerObject.AddComponent<AudioSource>();
        bool finished = false;

        audio.playOnAwake = false;
        audio.loop = false;
        audio.spatialBlend = 0f;
        audio.volume = videoVolume;

        player.playOnAwake = false;
        player.waitForFirstFrame = true;
        player.skipOnDrop = true;
        player.source = VideoSource.Url;
        player.url = new System.Uri(path).AbsoluteUri;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = texture;
        player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        player.EnableAudioTrack(0, true);
        player.SetTargetAudioSource(0, audio);
        player.loopPointReached += _ => finished = true;
        player.errorReceived += (_, __) => finished = true;

        player.Prepare();
        float prepareDeadline = Time.unscaledTime + 8f;
        while (!player.isPrepared && !finished && Time.unscaledTime < prepareDeadline)
        {
            yield return null;
        }

        if (player.isPrepared)
        {
            player.Play();

            float duration = (float)player.length;
            if (duration <= 0f || duration > 600f)
            {
                duration = 30f;
            }

            float playDeadline = Time.unscaledTime + duration + 2f;
            while (!finished && Time.unscaledTime < playDeadline)
            {
                if (!player.isPlaying && player.frame > 0)
                {
                    break;
                }

                yield return null;
            }

            player.Stop();
        }

        videoImage.texture = null;
        player.targetTexture = null;
        texture.Release();
        Destroy(texture);
        Destroy(playerObject);
        Destroy(overlay);
    }

    private Canvas EnsureMenuCanvas()
    {
        if (menuCanvas != null)
        {
            return menuCanvas;
        }

        menuCanvas = GetComponentInChildren<Canvas>();
        if (menuCanvas != null)
        {
            return menuCanvas;
        }

        GameObject canvasObject = new GameObject("OpeningVideoCanvas");
        canvasObject.transform.SetParent(transform, false);
        menuCanvas = canvasObject.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 2000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return menuCanvas;
    }

    private static GameObject CreateVideoOverlay(Transform parent, out RawImage videoImage)
    {
        GameObject overlay = new GameObject("OpeningVideoOverlay");
        overlay.transform.SetParent(parent, false);

        Image background = overlay.AddComponent<Image>();
        background.color = Color.black;
        RectTransform overlayRect = background.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        GameObject imageObject = new GameObject("OpeningVideoImage");
        imageObject.transform.SetParent(overlay.transform, false);
        videoImage = imageObject.AddComponent<RawImage>();
        videoImage.color = Color.white;

        RectTransform imageRect = videoImage.rectTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        AspectRatioFitter fitter = imageObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;

        return overlay;
    }

    private IEnumerator EnsureBackgroundMusic()
    {
        EnsureBackgroundSource();

        if (backgroundClip == null && !backgroundLoading)
        {
            backgroundLoading = true;
            string path = Path.Combine(Application.dataPath, backgroundAudioPath);
            if (File.Exists(path))
            {
                using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(new System.Uri(path).AbsoluteUri, AudioType.MPEG))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        backgroundClip = DownloadHandlerAudioClip.GetContent(request);
                    }
                }
            }

            backgroundLoading = false;
        }

        if (backgroundSource != null && backgroundClip != null)
        {
            backgroundSource.clip = backgroundClip;
            backgroundSource.volume = backgroundVolume;
            backgroundSource.loop = true;
            backgroundSource.spatialBlend = 0f;

            if (!backgroundSource.isPlaying)
            {
                backgroundSource.Play();
            }
        }
    }

    private static void EnsureBackgroundSource()
    {
        if (backgroundSource != null)
        {
            return;
        }

        backgroundHost = new GameObject("PersistentBackgroundMusic");
        DontDestroyOnLoad(backgroundHost);
        backgroundSource = backgroundHost.AddComponent<AudioSource>();
        backgroundSource.playOnAwake = false;
    }

    private static void StopBackgroundMusic()
    {
        if (backgroundSource != null)
        {
            backgroundSource.Stop();
        }
    }

    private void SetMenuButtonsInteractable(bool interactable)
    {
        SetButtonInteractable(startGameButton, interactable);
        SetButtonInteractable(introButton, interactable);
        SetButtonInteractable(controlsButton, interactable);
        SetButtonInteractable(settingsButton, interactable);
        SetButtonInteractable(creditsButton, interactable);
        SetButtonInteractable(exitButton, interactable);
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }
}
