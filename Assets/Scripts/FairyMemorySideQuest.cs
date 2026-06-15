using System.Collections.Generic;
using UnityEngine;

public class FairyMemorySideQuest : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform treeHouse;
    [SerializeField] private Transform[] memories;

    [Header("Tree House Choice")]
    [SerializeField] private float enterDistance = 5.5f;
    [SerializeField] private float verticalToleranceBelow = 1.2f;
    [SerializeField] private float verticalToleranceAbove = 4.5f;
    [SerializeField] private string choiceTitle = "System Choice";
    [SerializeField] private string choiceA = "A: Explore fairy memory fragments";
    [SerializeField] private string choiceB = "B: Skip and keep exploring";
    [SerializeField] private string choiceHint = "Press A / B to choose";

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
    [SerializeField] private Texture2D puzzleImage;
    [SerializeField] private Texture2D[] memoryImages;
    [SerializeField] private float memoryShowcaseSeconds = 10f;

    [Header("Choice UI")]
    [SerializeField] private float choicePanelMaxWidth = 980f;
    [SerializeField] private float choicePanelScreenPadding = 80f;
    [SerializeField] private float choicePanelCenterOffsetY = -220f;
    [SerializeField] private float choicePanelHeight = 440f;
    [SerializeField] private Rect choiceTitleRect = new Rect(42f, 110f, -72f, 68f);
    [SerializeField] private Rect choiceARect = new Rect(140f, 160f, -104f, 92f);
    [SerializeField] private Rect choiceBRect = new Rect(140f, 250f, -104f, 92f);
    [SerializeField] private Rect choiceHintRect = new Rect(0f, -108f, -150f, 48f);
    [SerializeField] private int choiceTitleFontSize = 30;
    [SerializeField] private int choiceOptionFontSize = 28;
    [SerializeField] private int choiceHintFontSize = 22;

    [Header("Quest UI")]
    [SerializeField] private Vector2 questPanelSize = new Vector2(630f, 260f);
    [SerializeField] private Rect questTitleRect = new Rect(120f, 90f, -44f, 76f);
    [SerializeField] private Rect questTaskRect = new Rect(120f, 130f, -44f, 58f);
    [SerializeField] private Rect questFragmentRect = new Rect(120f, 170f, -44f, 58f);
    [SerializeField] private int questTitleFontSize = 22;
    [SerializeField] private int questTaskFontSize = 22;

    [Header("Puzzle UI")]
    [SerializeField] private float puzzleSideBySideScreenWidth = 920f;
    [SerializeField] private Vector2 puzzleSideBySideSizeRatio = new Vector2(0.44f, 0.68f);
    [SerializeField] private Vector2 puzzleStackedSizeRatio = new Vector2(0.72f, 0.58f);
    [SerializeField] private float referenceSideBySideScale = 0.62f;
    [SerializeField] private float referenceSideBySideMaxSize = 280f;
    [SerializeField] private float referenceStackedScale = 0.42f;
    [SerializeField] private float referenceStackedMaxSize = 180f;
    [SerializeField] private Vector2 puzzleSideBySidePanelPadding = new Vector2(76f, 92f);
    [SerializeField] private Vector2 puzzleStackedPanelPadding = new Vector2(40f, 126f);
    [SerializeField] private Rect puzzleHintRect = new Rect(16f, 10f, -32f, 40f);
    [SerializeField] private Vector2 puzzleGridOffset = new Vector2(20f, 50f);
    [SerializeField] private float puzzleTileGap = 2f;
    [SerializeField] private Vector2 referenceSideBySideOffset = new Vector2(24f, 42f);
    [SerializeField] private float referenceStackedOffsetY = 16f;
    [SerializeField] private Rect referenceLabelRect = new Rect(0f, -28f, 0f, 24f);
    [SerializeField] private float referenceImagePadding = 4f;
    [SerializeField] private int puzzleHintFontSize = 20;
    [SerializeField] private int referenceLabelFontSize = 18;

    [Header("Memory Showcase UI")]
    [SerializeField] private float memoryShowcaseScreenPadding = 80f;
    [SerializeField] private float memoryShowcaseMinWidth = 240f;
    [SerializeField] private float memoryShowcaseGap = 10f;
    [SerializeField] private float memoryShowcaseHeightRatio = 0.32f;
    [SerializeField] private float memoryShowcaseSlotAspect = 1.25f;
    [SerializeField] private float memoryShowcasePanelPadding = 18f;
    [SerializeField] private float memoryShowcaseImagePadding = 4f;

    [Header("Prompt UI")]
    [SerializeField] private Vector2 messageBoxSize = new Vector2(920f, 156f);
    [SerializeField] private Rect messageTextRect = new Rect(18f, 12f, -36f, -24f);
    [SerializeField] private int messageFontSize = 24;
    [SerializeField] private Vector2 centeredPromptSize = new Vector2(680f, 118f);
    [SerializeField] private int memoryPromptFontSize = 28;
    [SerializeField] private int puzzleExitPromptFontSize = 22;

    private const int GridSize = 4;
    private const int TileCount = GridSize * GridSize;

    private readonly List<string> queuedMessages = new List<string>();
    private readonly Dictionary<Transform, Collider[]> memoryColliderCache = new Dictionary<Transform, Collider[]>();
    private readonly Dictionary<Transform, Renderer[]> memoryRendererCache = new Dictionary<Transform, Renderer[]>();
    private bool[] collected;
    private bool active;
    private bool completed;
    private bool choiceVisible;
    private bool choiceResolved;
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
    private GUIStyle choiceTitleStyle;
    private GUIStyle choiceOptionStyle;
    private GUIStyle choiceHintStyle;
    private GUIStyle questTitleStyle;
    private GUIStyle questTaskStyle;
    private GUIStyle puzzleHintStyle;
    private GUIStyle referenceLabelStyle;
    private GUIStyle messageStyle;
    private GUIStyle centeredLabelStyle;

    public bool IsCompleted
    {
        get { return completed; }
    }

    private void Awake()
    {
        CheckQuestSetup();
    }

    public void Activate()
    {
        if (completed)
        {
            return;
        }

        active = true;
        completed = false;
        SetPlayerMovementPaused(false);
        CheckQuestSetup();

        if (collected == null || collected.Length != memories.Length)
        {
            collected = new bool[memories.Length];
        }

        if (collectedCount >= memories.Length && !memoryShowcaseActive)
        {
            puzzleActive = true;
            puzzleStartPending = false;
            nearbyMemoryIndex = -1;
            SetPlayerMovementPaused(true);
            LoadPuzzleTexture();

            if (board == null || board.Length != TileCount)
            {
                CreateSolvableBoard();
            }
        }
    }

    private void Update()
    {
        UpdateTreeHouseChoice();
        UpdateMessageQueue();

        if (!active || completed)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            ReturnToTreeHouseEntrance();
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

    private void UpdateTreeHouseChoice()
    {
        if (completed)
        {
            choiceResolved = true;
            choiceVisible = false;
            return;
        }

        if (choiceResolved || active || player == null || treeHouse == null)
        {
            return;
        }

        if (!choiceVisible && IsPlayerInsideTreeHouse())
        {
            choiceVisible = true;
        }

        if (!choiceVisible)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChooseSideQuest();
        }
        else if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            SkipChoice();
        }
    }

    private void ChooseSideQuest()
    {
        Activate();
        choiceResolved = true;
        choiceVisible = false;
    }

    private void SkipChoice()
    {
        choiceResolved = true;
        choiceVisible = false;
    }

    private void CollectMemory(int index)
    {
        if (index < 0 || index >= collected.Length || collected[index])
        {
            return;
        }

        collected[index] = true;
        collectedCount++;
        GlobalBackpackUI.SetItemCount(inventoryName, collectedCount);
        GameAudioManager.PlayFetch();
        QueueMessage(GetMemoryLine(index));
        QueueMessage(fragmentRewardText);

        if (collectedCount >= memories.Length && !puzzleStartPending && !puzzleActive)
        {
            puzzleStartPending = true;
            QueueMessage(allFragmentsFoundText);
        }
    }

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

    private void CompletePuzzle()
    {
        puzzleActive = false;
        memoryFragmentsConsumed = true;
        GlobalBackpackUI.RemoveAll(inventoryName);
        SetPlayerMovementPaused(false);
        StartMemoryShowcase();
        GameAudioManager.PlaySuccess();
        QueueMessage(completedText);
    }

    private void StartMemoryShowcase()
    {
        memoryShowcaseActive = true;
        memoryShowcaseEndsAt = Time.time + memoryShowcaseSeconds;
        LoadMemoryShowcaseTextures();
    }

    private void FinishSideQuest()
    {
        SetPlayerMovementPaused(false);
        memoryShowcaseActive = false;
        puzzleActive = false;
        puzzleStartPending = false;
        completed = true;
        active = false;
        nearbyMemoryIndex = -1;
    }

    private void ReturnToTreeHouseEntrance()
    {
        SetPlayerMovementPaused(false);
        active = false;
        choiceVisible = false;
        choiceResolved = false;
        puzzleActive = false;
        memoryShowcaseActive = false;
        puzzleStartPending = false;
        nearbyMemoryIndex = -1;
        currentMessage = null;
        queuedMessages.Clear();
    }

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

    private static void AddCandidate(List<int> candidates, int x, int y)
    {
        if (x >= 0 && x < GridSize && y >= 0 && y < GridSize)
        {
            candidates.Add(y * GridSize + x);
        }
    }

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

            float distance = GetDistanceToMemory(player.position, memory);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private float GetDistanceToMemory(Vector3 position, Transform memory)
    {
        float bestDistance = Vector3.Distance(position, memory.position);

        Collider[] colliders = GetMemoryColliders(memory);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider itemCollider = colliders[i];
            if (itemCollider == null)
            {
                continue;
            }

            if (!CanUseClosestPoint(itemCollider))
            {
                continue;
            }

            bestDistance = Mathf.Min(bestDistance, Vector3.Distance(position, itemCollider.ClosestPoint(position)));
        }

        Renderer[] renderers = GetMemoryRenderers(memory);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer itemRenderer = renderers[i];
            if (itemRenderer == null)
            {
                continue;
            }

            bestDistance = Mathf.Min(bestDistance, Vector3.Distance(position, itemRenderer.bounds.ClosestPoint(position)));
        }

        return bestDistance;
    }

    private static bool CanUseClosestPoint(Collider itemCollider)
    {
        if (itemCollider is BoxCollider || itemCollider is SphereCollider || itemCollider is CapsuleCollider)
        {
            return true;
        }

        MeshCollider meshCollider = itemCollider as MeshCollider;
        return meshCollider != null && meshCollider.convex;
    }

    private Collider[] GetMemoryColliders(Transform memory)
    {
        if (!memoryColliderCache.TryGetValue(memory, out Collider[] colliders))
        {
            colliders = memory.GetComponentsInChildren<Collider>(true);
            memoryColliderCache[memory] = colliders;
        }

        return colliders;
    }

    private Renderer[] GetMemoryRenderers(Transform memory)
    {
        if (!memoryRendererCache.TryGetValue(memory, out Renderer[] renderers))
        {
            renderers = memory.GetComponentsInChildren<Renderer>(true);
            memoryRendererCache[memory] = renderers;
        }

        return renderers;
    }

    private void SetMemorySlots()
    {
        int count = memories != null ? memories.Length : 0;
        if (count == 0)
        {
            if (collected == null || collected.Length != 0)
            {
                collected = new bool[0];
            }

            return;
        }

        if (collected == null || collected.Length != count)
        {
            collected = new bool[count];
        }
    }

    private bool IsPlayerInsideTreeHouse()
    {
        Vector3 toPlayer = player.position - treeHouse.position;
        Vector2 horizontal = new Vector2(toPlayer.x, toPlayer.z);
        bool closeEnough = horizontal.magnitude <= enterDistance;
        bool verticalOk = player.position.y >= treeHouse.position.y - verticalToleranceBelow &&
            player.position.y <= treeHouse.position.y + verticalToleranceAbove;

        return closeEnough && verticalOk;
    }

    private void QueueMessage(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            queuedMessages.Add(text);
        }
    }

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
        messageEndsAt = Time.time + messageSeconds;
        GameAudioManager.PlayKnob();
    }

    private void LoadPuzzleTexture()
    {
        if (puzzleTexture != null || puzzleImage == null)
        {
            return;
        }

        puzzleTexture = puzzleImage;
        puzzleTexture.wrapMode = TextureWrapMode.Clamp;
        puzzleTexture.filterMode = FilterMode.Bilinear;
    }

    private void LoadMemoryShowcaseTextures()
    {
        if (memoryShowcaseTextures != null || memoryImages == null)
        {
            return;
        }

        memoryShowcaseTextures = new Texture2D[memoryImages.Length];
        for (int i = 0; i < memoryImages.Length; i++)
        {
            Texture2D texture = memoryImages[i];
            if (texture == null)
            {
                continue;
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            memoryShowcaseTextures[i] = texture;
        }
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        if (choiceVisible && !choiceResolved)
        {
            DrawTreeHouseChoicePanel();
            return;
        }

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
            DrawCenteredLabel(GetMemoryPrompt(nearbyMemoryIndex), memoryPromptFontSize);
        }

        if (!string.IsNullOrEmpty(currentMessage) && Time.time < messageEndsAt)
        {
            DrawMessageBox(currentMessage);
        }

        if (puzzleActive)
        {
            DrawPuzzle();
            DrawCenteredLabel("Press B to exit", puzzleExitPromptFontSize);
        }

        if (memoryShowcaseActive)
        {
            DrawMemoryShowcase();
        }
    }

    private void DrawTreeHouseChoicePanel()
    {
        float width = Mathf.Min(choicePanelMaxWidth, Screen.width - choicePanelScreenPadding);
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.5f + choicePanelCenterOffsetY, width, choicePanelHeight);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle titleStyle = GameUiStyle.LabelStyle(ref choiceTitleStyle, choiceTitleFontSize, TextAnchor.MiddleCenter, FontStyle.Bold);
        GUIStyle optionStyle = GameUiStyle.LabelStyle(ref choiceOptionStyle, choiceOptionFontSize, TextAnchor.MiddleLeft, FontStyle.Normal, true);
        GUIStyle hintStyle = GameUiStyle.LabelStyle(ref choiceHintStyle, choiceHintFontSize, TextAnchor.MiddleRight);
        hintStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

        GUI.Label(InnerRect(rect, choiceTitleRect), choiceTitle, titleStyle);
        GUI.Label(InnerRect(rect, choiceARect), choiceA, optionStyle);
        GUI.Label(InnerRect(rect, choiceBRect), choiceB, optionStyle);
        GUI.Label(InnerRect(rect, choiceHintRect), choiceHint, hintStyle);
    }

    private string GetMemoryPrompt(int index)
    {
        if (index < 0 || index >= memoryNames.Length || string.IsNullOrEmpty(memoryNames[index]))
        {
            return interactPrompt;
        }

        return interactPrompt + ": " + memoryNames[index];
    }

    private string GetMemoryLine(int index)
    {
        if (memoryLines == null || index < 0 || index >= memoryLines.Length)
        {
            Debug.LogError("FairyMemorySideQuest is missing Memory Lines entry " + index + ". Fill it in the Inspector.", this);
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(memoryLines[index]))
        {
            Debug.LogError("FairyMemorySideQuest has an empty Memory Lines entry " + index + ". Fill it in the Inspector.", this);
            return string.Empty;
        }

        return memoryLines[index];
    }

    private void DrawQuestPanel()
    {
        Rect rect = GameUiStyle.SideQuestRect(questPanelSize.x, questPanelSize.y);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle titleStyle = GameUiStyle.LabelStyle(ref questTitleStyle, questTitleFontSize, TextAnchor.UpperLeft, FontStyle.Normal, true);
        GUIStyle taskStyle = GameUiStyle.LabelStyle(ref questTaskStyle, questTaskFontSize, TextAnchor.UpperLeft, FontStyle.Normal, true);
        taskStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f);

        int visibleFragmentCount = memoryFragmentsConsumed ? 0 : collectedCount;
        int memoryCount = memories != null ? memories.Length : 0;
        string completionMark = collectedCount >= memoryCount ? " done" : string.Empty;
        GUI.Label(InnerRect(rect, questTitleRect), questTitle, titleStyle);
        GUI.Label(InnerRect(rect, questTaskRect), restoreTaskText + completionMark, taskStyle);
        GUI.Label(InnerRect(rect, questFragmentRect), "Memory fragments: " + visibleFragmentCount + "/" + memoryCount, taskStyle);
    }

    private void DrawPuzzle()
    {
        if (board == null)
        {
            return;
        }

        bool sideBySide = puzzleTexture != null && Screen.width >= puzzleSideBySideScreenWidth;
        float size = sideBySide
            ? Mathf.Min(Screen.width * puzzleSideBySideSizeRatio.x, Screen.height * puzzleSideBySideSizeRatio.y)
            : Mathf.Min(Screen.width * puzzleStackedSizeRatio.x, Screen.height * puzzleStackedSizeRatio.y);
        float referenceSize = sideBySide ? Mathf.Min(size * referenceSideBySideScale, referenceSideBySideMaxSize) : Mathf.Min(size * referenceStackedScale, referenceStackedMaxSize);
        float panelWidth = sideBySide ? size + referenceSize + puzzleSideBySidePanelPadding.x : size + puzzleStackedPanelPadding.x;
        float panelHeight = sideBySide ? size + puzzleSideBySidePanelPadding.y : size + referenceSize + puzzleStackedPanelPadding.y;
        Rect panel = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);
        GUI.Box(panel, GUIContent.none);

        GUIStyle hintStyle = GameUiStyle.LabelStyle(ref puzzleHintStyle, puzzleHintFontSize, TextAnchor.MiddleCenter, FontStyle.Normal, true);
        GUI.Label(InnerRect(panel, puzzleHintRect), "Use WASD to move tiles", hintStyle);

        Rect grid = new Rect(panel.x + puzzleGridOffset.x, panel.y + puzzleGridOffset.y, size, size);
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
            float tileInset = puzzleTileGap * 0.5f;
            Rect tileRect = new Rect(grid.x + x * tileSize + tileInset, grid.y + y * tileSize + tileInset, tileSize - puzzleTileGap, tileSize - puzzleTileGap);

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

    private void DrawReferenceImage(Rect panel, Rect grid, float referenceSize, bool sideBySide)
    {
        if (puzzleTexture == null)
        {
            return;
        }

        Rect referenceRect = sideBySide
            ? new Rect(grid.xMax + referenceSideBySideOffset.x, grid.y + referenceSideBySideOffset.y, referenceSize, referenceSize)
            : new Rect(panel.x + (panel.width - referenceSize) * 0.5f, grid.yMax + referenceStackedOffsetY, referenceSize, referenceSize);

        GUIStyle labelStyle = GameUiStyle.LabelStyle(ref referenceLabelStyle, referenceLabelFontSize, TextAnchor.MiddleCenter);

        GUI.Label(new Rect(referenceRect.x + referenceLabelRect.x, referenceRect.y + referenceLabelRect.y, referenceRect.width + referenceLabelRect.width, referenceLabelRect.height), "Reference", labelStyle);
        GUI.Box(referenceRect, GUIContent.none);
        GUI.DrawTexture(new Rect(referenceRect.x + referenceImagePadding, referenceRect.y + referenceImagePadding, referenceRect.width - referenceImagePadding * 2f, referenceRect.height - referenceImagePadding * 2f), puzzleTexture, ScaleMode.ScaleToFit, true);
    }

    private void DrawMemoryShowcase()
    {
        if (memoryShowcaseTextures == null || memoryShowcaseTextures.Length == 0)
        {
            return;
        }

        int count = memoryShowcaseTextures.Length;
        float gap = memoryShowcaseGap;
        float availableWidth = Mathf.Max(memoryShowcaseMinWidth, Screen.width - memoryShowcaseScreenPadding);
        float slotWidth = (availableWidth - gap * (count - 1)) / count;
        float slotHeight = Mathf.Min(Screen.height * memoryShowcaseHeightRatio, slotWidth * memoryShowcaseSlotAspect);
        float totalWidth = slotWidth * count + gap * (count - 1);
        float startX = (Screen.width - totalWidth) * 0.5f;
        float y = Screen.height * 0.5f - slotHeight * 0.5f;

        Rect panelRect = new Rect(startX - memoryShowcasePanelPadding, y - memoryShowcasePanelPadding, totalWidth + memoryShowcasePanelPadding * 2f, slotHeight + memoryShowcasePanelPadding * 2f);
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

            Rect paddedRect = new Rect(
                slotRect.x + memoryShowcaseImagePadding,
                slotRect.y + memoryShowcaseImagePadding,
                slotRect.width - memoryShowcaseImagePadding * 2f,
                slotRect.height - memoryShowcaseImagePadding * 2f);
            Rect imageRect = FitRectToTexture(paddedRect, texture);
            GUI.DrawTexture(imageRect, texture, ScaleMode.ScaleToFit, true);
        }
    }

    private static Rect FitRectToTexture(Rect bounds, Texture2D texture)
    {
        if (texture == null || texture.width <= 0 || texture.height <= 0 || bounds.width <= 0f || bounds.height <= 0f)
        {
            return bounds;
        }

        float textureAspect = (float)texture.width / texture.height;
        float boundsAspect = bounds.width / bounds.height;

        if (textureAspect > boundsAspect)
        {
            float height = bounds.width / textureAspect;
            float y = bounds.y + (bounds.height - height) * 0.5f;
            return new Rect(bounds.x, y, bounds.width, height);
        }

        float width = bounds.height * textureAspect;
        float x = bounds.x + (bounds.width - width) * 0.5f;
        return new Rect(x, bounds.y, width, bounds.height);
    }

    private void SetPlayerMovementPaused(bool paused)
    {
        if (paused)
        {
            AquariusMax.Fae.demo.DemoCharacter.SetControlLocked(true);
            return;
        }

        AquariusMax.Fae.demo.DemoCharacter.ResetControlFlags();
    }

    private void OnDisable()
    {
        SetPlayerMovementPaused(false);
    }

    private void OnDestroy()
    {
        SetPlayerMovementPaused(false);
    }

    private void DrawMessageBox(string text)
    {
        Rect rect = GameUiStyle.SystemPromptRect(messageBoxSize.x, messageBoxSize.y);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle style = GameUiStyle.LabelStyle(ref messageStyle, messageFontSize, TextAnchor.MiddleCenter, FontStyle.Normal, true);
        GUI.Label(InnerRect(rect, messageTextRect), text, style);
    }

    private void DrawCenteredLabel(string text, int fontSize)
    {
        GUIStyle style = GameUiStyle.LabelStyle(ref centeredLabelStyle, fontSize, TextAnchor.MiddleCenter, FontStyle.Normal, true);
        Rect rect = GameUiStyle.InteractionPromptRect(centeredPromptSize.x, centeredPromptSize.y);
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(rect, text, style);
    }

    private static Rect InnerRect(Rect parent, Rect localRect)
    {
        float y = localRect.y >= 0f ? parent.y + localRect.y : parent.yMax + localRect.y;
        float width = localRect.width >= 0f ? localRect.width : parent.width + localRect.width;
        float height = localRect.height >= 0f ? localRect.height : parent.height + localRect.height;
        return new Rect(parent.x + localRect.x, y, width, height);
    }

    private void CheckQuestSetup()
    {
        SetMemorySlots();
        ValidateMemoryText();
    }

    private void ValidateMemoryText()
    {
        int count = memories != null ? memories.Length : 0;
        if (count == 0)
        {
            return;
        }

        if (memoryNames == null || memoryNames.Length != count || memoryLines == null || memoryLines.Length != count)
        {
            Debug.LogError("FairyMemorySideQuest memory text arrays must match Memories length. Fill Memory Names and Memory Lines in the Inspector.", this);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (string.IsNullOrWhiteSpace(memoryNames[i]) || string.IsNullOrWhiteSpace(memoryLines[i]))
            {
                Debug.LogError("FairyMemorySideQuest has empty memory text at index " + i + ". Fill it in the Inspector.", this);
                return;
            }
        }
    }

    private void ResetQuestProgress()
    {
        puzzleActive = false;
        memoryShowcaseActive = false;
        puzzleStartPending = false;
        memoryFragmentsConsumed = false;
        collectedCount = 0;
        nearbyMemoryIndex = -1;
        currentMessage = null;
        queuedMessages.Clear();
    }
}
