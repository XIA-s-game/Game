using UnityEngine;

public class GameUiStyleReferences : MonoBehaviour
{
    [Header("Textures")]
    [SerializeField] private Texture2D panelTexture;
    [SerializeField] private Texture2D dialoguePanelTexture;
    [SerializeField] private Texture2D bagTexture;
    [SerializeField] private Texture2D menuTexture;

    [Header("Global UI Layout")]
    [SerializeField] private float margin = 24f;
    [SerializeField] private float bottomPromptY = 132f;
    [SerializeField] private float fontScale = 1.2f;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private Vector2 interactionPromptMinSize = new Vector2(640f, 112f);
    [SerializeField] private Vector2 systemPromptMinSize = new Vector2(920f, 156f);
    [SerializeField] private float dialogueMaxWidth = 1280f;
    [SerializeField] private float dialogueHorizontalPadding = 96f;
    [SerializeField] private float dialogueHeightScale = 1.45f;
    [SerializeField] private float dialogueMinHeight = 300f;
    [SerializeField] private float dialogueBottomOffset = 34f;
    [SerializeField] private float dialogueVerticalPadding = 80f;
    [SerializeField] private float sideQuestStackGap = 12f;

    [Header("Main Menu Buttons")]
    [SerializeField] private Vector2 mainMenuButtonSize = new Vector2(450f, 82f);
    [SerializeField] private Vector2 mainMenuStartButton = new Vector2(0f, 46f);
    [SerializeField] private Vector2 mainMenuIntroButton = new Vector2(0f, -45f);
    [SerializeField] private Vector2 mainMenuControlsButton = new Vector2(0f, -136f);
    [SerializeField] private Vector2 mainMenuSettingsButton = new Vector2(0f, -227f);
    [SerializeField] private Vector2 mainMenuCreditsButton = new Vector2(0f, -318f);
    [SerializeField] private Vector2 mainMenuExitButton = new Vector2(0f, -409f);

    private void Awake()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    private void Apply()
    {
        GameUiStyle.SetTextures(panelTexture, dialoguePanelTexture, bagTexture, menuTexture);
        GameUiStyle.SetLayout(
            margin,
            bottomPromptY,
            fontScale,
            referenceResolution,
            interactionPromptMinSize,
            systemPromptMinSize,
            dialogueMaxWidth,
            dialogueHorizontalPadding,
            dialogueHeightScale,
            dialogueMinHeight,
            dialogueBottomOffset,
            dialogueVerticalPadding,
            sideQuestStackGap);
        GameUiStyle.SetMainMenuButtons(
            mainMenuButtonSize,
            mainMenuStartButton,
            mainMenuIntroButton,
            mainMenuControlsButton,
            mainMenuSettingsButton,
            mainMenuCreditsButton,
            mainMenuExitButton);
    }
}
