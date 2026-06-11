// Runs old tree dialogue choices, nest lowering, and peasant girl conversation.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class OldTreeInteraction
{
    private void ReadChoiceKeys()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Alpha1))
        {
            Choose(answerA);
        }
        else if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartBranchDialogue();
        }
        else if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartAngryAttack();
        }
    }

    private void Choose(string answer)
    {
        branchFlowActive = false;
        currentAnswer = answer;
        state = DialogueState.Answered;

        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }

        resetCoroutine = StartCoroutine(CloseAnswerAndReset());
    }

    private void StartBranchDialogue()
    {
        branchFlowActive = true;
        StartDialogue(new[]
        {
            "Old Tree: Can you see the nest on my branch?",
            "Old Tree: I will lower it so you can look closer."
        }, StartNestMove);
    }

    private void StartNestDialogue()
    {
        StartDialogue(new[]
        {
            "Old Tree: This nest belongs to the reed bird.",
            "Old Tree: One egg does not belong here.",
            "Old Tree: Some birds leave eggs in smaller nests.",
            "Old Tree: When the chick hatches, the other eggs may be pushed out.",
            "Old Tree: Nature can be hard to judge.",
            "Old Tree: I have a small test for your eyes.",
            "Old Tree: Find the different egg before time runs out.",
            "Old Tree: Stay focused."
        }, StartEggChallenge);
    }

    private void StartDialogue(string[] lines, System.Action onComplete, bool autoCompleteLastLine)
    {
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }

        if (finalInstructionCoroutine != null)
        {
            StopCoroutine(finalInstructionCoroutine);
            finalInstructionCoroutine = null;
        }

        currentLines = lines;
        currentLineIndex = 0;
        currentDialogueComplete = onComplete;
        autoCompleteOnLastLine = autoCompleteLastLine;
        currentAnswer = currentLines[0];
        state = DialogueState.Speaking;

        if (autoCompleteOnLastLine && currentLines.Length == 1)
        {
            currentDialogueComplete?.Invoke();
        }
    }

    private void ShowNextLine()
    {
        currentLineIndex++;
        if (currentLines != null && currentLineIndex < currentLines.Length)
        {
            currentAnswer = currentLines[currentLineIndex];
            TryActivateSideQuestFromDialogue(currentAnswer);

            if (autoCompleteOnLastLine && currentLineIndex == currentLines.Length - 1)
            {
                System.Action finalLineComplete = currentDialogueComplete;
                currentLines = null;
                currentDialogueComplete = null;
                autoCompleteOnLastLine = false;
                finalLineComplete?.Invoke();
            }

            return;
        }

        System.Action dialogueComplete = currentDialogueComplete;
        currentLines = null;
        currentDialogueComplete = null;
        autoCompleteOnLastLine = false;
        dialogueComplete?.Invoke();
    }

    private void StartNestMove()
    {
        currentAnswer = null;
        state = DialogueState.MovingNest;

        if (nestMoveCoroutine != null)
        {
            StopCoroutine(nestMoveCoroutine);
        }

        nestMoveCoroutine = StartCoroutine(MoveNestBranchDown());
    }

    private IEnumerator MoveNestBranchDown()
    {
        Transform movingTarget = nestBranch != null ? nestBranch : nest;
        if (movingTarget == null)
        {
            StartNestDialogue();
            nestMoveCoroutine = null;
            yield break;
        }

        Vector3 startPosition = movingTarget.position;
        Vector3 targetPosition = new Vector3(startPosition.x, nestTargetY, startPosition.z);
        float elapsed = 0f;

        while (elapsed < nestMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / nestMoveDuration);
            movingTarget.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        movingTarget.position = targetPosition;
        nestMoveCoroutine = null;
        StartNestDialogue();
    }

    private void CloseDialogueAndReset()
    {
        if (eggResultCoroutine != null)
        {
            StopCoroutine(eggResultCoroutine);
            eggResultCoroutine = null;
        }

        ClearEggGrid();
        DisableMushroomGlow();
        UnlockPlayerForEggChallenge();
        currentAnswer = null;
        eggResultText = null;
        branchFlowActive = false;
        state = DialogueState.Waiting;
        ResetTreeToInitialState();
        StartLookCoroutine(ReturnToOriginalRotation());
    }

    private void CloseSideQuestReminder()
    {
        currentAnswer = null;
        state = DialogueState.Waiting;
        StartLookCoroutine(ReturnToOriginalRotation());
    }

    private void StartPeasantGirlDialogue()
    {
        SetPeasantStand();
        StartDialogue(new[]
        {
            peasantFirstLine,
            peasantSecondLine
        }, CompletePeasantGirlTrade);
    }

    private void CompletePeasantGirlTrade()
    {
        peasantRewardGiven = true;
        collectedSaplingCount = requiredSaplingCount;
        GlobalBackpackUI.SetItemCount(saplingInventoryName, Mathf.Max(0, collectedSaplingCount - plantedSaplingCount));
        currentAnswer = null;
        state = DialogueState.Waiting;
        ShowSaplingPlantTargets();
    }

    private void ShowFinalInstructionForFiveSeconds()
    {
        state = DialogueState.FinalInstruction;

        if (finalInstructionCoroutine != null)
        {
            StopCoroutine(finalInstructionCoroutine);
        }

        finalInstructionCoroutine = StartCoroutine(CloseFinalInstructionAfterDelay());
    }

    private IEnumerator CloseFinalInstructionAfterDelay()
    {
        yield return new WaitForSeconds(answerDuration);

        finalInstructionCoroutine = null;
        CloseDialogueAndReset();
    }

    private void TryActivateSideQuestFromDialogue(string line)
    {
        if (sideQuestActivatedOnce || string.IsNullOrEmpty(line))
        {
            return;
        }

        if (!line.Contains("safe shelter"))
        {
            return;
        }

        ActivateFairyBackstorySideQuest();
    }

    public void ActivateFairyBackstorySideQuest()
    {
        if (sideQuestActivatedOnce)
        {
            return;
        }

        sideQuestActivatedOnce = true;
        sideQuestActive = true;
        collectedFenceCount = 0;
        collectedSaplingCount = 0;
        plantedSaplingCount = 0;
        GlobalBackpackUI.RemoveAll(fenceInventoryName);
        GlobalBackpackUI.RemoveAll(saplingInventoryName);
        fenceBuilt = false;
        fenceBuildTargetShown = false;
        nearbyFenceBuildTarget = false;
        peasantRewardGiven = false;
        saplingPlantTargetsShown = false;
        nearbySaplingPlantTarget = null;
        if (fenceBuildTarget != null)
        {
            fenceBuildTarget.gameObject.SetActive(false);
        }

        PrepareSaplingPlantTargets();
        CollectFenceTargets();
    }
}
