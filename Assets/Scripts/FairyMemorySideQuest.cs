using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FairyMemorySideQuest : MonoBehaviour
{
    private static readonly string[] DefaultMemoryNames =
    {
        "Wooden Horse",
        "Fox Doll",
        "Tea Cup",
        "Colored Pencil",
        "Dinosaur Model"
    };

    private static readonly string[] DefaultMemoryLines =
    {
        "This wooden horse is an old memory.",
        "This fox doll was a favorite companion.",
        "This tea cup belonged to the family.",
        "These pencils came from far away.",
        "This dinosaur model was made together."
    };

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
    [SerializeField] private string puzzleImageRelativePath = "new/pic.jpg";
    [SerializeField] private string memoryImageRelativeFolder = "new/memory";
    [SerializeField] private int memoryImageCount = 6;
    [SerializeField] private float memoryShowcaseSeconds = 10f;

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
        PrepareQuestSetup();
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
        ResetQuestProgress();
        PrepareQuestSetup();
        collected = new bool[memories.Length];
    }

    private void Update()
    {
        if (player == null)
        {
            TryResolvePlayer();
        }

        UpdateTreeHouseChoice();
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
        QueueMessage(memoryLines[index]);
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

    private void PrepareArrays()
    {
        int count = memories != null ? memories.Length : 0;
        if (count == 0)
        {
            EnsureMemoryTextDefaults();
            if (collected == null || collected.Length != 0)
            {
                collected = new bool[0];
            }

            return;
        }

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

    private void EnsureMemoryTextDefaults()
    {
        if (memoryNames == null || memoryNames.Length == 0)
        {
            memoryNames = (string[])DefaultMemoryNames.Clone();
        }

        if (memoryLines == null || memoryLines.Length == 0)
        {
            memoryLines = (string[])DefaultMemoryLines.Clone();
        }

        FillMissingText(memoryNames, DefaultMemoryNames);
        FillMissingText(memoryLines, DefaultMemoryLines);
    }

    private static void FillMissingText(string[] target, string[] defaults)
    {
        if (target == null || defaults == null)
        {
            return;
        }

        int count = Mathf.Min(target.Length, defaults.Length);
        for (int i = 0; i < count; i++)
        {
            if (string.IsNullOrEmpty(target[i]))
            {
                target[i] = defaults[i];
            }
        }
    }

    private void ResolveSceneReferences()
    {
        TryResolvePlayer();
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
            DrawCenteredLabel(GetMemoryPrompt(nearbyMemoryIndex), 28);
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

    private void DrawTreeHouseChoicePanel()
    {
        float width = Mathf.Min(980f, Screen.width - 80f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.5f - 220f, width, 440f);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle titleStyle = GameUiStyle.LabelStyle(ref choiceTitleStyle, 30, TextAnchor.MiddleCenter, FontStyle.Bold);
        GUIStyle optionStyle = GameUiStyle.LabelStyle(ref choiceOptionStyle, 28, TextAnchor.MiddleLeft, FontStyle.Normal, true);
        GUIStyle hintStyle = GameUiStyle.LabelStyle(ref choiceHintStyle, 22, TextAnchor.MiddleRight);
        hintStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

        GUI.Label(new Rect(rect.x + 42f, rect.y + 110f, rect.width - 72f, 68f), choiceTitle, titleStyle);
        GUI.Label(new Rect(rect.x + 140f, rect.y + 160f, rect.width - 104f, 92f), choiceA, optionStyle);
        GUI.Label(new Rect(rect.x + 140f, rect.y + 250f, rect.width - 104f, 92f), choiceB, optionStyle);
        GUI.Label(new Rect(rect.x + 0f, rect.y + rect.height - 108f, rect.width - 150f, 48f), choiceHint, hintStyle);
    }

    private string GetMemoryPrompt(int index)
    {
        if (index < 0 || index >= memoryNames.Length || string.IsNullOrEmpty(memoryNames[index]))
        {
            return interactPrompt;
        }

        return interactPrompt + ": " + memoryNames[index];
    }

    private void DrawQuestPanel()
    {
        float width = 630f;
        float height = 260f;
        Rect rect = GameUiStyle.SideQuestRect(width, height);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle titleStyle = GameUiStyle.LabelStyle(ref questTitleStyle, 22, TextAnchor.UpperLeft, FontStyle.Normal, true);
        GUIStyle taskStyle = GameUiStyle.LabelStyle(ref questTaskStyle, 22, TextAnchor.UpperLeft, FontStyle.Normal, true);
        taskStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f);

        int visibleFragmentCount = memoryFragmentsConsumed ? 0 : collectedCount;
        int memoryCount = memories != null ? memories.Length : 0;
        string completionMark = collectedCount >= memoryCount ? " done" : string.Empty;
        GUI.Label(new Rect(rect.x + 120f, rect.y + 90f, rect.width - 44f, 76f), questTitle, titleStyle);
        GUI.Label(new Rect(rect.x + 120f, rect.y + 130f, rect.width - 44f, 58f), restoreTaskText + completionMark, taskStyle);
        GUI.Label(new Rect(rect.x + 120f, rect.y + 170f, rect.width - 44f, 58f), "Memory fragments: " + visibleFragmentCount + "/" + memoryCount, taskStyle);
    }

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

        GUIStyle hintStyle = GameUiStyle.LabelStyle(ref puzzleHintStyle, 20, TextAnchor.MiddleCenter, FontStyle.Normal, true);
        GUI.Label(new Rect(panel.x + 16f, panel.y + 10f, panel.width - 32f, 40f), "Use WASD to move tiles", hintStyle);

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

    private void DrawReferenceImage(Rect panel, Rect grid, float referenceSize, bool sideBySide)
    {
        if (puzzleTexture == null)
        {
            return;
        }

        Rect referenceRect = sideBySide
            ? new Rect(grid.xMax + 24f, grid.y + 42f, referenceSize, referenceSize)
            : new Rect(panel.x + (panel.width - referenceSize) * 0.5f, grid.yMax + 16f, referenceSize, referenceSize);

        GUIStyle labelStyle = GameUiStyle.LabelStyle(ref referenceLabelStyle, 18, TextAnchor.MiddleCenter);

        GUI.Label(new Rect(referenceRect.x, referenceRect.y - 28f, referenceRect.width, 24f), "Reference", labelStyle);
        GUI.Box(referenceRect, GUIContent.none);
        GUI.DrawTexture(new Rect(referenceRect.x + 4f, referenceRect.y + 4f, referenceRect.width - 8f, referenceRect.height - 8f), puzzleTexture, ScaleMode.ScaleToFit, true);
    }

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
        Rect rect = GameUiStyle.SystemPromptRect(920f, 156f);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle style = GameUiStyle.LabelStyle(ref messageStyle, 24, TextAnchor.MiddleCenter, FontStyle.Normal, true);
        GUI.Label(new Rect(rect.x + 18f, rect.y + 12f, rect.width - 36f, rect.height - 24f), text, style);
    }

    private void DrawCenteredLabel(string text, int fontSize)
    {
        GUIStyle style = GameUiStyle.LabelStyle(ref centeredLabelStyle, fontSize, TextAnchor.MiddleCenter, FontStyle.Normal, true);
        Rect rect = GameUiStyle.InteractionPromptRect(680f, 118f);
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(rect, text, style);
    }

    private void PrepareQuestSetup()
    {
        EnsureMemoryTextDefaults();
        ResolveSceneReferences();
        PrepareArrays();
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

    private void TryResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            player = mainCamera.transform.root;
        }
    }
}
