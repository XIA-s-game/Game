using System.Collections.Generic;
using UnityEngine;

public partial class ChapterTwoPuzzle : MonoBehaviour
{
    // Number of spaces in the dice board mini-game.
    private const int BoardTileCount = 21;
    // Dice face references include the six sides plus one spare slot used by the scene setup.
    private const int DiceFaceCount = 7;

    [Header("Scene")]
    // Scene where this controller is allowed to run.
    [SerializeField] private string targetSceneName = "Chapter2_ForestMaze_and_Chapter3_ForestTreehouse";
    // Backpack name for the page found in the locked house.
    [SerializeField] private string thirdPageItemName = "Third Page";
    // Scene opened after the Chapter 3 portal.
    [SerializeField] private string nextSceneName = "Chapter4_Forest_Swamp";

    [Header("Item Names")]
    // Inventory names stay here so dialogue, save data, and backpack UI use the same strings.
    [SerializeField] private string mazePassItemName = "Maze Pass";
    // Backpack name for the maze reward page.
    [SerializeField] private string secondPageItemName = "Second Page";
    // Key earned from the piano memory challenge.
    [SerializeField] private string redKeyItemName = "Red Key";
    // Key earned from the color square challenge.
    [SerializeField] private string blueKeyItemName = "Blue Key";
    // Key earned from the old man's card game.
    [SerializeField] private string greenKeyItemName = "Green Key";
    // Key earned from helping the baker.
    [SerializeField] private string yellowKeyItemName = "Yellow Key";
    // Empty honey jar item.
    [SerializeField] private string honeyJarItemName = "Honey Jar";
    // Filled honey jar item.
    [SerializeField] private string fullHoneyJarItemName = "Full Honey Jar";
    // Leaf item requested by the bear.
    [SerializeField] private string silverLeafItemName = "Silver Leaf";

    [Header("Tuning")]
    // Maze, board game, and lock motion tuning shared by the Chapter Two partial scripts.
    [SerializeField] private Vector3 openedMazeBlockPosition = new Vector3(355.87f, 16.58f, 663.52f);
    // Default distance for talking, picking up, and small interactions.
    [SerializeField] private float interactDistance = 4f;
    // Slightly tighter range for starting the board game.
    [SerializeField] private float boardInteractDistance = 3.5f;
    // Player movement speed while walking over board tiles.
    [SerializeField] private float boardMoveSpeed = 4f;
    // Time used for the dice throw animation.
    [SerializeField] private float diceThrowSeconds = 1.8f;
    // Height added to the dice during the throw.
    [SerializeField] private float diceThrowHeight = 4.5f;
    // Small offset so board movement lands just above the tile.
    [SerializeField] private float playerGroundOffset = 0.01f;
    // Distance used to detect the maze exit.
    [SerializeField] private float exitDistance = 0.9f;
    // How far the guard steps aside when the maze opens.
    [SerializeField] private float guardMoveRightDistance = 2f;
    // How far the second air wall drops out of view.
    [SerializeField] private float airWallTwoDropDistance = 100f;
    // Drop speed for the second air wall.
    [SerializeField] private float airWallTwoDropSpeed = 12f;
    // Local Y position for each lock part after its key is used.
    [SerializeField] private float lockPartTargetLocalY = 0.27f;
    // Time for the final locked-house opening motion.
    [SerializeField] private float finalUnlockMoveSeconds = 1.2f;
    // Main interaction key for this chapter.
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    // Key used to advance dialogue.
    [SerializeField] private KeyCode continueKey = KeyCode.C;
    // Debug/helper button size for dropping the wall.
    [SerializeField] private Vector2 wallButtonSize = new Vector2(180f, 56f);
    // Screen margin for the wall helper button.
    [SerializeField] private Vector2 wallButtonMargin = new Vector2(18f, 18f);
    // Label on the wall helper button.
    [SerializeField] private string wallButtonText = "Drop Wall";

