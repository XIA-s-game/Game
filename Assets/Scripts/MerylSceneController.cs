// Main function: Controls the Meryl cutscene and ending scene, including player setup, walkable ground fixes, video audio playback, prompts, and ending triggers.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AquariusMax.Fae.demo;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MerylSceneController : MonoBehaviour
{
    [Header("Player Setup")]
    [SerializeField] private string playerName = "AQM_FPS_Character";
    [SerializeField] private string visibleHeroName = "Walking";
    [SerializeField] private string playerPrefabAssetPath = "Assets/Aquarius Fantasy - Fae Pack/Aquarius Max Character, Scripts/AQM_FPS_Character.prefab";
    [SerializeField] private string visibleHeroAssetPath = "Assets/character/Walking.fbx";
    [SerializeField] private string visibleHeroAnimatorControllerPath = "Assets/character/PlayerAnimator.controller";
    [SerializeField] private Vector3 playerCameraLocalPosition = new Vector3(0f, 2.25f, -1.6f);
    [SerializeField] private Vector3 visibleHeroLocalPosition = new Vector3(0f, -0.128f, 0.349f);
    [SerializeField] private float visibleHeroScale = 0.42f;
    [SerializeField] private float visibleHeroFootGroundOffset = 0f;

    [Header("Scene Flow")]
    [SerializeField] private float respawnFallY = -10f;
    [SerializeField] private float spawnLift = 0.02f;
    [SerializeField] private float floorThickness = 0.3f;
    [SerializeField] private float minFloorWidth = 0.8f;
    [SerializeField] private float groundRayStartHeight = 25f;
    [SerializeField] private float groundRayDistance = 80f;
    [SerializeField] private float interactionDistance = 1.9f;
    [SerializeField] private float endingLineDuration = 5f;
    [SerializeField] private float fallRespawnDelay = 5f;

    private readonly string[] ltNames = { "lt1", "lt2", "lt3", "lt4", "lt5", "lt6", "lt7", "lt8", "lt9", "lt10" };
    private readonly string[] endingLines =
    {
        "You entered the forest looking for magic.",
        "You leave as a light in many hearts.",
        "Keep going. The path to becoming a great mage is long.",
        "To be continued in Magic Forest."
    };

    private readonly Dictionary<Collider, string> walkableGroundByCollider = new Dictionary<Collider, string>();
    private readonly List<GameObject> generatedFloorObjects = new List<GameObject>();

    private Scene activeScene;
    private GameObject player;
    private CharacterController playerController;
    private MonoBehaviour demoCharacterBehaviour;
    private Transform visibleHero;

    private GameObject startObject;
    private GameObject endObject;
    private GameObject platformObject;
    private GameObject stonePlatformObject;
    private GameObject stoneObject;
    private GameObject magicBoxLowObject;
    private GameObject pictureObject;
    private GameObject bookObject;

    private Canvas uiCanvas;
    private GameObject promptPanelObject;
    private Text promptText;
    private Image blackOverlay;
    private Text endingText;

    private VideoPlayer videoPlayer;
    private AudioSource videoAudioSource;
    private bool videoFinished;
    private bool lt1Triggered;
    private bool endingStarted;
    private bool canInteractWithBook;
    private bool bootstrapComplete;
    private float fallBelowThresholdStartedAt = -1f;

    private enum FlowState
    {
        Bootstrapping,
        FreeRoam,
        Lt1Sequence,
        AwaitBookPickup,
        Ending
    }

    private FlowState flowState = FlowState.Bootstrapping;

    // Function: Initializes component references, cached state, and default runtime data.
    private void Awake()
    {
        activeScene = SceneManager.GetActiveScene();
        CacheSceneReferences();
        EnsureUi();
        EnsureVideoPlayer();
        SetInitialSceneVisibility();
    }

    // Function: Runs one-time setup after the scene has started.
    private IEnumerator Start()
    {
        yield return null;

        BuildWalkableGround();
        SetupPlayerForMeryl();
        RespawnPlayerToStart();

        flowState = FlowState.FreeRoam;
        yield return null;

        if (player != null)
        {
            playerController = player.GetComponent<CharacterController>();
            ApplyPlayerCamera(player);
            RespawnPlayerToStart();
        }

        bootstrapComplete = true;
    }

    // Function: Updates input handling, interaction checks, and active gameplay flow each frame.
    private void Update()
    {
        if (!bootstrapComplete || player == null)
        {
            return;
        }

        if (flowState != FlowState.Ending)
        {
            if (player.transform.position.y < respawnFallY)
            {
                if (fallBelowThresholdStartedAt < 0f)
                {
                    fallBelowThresholdStartedAt = Time.time;
                }
                else if (Time.time - fallBelowThresholdStartedAt >= fallRespawnDelay)
                {
                    RespawnPlayerToStart();
                }
            }
            else
            {
                fallBelowThresholdStartedAt = -1f;
            }
        }

        if (!lt1Triggered && flowState == FlowState.FreeRoam && IsStandingOnGround("lt1"))
        {
            StartCoroutine(HandleLt1Sequence());
        }

        UpdateBookPrompt();
    }

    // Function: Caches the initial state or references for scene references.
    private void CacheSceneReferences()
    {
        startObject = FindSceneObject("start");
        endObject = FindSceneObject("end");
        platformObject = FindSceneObject("platform");
        stonePlatformObject = FindSceneObject("stone_platform");
        stoneObject = FindSceneObject("stone");
        magicBoxLowObject = FindSceneObject("Magic_box_low");
        pictureObject = FindSceneObject("picture");
        bookObject = FindSceneObject("book");
    }

    // Function: Sets initial scene visibility.
    private void SetInitialSceneVisibility()
    {
        if (pictureObject != null)
        {
            pictureObject.SetActive(false);
        }

        if (magicBoxLowObject != null)
        {
            magicBoxLowObject.SetActive(false);
        }

        if (bookObject != null)
        {
            bookObject.SetActive(false);
        }
    }

    // Function: Builds the data or scene objects needed for walkable ground.
    private void BuildWalkableGround()
    {
        ClearGeneratedFloors();
        walkableGroundByCollider.Clear();

        Collider[] sceneColliders = Resources.FindObjectsOfTypeAll<Collider>();
        for (int i = 0; i < sceneColliders.Length; i++)
        {
            Collider collider = sceneColliders[i];
            if (collider == null || collider.gameObject.scene != activeScene)
            {
                continue;
            }

            collider.enabled = false;
        }

        CreateWalkableFloor(startObject, "start");
        CreateWalkableFloor(endObject, "end");
        CreateWalkableFloor(platformObject, "platform");
        CreateWalkableFloor(stonePlatformObject != null ? stonePlatformObject : stoneObject, "stone_platform");

        for (int i = 0; i < ltNames.Length; i++)
        {
            CreateWalkableFloor(FindSceneObject(ltNames[i]), ltNames[i]);
        }
    }

    // Function: Clears generated floors.
    private void ClearGeneratedFloors()
    {
        for (int i = generatedFloorObjects.Count - 1; i >= 0; i--)
        {
            if (generatedFloorObjects[i] != null)
            {
                Destroy(generatedFloorObjects[i]);
            }
        }

        generatedFloorObjects.Clear();
    }

    // Function: Creates the objects, textures, or UI needed for walkable floor.
    private void CreateWalkableFloor(GameObject sourceObject, string groundName)
    {
        if (sourceObject == null)
        {
            return;
        }

        if (!TryGetWalkableFloorBounds(sourceObject, out Bounds bounds))
        {
            return;
        }

        float sizeX = Mathf.Max(bounds.size.x, minFloorWidth);
        float sizeZ = Mathf.Max(bounds.size.z, minFloorWidth);
        Vector3 position = new Vector3(bounds.center.x, bounds.max.y - floorThickness * 0.5f, bounds.center.z);

        GameObject floorObject = new GameObject("MerylFloor_" + groundName);
        floorObject.transform.SetParent(transform, true);
        floorObject.transform.position = position;
        floorObject.transform.rotation = Quaternion.identity;

        BoxCollider floorCollider = floorObject.AddComponent<BoxCollider>();
        floorCollider.size = new Vector3(sizeX, floorThickness, sizeZ);
        floorCollider.isTrigger = false;

        walkableGroundByCollider[floorCollider] = groundName;
        generatedFloorObjects.Add(floorObject);
    }

    // Function: Tries to get walkable floor bounds and returns whether it was found.
    private bool TryGetWalkableFloorBounds(GameObject target, out Bounds bounds)
    {
        if (TryGetColliderBounds(target, out bounds))
        {
            return true;
        }

        return TryGetRendererBounds(target, out bounds);
    }

    // Function: Tries to get collider bounds and returns whether it was found.
    private bool TryGetColliderBounds(GameObject target, out Bounds bounds)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        bool hasBounds = false;
        bounds = new Bounds(target.transform.position, Vector3.zero);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }

    // Function: Tries to get renderer bounds and returns whether it was found.
    private bool TryGetRendererBounds(GameObject target, out Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        bounds = new Bounds(target.transform.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
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

    // Function: Sets up player for Meryl.
    private void SetupPlayerForMeryl()
    {
        player = FindExistingPlayer();
        if (player == null)
        {
            player = InstantiatePlayerPrefab();
        }

        if (player == null)
        {
            return;
        }

        player.name = playerName;
        player.SetActive(true);
        DisableDuplicatePlayers(player);
        EnsureVisibleHero(player);
        ApplyPlayerCamera(player);
        UseDemoCharacterMovement(player);
        ResetDemoCharacterState();

        playerController = player.GetComponent<CharacterController>();
        visibleHero = FindChildByName(player.transform, visibleHeroName);
    }

    // Function: Finds existing player objects or component references.
    private GameObject FindExistingPlayer()
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.parent != null || !candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            if (candidate.name == playerName || candidate.name.StartsWith(playerName))
            {
                return candidate.gameObject;
            }
        }

        return null;
    }

    // Function: Runs the instantiate player prefab logic.
    private GameObject InstantiatePlayerPrefab()
    {
#if UNITY_EDITOR
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabAssetPath);
        if (prefab == null)
        {
            return null;
        }

        Object instanceObject = PrefabUtility.InstantiatePrefab(prefab);
        return instanceObject as GameObject;
