using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterOnePuzzle
{
    private void UpdateMainlineStory()
    {
        // Story order: hear help, inspect altar, ask fairy, solve puzzle, then start the attack sequence.
        if (UpdateActiveDialogue())
        {
            return;
        }

        UpdateRecognizeHelpPrompt();
        UpdateStrangeAltarPrompt();
        UpdateAskHelpInteraction();
    }

    private bool UpdateActiveDialogue()
    {
        // Dialogue consumes input while open so puzzle and portal interactions do not fire underneath it.
        if (!helpDialogueActive)
        {
            return false;
        }

        if (activeDialogueLines == null || activeDialogueLines.Length == 0)
        {
            EndDialogueWithoutResult();
            return true;
        }

        if (helpDialogueIndex < 0 || helpDialogueIndex >= activeDialogueLines.Length)
        {
            FinishActiveDialogue();
            return true;
        }

        if (Input.GetKeyDown(activeDialogueContinueKey))
        {
            interactionInputConsumed = true;
            helpDialogueIndex++;
            if (activeDialogueLines == null || helpDialogueIndex >= activeDialogueLines.Length)
            {
                FinishActiveDialogue();
            }
        }

        return true;
    }

    private void UpdateRecognizeHelpPrompt()
    {
        // First story hint is shown once when the player reaches the help marker.
        if (recognizeHelpShown || !IsPlayerNearStoryTarget(recognizeHelp))
        {
            return;
        }

        recognizeHelpShown = true;
        storyPrompt = recognizeHelpPrompt;
        storyPromptEndsAt = Time.time + 3f;
        GameAudioManager.PlayKnob();
    }

    private void UpdateStrangeAltarPrompt()
    {
        // The altar prompt explains the puzzle area before the fairy gives the six-step clue.
        if (strangeAltarPromptShown || !IsPlayerNearStoryTarget(strangeAltar))
        {
            return;
        }

        strangeAltarPromptShown = true;
        storyPrompt = strangeAltarPrompt;
        storyPromptEndsAt = Time.time + 3f;
    }

    private void UpdateAskHelpInteraction()
    {
        // Before rescue this is the ask-help point; after rescue it becomes the fairy reward talk.
        if (forestAttackDialogueFinished)
        {
            return;
        }

        Transform interactionTarget = rescueApplied ? fairy : askHelp;
        if (rescueApplied && pageRewardFinished)
        {
            return;
        }

        askHelpPromptVisible = IsPlayerNearStoryTarget(interactionTarget);
        if (!askHelpPromptVisible || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        interactionInputConsumed = true;
        if (rescueApplied)
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

    private void EndDialogueWithoutResult()
    {
        helpDialogueActive = false;
        helpDialogueIndex = 0;
        activeDialogueLines = null;
        RestorePlayerAfterDialogue();
    }

    private void FinishActiveDialogue()
    {
        // Dialogue completion advances the story flags that unlock later encounters.
        string[] finishedLines = activeDialogueLines;
        helpDialogueActive = false;
        helpDialogueIndex = 0;
        activeDialogueLines = null;
        RestorePlayerAfterDialogue();

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
        // The first page is awarded once, even if dialogue state is refreshed.
        if (firstPageAddedToBackpack)
        {
            return;
        }

        firstPageAddedToBackpack = true;
        GlobalBackpackUI.AddItem(firstPageItemName);
    }

    private void ApplyRescueResult()
    {
        // Puzzle success opens the cage and moves the fairy to the post-rescue position.
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

    private void CollectDelayedEnemies()
    {
        // Enemies start hidden so the forest attack feels triggered by the story beat.
        if (delayedEnemiesCollected)
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
            delayedEnemiesCollected = true;
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

        delayedEnemiesCollected = true;
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
        // After the attack dialogue, entering the trigger wakes the hidden enemies.
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
        // Restores enemy visuals, colliders, walking scripts, and audio at the same time.
        CollectDelayedEnemies();
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
        // Hero appears after the ambush and automatically fights the delayed enemies.
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
        // Combat ending stops attack audio and unlocks the final hero conversation.
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
        // Player can talk to the hero during combat for warning, then after combat for portal unlock.
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
            interactionInputConsumed = true;
            heroPromptVisible = false;
            StartDialogue(heroAfterCombatDialogueLines, KeyCode.C, "Press C to continue");
        }
    }

    private void UnlockPortal()
    {
        // Portal is only visible after the hero explains the next destination.
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
        // Final Chapter One interaction moves the player to Fae Homes Demo.
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
            interactionInputConsumed = true;
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
