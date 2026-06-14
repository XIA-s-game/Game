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
    private readonly string[] merchantConversation =
    {
        "Player: What happened?",
        "Merchant: My apple delivery is late. Can you check on it?"
    };

    private readonly string[] workerConversation =
    {
        "Worker: The cart broke down halfway here.",
        "Player: I can help repair it."
    };

    private readonly string[] workerRepairCompleteConversation =
    {
        "Worker: Thank you. The cart is fixed."
    };

    private readonly string[] merchantRewardConversation =
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

        float width = 600f;
        float height = repairTaskAdded ? 240f : 170f;
        Rect rect = GameUiStyle.SideQuestRect(width, height);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle titleStyle = GameUiStyle.LabelStyle(ref questTitleStyle, 26, TextAnchor.MiddleLeft, FontStyle.Bold);
        GUIStyle itemStyle = GameUiStyle.LabelStyle(ref questItemStyle, 24, TextAnchor.MiddleLeft);

        GUI.Label(new Rect(rect.x + 22f, rect.y + 18f, rect.width - 44f, 56f), "Side Quest", titleStyle);
        GUI.Label(new Rect(rect.x + 22f, rect.y + 86f, rect.width - 44f, 54f), "Check the merchant delivery", itemStyle);

        if (repairTaskAdded)
        {
            GUI.Label(new Rect(rect.x + 22f, rect.y + 154f, rect.width - 44f, 54f), "Repair the cart", itemStyle);
        }
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

        GUIStyle style = GameUiStyle.LabelStyle(ref promptStyle, 34, TextAnchor.MiddleCenter, FontStyle.Bold);

        Rect rect = GameUiStyle.InteractionPromptRect(420f, 60f);
        GameUiStyle.DrawDialoguePanel(rect);
        string text = showToolPrompt ? "Press E to pick up" : (showRepairPrompt ? "Press E to repair" : "Press E to talk");
        GUI.Label(rect, text, style);
    }

    private void DrawTimedMessage()
    {
        if (string.IsNullOrEmpty(timedMessage) || (!timedMessageWaitsForContinue && Time.time >= timedMessageEndsAt))
        {
            return;
        }

        Rect rect = GameUiStyle.DialogueRect(260f);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle style = GameUiStyle.LabelStyle(ref messageStyle, 34, TextAnchor.MiddleCenter, FontStyle.Normal, true);

        GUI.Label(new Rect(rect.x + 36f, rect.y + 30f, rect.width - 72f, rect.height - 126f), timedMessage, style);

        if (showContinueHint)
        {
            GUIStyle hintStyle = GameUiStyle.LabelStyle(ref continueHintStyle, 22, TextAnchor.MiddleRight);
            hintStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            GUI.Label(new Rect(rect.x + 36f, rect.y + rect.height - 72f, rect.width - 72f, 48f), "Press C to continue", hintStyle);
        }
    }

    private void DrawChoicePanel()
    {
        if (!merchantChoiceVisible)
        {
            return;
        }

        float width = Mathf.Min(900f, Screen.width - 80f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.5f - 180f, width, 360f);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle titleStyle = GameUiStyle.LabelStyle(ref choiceTitleStyle, 32, TextAnchor.MiddleCenter, FontStyle.Bold);
        GUIStyle optionStyle = GameUiStyle.LabelStyle(ref choiceOptionStyle, 28, TextAnchor.MiddleLeft);

        GUI.Label(new Rect(rect.x + 36f, rect.y + 28f, rect.width - 72f, 68f), "Choose", titleStyle);
        GUI.Label(new Rect(rect.x + 56f, rect.y + 124f, rect.width - 112f, 78f), "A: I will check", optionStyle);
        GUI.Label(new Rect(rect.x + 56f, rect.y + 222f, rect.width - 112f, 78f), "B: Leave", optionStyle);
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
