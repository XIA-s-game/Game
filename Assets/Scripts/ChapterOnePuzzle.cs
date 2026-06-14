using System.Collections.Generic;
using AquariusMax.Fae.demo;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterOnePuzzle : MonoBehaviour
{
    private static ChapterOnePuzzle activeInstance;

    [System.Serializable]
    // One push step connects a stone block with the marker it should move toward.
    private class PushStep
    {
        public Transform block;
        public Transform marker;
        public Vector3 solvedLocalPosition;
    }

    [SerializeField] private string nextSceneName = "Fae Homes Demo";
    // Hero animation clips used after the forest attack begins.
    [SerializeField] private RuntimeAnimatorController heroWalkController;
    [SerializeField] private RuntimeAnimatorController heroAttackController;
    [SerializeField] private string heroWalkStateName = "mixamo_com";
    [SerializeField] private string heroAttackStateName = "mixamo_com";
    [SerializeField] private float heroMoveSpeed = 5f;
    [SerializeField] private float heroTurnSpeed = 540f;
    [SerializeField] private float heroAttackDistance = 2.1f;
    [SerializeField] private float heroAttackHitDelay = 0.85f;
    [SerializeField] private float heroInteractDistance = 4f;
    [SerializeField] private float portalInteractDistance = 3f;
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
    // Mainline prompts: recognize help, inspect the altar, then ask the trapped fairy for clues.
    [SerializeField] private string recognizeHelpPrompt = "Someone is calling for help. Go check it out.";
    [SerializeField] private string strangeAltarPrompt = "What is this strange altar?";
    [SerializeField] private string askHelpPrompt = "Press E to ask";
    [SerializeField] private string[] helpDialogueLines =
    {
        "Fairy: Please help me. Dark magic trapped me here.",
        "Player: How can I help?",
        "Fairy: Push the stone buttons in the right order to break the spell.",
        "Player: I saw strange marks nearby. They may be the clue.",
        "Fairy: Yes. The clues are hidden around the forest.",
        "Player: I will do my best."
    };
    [SerializeField] private string[] clueDialogueLines =
    {
        "Fairy: The code has six steps. Keep looking for clues."
    };
    [SerializeField] private string[] pageRewardDialogueLines =
    {
        "Fairy: Thank you for saving me. Take this first magic page.",
        "Player: Thank you."
    };
    [SerializeField] private string[] forestAttackDialogueLines =
    {
        "Fairy: What was that sound?",
        "Fairy: The Dark King's monsters are attacking the forest.",
        "Fairy: Please be careful and take a look first.",
        "Fairy: Stay safe."
    };
    [SerializeField] private string[] heroWarningDialogueLines =
    {
        "Hero: Stay back. These monsters are dangerous."
    };
    [SerializeField] private string[] heroAfterCombatDialogueLines =
    {
        "Player: You are strong. You do not look like you are from here.",
        "Hero: I was sent to protect the forest. The Dark King is getting stronger.",
        "Player: Do you know where I can find the second magic page?",
        "Hero: I know a place. I will open a portal for you."
    };
    [SerializeField] private string firstPageItemName = "First Page";
    [SerializeField] private string heroInteractPrompt = "Press E to talk";
    [SerializeField] private string portalInteractPrompt = "Press E to travel";
    [SerializeField] private Vector3 cageChildSolvedLocalPosition = new Vector3(0f, -0.99f, 0f);
    [SerializeField] private Vector3 fairySolvedWorldPosition = new Vector3(559.99f, 16.86f, 579.14f);
    [SerializeField] private Vector3 fairySolvedEulerOffset = new Vector3(0f, 180f, 0f);
    [SerializeField] private float resultRotationSpeed = 90f;
    [SerializeField] private float storyAreaReachDistance = 2f;
    [SerializeField] private float enemyTriggerDistance = 6f;

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
    [SerializeField] private Transform recognizeHelp;
    [SerializeField] private Transform strangeAltar;
    [SerializeField] private Transform askHelp;
    [SerializeField] private Transform cage;
    [SerializeField] private Transform fairy;
    [SerializeField] private Transform enemyTrigger;
    [SerializeField] private Transform hero;
    [SerializeField] private Transform portalTrigger;
    [SerializeField] private GameObject portalDoor;
    [SerializeField] private GameObject[] delayedEnemyObjects;
    [SerializeField] private Animator heroAnimator;
    [SerializeField] private Transform redIndicator;
    [SerializeField] private Transform greenIndicator;
    private readonly List<GameObject> delayedEnemies = new List<GameObject>();
    private readonly List<Renderer> delayedEnemyRenderers = new List<Renderer>();
    private readonly List<Collider> delayedEnemyColliders = new List<Collider>();
    private readonly List<RouteWaypointWalker> delayedEnemyWalkers = new List<RouteWaypointWalker>();
    private readonly List<Animator> delayedEnemyAnimators = new List<Animator>();
    private int currentIndex;
    // Story flags keep the mainline in order: altar puzzle, fairy reward, monster attack, hero portal.
    private bool referencesReady;
    private bool sceneReady;
    private bool promptVisible;
    private int movingBlockIndex = -1;
    private bool movingWrongBlock;
    private string resultPrompt;
    private float resultPromptEndsAt;
    private string storyPrompt;
    private float storyPromptEndsAt;
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
    private bool forestAttackDialogueFinished;
    private bool delayedEnemiesCollected;
    private bool enemiesActivated;
    private bool heroCombatActive;
    private bool heroAttacking;
    private bool heroWarningShown;
    private bool heroCombatFinished;
    private bool heroPostCombatDialogueFinished;
    private bool portalUnlocked;
    private bool heroPromptVisible;
    private bool portalPromptVisible;
    private bool firstPageAddedToBackpack;
    private bool interactionInputConsumed;
    private float heroAttackHitsAt;
    private float heroCombatY;
    private GameObject heroTargetEnemy;
    private readonly HashSet<GameObject> defeatedEnemies = new HashSet<GameObject>();

    private void Awake()
    {
        // Chapter One owns the player control reset when Enchanted Forest loads.
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
        if (SceneManager.GetActiveScene().name == "Enchanted Forest A")
        {
            Debug.LogWarning("ChapterOnePuzzle was disabled while Enchanted Forest A is active.", this);
        }

        if (activeInstance == this)
        {
            activeInstance = null;
        }

        if (enemiesActivated && !heroCombatFinished)
        {
            GameAudioManager.StopEnemyLoop();
        }
    }

    private void Update()
    {
        // Main loop order: prompts, active push motion, puzzle input, enemy attack, hero story, portal.
        interactionInputConsumed = false;
        askHelpPromptVisible = false;
        promptVisible = false;
        heroPromptVisible = false;
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

        UpdateEnemyAmbush();
        UpdateHeroCombat();
        UpdateHeroStory();
        if (helpDialogueActive || interactionInputConsumed)
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
        if (activeInstance == null || SceneManager.GetActiveScene().name != "Enchanted Forest A")
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
}
