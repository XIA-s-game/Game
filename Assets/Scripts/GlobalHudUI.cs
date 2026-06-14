using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalHudUI : MonoBehaviour
{
    // Shared in-game Esc menu drawn in gameplay scenes.
    [SerializeField] private string mainMenuSceneName = "Mainmenu";
    [SerializeField] private string hintText = "Press Esc";
    [SerializeField] private bool lockCursorWhenClosed = true;

    private GUIStyle hintStyle;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private GUIStyle toggleStyle;
    private bool menuOpen;
    private bool settingsOpen;
    private bool controlsOpen;
    private bool cursorWasLocked;
    private bool cursorWasVisible;
    private float volumeValue = 1f;
    private bool muted;

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
            volumeValue = GlobalGameMenuUI.MasterVolume;
            muted = GlobalGameMenuUI.Muted;
        }
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
        if (IsMenuScene())
        {
            return;
        }

        Rect hintRect = new Rect(20f, 16f, 190f, 48f);
        GameUiStyle.DrawPanel(hintRect);
        GUI.Label(new Rect(hintRect.x + 16f, hintRect.y + 5f, hintRect.width - 32f, hintRect.height - 10f),
            hintText,
            GameUiStyle.LabelStyle(ref hintStyle, 15, TextAnchor.MiddleCenter, FontStyle.Bold));

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
            DrawSettings(MainMenuPanelRect(900f, 620f));
            return;
        }

        if (controlsOpen)
        {
            DrawControls(MainMenuPanelRect(1688f, 1080f));
            return;
        }

        Rect rect = new Rect((Screen.width - 520f) * 0.5f, (Screen.height - 390f) * 0.5f, 520f, 390f);
        GameUiStyle.DrawPanel(rect);
        GUI.Label(new Rect(rect.x + 24f, rect.y + 18f, rect.width - 48f, 34f), "Game Menu", GameUiStyle.LabelStyle(ref titleStyle, 22, TextAnchor.MiddleCenter, FontStyle.Bold));

        if (GUI.Button(new Rect(rect.x + 66f, rect.y + 76f, rect.width - 132f, 54f), "Save and Exit to Menu", GameUiStyle.ButtonStyle(ref buttonStyle, 18)))
        {
            GameSaveManager.SaveCurrentGame();
            GlobalBackpackUI.DisableForGameSession();
            MainMenuController.SkipCoverOnNextMenuLoad();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        if (GUI.Button(new Rect(rect.x + 66f, rect.y + 144f, rect.width - 132f, 54f), "Settings", GameUiStyle.ButtonStyle(ref buttonStyle, 18)))
        {
            settingsOpen = true;
            volumeValue = GlobalGameMenuUI.MasterVolume;
            muted = GlobalGameMenuUI.Muted;
        }

        if (GUI.Button(new Rect(rect.x + 66f, rect.y + 212f, rect.width - 132f, 54f), "Controls", GameUiStyle.ButtonStyle(ref buttonStyle, 18)))
        {
            controlsOpen = true;
        }

        if (GUI.Button(new Rect(rect.x + 66f, rect.y + 280f, rect.width - 132f, 54f), "Resume", GameUiStyle.ButtonStyle(ref buttonStyle, 18)))
        {
            SetMenuOpen(false);
        }
    }

    private void DrawSettings(Rect rect)
    {
        // Settings mirrors the main menu audio controls.
        GameUiStyle.DrawPanel(rect);
        GUI.Label(new Rect(rect.x + 74f, rect.y + 60f, rect.width - 148f, 44f), "Settings", GameUiStyle.LabelStyle(ref titleStyle, 24, TextAnchor.MiddleCenter, FontStyle.Bold));

        float contentX = rect.x + 74f;
        float contentWidth = rect.width - 148f;
        GUI.Label(new Rect(contentX, rect.y + 156f, contentWidth, 40f), "Volume", GameUiStyle.LabelStyle(ref labelStyle, 20, TextAnchor.MiddleLeft, FontStyle.Bold));
        volumeValue = GUI.HorizontalSlider(new Rect(contentX, rect.y + 224f, contentWidth, 36f), volumeValue, 0f, 1f);

        muted = GUI.Toggle(new Rect(contentX, rect.y + 306f, 220f, 44f), muted, "Mute", ToggleStyle());
        GlobalGameMenuUI.SetAudioSettings(volumeValue, muted);

        if (GUI.Button(BackButtonRect(rect), "Back", GameUiStyle.ButtonStyle(ref buttonStyle, 18)))
        {
            settingsOpen = false;
        }
    }

    private void DrawControls(Rect rect)
    {
        // Controls panel uses the same shared panel art as the main menu.
        GameUiStyle.DrawPanel(rect);
        GUI.Label(new Rect(rect.x + 100f, rect.y + 78f, rect.width - 200f, 48f), "Controls", GameUiStyle.LabelStyle(ref titleStyle, 28, TextAnchor.MiddleCenter, FontStyle.Bold));

        GUI.Label(new Rect(rect.x + 180f, rect.y + 190f, rect.width - 360f, rect.height - 410f),
            "WASD Move\nMouse Look\nShift Run\nSpace Jump\nV Crouch\nE Interact\nB Backpack\nM Map",
            GameUiStyle.LabelStyle(ref labelStyle, 28, TextAnchor.MiddleCenter, FontStyle.Bold, true));

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

    private static Rect BackButtonRect(Rect panelRect)
    {
        float width = Mathf.Min(150f, panelRect.width - 148f);
        float height = 58f;
        return new Rect(panelRect.center.x - width * 0.5f, panelRect.yMax - 110f - height, width, height);
    }

    private GUIStyle ToggleStyle()
    {
        if (toggleStyle == null)
        {
            toggleStyle = new GUIStyle(GUI.skin.toggle);
        }

        toggleStyle.fontSize = GameUiStyle.ScaledFontSize(20);
        toggleStyle.fontStyle = FontStyle.Bold;
        toggleStyle.normal.textColor = Color.white;
        toggleStyle.onNormal.textColor = Color.white;
        toggleStyle.hover.textColor = Color.white;
        toggleStyle.onHover.textColor = Color.white;
        return toggleStyle;
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
}
