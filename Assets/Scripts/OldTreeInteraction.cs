using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OldTreeInteraction : MonoBehaviour
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

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.Find(playerName);
        if (playerObject != null)
        {
            player = playerObject.transform;
            FindPlayerAnimator();
            FindPlayerCamera();
        }
    }

    private void FindPeasantGirl()
    {
        if (peasantGirl != null)
        {
            return;
        }

        Transform found = FindSceneTransform(peasantGirlName);
        if (found == null)
        {
            return;
        }

        peasantGirl = found;
        peasantAnimator = peasantGirl.GetComponentInChildren<Animator>();
    }

    private void SetPeasantDance()
    {
        FindPeasantGirl();
        if (peasantAnimator == null || peasantRewardGiven)
        {
            return;
        }

        if (peasantDanceController != null && peasantAnimator.runtimeAnimatorController != peasantDanceController)
        {
            peasantAnimator.runtimeAnimatorController = peasantDanceController;
        }

        if (!string.IsNullOrEmpty(peasantDanceStateName))
        {
            peasantAnimator.Play(peasantDanceStateName);
        }
    }

    private void SetPeasantStand()
    {
        FindPeasantGirl();
        if (peasantAnimator == null)
        {
            return;
        }

        if (peasantStandController != null)
        {
            peasantAnimator.runtimeAnimatorController = peasantStandController;
        }

        if (!string.IsNullOrEmpty(peasantStandStateName))
        {
            peasantAnimator.Play(peasantStandStateName);
        }
    }

    private void UpdatePeasantGirlDance()
    {
        if (!peasantRewardGiven)
        {
            SetPeasantDance();
        }
    }

    private IEnumerator TurnTowardPlayer()
    {
        Quaternion startRotation = lookRoot.rotation;
        Quaternion targetRotation = GetLookAtPlayerRotation();
        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / turnDuration);
            lookRoot.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        lookRoot.rotation = targetRotation;
        lookCoroutine = null;
    }

    private IEnumerator ReturnToOriginalRotation()
    {
        Quaternion startRotation = lookRoot.rotation;
        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / turnDuration);
            lookRoot.rotation = Quaternion.Slerp(startRotation, originalRotation, t);
            yield return null;
        }

        lookRoot.rotation = originalRotation;
        lookCoroutine = null;
    }

    private Quaternion GetLookAtPlayerRotation()
    {
        Vector3 direction = player.position - lookRoot.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return originalRotation;
        }

        Quaternion yawRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        return yawRotation * Quaternion.Euler(lookDownAngle, 0f, 0f);
    }

    private void ReadChoiceKeys()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Alpha1))
        {
            Choose(answerA);
        }
        else if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartBranchDialogue();
        }
        else if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartAngryAttack();
        }
    }

    private void Choose(string answer)
    {
        branchFlowActive = false;
        currentAnswer = answer;
        state = DialogueState.Answered;

        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }

        resetCoroutine = StartCoroutine(CloseAnswerAndReset());
    }

    private void StartAngryAttack()
    {
        branchFlowActive = false;
        currentAnswer = answerC;
        currentLines = null;
        currentDialogueComplete = null;
        autoCompleteOnLastLine = false;
        state = DialogueState.Attacking;

        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }

        if (finalInstructionCoroutine != null)
        {
            StopCoroutine(finalInstructionCoroutine);
            finalInstructionCoroutine = null;
        }

        CacheAttackSceneState();
        LockPlayerControl();
        CollectAttackCylinders();

        if (cylinderAttackCoroutine != null)
        {
            StopCoroutine(cylinderAttackCoroutine);
        }

        if (playerLaunchCoroutine != null)
        {
            StopCoroutine(playerLaunchCoroutine);
        }

        cylinderAttackCoroutine = StartCoroutine(AnimateAttackCylinders());
        playerLaunchCoroutine = StartCoroutine(SweepAndLaunchPlayer());
    }

    private void CacheAttackSceneState()
    {
        if (player != null)
        {
            originalPlayerPosition = player.position;
            originalPlayerRotation = player.rotation;
            hasPlayerOriginal = true;

            CharacterController controller = player.GetComponent<CharacterController>();
            characterControllerWasEnabled = controller != null && controller.enabled;

            Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
            rigidbodyWasKinematic = playerRigidbody == null || playerRigidbody.isKinematic;
        }

        FindPlayerAnimator();
        if (playerAnimator != null)
        {
            originalAnimatorController = playerAnimator.runtimeAnimatorController;
            hasAnimatorOriginal = true;
        }

        FindPlayerCamera();
        if (playerCameraTransform != null)
        {
            originalCameraLocalPosition = playerCameraTransform.localPosition;
            originalCameraLocalRotation = playerCameraTransform.localRotation;
            originalCameraForward = playerCameraTransform.forward;
            hasCameraOriginal = true;
        }
    }

    private void StartBranchDialogue()
    {
        branchFlowActive = true;
        StartDialogue(new[]
        {
            "Old Tree: Can you see the nest on my branch?",
            "Old Tree: I will lower it so you can look closer."
        }, StartNestMove);
    }

    private void StartNestDialogue()
    {
        StartDialogue(new[]
        {
            "Old Tree: This nest belongs to the reed bird.",
            "Old Tree: One egg does not belong here.",
            "Old Tree: Some birds leave eggs in smaller nests.",
            "Old Tree: When the chick hatches, the other eggs may be pushed out.",
            "Old Tree: Nature can be hard to judge.",
            "Old Tree: I have a small test for your eyes.",
            "Old Tree: Find the different egg before time runs out.",
            "Old Tree: Stay focused."
        }, StartEggChallenge);
    }

    private void StartDialogue(string[] lines, System.Action onComplete)
    {
        StartDialogue(lines, onComplete, false);
    }

    private void StartDialogue(string[] lines, System.Action onComplete, bool autoCompleteLastLine)
    {
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }

        if (finalInstructionCoroutine != null)
        {
            StopCoroutine(finalInstructionCoroutine);
            finalInstructionCoroutine = null;
        }

        currentLines = lines;
        currentLineIndex = 0;
        currentDialogueComplete = onComplete;
        autoCompleteOnLastLine = autoCompleteLastLine;
        currentAnswer = currentLines[0];
        state = DialogueState.Speaking;

        if (autoCompleteOnLastLine && currentLines.Length == 1)
        {
            currentDialogueComplete?.Invoke();
        }
    }

    private void ShowNextLine()
    {
        currentLineIndex++;
        if (currentLines != null && currentLineIndex < currentLines.Length)
        {
            currentAnswer = currentLines[currentLineIndex];
            TryActivateSideQuestFromDialogue(currentAnswer);

            if (autoCompleteOnLastLine && currentLineIndex == currentLines.Length - 1)
            {
                System.Action finalLineComplete = currentDialogueComplete;
                currentLines = null;
                currentDialogueComplete = null;
                autoCompleteOnLastLine = false;
                finalLineComplete?.Invoke();
            }

            return;
        }

        System.Action dialogueComplete = currentDialogueComplete;
        currentLines = null;
        currentDialogueComplete = null;
        autoCompleteOnLastLine = false;
        dialogueComplete?.Invoke();
    }

    private void StartNestMove()
    {
        currentAnswer = null;
        state = DialogueState.MovingNest;

        if (nestMoveCoroutine != null)
        {
            StopCoroutine(nestMoveCoroutine);
        }

        nestMoveCoroutine = StartCoroutine(MoveNestBranchDown());
    }

    private IEnumerator MoveNestBranchDown()
    {
        Transform movingTarget = nestBranch != null ? nestBranch : nest;
        if (movingTarget == null)
        {
            StartNestDialogue();
            nestMoveCoroutine = null;
            yield break;
        }

        Vector3 startPosition = movingTarget.position;
        Vector3 targetPosition = new Vector3(startPosition.x, nestTargetY, startPosition.z);
        float elapsed = 0f;

        while (elapsed < nestMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / nestMoveDuration);
            movingTarget.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        movingTarget.position = targetPosition;
        nestMoveCoroutine = null;
        StartNestDialogue();
    }

    private void CloseDialogueAndReset()
    {
        if (eggResultCoroutine != null)
        {
            StopCoroutine(eggResultCoroutine);
            eggResultCoroutine = null;
        }

        ClearEggGrid();
        DisableMushroomGlow();
        UnlockPlayerForEggChallenge();
        currentAnswer = null;
        eggResultText = null;
        branchFlowActive = false;
        state = DialogueState.Waiting;
        ResetTreeToInitialState();
        StartLookCoroutine(ReturnToOriginalRotation());
    }

    private void CloseSideQuestReminder()
    {
        currentAnswer = null;
        state = DialogueState.Waiting;
        StartLookCoroutine(ReturnToOriginalRotation());
    }

    private void StartPeasantGirlDialogue()
    {
        SetPeasantStand();
        StartDialogue(new[]
        {
            peasantFirstLine,
            peasantSecondLine
        }, CompletePeasantGirlTrade);
    }

    private void CompletePeasantGirlTrade()
    {
        peasantRewardGiven = true;
        collectedSaplingCount = requiredSaplingCount;
        GlobalBackpackUI.SetItemCount(saplingInventoryName, Mathf.Max(0, collectedSaplingCount - plantedSaplingCount));
        currentAnswer = null;
        state = DialogueState.Waiting;
        ShowSaplingPlantTargets();
    }

    private void ShowFinalInstructionForFiveSeconds()
    {
        state = DialogueState.FinalInstruction;

        if (finalInstructionCoroutine != null)
        {
            StopCoroutine(finalInstructionCoroutine);
        }

        finalInstructionCoroutine = StartCoroutine(CloseFinalInstructionAfterDelay());
    }

    private IEnumerator CloseFinalInstructionAfterDelay()
    {
        yield return new WaitForSeconds(answerDuration);

        finalInstructionCoroutine = null;
        CloseDialogueAndReset();
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

    private void StartEggChallenge()
    {
        currentAnswer = null;
        branchFlowActive = true;
        LockPlayerForEggChallenge();
        BeginEggLevel(1);
    }

    private void BeginEggLevel(int level)
    {
        ClearEggGrid();

        currentEggLevel = Mathf.Clamp(level, 1, 3);
        currentEggGridSize = GetEggGridSize(currentEggLevel);
        eggTimer = eggLevelDuration;
        eggResultText = null;
        state = DialogueState.EggChallenge;

        SpawnEggGrid(currentEggGridSize, GetOddEggPrefab(currentEggLevel));
    }

    private void UpdateEggChallenge()
    {
        eggTimer -= Time.deltaTime;
        if (eggTimer <= 0f)
        {
            FailEggChallenge();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TrySelectEgg();
        }
    }

    private void TrySelectEgg()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, 100f))
        {
            return;
        }

        GameObject selectedEgg = FindSpawnedEggRoot(hit.transform);
        if (selectedEgg == null)
        {
            return;
        }

        if (selectedEgg == correctEgg)
        {
            CompleteEggLevel();
        }
        else
        {
            FailEggChallenge();
        }
    }

    private GameObject FindSpawnedEggRoot(Transform hitTransform)
    {
        Transform current = hitTransform;
        while (current != null)
        {
            GameObject currentObject = current.gameObject;
            if (spawnedEggs.Contains(currentObject))
            {
                return currentObject;
            }

            current = current.parent;
        }

        return null;
    }

    private void CompleteEggLevel()
    {
        if (eggResultCoroutine != null)
        {
            StopCoroutine(eggResultCoroutine);
        }

        string successText = GetEggSuccessText(currentEggLevel);
        eggResultCoroutine = StartCoroutine(ShowEggSuccessThenContinue(successText));
    }

    private IEnumerator ShowEggSuccessThenContinue(string successText)
    {
        eggResultText = successText;
        state = DialogueState.EggChallengeResult;
        yield return new WaitForSeconds(eggResultDuration);

        ClearEggGrid();
        eggResultText = gameSuccessText;
        yield return new WaitForSeconds(eggResultDuration);
        eggResultText = null;
        state = DialogueState.RewardChoosing;
        eggResultCoroutine = null;
    }

    private void FailEggChallenge()
    {
        ClearEggGrid();
        eggResultText = gameFailedText;
        state = DialogueState.EggChallengeFailed;

        if (eggResultCoroutine != null)
        {
            StopCoroutine(eggResultCoroutine);
            eggResultCoroutine = null;
        }
    }

    private void RestartEggChallenge()
    {
        if (eggResultCoroutine != null)
        {
            StopCoroutine(eggResultCoroutine);
            eggResultCoroutine = null;
        }

        BeginEggLevel(1);
    }

    private void ExitEggChallenge()
    {
        ClearEggGrid();
        eggResultText = null;
        CloseDialogueAndReset();
    }

    private void LockPlayerForEggChallenge()
    {
        if (eggPlayerControlLocked || player == null)
        {
            return;
        }

        eggPlayerControlLocked = true;
        originalCursorLockMode = Cursor.lockState;
        originalCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        eggDisabledPlayerBehaviours.Clear();
        MonoBehaviour[] playerBehaviours = player.GetComponentsInChildren<MonoBehaviour>();
        for (int i = 0; i < playerBehaviours.Length; i++)
        {
            MonoBehaviour behaviour = playerBehaviours[i];
            if (behaviour != null && behaviour.enabled)
            {
                behaviour.enabled = false;
                eggDisabledPlayerBehaviours.Add(behaviour);
            }
        }
    }

    private void UnlockPlayerForEggChallenge()
    {
        if (!eggPlayerControlLocked)
        {
            return;
        }

        for (int i = 0; i < eggDisabledPlayerBehaviours.Count; i++)
        {
            MonoBehaviour behaviour = eggDisabledPlayerBehaviours[i];
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        eggDisabledPlayerBehaviours.Clear();
        Cursor.lockState = originalCursorLockMode;
        Cursor.visible = originalCursorVisible;
        eggPlayerControlLocked = false;
    }

    private void ReadRewardChoiceKeys()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChooseReward(rewardChoiceA);
        }
        else if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChooseReward(rewardChoiceB);
        }
        else if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChooseReward(rewardChoiceC);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Alpha4))
        {
            ChooseReward(rewardChoiceD);
        }
    }

    private void ChooseReward(string choice)
    {
        UnlockPlayerForEggChallenge();
        branchFlowActive = false;

        if (choice == rewardChoiceA)
        {
            StartDialogue(new[]
            {
                "Old Tree: You want to take the egg?",
                "Old Tree: That is not your choice to make.",
                "Old Tree: The forest has its own rules.",
                "Old Tree: Good intentions can still cause harm.",
                "Old Tree: Watch first, then act.",
                "Old Tree: You are not ready for this lesson.",
                "Old Tree: Leave the nest alone.",
                "Old Tree: Come back when you understand patience."
            }, CloseDialogueAndReset);
        }
        else if (choice == rewardChoiceB)
        {
            StartDialogue(new[]
            {
                "Old Tree: Destroy it?",
                "Old Tree: Magic is not for removing things you dislike.",
                "Old Tree: A mage must understand balance.",
                "Old Tree: Both lives belong to the forest.",
                "Old Tree: Deciding who should live is not wisdom.",
                "Old Tree: That kind of certainty is dangerous.",
                "Old Tree: I will not help you with that.",
                "Old Tree: Step away from the nest.",
                "Old Tree: Think before you judge."
            }, CloseDialogueAndReset);
        }
        else if (choice == rewardChoiceC)
        {
            StartDialogue(new[]
            {
                "Old Tree: Good. You chose restraint.",
                "Old Tree: Many young mages rush to interfere.",
                "Old Tree: The forest does not always need rescue.",
                "Old Tree: It needs understanding.",
                "Old Tree: You kept your hands still.",
                "Old Tree: That deserves a small gift."
            }, StartMushroomGift);
        }
        else
        {
            StartDialogue(new[]
            {
                "Old Tree: Moving it sounds kind, but it still changes the nest.",
                "Old Tree: Help should solve the problem, not create a new one.",
                "Old Tree: If you want to help, build a safe shelter nearby.",
                "Old Tree: Use wisdom, not force.",
                "Old Tree: That is the lesson."
            }, CloseDialogueAndReset);
        }
    }

    private void StartMushroomGift()
    {
        currentAnswer = null;
        state = DialogueState.MushroomGift;
        LockPlayerForEggChallenge();
        PrepareMushroomGift();
    }

    private void PrepareMushroomGift()
    {
        if (mushroomGift == null)
        {
            mushroomGift = FindSceneTransform(mushroomGiftName);
        }

        if (mushroomGift == null)
        {
            PickUpMushroomGift();
            return;
        }

        FindPlayerCamera();
        UpdateMushroomTargetPosition();
        EnableMushroomGlow();

        Vector3 outward = mushroomGift.position - transform.position;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.01f && player != null)
        {
            outward = player.position - transform.position;
            outward.y = 0f;
        }

        if (outward.sqrMagnitude < 0.01f)
        {
            outward = transform.forward;
        }

        outward.Normalize();
        mushroomTreeExitPosition = mushroomGift.position + outward * mushroomMoveOutDistance;
        mushroomTreeExitPosition.y = mushroomGift.position.y + mushroomMoveOutHeight;
        mushroomGiftStartTime = Time.time;
        mushroomReachedTreeExit = false;
    }

    private void UpdateMushroomGift()
    {
        if (mushroomGift == null)
        {
            mushroomGift = FindSceneTransform(mushroomGiftName);
            if (mushroomGift == null)
            {
                PickUpMushroomGift();
                return;
            }
        }

        UpdateMushroomTargetPosition();

        if (!mushroomReachedTreeExit)
        {
            mushroomGift.position = Vector3.MoveTowards(
                mushroomGift.position,
                mushroomTreeExitPosition,
                mushroomMoveSpeed * Time.deltaTime);

            if (Vector3.Distance(mushroomGift.position, mushroomTreeExitPosition) <= 0.03f)
            {
                mushroomReachedTreeExit = true;
                mushroomGiftStartTime = Time.time;
            }
        }
        else
        {
            mushroomGift.position = Vector3.MoveTowards(
                mushroomGift.position,
                mushroomTargetPosition,
                mushroomMoveSpeed * Time.deltaTime);

            if (Vector3.Distance(mushroomGift.position, mushroomTargetPosition) <= 0.03f)
            {
                float bob = Mathf.Sin((Time.time - mushroomGiftStartTime) * mushroomBobSpeed) * mushroomBobAmount;
                mushroomGift.position = mushroomTargetPosition + Vector3.up * bob;
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            PickUpMushroomGift();
        }
    }

    private void UpdateMushroomTargetPosition()
    {
        Transform targetTransform = player != null ? player : playerCameraTransform;
        Vector3 basePosition = targetTransform != null ? targetTransform.position : transform.position + Vector3.up * 2f;
        Vector3 forward = targetTransform != null ? targetTransform.forward : transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.01f && playerCameraTransform != null)
        {
            forward = playerCameraTransform.forward;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.01f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        forward.Normalize();
        mushroomTargetPosition = basePosition + forward * mushroomFrontDistance + Vector3.up * mushroomFrontHeight;
    }

    private void PickUpMushroomGift()
    {
        DisableMushroomGlow();
        UnlockPlayerForEggChallenge();
        StartDialogue(new[]
        {
            "Old Tree: This is a magic mushroom.",
            "Old Tree: It reminds careful mages to observe before acting.",
            "Old Tree: Keep that lesson with you."
        }, CloseDialogueAndReset);
    }

    private void TryActivateSideQuestFromDialogue(string line)
    {
        if (sideQuestActivatedOnce || string.IsNullOrEmpty(line))
        {
            return;
        }

        if (!line.Contains("safe shelter"))
        {
            return;
        }

        ActivateFairyBackstorySideQuest();
    }

    public void ActivateFairyBackstorySideQuest()
    {
        if (sideQuestActivatedOnce)
        {
            return;
        }

        sideQuestActivatedOnce = true;
        sideQuestActive = true;
        collectedFenceCount = 0;
        collectedSaplingCount = 0;
        plantedSaplingCount = 0;
        GlobalBackpackUI.RemoveAll(fenceInventoryName);
        GlobalBackpackUI.RemoveAll(saplingInventoryName);
        fenceBuilt = false;
        fenceBuildTargetShown = false;
        nearbyFenceBuildTarget = false;
        peasantRewardGiven = false;
        saplingPlantTargetsShown = false;
        nearbySaplingPlantTarget = null;
        if (fenceBuildTarget != null)
        {
            fenceBuildTarget.gameObject.SetActive(false);
        }

        PrepareSaplingPlantTargets();
        CollectFenceTargets();
    }

    private void CollectFenceTargets()
    {
        fenceCollectibles.Clear();

        Transform[] allTransforms = FindObjectsOfType<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            string normalizedName = NormalizeName(candidate.name);
            if (fenceBuildTarget != null && candidate == fenceBuildTarget)
            {
                continue;
            }

            if (NamesMatch(candidate.name, fenceBuildTargetName))
            {
                continue;
            }

            if (!normalizedName.StartsWith("fence"))
            {
                continue;
            }

            if (candidate.parent != null && NormalizeName(candidate.parent.name).StartsWith("fence"))
            {
                continue;
            }

            fenceCollectibles.Add(candidate);
        }
    }

    private void UpdateSideQuestCollection()
    {
        if (player == null)
        {
            ClearFenceHighlight();
            return;
        }

        if (collectedFenceCount >= requiredFenceCount)
        {
            UpdateFenceBuildTarget();
            return;
        }

        Transform closest = null;
        float closestDistance = fencePickupDistance * fencePickupDistance;
        for (int i = 0; i < fenceCollectibles.Count; i++)
        {
            Transform candidate = fenceCollectibles[i];
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(candidate.position - player.position);
            if (distance <= closestDistance)
            {
                closest = candidate;
                closestDistance = distance;
            }
        }

        if (closest != nearbyFence)
        {
            ClearFenceHighlight();
            nearbyFence = closest;
            HighlightFence(nearbyFence);
        }

        if (nearbyFence != null && Input.GetKeyDown(interactKey))
        {
            PickupNearbyFence();
        }
    }

    private void UpdateFenceBuildTarget()
    {
        if (fenceBuilt)
        {
            return;
        }

        if (fenceBuildTarget == null)
        {
            fenceBuildTarget = FindSceneTransform(fenceBuildTargetName);
        }

        if (fenceBuildTarget == null)
        {
            ClearFenceHighlight();
            nearbyFenceBuildTarget = false;
            return;
        }

        if (!fenceBuildTargetShown)
        {
            fenceBuildTarget.gameObject.SetActive(true);
            fenceBuildTargetShown = true;
            ApplyFenceBuildGhost();
        }

        bool isNearBuildTarget = Vector3.SqrMagnitude(fenceBuildTarget.position - player.position) <= fenceBuildDistance * fenceBuildDistance;
        if (isNearBuildTarget != nearbyFenceBuildTarget)
        {
            ClearFenceHighlight();
            if (isNearBuildTarget)
            {
                HighlightFence(fenceBuildTarget);
            }
        }

        nearbyFenceBuildTarget = isNearBuildTarget;

        if (nearbyFenceBuildTarget && Input.GetKeyDown(interactKey))
        {
            fenceBuilt = true;
            GlobalBackpackUI.RemoveAll(fenceInventoryName);
            ClearFenceHighlight();
            RestoreFenceBuildSolid();
            nearbyFenceBuildTarget = false;
        }
    }

    private bool IsSideQuestInProgress()
    {
        return sideQuestActive && (!fenceBuilt || collectedSaplingCount < requiredSaplingCount || plantedSaplingCount < saplingPlantTargets.Count);
    }

    private void PrepareSaplingPlantTargets()
    {
        saplingPlantTargets.Clear();
        for (int i = 0; i < saplingPlantTargetNames.Length; i++)
        {
            Transform target = FindSceneTransform(saplingPlantTargetNames[i]);
            if (target == null && saplingPreviewPrefab != null)
            {
                Vector3 spawnPosition = GetSaplingPlantPosition(i);
                GameObject spawned = Instantiate(saplingPreviewPrefab, spawnPosition, Quaternion.identity);
                spawned.name = saplingPlantTargetNames[i];
                target = spawned.transform;
            }

            if (target == null)
            {
                continue;
            }

            target.gameObject.SetActive(false);
            saplingPlantTargets.Add(target);
        }
    }

    private Vector3 GetSaplingPlantPosition(int index)
    {
        Vector3 basePosition = fenceBuildTarget != null ? fenceBuildTarget.position : transform.position;
        Vector3 offset = index < saplingPlantOffsets.Length ? saplingPlantOffsets[index] : Vector3.zero;
        return basePosition + offset;
    }

    private void ShowSaplingPlantTargets()
    {
        if (saplingPlantTargets.Count == 0)
        {
            PrepareSaplingPlantTargets();
        }

        if (saplingPlantTargetsShown)
        {
            return;
        }

        for (int i = 0; i < saplingPlantTargets.Count; i++)
        {
            Transform target = saplingPlantTargets[i];
            if (target == null)
            {
                continue;
            }

            target.gameObject.SetActive(true);
            ApplySaplingGhost(target);
        }

        saplingPlantTargetsShown = true;
    }

    private void UpdateSaplingPlanting()
    {
        if (collectedSaplingCount < requiredSaplingCount)
        {
            return;
        }

        ShowSaplingPlantTargets();

        Transform closest = null;
        float closestDistance = saplingPlantDistance * saplingPlantDistance;
        for (int i = 0; i < saplingPlantTargets.Count; i++)
        {
            Transform target = saplingPlantTargets[i];
            if (target == null || !target.gameObject.activeInHierarchy || !IsSaplingGhost(target))
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(target.position - player.position);
            if (distance <= closestDistance)
            {
                closest = target;
                closestDistance = distance;
            }
        }

        if (closest != nearbySaplingPlantTarget)
        {
            ClearFenceHighlight();
            nearbySaplingPlantTarget = closest;
            if (nearbySaplingPlantTarget != null)
            {
                HighlightFence(nearbySaplingPlantTarget);
            }
        }

        if (nearbySaplingPlantTarget != null && Input.GetKeyDown(interactKey))
        {
            Transform planted = nearbySaplingPlantTarget;
            ClearFenceHighlight();
            RestoreSaplingSolid(planted);
            nearbySaplingPlantTarget = null;
            plantedSaplingCount++;
            GlobalBackpackUI.SetItemCount(saplingInventoryName, Mathf.Max(0, collectedSaplingCount - plantedSaplingCount));
        }
    }

    private void ApplySaplingGhost(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            if (!material.HasProperty("_Color"))
            {
                continue;
            }

            if (!saplingGhostRenderers.Contains(renderer))
            {
                saplingGhostRenderers.Add(renderer);
                saplingGhostOriginalColors.Add(material.color);
            }

            Color ghostColor = material.color;
            ghostColor.a = 0.32f;
            material.color = ghostColor;
            SetMaterialTransparent(material, true);
        }
    }

    private bool IsSaplingGhost(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (saplingGhostRenderers.Contains(renderers[i]))
            {
                return true;
            }
        }

        return false;
    }

    private void RestoreSaplingSolid(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            int index = saplingGhostRenderers.IndexOf(renderer);
            if (index < 0)
            {
                continue;
            }

            Material material = renderer.material;
            if (material.HasProperty("_Color") && index < saplingGhostOriginalColors.Count)
            {
                material.color = saplingGhostOriginalColors[index];
            }

            SetMaterialTransparent(material, false);
            saplingGhostRenderers.RemoveAt(index);
            saplingGhostOriginalColors.RemoveAt(index);
        }
    }

    private void ApplyFenceBuildGhost()
    {
        fenceBuildRenderers.Clear();
        fenceBuildOriginalColors.Clear();

        if (fenceBuildTarget == null)
        {
            return;
        }

        Renderer[] renderers = fenceBuildTarget.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            if (!material.HasProperty("_Color"))
            {
                continue;
            }

            fenceBuildRenderers.Add(renderer);
            fenceBuildOriginalColors.Add(material.color);

            Color ghostColor = material.color;
            ghostColor.a = 0.32f;
            material.color = ghostColor;
            SetMaterialTransparent(material, true);
        }
    }

    private void RestoreFenceBuildSolid()
    {
        for (int i = 0; i < fenceBuildRenderers.Count; i++)
        {
            Renderer renderer = fenceBuildRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            if (material.HasProperty("_Color") && i < fenceBuildOriginalColors.Count)
            {
                material.color = fenceBuildOriginalColors[i];
            }

            SetMaterialTransparent(material, false);
        }

        fenceBuildRenderers.Clear();
        fenceBuildOriginalColors.Clear();
    }

    private static void SetMaterialTransparent(Material material, bool transparent)
    {
        if (material == null || !material.HasProperty("_Mode"))
        {
            return;
        }

        if (transparent)
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
        else
        {
            material.SetFloat("_Mode", 0f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
        }
    }

    private void PickupNearbyFence()
    {
        if (nearbyFence == null)
        {
            return;
        }

        Transform pickedFence = nearbyFence;
        ClearFenceHighlight();
        pickedFence.gameObject.SetActive(false);
        collectedFenceCount = Mathf.Min(collectedFenceCount + 1, requiredFenceCount);
        GlobalBackpackUI.SetItemCount(fenceInventoryName, collectedFenceCount);
        nearbyFence = null;
    }

    private void HighlightFence(Transform target)
    {
        if (target == null)
        {
            return;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Material material = renderer.material;
            highlightedFenceRenderers.Add(renderer);
            highlightedFenceOriginalColors.Add(material.HasProperty("_Color") ? material.color : Color.white);
            highlightedFenceOriginalEmissionColors.Add(material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black);
            highlightedFenceHadEmissionEnabled.Add(material.IsKeywordEnabled("_EMISSION"));

            if (material.HasProperty("_Color"))
            {
                material.color = fenceHighlightColor;
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", fenceHighlightColor * 1.5f);
            }
        }
    }

    private void ClearFenceHighlight()
    {
        for (int i = 0; i < highlightedFenceRenderers.Count; i++)
        {
            Renderer renderer = highlightedFenceRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            if (material.HasProperty("_Color") && i < highlightedFenceOriginalColors.Count)
            {
                material.color = highlightedFenceOriginalColors[i];
            }

            if (material.HasProperty("_EmissionColor") && i < highlightedFenceOriginalEmissionColors.Count)
            {
                material.SetColor("_EmissionColor", highlightedFenceOriginalEmissionColors[i]);
            }

            bool hadEmission = i < highlightedFenceHadEmissionEnabled.Count && highlightedFenceHadEmissionEnabled[i];
            if (hadEmission)
            {
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }
        }

        highlightedFenceRenderers.Clear();
        highlightedFenceOriginalColors.Clear();
        highlightedFenceOriginalEmissionColors.Clear();
        highlightedFenceHadEmissionEnabled.Clear();
        nearbyFence = null;
    }

    private void EnableMushroomGlow()
    {
        DisableMushroomGlow();

        if (mushroomGift == null)
        {
            return;
        }

        Renderer[] renderers = mushroomGift.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Material material = renderer.material;
            mushroomGlowRenderers.Add(renderer);
            mushroomOriginalColors.Add(material.HasProperty("_Color") ? material.color : Color.white);
            mushroomOriginalEmissionColors.Add(material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black);

            if (material.HasProperty("_Color"))
            {
                material.color = mushroomGlowColor;
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", mushroomGlowColor * mushroomGlowIntensity);
            }
        }

        GameObject lightObject = new GameObject("Mu_Gift_Light");
        lightObject.transform.SetParent(mushroomGift, false);
        lightObject.transform.localPosition = Vector3.up * 1.2f;
        mushroomGiftLight = lightObject.AddComponent<Light>();
        mushroomGiftLight.type = LightType.Point;
        mushroomGiftLight.color = mushroomGlowColor;
        mushroomGiftLight.intensity = mushroomGlowIntensity;
        mushroomGiftLight.range = 6f;
    }

    private void DisableMushroomGlow()
    {
        for (int i = 0; i < mushroomGlowRenderers.Count; i++)
        {
            Renderer renderer = mushroomGlowRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            if (material.HasProperty("_Color") && i < mushroomOriginalColors.Count)
            {
                material.color = mushroomOriginalColors[i];
            }

            if (material.HasProperty("_EmissionColor") && i < mushroomOriginalEmissionColors.Count)
            {
                material.SetColor("_EmissionColor", mushroomOriginalEmissionColors[i]);
            }
        }

        mushroomGlowRenderers.Clear();
        mushroomOriginalColors.Clear();
        mushroomOriginalEmissionColors.Clear();

        if (mushroomGiftLight != null)
        {
            Destroy(mushroomGiftLight.gameObject);
            mushroomGiftLight = null;
        }
    }

    private void SpawnEggGrid(int gridSize, GameObject oddPrefab)
    {
        if (eggPrefab == null || oddPrefab == null)
        {
            FailEggChallenge();
            return;
        }

        eggGridRoot = new GameObject("Old Tree Egg Challenge");
        Transform cameraTransform = Camera.main != null ? Camera.main.transform : null;
        Vector3 center;
        Vector3 right;
        Vector3 up;
        Quaternion rotation;

        if (cameraTransform != null)
        {
            center = cameraTransform.position + cameraTransform.forward * eggGridDistance;
            right = cameraTransform.right;
            up = cameraTransform.up;
            rotation = Quaternion.LookRotation(-cameraTransform.forward, cameraTransform.up);
        }
        else
        {
            center = interactionTarget.position + Vector3.up * 2.5f + transform.forward * eggGridDistance;
            right = transform.right;
            up = Vector3.up;
            rotation = transform.rotation;
        }

        int totalCount = gridSize * gridSize;
        int oddIndex = Random.Range(0, totalCount);
        float half = (gridSize - 1) * 0.5f;

        for (int i = 0; i < totalCount; i++)
        {
            int x = i % gridSize;
            int y = i / gridSize;
            bool isOdd = i == oddIndex;
            GameObject prefab = isOdd ? oddPrefab : eggPrefab;
            Vector3 position = center + right * ((x - half) * eggGridSpacing) + up * ((half - y) * eggGridSpacing);
            GameObject egg = Instantiate(prefab, position, rotation, eggGridRoot.transform);
            egg.transform.localScale = Vector3.one * eggScale;
            EnsureClickableCollider(egg);
            spawnedEggs.Add(egg);

            if (isOdd)
            {
                correctEgg = egg;
            }
        }
    }

    private void EnsureClickableCollider(GameObject target)
    {
        if (target.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        BoxCollider collider = target.AddComponent<BoxCollider>();
        if (renderers.Length == 0)
        {
            collider.size = Vector3.one;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        collider.center = target.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = target.transform.InverseTransformVector(bounds.size);
        collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
    }

    private void ClearEggGrid()
    {
        for (int i = 0; i < spawnedEggs.Count; i++)
        {
            if (spawnedEggs[i] != null)
            {
                Destroy(spawnedEggs[i]);
            }
        }

        spawnedEggs.Clear();
        correctEgg = null;

        if (eggGridRoot != null)
        {
            Destroy(eggGridRoot);
            eggGridRoot = null;
        }
    }

    private int GetEggGridSize(int level)
    {
        if (level == 1)
        {
            return 5;
        }

        if (level == 2)
        {
            return 10;
        }

        return 20;
    }

    private GameObject GetOddEggPrefab(int level)
    {
        if (level == 1)
        {
            return levelOneOddEggPrefab;
        }

        if (level == 2)
        {
            return levelTwoOddEggPrefab;
        }

        return levelThreeOddEggPrefab;
    }

    private string GetEggLevelTitle()
    {
        if (currentEggLevel == 1)
        {
            return levelOneTitle;
        }

        if (currentEggLevel == 2)
        {
            return levelTwoTitle;
        }

        return levelThreeTitle;
    }

    private string GetEggSuccessText(int level)
    {
        if (level == 1)
        {
            return levelOneSuccessText;
        }

        if (level == 2)
        {
            return levelTwoSuccessText;
        }

        return gameSuccessText;
    }

    private void StartLookCoroutine(IEnumerator routine)
    {
        if (lookCoroutine != null)
        {
            StopCoroutine(lookCoroutine);
        }

        lookCoroutine = StartCoroutine(routine);
    }

    private void LockPlayerControl()
    {
        disabledPlayerBehaviours.Clear();

        PlayerCharacterController playerController = player.GetComponent<PlayerCharacterController>();
        if (playerController != null)
        {
            if (playerController.enabled)
            {
                playerController.enabled = false;
                disabledPlayerBehaviours.Add(playerController);
            }
        }

        MonoBehaviour[] playerBehaviours = player.GetComponents<MonoBehaviour>();
        for (int i = 0; i < playerBehaviours.Length; i++)
        {
            MonoBehaviour behaviour = playerBehaviours[i];
            if (behaviour != null && behaviour.enabled && !disabledPlayerBehaviours.Contains(behaviour))
            {
                behaviour.enabled = false;
                disabledPlayerBehaviours.Add(behaviour);
            }
        }
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

    private void CollectAttackCylinders(Transform root)
    {
        if (root == null)
        {
            return;
        }

        if (IsAttackCylinder(root))
        {
            attackCylinders.Add(root);
            attackCylinderOriginalPositions.Add(root.position);
            attackCylinderOriginalRotations.Add(root.rotation);
            attackCylinderOriginalScales.Add(root.localScale);
            attackCylinderMoveAxes.Add(GetIrregularMoveAxis(root));
            attackCylinderSeeds.Add(attackCylinders.Count * 1.73f);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectAttackCylinders(root.GetChild(i));
        }
    }

    private bool IsAttackCylinder(Transform target)
    {
        string normalizedName = NormalizeName(target.name);
        if (!normalizedName.Contains("cylinder") && !normalizedName.Contains("cyliner"))
        {
            return false;
        }

        if (NamesMatch(target.name, nestBranchName) ||
            NamesMatch(target.name, cylinderResetName) ||
            NamesMatch(target.name, cylinderResetFallbackName))
        {
            return false;
        }

        return target != nestBranch && target != cylinderResetTarget;
    }

    private Vector3 GetIrregularMoveAxis(Transform target)
    {
        float seed = Mathf.Abs(target.name.GetHashCode() % 1000) * 0.01f;
        Vector3 axis = new Vector3(
            Mathf.Sin(seed * 1.7f),
            Mathf.Cos(seed * 2.3f),
            Mathf.Sin(seed * 3.1f));

        if (axis.sqrMagnitude < 0.01f)
        {
            axis = Vector3.up;
        }

        return axis.normalized;
    }

    private IEnumerator AnimateAttackCylinders()
    {
        while (state == DialogueState.Attacking)
        {
            float time = Time.time * attackMoveSpeed;
            for (int i = 0; i < attackCylinders.Count; i++)
            {
                Transform target = attackCylinders[i];
                if (target == null)
                {
                    continue;
                }

                float seed = attackCylinderSeeds[i];
                Vector3 basePosition = attackCylinderOriginalPositions[i];
                Vector3 axis = attackCylinderMoveAxes[i];
                Vector3 offset = axis * Mathf.Sin(time + seed) * attackMoveAmount;
                offset += Vector3.up * Mathf.Cos(time * 1.37f + seed) * attackMoveAmount * 0.55f;

                target.position = basePosition + offset;
                target.Rotate(
                    attackRotateSpeed * Time.deltaTime * (0.6f + Mathf.Abs(Mathf.Sin(seed))),
                    attackRotateSpeed * Time.deltaTime * (0.8f + Mathf.Abs(Mathf.Cos(seed))),
                    attackRotateSpeed * Time.deltaTime,
                    Space.Self);
            }

            yield return null;
        }

        cylinderAttackCoroutine = null;
    }

    private IEnumerator SweepAndLaunchPlayer()
    {
        yield return new WaitForSeconds(0.28f);

        Transform sweeper = GetSweepCylinder();
        if (sweeper != null)
        {
            yield return SweepCylinderThroughPlayer(sweeper);
        }

        yield return LaunchPlayerHigh();
        playerLaunchCoroutine = null;
    }

    private Transform GetSweepCylinder()
    {
        Transform best = null;
        float bestDistance = float.MaxValue;
        Vector3 playerPosition = player.position;

        for (int i = 0; i < attackCylinders.Count; i++)
        {
            Transform candidate = attackCylinders[i];
            if (candidate == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(candidate.position - playerPosition);
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    private IEnumerator SweepCylinderThroughPlayer(Transform sweeper)
    {
        Vector3 playerPosition = player.position + Vector3.up * sweepHeightOffset;
        Vector3 direction = playerPosition - interactionTarget.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = transform.forward;
        }

        direction.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
        Vector3 startPosition = playerPosition - side * sweepStartDistance;
        Vector3 endPosition = playerPosition + side * sweepEndDistance;
        Quaternion startRotation = sweeper.rotation;
        float elapsed = 0f;

        while (elapsed < sweepDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sweepDuration);
            sweeper.position = Vector3.Lerp(startPosition, endPosition, t);
            sweeper.rotation = startRotation * Quaternion.Euler(720f * t, 0f, 1080f * t);
            yield return null;
        }

        sweeper.position = endPosition;
    }

    private IEnumerator LaunchPlayerHigh()
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }

        PlayPlayerController(fallingController);

        Vector3 startPosition = player.position;
        Vector3 launchDirection = startPosition - interactionTarget.position;
        launchDirection.y = 0f;

        if (launchDirection.sqrMagnitude < 0.01f)
        {
            launchDirection = transform.forward;
        }

        launchDirection.Normalize();
        float topY = Mathf.Max(launchTopY, startPosition.y);
        Vector3 topPosition = startPosition + launchDirection * launchForwardDistance;
        topPosition.y = topY;
        float elapsed = 0f;

        while (elapsed < launchRiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / launchRiseDuration);
            float arc = Mathf.Sin(t * Mathf.PI * 0.5f);
            player.position = Vector3.Lerp(startPosition, topPosition, arc);
            SetLaunchCameraView(launchDirection);
            yield return null;
        }

        player.position = topPosition;
        SetLaunchCameraView(launchDirection);

        StopAttackCylindersInPlace();

        Vector3 impactPosition = topPosition + launchDirection * launchForwardDistance;
        impactPosition.y = impactY;
        elapsed = 0f;

        while (elapsed < launchFallDuration && player.position.y > impactY)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / launchFallDuration);
            float fall = t * t;
            player.position = Vector3.Lerp(topPosition, impactPosition, fall);
            SetLaunchCameraView(launchDirection);
            yield return null;
        }

        player.position = impactPosition;
        SetImpactCameraView();
        PlayPlayerController(impactController);
        yield return new WaitForSeconds(impactHoldDuration);

        PlayPlayerController(standingUpController);
        yield return WaitForCurrentPlayerAnimation(standingUpDuration);
        yield return new WaitForSeconds(postStandingResetDelay);

        ResetAttackSequence();
    }

    private void PlayPlayerController(RuntimeAnimatorController controller)
    {
        if (controller == null)
        {
            return;
        }

        FindPlayerAnimator();

        if (playerAnimator == null)
        {
            return;
        }

        playerAnimator.enabled = true;
        playerAnimator.runtimeAnimatorController = controller;

        if (!string.IsNullOrEmpty(controllerStateName))
        {
            playerAnimator.Play(controllerStateName, 0, 0f);
        }
    }

    private IEnumerator WaitForCurrentPlayerAnimation(float fallbackDuration)
    {
        yield return null;

        if (playerAnimator == null)
        {
            yield return new WaitForSeconds(fallbackDuration);
            yield break;
        }

        AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
        float speed = Mathf.Abs(stateInfo.speed);
        float duration = speed > 0.01f ? stateInfo.length / speed : fallbackDuration;
        if (duration <= 0.01f)
        {
            duration = fallbackDuration;
        }

        yield return new WaitForSeconds(duration);
    }

    private void StopAttackCylindersInPlace()
    {
        if (cylinderAttackCoroutine != null)
        {
            StopCoroutine(cylinderAttackCoroutine);
            cylinderAttackCoroutine = null;
        }
    }

    private void SetCameraLocalView(Vector3 localPosition, Quaternion localRotation)
    {
        FindPlayerCamera();
        if (playerCameraTransform == null)
        {
            return;
        }

        playerCameraTransform.localPosition = localPosition;
        playerCameraTransform.localRotation = localRotation;
    }

    private void SetLaunchCameraView(Vector3 launchDirection)
    {
        FindPlayerCamera();
        if (playerCameraTransform == null || player == null)
        {
            return;
        }

        Vector3 flatDirection = hasCameraOriginal ? originalCameraForward : player.forward;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.01f)
        {
            flatDirection = launchDirection;
            flatDirection.y = 0f;
        }

        if (flatDirection.sqrMagnitude < 0.01f)
        {
            flatDirection = player.forward;
        }

        flatDirection.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, flatDirection).normalized;
        Vector3 cameraPosition = player.position
            - flatDirection * Mathf.Abs(launchCameraOffset.z)
            + side * launchCameraOffset.x
            + Vector3.up * launchCameraOffset.y;

        Vector3 lookTarget = player.position + launchCameraLookOffset;
        Vector3 lookDirection = lookTarget - cameraPosition;
        if (lookDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        playerCameraTransform.position = cameraPosition;
        playerCameraTransform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    private void SetImpactCameraView()
    {
        FindPlayerCamera();
        if (playerCameraTransform == null || player == null)
        {
            return;
        }

        playerCameraTransform.localPosition = impactCameraLocalPosition;
        Vector3 lookTarget = player.position + impactCameraLookOffset;
        Vector3 direction = lookTarget - playerCameraTransform.position;
        if (direction.sqrMagnitude > 0.01f)
        {
            playerCameraTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void ResetAttackSequence()
    {
        StopAttackCylindersInPlace();
        ResetTreeToInitialState();
        RestorePlayerState();
        currentAnswer = null;
        state = DialogueState.Waiting;
        playerLaunchCoroutine = null;
        StartLookCoroutine(ReturnToOriginalRotation());
    }

    private void RestoreAttackCylinders()
    {
        if (!hasAttackOriginals)
        {
            return;
        }

        for (int i = 0; i < attackCylinders.Count; i++)
        {
            Transform target = attackCylinders[i];
            if (target == null || i >= attackCylinderOriginalPositions.Count || i >= attackCylinderOriginalRotations.Count)
            {
                continue;
            }

            target.position = attackCylinderOriginalPositions[i];
            target.rotation = attackCylinderOriginalRotations[i];
            if (i < attackCylinderOriginalScales.Count)
            {
                target.localScale = attackCylinderOriginalScales[i];
            }
        }
    }

    private void RestorePlayerState()
    {
        if (player != null && hasPlayerOriginal)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            player.position = originalPlayerPosition;
            player.rotation = originalPlayerRotation;

            if (controller != null)
            {
                controller.enabled = characterControllerWasEnabled;
            }

            Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                playerRigidbody.velocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
                playerRigidbody.isKinematic = rigidbodyWasKinematic;
            }
        }

        if (hasCameraOriginal)
        {
            SetCameraLocalView(originalCameraLocalPosition, originalCameraLocalRotation);
        }

        FindPlayerAnimator();
        if (playerAnimator != null && hasAnimatorOriginal)
        {
            playerAnimator.runtimeAnimatorController = originalAnimatorController;
        }

        for (int i = 0; i < disabledPlayerBehaviours.Count; i++)
        {
            MonoBehaviour behaviour = disabledPlayerBehaviours[i];
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        disabledPlayerBehaviours.Clear();
    }

    private void FindPlayerAnimator()
    {
        if (playerAnimator != null)
        {
            return;
        }

        if (player != null)
        {
            playerAnimator = player.GetComponentInChildren<Animator>();
        }

        if (playerAnimator != null || player == null)
        {
            return;
        }

        Animator[] animators = FindObjectsOfType<Animator>();
        float bestDistance = float.MaxValue;
        for (int i = 0; i < animators.Length; i++)
        {
            Animator candidate = animators[i];
            if (candidate == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(candidate.transform.position - player.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                playerAnimator = candidate;
            }
        }
    }

    private void FindPlayerCamera()
    {
        if (playerCameraTransform != null)
        {
            return;
        }

        Camera playerCamera = null;
        if (player != null)
        {
            playerCamera = player.GetComponentInChildren<Camera>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera != null)
        {
            playerCameraTransform = playerCamera.transform;
        }
    }

    private void KeepPlayerInsideTreeRange()
    {
        Vector3 center = interactionTarget.position;
        Vector3 playerPosition = player.position;
        Vector3 offset = playerPosition - center;
        offset.y = 0f;

        if (offset.magnitude <= interactDistance)
        {
            return;
        }

        Vector3 clampedPosition = center + offset.normalized * interactDistance;
        clampedPosition.y = playerPosition.y;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            player.position = clampedPosition;
            controller.enabled = true;
        }
        else
        {
            player.position = clampedPosition;
        }
    }

    private void CacheResetTransforms()
    {
        hasInteractionTargetOriginal = TryCacheTransform(interactionTarget, out interactionTargetOriginalPosition, out interactionTargetOriginalRotation, out interactionTargetOriginalScale);
        hasNestBranchOriginal = TryCacheTransform(nestBranch, out nestBranchOriginalPosition, out nestBranchOriginalRotation, out nestBranchOriginalScale);
        hasCylinderOriginal = TryCacheTransform(cylinderResetTarget, out cylinderOriginalPosition, out cylinderOriginalRotation, out cylinderOriginalScale);
        hasNestOriginal = TryCacheTransform(nest, out nestOriginalPosition, out nestOriginalRotation, out nestOriginalScale);
        hasFaceOriginal = TryCacheTransform(faceResetTarget, out faceOriginalPosition, out faceOriginalRotation, out faceOriginalScale);
        hasMushroomOriginal = TryCacheTransform(mushroomGift, out mushroomOriginalPosition, out mushroomOriginalRotation, out mushroomOriginalScale);
    }

    private static bool TryCacheTransform(Transform target, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        if (target == null)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one;
            return false;
        }

        position = target.position;
        rotation = target.rotation;
        scale = target.localScale;
        return true;
    }

    private void ResetTreeToInitialState()
    {
        DisableMushroomGlow();
        RestoreTransform(interactionTarget, hasInteractionTargetOriginal, interactionTargetOriginalPosition, interactionTargetOriginalRotation, interactionTargetOriginalScale);
        RestoreTransform(nestBranch, hasNestBranchOriginal, nestBranchOriginalPosition, nestBranchOriginalRotation, nestBranchOriginalScale);
        RestoreTransform(cylinderResetTarget, hasCylinderOriginal, cylinderOriginalPosition, cylinderOriginalRotation, cylinderOriginalScale);
        RestoreTransform(nest, hasNestOriginal, nestOriginalPosition, nestOriginalRotation, nestOriginalScale);
        RestoreTransform(faceResetTarget, hasFaceOriginal, faceOriginalPosition, faceOriginalRotation, faceOriginalScale);
        RestoreTransform(mushroomGift, hasMushroomOriginal, mushroomOriginalPosition, mushroomOriginalRotation, mushroomOriginalScale);
        RestoreAttackCylinders();
        lookRoot.rotation = originalRotation;
    }

    private static void RestoreTransform(Transform target, bool hasOriginal, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (target == null || !hasOriginal)
        {
            return;
        }

        target.position = position;
        target.rotation = rotation;
        target.localScale = scale;
    }

    private void OnGUI()
    {
        if (!IsTargetScene())
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        if (state == DialogueState.Waiting && IsPlayerNear())
        {
            if (!sideQuestActive || (nearbyFence == null && !nearbyFenceBuildTarget && nearbySaplingPlantTarget == null && !IsPlayerNearPeasant()))
            {
                DrawCenteredLabel(prompt, Screen.height * 0.72f, 28);
            }
        }

        if (state == DialogueState.Choosing)
        {
            DrawDialogueBox(greeting, true);
        }
        else if (state == DialogueState.Speaking)
        {
            DrawDialogueBox(currentAnswer, false, true);
        }
        else if (state == DialogueState.FinalInstruction)
        {
            DrawDialogueBox(currentAnswer, false, false);
        }
        else if (state == DialogueState.MovingNest)
        {
            DrawDialogueBox("The nest is moving down...", false, false);
        }
        else if (state == DialogueState.Attacking)
        {
            DrawDialogueBox(currentAnswer, false);
        }
        else if (state == DialogueState.Answered)
        {
            DrawDialogueBox(currentAnswer, false);
        }
        else if (state == DialogueState.EggChallenge)
        {
            DrawEggChallengePanel();
        }
        else if (state == DialogueState.EggChallengeResult)
        {
            DrawCenteredResult(eggResultText);
        }
        else if (state == DialogueState.EggChallengeFailed)
        {
            DrawEggFailurePanel();
        }
        else if (state == DialogueState.RewardChoosing)
        {
            DrawRewardChoiceBox();
        }
        else if (state == DialogueState.MushroomGift)
        {
            DrawCenteredLabel(mushroomPickupPrompt, Screen.height * 0.72f, 28);
        }

        if (sideQuestActive)
        {
            DrawSideQuestPanel();

            if (nearbyFence != null)
            {
                DrawCenteredLabel(fencePickupPrompt, Screen.height * 0.68f, 26);
            }
            else if (nearbyFenceBuildTarget)
            {
                DrawCenteredLabel(fenceBuildPrompt, Screen.height * 0.68f, 26);
            }
            else if (nearbySaplingPlantTarget != null)
            {
                DrawCenteredLabel(saplingPlantPrompt, Screen.height * 0.68f, 26);
            }
            else if (IsSideQuestInProgress() && !peasantRewardGiven && IsPlayerNearPeasant())
            {
                DrawCenteredLabel(prompt, Screen.height * 0.68f, 26);
            }
        }
    }

    private void DrawDialogueBox(string text, bool showChoices)
    {
        DrawDialogueBox(text, showChoices, false);
    }

    private void DrawDialogueBox(string text, bool showChoices, bool showContinueHint)
    {
        float height = showChoices ? 330f : (showContinueHint ? 190f : 160f);
        Rect rect = GameUiStyle.DialogueRect(height);

        GameUiStyle.DrawPanel(rect);

        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        ApplyDialogueFont(textStyle);
        textStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(rect.x + 24f, rect.y + 18f, rect.width - 48f, showContinueHint ? 92f : 70f), text, textStyle);

        if (showContinueHint)
        {
            GUIStyle continueHintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleRight
            };
            ApplyDialogueFont(continueHintStyle);
            continueHintStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            GUI.Label(new Rect(rect.x + 24f, rect.y + rect.height - 42f, rect.width - 48f, 24f), continueHint, continueHintStyle);
        }

        if (!showChoices)
        {
            return;
        }

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleLeft
        };
        ApplyDialogueFont(hintStyle);
        hintStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
        GUI.Label(new Rect(rect.x + 24f, rect.y + 84f, rect.width - 48f, 24f), chooseHint, hintStyle);

        if (DrawChoiceButton(rect, 118f, 36f, choiceA))
        {
            Choose(answerA);
        }

        if (DrawChoiceButton(rect, 164f, 58f, choiceB))
        {
            StartBranchDialogue();
        }

        if (DrawChoiceButton(rect, 236f, 36f, choiceC))
        {
            StartAngryAttack();
        }
    }

    private bool DrawChoiceButton(Rect parent, float yOffset, float height, string text)
    {
        Rect rect = new Rect(parent.x + 24f, parent.y + yOffset, parent.width - 48f, height);
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 24,
            wordWrap = true
        };
        ApplyDialogueFont(style);

        return GUI.Button(rect, text, style);
    }

    private void DrawEggChallengePanel()
    {
        float width = Mathf.Min(420f, Screen.width - 40f);
        Rect rect = GameUiStyle.SystemPromptRect(width, 104f);
        GameUiStyle.DrawPanel(rect);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 26
        };
        ApplyDialogueFont(titleStyle);
        titleStyle.normal.textColor = Color.white;

        GUIStyle infoStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18
        };
        ApplyDialogueFont(infoStyle);
        infoStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, rect.width - 32f, 34f), GetEggLevelTitle(), titleStyle);
        GUI.Label(new Rect(rect.x + 16f, rect.y + 52f, rect.width - 32f, 28f), "Time: " + Mathf.CeilToInt(eggTimer) + "s", infoStyle);
    }

    private void DrawCenteredResult(string text)
    {
        float width = Mathf.Min(520f, Screen.width - 40f);
        Rect rect = GameUiStyle.SystemPromptRect(width, 110f);
        GameUiStyle.DrawPanel(rect);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 26,
            wordWrap = true
        };
        ApplyDialogueFont(style);
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(rect.x + 20f, rect.y + 20f, rect.width - 40f, rect.height - 40f), text, style);
    }

    private void DrawSideQuestPanel()
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
        ApplyDialogueFont(titleStyle);
        titleStyle.normal.textColor = Color.white;

        GUIStyle taskStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        ApplyDialogueFont(taskStyle);
        taskStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f);

        GUI.Label(new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, 44f), sideQuestTitle, titleStyle);
        string fenceCompletionMark = collectedFenceCount >= requiredFenceCount ? " done" : string.Empty;
        string saplingCompletionMark = collectedSaplingCount >= requiredSaplingCount ? " done" : string.Empty;
        GUI.Label(new Rect(rect.x + 14f, rect.y + 62f, rect.width - 28f, 28f), "1: " + fenceTaskText + " " + collectedFenceCount + "/" + requiredFenceCount + fenceCompletionMark, taskStyle);
        GUI.Label(new Rect(rect.x + 14f, rect.y + 96f, rect.width - 28f, 28f), "2: " + saplingTaskText + " " + collectedSaplingCount + "/" + requiredSaplingCount + saplingCompletionMark, taskStyle);
    }

    private void DrawBackpackPanel()
    {
        float slotSize = 72f;
        Rect panelRect = GameUiStyle.BackpackRect(260f, 94f);
        GameUiStyle.DrawPanel(panelRect);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleLeft
        };
        ApplyDialogueFont(labelStyle);
        labelStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 8f, 120f, 22f), "Backpack", labelStyle);

        int availableFenceCount = fenceBuilt ? 0 : collectedFenceCount;
        int availableSaplingCount = Mathf.Max(0, collectedSaplingCount - plantedSaplingCount);
        if (availableFenceCount <= 0 && availableSaplingCount <= 0)
        {
            GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 42f, panelRect.width - 24f, 24f), "Empty", labelStyle);
            return;
        }

        Rect slotRect = new Rect(panelRect.x + 12f, panelRect.y + 34f, slotSize, slotSize - 12f);
        if (availableFenceCount > 0)
        {
            GUI.Box(slotRect, GUIContent.none);
            GUI.Label(new Rect(slotRect.x + 8f, slotRect.y + 8f, slotRect.width - 16f, 22f), fenceInventoryName, labelStyle);
            GUI.Label(new Rect(slotRect.x + 8f, slotRect.y + 32f, slotRect.width - 16f, 22f), "x" + availableFenceCount, labelStyle);
        }

        if (availableSaplingCount > 0)
        {
            Rect saplingSlotRect = availableFenceCount > 0
                ? new Rect(slotRect.xMax + 12f, slotRect.y, slotSize, slotSize - 12f)
                : slotRect;
            GUI.Box(saplingSlotRect, GUIContent.none);
            GUI.Label(new Rect(saplingSlotRect.x + 8f, saplingSlotRect.y + 8f, saplingSlotRect.width - 16f, 22f), saplingInventoryName, labelStyle);
            GUI.Label(new Rect(saplingSlotRect.x + 8f, saplingSlotRect.y + 32f, saplingSlotRect.width - 16f, 22f), "x" + availableSaplingCount, labelStyle);
        }
    }

    private void DrawEggFailurePanel()
    {
        float width = Mathf.Min(520f, Screen.width - 40f);
        Rect rect = GameUiStyle.SystemPromptRect(width, 210f);
        GameUiStyle.DrawPanel(rect);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 26,
            wordWrap = true
        };
        ApplyDialogueFont(style);
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(rect.x + 20f, rect.y + 20f, rect.width - 40f, 54f), eggResultText, style);

        if (GUI.Button(new Rect(rect.x + 60f, rect.y + 112f, 170f, 48f), "Restart"))
        {
            RestartEggChallenge();
        }

        if (GUI.Button(new Rect(rect.x + rect.width - 230f, rect.y + 112f, 170f, 48f), "Exit"))
        {
            ExitEggChallenge();
        }
    }

    private void DrawRewardChoiceBox()
    {
        float width = Mathf.Min(900f, Screen.width - 80f);
        float height = 320f;
        Rect rect = GameUiStyle.DialogueRect(height);

        GameUiStyle.DrawPanel(rect);

        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        ApplyDialogueFont(textStyle);
        textStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(rect.x + 24f, rect.y + 18f, rect.width - 48f, 60f), rewardGreeting, textStyle);

        if (DrawRewardButton(rect, 92f, rewardChoiceA))
        {
            ChooseReward(rewardChoiceA);
        }

        if (DrawRewardButton(rect, 142f, rewardChoiceB))
        {
            ChooseReward(rewardChoiceB);
        }

        if (DrawRewardButton(rect, 192f, rewardChoiceC))
        {
            ChooseReward(rewardChoiceC);
        }

        if (DrawRewardButton(rect, 242f, rewardChoiceD))
        {
            ChooseReward(rewardChoiceD);
        }
    }

    private bool DrawRewardButton(Rect parent, float yOffset, string text)
    {
        Rect rect = new Rect(parent.x + 24f, parent.y + yOffset, parent.width - 48f, 38f);
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 18,
            wordWrap = true
        };
        ApplyDialogueFont(style);

        return GUI.Button(rect, text, style);
    }

    private void DrawCenteredLabel(string text, float y, int fontSize)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize
        };
        ApplyDialogueFont(style);
        style.normal.textColor = Color.white;

        Rect rect = GameUiStyle.InteractionPromptRect(520f, 60f);
        GameUiStyle.DrawPanel(rect);
        GUI.Label(rect, text, style);
    }

    private void ApplyDialogueFont(GUIStyle style)
    {
        if (dialogueFont != null)
        {
            style.font = dialogueFont;
        }
    }

    private bool IsPlayerNear()
    {
        Vector3 targetPosition = interactionTarget.position;
        Vector3 playerPosition = player.position;
        targetPosition.y = 0f;
        playerPosition.y = 0f;

        return Vector3.Distance(targetPosition, playerPosition) <= interactDistance;
    }

    private bool IsPlayerNearPeasant()
    {
        FindPeasantGirl();
        if (peasantGirl == null || player == null)
        {
            return false;
        }

        Vector3 targetPosition = peasantGirl.position;
        Vector3 playerPosition = player.position;
        targetPosition.y = 0f;
        playerPosition.y = 0f;

        return Vector3.Distance(targetPosition, playerPosition) <= peasantInteractDistance;
    }

    private Transform FindSceneTransform(string objectName)
    {
        Transform found = FindChildByName(transform, objectName);
        if (found != null)
        {
            return found;
        }

        GameObject foundObject = GameObject.Find(objectName);
        if (foundObject != null)
        {
            return foundObject.transform;
        }

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (candidate != null && NamesMatch(candidate.name, objectName) && candidate.gameObject.scene.IsValid())
            {
                return candidate;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        if (NamesMatch(root.name, childName))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool NamesMatch(string sceneName, string searchName)
    {
        if (string.Equals(sceneName, searchName, System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return NormalizeName(sceneName) == NormalizeName(searchName);
    }

    private bool IsTargetScene()
    {
        return SceneManager.GetActiveScene().name == targetSceneName;
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char character = value[i];
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }
}
