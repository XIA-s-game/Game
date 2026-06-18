using System.Collections;
using UnityEngine;

public class MerchantDeliverySideQuest : MonoBehaviour
{
    [Header("Scene References")]
    // Player used for side quest distance checks.
    [SerializeField] private Transform player;
    // Merchant who starts and finishes the quest.
    [SerializeField] private Transform merchant;
    // Worker found near the broken cart.
    [SerializeField] private Transform worker;
    // Repair tools pickup.
    [SerializeField] private Transform tools;
    // Broken cart object repaired during the quest.
    [SerializeField] private GameObject cart;

    [Header("Items")]
    // Backpack name for the repair tools.
    [SerializeField] private string repairToolItemName = "Repair Tools";
    // Backpack reward name.
    [SerializeField] private string rewardItemName = "Glow Berry";

    [Header("Interaction")]
    // Distance for merchant, worker, tools, and cart interactions.
    [SerializeField] private float interactDistance = 4f;
    // Main interaction key.
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Side Quest UI")]
    // Compact quest panel size.
    [SerializeField] private Vector2 sideQuestPanelSize = new Vector2(600f, 170f);
    // Taller panel height when two tasks are visible.
    [SerializeField] private float sideQuestExpandedHeight = 240f;
    // Title text rect.
    [SerializeField] private Rect sideQuestTitleRect = new Rect(22f, 18f, 556f, 56f);
    // First task text rect.
    [SerializeField] private Rect sideQuestFirstTaskRect = new Rect(22f, 86f, 556f, 54f);
    // Second task text rect.
    [SerializeField] private Rect sideQuestSecondTaskRect = new Rect(22f, 154f, 556f, 54f);
    // Quest title font size.
    [SerializeField] private int sideQuestTitleFontSize = 26;
    // Quest task font size.
    [SerializeField] private int sideQuestItemFontSize = 24;

    [Header("Merchant UI")]
    // Interaction prompt box size.
    [SerializeField] private Vector2 interactionPromptSize = new Vector2(420f, 60f);
    // Interaction prompt font size.
    [SerializeField] private int interactionPromptFontSize = 34;
    // Dialogue/message panel height.
    [SerializeField] private float messagePanelHeight = 260f;
    // Message text rect inside the panel.
    [SerializeField] private Rect messageTextRect = new Rect(180f, 118f, -252f, 90f);
    // Message font size.
    [SerializeField] private int messageFontSize = 30;
    // Message alignment.
    [SerializeField] private TextAnchor messageAlignment = TextAnchor.UpperLeft;
    // Continue hint rect.
    [SerializeField] private Rect continueHintRect = new Rect(36f, -72f, -72f, 48f);
    // Continue hint font size.
    [SerializeField] private int continueHintFontSize = 22;
    // Maximum choice panel width.
    [SerializeField] private float choicePanelMaxWidth = 900f;
    // Choice panel screen padding.
    [SerializeField] private float choicePanelScreenPadding = 80f;
    // Vertical offset for the choice panel.
    [SerializeField] private float choicePanelCenterOffsetY = -180f;
    // Choice panel height.
    [SerializeField] private float choicePanelHeight = 360f;
    // Choice panel title rect.
    [SerializeField] private Rect choiceTitleRect = new Rect(36f, 28f, -72f, 68f);
    // Accept choice rect.
    [SerializeField] private Rect choiceAcceptRect = new Rect(56f, 124f, -112f, 78f);
    // Leave choice rect.
    [SerializeField] private Rect choiceLeaveRect = new Rect(56f, 222f, -112f, 78f);
    // Choice title font size.
    [SerializeField] private int choiceTitleFontSize = 32;
    // Choice option font size.
    [SerializeField] private int choiceOptionFontSize = 28;

    [Header("Merchant Text")]
    // Hint shown while dialogue waits for C.
    [SerializeField] private string continueHintText = "Press C to continue";
    // Prompt when near the merchant.
    [SerializeField] private string merchantTalkPrompt = "Press E to talk";
    // Prompt when near repair tools.
    [SerializeField] private string pickUpPrompt = "Press E to pick up";
    // Prompt when near the broken cart.
    [SerializeField] private string repairPrompt = "Press E to repair";
    // Choice panel title.
    [SerializeField] private string choiceTitleText = "Choose";
    // Accept quest choice text.
    [SerializeField] private string choiceAcceptText = "A: I will check";
    // Decline quest choice text.
    [SerializeField] private string choiceLeaveText = "B: Leave";