    [Header("Dialogue")]
    // Line shown when Casper finds the page box.
    [SerializeField] private string[] boxFoundDialogue = { "Casper: The third page is in this box." };
    // Line shown before all four keys are collected.
    [SerializeField] private string[] lockedHouseDialogue = { "Casper: This house is locked. I need four keys." };
    // Villager hint that points the player to the locked house.
    [SerializeField] private string[] listenerDialogue =
    {
        "Casper: Do you know where the third page is?",
        "Villager: Try the locked house up the hill.",
        "Casper: Thank you."
    };
    // Short line after the player exits the maze.
    [SerializeField] private string[] mazeExitDialogue = { "Casper: I finally made it out." };
    // Baker line once the honey quest is no longer needed.
    [SerializeField] private string[] bakerReadyDialogue = { "Baker: Everything is ready now." };
    // Baker reward line before giving the yellow key.
    [SerializeField] private string[] bakerRewardDialogue = { "Baker: Thank you. Take this yellow key." };
    // First baker conversation.
    [SerializeField] private string[] bakerIntroDialogue =
    {
        "Casper: Do you know where the four keys are?",
        "Baker: I have the yellow key, but I need honey first.",
        "Casper: I can help with that.",
        "Baker: Please bring me a jar of honey."
    };
    // Baker reminder if the player has not met the bear yet.
    [SerializeField] private string[] bakerHintDialogue = { "Baker: The bear near the tree has honey." };
    // Bear line after the honey has already been taken.
    [SerializeField] private string[] bearEmptyDialogue = { "Bear: That was my only honey jar." };
    // Bear line after the silver leaf is found.
    [SerializeField] private string[] bearLeafFoundDialogue =
    {
        "Casper: I found the silver leaf.",
        "Bear: Pour the honey into the jar over there."
    };
    // First bear conversation.
    [SerializeField] private string[] bearIntroDialogue =
    {
        "Casper: I need some honey.",
        "Bear: Bring me a silver leaf first.",
        "Casper: I will find one."
    };
    // Bear reminder while the silver leaf is missing.
    [SerializeField] private string[] bearWaitingDialogue = { "Bear: Did you find the silver leaf?" };
    // Bear reminder after the leaf is accepted.
    [SerializeField] private string[] bearPourDialogue = { "Bear: Pour the honey into the jar over there." };
    // Guard line after the quiz reward has already been earned.
    [SerializeField] private string[] guardDoneDialogue = { "Guard: You already proved yourself. The second page is yours." };
    // Guard line once the player has the maze pass.
    [SerializeField] private string[] guardMazeIntroDialogue = { "Guard: Pass the maze and answer my questions to earn the second page." };
    // First guard conversation.
    [SerializeField] private string[] guardFirstDialogue =
    {
        "Casper: I am looking for the second page.",
        "Guard: No pass, no entry.",
        "Casper: Then I need to find a pass first."
    };
    // Guard reminder before the pass is earned.
    [SerializeField] private string[] guardNoPassDialogue = { "Guard: No pass, no entry." };
    // Opening line for the maze area.
    [SerializeField] private string[] welcomeDialogue = { "Casper: This maze is huge." };
    // Dialogue that starts the quality quiz.
    [SerializeField] private string[] quizIntroDialogue =
    {
        "Guard: You passed the maze. Now answer my questions.",
        "Guard: You need eight correct answers. Two wrong answers sends you back.",
        "Guard: Listen carefully."
    };

