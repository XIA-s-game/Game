// Main old tree quest controller: shared state, update loop, and scene hooks.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class OldTreeInteraction : MonoBehaviour
{
    private enum DialogueState
    {
        Waiting,
        Choosing,
        Speaking,
        FinalInstruction,
        MovingNest,
        Attacking,
        Answered,
        EggChallenge,
        EggChallengeResult,
        EggChallengeFailed,
        RewardChoosing,
        MushroomGift
    }

    [Header("Player")]
    [SerializeField] private string targetSceneName = "my scene";
    [SerializeField] private string playerName = "AQM_FPS_Character";
    [SerializeField] private string interactionTargetName = "old face";
    [SerializeField] private float interactDistance = 20f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Tree Look")]
    [SerializeField] private Transform lookRoot;
    [SerializeField] private float turnDuration = 1.8f;
    [SerializeField] private float lookDownAngle = 8f;
    [SerializeField] private float answerDuration = 5f;

    [Header("Nest Quest")]
    [SerializeField] private string nestBranchName = "Cylinder.005";
    [SerializeField] private string cylinderResetName = "Cylinder.002";
    [SerializeField] private string cylinderResetFallbackName = "cyliner002";
    [SerializeField] private string nestName = "nest";
    [SerializeField] private string faceResetName = "face";
    [SerializeField] private float nestTargetY = 1.89f;
    [SerializeField] private float nestMoveDuration = 4f;

    [Header("Egg Challenge")]
    [SerializeField] private GameObject eggPrefab;
    [SerializeField] private GameObject levelOneOddEggPrefab;
    [SerializeField] private GameObject levelTwoOddEggPrefab;
    [SerializeField] private GameObject levelThreeOddEggPrefab;
    [SerializeField] private float eggLevelDuration = 5f;
    [SerializeField] private float eggGridDistance = 8f;
    [SerializeField] private float eggGridSpacing = 0.45f;
    [SerializeField] private float eggScale = 0.28f;
    [SerializeField] private float eggResultDuration = 1.4f;
    [SerializeField] private string levelOneTitle = "Level 1";
    [SerializeField] private string levelTwoTitle = "Level 2";
    [SerializeField] private string levelThreeTitle = "Level 3";
    [SerializeField] private string levelOneSuccessText = "Level 1 complete";
    [SerializeField] private string levelTwoSuccessText = "Level 2 complete";
    [SerializeField] private string gameSuccessText = "Game complete";
    [SerializeField] private string gameFailedText = "Game over";
    [SerializeField] private string rewardGreeting = "You passed my test. Choose one:";
    [SerializeField] private string rewardChoiceA = "A: Take the egg";
    [SerializeField] private string rewardChoiceB = "B: Destroy the egg";
    [SerializeField] private string rewardChoiceC = "C: Leave it there";
    [SerializeField] private string rewardChoiceD = "D: Move it somewhere safer";

    [Header("Mushroom Gift")]
    [SerializeField] private string mushroomGiftName = "mu";
    [SerializeField] private float mushroomMoveOutDistance = 3f;
    [SerializeField] private float mushroomMoveOutHeight = 0f;
    [SerializeField] private float mushroomFrontDistance = 2.2f;
    [SerializeField] private float mushroomFrontHeight = -0.25f;
    [SerializeField] private float mushroomMoveSpeed = 5f;
    [SerializeField] private float mushroomBobAmount = 0f;
    [SerializeField] private float mushroomBobSpeed = 2.2f;
    [SerializeField] private Color mushroomGlowColor = new Color(0.45f, 1f, 0.35f, 1f);
    [SerializeField] private float mushroomGlowIntensity = 5f;
    [SerializeField] private string mushroomPickupPrompt = "Press F to pick up";

    [Header("Side Quest")]
    [SerializeField] private string sideQuestTitle = "Side Quest: Build a safe shelter";
    [SerializeField] private string fenceTaskText = "Find 7 fences";
    [SerializeField] private string saplingTaskText = "Ask the witch for 2 saplings";
    [SerializeField] private string fenceInventoryName = "Fence";
    [SerializeField] private string fencePickupPrompt = "Press E to pick up";
    [SerializeField] private string fenceBuildTargetName = "Fence_A5 (2)";
    [SerializeField] private string fenceBuildPrompt = "Press E to build";
    [SerializeField] private string sideQuestTreeReminder = "Go finish this task.";
    [SerializeField] private string peasantGirlName = "Peasant Girl";
    [SerializeField] private float peasantInteractDistance = 4f;
    [SerializeField] private RuntimeAnimatorController peasantDanceController;
    [SerializeField] private RuntimeAnimatorController peasantStandController;
    [SerializeField] private string peasantDanceStateName = "mixamo_com";
    [SerializeField] private string peasantStandStateName = "mixamo_com";
    [SerializeField] private string peasantFirstLine = "Bring me something to trade.";
    [SerializeField] private string peasantSecondLine = "Here are two saplings. Plant them well.";
    [SerializeField] private string saplingInventoryName = "Sapling";
    [SerializeField] private string saplingPlantPrompt = "Press E to plant";
    [SerializeField] private GameObject saplingPreviewPrefab;
    [SerializeField] private string[] saplingPlantTargetNames = { "Mini_Tree_1A1 (2)", "Mini_Tree_1A1 (3)", "Mini_Tree_1A1 (4)" };
    [SerializeField] private Vector3[] saplingPlantOffsets =
    {
        new Vector3(-2f, 0f, 2f),
        new Vector3(0f, 0f, 2.8f),
        new Vector3(2f, 0f, 2f)
    };
    [SerializeField] private int requiredFenceCount = 7;
    [SerializeField] private int requiredSaplingCount = 2;
    [SerializeField] private float fencePickupDistance = 3f;
    [SerializeField] private float fenceBuildDistance = 5f;
    [SerializeField] private float saplingPlantDistance = 5f;
    [SerializeField] private Color fenceHighlightColor = new Color(1f, 0.9f, 0.2f, 1f);

    [Header("Angry Attack")]
    [SerializeField] private float attackMoveAmount = 1.4f;
    [SerializeField] private float attackMoveSpeed = 3.5f;
    [SerializeField] private float attackRotateSpeed = 260f;
    [SerializeField] private float sweepStartDistance = 14f;
    [SerializeField] private float sweepEndDistance = 14f;
    [SerializeField] private float sweepHeightOffset = 1.2f;
    [SerializeField] private float sweepDuration = 0.38f;
    [SerializeField] private float launchTopY = 100f;
    [SerializeField] private float impactY = 10f;
    [SerializeField] private float launchForwardDistance = 5f;
    [SerializeField] private float launchRiseDuration = 0.9f;
    [SerializeField] private float launchFallDuration = 1.35f;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private RuntimeAnimatorController fallingController;
    [SerializeField] private RuntimeAnimatorController impactController;
    [SerializeField] private RuntimeAnimatorController standingUpController;
    [SerializeField] private string controllerStateName = "mixamo_com";
    [SerializeField] private float impactHoldDuration = 5f;
    [SerializeField] private float standingUpDuration = 2.5f;
    [SerializeField] private float postStandingResetDelay = 2f;
    [SerializeField] private Vector3 launchCameraOffset = new Vector3(0f, 4.5f, -9f);
    [SerializeField] private Vector3 launchCameraLookOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private Vector3 impactCameraLocalPosition = new Vector3(0f, 4f, -7f);
    [SerializeField] private Vector3 impactCameraLookOffset = new Vector3(0f, 1f, 0f);

    [Header("Text")]
    [SerializeField] private Font dialogueFont;
    [SerializeField] private string prompt = "Press E to talk";
    [SerializeField] private string greeting = "Hello, young mage. What brings you here?";
    [SerializeField] private string choiceA = "A: Just passing by";
    [SerializeField] private string choiceB = "B: I am learning magic";
    [SerializeField] private string choiceC = "C: None of your business";
    [SerializeField] private string chooseHint = "Press A / B / C or 1 / 2 / 3";
    [SerializeField] private string continueHint = "Press C to continue";
    [SerializeField] private string answerA = "Do not rush, young one.";
    [SerializeField] private string answerC = "Rude words have consequences.";

    private Transform player;
    private Transform interactionTarget;
    private Transform nestBranch;
    private Transform cylinderResetTarget;
    private Transform nest;
    private Transform faceResetTarget;
    private Transform mushroomGift;
    private Quaternion originalRotation;
    private Vector3 interactionTargetOriginalPosition;
    private Vector3 nestBranchOriginalPosition;
    private Vector3 cylinderOriginalPosition;
    private Vector3 nestOriginalPosition;
    private Vector3 faceOriginalPosition;
    private Vector3 mushroomOriginalPosition;
    private Vector3 interactionTargetOriginalScale;
    private Vector3 nestBranchOriginalScale;
    private Vector3 cylinderOriginalScale;
    private Vector3 nestOriginalScale;
    private Vector3 faceOriginalScale;
    private Vector3 mushroomOriginalScale;
    private Quaternion interactionTargetOriginalRotation;
    private Quaternion nestBranchOriginalRotation;
    private Quaternion cylinderOriginalRotation;
    private Quaternion nestOriginalRotation;
    private Quaternion faceOriginalRotation;
    private Quaternion mushroomOriginalRotation;
    private bool hasInteractionTargetOriginal;
    private bool hasNestBranchOriginal;
    private bool hasCylinderOriginal;
    private bool hasNestOriginal;
    private bool hasFaceOriginal;
    private bool hasMushroomOriginal;
    private bool branchFlowActive;
    private readonly List<Transform> attackCylinders = new List<Transform>();
    private readonly List<Vector3> attackCylinderOriginalPositions = new List<Vector3>();
    private readonly List<Quaternion> attackCylinderOriginalRotations = new List<Quaternion>();
    private readonly List<Vector3> attackCylinderOriginalScales = new List<Vector3>();
    private readonly List<Vector3> attackCylinderMoveAxes = new List<Vector3>();
    private readonly List<float> attackCylinderSeeds = new List<float>();
    private readonly List<MonoBehaviour> disabledPlayerBehaviours = new List<MonoBehaviour>();
    private readonly List<MonoBehaviour> eggDisabledPlayerBehaviours = new List<MonoBehaviour>();
    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;
    private Vector3 originalCameraForward;
    private RuntimeAnimatorController originalAnimatorController;
    private Transform playerCameraTransform;
    private bool hasAttackOriginals;
    private bool hasPlayerOriginal;
    private bool hasCameraOriginal;
    private bool attackCylinderBaselineCached;
    private bool hasAnimatorOriginal;
    private bool characterControllerWasEnabled;
    private bool rigidbodyWasKinematic;
    private bool eggPlayerControlLocked;
    private CursorLockMode originalCursorLockMode;
    private bool originalCursorVisible;
    private DialogueState state = DialogueState.Waiting;
    private string currentAnswer;
    private string[] currentLines;
    private int currentLineIndex;
    private System.Action currentDialogueComplete;
    private bool autoCompleteOnLastLine;
    private Coroutine lookCoroutine;
    private Coroutine resetCoroutine;
    private Coroutine nestMoveCoroutine;
    private Coroutine finalInstructionCoroutine;
    private Coroutine cylinderAttackCoroutine;
    private Coroutine playerLaunchCoroutine;
    private Coroutine eggResultCoroutine;
    private readonly List<GameObject> spawnedEggs = new List<GameObject>();
    private GameObject eggGridRoot;
    private GameObject correctEgg;
    private int currentEggLevel;
    private int currentEggGridSize;
    private float eggTimer;
    private string eggResultText;
    private Vector3 mushroomTreeExitPosition;
    private Vector3 mushroomTargetPosition;
    private float mushroomGiftStartTime;
    private bool mushroomReachedTreeExit;
    private Light mushroomGiftLight;
    private readonly List<Renderer> mushroomGlowRenderers = new List<Renderer>();
    private readonly List<Color> mushroomOriginalColors = new List<Color>();
    private readonly List<Color> mushroomOriginalEmissionColors = new List<Color>();
    private readonly List<Transform> fenceCollectibles = new List<Transform>();
    private readonly List<Renderer> highlightedFenceRenderers = new List<Renderer>();
    private readonly List<Color> highlightedFenceOriginalColors = new List<Color>();
    private readonly List<Color> highlightedFenceOriginalEmissionColors = new List<Color>();
    private readonly List<bool> highlightedFenceHadEmissionEnabled = new List<bool>();
    private readonly List<Renderer> fenceBuildRenderers = new List<Renderer>();
    private readonly List<Color> fenceBuildOriginalColors = new List<Color>();
    private readonly List<Transform> saplingPlantTargets = new List<Transform>();
    private readonly List<Renderer> saplingGhostRenderers = new List<Renderer>();
    private readonly List<Color> saplingGhostOriginalColors = new List<Color>();
    private Transform nearbyFence;
    private Transform fenceBuildTarget;
    private Transform peasantGirl;
    private Animator peasantAnimator;
    private Transform nearbySaplingPlantTarget;
    private bool fenceBuildTargetShown;
    private bool nearbyFenceBuildTarget;
    private bool fenceBuilt;
    private bool peasantRewardGiven;
    private bool saplingPlantTargetsShown;
    private bool sideQuestActive;
    private bool sideQuestActivatedOnce;
    private int collectedFenceCount;
    private int collectedSaplingCount;
    private int plantedSaplingCount;

    private void Awake()
    {
        if (lookRoot == null)
        {
            lookRoot = transform;
        }

        interactionTarget = FindChildByName(transform, interactionTargetName);
        if (interactionTarget == null)
        {
            GameObject targetObject = GameObject.Find(interactionTargetName);
            if (targetObject != null)
            {
                interactionTarget = targetObject.transform;
            }
        }

        if (interactionTarget == null)
        {
            interactionTarget = transform;
        }

        nestBranch = FindSceneTransform(nestBranchName);
        cylinderResetTarget = FindSceneTransform(cylinderResetName);
        if (cylinderResetTarget == null)
        {
            cylinderResetTarget = FindSceneTransform(cylinderResetFallbackName);
        }

        nest = FindSceneTransform(nestName);
        faceResetTarget = FindSceneTransform(faceResetName);
        if (faceResetTarget == null)
        {
            faceResetTarget = interactionTarget;
        }

        mushroomGift = FindSceneTransform(mushroomGiftName);
        fenceBuildTarget = FindSceneTransform(fenceBuildTargetName);
        if (fenceBuildTarget != null)
        {
            fenceBuildTarget.gameObject.SetActive(false);
        }

        FindPeasantGirl();
        SetPeasantDance();
        PrepareSaplingPlantTargets();

        originalRotation = lookRoot.rotation;
        CacheResetTransforms();
        CollectAttackCylinders();
        FindPlayer();
    }

    private void Update()
    {
        if (!IsTargetScene())
        {
            return;
        }

        if (player == null)
        {
            FindPlayer();
        }

        if (player == null)
        {
            return;
        }

        if (state == DialogueState.Waiting && IsSideQuestInProgress() && !peasantRewardGiven && nearbyFence == null && !nearbyFenceBuildTarget && nearbySaplingPlantTarget == null && IsPlayerNearPeasant() && Input.GetKeyDown(interactKey))
        {
            StartPeasantGirlDialogue();
        }

        if (state == DialogueState.Waiting && IsPlayerNear() && Input.GetKeyDown(interactKey))
        {
            if (IsSideQuestInProgress() && nearbyFence == null && !nearbyFenceBuildTarget && nearbySaplingPlantTarget == null)
            {
                StartLookCoroutine(TurnTowardPlayer());
                StartDialogue(new[] { sideQuestTreeReminder }, CloseSideQuestReminder);
            }
            else
            {
                StartLookCoroutine(TurnTowardPlayer());
                state = DialogueState.Choosing;
            }
        }

        if (state == DialogueState.Choosing)
        {
            ReadChoiceKeys();
        }

        if (branchFlowActive)
        {
            KeepPlayerInsideTreeRange();
        }

        if (state == DialogueState.Speaking && Input.GetKeyDown(KeyCode.C))
        {
            ShowNextLine();
        }

        if (state == DialogueState.EggChallenge)
        {
            UpdateEggChallenge();
        }

        if (state == DialogueState.RewardChoosing)
        {
            ReadRewardChoiceKeys();
        }

        if (state == DialogueState.MushroomGift)
        {
            UpdateMushroomGift();
        }

        if (sideQuestActive)
        {
            UpdateSideQuestCollection();
            UpdatePeasantGirlDance();
            UpdateSaplingPlanting();
        }
    }

    private void StartDialogue(string[] lines, System.Action onComplete)
    {
        StartDialogue(lines, onComplete, false);
    }

    private IEnumerator CloseAnswerAndReset()
    {
        yield return new WaitForSeconds(answerDuration);

        currentAnswer = null;
        state = DialogueState.Waiting;
        UnlockPlayerForEggChallenge();
        ResetTreeToInitialState();
        StartLookCoroutine(ReturnToOriginalRotation());
        resetCoroutine = null;
    }

    private void CollectAttackCylinders()
    {
        if (attackCylinderBaselineCached)
        {
            RestoreAttackCylinders();
            return;
        }

        CollectAttackCylinders(transform);
        if (attackCylinders.Count == 0 && transform.parent != null)
        {
            CollectAttackCylinders(transform.parent);
        }

        hasAttackOriginals = attackCylinders.Count > 0;
        attackCylinderBaselineCached = hasAttackOriginals;
    }

    private void DrawDialogueBox(string text, bool showChoices)
    {
        DrawDialogueBox(text, showChoices, false);
    }
}
