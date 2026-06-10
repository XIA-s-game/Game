using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    private static bool skipCoverOnce;
    private static GameObject backgroundHost;
    private static AudioSource backgroundSource;
    private static AudioClip backgroundClip;
    private static AudioListener backgroundListener;
    private static bool backgroundLoading;

    [Header("Scene Names")]
    [SerializeField] private string menuSceneName = "Mainmenu";
    [SerializeField] private string gameSceneName = "Assets/Scenes/Enchanted Forest A.unity";

    [Header("Audio And Video")]
    [SerializeField] private string backgroundAudioPath = "Audio/background.mp3";
    [SerializeField] private string openingVideoPath = "Audio/openingvideo.mp4";
    [SerializeField] private float backgroundVolume = 0.6f;
    [SerializeField] private float videoVolume = 1f;

    [Header("Cover")]
    [SerializeField] private string coverImagePath = "new/cover.png";
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
    [SerializeField] private GameObject openingVideoOverlayObject;
    [SerializeField] private RawImage openingVideoImage;
    [SerializeField] private AspectRatioFitter openingVideoAspectFitter;

    private bool coverSequenceStarted;
    private bool coverSequenceComplete;
    private bool startingGame;

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SetBackgroundListenerEnabled(IsMenuScene(SceneManager.GetActiveScene().name));
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Awake()
    {
        if (!IsConfiguredMenuScene(SceneManager.GetActiveScene().name))
        {
            enabled = false;
            return;
        }

        if (mainMenuPanel == null ||
            continuePanel == null ||
            introPanel == null ||
            controlsPanel == null ||
            settingsPanel == null ||
            creditsPanel == null ||
            startGameButton == null ||
            continueGameButton == null ||
            newGameButton == null ||
            introButton == null ||
            controlsButton == null ||
            settingsButton == null ||
            creditsButton == null ||
            exitButton == null ||
            continueBackButton == null ||
            introBackButton == null ||
            controlsBackButton == null ||
            settingsBackButton == null ||
            creditsBackButton == null ||
            volumeSlider == null ||
            muteToggle == null ||
            menuCanvas == null ||
            coverBackgroundImage == null ||
            coverAspectFitter == null ||
            openingVideoOverlayObject == null ||
            openingVideoImage == null ||
            openingVideoAspectFitter == null)
        {
            Debug.LogError("MainMenuController is missing Inspector references.", this);
            enabled = false;
            return;
        }

        BindButtons();
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
        if (startingGame)
        {
            return;
        }

        if (GlobalGameMenuUI.HasSave())
        {
            mainMenuPanel.SetActive(false);
            continuePanel.SetActive(true);
            introPanel.SetActive(false);
            controlsPanel.SetActive(false);
            settingsPanel.SetActive(false);
            creditsPanel.SetActive(false);
        }
        else
        {
            StartNewGame();
        }
    }

    public void ContinueGame()
    {
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
        GlobalGameMenuUI.PrepareContinueLoad();
        StartCoroutine(StartGameRoutine(GlobalGameMenuUI.GetSavedSceneName(gameSceneName), false));
    }

    public void StartNewGame()
    {
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
        GlobalGameMenuUI.ClearSave();
        GlobalBackpackUI.ClearAllItems();
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
        if (backgroundSource != null && backgroundSource.isPlaying)
        {
            backgroundSource.Pause();
        }
    }

    public static void ResumeBackgroundMusicAfterSceneAudio()
    {
        if (backgroundSource != null && backgroundSource.clip != null && !backgroundSource.isPlaying)
        {
            backgroundSource.UnPause();
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetBackgroundListenerEnabled(IsMenuScene(scene.name));
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
        volumeSlider.SetValueWithoutNotify(GlobalGameMenuUI.Muted ? 0f : GlobalGameMenuUI.MasterVolume);
        muteToggle.SetIsOnWithoutNotify(GlobalGameMenuUI.Muted);
        GlobalGameMenuUI.ApplySavedAudioSettings();
    }

    private void HandleVolumeChanged(float value)
    {
        GlobalGameMenuUI.SetAudioSettings(value, muteToggle.isOn);
    }

    private void HandleMuteChanged(bool muted)
    {
        GlobalGameMenuUI.SetAudioSettings(volumeSlider.value, muted);
    }

    private IEnumerator StartGameRoutine(string targetSceneName, bool playOpeningVideo)
    {
        startingGame = true;
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
            if (texture.width > 0 && texture.height > 0)
            {
                coverAspectFitter.aspectRatio = (float)texture.width / texture.height;
            }
        }
    }

    private IEnumerator PlayOpeningVideo()
    {
        string path = Path.Combine(Application.dataPath, openingVideoPath);
        if (!File.Exists(path))
        {
            yield break;
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        RenderTexture texture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        texture.Create();
        openingVideoImage.texture = texture;
        openingVideoOverlayObject.SetActive(true);

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

        openingVideoImage.texture = null;
        openingVideoOverlayObject.SetActive(false);
        player.targetTexture = null;
        texture.Release();
        Destroy(texture);
        Destroy(playerObject);
    }

    private IEnumerator LoadBackgroundMusic()
    {
        if (backgroundSource == null)
        {
            backgroundHost = new GameObject("PersistentBackgroundMusic");
            DontDestroyOnLoad(backgroundHost);
            backgroundSource = backgroundHost.AddComponent<AudioSource>();
            backgroundSource.playOnAwake = false;
            backgroundSource.spatialBlend = 0f;
            backgroundSource.ignoreListenerPause = true;
            backgroundListener = backgroundHost.AddComponent<AudioListener>();
            backgroundListener.enabled = false;
        }

        SetBackgroundListenerEnabled(IsMenuScene(SceneManager.GetActiveScene().name));

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
            backgroundSource.priority = 200;
            backgroundSource.loop = true;
            backgroundSource.spatialBlend = 0f;
            backgroundSource.ignoreListenerPause = true;

            if (!backgroundSource.isPlaying)
            {
                backgroundSource.Play();
            }
        }
    }

    private static void StopBackgroundMusic()
    {
        if (backgroundSource != null)
        {
            backgroundSource.Stop();
        }

        SetBackgroundListenerEnabled(false);
    }

    private static void SetBackgroundListenerEnabled(bool enabledInMenu)
    {
        if (backgroundListener == null)
        {
            return;
        }

        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener != null && listener.enabled && listener != backgroundListener)
            {
                backgroundListener.enabled = false;
                return;
            }
        }

        backgroundListener.enabled = enabledInMenu;
    }

    private void SetMenuButtonsInteractable(bool interactable)
    {
        startGameButton.interactable = interactable;
        introButton.interactable = interactable;
        controlsButton.interactable = interactable;
        settingsButton.interactable = interactable;
        creditsButton.interactable = interactable;
        exitButton.interactable = interactable;
    }
}
