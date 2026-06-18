// Builds and controls the main menu, cover sequence, opening video, settings, and game start flow.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    // Built with AI assistance to keep shared menu layout consistent across scenes.
    // Main menu owns cover art, opening video, settings, controls, and start/continue buttons.
    // Skips the cover after returning to the menu from another scene.
    private static bool skipCoverOnce;
    // Static music objects survive menu reloads so the background track does not restart unnecessarily.
    private static AudioSource backgroundSource;
    private static AudioClip backgroundClip;

    [Header("Scene Names")]
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "Chapter1_MagicForest";

    [Header("Audio And Video")]
    [SerializeField] private string backgroundAudioPath = "Audio/background.mp3";
    [SerializeField] private string openingVideoPath = "new/openning.mp4";
    [SerializeField] private AudioClip backgroundAudioClip;
    [SerializeField] private AudioSource backgroundAudioSource;
    [SerializeField] private VideoPlayer openingVideoPlayer;
    [SerializeField] private AudioSource openingVideoAudioSource;
    [SerializeField] private float backgroundVolume = 0.6f;
    [SerializeField] private float videoVolume = 1f;

    [Header("Cover")]
    [SerializeField] private string coverImagePath = "new/menu.png";
    [SerializeField] private string mainMenuImagePath = "new/select.png";
    [SerializeField] private string panelImagePath = "new/panel.png";
    [SerializeField] private float coverOnlyDuration = 5f;

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject continuePanel;
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Main Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button continueGameButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button introButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;

    [Header("Back Buttons")]
    [SerializeField] private Button continueBackButton;
    [SerializeField] private Button introBackButton;
    [SerializeField] private Button controlsBackButton;
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button creditsBackButton;

    [Header("Settings Controls")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;

    [Header("Cover And Video UI")]
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private RawImage coverBackgroundImage;
    [SerializeField] private AspectRatioFitter coverAspectFitter;
    [SerializeField] private Image controlsPanelImage;
    [SerializeField] private GameObject openingVideoOverlayObject;
    [SerializeField] private RawImage openingVideoImage;
    [SerializeField] private AspectRatioFitter openingVideoAspectFitter;

    [Header("Optional Dragged Media")]
    [SerializeField] private Texture2D coverTexture;
    [SerializeField] private Texture2D mainMenuTexture;
    [SerializeField] private Texture2D panelTexture;
    [SerializeField] private VideoClip openingVideoClip;

    private bool coverSequenceStarted;
    private bool coverSequenceComplete;
    private bool startingGame;
    // Sprites created from runtime-loaded textures are kept alive for the menu lifetime.
    private readonly List<Sprite> runtimeSprites = new List<Sprite>();

    private void Awake()
    {
        // Menu scene unlocks the mouse and prepares all panels before showing the cover sequence.
        if (!IsConfiguredMenuScene(SceneManager.GetActiveScene().name))
        {
            enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!HasRequiredInspectorReferences())
        {
            enabled = false;
            return;
        }

        BindButtons();
        ApplyResponsiveLayout();
        StartCoroutine(ApplyMenuTextures());
        mainMenuPanel.SetActive(false);
        continuePanel.SetActive(false);
        introPanel.SetActive(false);
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);

        if (skipCoverOnce)
        {
            skipCoverOnce = false;
            coverSequenceStarted = true;
            coverSequenceComplete = true;
            coverBackgroundImage.transform.SetAsFirstSibling();
            ShowMainMenu();
        }
        else
        {
            StartCoroutine(ShowCoverThenMenu());
        }

        StartCoroutine(LoadBackgroundMusic());
    }
    // Apply a responsive layout to the menu UI elements, stretching background images to fill the canvas and setting aspect fitters to envelope parent mode for consistent display across different screen resolutions and aspect ratios.
    private void ApplyResponsiveLayout()
    {
        // Fullscreen media stretches to the canvas so cover art and video fill different resolutions.
        StretchToCanvas(coverBackgroundImage != null ? coverBackgroundImage.rectTransform : null);
        StretchToCanvas(openingVideoOverlayObject != null ? openingVideoOverlayObject.GetComponent<RectTransform>() : null);
        StretchToCanvas(openingVideoImage != null ? openingVideoImage.rectTransform : null);

        StretchToCanvas(mainMenuPanel != null ? mainMenuPanel.GetComponent<RectTransform>() : null);

        if (coverAspectFitter != null)
        {
            coverAspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        }

        if (openingVideoAspectFitter != null)
        {
            openingVideoAspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        }
    }
    // Check that all required Inspector references are assigned, logging an error with missing fields if any are null. This helps catch setup issues in the Unity Editor before runtime.
    private bool HasRequiredInspectorReferences()
    {
        // UI hierarchy should be dragged in the Inspector; missing references stop menu startup.
        List<string> missing = new List<string>();
        AddMissing(missing, mainMenuPanel, "Main Menu Panel");
        AddMissing(missing, continuePanel, "Continue Panel");
        AddMissing(missing, introPanel, "Intro Panel");
        AddMissing(missing, controlsPanel, "Controls Panel");
        AddMissing(missing, settingsPanel, "Settings Panel");
        AddMissing(missing, creditsPanel, "Credits Panel");
        AddMissing(missing, startGameButton, "Start Game Button");
        AddMissing(missing, continueGameButton, "Continue Game Button");
        AddMissing(missing, newGameButton, "New Game Button");
        AddMissing(missing, introButton, "Intro Button");
        AddMissing(missing, controlsButton, "Controls Button");
        AddMissing(missing, settingsButton, "Settings Button");
        AddMissing(missing, creditsButton, "Credits Button");
        AddMissing(missing, exitButton, "Exit Button");
        AddMissing(missing, continueBackButton, "Continue Back Button");
        AddMissing(missing, introBackButton, "Intro Back Button");
        AddMissing(missing, controlsBackButton, "Controls Back Button");
        AddMissing(missing, settingsBackButton, "Settings Back Button");
        AddMissing(missing, creditsBackButton, "Credits Back Button");
        AddMissing(missing, volumeSlider, "Volume Slider");
        AddMissing(missing, muteToggle, "Mute Toggle");
        AddMissing(missing, menuCanvas, "Menu Canvas");
        AddMissing(missing, coverBackgroundImage, "Cover Background Image");
        AddMissing(missing, coverAspectFitter, "Cover Aspect Fitter");
        AddMissing(missing, openingVideoOverlayObject, "Opening Video Overlay Object");
        AddMissing(missing, openingVideoImage, "Opening Video Image");
        AddMissing(missing, openingVideoAspectFitter, "Opening Video Aspect Fitter");

        if (missing.Count == 0)
        {
            return true;
        }

        Debug.LogError("MainMenuController is missing Inspector references: " + string.Join(", ", missing), this);
        return false;
    }

    private static void AddMissing(List<string> missing, UnityEngine.Object value, string fieldName)
    {
        if (value == null)
        {
            missing.Add(fieldName);
        }
    }

    private static void StretchToCanvas(RectTransform rectTransform)
    {
        // Anchors fullscreen menu art and video to all edges of the canvas.
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private IEnumerator ApplyMenuTextures()
    {
        // Dragged textures are used first; file paths are fallback for the existing menu artwork.
        Texture2D cover = coverTexture;
        Texture2D mainMenu = mainMenuTexture;
        Texture2D panel = panelTexture;

        yield return StartCoroutine(LoadTextureIfNeeded(coverImagePath, texture => cover = texture, cover == null));
        yield return StartCoroutine(LoadTextureIfNeeded(mainMenuImagePath, texture => mainMenu = texture, mainMenu == null));
        yield return StartCoroutine(LoadTextureIfNeeded(panelImagePath, texture => panel = texture, panel == null));

        if (cover != null)
        {
            coverBackgroundImage.texture = cover;
            coverBackgroundImage.color = Color.white;
            if (cover.height > 0)
            {
                coverAspectFitter.aspectRatio = (float)cover.width / cover.height;
            }
        }

        ApplyPanelTexture(mainMenuPanel, mainMenu);
        ApplyPanelTexture(continuePanel, panel);
        ApplyPanelTexture(introPanel, panel);
        ApplyPanelTexture(controlsPanel, panel);
        ApplyPanelTexture(settingsPanel, panel);
        ApplyPanelTexture(creditsPanel, panel);
    }

    private void ApplyPanelTexture(GameObject panel, Texture2D texture)
    {
        // Panels may keep their Image on the root or on a child named Background.
        if (panel == null || texture == null)
        {
            return;
        }

        Image image = panel.GetComponent<Image>();
        if (image == null)
        {
            Transform background = panel.transform.Find("Background");
            if (background != null)
            {
                image = background.GetComponent<Image>();
            }
        }

        if (image == null && panel == controlsPanel && controlsPanelImage != null)
        {
            image = controlsPanelImage;
        }

        if (image == null)
        {
            return;
        }

        image.sprite = CreateSprite(texture);
        image.color = Color.white;
        image.preserveAspect = false;
        if (panel == mainMenuPanel)
        {
            StretchToCanvas(image.rectTransform);
        }
    }

    private Sprite CreateSprite(Texture2D texture)
    {
        // Runtime sprites must be stored so Unity does not release their backing object too early.
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));
        runtimeSprites.Add(sprite);
        return sprite;
    }

    private void Update()
    {
        if (coverSequenceComplete && Input.GetKeyDown(KeyCode.Escape))
        {
            ShowMainMenu();
        }
    }

    public static void SkipCoverOnNextMenuLoad()
    {
        skipCoverOnce = true;
    }

    public void StartGame()
    {
        // First click opens the continue/new-game choice panel.
        if (startingGame)
        {
            return;
        }

        mainMenuPanel.SetActive(false);
        continuePanel.SetActive(true);
        introPanel.SetActive(false);
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        continueGameButton.interactable = GameSaveManager.HasSave;
    }

    public void ContinueGame()
    {
        // Continue loads the saved scene directly and skips the opening video.
        if (startingGame)
        {
            return;
        }

        mainMenuPanel.SetActive(false);
        continuePanel.SetActive(false);
        introPanel.SetActive(false);
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        string targetSceneName = GameSaveManager.GetContinueScene(gameSceneName);
        StartCoroutine(StartGameRoutine(targetSceneName, false));
    }

    public void StartNewGame()
    {
        // New game clears saves and plays the opening video before loading Chapter One.
        if (startingGame)
        {
            return;
        }

        mainMenuPanel.SetActive(false);
        continuePanel.SetActive(false);
        introPanel.SetActive(false);
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        GameSaveManager.StartNewGame();
        StartCoroutine(StartGameRoutine(gameSceneName, true));
    }

    public void ShowIntro()
    {
        mainMenuPanel.SetActive(false);
        continuePanel.SetActive(false);
        introPanel.SetActive(true);
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    public void ShowControls()
    {
        mainMenuPanel.SetActive(false);
        continuePanel.SetActive(false);
        introPanel.SetActive(false);
        controlsPanel.SetActive(true);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    public void ShowSettings()
    {
        SyncSettingsControls();
        mainMenuPanel.SetActive(false);
        continuePanel.SetActive(false);
        introPanel.SetActive(false);
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(true);
        creditsPanel.SetActive(false);
    }

    public void ShowCredits()
    {
        mainMenuPanel.SetActive(false);
        continuePanel.SetActive(false);
        introPanel.SetActive(false);
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void ShowMainMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        mainMenuPanel.SetActive(true);
        continuePanel.SetActive(false);
        introPanel.SetActive(false);
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public static void PauseBackgroundMusicForSceneAudio()
    {
        // Cutscenes can pause menu music while they play their own audio.
        if (backgroundSource != null && backgroundSource.isPlaying)
        {
            backgroundSource.Pause();
        }
    }

    public static void ResumeBackgroundMusicAfterSceneAudio()
    {
        // Scene audio callers can resume the static menu music source afterward.
        if (backgroundSource != null && backgroundSource.clip != null && !backgroundSource.isPlaying)
        {
            backgroundSource.UnPause();
        }
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

    private void BindButtons()
    {
        // All menu and sub-panel buttons are wired here to keep Awake readable.
        startGameButton.onClick.RemoveAllListeners();
        startGameButton.onClick.AddListener(StartGame);
        continueGameButton.onClick.RemoveAllListeners();
        continueGameButton.onClick.AddListener(ContinueGame);
        newGameButton.onClick.RemoveAllListeners();
        newGameButton.onClick.AddListener(StartNewGame);
        introButton.onClick.RemoveAllListeners();
        introButton.onClick.AddListener(ShowIntro);
        controlsButton.onClick.RemoveAllListeners();
        controlsButton.onClick.AddListener(ShowControls);
        settingsButton.onClick.RemoveAllListeners();
        settingsButton.onClick.AddListener(ShowSettings);
        creditsButton.onClick.RemoveAllListeners();
        creditsButton.onClick.AddListener(ShowCredits);
        exitButton.onClick.RemoveAllListeners();
        exitButton.onClick.AddListener(ExitGame);
        continueBackButton.onClick.RemoveAllListeners();
        continueBackButton.onClick.AddListener(ShowMainMenu);
        introBackButton.onClick.RemoveAllListeners();
        introBackButton.onClick.AddListener(ShowMainMenu);
        controlsBackButton.onClick.RemoveAllListeners();
        controlsBackButton.onClick.AddListener(ShowMainMenu);
        settingsBackButton.onClick.RemoveAllListeners();
        settingsBackButton.onClick.AddListener(ShowMainMenu);
        creditsBackButton.onClick.RemoveAllListeners();
        creditsBackButton.onClick.AddListener(ShowMainMenu);

        volumeSlider.onValueChanged.RemoveAllListeners();
        volumeSlider.onValueChanged.AddListener(HandleVolumeChanged);

        muteToggle.onValueChanged.RemoveAllListeners();
        muteToggle.onValueChanged.AddListener(HandleMuteChanged);
    }

    private void SyncSettingsControls()
    {
        // Settings UI mirrors saved audio state before the panel becomes visible.
        volumeSlider.SetValueWithoutNotify(GameAudioSettings.Muted ? 0f : GameAudioSettings.MasterVolume);
        muteToggle.SetIsOnWithoutNotify(GameAudioSettings.Muted);
        GameAudioSettings.ApplySavedAudioSettings();
    }

    private void HandleVolumeChanged(float value)
    {
        GameAudioSettings.SetAudioSettings(value, muteToggle.isOn);
    }

    private void HandleMuteChanged(bool muted)
    {
        GameAudioSettings.SetAudioSettings(volumeSlider.value, muted);
    }

    private IEnumerator StartGameRoutine(string targetSceneName, bool playOpeningVideo)
    {
        // Start and continue share this load path; only new game plays the opening video.
        startingGame = true;
        GameSaveManager.MarkSessionStartedFromMainMenu();
        GlobalBackpackUI.EnableForGameSession();
        SetMenuButtonsInteractable(false);
        StopBackgroundMusic();

        if (playOpeningVideo)
        {
            yield return StartCoroutine(PlayOpeningVideo());
        }

        yield return StartCoroutine(LoadBackgroundMusic());
        SceneManager.LoadScene(targetSceneName);
    }

    private IEnumerator ShowCoverThenMenu()
    {
        // Initial cover stays on screen before the main menu buttons appear.
        if (coverSequenceStarted)
        {
            yield break;
        }

        coverSequenceStarted = true;
        coverSequenceComplete = false;
        mainMenuPanel.SetActive(false);
        continuePanel.SetActive(false);
        introPanel.SetActive(false);
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        coverBackgroundImage.transform.SetAsFirstSibling();

        yield return StartCoroutine(LoadCoverImage());
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, coverOnlyDuration));

        coverSequenceComplete = true;
        ShowMainMenu();
    }

    private IEnumerator LoadCoverImage()
    {
        Texture2D texture = coverTexture;
        yield return StartCoroutine(LoadTextureIfNeeded(coverImagePath, loaded => texture = loaded, texture == null));

        if (texture != null)
        {
            coverBackgroundImage.texture = texture;
            coverBackgroundImage.color = Color.white;
            if (texture.height > 0)
            {
                coverAspectFitter.aspectRatio = (float)texture.width / texture.height;
            }
        }

        yield break;
    }

    private IEnumerator PlayOpeningVideo()
    {
        // Opening video uses a dragged VideoClip when present, otherwise the project mp4 path.
        bool useClip = openingVideoClip != null;
        string videoUrl = AssetFileUrl(openingVideoPath);
        if (!useClip && string.IsNullOrEmpty(videoUrl))
        {
            yield break;
        }

        RenderTexture texture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        texture.Create();
        openingVideoImage.texture = texture;
        openingVideoOverlayObject.SetActive(true);

        VideoPlayer player = openingVideoPlayer;
        AudioSource audio = openingVideoAudioSource;
        GameObject runtimeVideoObject = null;
        if (player == null || audio == null)
        {
            runtimeVideoObject = new GameObject("Runtime Opening Video Player");
            runtimeVideoObject.transform.SetParent(transform, false);
            player = runtimeVideoObject.AddComponent<VideoPlayer>();
            audio = runtimeVideoObject.AddComponent<AudioSource>();
        }

        bool finished = false;

        audio.playOnAwake = false;
        audio.loop = false;
        audio.spatialBlend = 0f;
        audio.volume = videoVolume;

        player.playOnAwake = false;
        player.waitForFirstFrame = true;
        player.skipOnDrop = true;
        player.source = useClip ? VideoSource.VideoClip : VideoSource.Url;
        player.clip = useClip ? openingVideoClip : null;
        player.url = useClip ? string.Empty : videoUrl;
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

        openingVideoImage.texture = null;
        openingVideoOverlayObject.SetActive(false);
        player.targetTexture = null;
        texture.Release();
        Destroy(texture);
        if (runtimeVideoObject != null)
        {
            Destroy(runtimeVideoObject);
        }
    }

    private IEnumerator LoadBackgroundMusic()
    {
        // Background music is kept static so it can continue across menu refreshes.
        if (backgroundSource == null && backgroundAudioSource != null)
        {
            backgroundSource = backgroundAudioSource;
        }

        if (backgroundClip == null && backgroundAudioClip != null)
        {
            backgroundClip = backgroundAudioClip;
        }

        if (backgroundClip == null)
        {
            string audioUrl = AssetFileUrl(backgroundAudioPath);
            if (!string.IsNullOrEmpty(audioUrl))
            {
                using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.MPEG))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        backgroundClip = DownloadHandlerAudioClip.GetContent(request);
                    }
                    else
                    {
                        Debug.LogWarning("MainMenuController could not load background audio: " + backgroundAudioPath, this);
                    }
                }
            }
        }

        if (backgroundSource == null && backgroundClip != null)
        {
            GameObject audioObject = new GameObject("Main Menu Background Audio");
            DontDestroyOnLoad(audioObject);
            backgroundSource = audioObject.AddComponent<AudioSource>();
        }

        if (backgroundSource != null && backgroundClip != null)
        {
            backgroundSource.clip = backgroundClip;
            backgroundSource.volume = backgroundVolume;
            backgroundSource.priority = 200;
            backgroundSource.loop = true;
            backgroundSource.spatialBlend = 0f;
            backgroundSource.ignoreListenerPause = true;

            if (!backgroundSource.isPlaying)
            {
                backgroundSource.Play();
            }
        }

        yield break;
    }

    private IEnumerator LoadTextureIfNeeded(string relativeAssetPath, Action<Texture2D> applyTexture, bool shouldLoad)
    {
        // Build-safe Resources loading is tried before the old editor file path fallback.
        if (!shouldLoad)
        {
            yield break;
        }

        Texture2D resourceTexture = LoadResourceTexture(relativeAssetPath);
        if (resourceTexture != null)
        {
            applyTexture(resourceTexture);
            yield break;
        }

        string url = AssetFileUrl(relativeAssetPath);
        if (string.IsNullOrEmpty(url))
        {
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                applyTexture(DownloadHandlerTexture.GetContent(request));
            }
            else
            {
                Debug.LogWarning("MainMenuController could not load texture: " + relativeAssetPath, this);
            }
        }
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

    private static string AssetFileUrl(string relativeAssetPath)
    {
        // UnityWebRequest needs file:// style URLs for project-local Assets files.
        if (string.IsNullOrWhiteSpace(relativeAssetPath))
        {
            return string.Empty;
        }

        string path = Path.Combine(Application.dataPath, relativeAssetPath);
        return File.Exists(path) ? new Uri(path).AbsoluteUri : string.Empty;
    }

    private static void StopBackgroundMusic()
    {
        // Stops menu music before scene load or video playback.
        if (backgroundSource != null)
        {
            backgroundSource.Stop();
        }
    }

    private void SetMenuButtonsInteractable(bool interactable)
    {
        // Prevents duplicate clicks while the start flow is already running.
        startGameButton.interactable = interactable;
        introButton.interactable = interactable;
        controlsButton.interactable = interactable;
        settingsButton.interactable = interactable;
        creditsButton.interactable = interactable;
        exitButton.interactable = interactable;
    }
}