    // True after the player accepts the merchant's request.
    private bool questAccepted;
    // True after the cart repair task is shown.
    private bool repairTaskAdded;
    // True while the merchant choice panel is open.
    private bool merchantChoiceVisible;
    // True while a merchant or worker conversation is active.
    private bool merchantConversationRunning;
    // Player is close to the merchant.
    private bool nearMerchant;
    // Player is close to the worker.
    private bool nearWorker;
    // Player is close to the repair tools.
    private bool nearTools;
    // Current timed message waits for C instead of auto-closing.
    private bool timedMessageWaitsForContinue;
    // Player has picked up repair tools.
    private bool hasRepairTools;
    // Repair coroutine is running.
    private bool repairingCart;
    // Cart repair has finished.
    private bool repairCompleted;
    // Merchant has already given the reward.
    private bool merchantRewardGiven;
    // Whole side quest is finished.
    private bool questCompleted;
    // Current line in active conversation.
    private int conversationIndex;
    // Current timed message text.
    private string timedMessage;
    // Time when timed message closes.
    private float timedMessageEndsAt;
    // Conversation currently being shown.
    private string[] activeConversation;
    // Whether the continue hint should draw.
    private bool showContinueHint;
    // Cached quest title style.
    private GUIStyle questTitleStyle;
    // Cached quest task style.
    private GUIStyle questItemStyle;
    // Cached interaction prompt style.
    private GUIStyle promptStyle;
    // Cached message style.
    private GUIStyle messageStyle;
    // Cached continue hint style.
    private GUIStyle continueHintStyle;
    // Cached choice title style.
    private GUIStyle choiceTitleStyle;
    // Cached choice option style.
    private GUIStyle choiceOptionStyle;
    [Header("Merchant Dialogue")]
    // First conversation with the merchant.
    [SerializeField] private string[] merchantConversation =
    {
        "Casper: What happened?",
        "Merchant: My apple delivery is late. Can you check on it?"
    };

    // First conversation with the worker.
    [SerializeField] private string[] workerConversation =
    {
        "Worker: The cart broke down halfway here.",
        "Casper: I can help repair it."
    };

    // Worker dialogue after the cart is repaired.
    [SerializeField] private string[] workerRepairCompleteConversation =
    {
        "Worker: Thank you. The cart is fixed."
    };

    // Merchant reward dialogue after the repair is done.
    [SerializeField] private string[] merchantRewardConversation =
    {
        "Merchant: Thank you for helping. Please take this glow berry."
    };

    private void Awake()
    {
        SetupSceneObjects();
    }

    private void Update()
    {
        UpdateNearFlags();
        UpdateInput();
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        DrawQuestPanel();
        DrawInteractPrompt();
        DrawTimedMessage();
        DrawChoicePanel();
    }

    private void UpdateInput()
    {
        if (merchantChoiceVisible)
        {
            HandleChoiceInput();
            return;
        }

        if (merchantConversationRunning)
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                AdvanceConversation();
            }

