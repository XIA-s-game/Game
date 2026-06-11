using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class GlobalGameMenuUI : MonoBehaviour
{
    private const string SaveSceneKey = "SavedSceneName";
    private const string SaveExistsKey = "SaveExists";
    private const string SaveHasPlayerTransformKey = "SaveHasPlayerTransform";
    private const string SavedPlayerPositionXKey = "SavedPlayerPositionX";
    private const string SavedPlayerPositionYKey = "SavedPlayerPositionY";
    private const string SavedPlayerPositionZKey = "SavedPlayerPositionZ";
    private const string SavedPlayerRotationYKey = "SavedPlayerRotationY";
    private const string PendingContinueLoadKey = "PendingContinueLoad";
    private const string MasterVolumeKey = "MasterVolume";
    private const string MutedKey = "Muted";

    private enum MenuPage
    {
        Closed,
        Main,
        Settings
    }

    private static GlobalGameMenuUI instance;

    [SerializeField] private string menuSceneName = "Mainmenu";
    [SerializeField] private Transform player;

    private MenuPage page;
    private float volume = 1f;
    private bool muted;
    private bool wasPausedByMenu;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;
    private bool cursorStateSaved;
    private GUIStyle labelStyle;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;

    public static float MasterVolume => instance != null ? (instance.muted ? 0f : instance.volume) : LoadSavedVolume();

    public static bool Muted => instance != null ? instance.muted : PlayerPrefs.GetInt(MutedKey, 0) == 1;

    public static void SaveCurrentGame()
    {
        if (IsMenuSceneName(SceneManager.GetActiveScene().name))
        {
            return;
        }

        PlayerPrefs.SetString(SaveSceneKey, SceneManager.GetActiveScene().name);
        PlayerPrefs.SetInt(SaveExistsKey, 1);
        SavePlayerTransform(instance != null ? instance.player : null);
        ChapterOnePuzzle.SavePersistentStateForActiveScene();
        GlobalBackpackUI.SaveNow();
        PlayerPrefs.Save();
    }

    public static bool HasSave()
    {
        return PlayerPrefs.GetInt(SaveExistsKey, 0) == 1 &&
               !string.IsNullOrEmpty(PlayerPrefs.GetString(SaveSceneKey, string.Empty));
    }

    public static string GetSavedSceneName(string fallbackSceneName)
    {
        string sceneName = PlayerPrefs.GetString(SaveSceneKey, string.Empty);
        return string.IsNullOrEmpty(sceneName) ? fallbackSceneName : sceneName;
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveSceneKey);
        PlayerPrefs.DeleteKey(SaveExistsKey);
        PlayerPrefs.DeleteKey(SaveHasPlayerTransformKey);
        PlayerPrefs.DeleteKey(SavedPlayerPositionXKey);
        PlayerPrefs.DeleteKey(SavedPlayerPositionYKey);
        PlayerPrefs.DeleteKey(SavedPlayerPositionZKey);
        PlayerPrefs.DeleteKey(SavedPlayerRotationYKey);
        PlayerPrefs.DeleteKey(PendingContinueLoadKey);
        PlayerInstructionTutorial.ClearPersistentState();
        ChapterOnePuzzle.ClearPersistentState();
        PlayerPrefs.Save();
    }

    public static void PrepareContinueLoad()
    {
        if (!HasSave())
        {
            return;
        }

        PlayerPrefs.SetInt(PendingContinueLoadKey, 1);
        PlayerPrefs.Save();
    }

    public static void SetAudioSettings(float newVolume, bool newMuted)
    {
        float clampedVolume = Mathf.Clamp01(newVolume);
        PlayerPrefs.SetFloat(MasterVolumeKey, clampedVolume);
        PlayerPrefs.SetInt(MutedKey, newMuted ? 1 : 0);
        PlayerPrefs.Save();

        AudioListener.volume = newMuted ? 0f : clampedVolume;

        if (instance != null)
        {
            instance.volume = clampedVolume;
            instance.muted = newMuted;
        }
    }

    public static void ApplySavedAudioSettings()
    {
        bool savedMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        float savedVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        AudioListener.volume = savedMuted ? 0f : savedVolume;

        if (instance != null)
        {
            instance.volume = savedVolume;
            instance.muted = savedMuted;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Time.timeScale = 1f;
        ApplySavedAudioSettings();
        TryRestoreSavedPlayerTransform(SceneManager.GetActiveScene().name);
    }

    private void Update()
    {
        if (IsMenuScene())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (page == MenuPage.Closed)
            {
                OpenMainMenu();
            }
            else
            {
                CloseMenu();
            }
        }
    }

    private void OnGUI()
    {
        if (IsMenuScene())
        {
            return;
        }

        if (page == MenuPage.Closed)
        {
            if (Event.current.type == EventType.Repaint)
            {
                DrawEscHint();
            }

            return;
        }

        if (page == MenuPage.Main)
        {
            DrawMenu();
        }
        else
        {
            DrawSettings();
        }
    }

    private void OpenMainMenu()
    {
        page = MenuPage.Main;
        if (Time.timeScale > 0f)
        {
            Time.timeScale = 0f;
            wasPausedByMenu = true;
        }

        SaveAndUnlockCursor();
    }

    private void OpenSettings()
    {
        page = MenuPage.Settings;
    }

    private void CloseMenu()
    {
        page = MenuPage.Closed;

        if (wasPausedByMenu)
        {
            Time.timeScale = 1f;
            wasPausedByMenu = false;
        }

        RestoreCursor();
    }

    private void DrawEscHint()
    {
        Rect rect = new Rect(GameUiStyle.Margin, GameUiStyle.Margin, 180f, 58f);
        GameUiStyle.DrawPanel(rect);
        GUI.Label(rect, "Press ESC", GameUiStyle.LabelStyle(ref labelStyle, 18, TextAnchor.MiddleCenter, FontStyle.Bold, true));
    }

    private void DrawMenu()
    {
        Rect rect = new Rect(GameUiStyle.Margin, Screen.height - 330f, 520f, 300f);
        GameUiStyle.DrawPanel(rect);
        GUI.Label(new Rect(rect.x + 32f, rect.y + 28f, rect.width - 64f, 56f), "Game Menu", GameUiStyle.LabelStyle(ref titleStyle, 24, TextAnchor.MiddleCenter, FontStyle.Bold));

        float y = rect.y + 92f;
        if (DrawMenuButton(rect, y, "Save Game"))
        {
            SaveCurrentGame();
        }

        y += 66f;
        if (DrawMenuButton(rect, y, "Settings"))
        {
            OpenSettings();
        }

        y += 66f;
        if (DrawMenuButton(rect, y, "Return to Menu"))
        {
            SaveCurrentGame();
            CloseMenuForSceneLoad();
            MainMenuController.SkipCoverOnNextMenuLoad();
            SceneManager.LoadScene(menuSceneName);
        }
    }

    private void DrawSettings()
    {
        Rect rect = new Rect((Screen.width - 560f) * 0.5f, (Screen.height - 300f) * 0.5f, 560f, 300f);
        GameUiStyle.DrawPanel(rect);

        GUI.Label(new Rect(rect.x + 32f, rect.y + 24f, rect.width - 64f, 48f), "Settings", GameUiStyle.LabelStyle(ref titleStyle, 24, TextAnchor.MiddleCenter, FontStyle.Bold));
        GUI.Label(new Rect(rect.x + 42f, rect.y + 96f, rect.width - 84f, 34f), "Sound Volume", GameUiStyle.LabelStyle(ref labelStyle, 16, TextAnchor.MiddleLeft, FontStyle.Bold, true));

        float newVolume = GUI.HorizontalSlider(new Rect(rect.x + 48f, rect.y + 150f, rect.width - 96f, 24f), volume, 0f, 1f);
        bool newMuted = GUI.Toggle(new Rect(rect.x + 48f, rect.y + 188f, rect.width - 96f, 32f), muted, "Mute");
        if (!Mathf.Approximately(newVolume, volume) || newMuted != muted)
        {
            SetAudioSettings(newVolume, newMuted);
        }

        if (GUI.Button(new Rect(rect.x + 110f, rect.y + 232f, rect.width - 220f, 48f), "Back", GetButtonStyle()))
        {
            OpenMainMenu();
        }
    }

    private void CloseMenuForSceneLoad()
    {
        page = MenuPage.Closed;

        if (wasPausedByMenu)
        {
            Time.timeScale = 1f;
            wasPausedByMenu = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorStateSaved = false;
    }

    private bool DrawMenuButton(Rect parent, float y, string text)
    {
        return GUI.Button(new Rect(parent.x + 54f, y, parent.width - 108f, 58f), text, GetButtonStyle());
    }

    private GUIStyle GetButtonStyle()
    {
        return GameUiStyle.ButtonStyle(ref buttonStyle, 18);
    }

    private bool IsMenuScene()
    {
        return IsMenuSceneName(SceneManager.GetActiveScene().name);
    }

    private static bool IsMenuSceneName(string sceneName)
    {
        return string.Equals(sceneName, "MainMenu", System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(sceneName, "Mainmenu", System.StringComparison.OrdinalIgnoreCase);
    }

    private static float LoadSavedVolume()
    {
        bool savedMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        float savedVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        return savedMuted ? 0f : savedVolume;
    }
}
