// Runs the first chapter puzzle manager: player checks, state flags, and the main update loop.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterOnePuzzle : MonoBehaviour
{
    [System.Serializable]
    private class PushStep
    {
        public Transform block;
        public Transform marker;
        public Vector3 solvedLocalPosition;
    }

    [SerializeField] private bool requireForestAttackDialogueBeforeEnemies = true;
    [SerializeField] private string nextSceneName = "Fae Homes Demo";
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
    [SerializeField] private PushStep[] pushSteps;
    [SerializeField] private int requiredOrderedPushCount = 6;
    [SerializeField] private float playerPushDistance = 10f;
    [SerializeField] private float markerReachDistance = 1.6f;
    [SerializeField] private float pushSpeed = 3.5f;
    [SerializeField] private float solvedDistance = 0.03f;
    [SerializeField] private string pushPrompt = "Press E to push";
    [SerializeField] private string failurePrompt = "Puzzle failed";
    [SerializeField] private string successPrompt = "Puzzle solved";
    [SerializeField] private string recognizeHelpPrompt = "Someone is calling for help. Go check it out.";
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
    private readonly HashSet<Transform> colliderReadyTargets = new HashSet<Transform>();
    private Vector3[] runtimeSolvedLocalPositions;
    private Vector3[] initialLocalPositions;
    private bool[] completedPushes;
    [Header("Scene References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform puzzleRoot;
    [SerializeField] private Transform center;
    [SerializeField] private Transform recognizeHelp;
    [SerializeField] private Transform askHelp;
    [SerializeField] private Transform cage;
    [SerializeField] private Transform fairy;
    [SerializeField] private Transform enemyTrigger;
    [SerializeField] private Transform hero;
    [SerializeField] private Transform portalTrigger;
    [SerializeField] private GameObject portalDoor;
    [SerializeField] private GameObject[] delayedEnemyObjects;
    private Animator heroAnimator;
    [SerializeField] private Transform redIndicator;
    [SerializeField] private Transform greenIndicator;
    private readonly List<GameObject> delayedEnemies = new List<GameObject>();
    private readonly List<Renderer> delayedEnemyRenderers = new List<Renderer>();
    private readonly List<Collider> delayedEnemyColliders = new List<Collider>();
    private readonly List<RouteWaypointWalker> delayedEnemyWalkers = new List<RouteWaypointWalker>();
    private readonly List<Animator> delayedEnemyAnimators = new List<Animator>();
    private int currentIndex;
    private bool referencesReady;
    private bool promptVisible;
    private int movingBlockIndex = -1;
    private bool movingWrongBlock;
    private string resultPrompt;
    private float resultPromptEndsAt;
    private string storyPrompt;
    private float storyPromptEndsAt;
    private bool recognizeHelpShown;
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
    private bool enemiesPrepared;
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
    private float heroAttackHitsAt;
    private float heroCombatY;
    private GameObject heroTargetEnemy;
    private readonly HashSet<GameObject> defeatedEnemies = new HashSet<GameObject>();

    private void Awake()
    {
        RefreshReferences();
    }

    private void OnDisable()
    {
        if (enemiesActivated && !heroCombatFinished)
        {
            GameAudioManager.StopEnemyLoop();
        }
    }

    private void Update()
    {
        RotateResultIndicators();

        if (!referencesReady)
        {
            RefreshReferences();
        }

        if (!referencesReady)
        {
            return;
        }

        UpdateHelpStory();
        UpdateEnemyAmbush();
        UpdateHeroCombat();
        UpdateHeroStory();
        UpdatePortalInteraction();

        if (movingBlockIndex >= 0)
        {
            MoveActiveBlock();
            return;
        }

        if (currentIndex >= requiredOrderedPushCount)
        {
            promptVisible = false;
            return;
        }

        int hoveredIndex = GetHoveredPushIndex();
        promptVisible = hoveredIndex >= 0;

        if (!promptVisible || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        StartPushingBlock(hoveredIndex);
    }

    private void RefreshReferences()
    {
        if (hero != null && heroAnimator == null)
        {
            heroAnimator = hero.GetComponentInChildren<Animator>(true);
        }
        if (!forestAttackDialogueFinished && !enemiesActivated)
        {
            SetHeroVisible(false);
        }

        if (!portalUnlocked)
        {
            SetPortalVisible(false);
        }

        SetIndicatorVisible(redIndicator, false);
        SetIndicatorVisible(greenIndicator, false);
        PrepareDelayedEnemies();
        if (center == null)
        {
            center = puzzleRoot;
        }

        RefreshPushReferences();

        BuildRuntimeSolvedLocalPositions();
        EnsureSolidCollider(center);
        int expectedPushCount = pushSteps != null ? pushSteps.Length : 0;
        referencesReady = player != null && puzzleRoot != null && center != null && pushBlocks.Count == expectedPushCount;
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