    [Header("Prompts")]
    // Generic dialogue continue text.
    [SerializeField] private string continuePrompt = "Press C to continue";
    // Prompt for starting the dice board game.
    [SerializeField] private string startPrompt = "Press E to start";
    // Prompt for picking up the empty honey jar.
    [SerializeField] private string takeHoneyPrompt = "Press E to take honey jar";
    // Prompt for collecting the silver leaf.
    [SerializeField] private string pickLeafPrompt = "Press E to pick leaf";
    // Prompt at the honey filling station.
    [SerializeField] private string pourHoneyPrompt = "Press E to pour honey";
    // Prompt at the locked house.
    [SerializeField] private string unlockPrompt = "Press E to unlock";
    // Generic pickup prompt.
    [SerializeField] private string pickupPrompt = "Press E to pick up";
    // Prompt for chapter portals.
    [SerializeField] private string travelPrompt = "Press E to travel";
    // Prompt for NPC conversations.
    [SerializeField] private string talkPrompt = "Press E to talk";
    // Fallback interaction prompt.
    [SerializeField] private string interactPrompt = "Press E to interact";
    // Task prompt before the honey jar is found.
    [SerializeField] private string findHoneyPrompt = "Find a honey jar.";
    // Task prompt after the empty jar is found.
    [SerializeField] private string useHoneyStationPrompt = "Use the honey jar station.";
    // Feedback after collecting the empty jar.
    [SerializeField] private string honeyFoundPrompt = "Honey jar found.";
    // Feedback after collecting the silver leaf.
    [SerializeField] private string silverLeafFoundPrompt = "Silver leaf found.";
    // Feedback after filling the honey jar.
    [SerializeField] private string fullHoneyFoundPrompt = "Full honey jar found.";
    // Message shown while the house unlock animation plays.
    [SerializeField] private string unlockingPrompt = "Unlocking...";
    // Message after the house is opened.
    [SerializeField] private string doorUnlockedPrompt = "Door unlocked.";
    // Message after the third page is picked up.
    [SerializeField] private string thirdPageFoundPrompt = "Third page found.";
    // Message after winning the board game.
    [SerializeField] private string boardWonPrompt = "Game won. Maze pass received.";
    // Full board prompt with round number.
    [SerializeField] private string boardRollPromptFormat = "Round {0}: press E to roll the dice.";
    // Shorter board prompt for narrow space.
    [SerializeField] private string boardRollShortFormat = "Round {0}: press E to roll";
    // Message while the dice is moving.
    [SerializeField] private string rollingDicePrompt = "Rolling dice...";
    // Message after the dice lands.
    [SerializeField] private string rolledPromptFormat = "Rolled {0}.";
    // Message telling the player where the board piece will go.
    [SerializeField] private string boardMovePromptFormat = "Move {0} to tile {1}.";
    // Short movement message after a dice roll.
    [SerializeField] private string boardMovingPromptFormat = "Rolled {0}, moving...";
    // Generic movement message.
    [SerializeField] private string movingPrompt = "Moving...";
    // First system prompt in the maze scene.
    [SerializeField] private string welcomePrompt = "Welcome to the forest maze.";
    // Message after passing the quiz.
    [SerializeField] private string quizPassedPrompt = "Quiz passed. Second page received.";
    // Message after failing the quiz.
    [SerializeField] private string quizFailedPrompt = "Two wrong answers. Start the maze again.";
    // Quiz feedback text before the reset.
    [SerializeField] private string quizFailedFeedback = "Two wrong answers. Go through the maze again.";

    [Header("Dialogue UI")]
    // Bottom dialogue panel height.
    [SerializeField] private float dialoguePanelHeight = 260f;
    // Top-left offset for dialogue text inside the panel.
    [SerializeField] private Vector2 dialogueTextOffset = new Vector2(180f, 130f);
    // Right padding for dialogue text.
    [SerializeField] private float dialogueTextRightPadding = 72f;
    // Bottom padding for dialogue text.
    [SerializeField] private float dialogueTextBottomPadding = 126f;
    // Dialogue body font size.
    [SerializeField] private int dialogueTextFontSize = 30;
    // Offset used by the continue prompt.
    [SerializeField] private Vector2 continuePromptOffset = new Vector2(0f, 130f);
    // Right padding for the continue prompt.
    [SerializeField] private float continuePromptRightPadding = 160f;
    // Continue prompt label height.
    [SerializeField] private float continuePromptHeight = 48f;
    // Continue prompt font size.
    [SerializeField] private int continuePromptFontSize = 22;

    [Header("Prompt UI")]
    // Size for top system prompt panels.
    [SerializeField] private Vector2 systemPromptSize = new Vector2(760f, 92f);
    // Font size for system prompts.
    [SerializeField] private int systemPromptFontSize = 30;
    // Size for small interaction prompts.
    [SerializeField] private Vector2 interactionPromptSize = new Vector2(440f, 60f);
    // Font size for interaction prompts.
    [SerializeField] private int interactionPromptFontSize = 30;
    // Size for board-game prompts.
    [SerializeField] private Vector2 boardPromptSize = new Vector2(620f, 86f);
    // Font size for board-game prompts.
    [SerializeField] private int boardPromptFontSize = 30;

    [Header("Quiz UI")]
    // Main quiz panel rectangle in reference-screen space.
    [SerializeField] private Rect quizPanelRect = new Rect(70f, 50f, -140f, -100f);
    // Feedback text area after an answer.
    [SerializeField] private Rect quizFeedbackTextRect = new Rect(396f, 558f, -112f, -160f);
    // Continue hint area on feedback screens.
    [SerializeField] private Rect quizFeedbackContinueRect = new Rect(-200f, 988f, -112f, 54f);
    // Quiz title/header area.
    [SerializeField] private Rect quizHeaderRect = new Rect(98f, 428f, -96f, 72f);
    // Current question text area.
    [SerializeField] private Rect quizQuestionRect = new Rect(388f, 528f, -116f, 168f);
    // First answer option area.
    [SerializeField] private Rect quizOptionStartRect = new Rect(388f, 590f, -140f, 78f);
    // Vertical gap between answer options.
    [SerializeField] private float quizOptionSpacing = 92f;
    // Font size for quiz feedback.
    [SerializeField] private int quizFeedbackFontSize = 36;
    // Font size for quiz feedback continue hint.
    [SerializeField] private int quizFeedbackContinueFontSize = 26;
    // Font size for quiz header.
    [SerializeField] private int quizHeaderFontSize = 28;
    // Font size for quiz question text.
    [SerializeField] private int quizQuestionFontSize = 30;
    // Font size for quiz answer options.
    [SerializeField] private int quizOptionFontSize = 26;

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
        // Quality this question belongs to.
        public readonly string virtue;
        // Question shown on screen.
        public readonly string text;
        // Three answer choices.
        public readonly string[] options;
        // Index of the correct choice.
        public readonly int correctIndex;
        // Short explanation shown after answering.
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

