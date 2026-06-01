// Guides the player through the first-scene controls and unlocks them step by step.
using AquariusMax.Fae.demo;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInstructionTutorial : MonoBehaviour
{
    private const string TutorialSceneName = "Enchanted Forest A";
    private const float StepPauseSeconds = 0.45f;
    private const float FinalMessageSeconds = 5f;

    private enum TutorialStep
    {
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
    private Texture2D panelTexture;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle messageStyle;
    private GUIStyle progressStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreateForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateForScene(scene);
    }

    private static void TryCreateForScene(Scene scene)
    {
        if (scene.name != TutorialSceneName || FindObjectOfType<PlayerInstructionTutorial>() != null)
        {
            return;
        }

        new GameObject("Player Instruction Tutorial").AddComponent<PlayerInstructionTutorial>();
    }

    private void Awake()
    {
        DemoCharacter.TutorialActive = true;
        DemoCharacter.TutorialAllowLook = true;
        DemoCharacter.TutorialAllowMove = false;
        DemoCharacter.TutorialAllowJump = false;
        DemoCharacter.TutorialAllowCrouch = false;
        DemoCharacter.TutorialAllowRun = false;
        DemoCharacter.TutorialRunObserved = false;
        EnterStep(TutorialStep.Look);
    }

    private void Update()
    {
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
                if (DemoCharacter.TutorialRunObserved)
                {
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
        DemoCharacter.TutorialActive = false;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        DemoCharacter.TutorialActive = false;
        DemoCharacter.TutorialAllowLook = true;
        DemoCharacter.TutorialAllowMove = true;
        DemoCharacter.TutorialAllowJump = true;
        DemoCharacter.TutorialAllowCrouch = true;
        DemoCharacter.TutorialAllowRun = true;
        DemoCharacter.TutorialRunObserved = false;
        if (panelTexture != null)
        {
            Destroy(panelTexture);
        }
    }

    private void OnGUI()
    {
        if (step == TutorialStep.Complete || Time.time - stepStartedAt < StepPauseSeconds)
        {
            return;
        }

        EnsureStyles();
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

    private void EnsureStyles()
    {
        if (panelStyle != null)
        {
            return;
        }

        panelStyle = new GUIStyle(GUI.skin.box);
        panelTexture = new Texture2D(1, 1);
        panelTexture.SetPixel(0, 0, new Color(0.035f, 0.055f, 0.08f, 0.92f));
        panelTexture.Apply();
        panelStyle.normal.background = panelTexture;
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
}
