using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalBackpackUI : MonoBehaviour
{
    // Shared backpack display and item counts for all gameplay scenes.
    [SerializeField] private string menuSceneName = "MainMenu";
    // Optional second name if a menu scene needs an alias.
    [SerializeField] private string menuSceneAlias = "";

    [Header("Backpack UI")]
    // Width of the backpack panel.
    [SerializeField] private float panelWidth = 560f;
    // Maximum height before the list area stops growing.
    [SerializeField] private float panelMaxHeight = 620f;
    // Base height before item rows are added.
    [SerializeField] private float panelBaseHeight = 150f;
    // Height used by each item row.
    [SerializeField] private float rowHeight = 62f;
    // Size of the bag button/icon.
    [SerializeField] private Vector2 bagIconSize = new Vector2(124f, 102f);
    // Screen offset for the bag icon.
    [SerializeField] private Vector2 bagIconOffset = new Vector2(148f, 18f);
    // Padding for the backpack title.
    [SerializeField] private UiPadding titlePadding;
    // Padding for the small hint line.
    [SerializeField] private UiPadding hintPadding;
    // Padding for the item list.
    [SerializeField] private UiPadding listPadding;
    // Font size for the title.
    [SerializeField] private int titleFontSize = 32;
    // Font size for the hint line.
    [SerializeField] private int hintFontSize = 18;
    // Font size for item rows.
    [SerializeField] private int itemFontSize = 24;

    // Current backpack instance.
    private static GlobalBackpackUI instance;
    // Backpack can only open after gameplay starts.
    private static bool enabledForGameSession;
    // Dialogues and choices can temporarily block backpack input.
    private static bool inputBlocked;
    // Shared item data survives scene loads even when each scene has its own UI object.
    private static readonly Dictionary<string, int> sharedItemCounts = new Dictionary<string, int>();
    // Shared display order for the backpack list.
    private static readonly List<string> sharedItemOrder = new List<string>();

    // True while the backpack panel is open.
    private bool inventoryOpen;
    // Cached item label style.
    private GUIStyle labelStyle;
    // Cached title style.
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

        AddShared(itemName, amount);
        GameAudioManager.PlayFetch();
    }

    public static void SetItemCount(string itemName, int count)
    {
        SetSharedCount(itemName, count);
    }

    public static void RemoveItem(string itemName, int amount = 1)
    {
        RemoveShared(itemName, amount);
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
        sharedItemCounts.Clear();
        sharedItemOrder.Clear();
    }

    public static void ExportItems(out string[] itemNames, out int[] itemAmounts)
    {
        // Save system exports the current backpack as parallel arrays for JsonUtility.
        itemNames = new string[sharedItemOrder.Count];
        itemAmounts = new int[sharedItemOrder.Count];
        for (int i = 0; i < sharedItemOrder.Count; i++)
        {
            string itemName = sharedItemOrder[i];
            itemNames[i] = itemName;
            itemAmounts[i] = sharedItemCounts.TryGetValue(itemName, out int amount) ? amount : 0;
        }
    }

    public static void ImportItems(string[] itemNames, int[] itemAmounts)
    {
        // Continue game rebuilds backpack contents from saved item names and counts.
        sharedItemCounts.Clear();
        sharedItemOrder.Clear();

        if (itemNames == null || itemAmounts == null)
        {
            return;
        }

        int count = Mathf.Min(itemNames.Length, itemAmounts.Length);
        for (int i = 0; i < count; i++)
        {
            if (!string.IsNullOrEmpty(itemNames[i]) && itemAmounts[i] > 0)
            {
                SetSharedCount(itemNames[i], itemAmounts[i]);
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
        AddShared(itemName, amount);
    }

    private static void AddShared(string itemName, int amount)
    {
        if (string.IsNullOrEmpty(itemName) || amount <= 0)
        {
            return;
        }

        if (!sharedItemCounts.ContainsKey(itemName))
        {
            sharedItemCounts.Add(itemName, 0);
            sharedItemOrder.Add(itemName);
        }

        sharedItemCounts[itemName] += amount;
    }

    private void SetCount(string itemName, int count)
    {
        SetSharedCount(itemName, count);
    }

    private static void SetSharedCount(string itemName, int count)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            return;
        }

        if (count <= 0)
        {
            sharedItemCounts.Remove(itemName);
            sharedItemOrder.Remove(itemName);
            return;
        }

        if (!sharedItemCounts.ContainsKey(itemName))
        {
            sharedItemOrder.Add(itemName);
        }

        sharedItemCounts[itemName] = count;
    }

    private void Remove(string itemName, int amount)
    {
        RemoveShared(itemName, amount);
    }

    private static void RemoveShared(string itemName, int amount)
    {
        if (string.IsNullOrEmpty(itemName) || amount <= 0 || !sharedItemCounts.ContainsKey(itemName))
        {
            return;
        }

        SetSharedCount(itemName, sharedItemCounts[itemName] - amount);
    }

    private bool IsMenuScene()
    {
        return IsMenuSceneName(SceneManager.GetActiveScene().name);
    }

    private bool IsMenuSceneName(string sceneName)
    {
        return string.Equals(sceneName, menuSceneName, System.StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(menuSceneAlias) &&
                string.Equals(sceneName, menuSceneAlias, System.StringComparison.OrdinalIgnoreCase));
    }

    private void DrawBackpack()
    {
        if (!inventoryOpen)
        {
            return;
        }

        float width = panelWidth;
        float height = Mathf.Min(panelMaxHeight, panelBaseHeight + Mathf.Max(1, sharedItemOrder.Count) * rowHeight);
        Rect rect = GameUiStyle.BackpackRect(width, height);

        GameUiStyle.DrawPanel(rect);
        Rect bagRect = new Rect(rect.x + rect.width - bagIconOffset.x, rect.y + bagIconOffset.y, bagIconSize.x, bagIconSize.y);
        GameUiStyle.DrawBag(bagRect);
        Rect titleRect = titlePadding.Apply(rect);
        Rect hintRect = hintPadding.Apply(rect);
        GUI.Label(new Rect(titleRect.x, titleRect.y, titleRect.width, 60f), "Backpack", GameUiStyle.LabelStyle(ref titleStyle, titleFontSize, TextAnchor.MiddleLeft, FontStyle.Bold));
        GUI.Label(new Rect(hintRect.x, hintRect.y, hintRect.width, 36f), "Press B to close", GameUiStyle.LabelStyle(ref labelStyle, hintFontSize, TextAnchor.MiddleLeft));

        float listY = rect.y + listPadding.top;

        if (sharedItemOrder.Count == 0)
        {
            GUI.Label(new Rect(rect.x + listPadding.left, listY, rect.width - listPadding.left - listPadding.right, 54f), "Empty", GameUiStyle.LabelStyle(ref labelStyle, itemFontSize, TextAnchor.MiddleLeft));
            return;
        }

        for (int i = 0; i < sharedItemOrder.Count; i++)
        {
            string itemName = sharedItemOrder[i];
            if (!sharedItemCounts.TryGetValue(itemName, out int count))
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
