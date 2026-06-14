using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalBackpackUI : MonoBehaviour
{
    // Shared backpack display and item counts for all gameplay scenes.
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
        // Story scripts call this when a reward or quest item should appear in the bag.
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
            return;
        }

        instance.itemCounts.Clear();
        instance.itemOrder.Clear();
    }

    public static void ExportItems(out string[] itemNames, out int[] itemAmounts)
    {
        // Save system exports the current backpack as parallel arrays for JsonUtility.
        if (instance == null)
        {
            itemNames = new string[0];
            itemAmounts = new int[0];
            return;
        }

        itemNames = new string[instance.itemOrder.Count];
        itemAmounts = new int[instance.itemOrder.Count];
        for (int i = 0; i < instance.itemOrder.Count; i++)
        {
            string itemName = instance.itemOrder[i];
            itemNames[i] = itemName;
            itemAmounts[i] = instance.itemCounts.TryGetValue(itemName, out int amount) ? amount : 0;
        }
    }

    public static void ImportItems(string[] itemNames, int[] itemAmounts)
    {
        // Continue game rebuilds backpack contents from saved item names and counts.
        if (instance == null)
        {
            return;
        }

        instance.itemCounts.Clear();
        instance.itemOrder.Clear();

        if (itemNames == null || itemAmounts == null)
        {
            return;
        }

        int count = Mathf.Min(itemNames.Length, itemAmounts.Length);
        for (int i = 0; i < count; i++)
        {
            if (!string.IsNullOrEmpty(itemNames[i]) && itemAmounts[i] > 0)
            {
                instance.SetCount(itemNames[i], itemAmounts[i]);
            }
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

        if (IsMenuScene())
        {
            enabledForGameSession = false;
            inventoryOpen = false;
        }
        else
        {
            enabledForGameSession = true;
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
            return;
        }

        if (!itemCounts.ContainsKey(itemName))
        {
            itemOrder.Add(itemName);
        }

        itemCounts[itemName] = count;
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

        float width = inventoryOpen ? 560f : 240f;
        float height = inventoryOpen ? Mathf.Min(620f, 150f + Mathf.Max(1, itemOrder.Count) * 62f) : 150f;
        Rect rect = GameUiStyle.BackpackRect(width, height);

        GameUiStyle.DrawPanel(rect);
        Rect bagRect = new Rect(rect.x + rect.width - 148f, rect.y + 18f, 124f, 102f);
        GameUiStyle.DrawBag(bagRect);
        GUI.Label(new Rect(rect.x + 34f, rect.y + 24f, rect.width - 190f, 60f), "Backpack", GameUiStyle.LabelStyle(ref titleStyle, 32, TextAnchor.MiddleLeft, FontStyle.Bold));
        GUI.Label(new Rect(rect.x + 34f, rect.y + 86f, rect.width - 68f, 36f), "Press B to close", GameUiStyle.LabelStyle(ref labelStyle, 18, TextAnchor.MiddleLeft));

        float listY = rect.y + 150f;

        if (itemOrder.Count == 0)
        {
            GUI.Label(new Rect(rect.x + 40f, listY, rect.width - 80f, 54f), "Empty", GameUiStyle.LabelStyle(ref labelStyle, 24, TextAnchor.MiddleLeft));
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
            GUI.Label(new Rect(rect.x + 40f, listY + i * 62f, rect.width - 80f, 56f), text, GameUiStyle.LabelStyle(ref labelStyle, 24, TextAnchor.MiddleLeft));
        }
    }

}