#else
        return null;
#endif
    }

    // Function: Disables duplicate players.
    private void DisableDuplicatePlayers(GameObject keepPlayer)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.parent != null || candidate.gameObject == keepPlayer || !candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            if (candidate.name == playerName || candidate.name.StartsWith(playerName))
            {
                candidate.gameObject.SetActive(false);
            }
        }
    }

    // Function: Ensures visible hero exists, is configured, or is ready to use.
    private void EnsureVisibleHero(GameObject playerObject)
    {
        Transform hero = FindChildByName(playerObject.transform, visibleHeroName);
        if (hero == null)
        {
#if UNITY_EDITOR
            GameObject heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(visibleHeroAssetPath);
            if (heroPrefab != null)
            {
                Object instanceObject = PrefabUtility.InstantiatePrefab(heroPrefab);
                GameObject heroObject = instanceObject as GameObject;
                if (heroObject != null)
                {
                    hero = heroObject.transform;
                }
            }
#endif
        }

        if (hero == null)
        {
            return;
        }

        hero.name = visibleHeroName;
        hero.SetParent(playerObject.transform, false);
        hero.localPosition = visibleHeroLocalPosition;
        hero.localRotation = Quaternion.identity;
        hero.localScale = Vector3.one * visibleHeroScale;
        hero.gameObject.SetActive(true);
        DisableNestedCameras(hero.gameObject);
        ConfigureVisibleHeroAnimator(hero.gameObject);
        AlignVisibleHeroFeetToController(playerObject, hero);
    }

    // Function: Runs the configure visible hero animator logic.
    private void ConfigureVisibleHeroAnimator(GameObject heroObject)
    {
        Animator animator = heroObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = heroObject.AddComponent<Animator>();
        }

