using System.Collections;
using System.IO;
using AquariusMax.Fae.demo;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class MerylSceneController : MonoBehaviour
{
    [Header("Player Setup")]
    [SerializeField] private string playerName = "AQM_FPS_Character";
    [SerializeField] private string visibleHeroName = "Walking";
    [SerializeField] private Vector3 playerCameraLocalPosition = new Vector3(0f, 2.25f, -1.6f);
    [SerializeField] private Camera sceneMainCamera;

    [Header("Scene Flow")]
    [SerializeField] private float respawnFallY = -10f;
    [SerializeField] private float spawnLift = 0.02f;
    [SerializeField] private float groundRayStartHeight = 25f;
    [SerializeField] private float groundRayDistance = 80f;
    [SerializeField] private float ltTriggerDistance = 1.5f;
    [SerializeField] private float fallRespawnDelay = 5f;

    [Header("Scene References")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject endObject;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private GameObject promptPanelObject;
    [SerializeField] private Text promptText;
    [SerializeField] private GameObject videoOverlayObject;
    [SerializeField] private RawImage videoImage;
    [SerializeField] private AspectRatioFitter videoAspectFitter;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource videoAudioSource;

    private Scene activeScene;
    private GameObject player;
    private CharacterController playerController;
    private DemoCharacter demoCharacter;
    private Transform visibleHero;
    private RenderTexture activeVideoTexture;
    private bool videoFinished;
    private bool lt1Triggered;
    private bool bootstrapComplete;
    private float fallBelowThresholdStartedAt = -1f;
    private Vector3 initialPlayerPosition;
    private bool hasInitialPlayerPosition;

    private enum FlowState
    {
        Bootstrapping,
        FreeRoam,
        Lt1Sequence
    }

    private FlowState flowState = FlowState.Bootstrapping;

    private void Awake()
    {
        activeScene = SceneManager.GetActiveScene();
        player = playerObject;

        if (uiCanvas == null)
        {
            GameObject canvasObject = new GameObject("MerylSceneUI");
            canvasObject.transform.SetParent(transform, false);

            uiCanvas = canvasObject.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 9999;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (promptPanelObject == null)
        {
            promptPanelObject = new GameObject("PromptPanel");
            promptPanelObject.transform.SetParent(uiCanvas.transform, false);

            Image promptPanelImage = promptPanelObject.AddComponent<Image>();
            promptPanelImage.color = new Color(0.04f, 0.06f, 0.06f, 0.78f);

            RectTransform promptPanelRect = promptPanelObject.GetComponent<RectTransform>();
            promptPanelRect.anchorMin = new Vector2(0.5f, 0f);
            promptPanelRect.anchorMax = new Vector2(0.5f, 0f);
            promptPanelRect.pivot = new Vector2(0.5f, 0f);
            promptPanelRect.sizeDelta = new Vector2(900f, 80f);
            promptPanelRect.anchoredPosition = new Vector2(0f, 74f);
        }

        if (promptText == null)
        {
            GameObject textObject = new GameObject("PromptText");
            textObject.transform.SetParent(promptPanelObject.transform, false);

            promptText = textObject.AddComponent<Text>();
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = 34;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.horizontalOverflow = HorizontalWrapMode.Wrap;
            promptText.verticalOverflow = VerticalWrapMode.Overflow;
            promptText.color = Color.white;

            RectTransform promptRect = promptText.rectTransform;
            promptRect.anchorMin = Vector2.zero;
            promptRect.anchorMax = Vector2.one;
            promptRect.offsetMin = new Vector2(18f, 8f);
            promptRect.offsetMax = new Vector2(-18f, -8f);
        }

        if (videoOverlayObject == null)
        {
            videoOverlayObject = new GameObject("VideoOverlay");
            videoOverlayObject.transform.SetParent(uiCanvas.transform, false);

            Image background = videoOverlayObject.AddComponent<Image>();
            background.color = Color.black;

            RectTransform overlayRect = videoOverlayObject.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            videoOverlayObject.SetActive(false);
        }

        if (videoImage == null)
        {
            GameObject imageObject = new GameObject("VideoImage");
            imageObject.transform.SetParent(videoOverlayObject.transform, false);

            videoImage = imageObject.AddComponent<RawImage>();
            videoImage.color = Color.white;

            RectTransform imageRect = videoImage.rectTransform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
        }

        if (videoAspectFitter == null)
        {
            videoAspectFitter = videoImage.gameObject.AddComponent<AspectRatioFitter>();
            videoAspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            videoAspectFitter.aspectRatio = 16f / 9f;
        }

        if (videoPlayer == null)
        {
            videoPlayer = gameObject.GetComponent<VideoPlayer>();
        }

        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        if (videoAudioSource == null)
        {
            videoAudioSource = gameObject.GetComponent<AudioSource>();
        }

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

        SetPromptText(string.Empty);
    }

    private IEnumerator Start()
    {
        yield return null;

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

    private void Update()
    {
        if (!bootstrapComplete || player == null)
        {
            return;
        }

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

        if (!lt1Triggered && flowState == FlowState.FreeRoam && IsNearObject(endObject, ltTriggerDistance))
        {
            StartCoroutine(HandleLt1Sequence());
        }
    }

    private void SetupPlayerForMeryl()
    {
        if (playerObject == null)
        {
            Debug.LogError("MerylSceneController is missing Player Object.", this);
            return;
        }

        player = playerObject;
        player.name = playerName;
        player.SetActive(true);
        Transform hero = FindChildByName(player.transform, visibleHeroName);
        if (hero != null)
        {
            hero.name = visibleHeroName;
            hero.SetParent(player.transform, false);
            hero.gameObject.SetActive(true);

            Camera[] cameras = hero.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null)
                {
                    cameras[i].gameObject.SetActive(false);
                }
            }

            AudioListener[] listeners = hero.GetComponentsInChildren<AudioListener>(true);
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null)
                {
                    listeners[i].enabled = false;
                }
            }

            Animator animator = hero.GetComponent<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.enabled = true;
            }
        }

        ApplyPlayerCamera(player);
        SetupDemoCharacter(player);
        playerController = player.GetComponent<CharacterController>();
        ResetDemoCharacterState();
        visibleHero = FindChildByName(player.transform, visibleHeroName);

        if (!hasInitialPlayerPosition)
        {
            initialPlayerPosition = player.transform.position;
            hasInitialPlayerPosition = true;
        }
    }

    private void ApplyPlayerCamera(GameObject playerObject)
    {
        Camera playerCamera = playerObject.GetComponentInChildren<Camera>(true);
        if (playerCamera == null)
        {
            return;
        }

        DisableSceneMainCamera(playerCamera);
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

    private void DisableSceneMainCamera(Camera playerCamera)
    {
        if (sceneMainCamera != null && sceneMainCamera != playerCamera)
        {
            DisableCamera(sceneMainCamera);
            return;
        }

        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            Camera[] sceneCameras = roots[rootIndex].GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < sceneCameras.Length; i++)
            {
                Camera camera = sceneCameras[i];
                if (camera == null || camera == playerCamera)
                {
                    continue;
                }

                if (camera.transform.IsChildOf(playerCamera.transform) || playerCamera.transform.IsChildOf(camera.transform))
                {
                    continue;
                }

                if (camera.CompareTag("MainCamera") || camera.name == "Main Camera")
                {
                    DisableCamera(camera);
                }
            }
        }
    }

    private void SetupDemoCharacter(GameObject playerObject)
    {
        demoCharacter = playerObject.GetComponentInChildren<DemoCharacter>(true);
        if (demoCharacter == null)
        {
            return;
        }

        demoCharacter.enabled = true;
        demoCharacter.SetCollisionOptions(false, false);
    }

    private void ResetDemoCharacterState()
    {
        DemoCharacter.ResetControlFlags();
        ClearPlayerMotionState();
    }

    private void ClearPlayerMotionState()
    {
        if (demoCharacter == null)
        {
            return;
        }

        demoCharacter.ClearMotionState();
    }

    private void SetPlayerLocked(bool locked)
    {
        DemoCharacter.SetControlLocked(locked);

        if (!locked)
        {
            ResetDemoCharacterState();
        }
    }

    private void RespawnPlayerToStart()
    {
        if (player == null)
        {
            return;
        }

        fallBelowThresholdStartedAt = -1f;
        Vector3 spawnPosition = hasInitialPlayerPosition ? initialPlayerPosition : player.transform.position;
        TeleportPlayer(spawnPosition);
        SetPromptText(string.Empty);

        if (flowState == FlowState.Lt1Sequence)
        {
            flowState = FlowState.FreeRoam;
        }

        SetPlayerLocked(false);
    }

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
        ClearPlayerMotionState();

        if (playerController != null)
        {
            playerController.enabled = controllerWasEnabled;
        }
    }

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

    private bool IsNearObject(GameObject targetObject, float distance)
    {
        if (player == null || targetObject == null)
        {
            return false;
        }

        Vector3 playerPosition = player.transform.position;
        Vector3 targetPosition = targetObject.transform.position;
        playerPosition.y = 0f;
        targetPosition.y = 0f;
        return Vector3.Distance(playerPosition, targetPosition) <= distance;
    }

    private IEnumerator HandleLt1Sequence()
    {
        lt1Triggered = true;
        flowState = FlowState.Lt1Sequence;
        SetPromptText(string.Empty);

        SetPlayerLocked(true);
        GameObject lt1Target = endObject;
        Vector3 targetPosition = lt1Target != null ? GetGroundedPositionAt(lt1Target.transform.position) : player.transform.position;
        TeleportPlayer(targetPosition);
        yield return null;

        if (lt1Target != null)
        {
            TeleportPlayer(GetGroundedPositionAt(lt1Target.transform.position));
            SetPlayerLocked(true);
        }

        yield return StartCoroutine(PlayVideoCutscene());
        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator PlayVideoCutscene()
    {
        string videoPath = Path.Combine(Application.dataPath, "new/final/video.mp4");
        if (!File.Exists(videoPath) || videoPlayer == null)
        {
            yield break;
        }

        if (videoImage == null)
        {
            yield break;
        }

        videoFinished = false;
        ReleaseActiveVideoTexture();
        activeVideoTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        activeVideoTexture.Create();
        videoImage.texture = activeVideoTexture;
        videoOverlayObject.SetActive(true);

        videoPlayer.Stop();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoPath;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = activeVideoTexture;
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
            CleanupVideoPlayback();
            yield break;
        }

        if (videoPlayer.width > 0 && videoPlayer.height > 0 && videoAspectFitter != null)
        {
            videoAspectFitter.aspectRatio = (float)videoPlayer.width / videoPlayer.height;
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
        CleanupVideoPlayback();
    }

    private void CleanupVideoPlayback()
    {
        if (videoOverlayObject != null)
        {
            videoOverlayObject.SetActive(false);
        }

        if (videoImage != null)
        {
            videoImage.texture = null;
        }

        if (videoPlayer != null)
        {
            videoPlayer.targetTexture = null;
        }

        ReleaseActiveVideoTexture();
    }

    private void ReleaseActiveVideoTexture()
    {
        if (activeVideoTexture == null)
        {
            return;
        }

        activeVideoTexture.Release();
        Destroy(activeVideoTexture);
        activeVideoTexture = null;
    }

    private void HandleVideoFinished(VideoPlayer source)
    {
        videoFinished = true;
    }

    private void HandleVideoError(VideoPlayer source, string message)
    {
        videoFinished = true;
    }

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

    private void DisableCamera(Camera camera)
    {
        AudioListener extraListener = camera.GetComponent<AudioListener>();
        if (extraListener != null)
        {
            extraListener.enabled = false;
        }

        camera.gameObject.SetActive(false);
    }

}