    // Active chapter-two controller.
    private static ChapterTwoPuzzle instance;

    [Header("Scene References")]
    // Dragged scene references for Chapter2_ForestMaze_and_Chapter3_ForestTreehouse; the scripts avoid object-name searches.
    [SerializeField] private Transform player;
    // Guard who blocks the maze.
    [SerializeField] private Transform guard;
    // Interaction point for the guard.
    [SerializeField] private Transform guardInteract;
    // Trigger point near the maze exit.
    [SerializeField] private Transform exitInteract;
    // Start point for board game and maze resume.
    [SerializeField] private Transform startTile;
    // Last maze tile or exit point.
    [SerializeField] private Transform endTile;
    // Dice object moved and rotated during the board game.
    [SerializeField] private Transform dice;
    // Baker conversation point.
    [SerializeField] private Transform bakerInteract;
    // Bear conversation point.
    [SerializeField] private Transform bearInteract;
    // Villager/listener hint point.
    [SerializeField] private Transform listenerInteract;
    // Locked house transform.
    [SerializeField] private Transform lockedHouse;
    // Box that holds the page.
    [SerializeField] private Transform box;
    // Honey filling station.
    [SerializeField] private Transform honeyGive;
    // Wall dropped after the quiz route is opened.
    [SerializeField] private Transform airWallTwo;
    // Board path tiles in order.
    [SerializeField] private Transform[] boardTiles = new Transform[BoardTileCount];
    // Dice face markers used to read the landed result.
    [SerializeField] private Transform[] diceFaces = new Transform[DiceFaceCount];

