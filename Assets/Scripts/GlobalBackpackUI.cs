using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalBackpackUI : MonoBehaviour
{
    // Shared backpack display and item counts for all gameplay scenes.
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private string menuSceneAlias = "Mainmenu";

    [Header("Backpack UI")]
    [SerializeField] private float panelWidth = 560f;
    [SerializeField] private float panelMaxHeight = 620f;
    [SerializeField] private float panelBaseHeight = 150f;
    [SerializeField] private float rowHeight = 62f;
    [SerializeField] private Vector2 bagIconSize = new Vector2(124f, 102f);
    [SerializeField] private Vector2 bagIconOffset = new Vector2(148f, 18f);
    [SerializeField] private UiPadding titlePadding;
    [SerializeField] private UiPadding hintPadding;
    [SerializeField] private UiPadding listPadding;
    [SerializeField] private int titleFontSize = 32;
    [SerializeField] private int hintFontSize = 18;
    [SerializeField] private int itemFontSize = 24;

    private static GlobalBackpackUI instance;
    private static bool enabledForGameSession;
    private static bool inputBlocked;

    private readonly Dictionary<string, int> itemCounts = new Dictionary<string, int>();
    private readonly List<string> itemOrder = new List<string>();
    private bool inventoryOpen;
    private GUIStyle labelStyle;
    private GUIStyle titleStyle;

    private void OnValidate()
    {
        FillMissingInspectorDefaults();
    }

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
    
    // Remove all quantities of a certain item.
    public static void RemoveAll(string itemName)
    {
        SetItemCount(itemName, 0);
    }
    // Enable the backpack for the current game session, allowing it to be opened and displayed. Does not add any items by itself.
    public static void EnableForGameSession()
    {
        enabledForGameSession = true;
    }
    // Disable the backpack for the current game session, hiding it and preventing it from being opened. Does not clear existing items.
    public static void DisableForGameSession()
    {
        enabledForGameSession = false;
        inputBlocked = false;
        if (instance != null)
        {
            instance.inventoryOpen = false;
        }
    }
    
    // Prevent backpack input during the dialogue/selection process.
    public static void SetInputBlocked(bool blocked)
    {
        inputBlocked = blocked;
        if (blocked && instance != null)
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
        FillMissingInspectorDefaults();
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

        if (!inputBlocked && Input.GetKeyDown(KeyCode.B))
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

        float width = panelWidth;
        float height = Mathf.Min(panelMaxHeight, panelBaseHeight + Mathf.Max(1, itemOrder.Count) * rowHeight);
        Rect rect = GameUiStyle.BackpackRect(width, height);

        GameUiStyle.DrawPanel(rect);
        Rect bagRect = new Rect(rect.x + rect.width - bagIconOffset.x, rect.y + bagIconOffset.y, bagIconSize.x, bagIconSize.y);
        GameUiStyle.DrawBag(bagRect);
        Rect titleRect = titlePadding.Apply(rect);
        Rect hintRect = hintPadding.Apply(rect);
        GUI.Label(new Rect(titleRect.x, titleRect.y, titleRect.width, 60f), "Backpack", GameUiStyle.LabelStyle(ref titleStyle, titleFontSize, TextAnchor.MiddleLeft, FontStyle.Bold));
        GUI.Label(new Rect(hintRect.x, hintRect.y, hintRect.width, 36f), "Press B to close", GameUiStyle.LabelStyle(ref labelStyle, hintFontSize, TextAnchor.MiddleLeft));

        float listY = rect.y + listPadding.top;

        if (itemOrder.Count == 0)
        {
            GUI.Label(new Rect(rect.x + listPadding.left, listY, rect.width - listPadding.left - listPadding.right, 54f), "Empty", GameUiStyle.LabelStyle(ref labelStyle, itemFontSize, TextAnchor.MiddleLeft));
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
            GUI.Label(new Rect(rect.x + listPadding.left, listY + i * rowHeight, rect.width - listPadding.left - listPadding.right, rowHeight - 6f), text, GameUiStyle.LabelStyle(ref labelStyle, itemFontSize, TextAnchor.MiddleLeft));
        }
    }

    private void FillMissingInspectorDefaults()
    {
        if (panelWidth <= 0f)
        {
            panelWidth = 560f;
        }

        if (panelMaxHeight <= 0f)
        {
            panelMaxHeight = 620f;
        }

        if (panelBaseHeight <= 0f)
        {
            panelBaseHeight = 150f;
        }

        if (rowHeight <= 0f)
        {
            rowHeight = 62f;
        }

        if (bagIconSize == Vector2.zero)
        {
            bagIconSize = new Vector2(124f, 102f);
        }

        if (bagIconOffset == Vector2.zero)
        {
            bagIconOffset = new Vector2(148f, 18f);
        }

        if (titlePadding.IsZero)
        {
            titlePadding = UiPadding.Create(34f, 190f, 24f, 0f);
        }

        if (hintPadding.IsZero)
        {
            hintPadding = UiPadding.Create(34f, 68f, 86f, 0f);
        }

        if (listPadding.IsZero)
        {
            listPadding = UiPadding.Create(40f, 80f, 150f, 0f);
        }

        if (titleFontSize <= 0)
        {
            titleFontSize = 32;
        }

        if (hintFontSize <= 0)
        {
            hintFontSize = 18;
        }

        if (itemFontSize <= 0)
        {
            itemFontSize = 24;
        }
    }

}