#if UNITY_EDITOR
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(visibleHeroAnimatorControllerPath);
        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
        }
#endif

        animator.applyRootMotion = false;
        animator.enabled = true;
    }

    // Function: Disables nested cameras.
    private void DisableNestedCameras(GameObject heroObject)
    {
        Camera[] cameras = heroObject.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
            {
                cameras[i].gameObject.SetActive(false);
            }
        }

        AudioListener[] listeners = heroObject.GetComponentsInChildren<AudioListener>(true);
        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] != null)
            {
                listeners[i].enabled = false;
            }
        }
    }

    // Function: Applies player camera effects to the scene or target object.
    private void ApplyPlayerCamera(GameObject playerObject)
    {
        Camera playerCamera = playerObject.GetComponentInChildren<Camera>(true);
        if (playerCamera == null)
        {
            return;
        }

        playerCamera.transform.localPosition = playerCameraLocalPosition;
        playerCamera.transform.localRotation = Quaternion.identity;
        playerCamera.gameObject.SetActive(true);
        playerCamera.tag = "MainCamera";

        AudioListener listener = playerCamera.GetComponent<AudioListener>();
        if (listener != null)
        {
            listener.enabled = true;
        }
    }

    // Function: Runs the use demo character movement logic.
    private void UseDemoCharacterMovement(GameObject playerObject)
    {
        MonoBehaviour[] behaviours = playerObject.GetComponentsInChildren<MonoBehaviour>(true);
        demoCharacterBehaviour = null;

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;
            if (typeName == "DemoCharacter")
            {
                demoCharacterBehaviour = behaviour;
                behaviour.enabled = true;
                SetPrivateBool(behaviour, "blockSolidObstacles", false);
                SetPrivateBool(behaviour, "usePreciseBodyCollision", false);
            }
            else if (typeName == "PlayerCharacterController")
            {
                behaviour.enabled = false;
            }
        }
    }

    // Function: Resets demo character state to its starting state.
    private void ResetDemoCharacterState()
    {
        DemoCharacter.LockPlayerInput = false;
        DemoCharacter.LockMovementInput = false;
        DemoCharacter.ForceWalkAnimation = false;
        DemoCharacter.UseLookPadInput = false;
        DemoCharacter.LookPadInput = Vector2.zero;

        ClearPlayerMotionState();
    }

    // Function: Clears player motion state.
    private void ClearPlayerMotionState()
    {
        if (demoCharacterBehaviour == null)
        {
            return;
        }

        SetPrivateField(demoCharacterBehaviour, "moveInput", Vector2.zero);
        SetPrivateField(demoCharacterBehaviour, "move", Vector3.zero);
        SetPrivateField(demoCharacterBehaviour, "jumpPressed", false);
        SetPrivateField(demoCharacterBehaviour, "isJumping", false);
        SetPrivateField(demoCharacterBehaviour, "isCrouching", false);
    }

    // Function: Sets player locked.
    private void SetPlayerLocked(bool locked)
    {
        DemoCharacter.LockPlayerInput = locked;
        DemoCharacter.LockMovementInput = locked;
        DemoCharacter.ForceWalkAnimation = false;
        DemoCharacter.UseLookPadInput = false;
        DemoCharacter.LookPadInput = Vector2.zero;

        if (!locked)
        {
            ResetDemoCharacterState();
        }
    }

    // Function: Respawns or returns player to start to the start point.
    private void RespawnPlayerToStart()
    {
        if (player == null || startObject == null)
        {
            return;
        }

        fallBelowThresholdStartedAt = -1f;
        Vector3 spawnPosition = GetGroundedPositionAt(startObject.transform.position);
        TeleportPlayer(spawnPosition);
        SetPromptText(string.Empty);

        if (flowState == FlowState.Lt1Sequence)
        {
            flowState = FlowState.FreeRoam;
            canInteractWithBook = false;
        }

        if (flowState != FlowState.Ending)
        {
            SetPlayerLocked(false);
        }
    }

    // Function: Teleports player to the requested position.
    private void TeleportPlayer(Vector3 targetPosition)
    {
        if (player == null)
        {
            return;
        }

        if (playerController == null)
        {
            playerController = player.GetComponent<CharacterController>();
        }

        bool controllerWasEnabled = playerController != null && playerController.enabled;
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        player.transform.position = targetPosition;
        AlignVisibleHeroFeetToController(player, visibleHero != null ? visibleHero : FindChildByName(player.transform, visibleHeroName));
        ClearPlayerMotionState();

        if (playerController != null)
        {
            playerController.enabled = controllerWasEnabled;
        }
    }

    // Function: Gets or calculates grounded position at.
    private Vector3 GetGroundedPositionAt(Vector3 worldPosition)
    {
        Vector3 position = worldPosition;
        float controllerFootOffset = 0f;

        if (playerController == null && player != null)
        {
            playerController = player.GetComponent<CharacterController>();
        }

        if (playerController != null)
        {
            controllerFootOffset = playerController.center.y - playerController.height * 0.5f;
        }

        Vector3 rayStart = new Vector3(worldPosition.x, worldPosition.y + groundRayStartHeight, worldPosition.z);
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRayDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            position.y = hit.point.y + spawnLift - controllerFootOffset;
            return position;
        }

        position.y += spawnLift;
        return position;
    }

    // Function: Checks whether standing on ground is true.
    private bool IsStandingOnGround(string groundName)
    {
        if (player == null)
        {
            return false;
        }

        Collider groundCollider = GetGroundBelowPlayer();
        return groundCollider != null &&
               walkableGroundByCollider.TryGetValue(groundCollider, out string currentGroundName) &&
               currentGroundName == groundName;
    }

    // Function: Gets or calculates ground below player.
    private Collider GetGroundBelowPlayer()
    {
        if (player == null)
        {
            return null;
        }

        if (playerController == null)
        {
            playerController = player.GetComponent<CharacterController>();
        }

        Vector3 origin = player.transform.position + Vector3.up * 0.8f;
        float distance = 2.2f;

        if (playerController != null)
        {
            origin = player.transform.TransformPoint(playerController.center) + Vector3.up * 0.1f;
            distance = Mathf.Max(1.4f, playerController.height * 0.75f + 0.6f);
        }

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && walkableGroundByCollider.ContainsKey(hit.collider))
            {
                return hit.collider;
            }
        }

        return null;
    }

    // Function: Handles lt 1 sequence interaction or progression.
    private IEnumerator HandleLt1Sequence()
    {
        lt1Triggered = true;
        flowState = FlowState.Lt1Sequence;
        canInteractWithBook = false;
        SetPromptText(string.Empty);

        if (pictureObject != null)
        {
            pictureObject.SetActive(true);
        }

        SetPlayerLocked(true);
        GameObject lt1Target = endObject != null ? endObject : magicBoxLowObject;
        Vector3 targetPosition = lt1Target != null ? GetGroundedPositionAt(lt1Target.transform.position) : player.transform.position;
        TeleportPlayer(targetPosition);
        yield return null;

        if (lt1Target != null)
        {
            TeleportPlayer(GetGroundedPositionAt(lt1Target.transform.position));
            SetPlayerLocked(true);
        }

        yield return StartCoroutine(PlayVideoAudioOnly());

        if (magicBoxLowObject != null)
        {
            magicBoxLowObject.SetActive(true);
        }

        SetPlayerLocked(false);
        flowState = FlowState.AwaitBookPickup;
        canInteractWithBook = true;
    }

    // Function: Plays video audio only animation, audio, or cutscene behavior.
    private IEnumerator PlayVideoAudioOnly()
    {
        string videoPath = Path.Combine(Application.dataPath, "new/final/video.mp4");
        if (!File.Exists(videoPath) || videoPlayer == null)
        {
            yield break;
        }

        videoFinished = false;
        videoPlayer.Stop();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoPath;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);
        videoPlayer.Prepare();

        float prepareDeadline = Time.unscaledTime + 5f;
        while (!videoPlayer.isPrepared && !videoFinished && Time.unscaledTime < prepareDeadline)
        {
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            yield break;
        }

        videoPlayer.Play();

        float fallbackDuration = (float)videoPlayer.length;
        if (fallbackDuration <= 0f || fallbackDuration > 600f)
        {
            fallbackDuration = 10f;
        }

        float playDeadline = Time.unscaledTime + fallbackDuration + 1f;
        while (!videoFinished && Time.unscaledTime < playDeadline)
        {
            if (!videoPlayer.isPlaying && videoPlayer.frame > 0)
            {
                break;
            }

            yield return null;
        }

        videoPlayer.Stop();
    }

    // Function: Ensures video player exists, is configured, or is ready to use.
    private void EnsureVideoPlayer()
    {
        videoPlayer = gameObject.GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoAudioSource = gameObject.GetComponent<AudioSource>();
        if (videoAudioSource == null)
        {
            videoAudioSource = gameObject.AddComponent<AudioSource>();
        }

        videoAudioSource.playOnAwake = false;
        videoAudioSource.loop = false;
        videoAudioSource.spatialBlend = 0f;

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.loopPointReached -= HandleVideoFinished;
        videoPlayer.loopPointReached += HandleVideoFinished;
        videoPlayer.errorReceived -= HandleVideoError;
        videoPlayer.errorReceived += HandleVideoError;
    }

    // Function: Handles video finished interaction or progression.
    private void HandleVideoFinished(VideoPlayer source)
    {
        videoFinished = true;
    }

    // Function: Handles video error interaction or progression.
    private void HandleVideoError(VideoPlayer source, string message)
    {
        videoFinished = true;
    }

    // Function: Updates book prompt state, input, or presentation.
    private void UpdateBookPrompt()
    {
        if (promptText == null)
        {
            return;
        }

        bool canShowPrompt = flowState == FlowState.AwaitBookPickup &&
                             canInteractWithBook &&
                             magicBoxLowObject != null &&
                             magicBoxLowObject.activeInHierarchy &&
                             player != null;

        if (!canShowPrompt)
        {
            SetPromptText(string.Empty);
            return;
        }

        Vector3 playerPosition = player.transform.position;
        Vector3 boxPosition = magicBoxLowObject.transform.position;
        playerPosition.y = 0f;
        boxPosition.y = 0f;

        bool isNear = Vector3.Distance(playerPosition, boxPosition) <= interactionDistance;
        SetPromptText(isNear ? "Press E to take the magic book" : string.Empty);

        if (isNear && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(PlayEndingSequence());
        }
    }

    // Function: Plays ending sequence animation, audio, or cutscene behavior.
    private IEnumerator PlayEndingSequence()
    {
        if (endingStarted)
        {
            yield break;
        }

        endingStarted = true;
        flowState = FlowState.Ending;
        canInteractWithBook = false;
        SetPromptText(string.Empty);
        SetPlayerLocked(true);

        if (bookObject != null)
        {
            bookObject.SetActive(true);
        }

        blackOverlay.gameObject.SetActive(true);
        blackOverlay.color = Color.black;
        endingText.gameObject.SetActive(true);

        for (int i = 0; i < endingLines.Length; i++)
        {
            endingText.text = endingLines[i];
            yield return new WaitForSecondsRealtime(endingLineDuration);
        }

        endingText.text = string.Empty;
    }

    // Function: Ensures UI exists, is configured, or is ready to use.
    private void EnsureUi()
    {
        if (uiCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("MerylSceneUI");
        canvasObject.transform.SetParent(transform, false);

        uiCanvas = canvasObject.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        promptPanelObject = new GameObject("PromptPanel");
        promptPanelObject.transform.SetParent(canvasObject.transform, false);
        Image promptPanelImage = promptPanelObject.AddComponent<Image>();
        promptPanelImage.color = new Color(0.04f, 0.06f, 0.06f, 0.78f);
        RectTransform promptPanelRect = promptPanelObject.GetComponent<RectTransform>();
        promptPanelRect.anchorMin = new Vector2(0.5f, 0f);
        promptPanelRect.anchorMax = new Vector2(0.5f, 0f);
        promptPanelRect.pivot = new Vector2(0.5f, 0f);
        promptPanelRect.sizeDelta = new Vector2(900f, 80f);
        promptPanelRect.anchoredPosition = new Vector2(0f, 74f);

        promptText = CreateText("PromptText", promptPanelObject.transform, 34, TextAnchor.MiddleCenter);
        RectTransform promptRect = promptText.rectTransform;
        promptRect.anchorMin = Vector2.zero;
        promptRect.anchorMax = Vector2.one;
        promptRect.offsetMin = new Vector2(18f, 8f);
        promptRect.offsetMax = new Vector2(-18f, -8f);
        SetPromptText(string.Empty);

        GameObject overlayObject = new GameObject("BlackOverlay");
        overlayObject.transform.SetParent(canvasObject.transform, false);
        blackOverlay = overlayObject.AddComponent<Image>();
        blackOverlay.color = Color.black;
        RectTransform overlayRect = blackOverlay.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        blackOverlay.gameObject.SetActive(false);

        endingText = CreateText("EndingText", overlayObject.transform, 52, TextAnchor.MiddleCenter);
        RectTransform endingRect = endingText.rectTransform;
        endingRect.anchorMin = new Vector2(0.5f, 0.5f);
        endingRect.anchorMax = new Vector2(0.5f, 0.5f);
        endingRect.sizeDelta = new Vector2(1400f, 240f);
        endingRect.anchoredPosition = Vector2.zero;
        endingText.gameObject.SetActive(false);
        endingText.text = string.Empty;
    }

    // Function: Creates the objects, textures, or UI needed for text.
    private Text CreateText(string objectName, Transform parent, int fontSize, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        Font builtInFont = null;
        try
        {
            builtInFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            builtInFont = null;
        }

        if (builtInFont == null)
        {
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            if (fonts != null && fonts.Length > 0)
            {
                builtInFont = fonts[0];
            }
        }

        text.font = builtInFont;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;

        RectTransform rect = text.rectTransform;
        rect.localScale = Vector3.one;

        return text;
    }

    // Function: Sets prompt text.
    private void SetPromptText(string value)
    {
        if (promptText != null)
        {
            promptText.text = value;
        }

        if (promptPanelObject != null)
        {
            promptPanelObject.SetActive(!string.IsNullOrEmpty(value));
        }
    }

    // Function: Finds scene object objects or component references.
    private GameObject FindSceneObject(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.gameObject.scene != activeScene)
            {
                continue;
            }

            if (candidate.name == objectName)
            {
                return candidate.gameObject;
            }
        }

        return null;
    }

    // Function: Finds child by name objects or component references.
    private Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }

    // Function: Runs the align visible hero feet to controller logic.
    private void AlignVisibleHeroFeetToController(GameObject playerObject, Transform hero)
    {
        if (playerObject == null || hero == null)
        {
            return;
        }

        CharacterController controller = playerObject.GetComponent<CharacterController>();
        if (controller == null)
        {
            return;
        }

        Renderer[] renderers = hero.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
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

        if (!hasBounds)
        {
            return;
        }

        float controllerScaleY = Mathf.Abs(playerObject.transform.lossyScale.y);
        Vector3 controllerCenter = playerObject.transform.TransformPoint(controller.center);
        float footY = controllerCenter.y - controller.height * controllerScaleY * 0.5f + visibleHeroFootGroundOffset;
        float deltaY = footY - bounds.min.y;
        hero.position += Vector3.up * deltaY;
    }

    // Function: Sets private bool.
    private void SetPrivateBool(MonoBehaviour behaviour, string fieldName, bool value)
    {
        FieldInfo field = behaviour.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(bool))
        {
            field.SetValue(behaviour, value);
        }
    }

    // Function: Sets private field.
    private void SetPrivateField(MonoBehaviour behaviour, string fieldName, object value)
    {
        FieldInfo field = behaviour.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(behaviour, value);
        }
    }
}
