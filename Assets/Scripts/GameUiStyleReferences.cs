using UnityEngine;

public class GameUiStyleReferences : MonoBehaviour
{
    [Header("Textures")]
    // Standard panel texture shared by UI scripts.
    [SerializeField] private Texture2D panelTexture;
    // Dialogue panel texture shared by story UI.
    [SerializeField] private Texture2D dialoguePanelTexture;
    // Backpack icon texture.
    [SerializeField] private Texture2D bagTexture;
    // Main menu panel texture.
    [SerializeField] private Texture2D menuTexture;

    [Header("Global UI Layout")]
    // Shared margin pushed into GameUiStyle.
    [SerializeField] private float margin = 24f;
    // Shared bottom prompt baseline.
    [SerializeField] private float bottomPromptY = 132f;
    // Shared font scale.
    [SerializeField] private float fontScale = 1.2f;
    // Reference resolution for UI layout.
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    // Minimum size for interaction prompts.
    [SerializeField] private Vector2 interactionPromptMinSize = new Vector2(640f, 112f);
    // Minimum size for system prompts.
    [SerializeField] private Vector2 systemPromptMinSize = new Vector2(920f, 156f);
    // Maximum dialogue panel width.
    [SerializeField] private float dialogueMaxWidth = 1280f;
    // Horizontal padding around dialogue panels.
    [SerializeField] private float dialogueHorizontalPadding = 96f;
    // Height multiplier for dialogue panels.
    [SerializeField] private float dialogueHeightScale = 1.45f;
    // Minimum dialogue panel height.
    [SerializeField] private float dialogueMinHeight = 300f;
    // Bottom offset for dialogue panels.
    [SerializeField] private float dialogueBottomOffset = 34f;
    // Vertical padding around dialogue panels.
    [SerializeField] private float dialogueVerticalPadding = 80f;
    // Gap for stacked side quest panels.
    [SerializeField] private float sideQuestStackGap = 12f;

    [Header("Main Menu Buttons")]
    // Shared size for main menu buttons.
    [SerializeField] private Vector2 mainMenuButtonSize = new Vector2(450f, 82f);
    // Start button offset.
    [SerializeField] private Vector2 mainMenuStartButton = new Vector2(0f, 46f);
    // Introduction button offset.
    [SerializeField] private Vector2 mainMenuIntroButton = new Vector2(0f, -45f);
    // Controls button offset.
    [SerializeField] private Vector2 mainMenuControlsButton = new Vector2(0f, -136f);
    // Settings button offset.
    [SerializeField] private Vector2 mainMenuSettingsButton = new Vector2(0f, -227f);
    // Credits button offset.
    [SerializeField] private Vector2 mainMenuCreditsButton = new Vector2(0f, -318f);
    // Exit button offset.
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
