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
                if (lookAmount >= 1.5f)
                {
                    DemoCharacter.TutorialAllowMove = true;
                    EnterStep(TutorialStep.Move);
                }
                break;

            case TutorialStep.Move:
                if (HasMoveInput())
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
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    DemoCharacter.TutorialAllowCrouch = true;
                    EnterStep(TutorialStep.Crouch);
                }
                break;

            case TutorialStep.Crouch:
                if (Input.GetKeyDown(KeyCode.V))
                {
                    DemoCharacter.TutorialAllowRun = true;
                    EnterStep(TutorialStep.Run);
                }
                break;

            case TutorialStep.Run:
                if (DemoCharacter.TutorialRunObserved || HasRunInput())
                {
                    DemoCharacter.TutorialRunObserved = true;
                    EnterStep(TutorialStep.Interact);
                }
                break;

            case TutorialStep.Interact:
                if (Input.GetKeyDown(KeyCode.E))
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

        if (panelStyle != null && panelStyle.normal.background != null)
        {
            Destroy(panelStyle.normal.background);
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
        float width = Mathf.Min(760f, Screen.width - 48f);
        float height = step == TutorialStep.FinalMessage ? 146f : 176f;
        Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.72f, width, height);
        GUI.Box(panel, GUIContent.none, panelStyle);

        Rect titleRect = new Rect(panel.x + 24f, panel.y + 18f, panel.width - 48f, 32f);
        Rect messageRect = new Rect(panel.x + 24f, panel.y + 54f, panel.width - 48f, 64f);
        Rect progressRect = new Rect(panel.x + 24f, panel.y + panel.height - 38f, panel.width - 48f, 24f);
        GUI.Label(titleRect, step == TutorialStep.FinalMessage ? "Your Journey Begins" : "Movement Tutorial", titleStyle);
        GUI.Label(messageRect, GetStepMessage(), messageStyle);

        if (step != TutorialStep.FinalMessage)
        {
            GUI.Label(progressRect, GetProgressText(), progressStyle);
        }
    }

    private void BuildStyles()
    {
        if (panelStyle != null)
        {
            return;
        }

        panelStyle = new GUIStyle(GUI.skin.box);
        Texture2D panelBackground = new Texture2D(1, 1);
        panelBackground.SetPixel(0, 0, new Color(0.035f, 0.055f, 0.08f, 0.92f));
        panelBackground.Apply();
        panelStyle.normal.background = panelBackground;
        panelStyle.normal.textColor = Color.white;
        panelStyle.padding = new RectOffset(18, 18, 14, 14);

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 22;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(1f, 0.9f, 0.54f);

        messageStyle = new GUIStyle(GUI.skin.label);
        messageStyle.fontSize = 20;
        messageStyle.alignment = TextAnchor.MiddleCenter;
        messageStyle.wordWrap = true;
        messageStyle.normal.textColor = Color.white;

        progressStyle = new GUIStyle(GUI.skin.label);
        progressStyle.fontSize = 15;
        progressStyle.alignment = TextAnchor.MiddleCenter;
        progressStyle.normal.textColor = new Color(0.82f, 0.86f, 0.9f);
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
