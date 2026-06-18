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
    // Old tree is only active in Chapter4_Forest_Swamp and uses this player reference for distance checks.
    [SerializeField] private string targetSceneName = "Chapter4_Forest_Swamp";
    // Distance needed to start talking to the tree.
    [SerializeField] private float interactDistance = 20f;
    // Main interaction key.
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    // Player transform.
    [SerializeField] private Transform player;
    // Camera point used when the tree looks at the player.
    [SerializeField] private Transform playerCameraTransform;

    [Header("Tree Look")]
    // The tree turns toward the player while a conversation is active.
    [SerializeField] private Transform lookRoot;
    // Target point used for keeping the player near the tree.
    [SerializeField] private Transform interactionTarget;
    // Time for tree face rotation.
    [SerializeField] private float turnDuration = 1.8f;
    // Extra downward angle while the tree looks at the player.
    [SerializeField] private float lookDownAngle = 8f;
    // How long short answer text stays up.
    [SerializeField] private float answerDuration = 5f;

    [Header("Nest Quest")]
    // Branch and nest transforms are reset after the lesson branch finishes.
    [SerializeField] private Transform nestBranch;
    // Reset helper for the branch/cylinder piece.
    [SerializeField] private Transform cylinderResetTarget;
    // Nest transform moved during the lesson.
    [SerializeField] private Transform nest;
    // Face reset helper.
    [SerializeField] private Transform faceResetTarget;
    // Target local Y for the nest lift.
    [SerializeField] private float nestTargetY = 1.89f;
    // Time for the nest movement.
    [SerializeField] private float nestMoveDuration = 4f;

    [Header("Reward Choices")]
    // Final nest choice controls whether the player receives the magic mushroom.
    [SerializeField] private string rewardGreeting = "Make a choice:";
    // Reward option A text.
    [SerializeField] private string rewardChoiceA = "A: Take the egg";
    // Reward option B text.
    [SerializeField] private string rewardChoiceB = "B: Destroy the egg";
    // Reward option C text.
    [SerializeField] private string rewardChoiceC = "C: Leave it there";
    // Backpack item name for the reward.
    [SerializeField] private string magicMushroomInventoryName = "Magic Mushroom";

    [Header("Angry Attack")]
    // The rude-answer branch locks the player and plays the falling impact sequence.
    [SerializeField] private Transform[] attackCylinderTargets;
    // Distance each attack cylinder moves.
    [SerializeField] private float attackMoveAmount = 1.4f;
    // Movement speed for attack cylinders.
    [SerializeField] private float attackMoveSpeed = 3.5f;
    // Rotation speed for attack cylinders.
    [SerializeField] private float attackRotateSpeed = 260f;
    // Sweep starts this far in front of the player.
    [SerializeField] private float sweepStartDistance = 14f;
    // Sweep ends this far past the player.
    [SerializeField] private float sweepEndDistance = 14f;
    // Height offset for sweep movement.
    [SerializeField] private float sweepHeightOffset = 1.2f;
    // Time for one sweep.
    [SerializeField] private float sweepDuration = 0.38f;
    // Top Y reached during the launch.
    [SerializeField] private float launchTopY = 100f;
    // Impact Y after the launch fall.
    [SerializeField] private float impactY = 10f;
    // Forward distance moved during launch.
    [SerializeField] private float launchForwardDistance = 5f;
    // Time for the launch rise.
    [SerializeField] private float launchRiseDuration = 0.9f;
    // Time for the launch fall.
    [SerializeField] private float launchFallDuration = 1.35f;
    // Player animator swapped during the attack sequence.
    [SerializeField] private Animator playerAnimator;
    // Falling animation controller.
    [SerializeField] private RuntimeAnimatorController fallingController;
    // Impact animation controller.
    [SerializeField] private RuntimeAnimatorController impactController;
    // Standing-up animation controller.
    [SerializeField] private RuntimeAnimatorController standingUpController;
    // State name used in the swapped controllers.
    [SerializeField] private string controllerStateName = "mixamo_com";
    // Time to hold the impact pose.
    [SerializeField] private float impactHoldDuration = 5f;
    // Time to play standing up.
    [SerializeField] private float standingUpDuration = 2.5f;
    // Delay before controls return.
    [SerializeField] private float postStandingResetDelay = 2f;
    // Camera offset during launch.
    [SerializeField] private Vector3 launchCameraOffset = new Vector3(0f, 4.5f, -9f);
    // Look offset during launch.
    [SerializeField] private Vector3 launchCameraLookOffset = new Vector3(0f, 2.2f, 0f);
    // Camera local position at impact.
    [SerializeField] private Vector3 impactCameraLocalPosition = new Vector3(0f, 4f, -7f);
    // Look offset at impact.
    [SerializeField] private Vector3 impactCameraLookOffset = new Vector3(0f, 1f, 0f);

    [Header("Text")]
    // Optional font used by old tree dialogue.
    [SerializeField] private Font dialogueFont;
    // Prompt shown near the tree.
    [SerializeField] private string prompt = "Press E to talk";
    // First greeting line.
    [SerializeField] private string greeting = "Hello, young mage. What brings you here?";
    // First player choice.
    [SerializeField] private string choiceA = "A: Just passing by";
    // Second player choice.
    [SerializeField] private string choiceB = "B: I am learning magic";
    // Rude player choice.
    [SerializeField] private string choiceC = "C: None of your business";
    // Hint for choosing A/B/C.
    [SerializeField] private string chooseHint = "Press A / B / C or 1 / 2 / 3";
    // Hint for continuing dialogue.
    [SerializeField] private string continueHint = "Press C to continue";
    // Short answer for choice A.
    [SerializeField] private string answerA = "Do not rush, young one.";
    // Short answer for rude choice.
    [SerializeField] private string answerC = "Rude words have consequences.";

    [Header("Dialogue UI")]
    // Height for choice dialogue panel.
    [SerializeField] private float choiceDialogueHeight = 470f;
    // Height for continue dialogue panel.
    [SerializeField] private float continueDialogueHeight = 260f;
    // Height for normal dialogue panel.
    [SerializeField] private float normalDialogueHeight = 230f;
    // Maximum width for the choice dialogue.
    [SerializeField] private float choiceDialogueMaxWidth = 1280f;
    // Horizontal padding for the choice dialogue.
    [SerializeField] private float choiceDialogueHorizontalPadding = 96f;
    // Rect for normal dialogue text.
    [SerializeField] private Rect dialogueTextRect = new Rect(180f, 130f, -252f, -126f);
    // Extra Y offset for choice text.
    [SerializeField] private float choiceTextExtraY = 80f;
    // Height for each choice text row.
    [SerializeField] private float choiceTextHeight = 120f;
    // Continue hint X position.
    [SerializeField] private float continueHintX = 840f;
    // Continue hint Y position.
    [SerializeField] private float continueHintY = 130f;
    // Continue hint width.
    [SerializeField] private float continueHintWidth = 280f;
    // Continue hint height.
    [SerializeField] private float continueHintHeight = 48f;
    // Rect for the choice hint.
    [SerializeField] private Rect choiceHintRect = new Rect(180f, 326f, -252f, 38f);
    // Rect for choice A.
    [SerializeField] private Rect choiceARect = new Rect(180f, 382f, -252f, 42f);
    // Rect for choice B.
    [SerializeField] private Rect choiceBRect = new Rect(180f, 432f, -252f, 54f);
    // Rect for choice C.
    [SerializeField] private Rect choiceCRect = new Rect(180f, 494f, -252f, 42f);
    // Dialogue body font size.
    [SerializeField] private int dialogueTextFontSize = 30;
    // Continue hint font size.
    [SerializeField] private int continueHintFontSize = 22;
    // Choice hint font size.
    [SerializeField] private int choiceHintFontSize = 22;
    // Choice option font size.
    [SerializeField] private int choiceOptionFontSize = 26;
    // Interaction prompt size.
    [SerializeField] private Vector2 interactionPromptSize = new Vector2(520f, 60f);
    // Interaction prompt font size.
    [SerializeField] private int interactionPromptFontSize = 28;

    [Header("Reward UI")]
    // Height for the reward choice panel.
    [SerializeField] private float rewardDialogueHeight = 500f;
    // Rect for reward greeting.
    [SerializeField] private Rect rewardGreetingRect = new Rect(180f, 280f, -252f, 88f);
    // Rect for reward choice A.
    [SerializeField] private Rect rewardChoiceARect = new Rect(180f, 382f, -252f, 42f);
    // Rect for reward choice B.
    [SerializeField] private Rect rewardChoiceBRect = new Rect(180f, 432f, -252f, 42f);
    // Rect for reward choice C.
    [SerializeField] private Rect rewardChoiceCRect = new Rect(180f, 494f, -252f, 42f);
    // Reward greeting font size.
    [SerializeField] private int rewardGreetingFontSize = 28;
    // Reward choice font size.
    [SerializeField] private int rewardChoiceFontSize = 24;

    // Starting rotation for the tree look root.
    private Quaternion originalRotation;
    // Cached transforms let the tree scene return to its starting pose after each branch.
    private Vector3 interactionTargetOriginalPosition;
    // Original nest branch position.
    private Vector3 nestBranchOriginalPosition;
    // Original cylinder reset target position.
    private Vector3 cylinderOriginalPosition;
    // Original nest position.
    private Vector3 nestOriginalPosition;
    // Original face reset target position.
    private Vector3 faceOriginalPosition;
    // Original interaction target scale.
    private Vector3 interactionTargetOriginalScale;
    // Original nest branch scale.
    private Vector3 nestBranchOriginalScale;
    // Original cylinder reset target scale.
    private Vector3 cylinderOriginalScale;
    // Original nest scale.
    private Vector3 nestOriginalScale;
    // Original face reset target scale.
    private Vector3 faceOriginalScale;
    // Original interaction target rotation.
    private Quaternion interactionTargetOriginalRotation;
    // Original nest branch rotation.
    private Quaternion nestBranchOriginalRotation;
    // Original cylinder reset target rotation.
    private Quaternion cylinderOriginalRotation;
    // Original nest rotation.
    private Quaternion nestOriginalRotation;
    // Original face reset target rotation.
    private Quaternion faceOriginalRotation;
    // True once interaction target reset data is cached.
    private bool hasInteractionTargetOriginal;
    // True once nest branch reset data is cached.
    private bool hasNestBranchOriginal;
    // True once cylinder reset data is cached.
    private bool hasCylinderOriginal;
    // True once nest reset data is cached.
    private bool hasNestOriginal;
    // True once face reset data is cached.
    private bool hasFaceOriginal;
    // True while the tree branch lesson or attack owns the player.
    private bool branchFlowActive;
    // Attack cylinder transforms.
    private readonly List<Transform> attackCylinders = new List<Transform>();
    // Original attack cylinder positions.
    private readonly List<Vector3> attackCylinderOriginalPositions = new List<Vector3>();
    // Original attack cylinder rotations.
    private readonly List<Quaternion> attackCylinderOriginalRotations = new List<Quaternion>();
    // Original attack cylinder scales.
    private readonly List<Vector3> attackCylinderOriginalScales = new List<Vector3>();
    // Per-cylinder move axes.
    private readonly List<Vector3> attackCylinderMoveAxes = new List<Vector3>();
    // Per-cylinder random-looking offsets.
    private readonly List<float> attackCylinderSeeds = new List<float>();
    // Player behaviours disabled during the attack branch.
    private readonly List<MonoBehaviour> disabledPlayerBehaviours = new List<MonoBehaviour>();
    // Player position before the attack branch.
    private Vector3 originalPlayerPosition;
    // Player rotation before the attack branch.
    private Quaternion originalPlayerRotation;
    // Camera local position before the attack branch.
    private Vector3 originalCameraLocalPosition;
    // Camera local rotation before the attack branch.
    private Quaternion originalCameraLocalRotation;
    // Camera forward direction before the attack branch.
    private Vector3 originalCameraForward;
    // Animator controller before swapping attack animations.
    private RuntimeAnimatorController originalAnimatorController;
    // True once attack cylinder reset data is cached.
    private bool hasAttackOriginals;
    // True once player reset data is cached.
    private bool hasPlayerOriginal;
    // True once camera reset data is cached.
    private bool hasCameraOriginal;
    // True after attack cylinder baseline collection.
    private bool attackCylinderBaselineCached;
    // True once original animator controller is cached.
    private bool hasAnimatorOriginal;
    // CharacterController enabled state before attack.
    private bool characterControllerWasEnabled;
    // Rigidbody kinematic state before attack.
    private bool rigidbodyWasKinematic;
    // Current old tree dialogue state.
    private DialogueState state = DialogueState.Waiting;
    // Current dialogue line data shared by the UI partial and dialogue partial.
    private string currentAnswer;
    // Dialogue lines currently being shown.
    private string[] currentLines;
    // Current dialogue line index.
    private int currentLineIndex;
    // Callback after current dialogue closes.
    private System.Action currentDialogueComplete;
    // True when the dialogue should close after the last line.
    private bool finishAfterLastLine;
    // Running look rotation coroutine.
    private Coroutine lookCoroutine;
    // Running reset coroutine.
    private Coroutine resetCoroutine;
    // Running nest move coroutine.
    private Coroutine nestMoveCoroutine;
    // Running final instruction coroutine.
    private Coroutine finalInstructionCoroutine;
    // Running cylinder attack coroutine.
    private Coroutine cylinderAttackCoroutine;
    // Running player launch coroutine.
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
