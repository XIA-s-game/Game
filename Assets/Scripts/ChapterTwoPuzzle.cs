// Main function: Controls the Chapter Two Fae Homes flow, including virtue quizzes, the dice board game, the honey side quest, key collection, maze progression, and rewards.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChapterTwoPuzzle : MonoBehaviour
{
    private const int BoardTileCount = 21;
    private const int DiceFaceCount = 7;

    [Header("Scene")]
    [SerializeField] private string targetSceneName = "Fae Homes Demo";
    [SerializeField] private string thirdPageItemName = "Third Page";
    [SerializeField] private string nextSceneName = "my scene";

    [Header("Item Names")]
    [SerializeField] private string mazePassItemName = "Maze Pass";
    [SerializeField] private string secondPageItemName = "Second Page";
    [SerializeField] private string redKeyItemName = "Red Key";
    [SerializeField] private string blueKeyItemName = "Blue Key";
    [SerializeField] private string greenKeyItemName = "Green Key";
    [SerializeField] private string yellowKeyItemName = "Yellow Key";
    [SerializeField] private string honeyJarItemName = "Honey Jar";
    [SerializeField] private string fullHoneyJarItemName = "Full Honey Jar";
    [SerializeField] private string silverLeafItemName = "Silver Leaf";

    [Header("Tuning")]
    [SerializeField] private Vector3 openedMazeBlockPosition = new Vector3(355.87f, 16.58f, 663.52f);
    [SerializeField] private Vector3 mazeColliderCenter = new Vector3(356.34f, 16.58f, 663.52f);
    [SerializeField] private Vector3 mazeColliderExtents = new Vector3(70f, 28f, 70f);
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private float boardInteractDistance = 3.5f;
    [SerializeField] private float boardMoveSpeed = 4f;
    [SerializeField] private float diceThrowSeconds = 1.8f;
    [SerializeField] private float diceThrowHeight = 4.5f;
    [SerializeField] private float playerGroundOffset = 0.01f;
    [SerializeField] private float exitDistance = 0.9f;
    [SerializeField] private float guardMoveRightDistance = 2f;
    [SerializeField] private float airWallTwoDropDistance = 100f;
    [SerializeField] private float airWallTwoDropSpeed = 12f;
    [SerializeField] private float lockPartTargetLocalY = 0.27f;
    [SerializeField] private float keyDropDistance = 40f;
    [SerializeField] private float finalUnlockMoveSeconds = 1.2f;
    [SerializeField] private float lookPadRadius = 258f;
    [SerializeField] private float lookPadKnobRadius = 54f;
    [SerializeField] private float lookPadSensitivity = 0.55f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode continueKey = KeyCode.C;

    [Header("Dialogue")]
    [SerializeField] private string[] boxFoundDialogue = { "Player: The third page is in this box." };
    [SerializeField] private string[] lockedHouseDialogue = { "Player: This house is locked. I need four keys." };
    [SerializeField] private string[] listenerDialogue =
    {
        "Player: Do you know where the third page is?",
        "Villager: Try the locked house up the hill.",
        "Player: Thank you."
    };
    [SerializeField] private string[] mazeExitDialogue = { "Player: I finally made it out." };
    [SerializeField] private string[] bakerReadyDialogue = { "Baker: Everything is ready now." };
    [SerializeField] private string[] bakerRewardDialogue = { "Baker: Thank you. Take this yellow key." };
    [SerializeField] private string[] bakerIntroDialogue =
    {
        "Player: Do you know where the four keys are?",
        "Baker: I have the yellow key, but I need honey first.",
        "Player: I can help with that.",
        "Baker: Please bring me a jar of honey."
    };
    [SerializeField] private string[] bakerHintDialogue = { "Baker: The bear near the tree has honey." };
    [SerializeField] private string[] bearEmptyDialogue = { "Bear: That was my only honey jar." };
    [SerializeField] private string[] bearLeafFoundDialogue =
    {
        "Player: I found the silver leaf.",
        "Bear: Pour the honey into the jar over there."
    };
    [SerializeField] private string[] bearIntroDialogue =
    {
        "Player: I need some honey.",
        "Bear: Bring me a silver leaf first.",
        "Player: I will find one."
    };
    [SerializeField] private string[] bearWaitingDialogue = { "Bear: Did you find the silver leaf?" };
    [SerializeField] private string[] bearPourDialogue = { "Bear: Pour the honey into the jar over there." };
    [SerializeField] private string[] guardDoneDialogue = { "Guard: You already proved yourself. The second page is yours." };
    [SerializeField] private string[] guardMazeIntroDialogue = { "Guard: Pass the maze and answer my questions to earn the second page." };
    [SerializeField] private string[] guardFirstDialogue =
    {
        "Player: I am looking for the second page.",
        "Guard: No pass, no entry.",
        "Player: Then I need to find a pass first."
    };
    [SerializeField] private string[] guardNoPassDialogue = { "Guard: No pass, no entry." };
    [SerializeField] private string[] welcomeDialogue = { "Player: This maze is huge." };
    [SerializeField] private string[] quizIntroDialogue =
    {
        "Guard: You passed the maze. Now answer my questions.",
        "Guard: You need eight correct answers. Two wrong answers sends you back.",
        "Guard: Listen carefully."
    };

    [Header("Prompts")]
    [SerializeField] private string continuePrompt = "Press C to continue";
    [SerializeField] private string startPrompt = "Press E to start";
    [SerializeField] private string takeHoneyPrompt = "Press E to take honey jar";
    [SerializeField] private string pickLeafPrompt = "Press E to pick leaf";
    [SerializeField] private string pourHoneyPrompt = "Press E to pour honey";
    [SerializeField] private string unlockPrompt = "Press E to unlock";
    [SerializeField] private string pickupPrompt = "Press E to pick up";
    [SerializeField] private string travelPrompt = "Press E to travel";
    [SerializeField] private string talkPrompt = "Press E to talk";
    [SerializeField] private string interactPrompt = "Press E to interact";
    [SerializeField] private string backpackTitle = "Backpack B";
    [SerializeField] private string backpackEmptyText = "Empty";
    [SerializeField] private string findHoneyPrompt = "Find a honey jar.";
    [SerializeField] private string useHoneyStationPrompt = "Use the honey jar station.";
    [SerializeField] private string honeyFoundPrompt = "Honey jar found.";
    [SerializeField] private string silverLeafFoundPrompt = "Silver leaf found.";
    [SerializeField] private string fullHoneyFoundPrompt = "Full honey jar found.";
    [SerializeField] private string unlockingPrompt = "Unlocking...";
    [SerializeField] private string doorUnlockedPrompt = "Door unlocked.";
    [SerializeField] private string thirdPageFoundPrompt = "Third page found.";
    [SerializeField] private string boardWonPrompt = "Game won. Maze pass received.";
    [SerializeField] private string boardRollPromptFormat = "Round {0}: press E to roll the dice.";
    [SerializeField] private string boardRollShortFormat = "Round {0}: press E to roll";
    [SerializeField] private string rollingDicePrompt = "Rolling dice...";
    [SerializeField] private string rolledPromptFormat = "Rolled {0}.";
    [SerializeField] private string boardMovePromptFormat = "Move {0} to tile {1}.";
    [SerializeField] private string boardMovingPromptFormat = "Rolled {0}, moving...";
    [SerializeField] private string movingPrompt = "Moving...";
    [SerializeField] private string welcomePrompt = "Welcome to the forest maze.";
    [SerializeField] private string quizPassedPrompt = "Quiz passed. Second page received.";
    [SerializeField] private string quizFailedPrompt = "Two wrong answers. Start the maze again.";
    [SerializeField] private string quizFailedFeedback = "Two wrong answers. Go through the maze again.";

    private enum FlowState
    {
        Exploring,
        Dialogue,
        Quiz,
        BoardGame
    }

    private enum BoardGamePhase
    {
        NotStarted,
        WaitingToRoll,
        Rolling,
        Moving,
        Won
    }

    private class Question
    {
        public readonly string virtue;
        public readonly string text;
        public readonly string[] options;
        public readonly int correctIndex;
        public readonly string reason;

        // Function: Stores one quiz question, its options, correct answer index, and explanation.
        public Question(string virtue, string text, string[] options, int correctIndex, string reason)
        {
            this.virtue = virtue;
            this.text = text;
            this.options = options;
            this.correctIndex = correctIndex;
            this.reason = reason;
        }
    }

    private static ChapterTwoPuzzle instance;

    [Header("Scene References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform guard;
    [SerializeField] private Transform guardInteract;
    [SerializeField] private Transform exitInteract;
    [SerializeField] private Transform startTile;
    [SerializeField] private Transform endTile;
    [SerializeField] private Transform dice;
    [SerializeField] private Transform bakerInteract;
    [SerializeField] private Transform bearInteract;
    [SerializeField] private Transform listenerInteract;
    [SerializeField] private Transform lockedHouse;
    [SerializeField] private Transform box;
    [SerializeField] private Transform honeyGive;
    [SerializeField] private Transform airWallTwo;
    [SerializeField] private Transform[] boardTiles = new Transform[BoardTileCount];
    [SerializeField] private Transform[] diceFaces = new Transform[DiceFaceCount];

    [Header("Item Objects")]
    [SerializeField] private GameObject honeyObject;
    [SerializeField] private GameObject silverLeafObject;
    [SerializeField] private GameObject rockBearObject;
    [SerializeField] private GameObject redKeyObject;
    [SerializeField] private GameObject blueKeyObject;
    [SerializeField] private GameObject greenKeyObject;
    [SerializeField] private GameObject yellowKeyObject;
    [SerializeField] private GameObject finalDoorObject;
    [SerializeField] private GameObject finalWindowObject;
    [SerializeField] private GameObject fourthPagePaperObject;
    [SerializeField] private GameObject thirdPagePortalObject;
    [SerializeField] private GameObject mazeBlock;
    [SerializeField] private Transform redLockPart;
    [SerializeField] private Transform blueLockPart;
    [SerializeField] private Transform greenLockPart;
    [SerializeField] private Transform yellowLockPart;
    [SerializeField] private RuntimeAnimatorController guardStandController;
    [SerializeField] private Avatar guardAvatar;

    private readonly Vector3[] diceFaceLocalNormals = new Vector3[DiceFaceCount];
    private Vector3 guardOriginalPosition;
    private Vector3 mazeBlockOriginalPosition;
    private bool guardOriginalPositionReady;
    private bool mazeBlockOriginalPositionReady;
    private bool welcomeStarted;
    private bool firstGuardDialogueShown;
    private bool hasPass;
    private bool mazeOpened;
    private bool exitedMaze;
    private bool quizStarted;
    private bool quizCompleted;
    private bool mazeCollidersReady;
    private bool airWallTwoDropped;
    private Coroutine airWallTwoRoutine;
    private BoardGamePhase boardGamePhase;
    private int boardRound;
    private int boardPosition;
    private int lastDiceRoll;
    private bool boardReferencesReady;
    private Vector3 diceOriginalPosition;
    private Quaternion diceOriginalRotation;
    private bool diceOriginalTransformReady;
    private CharacterController boardMoveController;
    private bool boardMoveControllerWasEnabled;
    private Coroutine boardRoutine;
    private bool bakerIntroDone;
    private bool waitingForHoneyBottle;
    private bool hasHoneyBottle;
    private bool bearIntroDone;
    private bool bearAskedForSilverLeaf;
    private bool hasSilverLeaf;
    private bool bearRewardReady;
    private bool honeyPourReady;
    private bool hasFullHoneyBottle;
    private bool bakerQuestCompleted;
    private bool listenerDialogueShown;
    private bool lockedHouseDialogueShown;
    private bool lockedHouseOpened;
    private bool unlockingHouse;
    private bool boxDialogueShown;
    private bool waitingForFourthPagePickup;
    private bool fourthPagePicked;
    private bool thirdPagePortalUnlocked;
    private bool inventoryOpen;
    private FlowState state;
    private string[] activeLines;
    private int lineIndex;
    private System.Action dialogueFinished;
    private string currentSystemPrompt;
    private float systemPromptEndsAt;
    private int currentQuestionIndex;
    private int correctAnswerCount;
    private int wrongAnswerCount;
    private string quizFeedback;
    private bool showingQuizFeedback;
    private bool quizPassedAfterFeedback;
    private bool quizFailedAfterFeedback;
    private Vector2 lookPadDirection;
    private bool draggingLookPad;
    private Texture2D lookPadTexture;
    private Texture2D lookPadKnobTexture;
    private GUIStyle promptStyle;
    private GUIStyle dialogueStyle;
    private GUIStyle hintStyle;
    private GUIStyle titleStyle;
    private GUIStyle optionStyle;
    private GUIStyle inventoryStyle;

    private readonly List<Question> questions = new List<Question>();
    private readonly List<Question> quizQuestions = new List<Question>();
    private readonly List<string> inventoryItems = new List<string>();

    // Function: Adds an item to the Chapter Two inventory from another script.
    public static void AddItemToInventory(string itemName)
    {
        if (instance != null)
        {
            instance.AddInventoryItem(itemName);
        }
    }


    // Function: Initializes component references, cached state, and default runtime data.
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildQuestions();
        FixReferenceArraySizes();
        RefreshReferences();
    }

    // Function: Stops running routines, unregisters events, and restores temporary state when disabled.
    private void OnDisable()
    {
        AquariusMax.Fae.demo.DemoCharacter.LockPlayerInput = false;
        AquariusMax.Fae.demo.DemoCharacter.LockMovementInput = false;
        AquariusMax.Fae.demo.DemoCharacter.ForceWalkAnimation = false;
        AquariusMax.Fae.demo.DemoCharacter.UseLookPadInput = false;
        AquariusMax.Fae.demo.DemoCharacter.LookPadInput = Vector2.zero;
    }

    // Function: Updates input handling, interaction checks, and active gameplay flow each frame.
    private void Update()
    {
        if (!IsTargetScene())
        {
            AquariusMax.Fae.demo.DemoCharacter.LockPlayerInput = false;
            AquariusMax.Fae.demo.DemoCharacter.LockMovementInput = false;
            AquariusMax.Fae.demo.DemoCharacter.ForceWalkAnimation = false;
            AquariusMax.Fae.demo.DemoCharacter.UseLookPadInput = false;
            AquariusMax.Fae.demo.DemoCharacter.LookPadInput = Vector2.zero;
            return;
        }

        EnsureGuardStandAnimation();
        EnsureMazeColliders();
        StartWelcomeIfNeeded();
        if (Input.GetKeyDown(KeyCode.B))
        {
            inventoryOpen = !inventoryOpen;
        }

        AquariusMax.Fae.demo.DemoCharacter.LockPlayerInput = state == FlowState.Quiz;
        AquariusMax.Fae.demo.DemoCharacter.LockMovementInput = state == FlowState.BoardGame;
        AquariusMax.Fae.demo.DemoCharacter.UseLookPadInput = false;
        AquariusMax.Fae.demo.DemoCharacter.LookPadInput = Vector2.zero;
        Cursor.visible = state == FlowState.Quiz;
        Cursor.lockState = state == FlowState.Quiz ? CursorLockMode.None : CursorLockMode.Locked;

        if (state == FlowState.Dialogue)
        {
            if (Input.GetKeyDown(continueKey))
            {
                AdvanceDialogue();
            }

            return;
        }

        if (state == FlowState.Quiz)
        {
            UpdateQuizInput();
            return;
        }

        if (state == FlowState.BoardGame)
        {
            UpdateBoardGameInput();
            return;
        }

        UpdateExplorationInput();
    }

    // Function: Draws this script's IMGUI prompts, panels, and dialogue.
    private void OnGUI()
    {
        if (!IsTargetScene())
        {
            return;
        }

        DrawSystemPrompt();
        if (state == FlowState.Dialogue)
        {
            DrawDialogue();
            return;
        }

        if (state == FlowState.Quiz)
        {
            DrawQuiz();
            return;
        }

        if (state == FlowState.BoardGame)
        {
            DrawBoardGame();
            return;
        }

        DrawInteractPrompts();
    }

    // Function: Updates exploration input state, input, or presentation.
    private void UpdateExplorationInput()
    {
        if (!hasPass && IsNearStartTile() && Input.GetKeyDown(interactKey))
        {
            StartBoardGame();
            return;
        }

        if (waitingForHoneyBottle && !hasHoneyBottle && IsNearHoney() && Input.GetKeyDown(interactKey))
        {
            PickHoneyBottle();
            return;
        }

        if (bearAskedForSilverLeaf && !hasSilverLeaf && IsNearSilverLeaf() && Input.GetKeyDown(interactKey))
        {
            PickSilverLeaf();
            return;
        }

        if (honeyPourReady && !hasFullHoneyBottle && IsNearHoneyGive() && Input.GetKeyDown(interactKey))
        {
            PourHoney();
            return;
        }

        if (!lockedHouseOpened && HasAllFourKeys() && IsNearLockedHouse() && Input.GetKeyDown(interactKey))
        {
            StartCoroutine(OpenLockedHouse());
            return;
        }

        if (lockedHouseOpened && waitingForFourthPagePickup && !fourthPagePicked && IsNearBox() && Input.GetKeyDown(interactKey))
        {
            PickFourthPage();
            return;
        }

        if (thirdPagePortalUnlocked && IsNearThirdPagePortal() && Input.GetKeyDown(interactKey))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        if (lockedHouseOpened && !boxDialogueShown && !fourthPagePicked && IsNearBox())
        {
            boxDialogueShown = true;
            StartDialogue(boxFoundDialogue, () => waitingForFourthPagePickup = true);
            return;
        }

        if (!lockedHouseDialogueShown && !HasAllFourKeys() && IsNearLockedHouse())
        {
            lockedHouseDialogueShown = true;
            StartDialogue(lockedHouseDialogue, null);
            return;
        }

        if (!listenerDialogueShown && IsNearListener() && Input.GetKeyDown(interactKey))
        {
            listenerDialogueShown = true;
            StartDialogue(listenerDialogue, null);
            return;
        }

        if (IsNearBaker() && Input.GetKeyDown(interactKey))
        {
            HandleBakerInteraction();
            return;
        }

        if (IsNearBear() && Input.GetKeyDown(interactKey))
        {
            HandleBearInteraction();
            return;
        }

        if (IsNearGuard() && Input.GetKeyDown(interactKey))
        {
            HandleGuardInteraction();
            return;
        }

        if (mazeOpened && !exitedMaze && IsNearExit())
        {
            exitedMaze = true;
            HideMazeBlock();
            StartDialogue(mazeExitDialogue, null);
        }
    }

    // Function: Handles baker interaction interaction or progression.
    private void HandleBakerInteraction()
    {
        if (bakerQuestCompleted)
        {
            StartDialogue(bakerReadyDialogue, null);
            return;
        }

        if (hasFullHoneyBottle && !bakerQuestCompleted)
        {
            bakerQuestCompleted = true;
            RemoveInventoryItem(fullHoneyJarItemName);
            AddInventoryItem(yellowKeyItemName);
            StartDialogue(bakerRewardDialogue, null);
            return;
        }

        if (!bakerIntroDone)
        {
            bakerIntroDone = true;
            StartDialogue(bakerIntroDialogue, () =>
            {
                waitingForHoneyBottle = true;
                HideRockBear();
                ShowSystemPrompt(findHoneyPrompt, 3f);
            });
            return;
        }

        if (!hasFullHoneyBottle)
        {
            StartDialogue(bakerHintDialogue, null);
            return;
        }

        StartDialogue(bakerRewardDialogue, null);
    }

    // Function: Handles bear interaction interaction or progression.
    private void HandleBearInteraction()
    {
        if (hasFullHoneyBottle || bakerQuestCompleted)
        {
            StartDialogue(bearEmptyDialogue, null);
            return;
        }

        if (hasSilverLeaf && !bearRewardReady)
        {
            RemoveInventoryItem(silverLeafItemName);
            bearRewardReady = true;
            honeyPourReady = true;
            StartDialogue(bearLeafFoundDialogue, () => ShowSystemPrompt(useHoneyStationPrompt, 3f));
            return;
        }

        if (!bearIntroDone)
        {
            bearIntroDone = true;
            bearAskedForSilverLeaf = true;
            ShowSilverLeaf();
            StartDialogue(bearIntroDialogue, null);
            return;
        }

        if (!hasSilverLeaf)
        {
            StartDialogue(bearWaitingDialogue, null);
            return;
        }

        StartDialogue(bearPourDialogue, null);
    }

    // Function: Picks up or selects honey bottle.
    private void PickHoneyBottle()
    {
        hasHoneyBottle = true;
        waitingForHoneyBottle = false;
        AddInventoryItem(honeyJarItemName);
        ShowSystemPrompt(honeyFoundPrompt, 3f);
    }

    // Function: Picks up or selects silver leaf.
    private void PickSilverLeaf()
    {
        hasSilverLeaf = true;
        AddInventoryItem(silverLeafItemName);
        if (silverLeafObject != null)
        {
            silverLeafObject.SetActive(false);
        }

        ShowSystemPrompt(silverLeafFoundPrompt, 3f);
    }

    // Function: Runs the pour honey logic.
    private void PourHoney()
    {
        hasFullHoneyBottle = true;
        honeyPourReady = false;
        RemoveInventoryItem(honeyJarItemName);
        AddInventoryItem(fullHoneyJarItemName);
        ShowSystemPrompt(fullHoneyFoundPrompt, 3f);
    }

    // Function: Runs the open locked house logic.
    private IEnumerator OpenLockedHouse()
    {
        if (unlockingHouse || lockedHouseOpened)
        {
            yield break;
        }

        unlockingHouse = true;
        ShowSystemPrompt(unlockingPrompt, 3f);

        Transform[] lockParts = { redLockPart, blueLockPart, greenLockPart, yellowLockPart };
        Vector3[] lockStarts = new Vector3[lockParts.Length];
        Vector3[] lockTargets = new Vector3[lockParts.Length];
        for (int i = 0; i < lockParts.Length; i++)
        {
            if (lockParts[i] == null)
            {
                continue;
            }

            lockStarts[i] = lockParts[i].localPosition;
            lockTargets[i] = new Vector3(lockStarts[i].x, lockPartTargetLocalY, lockStarts[i].z);
        }

        float elapsed = 0f;
        while (elapsed < finalUnlockMoveSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / finalUnlockMoveSeconds));
            for (int i = 0; i < lockParts.Length; i++)
            {
                if (lockParts[i] != null)
                {
                    lockParts[i].localPosition = Vector3.Lerp(lockStarts[i], lockTargets[i], t);
                }
            }

            yield return null;
        }

        for (int i = 0; i < lockParts.Length; i++)
        {
            if (lockParts[i] != null)
            {
                lockParts[i].localPosition = lockTargets[i];
            }
        }

        GameObject[] keyObjects = { redKeyObject, blueKeyObject, greenKeyObject, yellowKeyObject };
        Vector3[] keyStarts = new Vector3[keyObjects.Length];
        Vector3[] keyTargets = new Vector3[keyObjects.Length];
        for (int i = 0; i < keyObjects.Length; i++)
        {
            if (keyObjects[i] == null)
            {
                continue;
            }

            keyStarts[i] = keyObjects[i].transform.position;
            keyTargets[i] = keyStarts[i] + Vector3.down * keyDropDistance;
        }

        elapsed = 0f;
        while (elapsed < finalUnlockMoveSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / finalUnlockMoveSeconds));
            for (int i = 0; i < keyObjects.Length; i++)
            {
                if (keyObjects[i] != null)
                {
                    keyObjects[i].transform.position = Vector3.Lerp(keyStarts[i], keyTargets[i], t);
                }
            }

            yield return null;
        }

        for (int i = 0; i < keyObjects.Length; i++)
        {
            if (keyObjects[i] != null)
            {
                keyObjects[i].SetActive(false);
            }
        }

        if (finalDoorObject != null)
        {
            finalDoorObject.SetActive(false);
        }

        if (finalWindowObject != null)
        {
            finalWindowObject.SetActive(false);
        }

        RemoveInventoryItem(redKeyItemName);
        RemoveInventoryItem(blueKeyItemName);
        RemoveInventoryItem(greenKeyItemName);
        RemoveInventoryItem(yellowKeyItemName);

        lockedHouseOpened = true;
        unlockingHouse = false;
        ShowSystemPrompt(doorUnlockedPrompt, 3f);
    }

    // Function: Picks up or selects fourth page.
    private void PickFourthPage()
    {
        fourthPagePicked = true;
        waitingForFourthPagePickup = false;
        AddInventoryItem(thirdPageItemName);

        if (fourthPagePaperObject != null)
        {
            fourthPagePaperObject.SetActive(false);
        }

        UnlockThirdPagePortal();
        ShowSystemPrompt(thirdPageFoundPrompt, 3f);
    }

    // Function: Unlocks third page portal and restores normal interaction.
    private void UnlockThirdPagePortal()
    {
        thirdPagePortalUnlocked = true;

        if (thirdPagePortalObject != null)
        {
            thirdPagePortalObject.SetActive(true);
        }
    }

    // Function: Hides rock bear.
    private void HideRockBear()
    {
        if (rockBearObject != null)
        {
            rockBearObject.SetActive(false);
        }

        if (silverLeafObject != null)
        {
            silverLeafObject.SetActive(false);
        }
    }

    // Function: Shows silver leaf.
    private void ShowSilverLeaf()
    {
        if (silverLeafObject != null)
        {
            silverLeafObject.SetActive(true);
        }
    }

    // Function: Handles guard interaction interaction or progression.
    private void HandleGuardInteraction()
    {
        if (quizCompleted)
        {
            StartDialogue(guardDoneDialogue, null);
            return;
        }

        if (exitedMaze)
        {
            StartQuizIntro();
            return;
        }

        if (hasPass)
        {
            StartDialogue(guardMazeIntroDialogue, OpenMaze);
            return;
        }

        if (!firstGuardDialogueShown)
        {
            firstGuardDialogueShown = true;
            StartDialogue(guardFirstDialogue, null);
        }
        else
        {
            StartDialogue(guardNoPassDialogue, null);
        }
    }

    // Function: Starts the board game flow.
    private void StartBoardGame()
    {
        RefreshBoardReferences();
        boardRound = 1;
        boardPosition = 0;
        lastDiceRoll = 0;
        boardGamePhase = BoardGamePhase.WaitingToRoll;
        state = FlowState.BoardGame;
        ShowSystemPrompt(GetRollPrompt(), 3f);
    }

    // Function: Updates board game input state, input, or presentation.
    private void UpdateBoardGameInput()
    {
        if (boardGamePhase == BoardGamePhase.WaitingToRoll && Input.GetKeyDown(interactKey))
        {
            StopBoardRoutine();
            boardRoutine = StartCoroutine(RollDiceAndMove());
        }
    }

    // Function: Runs the roll dice and move logic.
    private IEnumerator RollDiceAndMove()
    {
        boardGamePhase = BoardGamePhase.Rolling;
        int targetFace = Random.Range(1, 7);
        lastDiceRoll = 0;
        ShowSystemPrompt(rollingDicePrompt, 3f);

        yield return ThrowDice(targetFace);
        lastDiceRoll = GetFacePointingUp();
        ShowSystemPrompt(string.Format(rolledPromptFormat, lastDiceRoll), 3f);
        yield return new WaitForSeconds(0.35f);

        int target = Mathf.Min(boardPosition + lastDiceRoll, 20);
        boardGamePhase = BoardGamePhase.Moving;
        BeginBoardMove();
        yield return MoveAlongBoard(boardPosition, target);
        boardPosition = target;

        int adjusted = GetBoardAdjustment(boardPosition);
        if (adjusted != boardPosition)
        {
            string action = adjusted > boardPosition ? "forward" : "back";
            ShowSystemPrompt(string.Format(boardMovePromptFormat, action, adjusted), 3f);
            yield return new WaitForSeconds(0.6f);
            yield return MoveAlongBoard(boardPosition, adjusted);
            boardPosition = adjusted;
        }

        if (boardPosition >= 20)
        {
            yield return MovePlayerToTransform(endTile);
            EndBoardMove();
            CompleteBoardGame();
            yield break;
        }

        EndBoardMove();
        boardRound++;
        boardGamePhase = BoardGamePhase.WaitingToRoll;
        ShowSystemPrompt(GetRollPrompt(), 3f);
        boardRoutine = null;
    }

    // Function: Runs the throw dice logic.
    private IEnumerator ThrowDice(int result)
    {
        if (dice == null)
        {
            yield return new WaitForSeconds(0.4f);
            yield break;
        }

        if (!diceOriginalTransformReady)
        {
            CacheDiceOriginalTransform();
        }

        Vector3 start = diceOriginalPosition;
        Quaternion finalRotation = GetDiceResultRotation(result);
        float elapsed = 0f;
        Vector3 drift = player != null ? player.forward * 0.8f : Vector3.forward * 0.8f;
        Vector3 spinAxis = new Vector3(0.73f, 1f, 0.41f).normalized;

        while (elapsed < diceThrowSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / diceThrowSeconds);
            float arc = Mathf.Sin(t * Mathf.PI) * diceThrowHeight;
            dice.position = Vector3.Lerp(start, start + drift, t) + Vector3.up * arc;

            Quaternion spin = Quaternion.AngleAxis(1440f * t, spinAxis) * Quaternion.AngleAxis(980f * t, Vector3.up);
            dice.rotation = t < 0.72f
                ? spin * diceOriginalRotation
                : Quaternion.Slerp(spin * diceOriginalRotation, finalRotation, Mathf.InverseLerp(0.72f, 1f, t));
            yield return null;
        }

        dice.position = start;
        dice.rotation = finalRotation;
    }

    // Function: Gets or calculates dice result rotation.
    private Quaternion GetDiceResultRotation(int result)
    {
        result = Mathf.Clamp(result, 1, 6);
        Vector3 localNormal = diceFaceLocalNormals[result].sqrMagnitude > 0.001f ? diceFaceLocalNormals[result] : Vector3.up;
        Quaternion faceUp = Quaternion.FromToRotation(localNormal, Vector3.up);
        return Quaternion.AngleAxis(Random.Range(0, 4) * 90f, Vector3.up) * faceUp;
    }

    // Function: Gets or calculates face pointing up.
    private int GetFacePointingUp()
    {
        if (dice == null)
        {
            return Mathf.Clamp(lastDiceRoll, 1, 6);
        }

        int bestFace = 1;
        float bestDot = float.NegativeInfinity;
        for (int i = 1; i < diceFaces.Length; i++)
        {
            Vector3 localNormal = diceFaceLocalNormals[i].sqrMagnitude > 0.001f ? diceFaceLocalNormals[i] : Vector3.up;
            float dot = Vector3.Dot(dice.TransformDirection(localNormal), Vector3.up);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestFace = i;
            }
        }

        return bestFace;
    }

    // Function: Moves along board toward its target position or state.
    private IEnumerator MoveAlongBoard(int from, int to)
    {
        int index = from;
        while (index < to)
        {
            index++;
            yield return MovePlayerToBoardIndex(index);
        }

        while (index > to)
        {
            index--;
            yield return MovePlayerToBoardIndex(index);
        }
    }

    // Function: Moves player to board index toward its target position or state.
    private IEnumerator MovePlayerToBoardIndex(int index)
    {
        if (index <= 0)
        {
            yield return MovePlayerToTransform(startTile);
            yield break;
        }

        index = Mathf.Clamp(index, 1, 20);
        yield return MovePlayerToTransform(boardTiles[index]);
    }

    // Function: Moves player to transform toward its target position or state.
    private IEnumerator MovePlayerToTransform(Transform targetTransform)
    {
        if (player == null || targetTransform == null)
        {
            yield break;
        }

        Vector3 start = player.position;
        Vector3 target = targetTransform.position + Vector3.up * playerGroundOffset;
        target.y = start.y;
        float distance = Vector3.Distance(start, target);
        float duration = Mathf.Max(0.15f, distance / Mathf.Max(0.1f, boardMoveSpeed));
        float elapsed = 0f;
        Animator animator = player.GetComponentInChildren<Animator>();
        AquariusMax.Fae.demo.DemoCharacter.ForceWalkAnimation = true;
        SetBoardWalkAnimation(animator, true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 direction = target - player.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion facing = Quaternion.LookRotation(direction.normalized, Vector3.up);
                player.rotation = Quaternion.RotateTowards(player.rotation, facing, 360f * Time.deltaTime);
            }

            player.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        player.position = target;
        AquariusMax.Fae.demo.DemoCharacter.ForceWalkAnimation = false;
        SetBoardWalkAnimation(animator, false);

    }

    // Function: Begins the board move phase.
    private void BeginBoardMove()
    {
        if (player == null || boardMoveController != null)
        {
            return;
        }

        boardMoveController = player.GetComponent<CharacterController>();
        boardMoveControllerWasEnabled = boardMoveController != null && boardMoveController.enabled;
        if (boardMoveController != null)
        {
            boardMoveController.enabled = false;
        }
    }

    // Function: Ends the board move phase and restores follow-up state.
    private void EndBoardMove()
    {
        if (boardMoveController != null)
        {
            boardMoveController.enabled = boardMoveControllerWasEnabled;
        }

        boardMoveController = null;
        boardMoveControllerWasEnabled = false;
        AquariusMax.Fae.demo.DemoCharacter.ForceWalkAnimation = false;
    }

    // Function: Sets board walk animation.
    private void SetBoardWalkAnimation(Animator animator, bool walking)
    {
        if (animator == null)
        {
            return;
        }

        SetAnimatorBool(animator, "IsMoving", walking);
        SetAnimatorBool(animator, "IsRunning", false);
        SetAnimatorFloat(animator, "Speed", walking ? 0.5f : 0f);

        string stateName = walking ? "Walk" : "Idle";
        int fullPathHash = Animator.StringToHash("Base Layer." + stateName);
        int shortNameHash = Animator.StringToHash(stateName);
        if (animator.HasState(0, fullPathHash))
        {
            animator.CrossFade(fullPathHash, 0.08f, 0);
        }
        else if (animator.HasState(0, shortNameHash))
        {
            animator.CrossFade(shortNameHash, 0.08f, 0);
        }
    }

    // Function: Sets animator bool.
    private void SetAnimatorBool(Animator animator, string parameterName, bool value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }

    // Function: Sets animator float.
    private void SetAnimatorFloat(Animator animator, string parameterName, float value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(parameterName, value);
                return;
            }
        }
    }

    // Function: Gets or calculates board adjustment.
    private int GetBoardAdjustment(int position)
    {
        switch (position)
        {
            case 3:
                return 1;
            case 9:
                return 8;
            case 12:
                return 13;
            case 15:
                return 13;
            default:
                return position;
        }
    }

    // Function: Completes board game and applies its result or reward.
    private void CompleteBoardGame()
    {
        hasPass = true;
        boardGamePhase = BoardGamePhase.Won;
        state = FlowState.Exploring;
        AddInventoryItem(mazePassItemName);
        ShowSystemPrompt(boardWonPrompt, 3f);
    }

    // Function: Runs the stop board routine logic.
    private void StopBoardRoutine()
    {
        if (boardRoutine != null)
        {
            StopCoroutine(boardRoutine);
            boardRoutine = null;
        }

        EndBoardMove();
    }

    // Function: Starts the welcome if needed flow.
    private void StartWelcomeIfNeeded()
    {
        if (welcomeStarted || player == null)
        {
            return;
        }

        welcomeStarted = true;
        ShowSystemPrompt(welcomePrompt, 3f);
        StartDialogue(welcomeDialogue, null);
    }

    // Function: Starts the quiz intro flow.
    private void StartQuizIntro()
    {
        if (quizStarted)
        {
            state = FlowState.Quiz;
            return;
        }

        quizStarted = true;
        StartDialogue(quizIntroDialogue, StartQuiz);
    }

    // Function: Starts the quiz flow.
    private void StartQuiz()
    {
        BuildQuizQuestions();
        currentQuestionIndex = 0;
        correctAnswerCount = 0;
        wrongAnswerCount = 0;
        quizFeedback = null;
        quizPassedAfterFeedback = false;
        quizFailedAfterFeedback = false;
        showingQuizFeedback = false;
        state = FlowState.Quiz;
    }

    // Function: Builds the data or scene objects needed for quiz questions.
    private void BuildQuizQuestions()
    {
        quizQuestions.Clear();
        AddQuestionsForVirtue("Courage", 2);
        AddQuestionsForVirtue("Kindness", 2);
        AddQuestionsForVirtue("Wisdom", 2);
        AddQuestionsForVirtue("Resolve", 2);
        AddQuestionsForVirtue("Patience", 2);
    }

    // Function: Adds questions for virtue.
    private void AddQuestionsForVirtue(string virtue, int count)
    {
        int added = 0;
        foreach (Question question in questions)
        {
            if (question.virtue != virtue)
            {
                continue;
            }

            quizQuestions.Add(question);
            added++;
            if (added >= count)
            {
                return;
            }
        }
    }

    // Function: Updates quiz input state, input, or presentation.
    private void UpdateQuizInput()
    {
        if (showingQuizFeedback)
        {
            if (Input.GetKeyDown(continueKey))
            {
                showingQuizFeedback = false;

                if (quizFailedAfterFeedback)
                {
                    ResetMazeAfterWrongAnswer();
                    return;
                }

                if (quizPassedAfterFeedback)
                {
                    CompleteSecondPageReward();
                    return;
                }

                currentQuestionIndex++;
                if (currentQuestionIndex >= quizQuestions.Count)
                {
                    if (correctAnswerCount >= 8)
                    {
                        CompleteSecondPageReward();
                    }
                    else
                    {
                        ResetMazeAfterWrongAnswer();
                    }
                }
            }

            return;
        }

        int selected = -1;
        if (Input.GetKeyDown(KeyCode.A)) selected = 0;
        if (Input.GetKeyDown(KeyCode.B)) selected = 1;
        if (Input.GetKeyDown(KeyCode.C)) selected = 2;
        if (Input.GetKeyDown(KeyCode.D)) selected = 3;

        if (selected < 0 || currentQuestionIndex < 0 || currentQuestionIndex >= quizQuestions.Count)
        {
            return;
        }

        quizPassedAfterFeedback = false;
        quizFailedAfterFeedback = false;
        Question question = quizQuestions[currentQuestionIndex];
        if (selected == question.correctIndex)
        {
            correctAnswerCount++;
            quizFeedback = "Correct.\n" + question.reason;
            if (correctAnswerCount >= 8)
            {
                quizPassedAfterFeedback = true;
                quizFeedback += "\n\nYou have eight correct answers. You may continue.";
            }
        }
        else
        {
            wrongAnswerCount++;
            string correct = question.options[question.correctIndex];
            quizFeedback = "Wrong.\nCorrect answer: " + correct + "\nReason: " + question.reason;
            if (wrongAnswerCount >= 2)
            {
                quizFailedAfterFeedback = true;
                quizFeedback += "\n\n" + quizFailedFeedback;
            }
        }

        showingQuizFeedback = true;
    }

    // Function: Resets maze after wrong answer to its starting state.
    private void ResetMazeAfterWrongAnswer()
    {
        state = FlowState.Exploring;
        quizStarted = false;
        exitedMaze = false;
        mazeOpened = true;
        currentQuestionIndex = 0;
        correctAnswerCount = 0;
        wrongAnswerCount = 0;
        showingQuizFeedback = false;
        quizPassedAfterFeedback = false;
        quizFailedAfterFeedback = false;
        quizFeedback = null;

        if (mazeBlock != null)
        {
            mazeBlock.SetActive(true);
            mazeBlock.transform.position = openedMazeBlockPosition;
            SetCollidersEnabled(mazeBlock, true);
        }

        if (guard != null && guardOriginalPositionReady)
        {
            guard.position = guardOriginalPosition + Vector3.right * guardMoveRightDistance;
        }

        MovePlayerToMazeStart();
        ShowSystemPrompt(quizFailedPrompt, 3f);
    }

    // Function: Completes second page reward and applies its result or reward.
    private void CompleteSecondPageReward()
    {
        quizCompleted = true;
        state = FlowState.Exploring;
        AddInventoryItem(secondPageItemName);
        ShowSystemPrompt(quizPassedPrompt, 3f);
        DropAirWallTwo();
    }

    // Function: Runs the drop air wall two logic.
    private void DropAirWallTwo()
    {
        if (airWallTwoDropped)
        {
            return;
        }

        if (airWallTwo == null)
        {
            return;
        }

        airWallTwoDropped = true;
        if (airWallTwoRoutine != null)
        {
            StopCoroutine(airWallTwoRoutine);
        }

        airWallTwoRoutine = StartCoroutine(MoveAirWallTwoDown());
    }

    // Function: Moves air wall two down toward its target position or state.
    private IEnumerator MoveAirWallTwoDown()
    {
        Vector3 start = airWallTwo.position;
        Vector3 target = start + Vector3.down * airWallTwoDropDistance;

        while (airWallTwo != null && Vector3.Distance(airWallTwo.position, target) > 0.01f)
        {
            airWallTwo.position = Vector3.MoveTowards(airWallTwo.position, target, airWallTwoDropSpeed * Time.deltaTime);
            yield return null;
        }

        if (airWallTwo != null)
        {
            airWallTwo.position = target;
        }

        airWallTwoRoutine = null;
    }

    // Function: Moves player to maze start toward its target position or state.
    private void MovePlayerToMazeStart()
    {
        if (player == null || guardInteract == null)
        {
            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controller != null)
        {
            controller.enabled = false;
        }

        Vector3 target = guardInteract.position;
        target.y = player.position.y;
        player.position = target;

        if (controller != null)
        {
            controller.enabled = controllerWasEnabled;
        }
    }

    // Function: Runs the open maze logic.
    private void OpenMaze()
    {
        if (!mazeOpened)
        {
            mazeOpened = true;
            if (guard != null)
            {
                guard.position += Vector3.right * guardMoveRightDistance;
            }

            RemoveInventoryItem(mazePassItemName);

            if (mazeBlock != null)
            {
                mazeBlock.SetActive(true);
                mazeBlock.transform.position = openedMazeBlockPosition;
                SetCollidersEnabled(mazeBlock, true);
            }
        }
    }

    // Function: Hides maze block.
    private void HideMazeBlock()
    {
        if (mazeBlock != null)
        {
            mazeBlock.SetActive(false);
        }
    }

    // Function: Starts the dialogue flow.
    private void StartDialogue(string[] lines, System.Action onFinished)
    {
        activeLines = lines;
        lineIndex = 0;
        dialogueFinished = onFinished;
        state = FlowState.Dialogue;
    }

    // Function: Runs the advance dialogue logic.
    private void AdvanceDialogue()
    {
        lineIndex++;
        if (activeLines != null && lineIndex < activeLines.Length)
        {
            return;
        }

        System.Action finished = dialogueFinished;
        activeLines = null;
        dialogueFinished = null;
        lineIndex = 0;
        state = FlowState.Exploring;

        if (finished != null)
        {
            finished.Invoke();
        }
    }

    // Function: Draws the UI elements for system prompt.
    private void DrawSystemPrompt()
    {
        if (string.IsNullOrEmpty(currentSystemPrompt) || Time.time >= systemPromptEndsAt)
        {
            return;
        }

        Rect rect = GameUiStyle.SystemPromptRect(760f, 92f);
        DrawPanel(rect);
        GUI.Label(rect, currentSystemPrompt, GetStyle(ref promptStyle, 30, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    // Function: Draws the UI elements for dialogue.
    private void DrawDialogue()
    {
        string line = activeLines != null && lineIndex >= 0 && lineIndex < activeLines.Length ? activeLines[lineIndex] : string.Empty;
        Rect rect = GameUiStyle.DialogueRect(220f);
        DrawPanel(rect);
        GUI.Label(new Rect(rect.x + 26f, rect.y + 24f, rect.width - 52f, 128f), line, GetStyle(ref dialogueStyle, 30, TextAnchor.UpperLeft, FontStyle.Normal, true));
        GUI.Label(new Rect(rect.x + 26f, rect.y + rect.height - 48f, rect.width - 52f, 28f), continuePrompt, GetStyle(ref hintStyle, 22, TextAnchor.MiddleRight, FontStyle.Normal));
    }

    // Function: Draws the UI elements for quiz.
    private void DrawQuiz()
    {
        Rect rect = new Rect(70f, 70f, Screen.width - 140f, Screen.height - 140f);
        GUI.Box(rect, GUIContent.none);

        if (showingQuizFeedback)
        {
            GUI.Label(new Rect(rect.x + 40f, rect.y + 48f, rect.width - 80f, rect.height - 120f), quizFeedback, GetStyle(ref dialogueStyle, 30, TextAnchor.UpperLeft, FontStyle.Normal, true));
            GUI.Label(new Rect(rect.x + 40f, rect.y + rect.height - 54f, rect.width - 80f, 30f), continuePrompt, GetStyle(ref hintStyle, 22, TextAnchor.MiddleRight, FontStyle.Normal));
            return;
        }

        Question q = quizQuestions[Mathf.Clamp(currentQuestionIndex, 0, quizQuestions.Count - 1)];
        GUI.Label(new Rect(rect.x + 36f, rect.y + 24f, rect.width - 72f, 38f), "Question " + (currentQuestionIndex + 1) + " / " + quizQuestions.Count + "   " + q.virtue + "   Correct " + correctAnswerCount + "/8   Wrong " + wrongAnswerCount + "/2", GetStyle(ref titleStyle, 30, TextAnchor.MiddleCenter, FontStyle.Bold));
        GUI.Label(new Rect(rect.x + 46f, rect.y + 82f, rect.width - 92f, 118f), q.text, GetStyle(ref dialogueStyle, 28, TextAnchor.UpperLeft, FontStyle.Normal, true));

        string[] labels = { "A: ", "B: ", "C: ", "D: " };
        for (int i = 0; i < q.options.Length; i++)
        {
            GUI.Label(new Rect(rect.x + 58f, rect.y + 220f + i * 52f, rect.width - 116f, 42f), labels[i] + q.options[i], GetStyle(ref optionStyle, 25, TextAnchor.MiddleLeft, FontStyle.Normal, true));
        }
    }

    // Function: Draws the UI elements for interact prompts.
    private void DrawInteractPrompts()
    {
        if (!hasPass && IsNearStartTile())
        {
            DrawPrompt(startPrompt);
            return;
        }

        if (waitingForHoneyBottle && !hasHoneyBottle && IsNearHoney())
        {
            DrawPrompt(takeHoneyPrompt);
            return;
        }

        if (bearAskedForSilverLeaf && !hasSilverLeaf && IsNearSilverLeaf())
        {
            DrawPrompt(pickLeafPrompt);
            return;
        }

        if (honeyPourReady && !hasFullHoneyBottle && IsNearHoneyGive())
        {
            DrawPrompt(pourHoneyPrompt);
            return;
        }

        if (!lockedHouseOpened && HasAllFourKeys() && IsNearLockedHouse())
        {
            DrawPrompt(unlockPrompt);
            return;
        }

        if (lockedHouseOpened && waitingForFourthPagePickup && !fourthPagePicked && IsNearBox())
        {
            DrawPrompt(pickupPrompt);
            return;
        }

        if (thirdPagePortalUnlocked && IsNearThirdPagePortal())
        {
            DrawPrompt(travelPrompt);
            return;
        }

        if (IsNearBaker() || (!listenerDialogueShown && IsNearListener()) || IsNearBear())
        {
            DrawPrompt(talkPrompt);
            return;
        }

        if (IsNearGuard())
        {
            DrawPrompt(exitedMaze ? interactPrompt : talkPrompt);
        }
    }

    // Function: Draws the UI elements for prompt.
    private void DrawPrompt(string text)
    {
        Rect rect = GameUiStyle.InteractionPromptRect(440f, 60f);
        DrawPanel(rect);
        GUI.Label(rect, text, GetStyle(ref promptStyle, 30, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    // Function: Draws the UI elements for board game.
    private void DrawBoardGame()
    {
        string text;
        if (boardGamePhase == BoardGamePhase.WaitingToRoll)
        {
            text = string.Format(boardRollShortFormat, boardRound);
        }
        else if (boardGamePhase == BoardGamePhase.Rolling)
        {
            text = rollingDicePrompt;
        }
        else if (boardGamePhase == BoardGamePhase.Moving)
        {
            text = lastDiceRoll > 0 ? string.Format(boardMovingPromptFormat, lastDiceRoll) : movingPrompt;
        }
        else
        {
            text = boardWonPrompt;
        }

        Rect rect = GameUiStyle.SystemPromptRect(620f, 86f);
        DrawPanel(rect);
        GUI.Label(rect, text, GetStyle(ref promptStyle, 30, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    // Function: Draws the UI elements for inventory.
    private void DrawInventory()
    {
        float width = inventoryOpen ? 230f : 118f;
        float height = inventoryOpen ? 150f : 48f;
        Rect rect = GameUiStyle.BackpackRect(width, height);
        DrawPanel(rect);

        GUI.Label(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 34f), backpackTitle, GetStyle(ref inventoryStyle, 20, TextAnchor.MiddleCenter, FontStyle.Bold));

        if (!inventoryOpen)
        {
            return;
        }

        if (inventoryItems.Count == 0)
        {
            GUI.Label(new Rect(rect.x + 12f, rect.y + 52f, rect.width - 24f, 28f), backpackEmptyText, GetStyle(ref hintStyle, 18, TextAnchor.MiddleLeft, FontStyle.Normal));
            return;
        }

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            GUI.Label(new Rect(rect.x + 16f, rect.y + 52f + i * 28f, rect.width - 32f, 26f), inventoryItems[i], GetStyle(ref hintStyle, 18, TextAnchor.MiddleLeft, FontStyle.Normal));
        }
    }

    // Function: Draws a reusable dark UI panel background.
    private void DrawPanel(Rect rect)
    {
        GameUiStyle.DrawPanel(rect);
    }

    // Function: Draws the UI elements for look pad.
    private void DrawLookPad()
    {
        if (state == FlowState.Quiz)
        {
            return;
        }

        Vector2 center = GetLookPadCenter();
        EnsureLookPadTextures();
        Color previous = GUI.color;
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(center.x - lookPadRadius, center.y - lookPadRadius, lookPadRadius * 2f, lookPadRadius * 2f), lookPadTexture);

        Vector2 knob = center + lookPadDirection * (lookPadRadius - lookPadKnobRadius - 8f);
        GUI.DrawTexture(new Rect(knob.x - lookPadKnobRadius, knob.y - lookPadKnobRadius, lookPadKnobRadius * 2f, lookPadKnobRadius * 2f), lookPadKnobTexture);
        GUI.color = previous;
    }

    // Function: Updates look pad input state, input, or presentation.
    private void UpdateLookPadInput()
    {
        if (state == FlowState.Quiz)
        {
            draggingLookPad = false;
            lookPadDirection = Vector2.zero;
            AquariusMax.Fae.demo.DemoCharacter.LookPadInput = Vector2.zero;
            return;
        }

        Vector2 center = GetLookPadCenter();
        Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

        if (Input.GetMouseButtonDown(0) && Vector2.Distance(mouse, center) <= lookPadRadius)
        {
            draggingLookPad = true;
        }

        if (!Input.GetMouseButton(0))
        {
            draggingLookPad = false;
        }

        if (draggingLookPad)
        {
            Vector2 offset = mouse - center;
            lookPadDirection = Vector2.ClampMagnitude(offset / lookPadRadius, 1f);
        }
        else
        {
            lookPadDirection = Vector2.zero;
        }

        AquariusMax.Fae.demo.DemoCharacter.LookPadInput = new Vector2(lookPadDirection.x, -lookPadDirection.y) * lookPadSensitivity;
    }

    // Function: Gets or calculates look pad center.
    private Vector2 GetLookPadCenter()
    {
        return new Vector2(Screen.width - lookPadRadius - 34f, Screen.height - lookPadRadius - 34f);
    }

    // Function: Ensures look pad textures exists, is configured, or is ready to use.
    private void EnsureLookPadTextures()
    {
        if (lookPadTexture == null)
        {
            lookPadTexture = CreateCircleTexture(128, new Color(1f, 1f, 1f, 0.13f), new Color(1f, 1f, 1f, 0.28f), 0.08f);
        }

        if (lookPadKnobTexture == null)
        {
            lookPadKnobTexture = CreateCircleTexture(64, new Color(1f, 1f, 1f, 0.36f), new Color(1f, 1f, 1f, 0.58f), 0.18f);
        }
    }

    // Function: Creates the objects, textures, or UI needed for circle texture.
    private Texture2D CreateCircleTexture(int size, Color fill, Color edge, float edgeWidth)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float radius = center;
        float innerRadius = radius * (1f - edgeWidth);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (distance > radius)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float t = Mathf.InverseLerp(innerRadius, radius, distance);
                    texture.SetPixel(x, y, Color.Lerp(fill, edge, t));
                }
            }
        }

        texture.Apply();
        return texture;
    }

    // Function: Gets or calculates style.
    private GUIStyle GetStyle(ref GUIStyle style, int fontSize, TextAnchor alignment, FontStyle fontStyle, bool wordWrap = false)
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
        }

        style.fontSize = fontSize;
        style.alignment = alignment;
        style.fontStyle = fontStyle;
        style.wordWrap = wordWrap;
        style.normal.textColor = Color.white;
        return style;
    }

    // Function: Shows system prompt.
    private void ShowSystemPrompt(string text, float seconds)
    {
        currentSystemPrompt = text;
        systemPromptEndsAt = Time.time + seconds;
    }

    // Function: Gets or calculates roll prompt.
    private string GetRollPrompt()
    {
        return string.Format(boardRollPromptFormat, boardRound);
    }

    // Function: Refreshes cached references or state for references.
    private void RefreshReferences()
    {
        if (!IsTargetScene())
        {
            return;
        }

        FixReferenceArraySizes();

        if (!thirdPagePortalUnlocked && thirdPagePortalObject != null)
        {
            thirdPagePortalObject.SetActive(false);
        }

        ApplyHoneyQuestObjectVisibility();
        RefreshBoardReferences();

        if (guard != null && !guardOriginalPositionReady)
        {
            guardOriginalPosition = guard.position;
            guardOriginalPositionReady = true;
        }

        if (mazeBlock != null && !mazeBlockOriginalPositionReady)
        {
            mazeBlockOriginalPosition = mazeBlock.transform.position;
            mazeBlockOriginalPositionReady = true;
        }
    }

    // Function: Refreshes cached references or state for board references.
    private void RefreshBoardReferences()
    {
        FixReferenceArraySizes();

        bool allReady = startTile != null && endTile != null && dice != null;
        for (int i = 1; i < boardTiles.Length; i++)
        {
            allReady &= boardTiles[i] != null;
        }

        for (int i = 1; i < diceFaces.Length; i++)
        {
            allReady &= diceFaces[i] != null;
        }

        if (dice != null && !diceOriginalTransformReady)
        {
            CacheDiceOriginalTransform();
        }

        boardReferencesReady = allReady;
    }

    // Function: Runs the fix reference array sizes logic.
    private void FixReferenceArraySizes()
    {
        if (boardTiles == null || boardTiles.Length != BoardTileCount)
        {
            System.Array.Resize(ref boardTiles, BoardTileCount);
        }

        if (diceFaces == null || diceFaces.Length != DiceFaceCount)
        {
            System.Array.Resize(ref diceFaces, DiceFaceCount);
        }
    }

    // Function: Caches the initial state or references for dice original transform.
    private void CacheDiceOriginalTransform()
    {
        if (dice == null)
        {
            return;
        }

        diceOriginalPosition = dice.position;
        diceOriginalRotation = dice.rotation;
        diceOriginalTransformReady = true;

        for (int i = 1; i < diceFaces.Length; i++)
        {
            if (diceFaces[i] != null)
            {
                Vector3 fromCenter = diceFaces[i].position - dice.position;
                diceFaceLocalNormals[i] = dice.InverseTransformDirection(fromCenter).normalized;
            }
        }
    }

    // Function: Applies honey quest object visibility effects to the scene or target object.
    private void ApplyHoneyQuestObjectVisibility()
    {
        if (silverLeafObject != null)
        {
            silverLeafObject.SetActive(bearAskedForSilverLeaf && !hasSilverLeaf);
        }

        if (honeyGive != null)
        {
            honeyGive.gameObject.SetActive(true);
        }

        if (rockBearObject != null && bakerIntroDone)
        {
            rockBearObject.SetActive(false);
        }
    }

    // Function: Ensures maze colliders exists, is configured, or is ready to use.
    private void EnsureMazeColliders()
    {
        if (mazeCollidersReady)
        {
            return;
        }

        if (mazeBlock != null)
        {
            mazeBlock.SetActive(true);
            SetCollidersEnabled(mazeBlock, true);
            EnsureBoxCollider(mazeBlock);
        }

        Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.gameObject.scene.IsValid() || renderer.gameObject.scene != SceneManager.GetActiveScene())
            {
                continue;
            }

            if (!IsInsideMazeColliderArea(renderer.bounds.center) || ShouldSkipRuntimeCollider(renderer.transform))
            {
                continue;
            }

            EnsureSolidCollider(renderer);
        }

        mazeCollidersReady = true;
    }

    // Function: Checks whether inside maze collider area is true.
    private bool IsInsideMazeColliderArea(Vector3 point)
    {
        Vector3 delta = point - mazeColliderCenter;
        return Mathf.Abs(delta.x) <= mazeColliderExtents.x &&
               Mathf.Abs(delta.y) <= mazeColliderExtents.y &&
               Mathf.Abs(delta.z) <= mazeColliderExtents.z;
    }

    // Function: Runs the should skip runtime collider logic.
    private bool ShouldSkipRuntimeCollider(Transform transform)
    {
        if (transform == null)
        {
            return true;
        }

        Transform root = transform.root;
        if ((player != null && root == player.root) ||
            (guard != null && root == guard.root))
        {
            return true;
        }

        string objectName = transform.name;
        return objectName.Contains("interact") ||
               objectName.Contains("Camera") ||
               objectName.Contains("Light");
    }

    // Function: Ensures solid collider exists, is configured, or is ready to use.
    private void EnsureSolidCollider(Renderer renderer)
    {
        Collider existing = renderer.GetComponent<Collider>();
        if (existing != null)
        {
            existing.isTrigger = false;
            existing.enabled = true;
            return;
        }

        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            MeshCollider meshCollider = renderer.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
        }
    }

    // Function: Ensures box collider exists, is configured, or is ready to use.
    private void EnsureBoxCollider(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        BoxCollider boxCollider = target.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = target.AddComponent<BoxCollider>();
        }

        boxCollider.isTrigger = false;
        boxCollider.enabled = true;
    }

    // Function: Sets colliders enabled.
    private void SetCollidersEnabled(GameObject target, bool enabled)
    {
        if (target == null)
        {
            return;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            collider.isTrigger = false;
            collider.enabled = enabled;
        }
    }

    // Function: Ensures guard stand animation exists, is configured, or is ready to use.
    private void EnsureGuardStandAnimation()
    {
        if (guard == null || guardStandController == null)
        {
            return;
        }

        Animator animator = guard.GetComponent<Animator>();
        if (animator == null)
        {
            animator = guard.gameObject.AddComponent<Animator>();
        }

        if (guardAvatar != null)
        {
            animator.avatar = guardAvatar;
        }

        animator.runtimeAnimatorController = guardStandController;
        animator.applyRootMotion = false;
        animator.enabled = true;
    }

    // Function: Checks whether nearby guard is true.
    private bool IsNearGuard()
    {
        return player != null && guardInteract != null && Vector3.Distance(player.position, guardInteract.position) <= interactDistance;
    }

    // Function: Checks whether nearby baker is true.
    private bool IsNearBaker()
    {
        return player != null && bakerInteract != null && Vector3.Distance(player.position, bakerInteract.position) <= interactDistance;
    }

    // Function: Checks whether nearby listener is true.
    private bool IsNearListener()
    {
        return player != null && listenerInteract != null && Vector3.Distance(player.position, listenerInteract.position) <= interactDistance;
    }

    // Function: Checks whether nearby locked house is true.
    private bool IsNearLockedHouse()
    {
        return player != null && lockedHouse != null && Vector3.Distance(player.position, lockedHouse.position) <= interactDistance;
    }

    // Function: Checks whether nearby box is true.
    private bool IsNearBox()
    {
        return player != null && box != null && Vector3.Distance(player.position, box.position) <= interactDistance;
    }

    // Function: Checks whether nearby honey is true.
    private bool IsNearHoney()
    {
        return player != null && honeyObject != null && honeyObject.activeInHierarchy && Vector3.Distance(player.position, honeyObject.transform.position) <= interactDistance;
    }

    // Function: Checks whether nearby bear is true.
    private bool IsNearBear()
    {
        return player != null && bearInteract != null && Vector3.Distance(player.position, bearInteract.position) <= interactDistance;
    }

    // Function: Checks whether nearby honey give is true.
    private bool IsNearHoneyGive()
    {
        return player != null && honeyGive != null && Vector3.Distance(player.position, honeyGive.position) <= interactDistance;
    }

    // Function: Checks whether nearby silver leaf is true.
    private bool IsNearSilverLeaf()
    {
        return player != null && silverLeafObject != null && silverLeafObject.activeInHierarchy && Vector3.Distance(player.position, silverLeafObject.transform.position) <= interactDistance;
    }

    // Function: Checks whether nearby third page portal is true.
    private bool IsNearThirdPagePortal()
    {
        return player != null &&
               thirdPagePortalObject != null &&
               thirdPagePortalObject.activeInHierarchy &&
               Vector3.Distance(player.position, thirdPagePortalObject.transform.position) <= interactDistance;
    }

    // Function: Adds inventory item.
    private void AddInventoryItem(string itemName)
    {
        if (!inventoryItems.Contains(itemName))
        {
            inventoryItems.Add(itemName);
            GlobalBackpackUI.AddItem(itemName);
        }
    }

    // Function: Removes inventory item.
    private void RemoveInventoryItem(string itemName)
    {
        if (inventoryItems.Remove(itemName))
        {
            GlobalBackpackUI.RemoveItem(itemName);
        }
    }

    // Function: Checks whether all four keys already exists or is available.
    private bool HasAllFourKeys()
    {
        return inventoryItems.Contains(redKeyItemName) &&
               inventoryItems.Contains(blueKeyItemName) &&
               inventoryItems.Contains(greenKeyItemName) &&
               inventoryItems.Contains(yellowKeyItemName);
    }

    // Function: Checks whether nearby start tile is true.
    private bool IsNearStartTile()
    {
        if (hasPass || player == null)
        {
            return false;
        }

        return startTile != null && Vector3.Distance(player.position, startTile.position) <= boardInteractDistance;
    }

    // Function: Checks whether nearby exit is true.
    private bool IsNearExit()
    {
        return player != null && exitInteract != null && Vector3.Distance(player.position, exitInteract.position) <= exitDistance;
    }

    // Function: Checks whether target scene is true.
    private bool IsTargetScene()
    {
        return SceneManager.GetActiveScene().name == targetSceneName;
    }

    // Function: Builds the data or scene objects needed for questions.
    private void BuildQuestions()
    {
        questions.Clear();
        questions.Add(new Question("Courage", "A dark path is shorter, but a bright path is safer. Which do you choose?", new[] { "Dark path", "Bright path", "Wait here" }, 0, "Courage means facing the unknown when the goal matters."));
        questions.Add(new Question("Courage", "You hear someone crying in a dangerous part of the forest. What do you do?", new[] { "Go help", "Ignore it", "Hide" }, 0, "Courage is helping even when it is difficult."));
        questions.Add(new Question("Courage", "A deep cave may hold the clue you need. What is the best choice?", new[] { "Jump in", "Prepare first, then enter", "Leave forever" }, 1, "Courage is not recklessness. Preparation matters."));
        questions.Add(new Question("Kindness", "A hungry creature looks at your magic fruit. What do you do?", new[] { "Share it", "Keep it", "Hide it" }, 0, "Kindness values life over pride."));
        questions.Add(new Question("Kindness", "A small fairy stole from you. What should you do first?", new[] { "Punish it", "Ask why", "Run away" }, 1, "Kindness tries to understand before judging."));
        questions.Add(new Question("Kindness", "You find a key that may belong to someone else. What do you do?", new[] { "Use it", "Look for the owner", "Throw it away" }, 1, "A useful item can still belong to another person."));
        questions.Add(new Question("Wisdom", "Three doors stand before you. What helps most?", new[] { "Guess", "Ask a careful question", "Walk away" }, 1, "Wisdom uses logic instead of panic."));
        questions.Add(new Question("Wisdom", "You have few tools to cross a river. What do you do?", new[] { "Give up", "Use the tools carefully", "Break them" }, 1, "Wisdom makes the best use of limited resources."));
        questions.Add(new Question("Wisdom", "A magic lamp answers only a few questions. What is best?", new[] { "Ask directly", "Use elimination", "Ask silly questions" }, 1, "Good questions save time and effort."));
        questions.Add(new Question("Resolve", "A weak bridge leads to treasure. What do you do?", new[] { "Rush across", "Test it first", "Give up" }, 1, "Resolve means acting after judging the risk."));
        questions.Add(new Question("Resolve", "Another apprentice also wants the page. What is fair?", new[] { "Share or take turns", "Steal it", "Quit" }, 0, "Resolve can be firm without being cruel."));
        questions.Add(new Question("Resolve", "A spell needs a small sacrifice. What is wise?", new[] { "Accept if safe", "Refuse every cost", "Use someone else" }, 0, "Resolve includes responsibility."));
        questions.Add(new Question("Patience", "You fail to learn a spell after many tries. What now?", new[] { "Try a new method", "Quit", "Argue" }, 0, "Patience also means adjusting your method."));
        questions.Add(new Question("Patience", "A seed has not grown for a month. What do you do?", new[] { "Give up", "Check the conditions", "Throw away the soil" }, 1, "Patience looks for the reason before quitting."));
        questions.Add(new Question("Patience", "You are lost and keep returning to the same place. What helps?", new[] { "Cry", "Mark the path", "Run blindly" }, 1, "Patience learns from failure."));
    }
}
