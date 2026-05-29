// Main function: Runs the fairy memory side quest, including memory collection, the sliding puzzle, memory showcase, quest completion, and backpack rewards.

using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FairyMemorySideQuest : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] memories;

    [Header("Memory Text")]
    [SerializeField] private string[] memoryNames =
    {
        "Wooden Horse",
        "Fox Doll",
        "Tea Cup",
        "Colored Pencil",
        "Dinosaur Model"
    };
    [SerializeField] private string[] memoryLines =
    {
        "This wooden horse is an old memory.",
        "This fox doll was a favorite companion.",
        "This tea cup belonged to the family.",
        "These pencils came from far away.",
        "This dinosaur model was made together."
    };
    [SerializeField] private float interactDistance = 3.5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string interactPrompt = "Press E to search";
    [SerializeField] private string questTitle = "Side Quest: Fairy Memory Fragments";
    [SerializeField] private string restoreTaskText = "Restore the fairy memory";
    [SerializeField] private string inventoryName = "Memory Fragment";
    [SerializeField] private string fragmentRewardText = "Memory Fragment +1";
    [SerializeField] private string allFragmentsFoundText = "All memory fragments found. Complete the final challenge.";
    [SerializeField] private string unlockText = "Memory puzzle unlocked. Use WASD to restore the picture.";
    [SerializeField] private string completedText = "Memory restored:";
    [SerializeField] private float messageSeconds = 3f;
    [SerializeField] private string puzzleImageRelativePath = "new/pic.jpg";
    [SerializeField] private string memoryImageRelativeFolder = "new/memory";
    [SerializeField] private int memoryImageCount = 6;
    [SerializeField] private float memoryShowcaseSeconds = 10f;

    private const int GridSize = 4;
    private const int TileCount = GridSize * GridSize;

    private readonly List<string> queuedMessages = new List<string>();
    private bool[] collected;
    private bool active;
    private bool completed;
    private bool puzzleActive;
    private bool memoryShowcaseActive;
    private bool puzzleStartPending;
    private bool memoryFragmentsConsumed;
    private int collectedCount;
    private int nearbyMemoryIndex = -1;
    private string currentMessage;
    private float messageEndsAt;
    private float memoryShowcaseEndsAt;
    private Texture2D puzzleTexture;
    private Texture2D[] memoryShowcaseTextures;
    private int[] board;
    private int emptyIndex;
    private readonly List<Behaviour> disabledPlayerControllers = new List<Behaviour>();

    public bool IsCompleted
    {
        get { return completed; }
    }

    // Function: Initializes component references, cached state, and default runtime data.
    private void Awake()
    {
        PrepareArrays();
    }

    // Function: Runs the activate logic.
    public void Activate()
    {
        if (completed)
        {
            return;
        }

        SetPlayerMovementPaused(false);
        active = true;
        completed = false;
        puzzleActive = false;
        memoryShowcaseActive = false;
        puzzleStartPending = false;
        memoryFragmentsConsumed = false;
        collectedCount = 0;
        nearbyMemoryIndex = -1;
        currentMessage = null;
        queuedMessages.Clear();
        PrepareArrays();
        collected = new bool[memories.Length];
    }

    // Function: Updates input handling, interaction checks, and active gameplay flow each frame.
    private void Update()
    {
        UpdateMessageQueue();

        if (!active || completed)
        {
            return;
        }

        if (memoryShowcaseActive)
        {
            if (Time.time >= memoryShowcaseEndsAt)
            {
                FinishSideQuest();
            }

            return;
        }

        if (puzzleActive)
        {
            UpdatePuzzleInput();
            return;
        }

        nearbyMemoryIndex = FindNearbyMemoryIndex();
        if (nearbyMemoryIndex >= 0 && Input.GetKeyDown(interactKey))
        {
            CollectMemory(nearbyMemoryIndex);
        }
    }

    // Function: Collects memory and updates counters or cached references.
    private void CollectMemory(int index)
    {
        if (index < 0 || index >= collected.Length || collected[index])
        {
            return;
        }

        collected[index] = true;
        collectedCount++;
        GlobalBackpackUI.SetItemCount(inventoryName, collectedCount);
        QueueMessage(memoryLines[index]);
        QueueMessage(fragmentRewardText);

        if (collectedCount >= memories.Length && !puzzleStartPending && !puzzleActive)
        {
            puzzleStartPending = true;
            QueueMessage(allFragmentsFoundText);
        }
    }

    // Function: Starts the puzzle flow.
    private void StartPuzzle()
    {
        puzzleActive = true;
        puzzleStartPending = false;
        nearbyMemoryIndex = -1;
        SetPlayerMovementPaused(true);
        LoadPuzzleTexture();
        CreateSolvableBoard();
        QueueMessage(unlockText);
    }

    // Function: Completes puzzle and applies its result or reward.
    private void CompletePuzzle()
    {
        puzzleActive = false;
        memoryFragmentsConsumed = true;
        GlobalBackpackUI.RemoveAll(inventoryName);
        SetPlayerMovementPaused(false);
        StartMemoryShowcase();
        QueueMessage(completedText);
    }

    // Function: Starts the memory showcase flow.
    private void StartMemoryShowcase()
    {
        memoryShowcaseActive = true;
        memoryShowcaseEndsAt = Time.time + memoryShowcaseSeconds;
        LoadMemoryShowcaseTextures();
    }

    // Function: Finishes the side quest flow and performs cleanup.
    private void FinishSideQuest()
    {
        memoryShowcaseActive = false;
        completed = true;
        active = false;
        nearbyMemoryIndex = -1;
    }

    // Function: Updates puzzle input state, input, or presentation.
    private void UpdatePuzzleInput()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            TryMoveEmpty(0, 1);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            TryMoveEmpty(0, -1);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            TryMoveEmpty(1, 0);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            TryMoveEmpty(-1, 0);
        }

        if (IsBoardSolved())
        {
            CompletePuzzle();
        }
    }

    // Function: Tries to move empty and returns whether it succeeded.
    private void TryMoveEmpty(int sourceOffsetX, int sourceOffsetY)
    {
        int emptyX = emptyIndex % GridSize;
        int emptyY = emptyIndex / GridSize;
        int sourceX = emptyX + sourceOffsetX;
        int sourceY = emptyY + sourceOffsetY;

        if (sourceX < 0 || sourceX >= GridSize || sourceY < 0 || sourceY >= GridSize)
        {
            return;
        }

        int sourceIndex = sourceY * GridSize + sourceX;
        board[emptyIndex] = board[sourceIndex];
        board[sourceIndex] = TileCount - 1;
        emptyIndex = sourceIndex;
    }

    // Function: Checks whether board solved is true.
    private bool IsBoardSolved()
    {
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] != i)
            {
                return false;
            }
        }

        return true;
    }

    // Function: Creates the objects, textures, or UI needed for solvable board.
    private void CreateSolvableBoard()
    {
        board = new int[TileCount];
        for (int i = 0; i < TileCount; i++)
        {
            board[i] = i;
        }

        emptyIndex = TileCount - 1;
        int previousEmpty = -1;
        for (int i = 0; i < 160; i++)
        {
            List<int> candidates = GetMovableTileIndices();
            if (candidates.Count > 1)
            {
                candidates.Remove(previousEmpty);
            }

            int sourceIndex = candidates[Random.Range(0, candidates.Count)];
            previousEmpty = emptyIndex;
            board[emptyIndex] = board[sourceIndex];
            board[sourceIndex] = TileCount - 1;
            emptyIndex = sourceIndex;
        }

        if (IsBoardSolved())
        {
            TryMoveEmpty(1, 0);
        }
    }

    // Function: Gets or calculates movable tile indices.
    private List<int> GetMovableTileIndices()
    {
        List<int> candidates = new List<int>();
        int x = emptyIndex % GridSize;
        int y = emptyIndex / GridSize;
        AddCandidate(candidates, x - 1, y);
        AddCandidate(candidates, x + 1, y);
        AddCandidate(candidates, x, y - 1);
        AddCandidate(candidates, x, y + 1);
        return candidates;
    }

    // Function: Adds candidate.
    private static void AddCandidate(List<int> candidates, int x, int y)
    {
        if (x >= 0 && x < GridSize && y >= 0 && y < GridSize)
        {
            candidates.Add(y * GridSize + x);
        }
    }

    // Function: Finds nearby memory index objects or component references.
    private int FindNearbyMemoryIndex()
    {
        if (player == null || memories == null)
        {
            return -1;
        }

        int bestIndex = -1;
        float bestDistance = interactDistance;
        for (int i = 0; i < memories.Length; i++)
        {
            if (collected != null && i < collected.Length && collected[i])
            {
                continue;
            }

            Transform memory = memories[i];
            if (memory == null)
            {
                continue;
            }

            float distance = Vector3.Distance(player.position, memory.position);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    // Function: Runs the prepare arrays logic.
    private void PrepareArrays()
    {
        int count = memories != null ? memories.Length : 0;
        if (memoryNames == null || memoryNames.Length != count)
        {
            System.Array.Resize(ref memoryNames, count);
        }

        if (memoryLines == null || memoryLines.Length != count)
        {
            System.Array.Resize(ref memoryLines, count);
        }

        if (collected == null || collected.Length != count)
        {
            collected = new bool[count];
        }
    }

    // Function: Queues message for later display.
    private void QueueMessage(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            queuedMessages.Add(text);
        }
    }

    // Function: Updates message queue state, input, or presentation.
    private void UpdateMessageQueue()
    {
        if (!string.IsNullOrEmpty(currentMessage) && Time.time < messageEndsAt)
        {
            return;
        }

        if (queuedMessages.Count == 0)
        {
            currentMessage = null;
            if (puzzleStartPending)
            {
                StartPuzzle();
            }

            return;
        }

        currentMessage = queuedMessages[0];
        queuedMessages.RemoveAt(0);
        messageEndsAt = Time.time + 3f;
    }

    // Function: Loads puzzle texture resources or controllers.
    private void LoadPuzzleTexture()
    {
        if (puzzleTexture != null)
        {
            return;
        }

        string absolutePath = Path.Combine(Application.dataPath, puzzleImageRelativePath);
        if (!File.Exists(absolutePath))
        {
            return;
        }

        byte[] bytes = File.ReadAllBytes(absolutePath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (texture.LoadImage(bytes))
        {
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            puzzleTexture = texture;
        }
    }

    // Function: Loads memory showcase textures resources or controllers.
    private void LoadMemoryShowcaseTextures()
    {
        if (memoryShowcaseTextures != null && memoryShowcaseTextures.Length == memoryImageCount)
        {
            return;
        }

        memoryShowcaseTextures = new Texture2D[memoryImageCount];
        for (int i = 0; i < memoryImageCount; i++)
        {
            string fileName = (i + 1).ToString() + ".png";
            string absolutePath = Path.Combine(Application.dataPath, memoryImageRelativeFolder, fileName);
            if (!File.Exists(absolutePath))
            {
                continue;
            }

            byte[] bytes = File.ReadAllBytes(absolutePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(bytes))
            {
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                memoryShowcaseTextures[i] = texture;
            }
        }
    }

    // Function: Draws this script's IMGUI prompts, panels, and dialogue.
    private void OnGUI()
    {
        if (!active)
        {
            if (!string.IsNullOrEmpty(currentMessage) && Time.time < messageEndsAt)
            {
                DrawMessageBox(currentMessage);
            }

            return;
        }

        DrawQuestPanel();

        if (!completed && !puzzleActive && !memoryShowcaseActive && nearbyMemoryIndex >= 0)
        {
            DrawCenteredLabel(GetMemoryPrompt(nearbyMemoryIndex), Screen.height * 0.68f, 28);
        }

        if (!string.IsNullOrEmpty(currentMessage) && Time.time < messageEndsAt)
        {
            DrawMessageBox(currentMessage);
        }

        if (puzzleActive)
        {
            DrawPuzzle();
        }

        if (memoryShowcaseActive)
        {
            DrawMemoryShowcase();
        }
    }

    // Function: Gets or calculates memory prompt.
    private string GetMemoryPrompt(int index)
    {
        if (index < 0 || index >= memoryNames.Length || string.IsNullOrEmpty(memoryNames[index]))
        {
            return interactPrompt;
        }

        return interactPrompt + ": " + memoryNames[index];
    }

    // Function: Draws the UI elements for quest panel.
    private void DrawQuestPanel()
    {
        float width = 430f;
        float height = 150f;
        Rect rect = GameUiStyle.SideQuestRect(width, height);
        GameUiStyle.DrawPanel(rect);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        titleStyle.normal.textColor = Color.white;

        GUIStyle taskStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        taskStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f);

        int visibleFragmentCount = memoryFragmentsConsumed ? 0 : collectedCount;
        int memoryCount = memories != null ? memories.Length : 0;
        string completionMark = collectedCount >= memoryCount ? " done" : string.Empty;
        GUI.Label(new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, 44f), questTitle, titleStyle);
        GUI.Label(new Rect(rect.x + 14f, rect.y + 62f, rect.width - 28f, 28f), restoreTaskText + completionMark, taskStyle);
        GUI.Label(new Rect(rect.x + 14f, rect.y + 96f, rect.width - 28f, 28f), "Memory fragments: " + visibleFragmentCount + "/" + memoryCount, taskStyle);
    }

    // Function: Draws the UI elements for backpack panel.
    private void DrawBackpackPanel()
    {
        Rect panelRect = GameUiStyle.BackpackRect(180f, 94f);
        GameUiStyle.DrawPanel(panelRect);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleLeft
        };
        labelStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 8f, 120f, 22f), "Backpack", labelStyle);

        int availableFragmentCount = memoryFragmentsConsumed ? 0 : collectedCount;
        if (availableFragmentCount <= 0)
        {
            GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 42f, panelRect.width - 24f, 24f), "Empty", labelStyle);
            return;
        }

        Rect slotRect = new Rect(panelRect.x + 12f, panelRect.y + 34f, 130f, 48f);
        GUI.Box(slotRect, GUIContent.none);
        GUI.Label(new Rect(slotRect.x + 8f, slotRect.y + 6f, slotRect.width - 16f, 20f), inventoryName, labelStyle);
        GUI.Label(new Rect(slotRect.x + 8f, slotRect.y + 26f, slotRect.width - 16f, 20f), "x" + availableFragmentCount, labelStyle);
    }

    // Function: Draws the UI elements for puzzle.
    private void DrawPuzzle()
    {
        if (board == null)
        {
            return;
        }

        bool sideBySide = puzzleTexture != null && Screen.width >= 920;
        float size = sideBySide
            ? Mathf.Min(Screen.width * 0.44f, Screen.height * 0.68f)
            : Mathf.Min(Screen.width * 0.72f, Screen.height * 0.58f);
        float referenceSize = sideBySide ? Mathf.Min(size * 0.62f, 280f) : Mathf.Min(size * 0.42f, 180f);
        float panelWidth = sideBySide ? size + referenceSize + 76f : size + 40f;
        float panelHeight = sideBySide ? size + 92f : size + referenceSize + 126f;
        Rect panel = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);
        GUI.Box(panel, GUIContent.none);

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            wordWrap = true
        };
        hintStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(panel.x + 16f, panel.y + 10f, panel.width - 32f, 32f), "Use WASD to move tiles", hintStyle);

        Rect grid = new Rect(panel.x + 20f, panel.y + 50f, size, size);
        DrawReferenceImage(panel, grid, referenceSize, sideBySide);

        float tileSize = size / GridSize;
        for (int boardIndex = 0; boardIndex < board.Length; boardIndex++)
        {
            int tile = board[boardIndex];
            if (tile == TileCount - 1)
            {
                continue;
            }

            int x = boardIndex % GridSize;
            int y = boardIndex / GridSize;
            Rect tileRect = new Rect(grid.x + x * tileSize + 1f, grid.y + y * tileSize + 1f, tileSize - 2f, tileSize - 2f);

            if (puzzleTexture != null)
            {
                int sourceX = tile % GridSize;
                int sourceY = tile / GridSize;
                Rect texCoords = new Rect(sourceX / (float)GridSize, 1f - ((sourceY + 1f) / GridSize), 1f / GridSize, 1f / GridSize);
                GUI.DrawTextureWithTexCoords(tileRect, puzzleTexture, texCoords, true);
            }
            else
            {
                GUI.Box(tileRect, (tile + 1).ToString());
            }
        }
    }

    // Function: Draws the UI elements for reference image.
    private void DrawReferenceImage(Rect panel, Rect grid, float referenceSize, bool sideBySide)
    {
        if (puzzleTexture == null)
        {
            return;
        }

        Rect referenceRect = sideBySide
            ? new Rect(grid.xMax + 24f, grid.y + 42f, referenceSize, referenceSize)
            : new Rect(panel.x + (panel.width - referenceSize) * 0.5f, grid.yMax + 16f, referenceSize, referenceSize);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18
        };
        labelStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(referenceRect.x, referenceRect.y - 28f, referenceRect.width, 24f), "Reference", labelStyle);
        GUI.Box(referenceRect, GUIContent.none);
        GUI.DrawTexture(new Rect(referenceRect.x + 4f, referenceRect.y + 4f, referenceRect.width - 8f, referenceRect.height - 8f), puzzleTexture, ScaleMode.ScaleToFit, true);
    }

    // Function: Draws the UI elements for memory showcase.
    private void DrawMemoryShowcase()
    {
        if (memoryShowcaseTextures == null || memoryShowcaseTextures.Length == 0)
        {
            return;
        }

        int count = memoryShowcaseTextures.Length;
        float gap = 10f;
        float availableWidth = Mathf.Max(240f, Screen.width - 80f);
        float slotWidth = (availableWidth - gap * (count - 1)) / count;
        float slotHeight = Mathf.Min(Screen.height * 0.32f, slotWidth * 1.25f);
        float totalWidth = slotWidth * count + gap * (count - 1);
        float startX = (Screen.width - totalWidth) * 0.5f;
        float y = Screen.height * 0.5f - slotHeight * 0.5f;

        Rect panelRect = new Rect(startX - 18f, y - 18f, totalWidth + 36f, slotHeight + 36f);
        GUI.Box(panelRect, GUIContent.none);

        for (int i = 0; i < count; i++)
        {
            Rect slotRect = new Rect(startX + i * (slotWidth + gap), y, slotWidth, slotHeight);
            GUI.Box(slotRect, GUIContent.none);

            Texture2D texture = memoryShowcaseTextures[i];
            if (texture == null)
            {
                continue;
            }

            Rect imageRect = new Rect(slotRect.x + 4f, slotRect.y + 4f, slotRect.width - 8f, slotRect.height - 8f);
            GUI.DrawTexture(imageRect, texture, ScaleMode.ScaleToFit, true);
        }
    }

    // Function: Sets player movement paused.
    private void SetPlayerMovementPaused(bool paused)
    {
        if (paused)
        {
            if (player == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = player.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || !behaviour.enabled || !IsPlayerController(behaviour))
                {
                    continue;
                }

                behaviour.enabled = false;
                disabledPlayerControllers.Add(behaviour);
            }

            return;
        }

        for (int i = 0; i < disabledPlayerControllers.Count; i++)
        {
            Behaviour behaviour = disabledPlayerControllers[i];
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        disabledPlayerControllers.Clear();
    }

    // Function: Checks whether player controller is true.
    private static bool IsPlayerController(MonoBehaviour behaviour)
    {
        string typeName = behaviour.GetType().Name;
        return typeName == "PlayerCharacterController" || typeName == "DemoCharacter";
    }

    // Function: Stops running routines, unregisters events, and restores temporary state when disabled.
    private void OnDisable()
    {
        SetPlayerMovementPaused(false);
    }

    // Function: Cleans up temporary state when this object is destroyed.
    private void OnDestroy()
    {
        SetPlayerMovementPaused(false);
    }

    // Function: Draws the UI elements for message box.
    private static void DrawMessageBox(string text)
    {
        Rect rect = GameUiStyle.SystemPromptRect(760f, 92f);
        GameUiStyle.DrawPanel(rect);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24,
            wordWrap = true
        };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(rect.x + 18f, rect.y + 12f, rect.width - 36f, rect.height - 24f), text, style);
    }

    // Function: Draws the UI elements for centered label.
    private static void DrawCenteredLabel(string text, float y, int fontSize)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize,
            wordWrap = true
        };
        style.normal.textColor = Color.white;
        Rect rect = GameUiStyle.InteractionPromptRect(520f, 60f);
        GameUiStyle.DrawPanel(rect);
        GUI.Label(rect, text, style);
    }
}