    [Header("Item Objects")]
    // Physical quest objects shown or hidden as the player completes side tasks.
    [SerializeField] private GameObject honeyObject;
    // Silver leaf pickup.
    [SerializeField] private GameObject silverLeafObject;
    // Bear/rock object hidden after the honey quest state changes.
    [SerializeField] private GameObject rockBearObject;
    // Door object that disappears or moves when unlocked.
    [SerializeField] private GameObject finalDoorObject;
    // Visible page paper inside the house.
    [SerializeField] private GameObject fourthPagePaperObject;
    // Portal to the next scene.
    [SerializeField] private GameObject thirdPagePortalObject;
    // Physical maze blocker.
    [SerializeField] private GameObject mazeBlock;
    // Red lock part on the house.
    [SerializeField] private Transform redLockPart;
    // Blue lock part on the house.
    [SerializeField] private Transform blueLockPart;
    // Green lock part on the house.
    [SerializeField] private Transform greenLockPart;
    // Yellow lock part on the house.
    [SerializeField] private Transform yellowLockPart;
    // Cached local face normals for dice result checks.
    private readonly Vector3[] diceFaceLocalNormals = new Vector3[DiceFaceCount];
    // Guard position before he moves aside.
    private Vector3 guardOriginalPosition;
    // Maze block position before it opens.
    private Vector3 mazeBlockOriginalPosition;
    // True once the guard start position is cached.
    private bool guardOriginalPositionReady;
    // True once the maze block start position is cached.
    private bool mazeBlockOriginalPositionReady;
    // Stops the welcome line from starting twice.
    private bool welcomeStarted;
    // Quest flags decide what resets on continue and what stays in the backpack.
    private bool firstGuardDialogueShown;
    // Player has earned the maze pass.
    private bool hasPass;
    // Maze entrance has opened.
    private bool mazeOpened;
    // Player has reached the maze exit.
    private bool exitedMaze;
    // Quiz intro has begun.
    private bool quizStarted;
    // Quiz reward has been earned.
    private bool quizCompleted;
    // Second air wall has already dropped.
    private bool airWallTwoDropped;
    // Running wall-drop coroutine.
    private Coroutine airWallTwoRoutine;
    // Current dice board phase.
    private BoardGamePhase boardGamePhase;
    // Current board round number.
    private int boardRound;
    // Current tile index on the board.
    private int boardPosition;
    // Last rolled dice value.
    private int lastDiceRoll;
    // True when all board references are usable.
    private bool boardReferencesReady;
    // Dice position before the board game starts.
    private Vector3 diceOriginalPosition;
    // Dice rotation before the board game starts.
    private Quaternion diceOriginalRotation;
    // True once dice transform is cached.
    private bool diceOriginalTransformReady;
    // Player controller temporarily disabled during board movement.
    private CharacterController boardMoveController;
    // Previous enabled state for the player controller.
    private bool boardMoveControllerWasEnabled;
    // Running board-game coroutine.
    private Coroutine boardRoutine;
    // Has the baker given the first request.
    private bool bakerIntroDone;
    // True after the baker sends the player to find honey.
    private bool waitingForHoneyBottle;
    // Player is carrying the empty honey jar.
    private bool hasHoneyBottle;
    // Has the bear given the first silver leaf request.
    private bool bearIntroDone;
    // Bear is waiting for the silver leaf.
    private bool bearAskedForSilverLeaf;
    // Player has collected the silver leaf.
    private bool hasSilverLeaf;
    // Bear is ready to let the player fill the honey jar.
    private bool bearRewardReady;
    // Honey station can now be used.
    private bool honeyPourReady;
    // Player is carrying the filled honey jar.
    private bool hasFullHoneyBottle;
    // Yellow key quest has been completed.
    private bool bakerQuestCompleted;
    // Villager hint has already been shown.
    private bool listenerDialogueShown;
    // Locked-house reminder has already been shown.
    private bool lockedHouseDialogueShown;
    // Locked house has finished opening.
    private bool lockedHouseOpened;
    // House opening animation is running.
    private bool unlockingHouse;
    // Page box dialogue has already been shown.
    private bool boxDialogueShown;
    // Player can now pick up the page paper.
    private bool waitingForFourthPagePickup;
    // Page paper has been picked up.
    private bool fourthPagePicked;
    // Portal to Chapter 4 is active.
    private bool thirdPagePortalUnlocked;
    // Current input/UI mode.
    private FlowState state;
    // Dialogue lines currently on screen.
    private string[] activeLines;
    // Current dialogue line index.
    private int lineIndex;
    // Callback used after dialogue closes.
    private System.Action dialogueFinished;
    // Current timed system prompt.
    private string currentSystemPrompt;
    // Time when the system prompt disappears.
    private float systemPromptEndsAt;
    // Index in the generated quiz list.
    private int currentQuestionIndex;
    // Number of correct quiz answers.
    private int correctAnswerCount;
    // Number of wrong quiz answers.
    private int wrongAnswerCount;
    // Text shown after the current quiz answer.
    private string quizFeedback;
    // True while the quiz feedback screen is open.
    private bool showingQuizFeedback;
    // Quiz should complete after feedback closes.
    private bool quizPassedAfterFeedback;
    // Quiz should reset after feedback closes.
    private bool quizFailedAfterFeedback;
    // Cached prompt label style.
    private GUIStyle promptStyle;
    // Cached dialogue text style.
    private GUIStyle dialogueStyle;
    // Cached continue-hint style.
    private GUIStyle hintStyle;
    // Cached quiz title/header style.
    private GUIStyle titleStyle;
    // Cached quiz option style.
    private GUIStyle optionStyle;

    // Full pool of possible quiz questions.
    private readonly List<Question> questions = new List<Question>();
    // Questions selected for the current quiz run.
    private readonly List<Question> quizQuestions = new List<Question>();
    // Lightweight local inventory mirror for chapter-two checks.
    private readonly List<string> inventoryItems = new List<string>();
    // Player animator used during board movement.
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
            DrawWallButton();
        }
    }

    private void DrawWallButton()
    {
        if (!IsTargetScene() || state != FlowState.Exploring || airWallTwo == null || airWallTwoDropped)
        {
            return;
        }

        Rect rect = new Rect(
            wallButtonMargin.x,
            Screen.height - wallButtonMargin.y - wallButtonSize.y,
            wallButtonSize.x,
            wallButtonSize.y);

        if (GUI.Button(rect, wallButtonText))
        {
            DropAirWallTwo();
        }
    }
}
