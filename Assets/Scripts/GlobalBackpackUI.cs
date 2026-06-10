using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalBackpackUI : MonoBehaviour
{
    private const string SaveItemsKey = "BackpackItems";

    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private string menuSceneAlias = "Mainmenu";

    private static GlobalBackpackUI instance;
    private static bool enabledForGameSession;

    private readonly Dictionary<string, int> itemCounts = new Dictionary<string, int>();
    private readonly List<string> itemOrder = new List<string>();
    private bool inventoryOpen;
    private GUIStyle labelStyle;
    private GUIStyle titleStyle;

    public static void AddItem(string itemName, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemName) || amount <= 0)
        {
            return;
        }

        if (instance != null)
        {
            instance.Add(itemName, amount);
            GameAudioManager.PlayFetch();
        }
    }

    public static void SetItemCount(string itemName, int count)
    {
        if (instance != null)
        {
            instance.SetCount(itemName, count);
        }
    }

    public static void RemoveItem(string itemName, int amount = 1)
    {
        if (instance != null)
        {
            instance.Remove(itemName, amount);
        }
    }

    public static void RemoveAll(string itemName)
    {
        SetItemCount(itemName, 0);
    }

    public static void EnableForGameSession()
    {
        enabledForGameSession = true;
    }

    public static void DisableForGameSession()
    {
        enabledForGameSession = false;
        if (instance != null)
        {
            instance.inventoryOpen = false;
        }
    }

    public static void ClearAllItems()
    {
        if (instance == null)
        {
            PlayerPrefs.DeleteKey(SaveItemsKey);
            return;
        }

        instance.itemCounts.Clear();
        instance.itemOrder.Clear();
        instance.SaveItems();
    }

    public static void SaveNow()
    {
        if (instance != null)
        {
            instance.SaveItems();
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
        LoadItems();

        if (IsMenuScene())
        {
            enabledForGameSession = false;
            inventoryOpen = false;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (IsMenuScene() || !enabledForGameSession)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            inventoryOpen = !inventoryOpen;
        }
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || IsMenuScene() || !enabledForGameSession)
        {
            return;
        }

        DrawBackpack();
    }

    private void Add(string itemName, int amount)
    {
        if (string.IsNullOrEmpty(itemName) || amount <= 0)
        {
            return;
        }

        if (!itemCounts.ContainsKey(itemName))
        {
            itemCounts.Add(itemName, 0);
            itemOrder.Add(itemName);
        }

        itemCounts[itemName] += amount;
        SaveItems();
    }

    private void SetCount(string itemName, int count)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            return;
        }

        if (count <= 0)
        {
            itemCounts.Remove(itemName);
            itemOrder.Remove(itemName);
            SaveItems();
            return;
        }

        if (!itemCounts.ContainsKey(itemName))
        {
            itemOrder.Add(itemName);
        }

        itemCounts[itemName] = count;
        SaveItems();
    }

    private void Remove(string itemName, int amount)
    {
        if (string.IsNullOrEmpty(itemName) || amount <= 0 || !itemCounts.ContainsKey(itemName))
        {
            return;
        }

        SetCount(itemName, itemCounts[itemName] - amount);
    }

    private bool IsMenuScene()
    {
        return IsMenuSceneName(SceneManager.GetActiveScene().name);
    }

    private bool IsMenuSceneName(string sceneName)
    {
        return string.Equals(sceneName, menuSceneName, System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sceneName, menuSceneAlias, System.StringComparison.OrdinalIgnoreCase);
    }

    private void DrawBackpack()
    {
        if (!inventoryOpen)
        {
            return;
        }

        float width = inventoryOpen ? 420f : 190f;
        float height = inventoryOpen ? Mathf.Min(460f, 118f + Mathf.Max(1, itemOrder.Count) * 48f) : 128f;
        Rect rect = GameUiStyle.BackpackRect(width, height);

        GameUiStyle.DrawPanel(rect);
        Rect bagRect = new Rect(rect.x + rect.width - 112f, rect.y + 14f, 92f, 76f);
        GameUiStyle.DrawBag(bagRect);
        GUI.Label(new Rect(rect.x + 28f, rect.y + 18f, rect.width - 154f, 54f), "Backpack", GameUiStyle.LabelStyle(ref titleStyle, 22, TextAnchor.MiddleLeft, FontStyle.Bold));
        GUI.Label(new Rect(rect.x + 28f, rect.y + 70f, rect.width - 56f, 32f), "Press B to close", GameUiStyle.LabelStyle(ref labelStyle, 12, TextAnchor.MiddleLeft));

        float listY = rect.y + 118f;

        if (itemOrder.Count == 0)
        {
            GUI.Label(new Rect(rect.x + 34f, listY, rect.width - 68f, 42f), "Empty", GameUiStyle.LabelStyle(ref labelStyle, 16, TextAnchor.MiddleLeft));
            return;
        }

        for (int i = 0; i < itemOrder.Count; i++)
        {
            string itemName = itemOrder[i];
            if (!itemCounts.TryGetValue(itemName, out int count))
            {
                continue;
            }

            string text = count > 1 ? itemName + " x" + count : itemName;
            GUI.Label(new Rect(rect.x + 34f, listY + i * 48f, rect.width - 68f, 42f), text, GameUiStyle.LabelStyle(ref labelStyle, 15, TextAnchor.MiddleLeft));
        }
    }

    private void SaveItems()
    {
        List<string> entries = new List<string>();
        for (int i = 0; i < itemOrder.Count; i++)
        {
            string itemName = itemOrder[i];
            if (itemCounts.TryGetValue(itemName, out int count) && count > 0)
            {
                entries.Add(itemName.Replace("|", string.Empty) + "|" + count);
            }
        }

        PlayerPrefs.SetString(SaveItemsKey, string.Join("\n", entries.ToArray()));
        PlayerPrefs.Save();
    }

    private void LoadItems()
    {
        itemCounts.Clear();
        itemOrder.Clear();

        string saved = PlayerPrefs.GetString(SaveItemsKey, string.Empty);
        if (string.IsNullOrEmpty(saved))
        {
            return;
        }

        string[] entries = saved.Split('\n');
        for (int i = 0; i < entries.Length; i++)
        {
            string[] parts = entries[i].Split('|');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int count) || count <= 0)
            {
                continue;
            }

            itemCounts[parts[0]] = count;
            itemOrder.Add(parts[0]);
        }
    }
}
