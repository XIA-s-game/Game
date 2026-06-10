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
        RewardChoosing
    }

    [Header("Player")]
    [SerializeField] private string targetSceneName = "my scene";
    [SerializeField] private float interactDistance = 20f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerCameraTransform;

    [Header("Tree Look")]
    [SerializeField] private Transform lookRoot;
    [SerializeField] private Transform interactionTarget;
    [SerializeField] private float turnDuration = 1.8f;
    [SerializeField] private float lookDownAngle = 8f;
    [SerializeField] private float answerDuration = 5f;

    [Header("Nest Quest")]
    [SerializeField] private Transform nestBranch;
    [SerializeField] private Transform cylinderResetTarget;
    [SerializeField] private Transform nest;
    [SerializeField] private Transform faceResetTarget;
    [SerializeField] private float nestTargetY = 1.89f;
    [SerializeField] private float nestMoveDuration = 4f;

    [Header("Reward Choices")]
    [SerializeField] private string rewardGreeting = "Make a choice:";
    [SerializeField] private string rewardChoiceA = "A: Take the egg";
    [SerializeField] private string rewardChoiceB = "B: Destroy the egg";
    [SerializeField] private string rewardChoiceC = "C: Leave it there";
    [SerializeField] private string rewardChoiceD = "D: Move it somewhere safer";
    [SerializeField] private string magicMushroomInventoryName = "Magic Mushroom";

    [Header("Side Quest")]
    [SerializeField] private string sideQuestTitle = "Side Quest: Build a safe shelter";
    [SerializeField] private string fenceTaskText = "Find 7 fences";
    [SerializeField] private string saplingTaskText = "Ask the witch for 2 saplings";
    [SerializeField] private string fenceInventoryName = "Fence";
    [SerializeField] private string fencePickupPrompt = "Press E to pick up";
    [SerializeField] private Transform[] fenceCollectibleTargets;
    [SerializeField] private Transform fenceBuildTarget;
    [SerializeField] private string fenceBuildPrompt = "Press E to build";
    [SerializeField] private string sideQuestTreeReminder = "Go finish this task.";
    [SerializeField] private Transform peasantGirl;
    [SerializeField] private float peasantInteractDistance = 4f;
    [SerializeField] private string peasantFirstLine = "Bring me something to trade.";
    [SerializeField] private string peasantSecondLine = "Here are two saplings. Plant them well.";
    [SerializeField] private string saplingInventoryName = "Sapling";
    [SerializeField] private string saplingPlantPrompt = "Press E to plant";
    [SerializeField] private Transform[] saplingPlantTargetRefs;
    [SerializeField] private int requiredFenceCount = 7;
    [SerializeField] private int requiredSaplingCount = 2;
    [SerializeField] private float fencePickupDistance = 3f;
    [SerializeField] private float fenceBuildDistance = 5f;
    [SerializeField] private float saplingPlantDistance = 5f;
    [SerializeField] private Color fenceHighlightColor = new Color(1f, 0.9f, 0.2f, 1f);

    [Header("Angry Attack")]
    [SerializeField] private Transform[] attackCylinderTargets;
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

    private Quaternion originalRotation;
    private Vector3 interactionTargetOriginalPosition;
    private Vector3 nestBranchOriginalPosition;
    private Vector3 cylinderOriginalPosition;
    private Vector3 nestOriginalPosition;
    private Vector3 faceOriginalPosition;
    private Vector3 interactionTargetOriginalScale;
    private Vector3 nestBranchOriginalScale;
    private Vector3 cylinderOriginalScale;
    private Vector3 nestOriginalScale;
    private Vector3 faceOriginalScale;
    private Quaternion interactionTargetOriginalRotation;
    private Quaternion nestBranchOriginalRotation;
    private Quaternion cylinderOriginalRotation;
    private Quaternion nestOriginalRotation;
    private Quaternion faceOriginalRotation;
    private bool hasInteractionTargetOriginal;
    private bool hasNestBranchOriginal;
    private bool hasCylinderOriginal;
    private bool hasNestOriginal;
    private bool hasFaceOriginal;
    private bool branchFlowActive;
    private readonly List<Transform> attackCylinders = new List<Transform>();
    private readonly List<Vector3> attackCylinderOriginalPositions = new List<Vector3>();
    private readonly List<Quaternion> attackCylinderOriginalRotations = new List<Quaternion>();
    private readonly List<Vector3> attackCylinderOriginalScales = new List<Vector3>();
    private readonly List<Vector3> attackCylinderMoveAxes = new List<Vector3>();
    private readonly List<float> attackCylinderSeeds = new List<float>();
    private readonly List<MonoBehaviour> disabledPlayerBehaviours = new List<MonoBehaviour>();
    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;
    private Vector3 originalCameraForward;
    private RuntimeAnimatorController originalAnimatorController;
    private bool hasAttackOriginals;
    private bool hasPlayerOriginal;
    private bool hasCameraOriginal;
    private bool attackCylinderBaselineCached;
    private bool hasAnimatorOriginal;
    private bool characterControllerWasEnabled;
    private bool rigidbodyWasKinematic;
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
    private readonly Dictionary<Transform, Renderer[]> treeRendererCache = new Dictionary<Transform, Renderer[]>();
    private Transform nearbyFence;
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
        if (fenceBuildTarget != null)
        {
            fenceBuildTarget.gameObject.SetActive(false);
        }

        PrepareSaplingPlantTargets();

        if (lookRoot != null)
        {
            originalRotation = lookRoot.rotation;
        }

        CacheResetTransforms();
        CollectAttackCylinders();
    }

    private void Update()
    {
        if (!IsTargetScene())
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        bool interactPressed = Input.GetKeyDown(interactKey);

        if (state == DialogueState.Waiting && IsSideQuestInProgress() && !peasantRewardGiven && nearbyFence == null && !nearbyFenceBuildTarget && nearbySaplingPlantTarget == null && IsPlayerNearPeasant() && interactPressed)
        {
            StartPeasantGirlDialogue();
        }

        if (state == DialogueState.Waiting && IsPlayerNear() && interactPressed)
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
            return;
        }

        if (state == DialogueState.RewardChoosing)
        {
            ReadRewardChoiceKeys();
        }

        if (sideQuestActive)
        {
            UpdateSideQuestCollection();
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

        CacheManualAttackCylinders();

        hasAttackOriginals = attackCylinders.Count > 0;
        attackCylinderBaselineCached = hasAttackOriginals;
    }

    private void DrawDialogueBox(string text, bool showChoices)
    {
        DrawDialogueBox(text, showChoices, false);
    }
}
