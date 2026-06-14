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

    public static bool HasSave => PlayerPrefs.HasKey(SaveKey);

    public static bool ContinueMode
    {
        get => PlayerPrefs.GetInt(ContinueModeKey, 0) == 1;
        private set => PlayerPrefs.SetInt(ContinueModeKey, value ? 1 : 0);
    }

    public static void StartNewGame()
    {
        // New game clears progress and enables the tutorial for the first Enchanted Forest load.
        ClearSave();
        ContinueMode = false;
        WriteTutorialFlag(true);
        GlobalBackpackUI.ClearAllItems();
    }

    public static string GetContinueScene(string defaultScene)
    {
        // Continue mode loads the saved scene and disables tutorial playback.
        SaveData data = Load();
        ContinueMode = true;
        WriteTutorialFlag(false);
        return data != null && !string.IsNullOrEmpty(data.sceneName) ? data.sceneName : defaultScene;
    }

    public static void SaveCurrentGame()
    {
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

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        ContinueMode = false;
        PlayerPrefs.Save();
    }

    public static void RegisterPlayer(Transform player)
    {
        registeredPlayer = player;
    }

    public static void ApplyLoadedSceneStateForCurrentScene()
    {
        ApplyLoadedSceneState(SceneManager.GetActiveScene());
    }

    private static void ApplyLoadedSceneState(Scene scene)
    {
        // Applies resume data only when the loaded scene matches the saved scene.
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

    private static bool IsMenuScene(string sceneName)
    {
        return string.Equals(sceneName, "Mainmenu", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sceneName, "MainMenu", StringComparison.OrdinalIgnoreCase);
    }

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
