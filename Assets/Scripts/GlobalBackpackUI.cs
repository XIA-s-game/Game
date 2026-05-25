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

    public static void AddItem(string itemName, int amount = 1)
    {
        if (instance != null)
        {
            instance.Add(itemName, amount);
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

    private void OnGUI()
    {
        if (IsMenuScene())
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
        string sceneName = SceneManager.GetActiveScene().name;
        return string.Equals(sceneName, menuSceneName, System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sceneName, menuSceneAlias, System.StringComparison.OrdinalIgnoreCase);
    }

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

    private GUIStyle GetLabelStyle(int fontSize, TextAnchor alignment, FontStyle fontStyle)
    {
        return GameUiStyle.LabelStyle(ref labelStyle, fontSize, alignment, fontStyle);
    }
}
