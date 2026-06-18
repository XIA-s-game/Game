using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NpcCryInteraction : MonoBehaviour
{
    // Tracks Luna's side quest from the first cry interaction through the portal reward.
    private enum QuestState
    {
        NotStarted,
        LunaIntro,
        GoAskWitch,
        NeedFeathers,
        CollectingFeathers,
        ReturnToWitch,
        NeedClimbLadder,
        NeedKey,
        ReturnToLuna,
        Complete
    }

    [Header("Player")]
    // Player reference used for all distance checks and ladder movement.
    [SerializeField] private Transform player;
    // Default interaction distance for Luna.
    [SerializeField] private float interactDistance = 3f;
    // Main interaction key.
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    // Dialogue continue key.
    [SerializeField] private KeyCode continueKey = KeyCode.C;

    [Header("Luna")]
    // Luna returns to this point after the first conversation.
    [SerializeField] private Transform lunaHome;
    // Animator used to make Luna stand/fly.
    [SerializeField] private Animator lunaAnimator;
    // Talk prompt near Luna.
    [SerializeField] private string prompt = "Press E to talk";
    // Animator parameter for Luna's stand state.
    [SerializeField] private string standParameter = "Stand";
    // Animator state name for Luna standing.
    [SerializeField] private string standStateName = "Stand";
    // Height offset while Luna floats home.
    [SerializeField] private float lunaFlyHeight = 1.2f;

    [Header("Witch")]
    // The witch gives the feather task and unlocks the ladder step.
    [SerializeField] private Transform witch;
    // Witch animator.
    [SerializeField] private Animator witchAnimator;
    // Interaction distance for the witch.
    [SerializeField] private float witchInteractDistance = 6f;
    // Controller used to switch the witch to a standing pose.
    [SerializeField] private RuntimeAnimatorController witchStandController;
    // State name inside the witch stand controller.
    [SerializeField] private string witchStandStateName = "mixamo_com";

    [Header("Feathers")]
    // Feathers are collected only for the active side quest and are removed after the witch accepts them.
    [SerializeField] private Transform[] feathers;
    // Backpack item name for feathers.
    [SerializeField] private string featherItemName = "Feather";
    // Pickup distance for feather objects.
    [SerializeField] private float featherPickupDistance = 6f;
    // Material color used to highlight active feathers.
    [SerializeField] private Color featherHighlightColor = new Color(1f, 0.92f, 0.2f, 1f);

    [Header("Ladder And Key")]
    // Ladder and key are hidden until the feather step is complete.
    [SerializeField] private Transform ladder;
    // Position the player moves to after climbing.
    [SerializeField] private Transform climbTarget;
    // Key object in the nest.
    [SerializeField] private Transform keyObject;
    // Backpack item name for Luna's key.
    [SerializeField] private string keyItemName = "Key";
    // Distance needed to use the ladder.
    [SerializeField] private float ladderInteractDistance = 4f;
    // Distance needed to pick up the key.
    [SerializeField] private float keyPickupDistance = 6f;
    // Time used for the ladder movement.
    [SerializeField] private float climbDuration = 2.2f;

    [Header("Reward And Portal")]
    // The fourth page reward opens the scene transition portal.
    [SerializeField] private string fourthPageItemName = "Fourth Page";
    // Portal to the next scene.
    [SerializeField] private Transform portal;
    // Scene loaded after the portal interaction.
    [SerializeField] private string nextSceneName = "Chapter5_MoonlitGlade";
    // Distance needed to use the portal.
    [SerializeField] private float portalInteractDistance = 4f;

    [Header("Side Quest UI")]
    // Side quest tracker size.
    [SerializeField] private Vector2 sideQuestPanelSize = new Vector2(640f, 230f);
    // Origin for side quest text inside the panel.
    [SerializeField] private Vector2 sideQuestTextOrigin = new Vector2(156f, 8f);
    // Width for side quest text.
    [SerializeField] private float sideQuestTextWidth = 448f;
    // Title rect inside the side quest panel.
    [SerializeField] private Rect sideQuestTitleRect = new Rect(0f, 58f, 448f, 48f);
    // Task rect inside the side quest panel.
    [SerializeField] private Rect sideQuestTaskRect = new Rect(0f, 112f, 448f, 48f);
    // Feather count rect inside the side quest panel.
    [SerializeField] private Rect sideQuestCountRect = new Rect(38f, 146f, 300f, 54f);
    // Side quest title font size.
    [SerializeField] private int sideQuestTitleFontSize = 26;
    // Side quest task font size.
    [SerializeField] private int sideQuestTaskFontSize = 24;
    // Feather count font size.
    [SerializeField] private int sideQuestCountFontSize = 32;

    [Header("Dialogue UI")]
    // Bottom dialogue panel height.
    [SerializeField] private float dialoguePanelHeight = 220f;
    // Dialogue body rect.
    [SerializeField] private Rect dialogueTextRect = new Rect(180f, 118f, -252f, -190f);
    // Continue hint rect.
    [SerializeField] private Rect dialogueContinueRect = new Rect(180f, -86f, -252f, 48f);
    // Dialogue font size.
    [SerializeField] private int dialogueFontSize = 30;
    // Continue hint font size.
    [SerializeField] private int dialogueContinueFontSize = 22;
    // Interaction prompt size.
    [SerializeField] private Vector2 promptSize = new Vector2(440f, 60f);
    // Interaction prompt font size.
    [SerializeField] private int promptFontSize = 28;

    // First conversation with Luna.
    private readonly string[] lunaIntroLines =
    {
        "Luna: I lost my house key.",
        "Casper: How can I help?",
        "Luna: Please ask the forest witch.",
        "Casper: I will help.",
        "Luna: Thank you."
    };

    // First witch conversation.
    private readonly string[] witchFirstLines =
    {
        "Witch: I can help, but bring me four feathers first.",
        "Casper: I will find them."
    };

    // Witch reminder while feathers are missing.
    private readonly string[] witchNeedFeathersLines =
    {
        "Witch: Did you bring the feathers?"
    };

    // Witch dialogue after all feathers are collected.
    private readonly string[] witchCompleteLines =
    {
        "Witch: These feathers look familiar.",
        "Casper: Maybe the key was taken by a bird.",
        "Witch: The ladder is ready. Check the nest.",
        "Casper: Thank you."
    };

    // Luna reward dialogue.
    private readonly string[] lunaCompleteLines =
    {
        "Casper: I found your key in the nest.",
        "Luna: Thank you. Please take this fourth magic page."
    };

    // Short line after the key is found.
    private readonly string[] keyDiscoveryLines =
    {
        "Casper: The key really was in the nest."
    };

    // Shared highlight material for active feathers.
    private Material highlightMaterial;
    // Original feather materials, restored after collection.
    private readonly Dictionary<Renderer, Material[]> originalFeatherMaterials = new Dictionary<Renderer, Material[]>();
    // Dialogue lines currently being shown.
    private string[] dialogueLines;
    // Current dialogue line index.
    private int dialogueIndex;
    // Callback after dialogue closes.
    private Action dialogueComplete;
    // Current Luna quest state.
    private QuestState state = QuestState.NotStarted;
    // Per-feather collection flags.
    private bool[] featherCollected;
    // True while dialogue is open.
    private bool isDialogueOpen;
    // True while the ladder movement is running.
    private bool isClimbing;
    // Stops the key discovery line repeating.
    private bool keyDiscoveryDialogueShown;
    // Cached dialogue text style.
    private GUIStyle dialogueStyle;
    // Cached continue hint style.
    private GUIStyle hintStyle;
    // Cached prompt style.
    private GUIStyle promptStyle;
    // Cached side quest title style.
    private GUIStyle titleStyle;

    private void Awake()
    {
        if (player == null)
        {
            Debug.LogWarning("NpcCryInteraction is missing Player reference.", this);
        }
    }

    private void Start()
    {
        featherCollected = new bool[feathers != null ? feathers.Length : 0];
        SetLadderVisible(false);
        SetPortalVisible(false);
        SetFeatherHighlights(false);
        PlayWitchStand();
    }

    private void Update()
    {
        if (isDialogueOpen)
        {
            if (Input.GetKeyDown(continueKey))
            {
                ShowNextDialogueLine();
            }

            return;
        }

        if (player == null || isClimbing)
        {
            return;
        }

        UpdateInteractions();
    }

    private void UpdateInteractions()
    {
        // Handles the current quest step in one place so the side quest has a clear order.
        if (state == QuestState.NotStarted && IsNear(transform, interactDistance) && Input.GetKeyDown(interactKey))
        {
            StartLunaIntro();
            return;
        }

        if ((state == QuestState.GoAskWitch || state == QuestState.NeedFeathers || state == QuestState.CollectingFeathers || state == QuestState.ReturnToWitch) &&
            IsNear(witch, witchInteractDistance) && Input.GetKeyDown(interactKey))
        {
            TalkToWitch();
            return;
        }

        if ((state == QuestState.NeedFeathers || state == QuestState.CollectingFeathers) && TryGetNearbyFeather(out int featherIndex) && Input.GetKeyDown(interactKey))
        {
            CollectFeather(featherIndex);
            return;
        }

        if (state == QuestState.NeedClimbLadder && IsNear(ladder, ladderInteractDistance) && Input.GetKeyDown(interactKey))
        {
            StartCoroutine(ClimbLadder());
            return;
        }

        if (state == QuestState.NeedKey && IsNearKey())
        {
            if (!keyDiscoveryDialogueShown)
            {
                keyDiscoveryDialogueShown = true;
                StartDialogue(keyDiscoveryLines, null);
                return;
            }

            if (Input.GetKeyDown(interactKey))
            {
                PickUpKey();
                return;
            }
        }

        if (state == QuestState.ReturnToLuna && IsNear(transform, interactDistance) && Input.GetKeyDown(interactKey))
        {
            StartDialogue(lunaCompleteLines, CompleteQuest);
            return;
        }

        if (state == QuestState.Complete && IsNear(portal, portalInteractDistance) && Input.GetKeyDown(interactKey))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void StartLunaIntro()
    {
        // First Luna conversation sends the player to the witch.
        SetLunaStand();
        state = QuestState.LunaIntro;
        StartDialogue(lunaIntroLines, () =>
        {
            state = QuestState.GoAskWitch;
            MoveLunaHome();
        });
    }

    private void TalkToWitch()
    {
        // Witch dialogue changes depending on feather progress.
        if (state == QuestState.GoAskWitch)
        {
            PlayWitchStand();
            StartDialogue(witchFirstLines, () =>
            {
                state = QuestState.NeedFeathers;
                SetFeatherHighlights(true);
            });
            return;
        }

        if (CollectedFeatherCount() < FeatherCount)
        {
            StartDialogue(witchNeedFeathersLines, () =>
            {
                state = QuestState.CollectingFeathers;
                SetFeatherHighlights(true);
            });
            return;
        }

        StartDialogue(witchCompleteLines, () =>
        {
            GlobalBackpackUI.RemoveAll(featherItemName);
            SetFeatherHighlights(false);
            SetLadderVisible(true);
            state = QuestState.NeedClimbLadder;
        });
    }

    private void SetLunaStand()
    {
        if (lunaAnimator == null || !lunaAnimator.gameObject.activeInHierarchy || lunaAnimator.runtimeAnimatorController == null)
        {
            return;
        }

        lunaAnimator.SetBool(standParameter, true);
        if (!string.IsNullOrEmpty(standStateName))
        {
            lunaAnimator.CrossFade(standStateName, 0.15f);
        }
    }

    private void MoveLunaHome()
    {
        if (lunaHome == null)
        {
            return;
        }

        transform.position = lunaHome.position + Vector3.up * lunaFlyHeight;
    }

    private void PlayWitchStand()
    {
        if (witch == null)
        {
            return;
        }

        if (witchAnimator == null)
        {
            return;
        }

        witchAnimator.applyRootMotion = false;

        if (witchStandController != null)
        {
            witchAnimator.runtimeAnimatorController = witchStandController;
        }

        if (!string.IsNullOrEmpty(witchStandStateName))
        {
            witchAnimator.Play(witchStandStateName);
        }
    }

    private bool TryGetNearbyFeather(out int featherIndex)
    {
        // Checks object bounds as well as transform distance so larger feather models are easier to pick up.
        featherIndex = -1;
        if (feathers == null || featherCollected == null)
        {
            return false;
        }

        for (int i = 0; i < feathers.Length; i++)
        {
            if (featherCollected[i] || feathers[i] == null || !feathers[i].gameObject.activeInHierarchy)
            {
                continue;
            }

            if (IsNearObjectBounds(feathers[i], featherPickupDistance))
            {
                featherIndex = i;
                return true;
            }
        }

        return false;
    }

    private void CollectFeather(int featherIndex)
    {
        if (featherIndex < 0 || featherIndex >= feathers.Length || feathers[featherIndex] == null)
        {
            return;
        }

        featherCollected[featherIndex] = true;
        feathers[featherIndex].gameObject.SetActive(false);
        GlobalBackpackUI.AddItem(featherItemName);

        if (CollectedFeatherCount() >= FeatherCount)
        {
            state = QuestState.ReturnToWitch;
        }
        else
        {
            state = QuestState.CollectingFeathers;
        }
    }

    private int CollectedFeatherCount()
    {
        int count = 0;
        if (featherCollected == null)
        {
            return count;
        }

        for (int i = 0; i < featherCollected.Length; i++)
        {
            if (featherCollected[i])
            {
                count++;
            }
        }

        return count;
    }

    private IEnumerator ClimbLadder()
    {
        // Moves the player to the nest target before enabling the key pickup step.
        isClimbing = true;

        Vector3 start = player.position;
        Vector3 target = climbTarget != null ? climbTarget.position : player.position;
        float elapsed = 0f;

        while (elapsed < climbDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / climbDuration);
            player.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        player.position = target;
        state = QuestState.NeedKey;
        isClimbing = false;
    }

    private void PickUpKey()
    {
        if (keyObject != null)
        {
            keyObject.gameObject.SetActive(false);
        }

        GlobalBackpackUI.AddItem(keyItemName);
        state = QuestState.ReturnToLuna;
    }

    private bool IsNearKey()
    {
        return keyObject != null &&
               keyObject.gameObject.activeInHierarchy &&
               IsNearObjectBounds(keyObject, keyPickupDistance);
    }

    private void CompleteQuest()
    {
        // Consumes the temporary key and gives the final page for this side quest.
        GlobalBackpackUI.RemoveAll(keyItemName);
        GlobalBackpackUI.AddItem(fourthPageItemName);
        SetPortalVisible(true);
        state = QuestState.Complete;
    }

    private void SetPortalVisible(bool visible)
    {
        if (portal != null)
        {
            portal.gameObject.SetActive(visible);
        }
    }

    private void SetLadderVisible(bool visible)
    {
        if (ladder != null)
        {
            ladder.gameObject.SetActive(visible);
        }
    }

    private void SetFeatherHighlights(bool highlighted)
    {
        // Swaps feather materials during the search step, then restores their original materials.
        if (feathers == null)
        {
            return;
        }

        if (highlighted && highlightMaterial == null)
        {
            highlightMaterial = new Material(Shader.Find("Standard"));
            highlightMaterial.color = featherHighlightColor;
        }

        for (int i = 0; i < feathers.Length; i++)
        {
            if (feathers[i] == null || featherCollected != null && featherCollected[i])
            {
                continue;
            }

            Renderer[] renderers = feathers[i].GetComponentsInChildren<Renderer>();
            for (int j = 0; j < renderers.Length; j++)
            {
                Renderer renderer = renderers[j];
                if (renderer == null)
                {
                    continue;
                }

                if (!originalFeatherMaterials.ContainsKey(renderer))
                {
                    originalFeatherMaterials.Add(renderer, renderer.sharedMaterials);
                }

                if (highlighted)
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        materials[materialIndex] = highlightMaterial;
                    }

                    renderer.sharedMaterials = materials;
                }
                else if (originalFeatherMaterials.TryGetValue(renderer, out Material[] originalMaterials))
                {
                    renderer.sharedMaterials = originalMaterials;
                }
            }
        }
    }

    private void StartDialogue(string[] lines, Action onComplete)
    {
        // Opens a blocking dialogue sequence and stores the action that should run after the last line.
        dialogueLines = lines;
        dialogueIndex = 0;
        dialogueComplete = onComplete;
        isDialogueOpen = true;
    }

    private void ShowNextDialogueLine()
    {
        dialogueIndex++;
        if (dialogueLines != null && dialogueIndex < dialogueLines.Length)
        {
            return;
        }

        Action complete = dialogueComplete;
        dialogueLines = null;
        dialogueComplete = null;
        dialogueIndex = 0;
        isDialogueOpen = false;
        complete?.Invoke();
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        if (isDialogueOpen)
        {
            DrawDialogue();
            return;
        }

        string promptText = GetPromptText();
        if (!string.IsNullOrEmpty(promptText))
        {
            DrawPrompt(promptText);
        }

        if (state == QuestState.NeedFeathers || state == QuestState.CollectingFeathers || state == QuestState.ReturnToWitch)
        {
            DrawFeatherProgress();
        }
    }

    private string GetPromptText()
    {
        if (player == null || isClimbing)
        {
            return null;
        }

        if (state == QuestState.NotStarted && IsNear(transform, interactDistance))
        {
            return prompt;
        }

        if ((state == QuestState.GoAskWitch || state == QuestState.NeedFeathers || state == QuestState.CollectingFeathers || state == QuestState.ReturnToWitch) &&
            IsNear(witch, witchInteractDistance))
        {
            return "Press E to talk";
        }

        if ((state == QuestState.NeedFeathers || state == QuestState.CollectingFeathers) && TryGetNearbyFeather(out _))
        {
            return "Press E to pick up";
        }

        if (state == QuestState.NeedClimbLadder && IsNear(ladder, ladderInteractDistance))
        {
            return "Press E to climb";
        }

        if (state == QuestState.NeedKey && keyDiscoveryDialogueShown && IsNearKey())
        {
            return "Press E to pick up";
        }

        if (state == QuestState.ReturnToLuna && IsNear(transform, interactDistance))
        {
            return "Press E to interact";
        }

        if (state == QuestState.Complete && IsNear(portal, portalInteractDistance))
        {
            return "Press E to travel";
        }

        return null;
    }

    private void DrawDialogue()
    {
        string text = dialogueLines != null && dialogueIndex < dialogueLines.Length ? dialogueLines[dialogueIndex] : string.Empty;
        Rect rect = GameUiStyle.DialogueRect(dialoguePanelHeight);
        GameUiStyle.DrawDialoguePanel(rect);

        GUI.Label(
            InnerRect(rect, dialogueTextRect),
            text,
            GameUiStyle.LabelStyle(ref dialogueStyle, dialogueFontSize, TextAnchor.UpperLeft, FontStyle.Normal, true));

        GUI.Label(
            InnerRect(rect, dialogueContinueRect),
            "Press C to continue",
            GameUiStyle.LabelStyle(ref hintStyle, dialogueContinueFontSize, TextAnchor.MiddleRight));
    }

    private void DrawPrompt(string text)
    {
        Rect rect = GameUiStyle.InteractionPromptRect(promptSize.x, promptSize.y);
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(rect, text, GameUiStyle.LabelStyle(ref promptStyle, promptFontSize, TextAnchor.MiddleCenter));
    }

    private void DrawFeatherProgress()
    {
        // Right-side quest tracker for the feather collection step.
        Rect rect = GameUiStyle.SideQuestRect(sideQuestPanelSize.x, sideQuestPanelSize.y);
        GameUiStyle.DrawDialoguePanel(rect);
        int collectedCount = CollectedFeatherCount();
        float textX = rect.x + sideQuestTextOrigin.x;
        float textY = rect.y + sideQuestTextOrigin.y;

        GUI.Label(
            OffsetRect(sideQuestTitleRect, textX, textY, sideQuestTextWidth),
            "Side Quest",
            GameUiStyle.LabelStyle(ref titleStyle, sideQuestTitleFontSize, TextAnchor.MiddleLeft, FontStyle.Bold));

        GUI.Label(
            OffsetRect(sideQuestTaskRect, textX, textY, sideQuestTextWidth),
            "Find feathers",
            GameUiStyle.LabelStyle(ref hintStyle, sideQuestTaskFontSize, TextAnchor.MiddleLeft, FontStyle.Bold));

        GUI.Label(
            OffsetRect(sideQuestCountRect, textX, textY, sideQuestCountRect.width),
            collectedCount + "/" + FeatherCount,
            GameUiStyle.LabelStyle(ref promptStyle, sideQuestCountFontSize, TextAnchor.MiddleRight, FontStyle.Bold));
    }

    private static Rect OffsetRect(Rect localRect, float originX, float originY, float fallbackWidth)
    {
        float width = localRect.width > 0f ? localRect.width : fallbackWidth;
        return new Rect(originX + localRect.x, originY + localRect.y, width, localRect.height);
    }

    private static Rect InnerRect(Rect parent, Rect localRect)
    {
        float y = localRect.y >= 0f ? parent.y + localRect.y : parent.yMax + localRect.y;
        float width = localRect.width >= 0f ? localRect.width : parent.width + localRect.width;
        float height = localRect.height >= 0f ? localRect.height : parent.height + localRect.height;
        return new Rect(parent.x + localRect.x, y, width, height);
    }

    private bool IsNear(Transform target, float distance)
    {
        if (target == null || player == null)
        {
            return false;
        }

        Vector3 targetPosition = target.position;
        Vector3 playerPosition = player.position;
        targetPosition.y = 0f;
        playerPosition.y = 0f;
        return Vector3.Distance(targetPosition, playerPosition) <= distance;
    }

    private bool IsNearObjectBounds(Transform target, float distance)
    {
        if (target == null || player == null)
        {
            return false;
        }

        if (IsNear(target, distance))
        {
            return true;
        }

        Vector3 playerPosition = player.position;
        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            if (HorizontalDistanceToBounds(collider.bounds, playerPosition) <= distance)
            {
                return true;
            }
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (HorizontalDistanceToBounds(renderer.bounds, playerPosition) <= distance)
            {
                return true;
            }
        }

        return false;
    }

    private float HorizontalDistanceToBounds(Bounds bounds, Vector3 position)
    {
        Vector3 closestPoint = bounds.ClosestPoint(position);
        closestPoint.y = 0f;
        position.y = 0f;
        return Vector3.Distance(closestPoint, position);
    }

    // Number of feather objects assigned in the scene.
    private int FeatherCount => feathers != null ? feathers.Length : 0;

}
