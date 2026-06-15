using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterOnePuzzle
{
    private void UpdateMainlineStory()
    {
        // Story order: hear help, inspect altar, ask fairy, solve puzzle, then use the portal.
        if (UpdateActiveDialogue())
        {
            return;
        }

        UpdateStrangeSymbolPrompt();
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

    private void UpdateStrangeSymbolPrompt()
    {
        if (strangeSymbolPromptShown || !IsPlayerNearStoryTarget(strangeSymbol))
        {
            return;
        }

        strangeSymbolPromptShown = true;
        storyPrompt = strangeSymbolPrompt;
        storyPromptEndsAt = Time.time + promptDuration;
        GameAudioManager.PlayKnob();
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
        storyPromptEndsAt = Time.time + promptDuration;
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
        storyPromptEndsAt = Time.time + promptDuration;
        GameAudioManager.PlayKnob();
    }

    private void UpdateAskHelpInteraction()
    {
        // Before rescue this is the ask-help point; after rescue it becomes the fairy reward talk.
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

    private void UnlockPortal()
    {
        // The rescued fairy opens the portal after giving the first page.
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
