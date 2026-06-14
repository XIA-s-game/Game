using System.Collections.Generic;
using UnityEngine;

public partial class ChapterTwoPuzzle : MonoBehaviour
{
    private const int BoardTileCount = 21;
    private const int DiceFaceCount = 7;

    [Header("Scene")]
    [SerializeField] private string targetSceneName = "Fae Homes Demo";
    [SerializeField] private string thirdPageItemName = "Third Page";
    [SerializeField] private string nextSceneName = "my scene";

    [Header("Item Names")]
    // Inventory names stay here so dialogue, save data, and backpack UI use the same strings.
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
    // Maze, board game, and lock motion tuning shared by the Chapter Two partial scripts.
    [SerializeField] private Vector3 openedMazeBlockPosition = new Vector3(355.87f, 16.58f, 663.52f);
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
    [SerializeField] private float finalUnlockMoveSeconds = 1.2f;
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
        // High-level mode gate so board, quiz, and dialogue input do not overlap.
        Exploring,
        Dialogue,
        Quiz,
        BoardGame
    }

    private enum BoardGamePhase
    {
        // Dice game state is separate from the chapter flow because it has its own turn loop.
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
    // Dragged scene references for Fae Homes Demo; the scripts avoid object-name searches.
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
    // Physical quest objects shown or hidden as the player completes side tasks.
    [SerializeField] private GameObject honeyObject;
    [SerializeField] private GameObject silverLeafObject;
    [SerializeField] private GameObject rockBearObject;
    [SerializeField] private GameObject finalDoorObject;
    [SerializeField] private GameObject fourthPagePaperObject;
    [SerializeField] private GameObject thirdPagePortalObject;
    [SerializeField] private GameObject mazeBlock;
    [SerializeField] private Transform redLockPart;
    [SerializeField] private Transform blueLockPart;
    [SerializeField] private Transform greenLockPart;
    [SerializeField] private Transform yellowLockPart;
    private readonly Vector3[] diceFaceLocalNormals = new Vector3[DiceFaceCount];
    private Vector3 guardOriginalPosition;
    private Vector3 mazeBlockOriginalPosition;
    private bool guardOriginalPositionReady;
    private bool mazeBlockOriginalPositionReady;
    private bool welcomeStarted;
    // Quest flags decide what resets on continue and what stays in the backpack.
    private bool firstGuardDialogueShown;
    private bool hasPass;
    private bool mazeOpened;
    private bool exitedMaze;
    private bool quizStarted;
    private bool quizCompleted;
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
    private GUIStyle promptStyle;
    private GUIStyle dialogueStyle;
    private GUIStyle hintStyle;
    private GUIStyle titleStyle;
    private GUIStyle optionStyle;

    private readonly List<Question> questions = new List<Question>();
    private readonly List<Question> quizQuestions = new List<Question>();
    private readonly List<string> inventoryItems = new List<string>();
    private Animator playerAnimator;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildQuestions();
        ReadSceneReferences();
        DropAirWallTwo();
    }

    private void OnDisable()
    {
        AquariusMax.Fae.demo.DemoCharacter.LockPlayerInput = false;
        AquariusMax.Fae.demo.DemoCharacter.LockMovementInput = false;
        AquariusMax.Fae.demo.DemoCharacter.ForceWalkAnimation = false;
    }

    private void Update()
    {
        if (!IsTargetScene())
        {
            AquariusMax.Fae.demo.DemoCharacter.LockPlayerInput = false;
            AquariusMax.Fae.demo.DemoCharacter.LockMovementInput = false;
            AquariusMax.Fae.demo.DemoCharacter.ForceWalkAnimation = false;
            return;
        }

        StartWelcomeIfNeeded();
        AquariusMax.Fae.demo.DemoCharacter.LockPlayerInput = state == FlowState.Quiz;
        AquariusMax.Fae.demo.DemoCharacter.LockMovementInput = state == FlowState.BoardGame;
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
        }
        else if (state == FlowState.Quiz)
        {
            DrawQuiz();
        }
        else if (state == FlowState.BoardGame)
        {
            DrawBoardGame();
        }
        else
        {
            DrawInteractPrompts();
        }
    }
}
