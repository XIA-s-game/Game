using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterOnePuzzle
{
    public static void SavePersistentStateForActiveScene()
    {
        if (SceneManager.GetActiveScene().name != SaveSceneName)
        {
            return;
        }

        if (activeInstance != null)
        {
            activeInstance.SavePersistentState();
        }
    }

    public static void ClearPersistentState()
    {
        DeleteSaveKeys(
            "CurrentIndex",
            "RecognizeHelpShown",
            "InitialHelpDialogueFinished",
            "RescueApplied",
            "PageRewardFinished",
            "ForestAttackDialogueFinished",
            "EnemiesActivated",
            "HeroWarningShown",
            "HeroCombatFinished",
            "HeroPostCombatDialogueFinished",
            "PortalUnlocked",
            "FirstPageAddedToBackpack",
            "CompletedPushes",
            "BlockPositions",
            "EnemyActiveStates");
    }

    private void SavePersistentState()
    {
        PlayerPrefs.SetInt(SaveKey("CurrentIndex"), currentIndex);
        SaveBool("RecognizeHelpShown", recognizeHelpShown);
        SaveBool("InitialHelpDialogueFinished", initialHelpDialogueFinished);
        SaveBool("RescueApplied", rescueApplied);
        SaveBool("PageRewardFinished", pageRewardFinished);
        SaveBool("ForestAttackDialogueFinished", forestAttackDialogueFinished);
        SaveBool("EnemiesActivated", enemiesActivated);
        SaveBool("HeroWarningShown", heroWarningShown);
        SaveBool("HeroCombatFinished", heroCombatFinished);
        SaveBool("HeroPostCombatDialogueFinished", heroPostCombatDialogueFinished);
        SaveBool("PortalUnlocked", portalUnlocked);
        SaveBool("FirstPageAddedToBackpack", firstPageAddedToBackpack);
        PlayerPrefs.SetString(SaveKey("CompletedPushes"), SerializeBoolArray(completedPushes));
        PlayerPrefs.SetString(SaveKey("BlockPositions"), SerializeBlockPositions());
        PlayerPrefs.SetString(SaveKey("EnemyActiveStates"), SerializeEnemyStates());
    }

    private void LoadPersistentState()
    {
        currentIndex = PlayerPrefs.GetInt(SaveKey("CurrentIndex"), currentIndex);
        recognizeHelpShown = LoadBool("RecognizeHelpShown", recognizeHelpShown);
        initialHelpDialogueFinished = LoadBool("InitialHelpDialogueFinished", initialHelpDialogueFinished);
        rescueApplied = LoadBool("RescueApplied", rescueApplied);
        pageRewardFinished = LoadBool("PageRewardFinished", pageRewardFinished);
        forestAttackDialogueFinished = LoadBool("ForestAttackDialogueFinished", forestAttackDialogueFinished);
        enemiesActivated = LoadBool("EnemiesActivated", enemiesActivated);
        heroWarningShown = LoadBool("HeroWarningShown", heroWarningShown);
        heroCombatFinished = LoadBool("HeroCombatFinished", heroCombatFinished);
        heroPostCombatDialogueFinished = LoadBool("HeroPostCombatDialogueFinished", heroPostCombatDialogueFinished);
        portalUnlocked = LoadBool("PortalUnlocked", portalUnlocked);
        firstPageAddedToBackpack = LoadBool("FirstPageAddedToBackpack", firstPageAddedToBackpack);
    }

    private void ApplySavedPushState()
    {
        if (pushBlocks.Count == 0)
        {
            return;
        }

        string completedPushesValue = PlayerPrefs.GetString(SaveKey("CompletedPushes"), string.Empty);
        string blockPositionsValue = PlayerPrefs.GetString(SaveKey("BlockPositions"), string.Empty);

        if (!string.IsNullOrEmpty(completedPushesValue))
        {
            ApplySerializedBoolArray(completedPushesValue);
        }

        if (!string.IsNullOrEmpty(blockPositionsValue))
        {
            ApplySerializedBlockPositions(blockPositionsValue);
        }
    }

    private string SerializeBoolArray(bool[] values)
    {
        if (values == null || values.Length == 0)
        {
            return string.Empty;
        }

        string[] parts = new string[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            parts[i] = values[i] ? "1" : "0";
        }

        return string.Join(",", parts);
    }

    private void ApplySerializedBoolArray(string serialized)
    {
        if (completedPushes == null || string.IsNullOrEmpty(serialized))
        {
            return;
        }

        string[] parts = serialized.Split(',');
        for (int i = 0; i < completedPushes.Length && i < parts.Length; i++)
        {
            completedPushes[i] = parts[i] == "1";
        }
    }

    private string SerializeBlockPositions()
    {
        if (pushBlocks.Count == 0)
        {
            return string.Empty;
        }

        string[] parts = new string[pushBlocks.Count];
        for (int i = 0; i < pushBlocks.Count; i++)
        {
            Transform block = pushBlocks[i];
            if (block == null)
            {
                parts[i] = string.Empty;
                continue;
            }

            Vector3 position = block.localPosition;
            parts[i] = position.x + "|" + position.y + "|" + position.z;
        }

        return string.Join(";", parts);
    }

    private void ApplySerializedBlockPositions(string serialized)
    {
        if (string.IsNullOrEmpty(serialized))
        {
            return;
        }

        string[] blockEntries = serialized.Split(';');
        for (int i = 0; i < pushBlocks.Count && i < blockEntries.Length; i++)
        {
            Transform block = pushBlocks[i];
            if (block == null || string.IsNullOrEmpty(blockEntries[i]))
            {
                continue;
            }

            string[] parts = blockEntries[i].Split('|');
            if (parts.Length != 3)
            {
                continue;
            }

            float x;
            float y;
            float z;
            if (!float.TryParse(parts[0], out x) ||
                !float.TryParse(parts[1], out y) ||
                !float.TryParse(parts[2], out z))
            {
                continue;
            }

            block.localPosition = new Vector3(x, y, z);
        }
    }

    private string SerializeEnemyStates()
    {
        if (delayedEnemyObjects == null || delayedEnemyObjects.Length == 0)
        {
            return string.Empty;
        }

        string[] parts = new string[delayedEnemyObjects.Length];
        for (int i = 0; i < delayedEnemyObjects.Length; i++)
        {
            GameObject enemy = delayedEnemyObjects[i];
            parts[i] = enemy != null && enemy.activeSelf ? "1" : "0";
        }

        return string.Join(",", parts);
    }

    private static string SaveKey(string keySuffix)
    {
        return SaveKeyPrefix + keySuffix;
    }

    private static void SaveBool(string keySuffix, bool value)
    {
        PlayerPrefs.SetInt(SaveKey(keySuffix), value ? 1 : 0);
    }

    private static bool LoadBool(string keySuffix, bool defaultValue)
    {
        return PlayerPrefs.GetInt(SaveKey(keySuffix), defaultValue ? 1 : 0) == 1;
    }

    private static void DeleteSaveKeys(params string[] keySuffixes)
    {
        for (int i = 0; i < keySuffixes.Length; i++)
        {
            PlayerPrefs.DeleteKey(SaveKey(keySuffixes[i]));
        }
    }
}
