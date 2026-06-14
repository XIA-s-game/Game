using System.Collections;
using AquariusMax.Fae.demo;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class MerylSceneController : MonoBehaviour
{
    // Built with AI assistance to keep shared menu layout consistent across scenes.
    private const string MenuSceneName = "Mainmenu";

    [Header("Player Setup")]
    // Visible hero is attached to the player controller used in the final scene.
    [SerializeField] private Transform visibleHero;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera sceneMainCamera;

    [Header("Scene Flow")]
    // Fall recovery and LT trigger settings for the 11 1 scene.
    [SerializeField] private float respawnFallY = -10f;
    [SerializeField] private float spawnLift = 0.02f;
    [SerializeField] private float groundRayStartHeight = 25f;
    [SerializeField] private float groundRayDistance = 80f;
    [SerializeField] private float ltTriggerDistance = 1.5f;
    [SerializeField] private float fallRespawnDelay = 5f;

    [Header("Scene References")]
    // Ending video is dragged here; the overlay UI is built at runtime.
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject endObject;
    [SerializeField] private VideoClip endingVideoClip;

    private GameObject player;
    private CharacterController playerController;
    private DemoCharacter demoCharacter;
    private RenderTexture activeVideoTexture;
    private GameObject videoOverlayObject;
    private RawImage videoImage;
    private VideoPlayer videoPlayer;
    private AudioSource videoAudioSource;
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
        player = playerObject;

        if (playerObject == null)
        {
            Debug.LogError("MerylSceneController is missing Player Object.", this);
            enabled = false;
            return;
        }

    }

    private IEnumerator Start()
    {
        SetupPlayerForMeryl();

        if (player != null)
        {
            playerController = player.GetComponent<CharacterController>();
            ApplyPlayerCamera(player);
        }

        RespawnPlayerToStart();
        flowState = FlowState.FreeRoam;
        bootstrapComplete = true;

        yield break;
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
        // Prepares the shared player controller and visible hero for the final scene.
        if (playerObject == null)
        {
            Debug.LogError("MerylSceneController is missing Player Object.", this);
            return;
        }

        player = playerObject;
        player.SetActive(true);
        Transform hero = visibleHero;
        if (hero != null)
        {
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

                if (demoCharacter != null)
                {
                    demoCharacter.SetAnimator(animator);
                }
            }
        }

        ApplyPlayerCamera(player);
        SetupDemoCharacter(player);
        playerController = player.GetComponent<CharacterController>();
        ResetDemoCharacterState();
        if (!hasInitialPlayerPosition)
        {
            initialPlayerPosition = player.transform.position;
            hasInitialPlayerPosition = true;
        }
    }

    private void ApplyPlayerCamera(GameObject playerObject)
    {
        Camera activePlayerCamera = playerCamera;
        if (activePlayerCamera == null)
        {
            return;
        }

        playerCamera = activePlayerCamera;
        DisableSceneMainCamera(activePlayerCamera);
        activePlayerCamera.gameObject.SetActive(true);
        activePlayerCamera.enabled = true;
        activePlayerCamera.tag = "MainCamera";

        AudioListener listener = activePlayerCamera.GetComponent<AudioListener>();
        if (listener != null)
        {
            listener.enabled = true;
        }

        if (demoCharacter != null)
        {
            demoCharacter.SetCamera(activePlayerCamera);
        }
    }

    private void DisableSceneMainCamera(Camera playerCamera)
    {
        if (sceneMainCamera != null && sceneMainCamera != playerCamera)
        {
            DisableCamera(sceneMainCamera);
            return;
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

        if (visibleHero != null)
        {
            Animator heroAnimator = visibleHero.GetComponent<Animator>();
            if (heroAnimator != null)
            {
                demoCharacter.SetAnimator(heroAnimator);
            }
        }
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
        // Used when the player falls out of the scene for several seconds.
        if (player == null)
        {
            return;
        }

        fallBelowThresholdStartedAt = -1f;
        Vector3 spawnPosition = hasInitialPlayerPosition ? initialPlayerPosition : player.transform.position;
        TeleportPlayer(spawnPosition);

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
        // End trigger locks the player, snaps to the ending position, plays video, then returns to menu.
        lt1Triggered = true;
        flowState = FlowState.Lt1Sequence;

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
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(MenuSceneName);
    }

    private IEnumerator PlayVideoCutscene()
    {
        // Creates a black fullscreen video overlay and plays the dragged ending clip.
        if (!CanPlayEndingVideo())
        {
            yield break;
        }

        videoFinished = false;
        MainMenuController.PauseBackgroundMusicForSceneAudio();
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        CreateVideoOverlay();
        ReleaseActiveVideoTexture();
        activeVideoTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        activeVideoTexture.Create();
        videoImage.texture = activeVideoTexture;
        videoOverlayObject.SetActive(true);

        videoPlayer.Stop();
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = endingVideoClip;
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

        UpdateVideoLayout();

        videoPlayer.Play();

        float videoDuration = (float)videoPlayer.length;
        if (videoDuration <= 0f || videoDuration > 600f)
        {
            videoDuration = 10f;
        }

        float playDeadline = Time.unscaledTime + videoDuration + 1f;
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
        // Destroys runtime video UI and releases the render texture after playback.
        MainMenuController.ResumeBackgroundMusicAfterSceneAudio();

        if (videoOverlayObject != null)
        {
            Destroy(videoOverlayObject);
            videoOverlayObject = null;
        }

        videoImage = null;
        videoAudioSource = null;

        if (videoPlayer != null)
        {
            videoPlayer.targetTexture = null;
            Destroy(videoPlayer);
            videoPlayer = null;
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

    private void UpdateVideoLayout()
    {
        if (videoImage == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        RectTransform imageRect = videoImage.rectTransform;
        RectTransform canvasRect = videoOverlayObject != null ? videoOverlayObject.GetComponent<RectTransform>() : null;
        float aspect = 16f / 9f;
        if (videoPlayer != null && videoPlayer.width > 0 && videoPlayer.height > 0)
        {
            aspect = (float)videoPlayer.width / videoPlayer.height;
        }

        float targetHeight = canvasRect != null ? canvasRect.rect.height : Screen.height;
        targetHeight *= 0.82f;
        float targetWidth = targetHeight * aspect;

        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = new Vector2(targetWidth, targetHeight);
    }

    private void CreateVideoOverlay()
    {
        // Runtime overlay avoids needing extra empty UI objects in the scene hierarchy.
        if (videoOverlayObject != null)
        {
            return;
        }

        videoOverlayObject = new GameObject("Ending Video Overlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = videoOverlayObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = videoOverlayObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject blackBackground = new GameObject("Black Background", typeof(Image));
        blackBackground.transform.SetParent(videoOverlayObject.transform, false);
        RectTransform backgroundRect = blackBackground.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        blackBackground.GetComponent<Image>().color = Color.black;

        GameObject videoObject = new GameObject("Ending Video Image", typeof(RawImage));
        videoObject.transform.SetParent(videoOverlayObject.transform, false);
        videoImage = videoObject.GetComponent<RawImage>();

        videoPlayer = videoOverlayObject.AddComponent<VideoPlayer>();
        videoAudioSource = videoOverlayObject.AddComponent<AudioSource>();
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

    private bool CanPlayEndingVideo()
    {
        return endingVideoClip != null;
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
