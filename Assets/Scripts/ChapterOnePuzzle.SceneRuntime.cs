using UnityEngine;

public partial class ChapterOnePuzzle
{
    private void RestorePlayerAfterDialogue()
    {
        AquariusMax.Fae.demo.DemoCharacter.ResetControlFlags();
        if (demoCharacter != null)
        {
            demoCharacter.ClearMotionState();
        }
    }

    private void SetUpChapterOneScene()
    {
        if (sceneReady)
        {
            return;
        }

        sceneReady = true;
        SetUpPuzzleState();

        if (!portalUnlocked)
        {
            if (portalTrigger != null && portalTrigger.gameObject.activeSelf)
            {
                portalTrigger.gameObject.SetActive(false);
            }

            if (portalDoor != null && portalDoor.activeSelf)
            {
                portalDoor.SetActive(false);
            }
        }

        if (!rescueApplied)
        {
            SetAudioSourcesPlayingInHierarchy(cage, true);
        }

        SetIndicatorVisible(redIndicator, false);
        SetIndicatorVisible(greenIndicator, false);
    }

    private void SetUpPuzzleState()
    {
        LoadPushStepReferences();
        SetSolvedBlockPositions();
        IgnorePlayerPushBlockCollisions();

        referencesReady =
            player != null &&
            strangeSymbol != null &&
            recognizeHelp != null &&
            strangeAltar != null &&
            askHelp != null &&
            fairy != null &&
            HasPushPuzzleReady();
    }

    private void SetPlayerControlReferences(bool clearMotionState)
    {
        if (demoCharacter == null)
        {
            return;
        }

        demoCharacter.enabled = true;
        demoCharacter.SetCollisionOptions(false, false);

        Animator boundAnimator = demoCharacter.GetCurrentAnimator();
        if (boundAnimator != null && boundAnimator.runtimeAnimatorController != null)
        {
            boundAnimator.applyRootMotion = false;
            boundAnimator.enabled = true;
        }

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
            playerCamera.enabled = true;
            demoCharacter.SetCamera(playerCamera);
        }

        if (playerAudioListener != null)
        {
            playerAudioListener.enabled = true;
        }

        if (clearMotionState)
        {
            demoCharacter.ClearMotionState();
        }
    }

    private bool HasPushPuzzleReady()
    {
        int expectedPushCount = pushSteps != null ? pushSteps.Length : 0;
        return player != null &&
            expectedPushCount > 0 &&
            pushBlocks.Count == expectedPushCount &&
            solvedBlockPositions != null &&
            solvedBlockPositions.Length == pushBlocks.Count &&
            completedPushes != null;
    }

    private void ApplySavedSceneState()
    {
        ApplySavedPushState();

        if (rescueApplied)
        {
            ApplyRescueSceneState();
            SetIndicatorVisible(greenIndicator, currentIndex >= requiredOrderedPushCount);
            SetIndicatorVisible(redIndicator, false);
        }

        if (portalUnlocked)
        {
            if (portalTrigger != null && !portalTrigger.gameObject.activeSelf)
            {
                portalTrigger.gameObject.SetActive(true);
            }

            if (portalDoor != null && !portalDoor.activeSelf)
            {
                portalDoor.SetActive(true);
            }
        }
    }

    private void ApplyRescueSceneState()
    {
        if (cage != null && cage.childCount > 0)
        {
            cage.GetChild(0).localPosition = cageChildSolvedLocalPosition;
            StopAudioSourcesInHierarchy(cage);
        }

        if (fairy != null)
        {
            fairy.position = fairySolvedWorldPosition;
            fairy.rotation *= Quaternion.Euler(fairySolvedEulerOffset);
        }
    }

    private bool IsPlayerNearTransform(Transform target, float distance)
    {
        return player != null &&
            target != null &&
            GetHorizontalDistanceToObject(Flatten(player.position), target) <= distance;
    }

    private bool IsPlayerNearStoryTarget(Transform target)
    {
        if (player == null || target == null)
        {
            return false;
        }

        float distance = Mathf.Max(storyAreaReachDistance, storyMinimumReachDistance);
        float directDistance = Vector3.Distance(Flatten(player.position), Flatten(target.position));
        if (directDistance <= distance)
        {
            return true;
        }

        if (TryGetTargetBounds(target, out Bounds bounds))
        {
            Vector3 position = player.position;
            bool nearY = Mathf.Abs(position.y - bounds.center.y) <= Mathf.Max(storyVerticalTolerance, bounds.extents.y + storyVerticalTolerance);
            Vector3 closest = bounds.ClosestPoint(new Vector3(position.x, bounds.center.y, position.z));
            float horizontalDistance = Vector3.Distance(Flatten(position), Flatten(closest));
            return nearY && horizontalDistance <= distance;
        }

        return GetHorizontalDistanceToObject(Flatten(player.position), target) <= distance;
    }

    private bool IsPlayerOnTrigger(Transform target, float defaultDistance)
    {
        if (player == null || target == null)
        {
            return false;
        }

        if (TryGetTargetBounds(target, out Bounds bounds))
        {
            Vector3 position = player.position;
            bool insideX = position.x >= bounds.min.x - triggerBoundsPadding && position.x <= bounds.max.x + triggerBoundsPadding;
            bool insideZ = position.z >= bounds.min.z - triggerBoundsPadding && position.z <= bounds.max.z + triggerBoundsPadding;
            bool nearY = Mathf.Abs(position.y - bounds.center.y) <= Mathf.Max(storyVerticalTolerance, bounds.extents.y + storyVerticalTolerance);
            return insideX && insideZ && nearY;
        }

        return GetHorizontalDistanceToObject(Flatten(player.position), target) <= defaultDistance;
    }

    private static bool TryGetTargetBounds(Transform target, out Bounds bounds)
    {
        bounds = new Bounds(target.position, Vector3.zero);
        return TryGetColliderBounds(target, ref bounds) || TryGetRendererBounds(target, ref bounds);
    }

    private static bool TryGetWorldBounds(Transform target, out Bounds bounds)
    {
        return TryGetTargetBounds(target, out bounds);
    }

    private static bool TryGetColliderBounds(Transform target, ref Bounds bounds)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        bool hasBounds = false;

        foreach (Collider collider in colliders)
        {
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

    private static bool TryGetRendererBounds(Transform target, ref Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
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

    private static void StopAudioSourcesInHierarchy(Transform root)
    {
        // Used when a story object should become silent without changing its hierarchy.
        if (root == null)
        {
            return;
        }

        AudioSource[] audioSources = root.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
            {
                audioSources[i].Stop();
            }
        }
    }

    private static void SetAudioSourcesPlayingInHierarchy(Transform root, bool shouldPlay)
    {
        if (root == null)
        {
            return;
        }

        AudioSource[] audioSources = root.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource audioSource = audioSources[i];
            if (audioSource == null)
            {
                continue;
            }

            if (shouldPlay)
            {
                if (audioSource.enabled && audioSource.gameObject.activeInHierarchy && !audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }
            else
            {
                audioSource.Stop();
            }
        }
    }
}
