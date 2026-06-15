using AquariusMax.Fae.demo;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInstructionTutorial : MonoBehaviour
{
    // Tutorial only runs on new-game loads of Enchanted Forest A.
    private const string TutorialSceneName = "Enchanted Forest A";
    private const string TutorialNextLoadKey = "Tutorial.EnabledForNextLoad";
    private const float StepPauseSeconds = 0.45f;
    private const float FinalMessageSeconds = 5f;

    [Header("Tutorial UI")]
    [SerializeField] private float panelMaxWidth = 760f;
    [SerializeField] private float panelScreenPadding = 48f;
    [SerializeField] private float panelScreenYRatio = 0.72f;
    [SerializeField] private float normalPanelHeight = 176f;
    [SerializeField] private float finalPanelHeight = 146f;
    [SerializeField] private Rect titleRect = new Rect(24f, 18f, -48f, 32f);
    [SerializeField] private Rect messageRect = new Rect(24f, 54f, -48f, 64f);
    [SerializeField] private Rect progressRect = new Rect(24f, -38f, -48f, 24f);
    [SerializeField] private int titleFontSize = 22;
    [SerializeField] private int messageFontSize = 20;
    [SerializeField] private int progressFontSize = 15;
    [SerializeField] private Color titleColor = new Color(1f, 0.9f, 0.54f);
    [SerializeField] private Color messageColor = Color.white;
    [SerializeField] private Color progressColor = new Color(0.82f, 0.86f, 0.9f);

    private enum TutorialStep
    {
        // Input gates open step by step so the player sees one instruction at a time.
        Look,
        Move,
        Jump,
        Crouch,
        Run,
        Interact,
        FinalMessage,
        Complete
    }

    private TutorialStep step;
    private float stepStartedAt;
    private float lookAmount;
    private float moveHeldSeconds;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle messageStyle;
    private GUIStyle progressStyle;
    private bool tutorialEnabled;

    private void Awake()
    {
        // Continue game disables tutorial through PlayerPrefs.
        tutorialEnabled = SceneManager.GetActiveScene().name == TutorialSceneName;
        if (tutorialEnabled && PlayerPrefs.GetInt(TutorialNextLoadKey, 1) == 0)
        {
            tutorialEnabled = false;
        }

        if (!tutorialEnabled)
        {
            enabled = false;
            return;
        }

        StartTutorialInputLock();
        EnterStep(TutorialStep.Look);
    }

    private void Update()
    {
        // Each tutorial step waits for the player to perform that action once.
        switch (step)
        {
            case TutorialStep.Look:
                lookAmount += Mathf.Abs(Input.GetAxis("Mouse X")) + Mathf.Abs(Input.GetAxis("Mouse Y"));
                if (CanFinishCurrentStep() && lookAmount >= 1.5f)
                {
                    DemoCharacter.TutorialAllowMove = true;
                    EnterStep(TutorialStep.Move);
                }
                break;

            case TutorialStep.Move:
                if (CanFinishCurrentStep() && HasMoveInput())
                {
                    moveHeldSeconds += Time.deltaTime;
                }

                if (moveHeldSeconds >= 0.8f)
                {
                    DemoCharacter.TutorialAllowJump = true;
                    EnterStep(TutorialStep.Jump);
                }
                break;

            case TutorialStep.Jump:
                if (CanFinishCurrentStep() && Input.GetKeyDown(KeyCode.Space))
                {
                    DemoCharacter.TutorialAllowCrouch = true;
                    EnterStep(TutorialStep.Crouch);
                }
                break;

            case TutorialStep.Crouch:
                if (CanFinishCurrentStep() && Input.GetKeyDown(KeyCode.V))
                {
                    DemoCharacter.TutorialAllowRun = true;
                    EnterStep(TutorialStep.Run);
                }
                break;

            case TutorialStep.Run:
                if (CanFinishCurrentStep() && (DemoCharacter.TutorialRunObserved || HasRunInput()))
                {
                    DemoCharacter.TutorialRunObserved = true;
                    EnterStep(TutorialStep.Interact);
                }
                break;

            case TutorialStep.Interact:
                if (CanFinishCurrentStep() && Input.GetKeyDown(KeyCode.E))
                {
                    EnterStep(TutorialStep.FinalMessage);
                }
                break;

            case TutorialStep.FinalMessage:
                if (Time.time - stepStartedAt >= FinalMessageSeconds)
                {
                    CompleteTutorial();
                }
                break;
        }
    }

    private void EnterStep(TutorialStep nextStep)
    {
        step = nextStep;
        stepStartedAt = Time.time;
        if (step == TutorialStep.Run)
        {
            DemoCharacter.TutorialRunObserved = false;
        }
    }

    private bool CanFinishCurrentStep()
    {
        return Time.time - stepStartedAt >= StepPauseSeconds;
    }

    private void CompleteTutorial()
    {
        step = TutorialStep.Complete;
        ReleaseTutorialInputLock(false);
        Destroy(this);
    }

    public static void SetTutorialEnabledForNextLoad(bool enabled)
    {
        PlayerPrefs.SetInt(TutorialNextLoadKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (tutorialEnabled)
        {
            ReleaseTutorialInputLock(false);
        }

    }

    private static void StartTutorialInputLock()
    {
        // Uses DemoCharacter tutorial flags so movement rules stay inside the player controller.
        DemoCharacter.TutorialActive = true;
        DemoCharacter.TutorialAllowLook = true;
        DemoCharacter.TutorialAllowMove = true;
        DemoCharacter.TutorialAllowJump = true;
        DemoCharacter.TutorialAllowCrouch = true;
        DemoCharacter.TutorialAllowRun = true;
        DemoCharacter.TutorialRunObserved = false;
    }

    private static void ReleaseTutorialInputLock(bool runObserved)
    {
        DemoCharacter.TutorialActive = false;
        DemoCharacter.TutorialAllowLook = true;
        DemoCharacter.TutorialAllowMove = true;
        DemoCharacter.TutorialAllowJump = true;
        DemoCharacter.TutorialAllowCrouch = true;
        DemoCharacter.TutorialAllowRun = true;
        DemoCharacter.TutorialRunObserved = runObserved;
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || step == TutorialStep.Complete || Time.time - stepStartedAt < StepPauseSeconds)
        {
            return;
        }

        BuildStyles();
        float width = Mathf.Min(panelMaxWidth, Screen.width - panelScreenPadding);
        float height = step == TutorialStep.FinalMessage ? finalPanelHeight : normalPanelHeight;
        Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height * panelScreenYRatio, width, height);
        GUI.Box(panel, GUIContent.none, panelStyle);

        GUI.Label(InnerRect(panel, titleRect), step == TutorialStep.FinalMessage ? "Your Journey Begins" : "Movement Tutorial", titleStyle);
        GUI.Label(InnerRect(panel, messageRect), GetStepMessage(), messageStyle);

        if (step != TutorialStep.FinalMessage)
        {
            GUI.Label(InnerRect(panel, progressRect), GetProgressText(), progressStyle);
        }
    }

    private void BuildStyles()
    {
        if (panelStyle == null)
        {
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.padding = new RectOffset(18, 18, 14, 14);
        }

        panelStyle.normal.textColor = Color.white;

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
        }

        titleStyle.fontSize = titleFontSize;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = titleColor;

        if (messageStyle == null)
        {
            messageStyle = new GUIStyle(GUI.skin.label);
        }

        messageStyle.fontSize = messageFontSize;
        messageStyle.alignment = TextAnchor.MiddleCenter;
        messageStyle.wordWrap = true;
        messageStyle.normal.textColor = messageColor;

        if (progressStyle == null)
        {
            progressStyle = new GUIStyle(GUI.skin.label);
        }

        progressStyle.fontSize = progressFontSize;
        progressStyle.alignment = TextAnchor.MiddleCenter;
        progressStyle.normal.textColor = progressColor;
    }

    private static Rect InnerRect(Rect parent, Rect localRect)
    {
        float y = localRect.y >= 0f ? parent.y + localRect.y : parent.yMax + localRect.y;
        float width = localRect.width >= 0f ? localRect.width : parent.width + localRect.width;
        float height = localRect.height >= 0f ? localRect.height : parent.height + localRect.height;
        return new Rect(parent.x + localRect.x, y, width, height);
    }

    private string GetStepMessage()
    {
        switch (step)
        {
            case TutorialStep.Look:
                return "Move the mouse to look around and take in your surroundings.";
            case TutorialStep.Move:
                return "Use W, A, S, and D to move. Try walking forward for a moment.";
            case TutorialStep.Jump:
                return "Press Space to jump over obstacles.";
            case TutorialStep.Crouch:
                return "Press V to crouch and pass through low spaces.";
            case TutorialStep.Run:
                return "Hold Shift while using W, A, S, or D to sprint.";
            case TutorialStep.Interact:
                return "Press E to interact with nearby objects and characters. Try it now.";
            case TutorialStep.FinalMessage:
                return "See this flower-lined path? Follow it and begin your adventure!";
            default:
                return string.Empty;
        }
    }

    private string GetProgressText()
    {
        return "Tutorial Progress  " + ((int)step + 1) + " / 6";
    }

    private static bool HasMoveInput()
    {
        return Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
            Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;
    }

    private static bool HasRunInput()
    {
        return HasMoveInput() &&
            (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
    }
}
