// Runs Luna and the witch feather side quest, including ladder, key, and portal steps.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NpcCryInteraction : MonoBehaviour
{
    private const string LegacyWitchName = "Peasant Girl";
    private const string CurrentWitchName = "Snake Hip Hop Dance";

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
    [SerializeField] private string playerName = "AQM_FPS_Character";
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode continueKey = KeyCode.C;

    [Header("Luna")]
    [SerializeField] private string lunaHomeName = "ln";
    [SerializeField] private string prompt = "Press E to talk";
    [SerializeField] private string standParameter = "Stand";
    [SerializeField] private string standStateName = "Stand";
    [SerializeField] private float lunaFlyHeight = 1.2f;

    [Header("Witch")]
    [SerializeField] private string witchName = "Snake Hip Hop Dance";
    [SerializeField] private float witchInteractDistance = 6f;
    [SerializeField] private RuntimeAnimatorController witchStandController;
    [SerializeField] private string witchStandControllerPath = "Assets/animator/stand.controller";
    [SerializeField] private string witchAvatarPath = "Assets/character/Snake Hip Hop Dance.fbx";
    [SerializeField] private string witchStandStateName = "mixamo_com";

    [Header("Feathers")]
    [SerializeField] private string[] featherNames = { "feather_only", "feather (1)", "feather (2)", "feather (3)" };
    [SerializeField] private string featherItemName = "Feather";
    [SerializeField] private float featherPickupDistance = 6f;
    [SerializeField] private Color featherHighlightColor = new Color(1f, 0.92f, 0.2f, 1f);

    [Header("Ladder And Key")]
    [SerializeField] private string ladderName = "Muddy_Ladder_FBX";
    [SerializeField] private string climbTargetName = "tz1";
    [SerializeField] private string keyName = "Key OBJ";
    [SerializeField] private string keyItemName = "Key";
    [SerializeField] private float ladderInteractDistance = 4f;
    [SerializeField] private float keyPickupDistance = 6f;
    [SerializeField] private float climbDuration = 2.2f;

    [Header("Reward And Portal")]
    [SerializeField] private string fourthPageItemName = "Fourth Page";
    [SerializeField] private string portalName = "Portal_01";
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

    private Animator animator;
    private Transform player;
    private Transform lunaHome;
    private Transform witch;
    private Animator witchAnimator;
    private Transform ladder;
    private Transform climbTarget;
    private Transform keyObject;
    private Transform portal;
    private Transform[] feathers;
    private Material highlightMaterial;
    private readonly Dictionary<Renderer, Material[]> originalFeatherMaterials = new Dictionary<Renderer, Material[]>();
    private string[] dialogueLines;
    private int dialogueIndex;
    private Action dialogueComplete;
    private QuestState state = QuestState.NotStarted;
    private bool[] featherCollected;
    private bool isDialogueOpen;
    private bool isClimbing;
    private bool witchHasStoppedDancing;
    private bool keyDiscoveryDialogueShown;
    private GUIStyle dialogueStyle;
    private GUIStyle hintStyle;
    private GUIStyle promptStyle;

    private void Awake()
    {
        if (witchName == LegacyWitchName)
        {
            witchName = CurrentWitchName;
        }

        animator = GetComponent<Animator>();
        LoadEditorControllers();
        FindSceneObjects();
    }

    private void Start()
    {
        featherCollected = new bool[featherNames.Length];
        SetLadderVisible(false);
        SetPortalVisible(false);
        SetFeatherHighlights(false);
        witchHasStoppedDancing = true;
        PlayWitchStand();
    }

    private void Update()
    {
        if (player == null || witch == null || lunaHome == null || FeathersNeedRefresh())
        {
            FindSceneObjects();
        }

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

    private void FindSceneObjects()
    {
        // This quest is dropped into a busy scene, so it finds its pieces by name at runtime.
        bool featherReferencesChanged = false;

        GameObject playerObject = FindSceneGameObject(playerName);
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        GameObject lunaHomeObject = FindSceneGameObject(lunaHomeName);
        if (lunaHomeObject != null)
        {
            lunaHome = lunaHomeObject.transform;
        }

        GameObject witchObject = FindSceneGameObject(witchName);
        if (witchObject != null)
        {
            witch = witchObject.transform;
            EnsureWitchAnimator();
            PlayWitchStand();
        }

        GameObject ladderObject = FindSceneGameObject(ladderName);
        if (ladderObject != null)
        {
            ladder = ladderObject.transform;
        }

        GameObject climbTargetObject = FindSceneGameObject(climbTargetName);
        if (climbTargetObject != null)
        {
            climbTarget = climbTargetObject.transform;
        }

        GameObject keyGameObject = FindSceneGameObject(keyName);
        if (keyGameObject != null)
        {
            keyObject = keyGameObject.transform;
        }

        GameObject portalObject = FindSceneGameObject(portalName);
        if (portalObject != null)
        {
            portal = portalObject.transform;
        }

        if (feathers == null || feathers.Length != featherNames.Length)
        {
            feathers = new Transform[featherNames.Length];
        }

        for (int i = 0; i < featherNames.Length; i++)
        {
            if (feathers[i] != null && feathers[i].gameObject != null)
            {
                continue;
            }

            GameObject featherObject = FindSceneGameObject(featherNames[i]);
            if (featherObject != null)
            {
                feathers[i] = featherObject.transform;
                featherReferencesChanged = true;
            }
        }

        if (featherReferencesChanged && IsFeatherQuestActive())
        {
            SetFeatherHighlights(true);
        }
    }

    private bool FeathersNeedRefresh()
    {
        if (feathers == null || feathers.Length != featherNames.Length)
        {
            return true;
        }

        for (int i = 0; i < feathers.Length; i++)
        {
            if (feathers[i] == null)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsFeatherQuestActive()
    {
        return state == QuestState.NeedFeathers ||
               state == QuestState.CollectingFeathers ||
               state == QuestState.ReturnToWitch;
    }

    private void UpdateInteractions()
    {
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
        if (state == QuestState.GoAskWitch)
        {
            PlayWitchStand();
            witchHasStoppedDancing = true;
            StartDialogue(witchFirstLines, () =>
            {
                state = QuestState.NeedFeathers;
                SetFeatherHighlights(true);
            });
            return;
        }

        if (CollectedFeatherCount() < featherNames.Length)
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
        if (animator == null)
        {
            return;
        }

        animator.SetBool(standParameter, true);
        if (!string.IsNullOrEmpty(standStateName))
        {
            animator.CrossFade(standStateName, 0.15f);
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
        EnsureWitchAnimator();
        if (witchAnimator == null)
        {
            return;
        }

        if (witchStandController != null)
        {
            witchAnimator.runtimeAnimatorController = witchStandController;
        }

        if (!string.IsNullOrEmpty(witchStandStateName))
        {
            witchAnimator.Play(witchStandStateName);
        }
    }

    private void EnsureWitchAnimator()
    {
        if (witch == null)
        {
            return;
        }

        if (witchAnimator == null)
        {
            witchAnimator = witch.GetComponentInChildren<Animator>();
        }

        if (witchAnimator == null)
        {
            witchAnimator = witch.gameObject.AddComponent<Animator>();
        }

        witchAnimator.applyRootMotion = false;

#if UNITY_EDITOR
        if (witchAnimator.avatar == null)
        {
            UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(witchAvatarPath);
            foreach (UnityEngine.Object asset in assets)
            {
                Avatar avatar = asset as Avatar;
                if (avatar != null)
                {
                    witchAnimator.avatar = avatar;
                    break;
                }
            }
        }
#endif
    }

    private bool TryGetNearbyFeather(out int featherIndex)
    {
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

        if (CollectedFeatherCount() >= featherNames.Length)
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
        isClimbing = true;

        Vector3 start = player.position;
        if (climbTarget == null)
        {
            GameObject climbTargetObject = FindSceneGameObject(climbTargetName);
            if (climbTargetObject != null)
            {
                climbTarget = climbTargetObject.transform;
            }
        }

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
        if (keyObject == null)
        {
            GameObject keyGameObject = FindSceneGameObject(keyName);
            if (keyGameObject != null)
            {
                keyObject = keyGameObject.transform;
            }
        }

        return keyObject != null &&
               keyObject.gameObject.activeInHierarchy &&
               IsNearObjectBounds(keyObject, keyPickupDistance);
    }

    private void CompleteQuest()
    {
        GlobalBackpackUI.RemoveAll(keyItemName);
        GlobalBackpackUI.AddItem(fourthPageItemName);
        SetPortalVisible(true);
        state = QuestState.Complete;
    }

    private void SetPortalVisible(bool visible)
    {
        if (portal == null)
        {
            GameObject portalObject = FindSceneGameObject(portalName);
            if (portalObject != null)
            {
                portal = portalObject.transform;
            }
        }

        if (portal != null)
        {
            portal.gameObject.SetActive(visible);
        }
    }

    private void SetLadderVisible(bool visible)
    {
        if (ladder == null)
        {
            GameObject ladderObject = FindSceneGameObject(ladderName);
            if (ladderObject != null)
            {
                ladder = ladderObject.transform;
            }
        }

        if (ladder != null)
        {
            ladder.gameObject.SetActive(visible);
        }
    }

    private void SetFeatherHighlights(bool highlighted)
    {
        if (feathers == null)
        {
            return;
        }

        EnsureHighlightMaterial();
        for (int i = 0; i < feathers.Length; i++)
        {
            if (feathers[i] == null || featherCollected != null && featherCollected[i])
            {
                continue;
            }

            ApplyFeatherHighlight(feathers[i], highlighted);
        }

    }

    private void ApplyFeatherHighlight(Transform featherTransform, bool highlighted)
    {
        if (featherTransform == null)
        {
            return;
        }

        Renderer[] renderers = featherTransform.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
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

    private void EnsureHighlightMaterial()
    {
        if (highlightMaterial != null)
        {
            return;
        }

        highlightMaterial = new Material(Shader.Find("Standard"));
        highlightMaterial.color = featherHighlightColor;
    }

    private void StartDialogue(string[] lines, Action onComplete)
    {
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
        Rect rect = GameUiStyle.DialogueRect(210f);
        GameUiStyle.DrawPanel(rect);

        GUI.Label(new Rect(rect.x + 24f, rect.y + 22f, rect.width - 48f, rect.height - 70f), text, GameUiStyle.LabelStyle(ref dialogueStyle, 30, TextAnchor.UpperLeft, FontStyle.Normal, true));
        GUI.Label(new Rect(rect.x + 24f, rect.yMax - 46f, rect.width - 48f, 28f), "Press C to continue", GameUiStyle.LabelStyle(ref hintStyle, 22, TextAnchor.MiddleRight));
    }

    private void DrawPrompt(string text)
    {
        Rect rect = GameUiStyle.InteractionPromptRect();
        GameUiStyle.DrawPanel(rect);
        GUI.Label(rect, text, GameUiStyle.LabelStyle(ref promptStyle, 28, TextAnchor.MiddleCenter));
    }

    private void DrawFeatherProgress()
    {
        Rect rect = GameUiStyle.SideQuestRect(320f, 86f);
        GameUiStyle.DrawPanel(rect);
        GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, rect.width - 32f, 28f), "Find feathers", GameUiStyle.LabelStyle(ref hintStyle, 22, TextAnchor.MiddleLeft, FontStyle.Bold));
        GUI.Label(new Rect(rect.x + 16f, rect.y + 46f, rect.width - 32f, 26f), CollectedFeatherCount() + "/" + featherNames.Length, GameUiStyle.LabelStyle(ref promptStyle, 20, TextAnchor.MiddleLeft));
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

    private GameObject FindSceneGameObject(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject != null)
        {
            return activeObject;
        }

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject sceneObject in allObjects)
        {
            if (sceneObject == null || !sceneObject.scene.IsValid())
            {
                continue;
            }

            if (sceneObject.name == objectName)
            {
                return sceneObject;
            }
        }

        return null;
    }

    private void LoadEditorControllers()
    {
#if UNITY_EDITOR
        if (witchStandController == null)
        {
            witchStandController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(witchStandControllerPath);
        }

#endif
    }
}
