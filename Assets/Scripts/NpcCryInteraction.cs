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
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode continueKey = KeyCode.C;

    [Header("Luna")]
    // Luna returns to this point after the first conversation.
    [SerializeField] private Transform lunaHome;
    [SerializeField] private Animator lunaAnimator;
    [SerializeField] private string prompt = "Press E to talk";
    [SerializeField] private string standParameter = "Stand";
    [SerializeField] private string standStateName = "Stand";
    [SerializeField] private float lunaFlyHeight = 1.2f;

    [Header("Witch")]
    // The witch gives the feather task and unlocks the ladder step.
    [SerializeField] private Transform witch;
    [SerializeField] private Animator witchAnimator;
    [SerializeField] private float witchInteractDistance = 6f;
    [SerializeField] private RuntimeAnimatorController witchStandController;
    [SerializeField] private string witchStandStateName = "mixamo_com";

    [Header("Feathers")]
    // Feathers are collected only for the active side quest and are removed after the witch accepts them.
    [SerializeField] private Transform[] feathers;
    [SerializeField] private string featherItemName = "Feather";
    [SerializeField] private float featherPickupDistance = 6f;
    [SerializeField] private Color featherHighlightColor = new Color(1f, 0.92f, 0.2f, 1f);

    [Header("Ladder And Key")]
    // Ladder and key are hidden until the feather step is complete.
    [SerializeField] private Transform ladder;
    [SerializeField] private Transform climbTarget;
    [SerializeField] private Transform keyObject;
    [SerializeField] private string keyItemName = "Key";
    [SerializeField] private float ladderInteractDistance = 4f;
    [SerializeField] private float keyPickupDistance = 6f;
    [SerializeField] private float climbDuration = 2.2f;

    [Header("Reward And Portal")]
    // The fourth page reward opens the scene transition portal.
    [SerializeField] private string fourthPageItemName = "Fourth Page";
    [SerializeField] private Transform portal;
    [SerializeField] private string nextSceneName = "11 1";
    [SerializeField] private float portalInteractDistance = 4f;


    private readonly string[] lunaIntroLines =
    {
        "Luna: I lost my house key.",
        "Player: How can I help?",
        "Luna: Please ask the forest witch.",
        "Player: I will help.",
        "Luna: Thank you."
    };

    private readonly string[] witchFirstLines =
    {
        "Witch: I can help, but bring me four feathers first.",
        "Player: I will find them."
    };

    private readonly string[] witchNeedFeathersLines =
    {
        "Witch: Did you bring the feathers?"
    };

    private readonly string[] witchCompleteLines =
    {
        "Witch: These feathers look familiar.",
        "Player: Maybe the key was taken by a bird.",
        "Witch: The ladder is ready. Check the nest.",
        "Player: Thank you."
    };

    private readonly string[] lunaCompleteLines =
    {
        "Player: I found your key in the nest.",
        "Luna: Thank you. Please take this fourth magic page."
    };

    private readonly string[] keyDiscoveryLines =
    {
        "Player: The key really was in the nest."
    };

    private Material highlightMaterial;
    private readonly Dictionary<Renderer, Material[]> originalFeatherMaterials = new Dictionary<Renderer, Material[]>();
    private string[] dialogueLines;
    private int dialogueIndex;
    private Action dialogueComplete;
    private QuestState state = QuestState.NotStarted;
    private bool[] featherCollected;
    private bool isDialogueOpen;
    private bool isClimbing;
    private bool keyDiscoveryDialogueShown;
    private GUIStyle dialogueStyle;
    private GUIStyle hintStyle;
    private GUIStyle promptStyle;
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
        Rect rect = GameUiStyle.DialogueRect(220f);
        GameUiStyle.DrawDialoguePanel(rect);

        GUI.Label(
            new Rect(rect.x + 180f, rect.y + 118f, rect.width - 252f, rect.height - 190f),
            text,
            GameUiStyle.LabelStyle(ref dialogueStyle, 30, TextAnchor.UpperLeft, FontStyle.Normal, true));

        GUI.Label(
            new Rect(rect.x + 180f, rect.yMax - 86f, rect.width - 252f, 48f),
            "Press C to continue",
            GameUiStyle.LabelStyle(ref hintStyle, 22, TextAnchor.MiddleRight));
    }

    private void DrawPrompt(string text)
    {
        Rect rect = GameUiStyle.InteractionPromptRect();
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(rect, text, GameUiStyle.LabelStyle(ref promptStyle, 28, TextAnchor.MiddleCenter));
    }

    private void DrawFeatherProgress()
    {
        // Right-side quest tracker for the feather collection step.
        const float panelWidth = 640f;
        Rect rect = GameUiStyle.SideQuestRect(panelWidth, 230f);
        GameUiStyle.DrawDialoguePanel(rect);
        int collectedCount = CollectedFeatherCount();
        float textX = rect.x + 156f;
        float textWidth = 448f;
        float textY = rect.y + 8f;

        GUI.Label(
            new Rect(textX, textY + 58f, textWidth, 48f),
            "Side Quest",
            GameUiStyle.LabelStyle(ref titleStyle, 26, TextAnchor.MiddleLeft, FontStyle.Bold));

        GUI.Label(
            new Rect(textX, textY + 112f, textWidth, 48f),
            "Find feathers",
            GameUiStyle.LabelStyle(ref hintStyle, 24, TextAnchor.MiddleLeft, FontStyle.Bold));

        GUI.Label(
            new Rect(textX + 38f, textY + 146f, 300f, 54f),
            collectedCount + "/" + FeatherCount,
            GameUiStyle.LabelStyle(ref promptStyle, 32, TextAnchor.MiddleRight, FontStyle.Bold));
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

    private int FeatherCount => feathers != null ? feathers.Length : 0;

}
