using System.Collections;
using UnityEngine;

public class MerchantDeliverySideQuest : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform merchant;
    [SerializeField] private Transform worker;
    [SerializeField] private Transform tools;
    [SerializeField] private GameObject cart;

    [Header("Items")]
    [SerializeField] private string repairToolItemName = "Repair Tools";
    [SerializeField] private string rewardItemName = "Glow Berry";

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Side Quest UI")]
    [SerializeField] private Vector2 sideQuestPanelSize = new Vector2(600f, 170f);
    [SerializeField] private float sideQuestExpandedHeight = 240f;
    [SerializeField] private Rect sideQuestTitleRect = new Rect(22f, 18f, 556f, 56f);
    [SerializeField] private Rect sideQuestFirstTaskRect = new Rect(22f, 86f, 556f, 54f);
    [SerializeField] private Rect sideQuestSecondTaskRect = new Rect(22f, 154f, 556f, 54f);
    [SerializeField] private int sideQuestTitleFontSize = 26;
    [SerializeField] private int sideQuestItemFontSize = 24;

    [Header("Merchant UI")]
    [SerializeField] private Vector2 interactionPromptSize = new Vector2(420f, 60f);
    [SerializeField] private int interactionPromptFontSize = 34;
    [SerializeField] private float messagePanelHeight = 260f;
    [SerializeField] private Rect messageTextRect = new Rect(180f, 118f, -252f, 90f);
    [SerializeField] private int messageFontSize = 30;
    [SerializeField] private TextAnchor messageAlignment = TextAnchor.UpperLeft;
    [SerializeField] private Rect continueHintRect = new Rect(36f, -72f, -72f, 48f);
    [SerializeField] private int continueHintFontSize = 22;
    [SerializeField] private float choicePanelMaxWidth = 900f;
    [SerializeField] private float choicePanelScreenPadding = 80f;
    [SerializeField] private float choicePanelCenterOffsetY = -180f;
    [SerializeField] private float choicePanelHeight = 360f;
    [SerializeField] private Rect choiceTitleRect = new Rect(36f, 28f, -72f, 68f);
    [SerializeField] private Rect choiceAcceptRect = new Rect(56f, 124f, -112f, 78f);
    [SerializeField] private Rect choiceLeaveRect = new Rect(56f, 222f, -112f, 78f);
    [SerializeField] private int choiceTitleFontSize = 32;
    [SerializeField] private int choiceOptionFontSize = 28;

    [Header("Merchant Text")]
    [SerializeField] private string continueHintText = "Press C to continue";
    [SerializeField] private string merchantTalkPrompt = "Press E to talk";
    [SerializeField] private string pickUpPrompt = "Press E to pick up";
    [SerializeField] private string repairPrompt = "Press E to repair";
    [SerializeField] private string choiceTitleText = "Choose";
    [SerializeField] private string choiceAcceptText = "A: I will check";
    [SerializeField] private string choiceLeaveText = "B: Leave";

    private bool questAccepted;
    private bool repairTaskAdded;
    private bool merchantChoiceVisible;
    private bool merchantConversationRunning;
    private bool nearMerchant;
    private bool nearWorker;
    private bool nearTools;
    private bool timedMessageWaitsForContinue;
    private bool hasRepairTools;
    private bool repairingCart;
    private bool repairCompleted;
    private bool merchantRewardGiven;
    private bool questCompleted;
    private int conversationIndex;
    private string timedMessage;
    private float timedMessageEndsAt;
    private string[] activeConversation;
    private bool showContinueHint;
    private GUIStyle questTitleStyle;
    private GUIStyle questItemStyle;
    private GUIStyle promptStyle;
    private GUIStyle messageStyle;
    private GUIStyle continueHintStyle;
    private GUIStyle choiceTitleStyle;
    private GUIStyle choiceOptionStyle;
    [Header("Merchant Dialogue")]
    [SerializeField] private string[] merchantConversation =
    {
        "Casper: What happened?",
        "Merchant: My apple delivery is late. Can you check on it?"
    };

    [SerializeField] private string[] workerConversation =
    {
        "Worker: The cart broke down halfway here.",
        "Casper: I can help repair it."
    };

    [SerializeField] private string[] workerRepairCompleteConversation =
    {
        "Worker: Thank you. The cart is fixed."
    };

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