            return;
        }

        if (repairingCart)
        {
            return;
        }

        bool interactPressed = Input.GetKeyDown(interactKey);

        if (interactPressed && nearMerchant)
        {
            HandleMerchantInteraction();
            return;
        }

        if (repairTaskAdded && !hasRepairTools && interactPressed && nearTools)
        {
            PickRepairTools();
            return;
        }

        if (repairTaskAdded && hasRepairTools && !repairCompleted && interactPressed && nearWorker)
        {
            StartCoroutine(RepairCart());
            return;
        }

        if (questAccepted && !repairTaskAdded && interactPressed && nearWorker)
        {
            StartConversation(workerConversation);
        }
    }

    private void StartConversation(string[] conversation)
    {
        merchantConversationRunning = true;
        conversationIndex = 0;
        activeConversation = conversation;
        merchantChoiceVisible = false;
        AdvanceConversation();
    }

    private void AdvanceConversation()
    {
        if (activeConversation != null && conversationIndex < activeConversation.Length)
        {
            ShowTimedMessage(activeConversation[conversationIndex], 0f, true);
            conversationIndex++;
            return;
        }

        string[] finishedConversation = activeConversation;
        merchantConversationRunning = false;
        ClearTimedMessage();
        activeConversation = null;
        HandleConversationFinished(finishedConversation);
    }

    private void AcceptQuest()
    {
        questAccepted = true;
        merchantChoiceVisible = false;
        ShowQuestObjects(true);
        ShowTimedMessage("Side quest started: check the delivery.", 3f, false);
    }

    private void PickRepairTools()
    {
        hasRepairTools = true;
        SetToolsVisible(false);
        GlobalBackpackUI.AddItem(repairToolItemName);
        ShowTimedMessage("Repair tools found.", 3f, false);
    }

    private IEnumerator RepairCart()
    {
        repairingCart = true;
        GlobalBackpackUI.RemoveItem(repairToolItemName);
        ShowTimedMessage("Repairing...", 3f, false);
        yield return new WaitForSeconds(3f);

        ShowTimedMessage("Repair complete.", 3f, false);
        yield return new WaitForSeconds(3f);

        repairingCart = false;
        repairCompleted = true;
        StartConversation(workerRepairCompleteConversation);
    }

    private void CompleteRepairTask()
    {
        ShowQuestObjects(false);
        ShowTimedMessage("Side quest complete: cart repaired.", 3f, false);
    }

    private void DrawQuestPanel()
    {
        if (!questAccepted || questCompleted)
        {
            return;
        }

        float height = repairTaskAdded ? sideQuestExpandedHeight : sideQuestPanelSize.y;
        Rect rect = GameUiStyle.SideQuestRect(sideQuestPanelSize.x, height);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle titleStyle = GameUiStyle.LabelStyle(ref questTitleStyle, sideQuestTitleFontSize, TextAnchor.MiddleLeft, FontStyle.Bold);
        GUIStyle itemStyle = GameUiStyle.LabelStyle(ref questItemStyle, sideQuestItemFontSize, TextAnchor.MiddleLeft);

        GUI.Label(OffsetRect(rect, sideQuestTitleRect), "Side Quest", titleStyle);
        GUI.Label(OffsetRect(rect, sideQuestFirstTaskRect), "Check the merchant delivery", itemStyle);

        if (repairTaskAdded)
        {
            GUI.Label(OffsetRect(rect, sideQuestSecondTaskRect), "Repair the cart", itemStyle);
        }
    }

    private static Rect OffsetRect(Rect parent, Rect localRect)
    {
        return new Rect(parent.x + localRect.x, parent.y + localRect.y, localRect.width, localRect.height);
    }

    private static Rect InnerRect(Rect parent, Rect localRect)
    {
        float y = localRect.y >= 0f ? parent.y + localRect.y : parent.yMax + localRect.y;
        float width = localRect.width >= 0f ? localRect.width : parent.width + localRect.width;
        float height = localRect.height >= 0f ? localRect.height : parent.height + localRect.height;
        return new Rect(parent.x + localRect.x, y, width, height);
    }

    private void DrawInteractPrompt()
    {
        if (merchantChoiceVisible || merchantConversationRunning)
        {
            return;
        }

        bool showMerchantPrompt = nearMerchant;
        bool showWorkerPrompt = questAccepted && !repairTaskAdded && nearWorker;
        bool showToolPrompt = repairTaskAdded && !hasRepairTools && nearTools;
        bool showRepairPrompt = repairTaskAdded && hasRepairTools && !repairCompleted && nearWorker;
        if (!showMerchantPrompt && !showWorkerPrompt && !showToolPrompt && !showRepairPrompt)
        {
            return;
        }

        GUIStyle style = GameUiStyle.LabelStyle(ref promptStyle, interactionPromptFontSize, TextAnchor.MiddleCenter, FontStyle.Bold);

        Rect rect = GameUiStyle.InteractionPromptRect(interactionPromptSize.x, interactionPromptSize.y);
        GameUiStyle.DrawDialoguePanel(rect);
        string text = showToolPrompt ? pickUpPrompt : (showRepairPrompt ? repairPrompt : merchantTalkPrompt);
        GUI.Label(rect, text, style);
    }

    private void DrawTimedMessage()
    {
        if (string.IsNullOrEmpty(timedMessage) || (!timedMessageWaitsForContinue && Time.time >= timedMessageEndsAt))
        {
            return;
        }

        Rect rect = GameUiStyle.DialogueRect(messagePanelHeight);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle style = GameUiStyle.LabelStyle(ref messageStyle, messageFontSize, messageAlignment, FontStyle.Normal, true);

        GUI.Label(InnerRect(rect, messageTextRect), timedMessage, style);

        if (showContinueHint)
        {
            GUIStyle hintStyle = GameUiStyle.LabelStyle(ref continueHintStyle, continueHintFontSize, TextAnchor.MiddleRight);
            hintStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            GUI.Label(InnerRect(rect, continueHintRect), continueHintText, hintStyle);
        }
    }

    private void DrawChoicePanel()
    {
        if (!merchantChoiceVisible)
        {
            return;
        }

        float width = Mathf.Min(choicePanelMaxWidth, Screen.width - choicePanelScreenPadding);
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.5f + choicePanelCenterOffsetY, width, choicePanelHeight);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle titleStyle = GameUiStyle.LabelStyle(ref choiceTitleStyle, choiceTitleFontSize, TextAnchor.MiddleCenter, FontStyle.Bold);
        GUIStyle optionStyle = GameUiStyle.LabelStyle(ref choiceOptionStyle, choiceOptionFontSize, TextAnchor.MiddleLeft);

        GUI.Label(InnerRect(rect, choiceTitleRect), choiceTitleText, titleStyle);
        GUI.Label(InnerRect(rect, choiceAcceptRect), choiceAcceptText, optionStyle);
        GUI.Label(InnerRect(rect, choiceLeaveRect), choiceLeaveText, optionStyle);
    }

    private void ShowTimedMessage(string message, float seconds, bool waitForContinue)
    {
        timedMessage = message;
        timedMessageWaitsForContinue = waitForContinue;
        showContinueHint = waitForContinue;
        timedMessageEndsAt = string.IsNullOrEmpty(message) || waitForContinue ? 0f : Time.time + seconds;

        if (!string.IsNullOrEmpty(message))
        {
            GameAudioManager.PlayKnob();
            if (message.IndexOf("complete", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                GameAudioManager.PlaySuccess();
            }
        }
    }

    private void ClearTimedMessage()
    {
        timedMessageWaitsForContinue = false;
        showContinueHint = false;
        timedMessage = null;
    }

    private void UpdateNearFlags()
    {
        nearMerchant = IsNear(player, merchant);
        nearWorker = IsNear(player, worker) &&
            ((questAccepted && !repairTaskAdded) || (repairTaskAdded && !repairCompleted));
        nearTools = repairTaskAdded && !hasRepairTools && IsNear(player, tools);
    }

    private void SetupSceneObjects()
    {
        ShowQuestObjects(questAccepted && !repairCompleted);
        if (hasRepairTools)
        {
            SetToolsVisible(false);
        }
    }

    private void HandleChoiceInput()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            AcceptQuest();
            return;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            merchantChoiceVisible = false;
            ClearTimedMessage();
        }
    }

    private void HandleMerchantInteraction()
    {
        if (repairCompleted && !merchantRewardGiven)
        {
            StartConversation(merchantRewardConversation);
            return;
        }

        if (questCompleted)
        {
            ShowTimedMessage("Merchant: Thank you.", 3f, false);
            return;
        }

        if (questAccepted)
        {
            ShowTimedMessage("Merchant: The worker should be west of here.", 3f, false);
            return;
        }

        StartConversation(merchantConversation);
    }

    private void HandleConversationFinished(string[] finishedConversation)
    {
        if (finishedConversation == merchantConversation)
        {
            merchantChoiceVisible = true;
            return;
        }

        if (finishedConversation == workerConversation)
        {
            repairTaskAdded = true;
            ShowTimedMessage("Side quest updated: repair the cart.", 3f, false);
            return;
        }

        if (finishedConversation == workerRepairCompleteConversation)
        {
            CompleteRepairTask();
            return;
        }

        if (finishedConversation != merchantRewardConversation)
        {
            return;
        }

        merchantRewardGiven = true;
        questCompleted = true;
        GlobalBackpackUI.AddItem(rewardItemName);
        ShowTimedMessage("Side quest complete: delivery checked.", 3f, false);
    }

    private void ShowQuestObjects(bool show)
    {
        if (cart != null)
        {
            cart.SetActive(show);
        }

        if (worker != null)
        {
            worker.gameObject.SetActive(show);
        }
    }

    private void SetToolsVisible(bool visible)
    {
        if (tools != null)
        {
            tools.gameObject.SetActive(visible);
        }
    }

    private bool IsNear(Transform source, Transform target)
    {
        if (source == null || target == null || !target.gameObject.activeInHierarchy)
        {
            return false;
        }

        return Vector3.Distance(source.position, target.position) <= interactDistance;
    }
}
