// Main controller for Chapter One: push puzzle, fairy rescue, portal unlock, and save restore.
using System.Collections.Generic;
using AquariusMax.Fae.demo;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterOnePuzzle : MonoBehaviour
{
    // Active scene instance used by the save manager.
    private static ChapterOnePuzzle activeInstance;

    [System.Serializable]
    private class PushStep
    {
        // Stone block moved by the player.
        public Transform block;
        // Marker or clue object used for interaction distance.
        public Transform marker;
        // Optional solved local position; zero means use the starting position.
        public Vector3 solvedLocalPosition;
    }

    [Header("Scene Flow")]
    [SerializeField] private string nextSceneName = "Chapter2_ForestMaze_and_Chapter3_ForestTreehouse";
    [SerializeField] private float portalInteractDistance = 3f;

    [Header("Push Puzzle")]
    // Ordered block sequence for the altar puzzle. The first required steps must be pushed in order.
    [SerializeField] private PushStep[] pushSteps;
    [SerializeField] private int requiredOrderedPushCount = 6;
    [SerializeField] private float playerPushDistance = 10f;
    [SerializeField] private float markerReachDistance = 1.6f;
    [SerializeField] private float pushSpeed = 3.5f;
    [SerializeField] private float solvedDistance = 0.03f;
    [SerializeField] private string pushPrompt = "Press E to push";
    [SerializeField] private string failurePrompt = "Puzzle failed";
    [SerializeField] private string successPrompt = "Puzzle solved";

    [Header("Story Text")]
    // Mainline prompts: recognize help, inspect the altar, then ask the trapped fairy for clues.
    [SerializeField] private string strangeSymbolPrompt = "What is this strange symbol?";
    [SerializeField] private string recognizeHelpPrompt = "Someone is calling for help. Go check it out.";
    [SerializeField] private string strangeAltarPrompt = "What is this strange altar?";
    [SerializeField] private string askHelpPrompt = "Press E to ask";
    [SerializeField] private string[] helpDialogueLines =
    {
        "Fairy: Please help me. Dark magic trapped me here.",
        "Casper: How can I help?",
        "Fairy: Push the stone buttons in the right order to break the spell.",
        "Casper: I saw strange marks nearby. They may be the clue.",
        "Fairy: Yes. The clues are hidden around the forest.",
        "Casper: I will do my best."
    };
    [SerializeField] private string[] clueDialogueLines =
    {
        "Fairy: The code has six steps. Keep looking for clues."
    };
    [SerializeField] private string[] pageRewardDialogueLines =
    {
        "Fairy: Thank you for saving me. Take this first magic page.",
        "Fairy: There is a portal over there. It can take you to the place you need to go.",
        "Fairy: May you become a true magician!"
    };
    [SerializeField] private string firstPageItemName = "First Page";
    [SerializeField] private string portalInteractPrompt = "Press E to travel";

    [Header("Solved Scene State")]
    [SerializeField] private Vector3 cageChildSolvedLocalPosition = new Vector3(0f, -0.99f, 0f);
    [SerializeField] private Vector3 fairySolvedWorldPosition = new Vector3(559.99f, 16.86f, 579.14f);
    [SerializeField] private Vector3 fairySolvedEulerOffset = new Vector3(0f, 180f, 0f);

    [Header("Distances")]
    [SerializeField] private float resultRotationSpeed = 90f;
    [SerializeField] private float storyAreaReachDistance = 2f;
    [SerializeField] private float storyMinimumReachDistance = 6f;
    [SerializeField] private float storyVerticalTolerance = 4f;
    [SerializeField] private float triggerBoundsPadding = 0.15f;
    [SerializeField] private float promptDuration = 3f;

    [Header("UI Layout")]
    [SerializeField] private float interactionPromptScreenY = 0.72f;
    [SerializeField] private Vector2 interactionPromptSize = new Vector2(520f, 64f);
    [SerializeField] private Vector2 systemPromptSize = new Vector2(760f, 92f);
    [SerializeField] private float systemPromptY = 36f;
    [SerializeField] private float dialoguePanelHeight = 260f;
    [SerializeField] private UiPadding promptTextPadding;
    [SerializeField] private UiPadding dialogueTextPadding;
    [SerializeField] private UiPadding dialogueHintPadding;
    [SerializeField] private int promptFontSize = 28;
    [SerializeField] private int systemPromptFontSize = 30;
    [SerializeField] private int dialogueFontSize = 30;
    [SerializeField] private int dialogueHintFontSize = 22;

    private readonly List<Transform> pushBlocks = new List<Transform>();
    private readonly List<Transform> pushMarkers = new List<Transform>();

    // Runtime puzzle state, rebuilt from the Inspector push steps on scene setup.
    private Vector3[] solvedBlockPositions;
    private Vector3[] initialLocalPositions;
    private bool[] completedPushes;
    [Header("Scene References")]
    // These scene references are intentionally dragged in the Inspector to avoid name-based lookup.
    [SerializeField] private Transform player;
    [SerializeField] private DemoCharacter demoCharacter;
    [SerializeField] private CharacterController playerController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener playerAudioListener;
    [SerializeField] private Transform puzzleRoot;
    [SerializeField] private Transform center;
    [SerializeField] private Transform strangeSymbol;
    [SerializeField] private Transform recognizeHelp;
    [SerializeField] private Transform strangeAltar;
    [SerializeField] private Transform askHelp;
    [SerializeField] private Transform cage;
    [SerializeField] private Transform fairy;
    [SerializeField] private Transform portalTrigger;
    [SerializeField] private GameObject portalDoor;
    [SerializeField] private Transform redIndicator;
    [SerializeField] private Transform greenIndicator;
    private int currentIndex;
    // Story flags keep the mainline in order: altar puzzle, fairy reward, then portal travel.
    private bool referencesReady;
    private bool sceneReady;
    // Prompt visibility flags are reset every frame and rebuilt by story/puzzle checks.
    private bool promptVisible;
    private int movingBlockIndex = -1;
    private bool movingWrongBlock;
    private string resultPrompt;
    private float resultPromptEndsAt;
    private string storyPrompt;
    private float storyPromptEndsAt;
    private bool strangeSymbolPromptShown;
    private bool recognizeHelpShown;
    private bool strangeAltarPromptShown;
    private bool askHelpPromptVisible;
    private bool helpDialogueActive;
    private int helpDialogueIndex;
    private string[] activeDialogueLines;
    private KeyCode activeDialogueContinueKey = KeyCode.C;
    private string activeDialogueContinueHint = "Press C to continue";
    private bool initialHelpDialogueFinished;
    private bool rescueApplied;
    private bool pageRewardFinished;
    private bool portalUnlocked;
    private bool portalPromptVisible;
    private bool firstPageAddedToBackpack;
    // Prevents one key press from triggering dialogue, puzzle, and portal actions in the same frame.
    private bool interactionInputConsumed;

    private void OnValidate()
    {
        FillMissingInspectorDefaults();
    }

    private void Awake()
    {
        // Chapter One owns the player control reset when Chapter1_MagicForest loads.
        FillMissingInspectorDefaults();
        AquariusMax.Fae.demo.DemoCharacter.ResetControlFlags();
        activeInstance = this;
        SetPlayerControlReferences(false);
        SetUpChapterOneScene();
        ApplySavedSceneState();
    }

    private void OnEnable()
    {
        activeInstance = this;
        movingBlockIndex = -1;
        movingWrongBlock = false;
        SetPlayerControlReferences(false);
    }

    private void OnDisable()
    {
        if (SceneManager.GetActiveScene().name == "Chapter1_MagicForest")
        {
            Debug.LogWarning("ChapterOnePuzzle was disabled while Chapter1_MagicForest is active.", this);
        }

        if (activeInstance == this)
        {
            activeInstance = null;
        }

    }

    private void Update()
    {
        // Main loop order: prompts, active push motion, puzzle input, fairy story, portal.
        interactionInputConsumed = false;
        askHelpPromptVisible = false;
        promptVisible = false;
        portalPromptVisible = false;
        RotateResultIndicators();

        SetUpPuzzleState();
        UpdateMainlineStory();
        if (helpDialogueActive || interactionInputConsumed)
        {
            return;
        }

        if (movingBlockIndex >= 0)
        {
            MoveActiveBlock();
            return;
        }

        UpdatePushPuzzleInteraction();
        if (interactionInputConsumed)
        {
            return;
        }

        UpdatePortalInteraction();
        if (interactionInputConsumed)
        {
            return;
        }
    }

    public static bool IsPuzzleSolvedForSave()
    {
        // Save system asks this to decide whether Chapter One should load solved or reset.
        return activeInstance != null && activeInstance.rescueApplied;
    }

    public static void ApplySaveState(bool puzzleSolved)
    {
        // Continue game restores only the completed puzzle state; unfinished attempts reset cleanly.
        if (activeInstance == null || SceneManager.GetActiveScene().name != "Chapter1_MagicForest")
        {
            return;
        }

        if (puzzleSolved)
        {
            activeInstance.ApplySolvedPuzzleForSave();
            return;
        }

        activeInstance.ApplySavedPushState();
    }

    private void ApplySolvedPuzzleForSave()
    {
        // Rebuilds the scene as if the player already rescued the fairy.
        SetUpPuzzleState();

        int count = completedPushes != null ? completedPushes.Length : 0;
        for (int i = 0; i < count; i++)
        {
            completedPushes[i] = i < requiredOrderedPushCount;
            if (i < pushBlocks.Count && pushBlocks[i] != null)
            {
                pushBlocks[i].localPosition = GetSolvedLocalPosition(pushBlocks[i], i);
            }
        }

        currentIndex = requiredOrderedPushCount;
        SetIndicatorVisible(redIndicator, false);
        SetIndicatorVisible(greenIndicator, true);
        ApplyRescueResult();
    }

    private static float GetHorizontalDistanceToObject(Vector3 flatPoint, Transform target)
    {
        if (TryGetWorldBounds(target, out Bounds bounds))
        {
            Vector3 closest = bounds.ClosestPoint(new Vector3(flatPoint.x, bounds.center.y, flatPoint.z));
            return Vector3.Distance(flatPoint, Flatten(closest));
        }

        return Vector3.Distance(flatPoint, Flatten(target.position));
    }

    private void FillMissingInspectorDefaults()
    {
        if (storyMinimumReachDistance <= 0f)
        {
            storyMinimumReachDistance = 6f;
        }

        if (storyVerticalTolerance <= 0f)
        {
            storyVerticalTolerance = 4f;
        }

        if (triggerBoundsPadding <= 0f)
        {
            triggerBoundsPadding = 0.15f;
        }

        if (promptDuration <= 0f)
        {
            promptDuration = 3f;
        }

        if (interactionPromptScreenY <= 0f)
        {
            interactionPromptScreenY = 0.72f;
        }

        if (interactionPromptSize == Vector2.zero)
        {
            interactionPromptSize = new Vector2(520f, 64f);
        }

        if (systemPromptSize == Vector2.zero)
        {
            systemPromptSize = new Vector2(760f, 92f);
        }

        if (systemPromptY == 0f)
        {
            systemPromptY = 36f;
        }

        if (dialoguePanelHeight <= 0f)
        {
            dialoguePanelHeight = 260f;
        }

        if (promptTextPadding.IsZero)
        {
            promptTextPadding = UiPadding.Create(14f, 14f, 8f, 16f);
        }

        if (dialogueTextPadding.IsZero)
        {
            dialogueTextPadding = UiPadding.Create(180f, 72f, 130f, 126f);
        }

        if (dialogueHintPadding.IsZero)
        {
            dialogueHintPadding = UiPadding.Create(0f, 160f, 0f, 130f);
        }

        if (promptFontSize <= 0)
        {
            promptFontSize = 28;
        }

        if (systemPromptFontSize <= 0)
        {
            systemPromptFontSize = 30;
        }

        if (dialogueFontSize <= 0)
        {
            dialogueFontSize = 30;
        }

        if (dialogueHintFontSize <= 0)
        {
            dialogueHintFontSize = 22;
        }
    }
}
