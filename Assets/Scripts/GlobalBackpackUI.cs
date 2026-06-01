// Main function: Stores global backpack item counts and draws the backpack panel outside menu scenes.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalBackpackUI : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private string menuSceneAlias = "Mainmenu";

    private static GlobalBackpackUI instance;

    private readonly Dictionary<string, int> itemCounts = new Dictionary<string, int>();
    private readonly List<string> itemOrder = new List<string>();
    private bool inventoryOpen;
    private GUIStyle labelStyle;

    // Function: Adds item.
    public static void AddItem(string itemName, int amount = 1)
    {
        if (instance != null)
        {
            instance.Add(itemName, amount);
        }
    }

    // Function: Sets item count.
    public static void SetItemCount(string itemName, int count)
    {
        if (instance != null)
        {
            instance.SetCount(itemName, count);
        }
    }

    // Function: Removes item.
    public static void RemoveItem(string itemName, int amount = 1)
    {
        if (instance != null)
        {
            instance.Remove(itemName, amount);
        }
    }

    // Function: Removes all.
    public static void RemoveAll(string itemName)
    {
        SetItemCount(itemName, 0);
    }

    // Function: Initializes component references, cached state, and default runtime data.
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Function: Updates input handling, interaction checks, and active gameplay flow each frame.
    private void Update()
    {
        if (IsMenuScene())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            inventoryOpen = !inventoryOpen;
        }
    }

    // Function: Draws this script's IMGUI prompts, panels, and dialogue.
    private void OnGUI()
    {
        if (IsMenuScene())
        {
            return;
        }

        DrawBackpack();
    }

    // Function: Adds the current script.
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

    // Function: Sets count.
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

    // Function: Removes the current script.
    private void Remove(string itemName, int amount)
    {
        if (string.IsNullOrEmpty(itemName) || amount <= 0 || !itemCounts.ContainsKey(itemName))
        {
            return;
        }

        SetCount(itemName, itemCounts[itemName] - amount);
    }

    // Function: Checks whether menu scene is true.
    private bool IsMenuScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return string.Equals(sceneName, menuSceneName, System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sceneName, menuSceneAlias, System.StringComparison.OrdinalIgnoreCase);
    }

    // Function: Draws the UI elements for backpack.
    private void DrawBackpack()
    {
        float width = inventoryOpen ? 250f : 118f;
        float height = inventoryOpen ? Mathf.Min(260f, 70f + Mathf.Max(1, itemOrder.Count) * 28f) : 48f;
        Rect rect = GameUiStyle.BackpackRect(width, height);
        GameUiStyle.DrawPanel(rect);

        GUI.Label(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 34f), "Backpack B", GetLabelStyle(20, TextAnchor.MiddleCenter, FontStyle.Bold));

        if (!inventoryOpen)
        {
            return;
        }

        if (itemOrder.Count == 0)
        {
            GUI.Label(new Rect(rect.x + 16f, rect.y + 52f, rect.width - 32f, 26f), "Empty", GetLabelStyle(18, TextAnchor.MiddleLeft, FontStyle.Normal));
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
            GUI.Label(new Rect(rect.x + 16f, rect.y + 52f + i * 28f, rect.width - 32f, 26f), text, GetLabelStyle(18, TextAnchor.MiddleLeft, FontStyle.Normal));
        }
    }

    // Function: Gets or calculates label style.
    private GUIStyle GetLabelStyle(int fontSize, TextAnchor alignment, FontStyle fontStyle)
    {
        return GameUiStyle.LabelStyle(ref labelStyle, fontSize, alignment, fontStyle);
    }
}
