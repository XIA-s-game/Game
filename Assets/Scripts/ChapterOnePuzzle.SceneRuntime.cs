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

        if (!forestAttackDialogueFinished && !enemiesActivated)
        {
            SetHeroVisible(false);
        }

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
        CollectDelayedEnemies();
    }

    private void SetUpPuzzleState()
    {
        LoadPushStepReferences();
        SetSolvedBlockPositions();
        IgnorePlayerPushBlockCollisions();

        referencesReady =
            player != null &&
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

        if (forestAttackDialogueFinished && fairy != null)
        {
            fairy.gameObject.SetActive(false);
        }

        if (enemiesActivated)
        {
            if (!heroCombatFinished)
            {
                GameAudioManager.StartRoarLoop();
            }

            ApplyEnemySceneState();
        }
        else if (!forestAttackDialogueFinished)
        {
            SetHeroVisible(false);
        }

        if (heroCombatFinished && hero != null)
        {
            SetHeroVisible(true);
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

    private void ApplyEnemySceneState()
    {
        CollectDelayedEnemies();
        bool anyEnemyActive = false;

        for (int i = 0; i < delayedEnemies.Count; i++)
        {
            GameObject enemy = delayedEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            bool shouldBeActive = true;
            enemy.SetActive(shouldBeActive);
            if (!shouldBeActive)
            {
                continue;
            }

            anyEnemyActive = true;
            SetAudioSourcesPlayingInHierarchy(enemy.transform, true);
        }

        for (int i = 0; i < delayedEnemyRenderers.Count; i++)
        {
            if (delayedEnemyRenderers[i] != null)
            {
                delayedEnemyRenderers[i].enabled = delayedEnemyRenderers[i].gameObject.activeInHierarchy;
            }
        }

        for (int i = 0; i < delayedEnemyColliders.Count; i++)
        {
            if (delayedEnemyColliders[i] != null)
            {
                delayedEnemyColliders[i].enabled = delayedEnemyColliders[i].gameObject.activeInHierarchy;
            }
        }

        for (int i = 0; i < delayedEnemyWalkers.Count; i++)
        {
            if (delayedEnemyWalkers[i] != null)
            {
                delayedEnemyWalkers[i].enabled = delayedEnemyWalkers[i].gameObject.activeInHierarchy;
            }
        }

        for (int i = 0; i < delayedEnemyAnimators.Count; i++)
        {
            if (delayedEnemyAnimators[i] != null)
            {
                delayedEnemyAnimators[i].enabled = delayedEnemyAnimators[i].gameObject.activeInHierarchy;
            }
        }

        if (hero == null)
        {
            return;
        }

        SetHeroVisible(true);
        heroCombatY = hero.position.y;

        if (heroCombatFinished)
        {
            heroCombatActive = false;
            heroAttacking = false;
            if (heroAnimator != null)
            {
                heroAnimator.speed = 0f;
            }

            GameAudioManager.StopEnemyLoop();
            return;
        }

        if (!anyEnemyActive)
        {
            GameAudioManager.StopEnemyLoop();
            FinishHeroCombat();
            return;
        }

        GameAudioManager.StartEnemyLoop();
        heroCombatActive = true;
        heroAttacking = false;
        heroTargetEnemy = FindNextHeroTarget();
        if (heroTargetEnemy != null)
        {
            PlayHeroAnimation(heroWalkController, heroWalkStateName);
        }
    }

    private void CacheHeroAnimator()
    {
        if (heroAnimator != null || hero == null)
        {
            return;
        }

        heroAnimator = hero.GetComponentInChildren<Animator>(true);
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

        float distance = Mathf.Max(storyAreaReachDistance, 6f);
        float directDistance = Vector3.Distance(Flatten(player.position), Flatten(target.position));
        if (directDistance <= distance)
        {
            return true;
        }

        if (TryGetTargetBounds(target, out Bounds bounds))
        {
            Vector3 position = player.position;
            bool nearY = Mathf.Abs(position.y - bounds.center.y) <= Mathf.Max(4f, bounds.extents.y + 4f);
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
            bool insideX = position.x >= bounds.min.x - 0.15f && position.x <= bounds.max.x + 0.15f;
            bool insideZ = position.z >= bounds.min.z - 0.15f && position.z <= bounds.max.z + 0.15f;
            bool nearY = Mathf.Abs(position.y - bounds.center.y) <= Mathf.Max(4f, bounds.extents.y + 4f);
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

}
