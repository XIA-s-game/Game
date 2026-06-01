// Main function: Runs the merchant repair and delivery side quest, including dialogue, accepting the task, collecting tools, repairing the cart, merchant animation, and quest UI.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MerchantDeliverySideQuest : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform merchant;
    [SerializeField] private Transform worker;
    [SerializeField] private Transform tools;
    [SerializeField] private Transform[] toolParts;
    [SerializeField] private GameObject cart;
    [SerializeField] private List<GameObject> apples = new List<GameObject>();

    [Header("Items")]
    [SerializeField] private string repairToolItemName = "Repair Tools";
    [SerializeField] private string rewardItemName = "Glow Berry";

    [Header("Animation")]
    [SerializeField] private Avatar merchantAvatar;
    [SerializeField] private RuntimeAnimatorController merchantMoveController;
    [SerializeField] private RuntimeAnimatorController merchantStandController;

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

    // Function: Initializes component references, cached state, and default runtime data.
    private void Awake()
    {
        SetupSceneObjects();
    }

    // Function: Updates input handling, interaction checks, and active gameplay flow each frame.
    private void Update()
    {
        UpdateNearFlags();
        UpdateMerchantAnimation();
        UpdateInput();
    }

    // Function: Draws this script's IMGUI prompts, panels, and dialogue.
    private void OnGUI()
    {
        DrawQuestPanel();
        DrawInteractPrompt();
        DrawTimedMessage();
        DrawChoicePanel();
    }

    // Function: Updates input state, input, or presentation.
    private void UpdateInput()
    {
        if (merchantChoiceVisible)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                AcceptQuest();
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                merchantChoiceVisible = false;
                ShowTimedMessage(null, 0f, false);
            }

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

        if (Input.GetKeyDown(interactKey) && nearMerchant)
        {
            if (repairCompleted && !merchantRewardGiven)
            {
                PlayMerchantStand();
                StartConversation(merchantRewardConversation);
            }
            else if (questCompleted)
            {
                PlayMerchantStand();
                ShowTimedMessage("Merchant: Thank you.", 3f, false);
            }
            else if (questAccepted)
            {
                PlayMerchantStand();
                ShowTimedMessage("Merchant: The worker should be west of here.", 3f, false);
            }
            else
            {
                StartConversation(merchantConversation);
            }

            return;
        }

        if (repairTaskAdded && !hasRepairTools && Input.GetKeyDown(interactKey) && nearTools)
        {
            PickRepairTools();
            return;
        }

        if (repairTaskAdded && hasRepairTools && !repairCompleted && Input.GetKeyDown(interactKey) && nearWorker)
        {
            StartCoroutine(RepairCart());
            return;
        }

        if (questAccepted && !repairTaskAdded && Input.GetKeyDown(interactKey) && nearWorker)
        {
            StartConversation(workerConversation);
        }
    }

    // Function: Starts the conversation flow.
    private void StartConversation(string[] conversation)
    {
        PlayMerchantStand();
        merchantConversationRunning = true;
        conversationIndex = 0;
        activeConversation = conversation;
        merchantChoiceVisible = false;
        AdvanceConversation();
    }

    // Function: Runs the advance conversation logic.
    private void AdvanceConversation()
    {
        if (activeConversation != null && conversationIndex < activeConversation.Length)
        {
            ShowTimedMessage(activeConversation[conversationIndex], 0f, true);
            conversationIndex++;
            return;
        }

        merchantConversationRunning = false;
        timedMessageWaitsForContinue = false;
        showContinueHint = false;
        timedMessage = null;

        if (activeConversation == merchantConversation)
        {
            merchantChoiceVisible = true;
        }
        else if (activeConversation == workerConversation)
        {
            repairTaskAdded = true;
            ShowTimedMessage("Side quest updated: repair the cart.", 3f, false);
        }
        else if (activeConversation == workerRepairCompleteConversation)
        {
            CompleteRepairTask();
        }
        else if (activeConversation == merchantRewardConversation)
        {
            merchantRewardGiven = true;
            questCompleted = true;
            GlobalBackpackUI.AddItem(rewardItemName);
            ShowTimedMessage("Side quest complete: delivery checked.", 3f, false);
        }

        activeConversation = null;
    }

    // Function: Runs the accept quest logic.
    private void AcceptQuest()
    {
        questAccepted = true;
        merchantChoiceVisible = false;
        ShowQuestObjects(true);
        ShowTimedMessage("Side quest started: check the delivery.", 3f, false);
    }

    // Function: Picks up or selects repair tools.
    private void PickRepairTools()
    {
        hasRepairTools = true;
        HideToolParts();
        GlobalBackpackUI.AddItem(repairToolItemName);
        ShowTimedMessage("Repair tools found.", 3f, false);
    }

    // Function: Runs the repair cart logic.
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

    // Function: Completes repair task and applies its result or reward.
    private void CompleteRepairTask()
    {
        ShowQuestObjects(false);
        ShowTimedMessage("Side quest complete: cart repaired.", 3f, false);
    }

    // Function: Updates merchant animation state, input, or presentation.
    private void UpdateMerchantAnimation()
    {
        if (merchant == null)
        {
            return;
        }

        if (!questAccepted && !merchantConversationRunning && !merchantChoiceVisible && !nearMerchant)
        {
            PlayMerchantMove();
        }
    }

    // Function: Plays merchant move animation, audio, or cutscene behavior.
    private void PlayMerchantMove()
    {
        ApplyMerchantController(merchantMoveController);
    }

    // Function: Plays merchant stand animation, audio, or cutscene behavior.
    private void PlayMerchantStand()
    {
        ApplyMerchantController(merchantStandController);
    }

    // Function: Applies merchant controller effects to the scene or target object.
    private void ApplyMerchantController(RuntimeAnimatorController controller)
    {
        if (merchant == null || controller == null)
        {
            return;
        }

        Animator animator = merchant.GetComponent<Animator>();
        if (animator == null)
        {
            animator = merchant.gameObject.AddComponent<Animator>();
        }

        if (animator.runtimeAnimatorController != controller)
        {
            animator.runtimeAnimatorController = controller;
        }

        if (merchantAvatar != null && animator.avatar != merchantAvatar)
        {
            animator.avatar = merchantAvatar;
        }

        animator.applyRootMotion = false;
        animator.enabled = true;
    }

    // Function: Draws the UI elements for quest panel.
    private void DrawQuestPanel()
    {
        if (!questAccepted || questCompleted)
        {
            return;
        }

        float width = 420f;
        float height = repairTaskAdded ? 112f : 78f;
        Rect rect = GameUiStyle.SideQuestRect(width, height);
        GameUiStyle.DrawPanel(rect);

        GUIStyle titleStyle = GetStyle(ref questTitleStyle, 26, TextAnchor.MiddleLeft, FontStyle.Bold);
        GUIStyle itemStyle = GetStyle(ref questItemStyle, 24, TextAnchor.MiddleLeft, FontStyle.Normal);

        GUI.Label(new Rect(rect.x + 18f, rect.y + 10f, rect.width - 36f, 26f), "Side Quest", titleStyle);
        GUI.Label(new Rect(rect.x + 18f, rect.y + 42f, rect.width - 36f, 24f), "Check the merchant delivery", itemStyle);

        if (repairTaskAdded)
        {
            GUI.Label(new Rect(rect.x + 18f, rect.y + 72f, rect.width - 36f, 24f), "Repair the cart", itemStyle);
        }
    }

    // Function: Draws the UI elements for interact prompt.
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

        GUIStyle style = GetStyle(ref promptStyle, 34, TextAnchor.MiddleCenter, FontStyle.Bold);

        Rect rect = GameUiStyle.InteractionPromptRect(420f, 60f);
        GameUiStyle.DrawPanel(rect);
        string text = showToolPrompt ? "Press E to pick up" : (showRepairPrompt ? "Press E to repair" : "Press E to talk");
        GUI.Label(rect, text, style);
    }

    // Function: Draws the UI elements for timed message.
    private void DrawTimedMessage()
    {
        if (string.IsNullOrEmpty(timedMessage) || (!timedMessageWaitsForContinue && Time.time >= timedMessageEndsAt))
        {
            return;
        }

        Rect rect = GameUiStyle.DialogueRect(190f);
        GameUiStyle.DrawPanel(rect);

        GUIStyle style = GetStyle(ref messageStyle, 34, TextAnchor.MiddleCenter, FontStyle.Normal, true);

        GUI.Label(new Rect(rect.x + 28f, rect.y + 18f, rect.width - 56f, rect.height - 58f), timedMessage, style);

        if (showContinueHint)
        {
            GUIStyle hintStyle = GetStyle(ref continueHintStyle, 22, TextAnchor.MiddleRight, FontStyle.Normal);
            hintStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            GUI.Label(new Rect(rect.x + 28f, rect.y + rect.height - 40f, rect.width - 56f, 28f), "Press C to continue", hintStyle);
        }
    }

    // Function: Draws the UI elements for choice panel.
    private void DrawChoicePanel()
    {
        if (!merchantChoiceVisible)
        {
            return;
        }

        float width = Mathf.Min(680f, Screen.width - 80f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.5f - 92f, width, 184f);
        GameUiStyle.DrawPanel(rect);

        GUIStyle titleStyle = GetStyle(ref choiceTitleStyle, 32, TextAnchor.MiddleCenter, FontStyle.Bold);
        GUIStyle optionStyle = GetStyle(ref choiceOptionStyle, 28, TextAnchor.MiddleLeft, FontStyle.Normal);

        GUI.Label(new Rect(rect.x + 24f, rect.y + 16f, rect.width - 48f, 32f), "Choose", titleStyle);
        GUI.Label(new Rect(rect.x + 48f, rect.y + 70f, rect.width - 96f, 30f), "A: I will check", optionStyle);
        GUI.Label(new Rect(rect.x + 48f, rect.y + 112f, rect.width - 96f, 30f), "B: Leave", optionStyle);
    }

    // Function: Shows timed message.
    private void ShowTimedMessage(string message, float seconds, bool waitForContinue)
    {
        timedMessage = message;
        timedMessageWaitsForContinue = waitForContinue;
        showContinueHint = waitForContinue;
        timedMessageEndsAt = string.IsNullOrEmpty(message) || waitForContinue ? 0f : Time.time + seconds;
    }

    // Function: Gets or calculates style.
    private GUIStyle GetStyle(ref GUIStyle style, int fontSize, TextAnchor alignment, FontStyle fontStyle, bool wordWrap = false)
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
        }

        style.fontSize = fontSize;
        style.alignment = alignment;
        style.fontStyle = fontStyle;
        style.wordWrap = wordWrap;
        style.normal.textColor = Color.white;
        return style;
    }

    // Function: Updates nearby flags state, input, or presentation.
    private void UpdateNearFlags()
    {
        nearMerchant = IsNear(player, merchant);
        nearWorker = questAccepted && !repairTaskAdded && IsNear(player, worker);
        if (repairTaskAdded && !repairCompleted)
        {
            nearWorker = IsNear(player, worker);
        }

        nearTools = repairTaskAdded && !hasRepairTools && IsNear(player, tools);
    }

    // Function: Sets up scene objects.
    private void SetupSceneObjects()
    {
        EnsureQuestColliders();
        ShowQuestObjects(questAccepted && !repairCompleted);
        if (hasRepairTools)
        {
            HideToolParts();
        }

        PlayMerchantMove();
    }

    // Function: Shows quest objects.
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

        foreach (GameObject apple in apples)
        {
            if (apple != null)
            {
                apple.SetActive(show);
            }
        }
    }

    // Function: Ensures quest colliders exists, is configured, or is ready to use.
    private void EnsureQuestColliders()
    {
        EnsureSolidCollider(merchant);
        EnsureSolidCollider(worker);
        EnsureSolidCollider(tools);

        if (cart != null)
        {
            EnsureSolidCollider(cart.transform);
        }

        foreach (GameObject apple in apples)
        {
            if (apple != null)
            {
                EnsureSolidCollider(apple.transform);
            }
        }
    }

    // Function: Ensures solid collider exists, is configured, or is ready to use.
    private void EnsureSolidCollider(Transform target)
    {
        if (target == null || HasSolidCollider(target))
        {
            return;
        }

        AddRendererBoxColliders(target);
    }

    // Function: Checks whether solid collider already exists or is available.
    private bool HasSolidCollider(Transform target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider != null && !collider.isTrigger)
            {
                return true;
            }
        }

        return false;
    }

    // Function: Adds renderer box colliders.
    private bool AddRendererBoxColliders(Transform target)
    {
        if (!TryGetWorldBounds(target, out Bounds bounds))
        {
            return false;
        }

        BoxCollider collider = target.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = target.gameObject.AddComponent<BoxCollider>();
        }

        collider.isTrigger = false;
        collider.center = target.InverseTransformPoint(bounds.center);
        collider.size = DivideByLossyScale(bounds.size, target.lossyScale);
        return true;
    }

    // Function: Tries to get world bounds and returns whether it was found.
    private bool TryGetWorldBounds(Transform target, out Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(target.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    // Function: Safely converts by lossy scale through object scale.
    private Vector3 DivideByLossyScale(Vector3 size, Vector3 lossyScale)
    {
        return new Vector3(
            DivideByScale(size.x, lossyScale.x),
            DivideByScale(size.y, lossyScale.y),
            DivideByScale(size.z, lossyScale.z));
    }

    // Function: Safely converts by scale through object scale.
    private float DivideByScale(float value, float scale)
    {
        return Mathf.Abs(scale) > 0.0001f ? value / Mathf.Abs(scale) : value;
    }

    // Function: Hides tool parts.
    private void HideToolParts()
    {
        if (toolParts == null)
        {
            return;
        }

        for (int i = 0; i < toolParts.Length; i++)
        {
            if (toolParts[i] != null)
            {
                toolParts[i].gameObject.SetActive(false);
            }
        }
    }

    // Function: Checks whether nearby is true.
    private bool IsNear(Transform source, Transform target)
    {
        if (source == null || target == null || !target.gameObject.activeInHierarchy)
        {
            return false;
        }

        return GetClosestDistance(source.position, target) <= interactDistance;
    }

    // Function: Gets or calculates closest distance.
    private float GetClosestDistance(Vector3 point, Transform target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        float closestSqrDistance = float.PositiveInfinity;
        bool found = false;

        foreach (Collider collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            Vector3 closest = collider.ClosestPoint(point);
            float sqrDistance = (point - closest).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                found = true;
            }
        }

        if (found)
        {
            return Mathf.Sqrt(closestSqrDistance);
        }

        return Vector3.Distance(point, target.position);
    }

}
