using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterOnePuzzle
{
    private void UpdateHelpStory()
    {
        if (helpDialogueActive)
        {
            if (Input.GetKeyDown(activeDialogueContinueKey))
            {
                helpDialogueIndex++;
                if (activeDialogueLines == null || helpDialogueIndex >= activeDialogueLines.Length)
                {
                    FinishActiveDialogue();
                }
            }

            return;
        }

        if (!recognizeHelpShown && IsPlayerNearTransform(recognizeHelp, storyAreaReachDistance))
        {
            recognizeHelpShown = true;
            storyPrompt = recognizeHelpPrompt;
            storyPromptEndsAt = Time.time + 3f;
            GameAudioManager.PlayKnob();
        }

        Transform interactionTarget = rescueApplied ? fairy : askHelp;
        askHelpPromptVisible = IsPlayerNearTransform(interactionTarget, storyAreaReachDistance) &&
            !forestAttackDialogueFinished &&
            (!rescueApplied || !pageRewardFinished);
        if (askHelpPromptVisible && Input.GetKeyDown(KeyCode.E))
        {
            if (rescueApplied && !pageRewardFinished)
            {
                StartDialogue(pageRewardDialogueLines, KeyCode.E, "Press E to continue");
            }
            else if (initialHelpDialogueFinished)
            {
                StartDialogue(clueDialogueLines, KeyCode.C, "Press C to continue");
            }
            else
            {
                StartDialogue(helpDialogueLines, KeyCode.C, "Press C to continue");
            }
        }
    }

    private void StartDialogue(string[] lines, KeyCode continueKey, string continueHint)
    {
        if (lines == null || lines.Length == 0)
        {
            return;
        }

        activeDialogueLines = lines;
        activeDialogueContinueKey = continueKey;
        activeDialogueContinueHint = continueHint;
        helpDialogueActive = true;
        askHelpPromptVisible = false;
        helpDialogueIndex = 0;
    }

    private void FinishActiveDialogue()
    {
        string[] finishedLines = activeDialogueLines;
        helpDialogueActive = false;
        helpDialogueIndex = 0;

        if (finishedLines == helpDialogueLines)
        {
            initialHelpDialogueFinished = true;
        }
        else if (finishedLines == pageRewardDialogueLines)
        {
            pageRewardFinished = true;
            AddFirstPageToBackpack();
            GameAudioManager.StartRoarLoop();
            StartDialogue(forestAttackDialogueLines, KeyCode.C, "Press C to continue");
        }
        else if (finishedLines == forestAttackDialogueLines)
        {
            forestAttackDialogueFinished = true;
            if (fairy != null)
            {
                fairy.gameObject.SetActive(false);
            }
        }
        else if (finishedLines == heroAfterCombatDialogueLines)
        {
            heroPostCombatDialogueFinished = true;
            UnlockPortal();
        }
    }

    private void AddFirstPageToBackpack()
    {
        if (firstPageAddedToBackpack)
        {
            return;
        }

        firstPageAddedToBackpack = true;
        GlobalBackpackUI.AddItem(firstPageItemName);
    }

    private void ApplyRescueResult()
    {
        if (rescueApplied)
        {
            return;
        }

        rescueApplied = true;
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

    private void PrepareDelayedEnemies()
    {
        if (enemiesPrepared)
        {
            return;
        }

        delayedEnemies.Clear();
        delayedEnemyRenderers.Clear();
        delayedEnemyColliders.Clear();
        delayedEnemyWalkers.Clear();
        delayedEnemyAnimators.Clear();
        if (delayedEnemyObjects == null)
        {
            enemiesPrepared = true;
            return;
        }

        for (int i = 0; i < delayedEnemyObjects.Length; i++)
        {
            GameObject enemy = delayedEnemyObjects[i];
            if (enemy == null || delayedEnemies.Contains(enemy))
            {
                continue;
            }

            delayedEnemies.Add(enemy);
            HideDelayedEnemy(enemy);
            SetAudioSourcesPlayingInHierarchy(enemy.transform, false);
        }

        enemiesPrepared = true;
    }

    private void HideDelayedEnemy(GameObject enemy)
    {
        Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && renderer.enabled)
            {
                delayedEnemyRenderers.Add(renderer);
                renderer.enabled = false;
            }
        }

        Collider[] colliders = enemy.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider != null && collider.enabled)
            {
                delayedEnemyColliders.Add(collider);
                collider.enabled = false;
            }
        }

        RouteWaypointWalker[] walkers = enemy.GetComponentsInChildren<RouteWaypointWalker>(true);
        foreach (RouteWaypointWalker walker in walkers)
        {
            if (walker != null && walker.enabled)
            {
                delayedEnemyWalkers.Add(walker);
                walker.enabled = false;
            }
        }

        Animator[] animators = enemy.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            if (animator != null && animator.enabled)
            {
                delayedEnemyAnimators.Add(animator);
                animator.enabled = false;
            }
        }
    }

    private void UpdateEnemyAmbush()
    {
        if (enemiesActivated || !forestAttackDialogueFinished)
        {
            return;
        }

        if (enemyTrigger == null || !IsPlayerOnTrigger(enemyTrigger, enemyTriggerDistance))
        {
            return;
        }

        ActivateDelayedEnemies();
    }

    private void ActivateDelayedEnemies()
    {
        PrepareDelayedEnemies();
        enemiesActivated = true;
        GameAudioManager.StartEnemyLoop();

        for (int i = 0; i < delayedEnemies.Count; i++)
        {
            GameObject enemy = delayedEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            RouteWaypointWalker walker = enemy.GetComponent<RouteWaypointWalker>();
            if (walker != null && !delayedEnemyWalkers.Contains(walker))
            {
                delayedEnemyWalkers.Add(walker);
            }

            SetAudioSourcesPlayingInHierarchy(enemy.transform, true);
        }

        for (int i = 0; i < delayedEnemyRenderers.Count; i++)
        {
            if (delayedEnemyRenderers[i] != null)
            {
                delayedEnemyRenderers[i].enabled = true;
            }
        }

        for (int i = 0; i < delayedEnemyColliders.Count; i++)
        {
            if (delayedEnemyColliders[i] != null)
            {
                delayedEnemyColliders[i].enabled = true;
            }
        }

        for (int i = 0; i < delayedEnemyWalkers.Count; i++)
        {
            if (delayedEnemyWalkers[i] != null)
            {
                delayedEnemyWalkers[i].enabled = true;
            }
        }

        for (int i = 0; i < delayedEnemyAnimators.Count; i++)
        {
            if (delayedEnemyAnimators[i] != null)
            {
                delayedEnemyAnimators[i].enabled = true;
            }
        }

        StartHeroCombat();
    }

    private void StartHeroCombat()
    {
        if (hero == null)
        {
            return;
        }

        SetHeroVisible(true);
        CacheHeroAnimator();

        if (heroAnimator != null)
        {
            heroAnimator.applyRootMotion = false;
        }

        heroCombatY = hero.position.y;
        if (heroAnimator != null)
        {
            heroAnimator.speed = 1f;
        }

        heroCombatActive = true;
        heroCombatFinished = false;
        heroAttacking = false;
        defeatedEnemies.Clear();
        heroTargetEnemy = FindNextHeroTarget();
        PlayHeroAnimation(heroWalkController, heroWalkStateName);
    }

    private void SetHeroVisible(bool visible)
    {
        if (hero == null)
        {
            return;
        }

        CacheHeroAnimator();

        if (heroAnimator != null)
        {
            heroAnimator.enabled = visible;
        }

        Renderer[] renderers = hero.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = visible;
            }
        }

        Collider[] colliders = hero.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = visible;
            }
        }
    }

    private void FinishHeroCombat()
    {
        heroCombatActive = false;
        heroAttacking = false;
        heroCombatFinished = true;
        heroPromptVisible = false;
        GameAudioManager.StopRoarLoop();
        GameAudioManager.StopEnemyLoop();

        if (heroAnimator != null)
        {
            heroAnimator.speed = 0f;
        }
    }

    private void UpdateHeroStory()
    {
        heroPromptVisible = false;
        if (helpDialogueActive || hero == null || !enemiesActivated)
        {
            return;
        }

        if (heroCombatActive)
        {
            if (!heroWarningShown && IsPlayerNearTransform(hero, heroInteractDistance))
            {
                heroWarningShown = true;
                StartDialogue(heroWarningDialogueLines, KeyCode.C, "Press C to continue");
            }

            return;
        }

        if (!heroCombatFinished || heroPostCombatDialogueFinished || !IsPlayerNearTransform(hero, heroInteractDistance))
        {
            return;
        }

        heroPromptVisible = true;
        if (Input.GetKeyDown(KeyCode.E))
        {
            heroPromptVisible = false;
            StartDialogue(heroAfterCombatDialogueLines, KeyCode.C, "Press C to continue");
        }
    }

    private void UnlockPortal()
    {
        portalUnlocked = true;

        if (portalTrigger != null && !portalTrigger.gameObject.activeSelf)
        {
            portalTrigger.gameObject.SetActive(true);
        }

        if (portalDoor != null && !portalDoor.activeSelf)
        {
            portalDoor.SetActive(true);
        }
    }

    private void UpdatePortalInteraction()
    {
        portalPromptVisible = false;
        if (!portalUnlocked || helpDialogueActive)
        {
            return;
        }

        if (portalTrigger != null && !portalTrigger.gameObject.activeSelf)
        {
            portalTrigger.gameObject.SetActive(true);
        }

        if (portalDoor != null && !portalDoor.activeSelf)
        {
            portalDoor.SetActive(true);
        }

        if (!IsPlayerOnTrigger(portalTrigger, portalInteractDistance))
        {
            return;
        }

        portalPromptVisible = true;
        if (Input.GetKeyDown(KeyCode.E))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
