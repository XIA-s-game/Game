using System;
using System.Collections.Generic;
using AquariusMax.Fae.demo;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSaveManager
{
    // Built with AI assistance to keep shared menu layout consistent across scenes.
    // Keeps menu, backpack, and scene resume state in one small save record.
    [Serializable]
    public class SaveData
    {
        public string sceneName = "Enchanted Forest A";
        public SerializableVector3 playerPosition;
        public bool hasPlayerPosition;
        public bool continueMode;
        public bool enchantedPuzzleSolved;
        public string[] backpackItems = Array.Empty<string>();
        public int[] backpackCounts = Array.Empty<int>();
    }

    [Serializable]
    public struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(Vector3 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    private const string SaveKey = "GameSave.Current";
    private const string ContinueModeKey = "GameSave.ContinueMode";
    private const string TutorialNextLoadKey = "Tutorial.EnabledForNextLoad";
    private static Transform registeredPlayer;
    private static bool sessionStartedFromMainMenu;

    public static bool HasSave => PlayerPrefs.HasKey(SaveKey);
    // Continue mode is enabled when loading from a save, allowing the game to resume in the saved scene with saved progress. It is disabled when starting a new game or when no valid save data is present.
    public static bool ContinueMode
    {
        get => PlayerPrefs.GetInt(ContinueModeKey, 0) == 1;
        private set => PlayerPrefs.SetInt(ContinueModeKey, value ? 1 : 0);
    }
    // Start a new game by clearing any existing save data, enabling tutorial playback for the first Enchanted Forest load, and marking the session as started from the main menu.
    public static void StartNewGame()
    {
        // New game clears progress and enables the tutorial for the first Enchanted Forest load.
        ClearSave();
        ContinueMode = false;
        WriteTutorialFlag(true);
        sessionStartedFromMainMenu = true;
        GlobalBackpackUI.ClearAllItems();
    }
    // Get the scene to load for continue mode, enabling continue mode if valid save data is found. If no valid save is found, continue mode will still be enabled but the default scene will be returned.
    public static string GetContinueScene(string defaultScene)
    {
        // Continue mode loads the saved scene and disables tutorial playback.
        SaveData data = Load();
        ContinueMode = true;
        WriteTutorialFlag(false);
        sessionStartedFromMainMenu = true;
        return data != null && !string.IsNullOrEmpty(data.sceneName) ? data.sceneName : defaultScene;
    }
    // Save the current game state, including player position, backpack contents, and scene-specific flags. Only saves if the session was started from the main menu to prevent saving in invalid states.
    public static void SaveCurrentGame()
    {
        if (!sessionStartedFromMainMenu)
        {
            return;
        }

        // Saves player position, backpack contents, and scene-specific completion flags.
        SaveData data = new SaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            continueMode = true
        };

        Transform player = registeredPlayer;
        if (player != null)
        {
            Vector3 resumePosition = player.position;
            if (ChapterTwoPuzzle.TryGetResumePosition(out Vector3 chapterTwoPosition))
            {
                resumePosition = chapterTwoPosition;
            }

            data.playerPosition = new SerializableVector3(resumePosition);
            data.hasPlayerPosition = true;
        }

        GlobalBackpackUI.ExportItems(out data.backpackItems, out data.backpackCounts);
        FilterTemporaryQuestItems(ref data.backpackItems, ref data.backpackCounts);
        data.enchantedPuzzleSolved = ChapterOnePuzzle.IsPuzzleSolvedForSave();
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
    // Load the saved game state, returning null if no valid save data is found. The loaded data can be applied to the game state using ApplyLoadedSceneStateForCurrentScene or ApplyLoadedSceneState.
    public static SaveData Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch
        {
            return null;
        }
    }
    // Clear the saved game data, resetting continue mode and deleting the save record from PlayerPrefs.
    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        ContinueMode = false;
        PlayerPrefs.Save();
    }
    // Register the player transform to allow the save manager to apply loaded position data. This should be called by a scene reference script after the player object is initialized in the scene.
    public static void RegisterPlayer(Transform player)
    {
        registeredPlayer = player;
    }
    // Mark the session as started from the main menu, enabling save functionality.
    public static void MarkSessionStartedFromMainMenu()
    {
        sessionStartedFromMainMenu = true;
    }
    // Apply the loaded scene state for the current active scene, if valid save data is present and the session was started from the main menu. This should be called after the scene has loaded to apply player position, backpack contents, and scene-specific flags.
    public static void ApplyLoadedSceneStateForCurrentScene()
    {
        ApplyLoadedSceneState(SceneManager.GetActiveScene());
    }

    private static void ApplyLoadedSceneState(Scene scene)
    {
        // Applies resume data only when the loaded scene matches the saved scene.
        if (!sessionStartedFromMainMenu)
        {
            return;
        }

        if (IsMenuScene(scene.name))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        SaveData data = Load();
        if (data == null || !ContinueMode || !string.Equals(data.sceneName, scene.name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        GlobalBackpackUI.ImportItems(data.backpackItems, data.backpackCounts);
        ChapterOnePuzzle.ApplySaveState(data.enchantedPuzzleSolved);

        if (data.hasPlayerPosition)
        {
            Transform player = registeredPlayer;
            if (player != null)
            {
                CharacterController controller = player.GetComponent<CharacterController>();
                bool controllerEnabled = controller != null && controller.enabled;
                if (controller != null)
                {
                    controller.enabled = false;
                }

                player.position = data.playerPosition.ToVector3();

                if (controller != null)
                {
                    controller.enabled = controllerEnabled;
                }
            }
        }

        DemoCharacter.ResetControlFlags();
    }
    // Check if the given scene name corresponds to a menu scene where the cursor should be unlocked and visible.
    private static bool IsMenuScene(string sceneName)
    {
        return string.Equals(sceneName, "Mainmenu", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sceneName, "MainMenu", StringComparison.OrdinalIgnoreCase);
    }
    // Set a flag to enable or disable the tutorial for the next load, which can be checked by the tutorial system to determine whether to play tutorial prompts.
    private static void WriteTutorialFlag(bool enabled)
    {
        PlayerPrefs.SetInt(TutorialNextLoadKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private static void FilterTemporaryQuestItems(ref string[] itemNames, ref int[] itemCounts)
    {
        // Temporary quest objects reset with their side quest and should not persist in the bag.
        if (itemNames == null || itemCounts == null)
        {
            return;
        }

        List<string> names = new List<string>();
        List<int> counts = new List<int>();
        int count = Mathf.Min(itemNames.Length, itemCounts.Length);
        for (int i = 0; i < count; i++)
        {
            if (itemCounts[i] <= 0 || IsTemporaryQuestItem(itemNames[i]))
            {
                continue;
            }

            names.Add(itemNames[i]);
            counts.Add(itemCounts[i]);
        }

        itemNames = names.ToArray();
        itemCounts = counts.ToArray();
    }
    // Check if the given item name corresponds to a temporary quest item that should not be saved in the backpack.
    private static bool IsTemporaryQuestItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            return false;
        }

        switch (itemName.Trim().ToLowerInvariant())
        {
            case "feather":
            case "key":
            case "red key":
            case "blue key":
            case "green key":
            case "yellow key":
            case "maze pass":
            case "honey jar":
            case "full honey jar":
            case "silver leaf":
                return true;
            default:
                return false;
        }
    }
}
