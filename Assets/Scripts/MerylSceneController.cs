using System.Collections;
using System;
using System.IO;
using AquariusMax.Fae.demo;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class MerylSceneController : MonoBehaviour
{
    // Built with AI assistance to keep shared menu layout consistent across scenes.
    // Scene loaded after the ending video.
    private const string MenuSceneName = "MainMenu";

    [Header("Player Setup")]
    // Visible hero is attached to the player controller used in the final scene.
    [SerializeField] private Transform visibleHero;
    // Camera used by the player controller.
    [SerializeField] private Camera playerCamera;
    // Scene camera disabled after the player is ready.
    [SerializeField] private Camera sceneMainCamera;

    [Header("Scene Flow")]
    // Fall recovery and LT trigger settings for the Chapter5_MoonlitGlade scene.
    [SerializeField] private float respawnFallY = -10f;
    // Small lift added when placing the player on the ground.
    [SerializeField] private float spawnLift = 0.02f;
    // Ray start height for finding ground.
    [SerializeField] private float groundRayStartHeight = 25f;
    // Ray length for finding ground.
    [SerializeField] private float groundRayDistance = 80f;
    // Distance needed to trigger the ending object.
    [SerializeField] private float ltTriggerDistance = 1.5f;
    // How long the player must fall before respawn.
    [SerializeField] private float fallRespawnDelay = 5f;

    [Header("Scene References")]
    // Ending video is dragged here; the overlay UI is built at runtime.
    [SerializeField] private GameObject playerObject;
    // Object that triggers the ending sequence.
    [SerializeField] private GameObject endObject;
    // Final video clip.
    [SerializeField] private VideoClip endingVideoClip;
    // Fallback mp4 path included in StreamingAssets for player builds.
    [SerializeField] private string endingVideoPath = "new/final/ending.mp4";

    // Runtime player object.
    private GameObject player;
    // Player CharacterController.
    private CharacterController playerController;
    // Player movement script.
    private DemoCharacter demoCharacter;
    // RenderTexture used by the ending video.
    private RenderTexture activeVideoTexture;
    // Runtime fullscreen video overlay.
    private GameObject videoOverlayObject;
    // RawImage that displays the ending video.
    private RawImage videoImage;
    // Runtime VideoPlayer.
    private VideoPlayer videoPlayer;
    // Runtime video audio source.
    private AudioSource videoAudioSource;
    // True once the video reaches the end.
    private bool videoFinished;
    // Stops the ending trigger from running twice.
    private bool lt1Triggered;
    // True after player setup has completed.
    private bool bootstrapComplete;
    // Time when the player first fell below the threshold.
    private float fallBelowThresholdStartedAt = -1f;
    // Initial player position for respawn.
    private Vector3 initialPlayerPosition;
    // True once the initial position is cached.
    private bool hasInitialPlayerPosition;

    private enum FlowState
    {
        Bootstrapping,
        FreeRoam,
        Lt1Sequence
    }

    // Current final-scene flow state.
    private FlowState flowState = FlowState.Bootstrapping;

    private void Awake()
    {
        // Keep a runtime shortcut to the dragged player object.
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
        // Build the playable final scene before allowing free movement.
        SetupPlayerForMeryl();

        if (player != null)
        {
            // Cache controller and camera once the player is active.
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
        // Wait until Start has finished the final scene setup.
        if (!bootstrapComplete || player == null)
        {
            return;
        }

        // If the player falls too low for a few seconds, send them back to the start.
        if (player.transform.position.y < respawnFallY)
        {
            if (fallBelowThresholdStartedAt < 0f)
            {
                // Start timing the fall.
                fallBelowThresholdStartedAt = Time.time;
            }
            else if (Time.time - fallBelowThresholdStartedAt >= fallRespawnDelay)
            {
                // Long fall means the player is probably stuck outside the scene.
                RespawnPlayerToStart();
            }
        }
        else
        {
            // Player recovered before the respawn delay finished.
            fallBelowThresholdStartedAt = -1f;
        }

        // Final trigger starts the ending sequence once.
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
        // Hero model is parented under the shared player controller.
        Transform hero = visibleHero;
        if (hero != null)
        {
            hero.SetParent(player.transform, false);
            hero.gameObject.SetActive(true);

            // Disable any cameras that came with the hero model.
            Camera[] cameras = hero.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null)
                {
                    cameras[i].gameObject.SetActive(false);
                }
            }

            // Disable extra listeners so Unity keeps only one active listener.
            AudioListener[] listeners = hero.GetComponentsInChildren<AudioListener>(true);
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null)
                {
                    listeners[i].enabled = false;
                }
            }

            // Use the hero animator on the shared movement controller.
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
            // First valid player position becomes the respawn point.
            initialPlayerPosition = player.transform.position;
            hasInitialPlayerPosition = true;
        }
    }

    private void ApplyPlayerCamera(GameObject playerObject)
    {
        // The scene uses the assigned player camera as the only active camera.
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

        // Make sure the player camera owns the scene audio listener.
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
        // The original scene camera should not compete with the player camera.
        if (sceneMainCamera != null && sceneMainCamera != playerCamera)
        {
            DisableCamera(sceneMainCamera);
            return;
        }

    }

    private void SetupDemoCharacter(GameObject playerObject)
    {
        // Find the movement controller on the player hierarchy.
        demoCharacter = playerObject.GetComponentInChildren<DemoCharacter>(true);
        if (demoCharacter == null)
        {
            return;
        }

        demoCharacter.enabled = true;
        demoCharacter.SetCollisionOptions(false, false);

        if (visibleHero != null)
        {
            // Reuse the visible hero animator for movement animation.
            Animator heroAnimator = visibleHero.GetComponent<Animator>();
            if (heroAnimator != null)
            {
                demoCharacter.SetAnimator(heroAnimator);
            }
        }
    }

    private void ResetDemoCharacterState()
    {
        // Clear any locks left by menus, cutscenes, or previous scenes.
        DemoCharacter.ResetControlFlags();
        ClearPlayerMotionState();
    }

    private void ClearPlayerMotionState()
    {
        // Stops old velocity from carrying into respawn or cutscene snaps.
        if (demoCharacter == null)
        {
            return;
        }

        demoCharacter.ClearMotionState();
    }

    private void SetPlayerLocked(bool locked)
    {
        // Shared helper for final cutscene input locking.
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
        // Respawn at cached start if possible, otherwise use current position.
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
        // CharacterController must be disabled before changing transform position.
        if (player == null)
        {
            return;
        }

        if (playerController == null)
        {
            playerController = player.GetComponent<CharacterController>();
        }

        // Keep the previous controller enabled state.
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
        // Start with the requested position, then adjust it to the ground.
        Vector3 position = worldPosition;
        // CharacterController foot offset keeps the capsule from sinking into the floor.
        float controllerFootOffset = 0f;

        if (playerController == null && player != null)
        {
            playerController = player.GetComponent<CharacterController>();
        }

        if (playerController != null)
        {
            controllerFootOffset = playerController.center.y - playerController.height * 0.5f;
        }

        // Raycast down from above the target to find usable ground.
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
        // Only compare horizontal distance so height differences do not block the ending trigger.
        if (player == null || targetObject == null)
        {
            return false;
        }

        // Flatten both positions to the XZ plane.
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
        // End object doubles as the snap point for the final cutscene.
        GameObject lt1Target = endObject;
        // Move the player to the end object, adjusted to ground height.
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

        videoPlayer.Stop();
        string endingUrl = AssetFileUrl(endingVideoPath);
        bool useClip = endingVideoClip != null;
        videoPlayer.source = useClip ? VideoSource.VideoClip : VideoSource.Url;
        videoPlayer.clip = useClip ? endingVideoClip : null;
        videoPlayer.url = useClip ? string.Empty : endingUrl;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = activeVideoTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);
        videoPlayer.Prepare();

        // Avoid waiting forever if the video cannot prepare.
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

        // Match the render texture to the video size when Unity reports it.
        int textureWidth = videoPlayer.width > 0 ? (int)videoPlayer.width : 1920;
        int textureHeight = videoPlayer.height > 0 ? (int)videoPlayer.height : 1080;
        activeVideoTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32);
        activeVideoTexture.Create();
        videoPlayer.targetTexture = activeVideoTexture;
        videoImage.texture = activeVideoTexture;
        videoOverlayObject.SetActive(true);

        UpdateVideoLayout();

        videoPlayer.Play();

        // Some clips do not report length correctly, so fall back to a short timeout.
        float videoDuration = (float)videoPlayer.length;
        if (videoDuration <= 0f || videoDuration > 600f)
        {
            videoDuration = 10f;
        }

        // Stop waiting after the estimated end in case the loop event does not fire.
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
        // RenderTexture is created at runtime and must be released manually.
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
        // VideoPlayer event callback.
        videoFinished = true;
    }

    private void HandleVideoError(VideoPlayer source, string message)
    {
        // Treat video errors as finished so the game can return to menu.
        videoFinished = true;
    }

    private void UpdateVideoLayout()
    {
        if (videoImage == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        // Image rect fills the overlay while preserving the video aspect ratio.
        RectTransform imageRect = videoImage.rectTransform;
        RectTransform canvasRect = videoOverlayObject != null ? videoOverlayObject.GetComponent<RectTransform>() : null;
        // Default aspect if the VideoPlayer has not reported dimensions yet.
        float aspect = 16f / 9f;
        if (videoPlayer != null && videoPlayer.width > 0 && videoPlayer.height > 0)
        {
            aspect = (float)videoPlayer.width / videoPlayer.height;
        }

        // Use height as the anchor so the video fills vertically.
        float targetHeight = canvasRect != null ? canvasRect.rect.height : Screen.height;
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
        // Fullscreen overlay is drawn above all other UI.
        Canvas canvas = videoOverlayObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        // Scale overlay UI consistently across resolutions.
        CanvasScaler scaler = videoOverlayObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Black background hides the scene behind the video.
        GameObject blackBackground = new GameObject("Black Background", typeof(Image));
        blackBackground.transform.SetParent(videoOverlayObject.transform, false);
        // Stretch the black image to the whole screen.
        RectTransform backgroundRect = blackBackground.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        blackBackground.GetComponent<Image>().color = Color.black;

        // RawImage receives the RenderTexture from VideoPlayer.
        GameObject videoObject = new GameObject("Ending Video Image", typeof(RawImage));
        videoObject.transform.SetParent(videoOverlayObject.transform, false);
        videoImage = videoObject.GetComponent<RawImage>();

        // Video components live on the overlay so cleanup is simple.
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
        // Ending can play from an assigned clip or the StreamingAssets fallback mp4.
        return endingVideoClip != null || !string.IsNullOrEmpty(AssetFileUrl(endingVideoPath));
    }

    private static string AssetFileUrl(string relativeAssetPath)
    {
        if (string.IsNullOrWhiteSpace(relativeAssetPath))
        {
            return string.Empty;
        }

        string streamingPath = Path.Combine(Application.streamingAssetsPath, relativeAssetPath);
        if (File.Exists(streamingPath))
        {
            return new Uri(streamingPath).AbsoluteUri;
        }

        string assetPath = Path.Combine(Application.dataPath, relativeAssetPath);
        return File.Exists(assetPath) ? new Uri(assetPath).AbsoluteUri : string.Empty;
    }

    private void DisableCamera(Camera camera)
    {
        // Disable listener first to avoid duplicate AudioListener warnings.
        AudioListener extraListener = camera.GetComponent<AudioListener>();
        if (extraListener != null)
        {
            extraListener.enabled = false;
        }

        camera.gameObject.SetActive(false);
    }

}
