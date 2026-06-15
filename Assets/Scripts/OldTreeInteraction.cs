using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class OldTreeInteraction : MonoBehaviour
{
    // Old tree conversation modes: normal talk, nest lesson, angry attack, and reward choice.
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
    // Old tree is only active in my scene and uses this player reference for distance checks.
    [SerializeField] private string targetSceneName = "my scene";
    [SerializeField] private float interactDistance = 20f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerCameraTransform;

    [Header("Tree Look")]
    // The tree turns toward the player while a conversation is active.
    [SerializeField] private Transform lookRoot;
    [SerializeField] private Transform interactionTarget;
    [SerializeField] private float turnDuration = 1.8f;
    [SerializeField] private float lookDownAngle = 8f;
    [SerializeField] private float answerDuration = 5f;

    [Header("Nest Quest")]
    // Branch and nest transforms are reset after the lesson branch finishes.
    [SerializeField] private Transform nestBranch;
    [SerializeField] private Transform cylinderResetTarget;
    [SerializeField] private Transform nest;
    [SerializeField] private Transform faceResetTarget;
    [SerializeField] private float nestTargetY = 1.89f;
    [SerializeField] private float nestMoveDuration = 4f;

    [Header("Reward Choices")]
    // Final nest choice controls whether the player receives the magic mushroom.
    [SerializeField] private string rewardGreeting = "Make a choice:";
    [SerializeField] private string rewardChoiceA = "A: Take the egg";
    [SerializeField] private string rewardChoiceB = "B: Destroy the egg";
    [SerializeField] private string rewardChoiceC = "C: Leave it there";
    [SerializeField] private string magicMushroomInventoryName = "Magic Mushroom";

    [Header("Angry Attack")]
    // The rude-answer branch locks the player and plays the falling impact sequence.
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

    [Header("Dialogue UI")]
    [SerializeField] private float choiceDialogueHeight = 470f;
    [SerializeField] private float continueDialogueHeight = 260f;
    [SerializeField] private float normalDialogueHeight = 230f;
    [SerializeField] private float choiceDialogueMaxWidth = 1280f;
    [SerializeField] private float choiceDialogueHorizontalPadding = 96f;
    [SerializeField] private Rect dialogueTextRect = new Rect(180f, 130f, -252f, -126f);
    [SerializeField] private float choiceTextExtraY = 80f;
    [SerializeField] private float choiceTextHeight = 120f;
    [SerializeField] private float continueHintX = 840f;
    [SerializeField] private float continueHintY = 130f;
    [SerializeField] private float continueHintWidth = 280f;
    [SerializeField] private float continueHintHeight = 48f;
    [SerializeField] private Rect choiceHintRect = new Rect(180f, 326f, -252f, 38f);
    [SerializeField] private Rect choiceARect = new Rect(180f, 382f, -252f, 42f);
    [SerializeField] private Rect choiceBRect = new Rect(180f, 432f, -252f, 54f);
    [SerializeField] private Rect choiceCRect = new Rect(180f, 494f, -252f, 42f);
    [SerializeField] private int dialogueTextFontSize = 30;
    [SerializeField] private int continueHintFontSize = 22;
    [SerializeField] private int choiceHintFontSize = 22;
    [SerializeField] private int choiceOptionFontSize = 26;
    [SerializeField] private Vector2 interactionPromptSize = new Vector2(520f, 60f);
    [SerializeField] private int interactionPromptFontSize = 28;

    [Header("Reward UI")]
    [SerializeField] private float rewardDialogueHeight = 500f;
    [SerializeField] private Rect rewardGreetingRect = new Rect(180f, 280f, -252f, 88f);
    [SerializeField] private Rect rewardChoiceARect = new Rect(180f, 382f, -252f, 42f);
    [SerializeField] private Rect rewardChoiceBRect = new Rect(180f, 432f, -252f, 42f);
    [SerializeField] private Rect rewardChoiceCRect = new Rect(180f, 494f, -252f, 42f);
    [SerializeField] private int rewardGreetingFontSize = 28;
    [SerializeField] private int rewardChoiceFontSize = 24;

    private Quaternion originalRotation;
    // Cached transforms let the tree scene return to its starting pose after each branch.
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
    // Current dialogue line data shared by the UI partial and dialogue partial.
    private string currentAnswer;
    private string[] currentLines;
    private int currentLineIndex;
    private System.Action currentDialogueComplete;
    private bool finishAfterLastLine;
    private Coroutine lookCoroutine;
    private Coroutine resetCoroutine;
    private Coroutine nestMoveCoroutine;
    private Coroutine finalInstructionCoroutine;
    private Coroutine cylinderAttackCoroutine;
    private Coroutine playerLaunchCoroutine;
    private void Awake()
    {
        // Cache all reset points before the player can start a branch.
        UseTreeAsMissingTargets();

        if (lookRoot != null)
        {
            originalRotation = lookRoot.rotation;
        }

        CacheResetTransforms();
        CollectAttackCylinders();
    }

    private void Update()
    {
        // Main old tree loop: start talk, read choices, keep branch player nearby, advance dialogue.
        if (!IsTargetScene())
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        bool interactPressed = Input.GetKeyDown(interactKey);

        if (state == DialogueState.Waiting && IsPlayerNear() && interactPressed)
        {
            StartLookCoroutine(TurnTowardPlayer());
            state = DialogueState.Choosing;
            GlobalBackpackUI.SetInputBlocked(true);
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
    }

    private void StartDialogue(string[] lines, System.Action onComplete)
    {
        StartDialogue(lines, onComplete, false);
    }

    private IEnumerator CloseAnswerAndReset()
    {
        // Short answers close automatically and then restore the tree pose.
        yield return new WaitForSeconds(answerDuration);

        currentAnswer = null;
        state = DialogueState.Waiting;
        ResetTreeToInitialState();
        StartLookCoroutine(ReturnToOriginalRotation());
        resetCoroutine = null;
    }

    private void CollectAttackCylinders()
    {
        // Attack cylinders are cached once and restored before each angry branch.
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

    private IEnumerator TurnTowardPlayer()
    {
        // Smooth tree face turn when the player starts interacting.
        if (lookRoot == null || player == null)
        {
            yield break;
        }

        Quaternion startRotation = lookRoot.rotation;
        Quaternion targetRotation = GetLookAtPlayerRotation();
        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            float t = turnDuration > 0.01f ? Mathf.Clamp01(elapsed / turnDuration) : 1f;
            lookRoot.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        lookRoot.rotation = targetRotation;
    }

    private IEnumerator ReturnToOriginalRotation()
    {
        if (lookRoot == null)
        {
            yield break;
        }

        Quaternion startRotation = lookRoot.rotation;
        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            float t = turnDuration > 0.01f ? Mathf.Clamp01(elapsed / turnDuration) : 1f;
            lookRoot.rotation = Quaternion.Slerp(startRotation, originalRotation, t);
            yield return null;
        }

        lookRoot.rotation = originalRotation;
    }

    private Quaternion GetLookAtPlayerRotation()
    {
        if (lookRoot == null || player == null)
        {
            return originalRotation;
        }

        Vector3 target = player.position;
        if (playerCameraTransform != null)
        {
            target = playerCameraTransform.position;
        }
        else if (interactionTarget != null)
        {
            target = interactionTarget.position;
        }

        Vector3 direction = target - lookRoot.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return lookRoot.rotation;
        }

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        return lookRotation * Quaternion.Euler(lookDownAngle, 0f, 0f);
    }
}
